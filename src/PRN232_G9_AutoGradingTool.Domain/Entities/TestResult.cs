using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class TestResult : BaseEntity
{
    public Guid SubmissionId { get; set; }

    public double TotalScore { get; set; }
    public ExamTestCaseOutcome TestStatus { get; set; } = ExamTestCaseOutcome.Pending;

    public ExamSubmission Submission { get; set; } = null!;
    public ICollection<TestResultDetail> Details { get; set; } = new List<TestResultDetail>();
}