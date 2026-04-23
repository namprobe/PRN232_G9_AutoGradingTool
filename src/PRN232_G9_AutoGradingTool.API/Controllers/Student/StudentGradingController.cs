using MediatR;
using Microsoft.AspNetCore.Mvc;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Application.Features.ExamGrading;
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

    [HttpPost("submissions")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "[SV] Nộp 2 zip Q1/Q2 — lưu storage qua FileServiceFactory",
        Description = "Chỉ chấp nhận trong khung giờ ca thi (UTC): StartsAtUtc ≤ now ≤ EndsAtUtc.",
        OperationId = "Student_SubmitZipSubmission")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitSubmission(
        [FromForm] Guid examSessionId,
        [FromForm] string studentCode,
        [FromForm] string? studentName,
        [FromForm] Guid? examSessionClassId,
        IFormFile q1Zip,
        IFormFile q2Zip,
        CancellationToken cancellationToken)
    {
        if (q1Zip == null || q2Zip == null || q1Zip.Length == 0 || q2Zip.Length == 0)
            return BadRequest(Result<Guid>.Failure("Thiếu file zip.", ErrorCodeEnum.ValidationFailed));

        var r = await _mediator.Send(
            new EgCreateSubmissionCommand(
                examSessionId,
                studentCode,
                studentName,
                examSessionClassId,
                q1Zip,
                q2Zip,
                BypassExamWindow: false),
            cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }
}
