namespace PRN232_G9_AutoGradingTool.Domain.Enums;

/// <summary>Trạng thái pipeline chấm một lần nộp (2 zip).</summary>
public enum ExamSubmissionStatus
{
    Pending = 0,
    Queued,
    Running,
    Completed,
    Failed
}
