using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class Semester : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }

    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
    public ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();
}
