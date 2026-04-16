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

    Task<Result<List<ExamSubmissionListItemDto>>> ListSubmissionsAsync(Guid examSessionId, CancellationToken cancellationToken = default);

    Task<Result<ExamSubmissionDetailDto>> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateSubmissionWithZipAsync(
        Guid examSessionId,
        string studentCode,
        string? studentName,
        IFormFile q1Zip,
        IFormFile q2Zip,
        CancellationToken cancellationToken = default);
}
