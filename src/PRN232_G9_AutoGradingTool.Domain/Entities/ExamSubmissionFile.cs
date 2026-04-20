using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

/// <summary>File zip bài nộp của sinh viên cho một câu hỏi (Q1, Q2, ...).</summary>
public class ExamSubmissionFile : BaseEntity
{
    public Guid ExamSubmissionId { get; set; }
    public ExamSubmission ExamSubmission { get; set; } = null!;

    /// <summary>Nhãn câu hỏi tương ứng, ví dụ: "Q1", "Q2".</summary>
    public string QuestionLabel { get; set; } = string.Empty;

    public string StorageRelativePath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
}
