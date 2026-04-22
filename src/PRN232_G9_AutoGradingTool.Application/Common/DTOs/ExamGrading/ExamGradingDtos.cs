namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;

public record SemesterListItemDto(Guid Id, string Code, string Name, DateTime? StartDateUtc, DateTime? EndDateUtc);

public record ExamSessionListItemDto(
    Guid Id,
    string Code,
    string Title,
    Guid SemesterId,
    string SemesterCode,
    DateTime StartsAtUtc,
    int ExamDurationMinutes,
    DateTime EndsAtUtc,
    bool DeferredClassGrading,
    int TopicCount,
    int QuestionCount,
    int SubmissionCount);

public record ExamSessionDetailDto(
    Guid Id,
    string Code,
    string Title,
    Guid SemesterId,
    string SemesterCode,
    DateTime StartsAtUtc,
    int ExamDurationMinutes,
    DateTime EndsAtUtc,
    bool DeferredClassGrading,
    IReadOnlyList<ExamTopicDetailDto> Topics);

public record ExamTopicDetailDto(Guid Id, string Title, int SortOrder, IReadOnlyList<ExamQuestionDetailDto> Questions);

public record ExamQuestionDetailDto(Guid Id, string Label, string Title, decimal MaxScore, IReadOnlyList<ExamTestCaseDetailDto> TestCases);

public record ExamTestCaseDetailDto(Guid Id, string Name, decimal MaxPoints, int SortOrder);

public record ExamSubmissionListItemDto(
    Guid Id,
    Guid ExamSessionId,
    Guid? ExamSessionClassId,
    string? ClassCode,
    string StudentCode,
    string? StudentName,
    string Status,
    DateTime SubmittedAtUtc,
    decimal? TotalScore);

public record ExamSubmissionDetailDto(
    Guid Id,
    Guid ExamSessionId,
    string ExamSessionCode,
    Guid? ExamSessionClassId,
    string? ClassCode,
    string StudentCode,
    string? StudentName,
    string Status,
    DateTime SubmittedAtUtc,
    decimal? TotalScore,
    IReadOnlyList<SubmissionFileDto> SubmissionFiles,
    IReadOnlyList<ExamQuestionScoreDto> QuestionScores,
    IReadOnlyList<ExamTestCaseScoreDto> TestCaseScores);

public record SubmissionFileDto(string QuestionLabel, string StorageRelativePath, string? OriginalFileName);

public record ExamQuestionScoreDto(Guid ExamQuestionId, string QuestionLabel, decimal Score, decimal MaxScore, string? Summary);

public record ExamTestCaseScoreDto(
    Guid ExamTestCaseId,
    string QuestionLabel,
    string TestCaseName,
    decimal PointsEarned,
    decimal MaxPoints,
    string Outcome,
    string? Message);

/// <summary>Response khi trigger manual regrade thành công.</summary>
public record TriggerRegradeResponseDto(
    Guid GradingJobId,
    string Trigger,
    string JobStatus,
    string Message);
