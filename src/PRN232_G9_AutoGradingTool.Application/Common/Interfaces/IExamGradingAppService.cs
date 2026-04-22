using Microsoft.AspNetCore.Http;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>REST Auto Grading — đọc ERD + upload zip (stub chấm điểm đồng bộ).</summary>
public interface IExamGradingAppService
{
    Task<Result<List<SemesterListItemDto>>> ListSemestersAsync(CancellationToken cancellationToken = default);

    Task<Result<List<ExamSessionListItemDto>>> ListExamSessionsAsync(Guid? semesterId, CancellationToken cancellationToken = default);

    Task<Result<ExamSessionDetailDto>> GetExamSessionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<List<ExamSubmissionListItemDto>>> ListSubmissionsAsync(
        Guid examSessionId,
        Guid? examSessionClassId = null,
        CancellationToken cancellationToken = default);

    Task<Result<ExamSubmissionDetailDto>> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <param name="bypassExamWindow">CMS/admin: true để nộp hộ hoặc xử lý ngoài khung giờ. SV: false.</param>
    Task<Result<Guid>> CreateSubmissionWithZipAsync(
        Guid examSessionId,
        string studentCode,
        string? studentName,
        IFormFile q1Zip,
        IFormFile q2Zip,
        bool bypassExamWindow = false,
        Guid? examSessionClassId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin upload lại file zip cho một câu của submission đã có.
    /// Không kiểm tra EndsAtUtc — dùng cho trường hợp SV gửi bài qua mail sau sự cố.
    /// </summary>
    Task<Result<bool>> ReplaceSubmissionFileAsync(
        Guid submissionId,
        string questionLabel,
        IFormFile zipFile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Trigger chấm lại thủ công cho một submission (sau khi đã upload lại file).
    /// Tạo GradingJob mới với Trigger=ManualRegrade và enqueue Hangfire ngay lập tức.
    /// </summary>
    Task<Result<TriggerRegradeResponseDto>> TriggerRegradeAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chấm batch theo lớp: chỉ áp dụng khi ca thi bật DeferredClassGrading.
    /// Mặc định cần đủ số bài đã upload Q1+Q2 (Ready) ≥ ExpectedStudentCount, trừ khi force.
    /// </summary>
    Task<Result<StartClassBatchGradingResponseDto>> StartClassBatchGradingAsync(
        Guid examSessionClassId,
        StartClassBatchGradingRequest request,
        CancellationToken cancellationToken = default);
}
