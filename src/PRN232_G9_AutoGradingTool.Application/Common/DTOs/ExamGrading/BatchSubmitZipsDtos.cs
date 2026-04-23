using Microsoft.AspNetCore.Http;

namespace PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;

// === Request ===
public class StudentZipEntry
{
    public Guid ExamTopicId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public IFormFile Q1Zip { get; set; } = null!;
    public IFormFile Q2Zip { get; set; } = null!;
}

public class BatchSubmitZipsRequest
{
    public List<StudentZipEntry> Entries { get; set; } = new();
}

// === Response ===
public record StudentSubmitResultItem(
    string StudentCode,
    bool Success,
    Guid? SubmissionId,
    string? Error);

public record BatchSubmitZipsResponseDto(
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<StudentSubmitResultItem> Results);
