namespace PRN232_G9_AutoGradingTool.Domain.Enums;

/// <summary>Một lần chạy chấm (sau này Hangfire sẽ cập nhật).</summary>
public enum GradingJobStatus
{
    Queued = 0,
    Running,
    Completed,
    Failed
}
