using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamQuestion : BaseEntity
{
    public Guid ExamTopicId { get; set; }
    public ExamTopic ExamTopic { get; set; } = null!;

    /// <summary>Ví dụ Q1, Q2</summary>
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }

    public ICollection<ExamTestCase> TestCases { get; set; } = new List<ExamTestCase>();
    public ICollection<ExamQuestionScore> QuestionScores { get; set; } = new List<ExamQuestionScore>();
}
