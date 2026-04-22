using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

/// <summary>Lớp học trong học kỳ (ví dụ SE1830), tối đa sĩ số do MaxStudents.</summary>
public class ExamClass : BaseEntity
{
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Giới hạn sĩ số (mặc định 35).</summary>
    public int MaxStudents { get; set; } = 35;

    public ICollection<ExamSessionClass> SessionClasses { get; set; } = new List<ExamSessionClass>();
}
