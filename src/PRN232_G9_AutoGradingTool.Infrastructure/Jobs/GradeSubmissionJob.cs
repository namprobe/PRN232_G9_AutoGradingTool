using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Jobs;

[Queue("grading")]
public class GradeSubmissionJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGradingProcessService _processSvc;
    private readonly IGradingResultParser _resultParser;
    private readonly IFileServiceFactory _fileFactory;
    private readonly ILogger<GradeSubmissionJob> _logger;

    public GradeSubmissionJob(
        IServiceScopeFactory scopeFactory,
        IGradingProcessService processSvc,
        IGradingResultParser resultParser,
        IFileServiceFactory fileFactory,
        ILogger<GradeSubmissionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _processSvc = processSvc;
        _resultParser = resultParser;
        _fileFactory = fileFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid gradingJobId, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var job = await uow.Repository<GradingJob>()
            .GetQueryable()
            .Include(j => j.ExamSubmission)
                .ThenInclude(s => s.SubmissionFiles)
            .Include(j => j.ExamSubmission)
                .ThenInclude(s => s.Result)
            .FirstOrDefaultAsync(j => j.Id == gradingJobId, ct);

        if (job is null)
        {
            _logger.LogWarning("GradeSubmissionJob: job {Id} not found.", gradingJobId);
            return;
        }

        var submission = job.ExamSubmission;
        var tempDir = Path.Combine(Path.GetTempPath(), "autograde", gradingJobId.ToString("N"));
        var processes = new List<(string Label, Process Proc, int Port)>();

        try
        {
            var session = await uow.Repository<ExamSession>()
                .GetQueryable()
                .Include(s => s.Semester)
                .Include(s => s.Topics)
                    .ThenInclude(t => t.Questions)
                        .ThenInclude(q => q.TestCases)
                .FirstOrDefaultAsync(s => s.Id == submission.ExamSessionId, ct)
                ?? throw new InvalidOperationException($"ExamSession {submission.ExamSessionId} was not found.");

            var gradingPlan = TopicAwareSubmissionResolver.Resolve(session, submission.SubmissionFiles);

            job.JobStatus = GradingJobStatus.Running;
            job.StartedAtUtc = DateTime.UtcNow;
            job.ErrorMessage = null;
            submission.WorkflowStatus = ExamSubmissionStatus.Running;
            uow.Repository<GradingJob>().Update(job);
            uow.Repository<ExamSubmission>().Update(submission);
            await uow.SaveChangesAsync(ct);

            await AddLogAsync(gradingJobId, GradingJobLogPhase.Extract, GradingJobLogLevel.Info, "Start grading submission.");
            await AddLogAsync(
                gradingJobId,
                GradingJobLogPhase.Discover,
                GradingJobLogLevel.Info,
                $"Resolved topic {gradingPlan.Topic.Id:N} from submission file paths.");

            var fileService = _fileFactory.CreateFileService();
            var extractedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var appFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var collectionPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Directory.CreateDirectory(tempDir);
            var rawDir = Path.Combine(tempDir, "raw");
            Directory.CreateDirectory(rawDir);

            foreach (var resolvedFile in gradingPlan.Files)
            {
                var label = resolvedFile.Question.Label;
                var (bytes, _) = await fileService.GetFileContentAsync(resolvedFile.SubmissionFile.StorageRelativePath);

                var zipPath = Path.Combine(rawDir, $"{label}.zip");
                await File.WriteAllBytesAsync(zipPath, bytes, ct);

                var extractedDir = _processSvc.ExtractZip(zipPath, tempDir);
                extractedPaths[label] = extractedDir;

                await AddLogAsync(
                    gradingJobId,
                    GradingJobLogPhase.Extract,
                    GradingJobLogLevel.Info,
                    $"Extracted {label} from {resolvedFile.SubmissionFile.StorageRelativePath} to {extractedDir}.");

                var appFolder = ResolveAppFolder(label, extractedDir);
                appFolders[label] = appFolder;

                await AddLogAsync(
                    gradingJobId,
                    GradingJobLogPhase.Discover,
                    GradingJobLogLevel.Info,
                    $"{label} app folder: {appFolder}");

                var collectionPath = Path.Combine(tempDir, $"collection.{label}.generated.json");
                var collectionJson = BuildCollectionJson(session, resolvedFile.Question);
                await File.WriteAllTextAsync(collectionPath, collectionJson, ct);
                collectionPaths[label] = collectionPath;

                await AddLogAsync(
                    gradingJobId,
                    GradingJobLogPhase.RunNewman,
                    GradingJobLogLevel.Info,
                    $"Generated Newman collection for {gradingPlan.Topic.Title} / {label}.");
            }

            foreach (var (label, folder) in appFolders)
            {
                var port = GetFreePort();
                var proc = _processSvc.RunApp(folder, port);
                await WaitForAppReadyAsync(port, ct, timeoutSec: 30);
                processes.Add((label, proc, port));

                await AddLogAsync(
                    gradingJobId,
                    GradingJobLogPhase.RunServer,
                    GradingJobLogLevel.Info,
                    $"{label} server started on port {port}.");
            }

            var rawOutputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (label, _, port) in processes)
            {
                var newmanProc = _processSvc.RunNewman(collectionPaths[label], $"http://localhost:{port}", tempDir);
                var json = await _processSvc.CaptureProcessOutputAsync(newmanProc, ct);
                rawOutputs[label] = json;

                await AddLogAsync(
                    gradingJobId,
                    GradingJobLogPhase.RunNewman,
                    GradingJobLogLevel.Info,
                    $"Newman completed for {label}.",
                    json);
            }

            var computed = BuildComputedSubmissionResult(gradingPlan.Topic, rawOutputs);
            foreach (var question in computed.Questions)
            {
                await AddLogAsync(
                    gradingJobId,
                    GradingJobLogPhase.Grade,
                    GradingJobLogLevel.Info,
                    $"{question.Label}: {question.Score}/{question.MaxScore} points");
            }

            await PersistCompletedGradingAsync(gradingJobId, computed, ct);

            await AddLogAsync(
                gradingJobId,
                GradingJobLogPhase.Grade,
                GradingJobLogLevel.Info,
                $"Grading completed. Total score: {computed.TotalScore}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GradeSubmissionJob {Id} failed.", gradingJobId);
            await AddLogAsync(
                gradingJobId,
                GradingJobLogPhase.Grade,
                GradingJobLogLevel.Error,
                ex.Message[..Math.Min(ex.Message.Length, 4000)]);

            await MarkJobFailedAsync(gradingJobId, ex.Message, CancellationToken.None);
            throw;
        }
        finally
        {
            foreach (var (label, proc, _) in processes)
            {
                try
                {
                    _processSvc.CleanupResources(proc, null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cleanup process {Label} failed.", label);
                }
            }

            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);

                await AddLogAsync(
                    gradingJobId,
                    GradingJobLogPhase.Cleanup,
                    GradingJobLogLevel.Info,
                    "Cleaned up temporary grading folder.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cleanup temp dir {Dir} failed.", tempDir);
            }
        }
    }

    private async Task PersistCompletedGradingAsync(
        Guid gradingJobId,
        ComputedSubmissionResult computed,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            await uow.BeginTransactionAsync(ct);

            var job = await uow.Repository<GradingJob>()
                .GetQueryable()
                .Include(j => j.ExamSubmission)
                    .ThenInclude(s => s.Result)
                .FirstOrDefaultAsync(j => j.Id == gradingJobId, ct)
                ?? throw new InvalidOperationException($"GradingJob {gradingJobId} was not found.");

            var submission = job.ExamSubmission;

            var oldTcScores = await uow.Repository<ExamTestCaseScore>()
                .GetQueryable()
                .Where(x => x.ExamSubmissionId == submission.Id)
                .ToListAsync(ct);
            uow.Repository<ExamTestCaseScore>().DeleteRange(oldTcScores);

            var oldQScores = await uow.Repository<ExamQuestionScore>()
                .GetQueryable()
                .Where(x => x.ExamSubmissionId == submission.Id)
                .ToListAsync(ct);
            uow.Repository<ExamQuestionScore>().DeleteRange(oldQScores);

            TestResult resultEntity;
            if (submission.Result is null)
            {
                resultEntity = new TestResult
                {
                    SubmissionId = submission.Id,
                    Status = EntityStatusEnum.Active
                };
                resultEntity.InitializeEntity();
                await uow.Repository<TestResult>().AddAsync(resultEntity, ct);
                await uow.SaveChangesAsync(ct);
            }
            else
            {
                resultEntity = submission.Result;

                var oldDetails = await uow.Repository<TestResultDetail>()
                    .GetQueryable()
                    .Where(x => x.ResultId == resultEntity.Id)
                    .ToListAsync(ct);
                uow.Repository<TestResultDetail>().DeleteRange(oldDetails);
            }

            foreach (var question in computed.Questions)
            {
                foreach (var testCase in question.TestCases)
                {
                    if (testCase.SaveResultDetail)
                    {
                        var tcDetail = new TestResultDetail
                        {
                            ResultId = resultEntity.Id,
                            TestCaseId = testCase.TestCaseId,
                            Passed = testCase.DetailPassed,
                            Score = testCase.DetailScore,
                            ResponseTime = testCase.DetailResponseTime,
                            ErrorMessage = testCase.DetailErrorMessage,
                            Status = EntityStatusEnum.Active
                        };
                        tcDetail.InitializeEntity();
                        await uow.Repository<TestResultDetail>().AddAsync(tcDetail, ct);
                    }

                    var tcScore = new ExamTestCaseScore
                    {
                        ExamSubmissionId = submission.Id,
                        ExamTestCaseId = testCase.TestCaseId,
                        PointsEarned = testCase.PointsEarned,
                        MaxPoints = testCase.MaxPoints,
                        Outcome = testCase.Outcome,
                        Message = testCase.Message,
                        RawOutputJson = testCase.RawOutputJson,
                        Status = EntityStatusEnum.Active
                    };
                    tcScore.InitializeEntity();
                    await uow.Repository<ExamTestCaseScore>().AddAsync(tcScore, ct);
                }

                var qScore = new ExamQuestionScore
                {
                    ExamSubmissionId = submission.Id,
                    ExamQuestionId = question.QuestionId,
                    Score = question.Score,
                    MaxScore = question.MaxScore,
                    Status = EntityStatusEnum.Active
                };
                qScore.InitializeEntity();
                await uow.Repository<ExamQuestionScore>().AddAsync(qScore, ct);
            }

            submission.TotalScore = computed.TotalScore;
            submission.WorkflowStatus = ExamSubmissionStatus.Completed;

            resultEntity.TotalScore = (double)computed.TotalScore;
            resultEntity.TestStatus = computed.TotalScore >= computed.TotalMaxScore * 0.5m
                ? ExamTestCaseOutcome.Pass
                : ExamTestCaseOutcome.Fail;

            job.JobStatus = GradingJobStatus.Completed;
            job.ErrorMessage = null;
            job.FinishedAtUtc = DateTime.UtcNow;

            uow.Repository<TestResult>().Update(resultEntity);
            uow.Repository<ExamSubmission>().Update(submission);
            uow.Repository<GradingJob>().Update(job);

            await uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await uow.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private async Task MarkJobFailedAsync(Guid gradingJobId, string errorMessage, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var job = await uow.Repository<GradingJob>()
                .GetQueryable()
                .Include(x => x.ExamSubmission)
                .FirstOrDefaultAsync(x => x.Id == gradingJobId, ct);

            if (job is null)
                return;

            job.JobStatus = GradingJobStatus.Failed;
            job.ErrorMessage = errorMessage[..Math.Min(errorMessage.Length, 4000)];
            job.FinishedAtUtc = DateTime.UtcNow;
            job.ExamSubmission.WorkflowStatus = ExamSubmissionStatus.Failed;

            uow.Repository<GradingJob>().Update(job);
            uow.Repository<ExamSubmission>().Update(job.ExamSubmission);
            await uow.SaveChangesAsync(ct);
        }
        catch (Exception saveEx)
        {
            _logger.LogError(saveEx, "Failed to save failure state for job {Id}.", gradingJobId);
        }
    }

    private async Task AddLogAsync(
        Guid jobId,
        GradingJobLogPhase phase,
        GradingJobLogLevel level,
        string message,
        string? detail = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var log = new GradingJobLog
        {
            GradingJobId = jobId,
            Phase = phase,
            Level = level,
            Message = message,
            DetailJson = detail,
            OccurredAtUtc = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        log.InitializeEntity();

        try
        {
            await uow.Repository<GradingJobLog>().AddAsync(log, CancellationToken.None);
            await uow.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save log for job {Id}.", jobId);
        }
    }

    private string ResolveAppFolder(string questionLabel, string extractedDir)
    {
        var directMatch = Directory
            .GetDirectories(extractedDir, "*", SearchOption.AllDirectories)
            .FirstOrDefault(directory =>
                Path.GetFileName(directory).StartsWith($"{questionLabel}_", StringComparison.OrdinalIgnoreCase));

        if (directMatch is not null)
            return directMatch;

        throw new InvalidOperationException(
            $"Could not find published app folder for {questionLabel} in {extractedDir}.");
    }

    private ComputedSubmissionResult BuildComputedSubmissionResult(
        ExamTopic topic,
        IReadOnlyDictionary<string, string> rawOutputs)
    {
        var parsedByLabel = rawOutputs.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var parsed = _resultParser.ParseNewmanTestResults(kvp.Value);
                return parsed.ToDictionary(r => r.TestCaseName, StringComparer.OrdinalIgnoreCase);
            },
            StringComparer.OrdinalIgnoreCase);

        var questions = new List<ComputedQuestionResult>();
        decimal totalScore = 0;
        decimal totalMaxScore = 0;

        foreach (var question in topic.Questions.OrderBy(q => q.Label, StringComparer.OrdinalIgnoreCase))
        {
            parsedByLabel.TryGetValue(question.Label, out var parsedByName);
            parsedByName ??= new Dictionary<string, ResultDetail>(StringComparer.OrdinalIgnoreCase);

            decimal questionScore = 0;
            var testCases = new List<ComputedTestCaseResult>();

            foreach (var tc in question.TestCases.OrderBy(t => t.SortOrder))
            {
                var outcome = ExamTestCaseOutcome.Fail;
                decimal earned = 0;
                string? message = null;
                string? rawJson = null;
                var saveResultDetail = false;
                var detailPassed = false;
                double detailScore = 0;
                var detailResponseTime = 0;
                var detailErrorMessage = string.Empty;

                if (parsedByName.TryGetValue(tc.Name, out var detail))
                {
                    outcome = detail.Passed ? ExamTestCaseOutcome.Pass : ExamTestCaseOutcome.Fail;
                    earned = detail.Passed ? tc.MaxPoints : 0;
                    message = string.IsNullOrWhiteSpace(detail.ErrorMessage) ? null : detail.ErrorMessage;
                    rawJson = detail.RawOutputJson;
                    saveResultDetail = true;
                    detailPassed = detail.Passed;
                    detailScore = detail.Score;
                    detailResponseTime = detail.ResponseTime;
                    detailErrorMessage = detail.ErrorMessage;
                }

                testCases.Add(new ComputedTestCaseResult(
                    tc.Id,
                    tc.MaxPoints,
                    earned,
                    outcome,
                    message,
                    rawJson,
                    saveResultDetail,
                    detailPassed,
                    detailScore,
                    detailResponseTime,
                    detailErrorMessage));

                questionScore += earned;
                totalMaxScore += tc.MaxPoints;
            }

            questions.Add(new ComputedQuestionResult(
                question.Id,
                question.Label,
                questionScore,
                question.MaxScore,
                testCases));

            totalScore += questionScore;
        }

        return new ComputedSubmissionResult(totalScore, totalMaxScore, questions);
    }

    private static string BuildCollectionJson(ExamSession session, ExamQuestion question)
    {
        var items = question.TestCases
            .OrderBy(tc => tc.SortOrder)
            .Select(tc => BuildCollectionItem(question.Label, tc))
            .ToList();

        var collection = new
        {
            info = new
            {
                name = $"{session.Code} - {question.Label}",
                schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            variable = new[]
            {
                new { key = "baseUrl", value = "http://localhost:5000" },
                new { key = "examSessionId", value = session.Id.ToString() },
                new { key = "semesterCode", value = session.Semester.Code },
                new { key = "questionLabel", value = question.Label }
            },
            item = items
        };

        return JsonSerializer.Serialize(collection, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static object BuildCollectionItem(string questionLabel, ExamTestCase testCase)
    {
        var scriptLines = BuildTestCaseScriptLines(questionLabel, testCase);
        var body = string.IsNullOrWhiteSpace(testCase.RequestBody)
            ? null
            : new
            {
                mode = "raw",
                raw = testCase.RequestBody,
                options = new { raw = new { language = "json" } }
            };

        return new
        {
            name = testCase.Name,
            request = new
            {
                method = string.IsNullOrWhiteSpace(testCase.Method) ? "GET" : testCase.Method.ToUpperInvariant(),
                header = Array.Empty<object>(),
                url = new
                {
                    raw = "{{baseUrl}}" + testCase.UrlTemplate,
                    host = new[] { "{{baseUrl}}" },
                    path = NormalizeCollectionPath(testCase.UrlTemplate)
                },
                body
            },
            @event = new[]
            {
                new
                {
                    listen = "test",
                    script = new
                    {
                        type = "text/javascript",
                        exec = scriptLines
                    }
                }
            }
        };
    }

    private static IReadOnlyList<string> BuildTestCaseScriptLines(string questionLabel, ExamTestCase testCase)
    {
        var title = EscapeJsString($"{questionLabel} - {testCase.Name}");
        var lines = new List<string>
        {
            $"pm.test(\"{title} - status {testCase.ExpectedStatus}\", function () {{",
            $"  pm.response.to.have.status({testCase.ExpectedStatus});",
            "});"
        };

        var expectedBody = (testCase.ExpectedBody ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(expectedBody))
        {
            lines.Add($"pm.test(\"{title} - body validation\", function () {{");
            lines.Add("  const bodyText = pm.response.text();");

            if (expectedBody.Equals("empty_array", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add("  const body = pm.response.json();");
                lines.Add("  pm.expect(Array.isArray(body)).to.be.true;");
                lines.Add("  pm.expect(body.length).to.equal(0);");
            }
            else if (expectedBody.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add($"  pm.expect(bodyText).to.match(/{EscapeJsRegex(expectedBody[6..].Trim())}/);");
            }
            else if (expectedBody.StartsWith("contains:", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add($"  pm.expect(bodyText).to.include(\"{EscapeJsString(expectedBody[9..].Trim())}\");");
            }
            else
            {
                lines.Add($"  pm.expect(bodyText).to.include(\"{EscapeJsString(expectedBody)}\");");
            }

            lines.Add("});");
        }

        return lines;
    }

    private static string[] NormalizeCollectionPath(string urlTemplate)
    {
        return urlTemplate
            .Trim()
            .TrimStart('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string EscapeJsString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", string.Empty)
            .Replace("\n", "\\n");
    }

    private static string EscapeJsRegex(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("/", "\\/")
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForAppReadyAsync(int port, CancellationToken ct, int timeoutSec = 30)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var url = $"http://localhost:{port}";
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSec);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await http.GetAsync(url, ct);
                return;
            }
            catch
            {
                await Task.Delay(500, ct);
            }
        }

        throw new TimeoutException($"App on port {port} did not respond within {timeoutSec}s.");
    }

    private sealed record ComputedSubmissionResult(
        decimal TotalScore,
        decimal TotalMaxScore,
        IReadOnlyList<ComputedQuestionResult> Questions);

    private sealed record ComputedQuestionResult(
        Guid QuestionId,
        string Label,
        decimal Score,
        decimal MaxScore,
        IReadOnlyList<ComputedTestCaseResult> TestCases);

    private sealed record ComputedTestCaseResult(
        Guid TestCaseId,
        decimal MaxPoints,
        decimal PointsEarned,
        ExamTestCaseOutcome Outcome,
        string? Message,
        string? RawOutputJson,
        bool SaveResultDetail,
        bool DetailPassed,
        double DetailScore,
        int DetailResponseTime,
        string DetailErrorMessage);
}
