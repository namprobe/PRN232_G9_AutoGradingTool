using MediatR;
using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Common;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Features.ExamSessions.Commands.CreateExamSession;

public class CreateExamSessionCommandHandler
    : IRequestHandler<CreateExamSessionCommand, Result<ExamSessionListItemDto>>
{
    private static readonly RoleEnum[] AllowedCreatorRoles = [RoleEnum.SystemAdmin, RoleEnum.Instructor];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IExamGradingJobService _jobService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalizationService _localizationService;

    public CreateExamSessionCommandHandler(
        IUnitOfWork unitOfWork,
        IExamGradingJobService jobService,
        ICurrentUserService currentUserService,
        ILocalizationService localizationService)
    {
        _unitOfWork = unitOfWork;
        _jobService = jobService;
        _currentUserService = currentUserService;
        _localizationService = localizationService;
    }

    public async Task<Result<ExamSessionListItemDto>> Handle(
        CreateExamSessionCommand command, CancellationToken cancellationToken)
    {
        var authorizationResult = await AuthorizeCreatorAsync();
        if (!authorizationResult.isAuthorized || authorizationResult.userId == null)
        {
            return authorizationResult.error!;
        }

        if (!await _unitOfWork.Repository<Semester>()
                .AnyAsync(x => x.Id == command.SemesterId, cancellationToken))
        {
            return Result<ExamSessionListItemDto>.Failure(
                "Khong tim thay hoc ky.", ErrorCodeEnum.NotFound);
        }

        var code = command.Code.Trim();
        var title = command.Title.Trim();

        if (await _unitOfWork.Repository<ExamSession>()
                .AnyAsync(x => x.SemesterId == command.SemesterId && x.Code == code, cancellationToken))
        {
            return Result<ExamSessionListItemDto>.Failure(
                "Ma ca thi da ton tai trong hoc ky.", ErrorCodeEnum.DuplicateEntry);
        }

        var entity = new ExamSession
        {
            SemesterId = command.SemesterId,
            Code = code,
            Title = title,
            StartsAtUtc = command.StartsAtUtc,
            ExamDurationMinutes = command.ExamDurationMinutes,
            EndsAtUtc = command.EndsAtUtc,
            DeferredClassGrading = command.DeferredClassGrading,
            Status = EntityStatusEnum.Active
        };
        entity.InitializeEntity(authorizationResult.userId);

        string? scheduleJobId = null;
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            scheduleJobId = _jobService.ScheduleSummarizeExamResultJob(entity.Id, entity.EndsAtUtc);
            entity.HangfireScheduleJobId = scheduleJobId;
            await _unitOfWork.Repository<ExamSession>().AddAsync(entity, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(scheduleJobId))
            {
                _jobService.DeleteScheduledJob(scheduleJobId);
            }

            throw;
        }

        var dto = await _unitOfWork.Repository<ExamSession>()
            .GetQueryable()
            .Where(x => x.Id == entity.Id)
            .Select(x => new ExamSessionListItemDto(
                x.Id,
                x.Code,
                x.Title,
                x.SemesterId,
                x.Semester.Code,
                x.StartsAtUtc,
                x.ExamDurationMinutes,
                x.EndsAtUtc,
                x.DeferredClassGrading,
                x.Topics.Count,
                x.Topics.SelectMany(t => t.Questions).Count(),
                x.Submissions.Count))
            .FirstAsync(cancellationToken);

        return Result<ExamSessionListItemDto>.Success(dto, "Da tao ca thi.");
    }

    private async Task<(bool isAuthorized, Guid? userId, Result<ExamSessionListItemDto>? error)> AuthorizeCreatorAsync()
    {
        var (isValid, userId, roles) = await _currentUserService.ValidateUserWithRolesAsync();
        if (!isValid || userId == null)
        {
            return (
                false,
                null,
                Result<ExamSessionListItemDto>.Failure(
                    _localizationService.GetErrorMessage(ErrorCodeEnum.Unauthorized),
                    ErrorCodeEnum.Unauthorized));
        }

        if (!roles.Any(role => AllowedCreatorRoles.Contains(role)))
        {
            return (
                false,
                userId,
                Result<ExamSessionListItemDto>.Failure(
                    _localizationService.GetErrorMessage(ErrorCodeEnum.InsufficientPermissions),
                    ErrorCodeEnum.InsufficientPermissions));
        }

        return (true, userId, null);
    }
}
