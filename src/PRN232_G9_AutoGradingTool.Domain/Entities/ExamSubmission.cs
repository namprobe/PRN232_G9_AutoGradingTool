using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

public class ExamSubmission : BaseEntity
{
    public Guid ExamSessionId { get; set; }
    public ExamSession ExamSession { get; set; } = null!;

    /// <summary>Lớp trong ca (nullable cho bài nộp cũ / không chia lớp).</summary>
    public Guid? ExamSessionClassId { get; set; }
    public ExamSessionClass? ExamSessionClass { get; set; }

    /// <summary>Pack dùng khi nộp (đóng băng version chấm).</summary>
    public Guid? ExamGradingPackId { get; set; }
    public ExamGradingPack? ExamGradingPack { get; set; }

    public string StudentCode { get; set; } = string.Empty;
    public string? StudentName { get; set; }

    /// <summary>Trạng thái pipeline (cột workflow_status — tránh trùng BaseEntity.Status).</summary>
    public ExamSubmissionStatus WorkflowStatus { get; set; } = ExamSubmissionStatus.Pending;

    public decimal? TotalScore { get; set; }
    public DateTime SubmittedAtUtc { get; set; }

    public ICollection<ExamSubmissionFile> SubmissionFiles { get; set; } = new List<ExamSubmissionFile>();
    public ICollection<ExamQuestionScore> QuestionScores { get; set; } = new List<ExamQuestionScore>();
    public ICollection<ExamTestCaseScore> TestCaseScores { get; set; } = new List<ExamTestCaseScore>();
    public ICollection<GradingJob> GradingJobs { get; set; } = new List<GradingJob>();
}
