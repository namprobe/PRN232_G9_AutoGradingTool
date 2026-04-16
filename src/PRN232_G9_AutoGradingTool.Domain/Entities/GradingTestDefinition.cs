using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

/// <summary>Một bước chấm máy trong pack (JSON payload cho runner sau này).</summary>
public class GradingTestDefinition : BaseEntity
{
    public Guid ExamGradingPackId { get; set; }
    public ExamGradingPack Pack { get; set; } = null!;

    /// <summary>Liên kết rubric <see cref="ExamTestCase"/> (nullable nếu chỉ định nghĩa tự do).</summary>
    public Guid? ExamTestCaseId { get; set; }
    public ExamTestCase? ExamTestCase { get; set; }

    public int SortOrder { get; set; }
    public GradingTestDefinitionKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Cấu hình runner (HTTP/Newman/…) — dạng JSON.</summary>
    public string? PayloadJson { get; set; }
}
