using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Jobs;

/// <summary>
/// Scheduled job chạy khi ca thi kết thúc (EndsAtUtc).
/// Tìm tất cả bài nộp còn Pending → tạo GradingJob → enqueue GradeSubmissionJob cho từng bài.
/// Mỗi lần thực thi mở một IServiceScope riêng để lấy IUnitOfWork — không inject DbContext trực tiếp.
/// </summary>
public class SummarizeExamResultJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundJobClient _client;
    private readonly ILogger<SummarizeExamResultJob> _logger;

    public SummarizeExamResultJob(
        IServiceProvider serviceProvider,
        IBackgroundJobClient client,
        ILogger<SummarizeExamResultJob> logger)
    {
        _serviceProvider = serviceProvider;
        _client = client;
        _logger = logger;
    }

    [Queue("grading")]
    public async Task ExecuteAsync(Guid examSessionId, CancellationToken ct = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // 1. Load session
        var session = await uow.Repository<ExamSession>()
            .GetFirstOrDefaultAsync(s => s.Id == examSessionId, cancellationToken: ct);

        if (session is null)
        {
            _logger.LogWarning("SummarizeExamResultJob: ExamSession {Id} not found.", examSessionId);
            return;
        }

        // 2. Idempotency guard — if fired too early, skip
        if (DateTime.UtcNow < session.EndsAtUtc.AddSeconds(-30))
        {
            _logger.LogInformation("SummarizeExamResultJob: session {Code} fired early — skipping.", session.Code);
            return;
        }

        // 3. Find active grading pack
        var pack = await uow.Repository<ExamGradingPack>()
            .GetFirstOrDefaultAsync(p => p.ExamSessionId == examSessionId && p.IsActive, cancellationToken: ct);

        if (pack is null)
        {
            _logger.LogWarning("SummarizeExamResultJob: No active grading pack for session {Code}.", session.Code);
            return;
        }

        // 4. Find pending submissions
        var pendingSubmissions = await uow.Repository<ExamSubmission>()
            .GetQueryable()
            .Include(s => s.SubmissionFiles)
            .Where(s => s.ExamSessionId == examSessionId && s.WorkflowStatus == ExamSubmissionStatus.Pending)
            .ToListAsync(ct);

        if (!pendingSubmissions.Any())
        {
            _logger.LogInformation("SummarizeExamResultJob: no pending submissions for session {Code}.", session.Code);
            return;
        }

        var readySubmissions = pendingSubmissions
            .Where(s => s.SubmissionFiles.Any())
            .ToList();

        var skippedSubmissions = pendingSubmissions
            .Where(s => !s.SubmissionFiles.Any())
            .ToList();

        foreach (var skipped in skippedSubmissions)
        {
            _logger.LogWarning(
                "SummarizeExamResultJob: submission {SubmissionId} of student {StudentCode} has no submission files, skipping enqueue.",
                skipped.Id,
                skipped.StudentCode);
        }

        if (!readySubmissions.Any())
        {
            _logger.LogInformation(
                "SummarizeExamResultJob: no pending submissions with files for session {Code}.",
                session.Code);
            return;
        }

        // 5. Create GradingJob for each pending submission
        var jobs = new List<GradingJob>();
        try
        {
            await uow.BeginTransactionAsync(ct);

            foreach (var sub in readySubmissions)
            {
                var gradingJob = new GradingJob
                {
                    ExamSubmissionId = sub.Id,
                    ExamGradingPackId = pack.Id,
                    JobStatus = GradingJobStatus.Queued,
                    Trigger = GradingJobTrigger.SessionEnd,
                    Status = EntityStatusEnum.Active
                };
                gradingJob.InitializeEntity();

                sub.WorkflowStatus = ExamSubmissionStatus.Queued;
                uow.Repository<ExamSubmission>().Update(sub);
                await uow.Repository<GradingJob>().AddAsync(gradingJob, ct);
                jobs.Add(gradingJob);
            }

            await uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await uow.RollbackTransactionAsync(ct);
            throw;
        }

        // 6. Enqueue each job in Hangfire and save the Hangfire job ID back
        foreach (var gradingJob in jobs)
        {
            var hangfireId = _client.Enqueue<GradeSubmissionJob>(
                x => x.ExecuteAsync(gradingJob.Id, CancellationToken.None));
            gradingJob.HangfireJobId = hangfireId;
            uow.Repository<GradingJob>().Update(gradingJob);
        }

        await uow.SaveChangesAsync(ct);

        _logger.LogInformation("SummarizeExamResultJob: enqueued {Count} grading jobs for session {Code}.",
            jobs.Count, session.Code);
    }
}
