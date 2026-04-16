using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamTopic : BaseEntity
{
    public Guid ExamSessionId { get; set; }
    public ExamSession ExamSession { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
}
