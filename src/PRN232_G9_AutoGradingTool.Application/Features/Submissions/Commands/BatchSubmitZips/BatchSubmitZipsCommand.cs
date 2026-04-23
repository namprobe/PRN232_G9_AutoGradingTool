using MediatR;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Models;

namespace PRN232_G9_AutoGradingTool.Application.Features.Submissions.Commands.BatchSubmitZips;

public record BatchSubmitZipsCommand(
    Guid ExamSessionId,
    BatchSubmitZipsRequest Request,
    bool BypassExamWindow)
    : IRequest<Result<BatchSubmitZipsResponseDto>>;
