using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

/// <summary>Lần chạy chấm cho một submission (theo một pack cụ thể).</summary>
public class GradingJob : BaseEntity
{
    public Guid ExamSubmissionId { get; set; }
    public ExamSubmission ExamSubmission { get; set; } = null!;

    public Guid ExamGradingPackId { get; set; }
    public ExamGradingPack Pack { get; set; } = null!;

    public GradingJobStatus JobStatus { get; set; } = GradingJobStatus.Queued;

    /// <summary>Hangfire string job ID — dùng để monitor/cancel trên Hangfire dashboard.</summary>
    public string? HangfireJobId { get; set; }

    public int RetryCount { get; set; } = 0;

    public string? ErrorMessage { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public ICollection<GradingJobLog> Logs { get; set; } = new List<GradingJobLog>();
}
