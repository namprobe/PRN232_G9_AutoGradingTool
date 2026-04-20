using Microsoft.AspNetCore.Mvc;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace PRN232_G9_AutoGradingTool.API.Controllers.Cms;

/// <summary>REST Auto Grading: demo va bao cao Swagger (PRN232 G9).</summary>
[ApiController]
[Route("api/cms/grading")]
[ApiExplorerSettings(GroupName = "v1")]
[Configurations.Tags("CMS", "CMS_Grading")]
public class GradingController : ControllerBase
{
    private readonly IExamGradingAppService _grading;

    public GradingController(IExamGradingAppService grading)
    {
        _grading = grading;
    }

    [HttpGet("semesters")]
    [SwaggerOperation(Summary = "Danh sách học kỳ", OperationId = "Grading_ListSemesters", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSemesters(CancellationToken cancellationToken)
    {
        var r = await _grading.ListSemestersAsync(cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("exam-sessions")]
    [SwaggerOperation(Summary = "Danh sách ca thi", OperationId = "Grading_ListExamSessions", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListExamSessions([FromQuery] Guid? semesterId, CancellationToken cancellationToken)
    {
        var r = await _grading.ListExamSessionsAsync(semesterId, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("exam-sessions/{id:guid}")]
    [SwaggerOperation(Summary = "Chi tiết ca thi (topic / câu / testcase)", OperationId = "Grading_GetExamSession", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamSession([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _grading.GetExamSessionAsync(id, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("submissions")]
    [SwaggerOperation(Summary = "Danh sách bài nộp theo ca thi", OperationId = "Grading_ListSubmissions", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListSubmissions([FromQuery] Guid examSessionId, CancellationToken cancellationToken)
    {
        var r = await _grading.ListSubmissionsAsync(examSessionId, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("submissions/{id:guid}")]
    [SwaggerOperation(Summary = "Chi tiết bài nộp + điểm câu + testcase", OperationId = "Grading_GetSubmission", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmission([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _grading.GetSubmissionAsync(id, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("submissions")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Nộp 2 file zip (Q1, Q2) — stub chấm ngay sau upload",
        OperationId = "Grading_CreateSubmission",
        Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubmission(
        [FromForm] Guid examSessionId,
        [FromForm] string studentCode,
        [FromForm] string? studentName,
        IFormFile q1Zip,
        IFormFile q2Zip,
        CancellationToken cancellationToken)
    {
        if (q1Zip == null || q2Zip == null || q1Zip.Length == 0 || q2Zip.Length == 0)
            return BadRequest(Result<Guid>.Failure("Thiếu file zip.", ErrorCodeEnum.ValidationFailed));

        var r = await _grading.CreateSubmissionWithZipAsync(examSessionId, studentCode, studentName, q1Zip, q2Zip, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Flow 2 — Manual regrade (Admin upload lại file + trigger chấm thủ công)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPut("submissions/{id:guid}/files")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Admin thay file zip cho một câu (bypass EndsAtUtc)",
        Description = "Dùng khi SV gặp sự cố kỹ thuật và gửi bài qua mail. " +
                      "Không trigger chấm — gọi POST /regrade tiếp theo.",
        OperationId = "Grading_ReplaceSubmissionFile",
        Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceSubmissionFile(
        [FromRoute] Guid id,
        [FromForm] string questionLabel,
        IFormFile zipFile,
        CancellationToken cancellationToken)
    {
        if (zipFile == null || zipFile.Length == 0)
            return BadRequest(Result<bool>.Failure("Thiếu file zip.", ErrorCodeEnum.ValidationFailed));

        var r = await _grading.ReplaceSubmissionFileAsync(id, questionLabel, zipFile, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("submissions/{id:guid}/regrade")]
    [SwaggerOperation(
        Summary = "Trigger chấm lại thủ công cho một submission",
        Description = "Tạo GradingJob mới với Trigger=ManualRegrade và enqueue ngay. " +
                      "Nên gọi sau PUT /files để đảm bảo có file mới nhất.",
        OperationId = "Grading_TriggerRegrade",
        Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<TriggerRegradeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TriggerRegrade(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var r = await _grading.TriggerRegradeAsync(id, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }
}

