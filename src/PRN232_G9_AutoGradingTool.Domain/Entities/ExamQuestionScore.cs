using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamQuestionScore : BaseEntity
{
    public Guid ExamSubmissionId { get; set; }
    public ExamSubmission ExamSubmission { get; set; } = null!;

    public Guid ExamQuestionId { get; set; }
    public ExamQuestion ExamQuestion { get; set; } = null!;

    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public string? Summary { get; set; }
}
