using Microsoft.AspNetCore.Mvc;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;
using PRN232_G9_AutoGradingTool.Application.Common.Interfaces;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Domain.Enums;
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
    private readonly IExamGradingAdminService _admin;

    public GradingController(IExamGradingAppService grading, IExamGradingAdminService admin)
    {
        _grading = grading;
        _admin = admin;
    }

    [HttpGet("semesters")]
    [SwaggerOperation(Summary = "Danh sách học kỳ", OperationId = "Grading_ListSemesters", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSemesters(CancellationToken cancellationToken)
    {
        var r = await _grading.ListSemestersAsync(cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("semesters")]
    [SwaggerOperation(Summary = "Tạo học kỳ", OperationId = "Grading_CreateSemester", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<SemesterListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.CreateSemesterAsync(body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("semesters/{id:guid}")]
    [SwaggerOperation(Summary = "Cập nhật học kỳ", OperationId = "Grading_UpdateSemester", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<SemesterListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSemester([FromRoute] Guid id, [FromBody] UpdateSemesterRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.UpdateSemesterAsync(id, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("semesters/{id:guid}")]
    [SwaggerOperation(Summary = "Xóa học kỳ (soft)", OperationId = "Grading_DeleteSemester", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSemester([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _admin.DeleteSemesterAsync(id, cancellationToken);
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

    [HttpPost("exam-sessions")]
    [SwaggerOperation(Summary = "Tạo ca thi", OperationId = "Grading_CreateExamSession", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamSessionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateExamSession([FromBody] CreateExamSessionRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.CreateExamSessionAsync(body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("exam-sessions/{id:guid}")]
    [SwaggerOperation(Summary = "Cập nhật ca thi", OperationId = "Grading_UpdateExamSession", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamSessionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateExamSession([FromRoute] Guid id, [FromBody] UpdateExamSessionRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.UpdateExamSessionAsync(id, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-sessions/{id:guid}")]
    [SwaggerOperation(Summary = "Xóa ca thi + cấu trúc đề + pack (soft, khi chưa có bài nộp)", OperationId = "Grading_DeleteExamSession", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteExamSession([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _admin.DeleteExamSessionAsync(id, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-sessions/{sessionId:guid}/topics")]
    [SwaggerOperation(Summary = "Tạo chủ đề trong ca thi", OperationId = "Grading_CreateTopic", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamTopicDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTopic([FromRoute] Guid sessionId, [FromBody] CreateExamTopicRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.CreateTopicAsync(sessionId, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("exam-topics/{topicId:guid}")]
    [SwaggerOperation(Summary = "Cập nhật chủ đề", OperationId = "Grading_UpdateTopic", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamTopicDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTopic([FromRoute] Guid topicId, [FromBody] UpdateExamTopicRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.UpdateTopicAsync(topicId, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-topics/{topicId:guid}")]
    [SwaggerOperation(Summary = "Xóa chủ đề (soft)", OperationId = "Grading_DeleteTopic", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTopic([FromRoute] Guid topicId, CancellationToken cancellationToken)
    {
        var r = await _admin.DeleteTopicAsync(topicId, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-topics/{topicId:guid}/questions")]
    [SwaggerOperation(Summary = "Tạo câu hỏi", OperationId = "Grading_CreateQuestion", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamQuestionDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateQuestion([FromRoute] Guid topicId, [FromBody] CreateExamQuestionRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.CreateQuestionAsync(topicId, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("exam-questions/{questionId:guid}")]
    [SwaggerOperation(Summary = "Cập nhật câu hỏi", OperationId = "Grading_UpdateQuestion", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamQuestionDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateQuestion([FromRoute] Guid questionId, [FromBody] UpdateExamQuestionRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.UpdateQuestionAsync(questionId, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-questions/{questionId:guid}")]
    [SwaggerOperation(Summary = "Xóa câu hỏi (soft)", OperationId = "Grading_DeleteQuestion", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteQuestion([FromRoute] Guid questionId, CancellationToken cancellationToken)
    {
        var r = await _admin.DeleteQuestionAsync(questionId, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-questions/{questionId:guid}/test-cases")]
    [SwaggerOperation(Summary = "Tạo testcase", OperationId = "Grading_CreateTestCase", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamTestCaseDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTestCase([FromRoute] Guid questionId, [FromBody] CreateExamTestCaseRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.CreateTestCaseAsync(questionId, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("exam-test-cases/{testCaseId:guid}")]
    [SwaggerOperation(Summary = "Cập nhật testcase", OperationId = "Grading_UpdateTestCase", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamTestCaseDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTestCase([FromRoute] Guid testCaseId, [FromBody] UpdateExamTestCaseRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.UpdateTestCaseAsync(testCaseId, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-test-cases/{testCaseId:guid}")]
    [SwaggerOperation(Summary = "Xóa testcase (soft)", OperationId = "Grading_DeleteTestCase", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTestCase([FromRoute] Guid testCaseId, CancellationToken cancellationToken)
    {
        var r = await _admin.DeleteTestCaseAsync(testCaseId, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("exam-sessions/{sessionId:guid}/grading-packs")]
    [SwaggerOperation(Summary = "Danh sách grading pack theo ca", OperationId = "Grading_ListGradingPacks", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<List<ExamGradingPackListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListGradingPacks([FromRoute] Guid sessionId, CancellationToken cancellationToken)
    {
        var r = await _admin.ListGradingPacksAsync(sessionId, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-sessions/{sessionId:guid}/grading-packs")]
    [SwaggerOperation(Summary = "Tạo grading pack (version tự tăng nếu null/0)", OperationId = "Grading_CreateGradingPack", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamGradingPackListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateGradingPack([FromRoute] Guid sessionId, [FromBody] CreateGradingPackRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.CreateGradingPackAsync(sessionId, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("grading-packs/{packId:guid}")]
    [SwaggerOperation(Summary = "Cập nhật grading pack", OperationId = "Grading_UpdateGradingPack", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamGradingPackListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGradingPack([FromRoute] Guid packId, [FromBody] UpdateGradingPackRequest body, CancellationToken cancellationToken)
    {
        var r = await _admin.UpdateGradingPackAsync(packId, body, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("grading-packs/{packId:guid}")]
    [SwaggerOperation(Summary = "Xóa grading pack + asset files (soft)", OperationId = "Grading_DeleteGradingPack", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGradingPack([FromRoute] Guid packId, CancellationToken cancellationToken)
    {
        var r = await _admin.DeleteGradingPackAsync(packId, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("grading-packs/{packId:guid}/assets")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Upload asset cho pack", OperationId = "Grading_CreatePackAsset", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<ExamPackAssetListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePackAsset(
        [FromRoute] Guid packId,
        [FromForm] int kind,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(ExamPackAssetKind), kind))
            return BadRequest(Result<ExamPackAssetListItemDto>.Failure("kind không hợp lệ.", ErrorCodeEnum.ValidationFailed));

        if (file == null || file.Length == 0)
            return BadRequest(Result<ExamPackAssetListItemDto>.Failure("Thiếu file.", ErrorCodeEnum.ValidationFailed));

        var r = await _admin.CreatePackAssetAsync(packId, (ExamPackAssetKind)kind, file, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("pack-assets/{assetId:guid}")]
    [SwaggerOperation(Summary = "Xóa pack asset (soft + xóa file)", OperationId = "Grading_DeletePackAsset", Tags = new[] { "CMS_Grading" })]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePackAsset([FromRoute] Guid assetId, CancellationToken cancellationToken)
    {
        var r = await _admin.DeletePackAssetAsync(assetId, cancellationToken);
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
        Description = "CMS bypass khung giờ ca thi (nộp hộ / xử lý muộn). Luồng SV dùng POST api/student/grading/submissions — có kiểm tra StartsAtUtc/EndsAtUtc.",
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

        var r = await _grading.CreateSubmissionWithZipAsync(
            examSessionId,
            studentCode,
            studentName,
            q1Zip,
            q2Zip,
            bypassExamWindow: true,
            cancellationToken);
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

