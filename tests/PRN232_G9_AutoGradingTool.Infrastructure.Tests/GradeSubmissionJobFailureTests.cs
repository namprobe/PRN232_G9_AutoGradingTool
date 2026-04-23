using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;
using PRN232_G9_AutoGradingTool.Infrastructure.Jobs;
using PRN232_G9_AutoGradingTool.Infrastructure.Repositories;
using Xunit;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Tests;

public class GradeSubmissionJobFailureTests
{
    [Fact]
    public async Task ExecuteAsync_MarksJobFailedAndWritesLog_WhenSubmissionPathIsInvalid()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PRN232_G9_AutoGradingToolDbContext>(options =>
            options.UseInMemoryDatabase(nameof(ExecuteAsync_MarksJobFailedAndWritesLog_WhenSubmissionPathIsInvalid)));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<PRN232_G9_AutoGradingToolDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        await using var provider = services.BuildServiceProvider();
        Guid gradingJobId;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PRN232_G9_AutoGradingToolDbContext>();
            gradingJobId = await SeedInvalidSubmissionAsync(db);
        }

        var job = new GradeSubmissionJob(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new NoopProcessService(),
            new NoopResultParser(),
            new NoopFileServiceFactory(),
            NullLogger<GradeSubmissionJob>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.ExecuteAsync(gradingJobId, CancellationToken.None));

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PRN232_G9_AutoGradingToolDbContext>();
        var savedJob = await verifyDb.GradingJobs.FirstAsync(x => x.Id == gradingJobId);
        var logs = await verifyDb.GradingJobLogs
            .Where(x => x.GradingJobId == gradingJobId)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync();

        Assert.Equal(GradingJobStatus.Failed, savedJob.JobStatus);
        Assert.Contains(logs, x => x.Level == GradingJobLogLevel.Error);
    }

    private static async Task<Guid> SeedInvalidSubmissionAsync(PRN232_G9_AutoGradingToolDbContext db)
    {
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Code = "SPRING2026",
            Name = "Spring 2026",
            StartDateUtc = DateTime.UtcNow.AddMonths(-1),
            EndDateUtc = DateTime.UtcNow.AddMonths(4),
            Status = EntityStatusEnum.Active
        };
        semester.InitializeEntity();

        var session = new ExamSession
        {
            Id = Guid.NewGuid(),
            SemesterId = semester.Id,
            Code = "SESSION-FAIL",
            Title = "Fail Session",
            StartsAtUtc = DateTime.UtcNow.AddHours(-2),
            EndsAtUtc = DateTime.UtcNow.AddHours(1),
            ExamDurationMinutes = 90,
            Status = EntityStatusEnum.Active
        };
        session.InitializeEntity();

        var topic = new ExamTopic
        {
            Id = Guid.NewGuid(),
            ExamSessionId = session.Id,
            Title = "Topic A",
            SortOrder = 1,
            Status = EntityStatusEnum.Active
        };
        topic.InitializeEntity();

        var question = new ExamQuestion
        {
            Id = Guid.NewGuid(),
            ExamTopicId = topic.Id,
            Label = "Q1",
            Title = "Q1",
            MaxScore = 5,
            Status = EntityStatusEnum.Active
        };
        question.InitializeEntity();

        var submission = new ExamSubmission
        {
            Id = Guid.NewGuid(),
            ExamSessionId = session.Id,
            StudentCode = "HE186501",
            StudentName = "Alice",
            WorkflowStatus = ExamSubmissionStatus.Queued,
            SubmittedAtUtc = DateTime.UtcNow,
            Status = EntityStatusEnum.Active
        };
        submission.InitializeEntity();

        var submissionFile = new ExamSubmissionFile
        {
            Id = Guid.NewGuid(),
            ExamSubmissionId = submission.Id,
            QuestionLabel = "Q2",
            StorageRelativePath = $"uploads/{session.Code}/{topic.Id:N}/HE186501_Alice/Q1/solution.zip",
            OriginalFileName = "solution.zip",
            Status = EntityStatusEnum.Active
        };
        submissionFile.InitializeEntity();

        var gradingJob = new GradingJob
        {
            Id = Guid.NewGuid(),
            ExamSubmissionId = submission.Id,
            ExamGradingPackId = Guid.NewGuid(),
            JobStatus = GradingJobStatus.Queued,
            Trigger = GradingJobTrigger.SessionEnd,
            Status = EntityStatusEnum.Active
        };
        gradingJob.InitializeEntity();

        db.Semesters.Add(semester);
        db.ExamSessions.Add(session);
        db.ExamTopics.Add(topic);
        db.ExamQuestions.Add(question);
        db.ExamSubmissions.Add(submission);
        db.ExamSubmissionFiles.Add(submissionFile);
        db.GradingJobs.Add(gradingJob);
        await db.SaveChangesAsync();

        return gradingJob.Id;
    }

    private sealed class NoopFileServiceFactory : IFileServiceFactory
    {
        public IFileService CreateFileService() => new NoopFileService();
    }

    private sealed class NoopFileService : IFileService
    {
        public Task<List<string?>> UploadFilesBulkAsync(List<Microsoft.AspNetCore.Http.IFormFile> files, List<string> fileNames, string subDirectory = "", CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<List<bool>> DeleteFilesBulkAsync(List<string> filePaths, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> UploadFileAsync(Microsoft.AspNetCore.Http.IFormFile file, string fileName, string subDirectory = "", CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string subDirectory = "", CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public string GetFileUrl(string filePath) => filePath;

        public Task<(byte[] FileContent, string ContentType)> GetFileContentAsync(string filePath, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class NoopProcessService : IGradingProcessService
    {
        public string ExtractZip(string zipPath, string? workingDirectory = null) => throw new NotSupportedException();

        public System.Diagnostics.Process RunApp(string path, int port) => throw new NotSupportedException();

        public System.Diagnostics.Process RunNewman(string collectionJsonPath, string baseUrl, string? workingDirectory = null) => throw new NotSupportedException();

        public Task<string> CaptureProcessOutputAsync(System.Diagnostics.Process process, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void CleanupResources(System.Diagnostics.Process? process, string? tempDirectory)
        {
        }
    }

    private sealed class NoopResultParser : IGradingResultParser
    {
        public (string? q1, string? q2) DetectProjects(string root) => (null, null);

        public IReadOnlyList<ResultDetail> ParseNewmanTestResults(string newmanJson) => [];
    }
}
