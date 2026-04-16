using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamTestCaseScore : BaseEntity
{
    public Guid ExamSubmissionId { get; set; }
    public ExamSubmission ExamSubmission { get; set; } = null!;

    public Guid ExamTestCaseId { get; set; }
    public ExamTestCase ExamTestCase { get; set; } = null!;

    public decimal PointsEarned { get; set; }
    public decimal MaxPoints { get; set; }
    public ExamTestCaseOutcome Outcome { get; set; } = ExamTestCaseOutcome.Pending;
    public string? Message { get; set; }
}
