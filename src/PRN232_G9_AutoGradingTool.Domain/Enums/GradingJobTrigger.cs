namespace PRN232_G9_AutoGradingTool.Domain.Enums;

public enum GradingJobTrigger
{
    /// <summary>Tự động khi ca thi kết thúc — SummarizeExamResultJob enqueue hàng loạt.</summary>
    SessionEnd = 0,

    /// <summary>
    /// Admin / Proctor upload lại file thủ công và trigger chấm riêng lẻ.
    /// Thường dùng khi SV gặp sự cố kỹ thuật, gửi bài qua mail hoặc kênh khác.
    /// </summary>
    ManualRegrade = 1
}
