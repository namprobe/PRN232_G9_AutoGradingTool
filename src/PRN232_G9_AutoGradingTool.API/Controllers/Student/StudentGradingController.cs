using MediatR;
using Microsoft.AspNetCore.Mvc;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Application.Features.Submissions.Commands.BatchSubmitZips;
using Swashbuckle.AspNetCore.Annotations;

namespace PRN232_G9_AutoGradingTool.API.Controllers.Student;

/// <summary>Giả lập portal nộp bài của SV — cùng luồng lưu file qua FileServiceFactory (local storage).</summary>
[ApiController]
[Route("api/student/grading")]
[ApiExplorerSettings(GroupName = "v1")]
public class StudentGradingController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentGradingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("exam-sessions/{sessionId:guid}/submissions/batch")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "[SV] Nộp batch tối đa 50 SV — mỗi SV 2 zip Q1+Q2",
        Description = "Checks StartsAtUtc <= now <= EndsAtUtc. Max 50 students per request. Each entry must include ExamTopicId so files are stored under the correct topic path.",
        OperationId = "Student_BatchSubmitZips")]
    [ProducesResponseType(typeof(Result<BatchSubmitZipsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<BatchSubmitZipsResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchSubmitZips(
        [FromRoute] Guid sessionId,
        [FromForm] BatchSubmitZipsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new BatchSubmitZipsCommand(sessionId, request, BypassExamWindow: false);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(result.GetHttpStatusCode(), result);
    }
}
