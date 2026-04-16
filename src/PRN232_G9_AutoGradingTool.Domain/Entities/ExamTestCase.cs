using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamTestCase : BaseEntity
{
    public Guid ExamQuestionId { get; set; }
    public ExamQuestion ExamQuestion { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public decimal MaxPoints { get; set; }
    public int SortOrder { get; set; }

    public ICollection<ExamTestCaseScore> Scores { get; set; } = new List<ExamTestCaseScore>();
}
