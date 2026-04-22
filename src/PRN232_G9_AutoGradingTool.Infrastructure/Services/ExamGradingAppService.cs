using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

public class ExamGradingAppService : IExamGradingAppService
{
    private readonly PRN232_G9_AutoGradingToolDbContext _db;
    private readonly IFileServiceFactory _fileServiceFactory;
    private readonly ILogger<ExamGradingAppService> _logger;

    public ExamGradingAppService(
        PRN232_G9_AutoGradingToolDbContext db,
        IFileServiceFactory fileServiceFactory,
        ILogger<ExamGradingAppService> logger)
    {
        _db = db;
        _fileServiceFactory = fileServiceFactory;
        _logger = logger;
    }

    public async Task<Result<List<SemesterListItemDto>>> ListSemestersAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Semesters.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new SemesterListItemDto(x.Id, x.Code, x.Name, x.StartDateUtc, x.EndDateUtc))
            .ToListAsync(cancellationToken);
        return Result<List<SemesterListItemDto>>.Success(rows, "OK");
    }

    public async Task<Result<List<ExamSessionListItemDto>>> ListExamSessionsAsync(Guid? semesterId, CancellationToken cancellationToken = default)
    {
        var q = _db.ExamSessions.AsNoTracking().Include(x => x.Semester).AsQueryable();
        if (semesterId.HasValue)
            q = q.Where(x => x.SemesterId == semesterId.Value);

        var rows = await q
            .OrderByDescending(x => x.StartsAtUtc)
            .Select(x => new ExamSessionListItemDto(
                x.Id,
                x.Code,
                x.Title,
                x.SemesterId,
                x.Semester.Code,
                x.StartsAtUtc,
                x.ExamDurationMinutes,
                x.EndsAtUtc,
                x.Topics.Count,
                x.Topics.SelectMany(t => t.Questions).Count(),
                x.Submissions.Count))
            .ToListAsync(cancellationToken);

        return Result<List<ExamSessionListItemDto>>.Success(rows, "OK");
    }

    public async Task<Result<ExamSessionDetailDto>> GetExamSessionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions.AsNoTracking()
            .Include(x => x.Semester)
            .Include(x => x.Topics).ThenInclude(t => t.Questions).ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (session == null)
            return Result<ExamSessionDetailDto>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var topics = session.Topics.OrderBy(t => t.SortOrder).Select(t => new ExamTopicDetailDto(
            t.Id,
            t.Title,
            t.SortOrder,
            t.Questions.OrderBy(q => q.Label).Select(q => new ExamQuestionDetailDto(
                q.Id,
                q.Label,
                q.Title,
                q.MaxScore,
                q.TestCases.OrderBy(tc => tc.SortOrder).Select(tc => new ExamTestCaseDetailDto(tc.Id, tc.Name, tc.MaxPoints, tc.SortOrder)).ToList()
            )).ToList()
        )).ToList();

        var dto = new ExamSessionDetailDto(
            session.Id,
            session.Code,
            session.Title,
            session.SemesterId,
            session.Semester.Code,
            session.StartsAtUtc,
            session.ExamDurationMinutes,
            session.EndsAtUtc,
            topics);

        return Result<ExamSessionDetailDto>.Success(dto, "OK");
    }

    public async Task<Result<List<ExamSubmissionListItemDto>>> ListSubmissionsAsync(Guid examSessionId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.ExamSessions.AnyAsync(x => x.Id == examSessionId, cancellationToken);
        if (!exists)
            return Result<List<ExamSubmissionListItemDto>>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        var rows = await _db.ExamSubmissions.AsNoTracking()
            .Where(x => x.ExamSessionId == examSessionId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Select(x => new ExamSubmissionListItemDto(
                x.Id,
                x.ExamSessionId,
                x.StudentCode,
                x.StudentName,
                x.WorkflowStatus.ToString(),
                x.SubmittedAtUtc,
                x.TotalScore))
            .ToListAsync(cancellationToken);

        return Result<List<ExamSubmissionListItemDto>>.Success(rows, "OK");
    }

    public async Task<Result<ExamSubmissionDetailDto>> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sub = await _db.ExamSubmissions.AsNoTracking()
            .Include(x => x.ExamSession)
            .Include(x => x.SubmissionFiles)
            .Include(x => x.QuestionScores).ThenInclude(qs => qs.ExamQuestion)
            .Include(x => x.TestCaseScores).ThenInclude(ts => ts.ExamTestCase).ThenInclude(tc => tc!.ExamQuestion)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (sub == null)
            return Result<ExamSubmissionDetailDto>.Failure("Không tìm thấy bài nộp.", ErrorCodeEnum.NotFound);

        var qScores = sub.QuestionScores
            .OrderBy(x => x.ExamQuestion.Label)
            .Select(x => new ExamQuestionScoreDto(x.ExamQuestionId, x.ExamQuestion.Label, x.Score, x.MaxScore, x.Summary))
            .ToList();

        var tcScores = sub.TestCaseScores
            .OrderBy(x => x.ExamTestCase.ExamQuestion.Label).ThenBy(x => x.ExamTestCase.SortOrder)
            .Select(x => new ExamTestCaseScoreDto(
                x.ExamTestCaseId,
                x.ExamTestCase.ExamQuestion.Label,
                x.ExamTestCase.Name,
                x.PointsEarned,
                x.MaxPoints,
                x.Outcome.ToString(),
                x.Message))
            .ToList();

        var files = sub.SubmissionFiles
            .OrderBy(x => x.QuestionLabel)
            .Select(x => new SubmissionFileDto(x.QuestionLabel, x.StorageRelativePath, x.OriginalFileName))
            .ToList();

        var dto = new ExamSubmissionDetailDto(
            sub.Id,
            sub.ExamSessionId,
            sub.ExamSession.Code,
            sub.StudentCode,
            sub.StudentName,
            sub.WorkflowStatus.ToString(),
            sub.SubmittedAtUtc,
            sub.TotalScore,
            files,
            qScores,
            tcScores);

        return Result<ExamSubmissionDetailDto>.Success(dto, "OK");
    }

    public async Task<Result<Guid>> CreateSubmissionWithZipAsync(
        Guid examSessionId,
        string studentCode,
        string? studentName,
        IFormFile q1Zip,
        IFormFile q2Zip,
        bool bypassExamWindow = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentCode))
            return Result<Guid>.Failure("MSSV / studentCode bắt buộc.", ErrorCodeEnum.ValidationFailed);

        var session = await _db.ExamSessions
            .Include(x => x.Topics).ThenInclude(t => t.Questions).ThenInclude(q => q.TestCases)
            .FirstOrDefaultAsync(x => x.Id == examSessionId, cancellationToken);

        if (session == null)
            return Result<Guid>.Failure("Không tìm thấy ca thi.", ErrorCodeEnum.NotFound);

        if (!bypassExamWindow)
        {
            var now = DateTime.UtcNow;
            if (now < session.StartsAtUtc)
                return Result<Guid>.Failure("Ca thi chưa mở nhận bài.", ErrorCodeEnum.ValidationFailed);
            if (now > session.EndsAtUtc)
                return Result<Guid>.Failure("Đã hết hạn nộp bài cho ca thi này.", ErrorCodeEnum.ValidationFailed);
        }

        if (!IsZip(q1Zip) || !IsZip(q2Zip))
            return Result<Guid>.Failure("Hai file phải là .zip.", ErrorCodeEnum.InvalidFileType);

        var pack = await _db.ExamGradingPacks
            .Where(p => p.ExamSessionId == examSessionId && p.IsActive)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var submission = new ExamSubmission
        {
            Id = Guid.NewGuid(),
            ExamSessionId = examSessionId,
            ExamGradingPackId = pack?.Id,
            StudentCode = studentCode.Trim(),
            StudentName = string.IsNullOrWhiteSpace(studentName) ? null : studentName.Trim(),
            WorkflowStatus = ExamSubmissionStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };

        Guid? gradingJobId = null;
        if (pack != null)
        {
            var job = new GradingJob
            {
                Id = Guid.NewGuid(),
                ExamSubmissionId = submission.Id,
                ExamGradingPackId = pack.Id,
                JobStatus = GradingJobStatus.Queued,
                CreatedAt = DateTime.UtcNow,
                Status = EntityStatusEnum.Active
            };
            gradingJobId = job.Id;
            _db.GradingJobs.Add(job);
        }

        _db.ExamSubmissions.Add(submission);
        await _db.SaveChangesAsync(cancellationToken);

        var fileService = _fileServiceFactory.CreateFileService();
        var subDir = Path.Combine("exam-submissions", submission.Id.ToString("N"));
        try
        {
            var p1 = await fileService.UploadFileAsync(q1Zip, "q1.zip", subDir, cancellationToken);
            var p2 = await fileService.UploadFileAsync(q2Zip, "q2.zip", subDir, cancellationToken);
            if (string.IsNullOrEmpty(p1) || string.IsNullOrEmpty(p2))
                throw new InvalidOperationException("Upload trả về đường dẫn rỗng.");

            _db.ExamSubmissionFiles.AddRange(
                new ExamSubmissionFile
                {
                    Id = Guid.NewGuid(),
                    ExamSubmissionId = submission.Id,
                    QuestionLabel = "Q1",
                    StorageRelativePath = p1,
                    OriginalFileName = q1Zip.FileName,
                    CreatedAt = DateTime.UtcNow,
                    Status = EntityStatusEnum.Active
                },
                new ExamSubmissionFile
                {
                    Id = Guid.NewGuid(),
                    ExamSubmissionId = submission.Id,
                    QuestionLabel = "Q2",
                    StorageRelativePath = p2,
                    OriginalFileName = q2Zip.FileName,
                    CreatedAt = DateTime.UtcNow,
                    Status = EntityStatusEnum.Active
                });
            submission.WorkflowStatus = ExamSubmissionStatus.Running;

            if (gradingJobId.HasValue)
            {
                var job = await _db.GradingJobs.FirstAsync(j => j.Id == gradingJobId.Value, cancellationToken);
                job.JobStatus = GradingJobStatus.Running;
                job.StartedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);

            await ApplyStubGradingAsync(submission.Id, cancellationToken);

            if (gradingJobId.HasValue)
            {
                var job = await _db.GradingJobs.FirstAsync(j => j.Id == gradingJobId.Value, cancellationToken);
                job.JobStatus = GradingJobStatus.Completed;
                job.FinishedAtUtc = DateTime.UtcNow;
                job.ErrorMessage = null;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi upload/chấm stub cho submission {Id}", submission.Id);
            submission.WorkflowStatus = ExamSubmissionStatus.Failed;
            if (gradingJobId.HasValue)
            {
                var job = await _db.GradingJobs.FirstOrDefaultAsync(j => j.Id == gradingJobId.Value, cancellationToken);
                if (job != null)
                {
                    job.JobStatus = GradingJobStatus.Failed;
                    job.FinishedAtUtc = DateTime.UtcNow;
                    job.ErrorMessage = ex.Message.Length > 4000 ? ex.Message[..4000] : ex.Message;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Failure("Lưu file hoặc chấm stub thất bại.", ErrorCodeEnum.FileUploadFailed);
        }

        return Result<Guid>.Success(submission.Id, "Đã nhận zip và chạy chấm stub (demo).");
    }

    public async Task<Result<bool>> ReplaceSubmissionFileAsync(
        Guid submissionId,
        string questionLabel,
        IFormFile zipFile,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(questionLabel))
            return Result<bool>.Failure("questionLabel bắt buộc (Q1, Q2, ...).", ErrorCodeEnum.ValidationFailed);

        if (!IsZip(zipFile))
            return Result<bool>.Failure("File phải là .zip.", ErrorCodeEnum.InvalidFileType);

        var submission = await _db.ExamSubmissions
            .Include(x => x.SubmissionFiles)
            .FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

        if (submission == null)
            return Result<bool>.Failure("Không tìm thấy bài nộp.", ErrorCodeEnum.NotFound);

        var label = questionLabel.Trim().ToUpperInvariant();
        var fileService = _fileServiceFactory.CreateFileService();
        var subDir = Path.Combine("exam-submissions", submission.Id.ToString("N"));

        var path = await fileService.UploadFileAsync(zipFile, $"{label.ToLowerInvariant()}.zip", subDir, cancellationToken);
        if (string.IsNullOrEmpty(path))
            return Result<bool>.Failure("Upload file thất bại.", ErrorCodeEnum.FileUploadFailed);

        // Xoá file cũ (nếu có), tạo mới
        var existing = submission.SubmissionFiles.FirstOrDefault(f =>
            f.QuestionLabel.Equals(label, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            _db.ExamSubmissionFiles.Remove(existing);

        _db.ExamSubmissionFiles.Add(new ExamSubmissionFile
        {
            Id = Guid.NewGuid(),
            ExamSubmissionId = submission.Id,
            QuestionLabel = label,
            StorageRelativePath = path,
            OriginalFileName = zipFile.FileName,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        });

        // Reset trạng thái về Pending để chờ trigger regrade
        submission.WorkflowStatus = ExamSubmissionStatus.Pending;
        submission.TotalScore = null;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin replaced {Label} file for submission {Id}", label, submissionId);
        return Result<bool>.Success(true, $"Đã thay thế file {label}. Gọi POST /regrade để chấm lại.");
    }

    public async Task<Result<TriggerRegradeResponseDto>> TriggerRegradeAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _db.ExamSubmissions
            .Include(x => x.ExamSession)
            .Include(x => x.SubmissionFiles)
            .FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

        if (submission == null)
            return Result<TriggerRegradeResponseDto>.Failure("Không tìm thấy bài nộp.", ErrorCodeEnum.NotFound);

        if (!submission.SubmissionFiles.Any())
            return Result<TriggerRegradeResponseDto>.Failure(
                "Bài nộp chưa có file. Upload file trước khi trigger regrade.",
                ErrorCodeEnum.ValidationFailed);

        var pack = await _db.ExamGradingPacks
            .Where(p => p.ExamSessionId == submission.ExamSessionId && p.IsActive)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (pack == null)
            return Result<TriggerRegradeResponseDto>.Failure(
                "Không có GradingPack active cho ca thi này.",
                ErrorCodeEnum.NotFound);

        // Tạo GradingJob mới với trigger ManualRegrade
        var job = new GradingJob
        {
            Id = Guid.NewGuid(),
            ExamSubmissionId = submission.Id,
            ExamGradingPackId = pack.Id,
            JobStatus = GradingJobStatus.Queued,
            Trigger = GradingJobTrigger.ManualRegrade,
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        _db.GradingJobs.Add(job);

        submission.WorkflowStatus = ExamSubmissionStatus.Queued;
        await _db.SaveChangesAsync(cancellationToken);

        // TODO (P3): BackgroundJob.Enqueue<GradeSubmissionJob>(x => x.ExecuteAsync(job.Id))
        // Tạm thời chạy stub để demo
        job.JobStatus = GradingJobStatus.Running;
        job.StartedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await ApplyStubGradingAsync(submission.Id, cancellationToken);
            job.JobStatus = GradingJobStatus.Completed;
            job.FinishedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Regrade stub thất bại cho submission {Id}", submissionId);
            job.JobStatus = GradingJobStatus.Failed;
            job.FinishedAtUtc = DateTime.UtcNow;
            job.ErrorMessage = ex.Message.Length > 4000 ? ex.Message[..4000] : ex.Message;
            submission.WorkflowStatus = ExamSubmissionStatus.Failed;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var dto = new TriggerRegradeResponseDto(
            job.Id,
            job.Trigger.ToString(),
            job.JobStatus.ToString(),
            job.JobStatus == GradingJobStatus.Completed
                ? "Chấm lại thành công (stub)."
                : "Chấm lại thất bại — xem log.");

        return Result<TriggerRegradeResponseDto>.Success(dto, dto.Message);
    }

    private static bool IsZip(IFormFile f)
    {
        var n = f.FileName?.ToLowerInvariant() ?? "";
        return n.EndsWith(".zip", StringComparison.Ordinal);
    }

    /// <summary>Stub P3: điểm ổn định để demo — sau thay bằng Hangfire + grader thật.</summary>
    private async Task ApplyStubGradingAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var sub = await _db.ExamSubmissions
            .Include(x => x.ExamSession).ThenInclude(es => es.Topics).ThenInclude(t => t.Questions).ThenInclude(q => q.TestCases)
            .Include(x => x.QuestionScores)
            .Include(x => x.TestCaseScores)
            .FirstAsync(x => x.Id == submissionId, cancellationToken);

        _db.ExamQuestionScores.RemoveRange(sub.QuestionScores);
        _db.ExamTestCaseScores.RemoveRange(sub.TestCaseScores);

        decimal total = 0;
        var now = DateTime.UtcNow;

        foreach (var topic in sub.ExamSession.Topics)
        {
            foreach (var question in topic.Questions.OrderBy(q => q.Label))
            {
                decimal qSum = 0;
                foreach (var tc in question.TestCases.OrderBy(t => t.SortOrder))
                {
                    var earned = Math.Round(tc.MaxPoints * 0.85m, 2, MidpointRounding.AwayFromZero);
                    if (earned > tc.MaxPoints)
                        earned = tc.MaxPoints;
                    qSum += earned;
                    total += earned;

                    _db.ExamTestCaseScores.Add(new ExamTestCaseScore
                    {
                        Id = Guid.NewGuid(),
                        ExamSubmissionId = sub.Id,
                        ExamTestCaseId = tc.Id,
                        PointsEarned = earned,
                        MaxPoints = tc.MaxPoints,
                        Outcome = ExamTestCaseOutcome.Pass,
                        Message = "Stub grader (demo PRN232)",
                        CreatedAt = now,
                        Status = EntityStatusEnum.Active
                    });
                }

                var qMax = question.MaxScore;
                if (qSum > qMax)
                    qSum = qMax;

                _db.ExamQuestionScores.Add(new ExamQuestionScore
                {
                    Id = Guid.NewGuid(),
                    ExamSubmissionId = sub.Id,
                    ExamQuestionId = question.Id,
                    Score = qSum,
                    MaxScore = qMax,
                    Summary = "Tổng testcase (stub)",
                    CreatedAt = now,
                    Status = EntityStatusEnum.Active
                });
            }
        }

        var sessionMax = sub.ExamSession.Topics.SelectMany(t => t.Questions).Sum(q => q.MaxScore);
        if (total > sessionMax)
            total = sessionMax;

        sub.TotalScore = Math.Round(total, 2, MidpointRounding.AwayFromZero);
        sub.WorkflowStatus = ExamSubmissionStatus.Completed;
        sub.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
