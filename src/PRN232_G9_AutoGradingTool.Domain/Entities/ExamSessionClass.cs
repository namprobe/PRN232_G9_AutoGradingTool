using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Domain.Entities;

/// <summary>Một lớp tham gia một ca thi: số suất nộp bài mong đợi + trạng thái batch chấm.</summary>
public class ExamSessionClass : BaseEntity
{
    public Guid ExamSessionId { get; set; }
    public ExamSession ExamSession { get; set; } = null!;

    public Guid ExamClassId { get; set; }
    public ExamClass ExamClass { get; set; } = null!;

    /// <summary>Số bài nộp đủ (Q1+Q2) cần trước khi chấm batch — thường = sĩ số lớp.</summary>
    public int ExpectedStudentCount { get; set; } = 35;

    public ClassGradingBatchStatus BatchStatus { get; set; } = ClassGradingBatchStatus.CollectingSubmissions;

    public ICollection<ExamSubmission> Submissions { get; set; } = new List<ExamSubmission>();
}
