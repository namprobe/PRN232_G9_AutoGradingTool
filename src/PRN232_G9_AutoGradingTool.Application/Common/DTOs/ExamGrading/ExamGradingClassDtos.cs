namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;

public record ExamClassListItemDto(Guid Id, Guid SemesterId, string Code, string Name, int MaxStudents);

public record CreateExamClassRequest(string Code, string Name, int MaxStudents = 35);

public record UpdateExamClassRequest(string Code, string Name, int MaxStudents);

public record ExamSessionClassListItemDto(
    Guid Id,
    Guid ExamSessionId,
    Guid ExamClassId,
    string ExamClassCode,
    string ExamClassName,
    int ExpectedStudentCount,
    string BatchStatus,
    int ReadySubmissionCount,
    int TotalSubmissionCount);

public record CreateExamSessionClassRequest(Guid ExamClassId, int ExpectedStudentCount);

public record StartClassBatchGradingRequest(
    /// <summary>Bắt đầu chấm dù chưa đủ số bài Ready theo ExpectedStudentCount.</summary>
    bool ForceStartWithoutFullRoster = false,

    /// <summary>Nếu batch đã Completed — xoá điểm cũ trong lớp và chấm lại.</summary>
    bool RedoCompletedBatch = false);

public record StartClassBatchGradingResponseDto(
    Guid ExamSessionClassId,
    string BatchStatus,
    int GradedCount,
    string Message);
