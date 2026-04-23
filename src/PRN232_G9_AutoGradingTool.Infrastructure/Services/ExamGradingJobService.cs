using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
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

    public Task<Result<StartClassBatchGradingResponseDto>> StartClassBatchGradingAsync(
        Guid examSessionClassId,
        StartClassBatchGradingRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<StartClassBatchGradingResponseDto>.Failure(
            "Chức năng chấm batch theo lớp chưa được triển khai trong job service hiện tại.",
            ErrorCodeEnum.InvalidOperation));
    }

    public Task<Result<bool>> ReplaceSubmissionFileAsync(
        Guid submissionId,
        string questionLabel,
        IFormFile zipFile,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<bool>.Failure(
            "Chức năng thay file submission chưa được triển khai trong job service hiện tại.",
            ErrorCodeEnum.InvalidOperation));
    }

    public Task<Result<TriggerRegradeResponseDto>> TriggerRegradeAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<TriggerRegradeResponseDto>.Failure(
            "Chức năng manual regrade chưa được triển khai trong job service hiện tại.",
            ErrorCodeEnum.InvalidOperation));
    }
}
