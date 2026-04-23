namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;

public record CreateSemesterRequest(string Code, string Name, DateTime? StartDateUtc, DateTime? EndDateUtc);

public record UpdateSemesterRequest(string Code, string Name, DateTime? StartDateUtc, DateTime? EndDateUtc);

public record CreateExamSessionRequest(
    Guid SemesterId,
    string Code,
    string Title,
    DateTime StartsAtUtc,
    int ExamDurationMinutes,
    DateTime EndsAtUtc,
    bool DeferredClassGrading = false);

public record UpdateExamSessionRequest(
    Guid SemesterId,
    string Code,
    string Title,
    DateTime StartsAtUtc,
    int ExamDurationMinutes,
    DateTime EndsAtUtc,
    bool DeferredClassGrading = false);

public record CreateExamSessionTopicRequest(string Title, int SortOrder);

public record CreateExamTopicRequest(string Title, int SortOrder);

public record UpdateExamTopicRequest(string Title, int SortOrder);

public record CreateExamQuestionRequest(string Label, string Title, decimal MaxScore);

public record UpdateExamQuestionRequest(string Label, string Title, decimal MaxScore);

public record CreateExamTestCaseRequest(string Name, decimal MaxPoints, int SortOrder);

public record UpdateExamTestCaseRequest(string Name, decimal MaxPoints, int SortOrder);

public record CreateGradingPackRequest(string Label, int? Version, bool IsActive);

public record UpdateGradingPackRequest(string Label, bool IsActive);

public record ExamGradingPackListItemDto(Guid Id, int Version, string Label, bool IsActive, int AssetCount);

public record ExamPackAssetListItemDto(
    Guid Id,
    Guid ExamGradingPackId,
    int Kind,
    string StorageRelativePath,
    string? OriginalFileName);
