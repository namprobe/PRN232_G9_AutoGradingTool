using MediatR;
using Microsoft.AspNetCore.Http;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Features.ExamGrading;

public record EgListSemestersQuery : IRequest<Result<List<SemesterListItemDto>>>;
public sealed class EgListSemestersQueryHandler(IExamGradingAppService grading)
    : IRequestHandler<EgListSemestersQuery, Result<List<SemesterListItemDto>>>
{
    public Task<Result<List<SemesterListItemDto>>> Handle(EgListSemestersQuery request, CancellationToken cancellationToken)
        => grading.ListSemestersAsync(cancellationToken);
}

public record EgCreateSemesterCommand(CreateSemesterRequest Body) : IRequest<Result<SemesterListItemDto>>;
public sealed class EgCreateSemesterCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgCreateSemesterCommand, Result<SemesterListItemDto>>
{
    public Task<Result<SemesterListItemDto>> Handle(EgCreateSemesterCommand request, CancellationToken cancellationToken)
        => admin.CreateSemesterAsync(request.Body, cancellationToken);
}

public record EgUpdateSemesterCommand(Guid Id, UpdateSemesterRequest Body) : IRequest<Result<SemesterListItemDto>>;
public sealed class EgUpdateSemesterCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgUpdateSemesterCommand, Result<SemesterListItemDto>>
{
    public Task<Result<SemesterListItemDto>> Handle(EgUpdateSemesterCommand request, CancellationToken cancellationToken)
        => admin.UpdateSemesterAsync(request.Id, request.Body, cancellationToken);
}

public record EgDeleteSemesterCommand(Guid Id) : IRequest<Result<bool>>;
public sealed class EgDeleteSemesterCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgDeleteSemesterCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgDeleteSemesterCommand request, CancellationToken cancellationToken)
        => admin.DeleteSemesterAsync(request.Id, cancellationToken);
}

public record EgListExamSessionsQuery(Guid? SemesterId) : IRequest<Result<List<ExamSessionListItemDto>>>;
public sealed class EgListExamSessionsQueryHandler(IExamGradingAppService grading)
    : IRequestHandler<EgListExamSessionsQuery, Result<List<ExamSessionListItemDto>>>
{
    public Task<Result<List<ExamSessionListItemDto>>> Handle(EgListExamSessionsQuery request, CancellationToken cancellationToken)
        => grading.ListExamSessionsAsync(request.SemesterId, cancellationToken);
}

public record EgCreateExamSessionCommand(CreateExamSessionRequest Body) : IRequest<Result<ExamSessionListItemDto>>;
public sealed class EgCreateExamSessionCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgCreateExamSessionCommand, Result<ExamSessionListItemDto>>
{
    public Task<Result<ExamSessionListItemDto>> Handle(EgCreateExamSessionCommand request, CancellationToken cancellationToken)
        => admin.CreateExamSessionAsync(request.Body, cancellationToken);
}

public record EgUpdateExamSessionCommand(Guid Id, UpdateExamSessionRequest Body) : IRequest<Result<ExamSessionListItemDto>>;
public sealed class EgUpdateExamSessionCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgUpdateExamSessionCommand, Result<ExamSessionListItemDto>>
{
    public Task<Result<ExamSessionListItemDto>> Handle(EgUpdateExamSessionCommand request, CancellationToken cancellationToken)
        => admin.UpdateExamSessionAsync(request.Id, request.Body, cancellationToken);
}

public record EgDeleteExamSessionCommand(Guid Id) : IRequest<Result<bool>>;
public sealed class EgDeleteExamSessionCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgDeleteExamSessionCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgDeleteExamSessionCommand request, CancellationToken cancellationToken)
        => admin.DeleteExamSessionAsync(request.Id, cancellationToken);
}

public record EgCreateTopicCommand(Guid SessionId, CreateExamTopicRequest Body) : IRequest<Result<ExamTopicDetailDto>>;
public sealed class EgCreateTopicCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgCreateTopicCommand, Result<ExamTopicDetailDto>>
{
    public Task<Result<ExamTopicDetailDto>> Handle(EgCreateTopicCommand request, CancellationToken cancellationToken)
        => admin.CreateTopicAsync(request.SessionId, request.Body, cancellationToken);
}

public record EgUpdateTopicCommand(Guid TopicId, UpdateExamTopicRequest Body) : IRequest<Result<ExamTopicDetailDto>>;
public sealed class EgUpdateTopicCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgUpdateTopicCommand, Result<ExamTopicDetailDto>>
{
    public Task<Result<ExamTopicDetailDto>> Handle(EgUpdateTopicCommand request, CancellationToken cancellationToken)
        => admin.UpdateTopicAsync(request.TopicId, request.Body, cancellationToken);
}

