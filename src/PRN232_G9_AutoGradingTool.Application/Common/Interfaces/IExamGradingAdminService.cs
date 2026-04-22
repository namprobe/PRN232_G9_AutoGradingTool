using Microsoft.AspNetCore.Http;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Common.Interfaces;

/// <summary>CRUD cấu trúc đề + pack (CMS).</summary>
public interface IExamGradingAdminService
{
    Task<Result<SemesterListItemDto>> CreateSemesterAsync(CreateSemesterRequest req, CancellationToken cancellationToken = default);
    Task<Result<SemesterListItemDto>> UpdateSemesterAsync(Guid id, UpdateSemesterRequest req, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteSemesterAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ExamSessionListItemDto>> CreateExamSessionAsync(CreateExamSessionRequest req, CancellationToken cancellationToken = default);
    Task<Result<ExamSessionListItemDto>> UpdateExamSessionAsync(Guid id, UpdateExamSessionRequest req, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteExamSessionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ExamTopicDetailDto>> CreateTopicAsync(Guid examSessionId, CreateExamTopicRequest req, CancellationToken cancellationToken = default);
    Task<Result<ExamTopicDetailDto>> UpdateTopicAsync(Guid topicId, UpdateExamTopicRequest req, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteTopicAsync(Guid topicId, CancellationToken cancellationToken = default);

    Task<Result<ExamQuestionDetailDto>> CreateQuestionAsync(Guid topicId, CreateExamQuestionRequest req, CancellationToken cancellationToken = default);
    Task<Result<ExamQuestionDetailDto>> UpdateQuestionAsync(Guid questionId, UpdateExamQuestionRequest req, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default);

    Task<Result<ExamTestCaseDetailDto>> CreateTestCaseAsync(Guid questionId, CreateExamTestCaseRequest req, CancellationToken cancellationToken = default);
    Task<Result<ExamTestCaseDetailDto>> UpdateTestCaseAsync(Guid testCaseId, UpdateExamTestCaseRequest req, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteTestCaseAsync(Guid testCaseId, CancellationToken cancellationToken = default);

    Task<Result<List<ExamGradingPackListItemDto>>> ListGradingPacksAsync(Guid examSessionId, CancellationToken cancellationToken = default);
    Task<Result<ExamGradingPackListItemDto>> CreateGradingPackAsync(Guid examSessionId, CreateGradingPackRequest req, CancellationToken cancellationToken = default);
    Task<Result<ExamGradingPackListItemDto>> UpdateGradingPackAsync(Guid packId, UpdateGradingPackRequest req, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteGradingPackAsync(Guid packId, CancellationToken cancellationToken = default);

    Task<Result<ExamPackAssetListItemDto>> CreatePackAssetAsync(
        Guid packId,
        ExamPackAssetKind kind,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeletePackAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<Result<List<ExamClassListItemDto>>> ListExamClassesAsync(Guid semesterId, CancellationToken cancellationToken = default);
    Task<Result<ExamClassListItemDto>> CreateExamClassAsync(Guid semesterId, CreateExamClassRequest req, CancellationToken cancellationToken = default);
    Task<Result<ExamClassListItemDto>> UpdateExamClassAsync(Guid id, UpdateExamClassRequest req, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteExamClassAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<List<ExamSessionClassListItemDto>>> ListExamSessionClassesAsync(Guid examSessionId, CancellationToken cancellationToken = default);
    Task<Result<ExamSessionClassListItemDto>> CreateExamSessionClassAsync(Guid examSessionId, CreateExamSessionClassRequest req, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteExamSessionClassAsync(Guid examSessionClassId, CancellationToken cancellationToken = default);
}
