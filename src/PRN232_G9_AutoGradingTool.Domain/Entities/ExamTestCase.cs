using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamTestCase : BaseEntity
{
    public Guid ExamQuestionId { get; set; }
    public ExamQuestion ExamQuestion { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string UrlTemplate { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public int ExpectedStatus { get; set; }
    public string ExpectedBody { get; set; } = string.Empty;
    public double Weight { get; set; }
    public int OrderIndex { get; set; }
    public TestCaseType Type { get; set; } // API / UI
    public bool IsHidden { get; set; }
    public decimal MaxPoints { get; set; }
    public int SortOrder { get; set; }

    public ICollection<ExamTestCaseScore> Scores { get; set; } = new List<ExamTestCaseScore>();
    public ICollection<GradingTestDefinition> GradingDefinitions { get; set; } = new List<GradingTestDefinition>();
    public ICollection<TestResultDetail> ResultDetails { get; set; } = new List<TestResultDetail>();
}