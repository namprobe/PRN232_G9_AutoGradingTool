using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamSession : BaseEntity
{
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }

    public ICollection<ExamTopic> Topics { get; set; } = new List<ExamTopic>();
    public ICollection<ExamSubmission> Submissions { get; set; } = new List<ExamSubmission>();
}
