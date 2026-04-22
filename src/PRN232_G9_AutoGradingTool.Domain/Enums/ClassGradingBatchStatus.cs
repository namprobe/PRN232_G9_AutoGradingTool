namespace PRN232_G9_AutoGradingTool.Domain.Enums;

/// <summary>Tiến độ chấm theo lớp (một ca thi × một lớp).</summary>
public enum ClassGradingBatchStatus
{
    /// <summary>Đang nhận bài — chưa chạy batch chấm.</summary>
    CollectingSubmissions = 0,

    /// <summary>Đang chấm lần lượt từng SV trong lớp.</summary>
    GradingInProgress,

    /// <summary>Đã chấm xong đợt batch (có thể redo).</summary>
    Completed,

    /// <summary>Lỗi khi chấm batch — xem log / submission Failed.</summary>
    Failed
}
