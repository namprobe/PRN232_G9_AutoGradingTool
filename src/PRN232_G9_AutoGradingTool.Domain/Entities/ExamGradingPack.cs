using PRN232_G9_AutoGradingTool.Domain.Common;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

/// <summary>Gói đề / bộ chấm version theo ca thi (topic đổi theo kỳ — dữ liệu DB).</summary>
public class ExamGradingPack : BaseEntity
{
    public Guid ExamSessionId { get; set; }
    public ExamSession ExamSession { get; set; } = null!;

    /// <summary>Tăng dần theo ca (1,2,…).</summary>
    public int Version { get; set; }

    public string Label { get; set; } = string.Empty;

    /// <summary>Pack đang dùng để chấm (một ca nên chỉ một active).</summary>
    public bool IsActive { get; set; }

    public ICollection<ExamPackAsset> Assets { get; set; } = new List<ExamPackAsset>();
    public ICollection<GradingTestDefinition> TestDefinitions { get; set; } = new List<GradingTestDefinition>();
    public ICollection<GradingJob> Jobs { get; set; } = new List<GradingJob>();
}