public record EgDeleteTopicCommand(Guid TopicId) : IRequest<Result<bool>>;
public sealed class EgDeleteTopicCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgDeleteTopicCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgDeleteTopicCommand request, CancellationToken cancellationToken)
        => admin.DeleteTopicAsync(request.TopicId, cancellationToken);
}

public record EgCreateQuestionCommand(Guid TopicId, CreateExamQuestionRequest Body) : IRequest<Result<ExamQuestionDetailDto>>;
public sealed class EgCreateQuestionCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgCreateQuestionCommand, Result<ExamQuestionDetailDto>>
{
    public Task<Result<ExamQuestionDetailDto>> Handle(EgCreateQuestionCommand request, CancellationToken cancellationToken)
        => admin.CreateQuestionAsync(request.TopicId, request.Body, cancellationToken);
}

public record EgUpdateQuestionCommand(Guid QuestionId, UpdateExamQuestionRequest Body) : IRequest<Result<ExamQuestionDetailDto>>;
public sealed class EgUpdateQuestionCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgUpdateQuestionCommand, Result<ExamQuestionDetailDto>>
{
    public Task<Result<ExamQuestionDetailDto>> Handle(EgUpdateQuestionCommand request, CancellationToken cancellationToken)
        => admin.UpdateQuestionAsync(request.QuestionId, request.Body, cancellationToken);
}

public record EgDeleteQuestionCommand(Guid QuestionId) : IRequest<Result<bool>>;
public sealed class EgDeleteQuestionCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgDeleteQuestionCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgDeleteQuestionCommand request, CancellationToken cancellationToken)
        => admin.DeleteQuestionAsync(request.QuestionId, cancellationToken);
}

public record EgCreateTestCaseCommand(Guid QuestionId, CreateExamTestCaseRequest Body) : IRequest<Result<ExamTestCaseDetailDto>>;
public sealed class EgCreateTestCaseCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgCreateTestCaseCommand, Result<ExamTestCaseDetailDto>>
{
    public Task<Result<ExamTestCaseDetailDto>> Handle(EgCreateTestCaseCommand request, CancellationToken cancellationToken)
        => admin.CreateTestCaseAsync(request.QuestionId, request.Body, cancellationToken);
}

public record EgUpdateTestCaseCommand(Guid TestCaseId, UpdateExamTestCaseRequest Body) : IRequest<Result<ExamTestCaseDetailDto>>;
public sealed class EgUpdateTestCaseCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgUpdateTestCaseCommand, Result<ExamTestCaseDetailDto>>
{
    public Task<Result<ExamTestCaseDetailDto>> Handle(EgUpdateTestCaseCommand request, CancellationToken cancellationToken)
        => admin.UpdateTestCaseAsync(request.TestCaseId, request.Body, cancellationToken);
}

public record EgDeleteTestCaseCommand(Guid TestCaseId) : IRequest<Result<bool>>;
public sealed class EgDeleteTestCaseCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgDeleteTestCaseCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgDeleteTestCaseCommand request, CancellationToken cancellationToken)
        => admin.DeleteTestCaseAsync(request.TestCaseId, cancellationToken);
}

public record EgListGradingPacksQuery(Guid SessionId) : IRequest<Result<List<ExamGradingPackListItemDto>>>;
public sealed class EgListGradingPacksQueryHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgListGradingPacksQuery, Result<List<ExamGradingPackListItemDto>>>
{
    public Task<Result<List<ExamGradingPackListItemDto>>> Handle(EgListGradingPacksQuery request, CancellationToken cancellationToken)
        => admin.ListGradingPacksAsync(request.SessionId, cancellationToken);
}

public record EgCreateGradingPackCommand(Guid SessionId, CreateGradingPackRequest Body) : IRequest<Result<ExamGradingPackListItemDto>>;
public sealed class EgCreateGradingPackCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgCreateGradingPackCommand, Result<ExamGradingPackListItemDto>>
{
    public Task<Result<ExamGradingPackListItemDto>> Handle(EgCreateGradingPackCommand request, CancellationToken cancellationToken)
        => admin.CreateGradingPackAsync(request.SessionId, request.Body, cancellationToken);
}

