using Microsoft.AspNetCore.Http;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>
/// Background-job dispatch + related submission mutations.
/// Tách riêng khỏi IExamGradingAppService để IExamGradingAppService chỉ còn READ operations.
/// </summary>
public interface IExamGradingJobService
{
    string ScheduleSummarizeExamResultJob(Guid examSessionId, DateTime endsAtUtc);
    bool DeleteScheduledJob(string jobId);

    Task<Result<StartClassBatchGradingResponseDto>> StartClassBatchGradingAsync(
        Guid examSessionClassId,
        StartClassBatchGradingRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ReplaceSubmissionFileAsync(
        Guid submissionId,
        string questionLabel,
        IFormFile zipFile,
        CancellationToken cancellationToken = default);

    Task<Result<TriggerRegradeResponseDto>> TriggerRegradeAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueue a Hangfire job to grade the specified GradingJob.
    /// Returns the Hangfire job ID.
    /// </summary>
    string EnqueueGradeSubmissionJob(Guid gradingJobId);
}
