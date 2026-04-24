using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Jobs;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Services;

public class ExamGradingJobService : IExamGradingJobService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExamGradingJobService> _logger;

    public ExamGradingJobService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExamGradingJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string ScheduleSummarizeExamResultJob(Guid examSessionId, DateTime endsAtUtc)
    {
        var delay = endsAtUtc - DateTime.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
        using var scope = _scopeFactory.CreateScope();
        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        return backgroundJobClient.Schedule<SummarizeExamResultJob>(
            x => x.ExecuteAsync(examSessionId, CancellationToken.None), delay);
    }

    public bool DeleteScheduledJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return false;

        try
        {
            return BackgroundJob.Delete(jobId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Hangfire job {JobId}.", jobId);
            return false;
        }
    }

    public async Task<Result<StartClassBatchGradingResponseDto>> StartClassBatchGradingAsync(
        Guid examSessionClassId,
        StartClassBatchGradingRequest request,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) return Result<StartClassBatchGradingResponseDto>.Failure("Unauthorized", ErrorCodeEnum.Unauthorized);

        var sessionClass = await uow.Repository<ExamSessionClass>()
            .GetQueryable()
            .FirstOrDefaultAsync(x => x.Id == examSessionClassId, cancellationToken);

        if (sessionClass == null)
            return Result<StartClassBatchGradingResponseDto>.Failure("Exam session class not found.", ErrorCodeEnum.NotFound);

        var pack = await uow.Repository<ExamGradingPack>()
            .GetQueryable()
            .FirstOrDefaultAsync(x => x.ExamSessionId == sessionClass.ExamSessionId && x.IsActive, cancellationToken);

        if (pack == null)
            return Result<StartClassBatchGradingResponseDto>.Failure("No active grading pack found for the session.", ErrorCodeEnum.BusinessRuleViolation);

        var submissionsQuery = uow.Repository<ExamSubmission>()
            .GetQueryable()
            .Where(x => x.ExamSessionId == sessionClass.ExamSessionId && x.ExamSessionClassId == sessionClass.Id);
            
        var allSubmissions = await submissionsQuery.ToListAsync(cancellationToken);

        if (!request.ForceStartWithoutFullRoster && allSubmissions.Count < sessionClass.ExpectedStudentCount)
        {
            return Result<StartClassBatchGradingResponseDto>.Failure($"Cannot start batch grading. Only {allSubmissions.Count}/{sessionClass.ExpectedStudentCount} submissions ready.", ErrorCodeEnum.BusinessRuleViolation);
        }

        int queuedCount = 0;
        foreach (var sub in allSubmissions)
        {
            if (!request.RedoCompletedBatch && sub.WorkflowStatus == ExamSubmissionStatus.Completed)
                continue;
            
            if (sub.WorkflowStatus == ExamSubmissionStatus.Running || sub.WorkflowStatus == ExamSubmissionStatus.Queued)
                continue;

            var gradingJob = new GradingJob
            {
                ExamSubmissionId = sub.Id,
                ExamGradingPackId = pack.Id,
                JobStatus = GradingJobStatus.Queued,
                Trigger = GradingJobTrigger.ManualRegrade,
                HangfireJobId = null!
            };
            gradingJob.InitializeEntity(userId);
            await uow.Repository<GradingJob>().AddAsync(gradingJob, cancellationToken);
            
            sub.WorkflowStatus = ExamSubmissionStatus.Queued;
            sub.UpdateEntity(userId);
            uow.Repository<ExamSubmission>().Update(sub);

            await uow.SaveChangesAsync(cancellationToken);

            var jobId = backgroundJobClient.Enqueue<GradeSubmissionJob>(job => job.ExecuteAsync(gradingJob.Id, CancellationToken.None));
            gradingJob.HangfireJobId = jobId;
            uow.Repository<GradingJob>().Update(gradingJob);
            await uow.SaveChangesAsync(cancellationToken);
            
            queuedCount++;
        }

        sessionClass.BatchStatus = ClassGradingBatchStatus.GradingInProgress;
        sessionClass.UpdateEntity(userId);
        uow.Repository<ExamSessionClass>().Update(sessionClass);
        await uow.SaveChangesAsync(cancellationToken);

        return Result<StartClassBatchGradingResponseDto>.Success(
            new StartClassBatchGradingResponseDto(examSessionClassId, sessionClass.BatchStatus.ToString(), queuedCount, $"Queued {queuedCount} submissions for grading."));
    }

    public async Task<Result<bool>> ReplaceSubmissionFileAsync(
        Guid submissionId,
        string questionLabel,
        IFormFile zipFile,
        CancellationToken cancellationToken = default)
    {
        if (zipFile == null || zipFile.Length == 0)
            return Result<bool>.Failure("File is empty", ErrorCodeEnum.InvalidInput);

        var extension = Path.GetExtension(zipFile.FileName);
        if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
            return Result<bool>.Failure("Only .zip files are allowed", ErrorCodeEnum.InvalidInput);

        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        var fileServiceFactory = scope.ServiceProvider.GetRequiredService<IFileServiceFactory>();

        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) return Result<bool>.Failure("Unauthorized", ErrorCodeEnum.Unauthorized);

        var submission = await uow.Repository<ExamSubmission>()
            .GetQueryable()
            .Include(x => x.SubmissionFiles)
            .FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

        if (submission == null)
            return Result<bool>.Failure("Submission not found", ErrorCodeEnum.NotFound);

        var existingFile = submission.SubmissionFiles?.FirstOrDefault(x => string.Equals(x.QuestionLabel, questionLabel, StringComparison.OrdinalIgnoreCase));
        if (existingFile == null)
        {
            return Result<bool>.Failure("File for this question does not exist yet. Please submit first.", ErrorCodeEnum.NotFound);
        }

        var directory = Path.GetDirectoryName(existingFile.StorageRelativePath)?.Replace("\\", "/") ?? string.Empty;
        var fileName = Path.GetFileName(existingFile.StorageRelativePath);
        if (string.IsNullOrEmpty(fileName)) fileName = "solution.zip";

        var fileService = fileServiceFactory.CreateFileService();
        var newPath = await fileService.UploadFileAsync(zipFile, fileName, directory, cancellationToken);

        existingFile.StorageRelativePath = newPath;
        existingFile.OriginalFileName = zipFile.FileName;
        existingFile.UpdateEntity(userId);
        
        uow.Repository<ExamSubmissionFile>().Update(existingFile);
        await uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true, "File replaced successfully.");
    }

    public async Task<Result<TriggerRegradeResponseDto>> TriggerRegradeAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

        var (isValid, userId) = await currentUserService.IsUserValidAsync();
        if (!isValid || userId == null) return Result<TriggerRegradeResponseDto>.Failure("Unauthorized", ErrorCodeEnum.Unauthorized);

        var submission = await uow.Repository<ExamSubmission>()
            .GetQueryable()
            .Include(x => x.SubmissionFiles)
            .FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

        if (submission == null)
            return Result<TriggerRegradeResponseDto>.Failure("Submission not found", ErrorCodeEnum.NotFound);

        if (submission.SubmissionFiles == null || submission.SubmissionFiles.Count == 0)
            return Result<TriggerRegradeResponseDto>.Failure("Submission has no files", ErrorCodeEnum.BusinessRuleViolation);

        var pack = await uow.Repository<ExamGradingPack>()
            .GetQueryable()
            .FirstOrDefaultAsync(x => x.ExamSessionId == submission.ExamSessionId && x.IsActive, cancellationToken);

        if (pack == null)
            return Result<TriggerRegradeResponseDto>.Failure("No active grading pack found for the session.", ErrorCodeEnum.BusinessRuleViolation);

        var existingJob = await uow.Repository<GradingJob>()
            .GetQueryable()
            .Where(x => x.ExamSubmissionId == submissionId && x.JobStatus == GradingJobStatus.Queued)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (existingJob != null && !string.IsNullOrEmpty(existingJob.HangfireJobId))
        {
            backgroundJobClient.Delete(existingJob.HangfireJobId);
            existingJob.JobStatus = GradingJobStatus.Failed;
            existingJob.ErrorMessage = "Cancelled by manual regrade.";
            existingJob.FinishedAtUtc = DateTime.UtcNow;
            existingJob.UpdateEntity(userId);
            uow.Repository<GradingJob>().Update(existingJob);
        }

        var gradingJob = new GradingJob
        {
            ExamSubmissionId = submissionId,
            ExamGradingPackId = pack.Id,
            JobStatus = GradingJobStatus.Queued,
            StartedAtUtc = null,
            FinishedAtUtc = null,
            ErrorMessage = null,
            HangfireJobId = null!,
            Trigger = GradingJobTrigger.ManualRegrade
        };
        gradingJob.InitializeEntity(userId);
        await uow.Repository<GradingJob>().AddAsync(gradingJob, cancellationToken);
        
        submission.WorkflowStatus = ExamSubmissionStatus.Queued;
        submission.UpdateEntity(userId);
        uow.Repository<ExamSubmission>().Update(submission);

        await uow.SaveChangesAsync(cancellationToken);

        var jobId = backgroundJobClient.Enqueue<GradeSubmissionJob>(job => job.ExecuteAsync(gradingJob.Id, CancellationToken.None));
        
        gradingJob.HangfireJobId = jobId;
        uow.Repository<GradingJob>().Update(gradingJob);
        await uow.SaveChangesAsync(cancellationToken);

        return Result<TriggerRegradeResponseDto>.Success(
            new TriggerRegradeResponseDto(gradingJob.Id, "ManualRegrade", "Queued", "Regrade job queued successfully."));
    }

    public string EnqueueGradeSubmissionJob(Guid gradingJobId)
    {
        using var scope = _scopeFactory.CreateScope();
        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        return backgroundJobClient.Enqueue<GradeSubmissionJob>(
            job => job.ExecuteAsync(gradingJobId, CancellationToken.None));
    }
}
