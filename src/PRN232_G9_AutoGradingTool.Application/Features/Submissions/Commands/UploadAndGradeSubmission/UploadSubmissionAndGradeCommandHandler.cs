using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Features.Submissions.Commands.UploadAndGradeSubmission;

public class UploadSubmissionAndGradeCommandHandler
    : IRequestHandler<UploadSubmissionAndGradeCommand, Result<UploadAndTriggerGradingResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileServiceFactory _fileServiceFactory;
    private readonly IExamGradingJobService _jobService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UploadSubmissionAndGradeCommandHandler> _logger;

    public UploadSubmissionAndGradeCommandHandler(
        IUnitOfWork unitOfWork,
        IFileServiceFactory fileServiceFactory,
        IExamGradingJobService jobService,
        ICurrentUserService currentUserService,
        ILogger<UploadSubmissionAndGradeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileServiceFactory = fileServiceFactory;
        _jobService = jobService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<UploadAndTriggerGradingResponseDto>> Handle(
        UploadSubmissionAndGradeCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Auth
        var (isValid, userId) = await _currentUserService.IsUserValidAsync();
        if (!isValid || userId == null)
            return Result<UploadAndTriggerGradingResponseDto>.Failure("Unauthorized", ErrorCodeEnum.Unauthorized);

        // 2. Load topic → session
        var topic = await _unitOfWork.Repository<ExamTopic>()
            .GetQueryable()
            .FirstOrDefaultAsync(t => t.Id == command.ExamTopicId, cancellationToken);

        if (topic == null)
            return Result<UploadAndTriggerGradingResponseDto>.Failure(
                "ExamTopic not found.", ErrorCodeEnum.NotFound);

        var sessionId = topic.ExamSessionId;

        // 3. Load session for path building
        var session = await _unitOfWork.Repository<ExamSession>()
            .GetQueryable()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
            return Result<UploadAndTriggerGradingResponseDto>.Failure(
                "ExamSession not found.", ErrorCodeEnum.NotFound);

        // 4. Find or create submission
        var studentCode = command.StudentCode.Trim();
        var submission = await _unitOfWork.Repository<ExamSubmission>()
            .GetQueryable()
            .Include(s => s.SubmissionFiles)
            .FirstOrDefaultAsync(
                s => s.ExamSessionId == sessionId && s.StudentCode == studentCode,
                cancellationToken);

        if (submission == null)
        {
            submission = new ExamSubmission
            {
                ExamSessionId = sessionId,
                StudentCode = studentCode,
                StudentName = studentCode,
                SubmittedAtUtc = DateTime.UtcNow,
                WorkflowStatus = ExamSubmissionStatus.Pending
            };
            submission.InitializeEntity(userId);
            await _unitOfWork.Repository<ExamSubmission>().AddAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Auto-created submission {SubmissionId} for student {StudentCode} in session {SessionId}",
                submission.Id, studentCode, sessionId);
        }

        // 5. Active grading pack
        var pack = await _unitOfWork.Repository<ExamGradingPack>()
            .GetQueryable()
            .Where(p => p.ExamSessionId == sessionId && p.IsActive)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (pack == null)
            return Result<UploadAndTriggerGradingResponseDto>.Failure(
                "No active grading pack found for the session.", ErrorCodeEnum.BusinessRuleViolation);

        // 6. Upload file
        var label = command.QuestionLabel.ToString(); // "Q1" or "Q2"
        var studentFolder = string.Concat(studentCode.Split(Path.GetInvalidPathChars()));
        var directory = $"{session.Code}/{topic.Id:N}/{studentFolder}/{label}";
        var fileName = "solution.zip";

        var fileService = _fileServiceFactory.CreateFileService();
        var newPath = await fileService.UploadFileAsync(
            command.ZipFile, fileName, directory, cancellationToken);

        // 7. Upsert ExamSubmissionFile
        var existingFile = submission.SubmissionFiles?
            .FirstOrDefault(f => string.Equals(f.QuestionLabel, label, StringComparison.OrdinalIgnoreCase));

        if (existingFile != null)
        {
            existingFile.StorageRelativePath = newPath;
            existingFile.OriginalFileName = command.ZipFile.FileName;
            existingFile.UpdateEntity(userId);
            _unitOfWork.Repository<ExamSubmissionFile>().Update(existingFile);
        }
        else
        {
            var newFile = new ExamSubmissionFile
            {
                ExamSubmissionId = submission.Id,
                QuestionLabel = label,
                StorageRelativePath = newPath,
                OriginalFileName = command.ZipFile.FileName
            };
            newFile.InitializeEntity(userId);
            await _unitOfWork.Repository<ExamSubmissionFile>().AddAsync(newFile, cancellationToken);
        }

        // 8. Cancel existing pending grading jobs
        var existingJob = await _unitOfWork.Repository<GradingJob>()
            .GetQueryable()
            .Where(j => j.ExamSubmissionId == submission.Id && j.JobStatus == GradingJobStatus.Queued)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingJob != null && !string.IsNullOrEmpty(existingJob.HangfireJobId))
        {
            _jobService.DeleteScheduledJob(existingJob.HangfireJobId);
            existingJob.JobStatus = GradingJobStatus.Failed;
            existingJob.ErrorMessage = "Cancelled by upload-and-grade.";
            existingJob.FinishedAtUtc = DateTime.UtcNow;
            existingJob.UpdateEntity(userId);
            _unitOfWork.Repository<GradingJob>().Update(existingJob);
        }

        // 9. Create new GradingJob
        var gradingJob = new GradingJob
        {
            ExamSubmissionId = submission.Id,
            ExamGradingPackId = pack.Id,
            JobStatus = GradingJobStatus.Queued,
            Trigger = GradingJobTrigger.ManualRegrade,
            HangfireJobId = null!
        };
        gradingJob.InitializeEntity(userId);
        await _unitOfWork.Repository<GradingJob>().AddAsync(gradingJob, cancellationToken);

        submission.WorkflowStatus = ExamSubmissionStatus.Queued;
        submission.UpdateEntity(userId);
        _unitOfWork.Repository<ExamSubmission>().Update(submission);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 10. Enqueue Hangfire job via service (no Infrastructure reference needed)
        var jobId = _jobService.EnqueueGradeSubmissionJob(gradingJob.Id);

        gradingJob.HangfireJobId = jobId;
        _unitOfWork.Repository<GradingJob>().Update(gradingJob);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "UploadAndGrade: submission={SubmissionId}, topic={TopicId}, question={Label}, job={JobId}",
            submission.Id, command.ExamTopicId, label, gradingJob.Id);

        return Result<UploadAndTriggerGradingResponseDto>.Success(
            new UploadAndTriggerGradingResponseDto(submission.Id, gradingJob.Id, "Queued"),
            "File uploaded and grading job queued successfully.");
    }
}