public record EgUpdateGradingPackCommand(Guid PackId, UpdateGradingPackRequest Body) : IRequest<Result<ExamGradingPackListItemDto>>;
public sealed class EgUpdateGradingPackCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgUpdateGradingPackCommand, Result<ExamGradingPackListItemDto>>
{
    public Task<Result<ExamGradingPackListItemDto>> Handle(EgUpdateGradingPackCommand request, CancellationToken cancellationToken)
        => admin.UpdateGradingPackAsync(request.PackId, request.Body, cancellationToken);
}

public record EgDeleteGradingPackCommand(Guid PackId) : IRequest<Result<bool>>;
public sealed class EgDeleteGradingPackCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgDeleteGradingPackCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgDeleteGradingPackCommand request, CancellationToken cancellationToken)
        => admin.DeleteGradingPackAsync(request.PackId, cancellationToken);
}

public record EgCreatePackAssetCommand(Guid PackId, ExamPackAssetKind Kind, IFormFile File) : IRequest<Result<ExamPackAssetListItemDto>>;
public sealed class EgCreatePackAssetCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgCreatePackAssetCommand, Result<ExamPackAssetListItemDto>>
{
    public Task<Result<ExamPackAssetListItemDto>> Handle(EgCreatePackAssetCommand request, CancellationToken cancellationToken)
        => admin.CreatePackAssetAsync(request.PackId, request.Kind, request.File, cancellationToken);
}

public record EgDeletePackAssetCommand(Guid AssetId) : IRequest<Result<bool>>;
public sealed class EgDeletePackAssetCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgDeletePackAssetCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgDeletePackAssetCommand request, CancellationToken cancellationToken)
        => admin.DeletePackAssetAsync(request.AssetId, cancellationToken);
}

public record EgGetExamSessionQuery(Guid Id) : IRequest<Result<ExamSessionDetailDto>>;
public sealed class EgGetExamSessionQueryHandler(IExamGradingAppService grading)
    : IRequestHandler<EgGetExamSessionQuery, Result<ExamSessionDetailDto>>
{
    public Task<Result<ExamSessionDetailDto>> Handle(EgGetExamSessionQuery request, CancellationToken cancellationToken)
        => grading.GetExamSessionAsync(request.Id, cancellationToken);
}

public record EgListSubmissionsQuery(Guid ExamSessionId, Guid? ExamSessionClassId) : IRequest<Result<List<ExamSubmissionListItemDto>>>;
public sealed class EgListSubmissionsQueryHandler(IExamGradingAppService grading)
    : IRequestHandler<EgListSubmissionsQuery, Result<List<ExamSubmissionListItemDto>>>
{
    public Task<Result<List<ExamSubmissionListItemDto>>> Handle(EgListSubmissionsQuery request, CancellationToken cancellationToken)
        => grading.ListSubmissionsAsync(request.ExamSessionId, request.ExamSessionClassId, cancellationToken);
}

public record EgGetSubmissionQuery(Guid Id) : IRequest<Result<ExamSubmissionDetailDto>>;
public sealed class EgGetSubmissionQueryHandler(IExamGradingAppService grading)
    : IRequestHandler<EgGetSubmissionQuery, Result<ExamSubmissionDetailDto>>
{
    public Task<Result<ExamSubmissionDetailDto>> Handle(EgGetSubmissionQuery request, CancellationToken cancellationToken)
        => grading.GetSubmissionAsync(request.Id, cancellationToken);
}

public record EgCreateSubmissionCommand(
    Guid ExamSessionId,
    string StudentCode,
    string? StudentName,
    Guid? ExamSessionClassId,
    IFormFile Q1Zip,
    IFormFile Q2Zip,
    bool BypassExamWindow) : IRequest<Result<Guid>>;
public sealed class EgCreateSubmissionCommandHandler(IExamGradingAppService grading)
    : IRequestHandler<EgCreateSubmissionCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(EgCreateSubmissionCommand request, CancellationToken cancellationToken)
        => grading.CreateSubmissionWithZipAsync(
            request.ExamSessionId,
            request.StudentCode,
            request.StudentName,
            request.Q1Zip,
            request.Q2Zip,
            request.BypassExamWindow,
            request.ExamSessionClassId,
            cancellationToken);
}

public record EgListExamClassesQuery(Guid SemesterId) : IRequest<Result<List<ExamClassListItemDto>>>;
public sealed class EgListExamClassesQueryHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgListExamClassesQuery, Result<List<ExamClassListItemDto>>>
{
    public Task<Result<List<ExamClassListItemDto>>> Handle(EgListExamClassesQuery request, CancellationToken cancellationToken)
        => admin.ListExamClassesAsync(request.SemesterId, cancellationToken);
}

