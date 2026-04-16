using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

/// <summary>File kèm pack (Postman, PDF đề, …) — đường dẫn tương đối storage.</summary>
public class ExamPackAsset : BaseEntity
{
    public Guid ExamGradingPackId { get; set; }
    public ExamGradingPack Pack { get; set; } = null!;

    public ExamPackAssetKind Kind { get; set; }

    public string StorageRelativePath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
}
