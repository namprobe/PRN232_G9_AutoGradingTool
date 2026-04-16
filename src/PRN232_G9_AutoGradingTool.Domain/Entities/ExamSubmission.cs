using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamSubmission : BaseEntity
{
    public Guid ExamSessionId { get; set; }
    public ExamSession ExamSession { get; set; } = null!;

    public string StudentCode { get; set; } = string.Empty;
    public string? StudentName { get; set; }

    /// <summary>Trạng thái pipeline (cột workflow_status — tránh trùng BaseEntity.Status).</summary>
    public ExamSubmissionStatus WorkflowStatus { get; set; } = ExamSubmissionStatus.Pending;

    public string? Q1ZipRelativePath { get; set; }
    public string? Q2ZipRelativePath { get; set; }

    public decimal? TotalScore { get; set; }
    public DateTime SubmittedAtUtc { get; set; }

    public ICollection<ExamQuestionScore> QuestionScores { get; set; } = new List<ExamQuestionScore>();
    public ICollection<ExamTestCaseScore> TestCaseScores { get; set; } = new List<ExamTestCaseScore>();
}
