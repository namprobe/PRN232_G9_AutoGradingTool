using MediatR;
using PRN232_G9_AutoGradingTool.Application.Common.Attributes;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Application.Features.ExamSessions.Commands.CreateExamSession;

[Authorize(Roles = $"{nameof(RoleEnum.SystemAdmin)},{nameof(RoleEnum.Instructor)}")]
public record CreateExamSessionCommand(
    Guid SemesterId,
    string Code,
    string Title,
    DateTime StartsAtUtc,
    int ExamDurationMinutes,
    DateTime EndsAtUtc,
    bool DeferredClassGrading = false,
    IReadOnlyList<CreateExamSessionTopicRequest>? Topics = null)
    : IRequest<Result<ExamSessionListItemDto>>;
