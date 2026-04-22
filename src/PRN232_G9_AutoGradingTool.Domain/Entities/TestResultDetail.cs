using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class TestResultDetail : BaseEntity
{
    public Guid ResultId { get; set; }
    public Guid TestCaseId { get; set; }

    public bool Passed { get; set; }
    public double Score { get; set; }

    public int ResponseTime { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public TestResult Result { get; set; } = null!;
    public ExamTestCase TestCase { get; set; } = null!;
}