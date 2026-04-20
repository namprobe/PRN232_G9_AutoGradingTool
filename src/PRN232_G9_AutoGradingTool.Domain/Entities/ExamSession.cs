using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamSession : BaseEntity
{
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Thời điểm bắt đầu ca thi (SV bắt đầu làm bài).</summary>
    public DateTime StartsAtUtc { get; set; }

    /// <summary>
    /// Thời lượng làm bài quy định (phút), ví dụ 90.
    /// Dùng để hiển thị cho SV biết họ có bao lâu — không phải thời điểm đóng nộp.
    /// </summary>
    public int ExamDurationMinutes { get; set; } = 90;

    /// <summary>
    /// Thời điểm đóng nộp bài do proctor tự đặt.
    /// Thường = StartsAtUtc + ExamDurationMinutes nhưng có thể gia hạn khi SV gặp sự cố kỹ thuật.
    /// SummarizeExamResultJob được schedule vào thời điểm này.
    /// </summary>
    public DateTime EndsAtUtc { get; set; }

    public ICollection<ExamTopic> Topics { get; set; } = new List<ExamTopic>();
    public ICollection<ExamSubmission> Submissions { get; set; } = new List<ExamSubmission>();
    public ICollection<ExamGradingPack> GradingPacks { get; set; } = new List<ExamGradingPack>();
}
