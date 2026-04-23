using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>Read-only query service for grading data.</summary>
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
}