public record EgCreateExamClassCommand(Guid SemesterId, CreateExamClassRequest Body) : IRequest<Result<ExamClassListItemDto>>;
public sealed class EgCreateExamClassCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgCreateExamClassCommand, Result<ExamClassListItemDto>>
{
    public Task<Result<ExamClassListItemDto>> Handle(EgCreateExamClassCommand request, CancellationToken cancellationToken)
        => admin.CreateExamClassAsync(request.SemesterId, request.Body, cancellationToken);
}

public record EgUpdateExamClassCommand(Guid Id, UpdateExamClassRequest Body) : IRequest<Result<ExamClassListItemDto>>;
public sealed class EgUpdateExamClassCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgUpdateExamClassCommand, Result<ExamClassListItemDto>>
{
    public Task<Result<ExamClassListItemDto>> Handle(EgUpdateExamClassCommand request, CancellationToken cancellationToken)
        => admin.UpdateExamClassAsync(request.Id, request.Body, cancellationToken);
}

public record EgDeleteExamClassCommand(Guid Id) : IRequest<Result<bool>>;
public sealed class EgDeleteExamClassCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgDeleteExamClassCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgDeleteExamClassCommand request, CancellationToken cancellationToken)
        => admin.DeleteExamClassAsync(request.Id, cancellationToken);
}

public record EgListExamSessionClassesQuery(Guid SessionId) : IRequest<Result<List<ExamSessionClassListItemDto>>>;
public sealed class EgListExamSessionClassesQueryHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgListExamSessionClassesQuery, Result<List<ExamSessionClassListItemDto>>>
{
    public Task<Result<List<ExamSessionClassListItemDto>>> Handle(EgListExamSessionClassesQuery request, CancellationToken cancellationToken)
        => admin.ListExamSessionClassesAsync(request.SessionId, cancellationToken);
}

public record EgCreateExamSessionClassCommand(Guid SessionId, CreateExamSessionClassRequest Body) : IRequest<Result<ExamSessionClassListItemDto>>;
public sealed class EgCreateExamSessionClassCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgCreateExamSessionClassCommand, Result<ExamSessionClassListItemDto>>
{
    public Task<Result<ExamSessionClassListItemDto>> Handle(EgCreateExamSessionClassCommand request, CancellationToken cancellationToken)
        => admin.CreateExamSessionClassAsync(request.SessionId, request.Body, cancellationToken);
}

public record EgDeleteExamSessionClassCommand(Guid Id) : IRequest<Result<bool>>;
public sealed class EgDeleteExamSessionClassCommandHandler(IExamGradingAdminService admin)
    : IRequestHandler<EgDeleteExamSessionClassCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgDeleteExamSessionClassCommand request, CancellationToken cancellationToken)
        => admin.DeleteExamSessionClassAsync(request.Id, cancellationToken);
}

public record EgStartClassBatchGradingCommand(Guid Id, StartClassBatchGradingRequest Body) : IRequest<Result<StartClassBatchGradingResponseDto>>;
public sealed class EgStartClassBatchGradingCommandHandler(IExamGradingAppService grading)
    : IRequestHandler<EgStartClassBatchGradingCommand, Result<StartClassBatchGradingResponseDto>>
{
    public Task<Result<StartClassBatchGradingResponseDto>> Handle(EgStartClassBatchGradingCommand request, CancellationToken cancellationToken)
        => grading.StartClassBatchGradingAsync(request.Id, request.Body, cancellationToken);
}

public record EgReplaceSubmissionFileCommand(Guid Id, string QuestionLabel, IFormFile ZipFile) : IRequest<Result<bool>>;
public sealed class EgReplaceSubmissionFileCommandHandler(IExamGradingAppService grading)
    : IRequestHandler<EgReplaceSubmissionFileCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(EgReplaceSubmissionFileCommand request, CancellationToken cancellationToken)
        => grading.ReplaceSubmissionFileAsync(request.Id, request.QuestionLabel, request.ZipFile, cancellationToken);
}

public record EgTriggerRegradeCommand(Guid Id) : IRequest<Result<TriggerRegradeResponseDto>>;
public sealed class EgTriggerRegradeCommandHandler(IExamGradingAppService grading)
    : IRequestHandler<EgTriggerRegradeCommand, Result<TriggerRegradeResponseDto>>
{
    public Task<Result<TriggerRegradeResponseDto>> Handle(EgTriggerRegradeCommand request, CancellationToken cancellationToken)
        => grading.TriggerRegradeAsync(request.Id, cancellationToken);
}
