using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

/// <summary>Log từng phase của một lần chạy chấm: build output, Newman report, runtime error, ...</summary>
public class GradingJobLog : BaseEntity
{
    public Guid GradingJobId { get; set; }
    public GradingJob GradingJob { get; set; } = null!;

    public GradingJobLogPhase Phase { get; set; }
    public GradingJobLogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Raw output: dotnet build stdout/stderr, Newman JSON report, stack trace, ...</summary>
    public string? DetailJson { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
