using MediatR;
using Microsoft.AspNetCore.Mvc;
using PRN232_G9_AutoGradingTool.API.Attributes;
using PRN232_G9_AutoGradingTool.Application.Common.DTOs.ExamGrading;
using PRN232_G9_AutoGradingTool.Application.Common.Enums;
using PRN232_G9_AutoGradingTool.Application.Common.Extensions;
using PRN232_G9_AutoGradingTool.Application.Common.Models;
using PRN232_G9_AutoGradingTool.Application.Features.ExamSessions.Commands.CreateExamSession;
using PRN232_G9_AutoGradingTool.Application.Features.Submissions.Commands.BatchSubmitZips;
using PRN232_G9_AutoGradingTool.Application.Features.ExamGrading;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace PRN232_G9_AutoGradingTool.API.Controllers.Cms;

/// <summary>REST Auto Grading: demo va bao cao Swagger (PRN232 G9).</summary>
[ApiController]
[Route("api/cms/grading")]
[ApiExplorerSettings(GroupName = "v1")]
[AuthorizeRoles(nameof(RoleEnum.Instructor), nameof(RoleEnum.SystemAdmin))]
[ProducesResponseType(typeof(Result<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(Result<object>), StatusCodes.Status403Forbidden)]
public class GradingController : ControllerBase
{
    private readonly IMediator _mediator;

    public GradingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("semesters")]
    [SwaggerOperation(Summary = "Danh sách học kỳ", OperationId = "Grading_ListSemesters")]
    [ProducesResponseType(typeof(Result<List<SemesterListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSemesters(CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgListSemestersQuery(), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("semesters")]
    [SwaggerOperation(Summary = "Tạo học kỳ", OperationId = "Grading_CreateSemester")]
    [ProducesResponseType(typeof(Result<SemesterListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgCreateSemesterCommand(body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("semesters/{id:guid}")]
    [SwaggerOperation(Summary = "Cập nhật học kỳ", OperationId = "Grading_UpdateSemester")]
    [ProducesResponseType(typeof(Result<SemesterListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSemester([FromRoute] Guid id, [FromBody] UpdateSemesterRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgUpdateSemesterCommand(id, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("semesters/{id:guid}")]
    [SwaggerOperation(Summary = "Xóa học kỳ (soft)", OperationId = "Grading_DeleteSemester")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSemester([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgDeleteSemesterCommand(id), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("exam-sessions")]
    [SwaggerOperation(Summary = "Danh sách ca thi", OperationId = "Grading_ListExamSessions")]
    [ProducesResponseType(typeof(Result<List<ExamSessionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListExamSessions([FromQuery] Guid? semesterId, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgListExamSessionsQuery(semesterId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-sessions")]
    [AuthorizeRoles()]
    [SwaggerOperation(Summary = "Tạo ca thi", OperationId = "Grading_CreateExamSession")]
    [ProducesResponseType(typeof(Result<ExamSessionListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExamSessionListItemDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<ExamSessionListItemDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateExamSession([FromBody] CreateExamSessionRequest body, CancellationToken cancellationToken)
    {
        var command = new CreateExamSessionCommand(
            body.SemesterId,
            body.Code,
            body.Title,
            body.StartsAtUtc,
            body.ExamDurationMinutes,
            body.EndsAtUtc,
            body.DeferredClassGrading);
        var r = await _mediator.Send(command, cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("exam-sessions/{id:guid}")]
    [SwaggerOperation(Summary = "Cập nhật ca thi", OperationId = "Grading_UpdateExamSession")]
    [ProducesResponseType(typeof(Result<ExamSessionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateExamSession([FromRoute] Guid id, [FromBody] UpdateExamSessionRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgUpdateExamSessionCommand(id, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-sessions/{id:guid}")]
    [SwaggerOperation(Summary = "Xóa ca thi + cấu trúc đề + pack (soft, khi chưa có bài nộp)", OperationId = "Grading_DeleteExamSession")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteExamSession([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgDeleteExamSessionCommand(id), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-sessions/{sessionId:guid}/topics")]
    [SwaggerOperation(Summary = "Tạo chủ đề trong ca thi", OperationId = "Grading_CreateTopic")]
    [ProducesResponseType(typeof(Result<ExamTopicDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTopic([FromRoute] Guid sessionId, [FromBody] CreateExamTopicRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgCreateTopicCommand(sessionId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("exam-topics/{topicId:guid}")]
    [SwaggerOperation(Summary = "Cập nhật chủ đề", OperationId = "Grading_UpdateTopic")]
    [ProducesResponseType(typeof(Result<ExamTopicDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTopic([FromRoute] Guid topicId, [FromBody] UpdateExamTopicRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgUpdateTopicCommand(topicId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-topics/{topicId:guid}")]
    [SwaggerOperation(Summary = "Xóa chủ đề (soft)", OperationId = "Grading_DeleteTopic")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTopic([FromRoute] Guid topicId, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgDeleteTopicCommand(topicId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-topics/{topicId:guid}/questions")]
    [SwaggerOperation(Summary = "Tạo câu hỏi", OperationId = "Grading_CreateQuestion")]
    [ProducesResponseType(typeof(Result<ExamQuestionDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateQuestion([FromRoute] Guid topicId, [FromBody] CreateExamQuestionRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgCreateQuestionCommand(topicId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("exam-questions/{questionId:guid}")]
    [SwaggerOperation(Summary = "Cập nhật câu hỏi", OperationId = "Grading_UpdateQuestion")]
    [ProducesResponseType(typeof(Result<ExamQuestionDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateQuestion([FromRoute] Guid questionId, [FromBody] UpdateExamQuestionRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgUpdateQuestionCommand(questionId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-questions/{questionId:guid}")]
    [SwaggerOperation(Summary = "Xóa câu hỏi (soft)", OperationId = "Grading_DeleteQuestion")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteQuestion([FromRoute] Guid questionId, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgDeleteQuestionCommand(questionId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-questions/{questionId:guid}/test-cases")]
    [SwaggerOperation(Summary = "Tạo testcase", OperationId = "Grading_CreateTestCase")]
    [ProducesResponseType(typeof(Result<ExamTestCaseDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTestCase([FromRoute] Guid questionId, [FromBody] CreateExamTestCaseRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgCreateTestCaseCommand(questionId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("exam-test-cases/{testCaseId:guid}")]
    [SwaggerOperation(Summary = "Cập nhật testcase", OperationId = "Grading_UpdateTestCase")]
    [ProducesResponseType(typeof(Result<ExamTestCaseDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTestCase([FromRoute] Guid testCaseId, [FromBody] UpdateExamTestCaseRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgUpdateTestCaseCommand(testCaseId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-test-cases/{testCaseId:guid}")]
    [SwaggerOperation(Summary = "Xóa testcase (soft)", OperationId = "Grading_DeleteTestCase")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTestCase([FromRoute] Guid testCaseId, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgDeleteTestCaseCommand(testCaseId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("exam-sessions/{sessionId:guid}/grading-packs")]
    [SwaggerOperation(Summary = "Danh sách grading pack theo ca", OperationId = "Grading_ListGradingPacks")]
    [ProducesResponseType(typeof(Result<List<ExamGradingPackListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListGradingPacks([FromRoute] Guid sessionId, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgListGradingPacksQuery(sessionId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-sessions/{sessionId:guid}/grading-packs")]
    [SwaggerOperation(Summary = "Tạo grading pack (version tự tăng nếu null/0)", OperationId = "Grading_CreateGradingPack")]
    [ProducesResponseType(typeof(Result<ExamGradingPackListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateGradingPack([FromRoute] Guid sessionId, [FromBody] CreateGradingPackRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgCreateGradingPackCommand(sessionId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("grading-packs/{packId:guid}")]
    [SwaggerOperation(Summary = "Cập nhật grading pack", OperationId = "Grading_UpdateGradingPack")]
    [ProducesResponseType(typeof(Result<ExamGradingPackListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGradingPack([FromRoute] Guid packId, [FromBody] UpdateGradingPackRequest body, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgUpdateGradingPackCommand(packId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("grading-packs/{packId:guid}")]
    [SwaggerOperation(Summary = "Xóa grading pack + asset files (soft)", OperationId = "Grading_DeleteGradingPack")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGradingPack([FromRoute] Guid packId, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgDeleteGradingPackCommand(packId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("grading-packs/{packId:guid}/assets")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Upload asset cho pack", OperationId = "Grading_CreatePackAsset")]
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

        var r = await _mediator.Send(new EgCreatePackAssetCommand(packId, (ExamPackAssetKind)kind, file), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("pack-assets/{assetId:guid}")]
    [SwaggerOperation(Summary = "Xóa pack asset (soft + xóa file)", OperationId = "Grading_DeletePackAsset")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePackAsset([FromRoute] Guid assetId, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgDeletePackAssetCommand(assetId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("exam-sessions/{id:guid}")]
    [SwaggerOperation(Summary = "Chi tiết ca thi (topic / câu / testcase)", OperationId = "Grading_GetExamSession")]
    [ProducesResponseType(typeof(Result<ExamSessionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExamSessionDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExamSession([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgGetExamSessionQuery(id), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("submissions")]
    [SwaggerOperation(Summary = "Danh sách bài nộp theo ca thi", OperationId = "Grading_ListSubmissions")]
    [ProducesResponseType(typeof(Result<List<ExamSubmissionListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<List<ExamSubmissionListItemDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListSubmissions(
        [FromQuery] Guid examSessionId,
        [FromQuery] Guid? examSessionClassId,
        CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgListSubmissionsQuery(examSessionId, examSessionClassId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("submissions/{id:guid}")]
    [SwaggerOperation(Summary = "Chi tiết bài nộp + điểm câu + testcase", OperationId = "Grading_GetSubmission")]
    [ProducesResponseType(typeof(Result<ExamSubmissionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExamSubmissionDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmission([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgGetSubmissionQuery(id), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-sessions/{sessionId:guid}/submissions/batch")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "[CMS] Nộp batch tối đa 50 SV — bypass khung giờ ca thi",
        Description = "CMS bypasses the exam window. Each entry must include ExamTopicId so files are stored under the correct topic path. Student flow uses POST api/student/grading/exam-sessions/{sessionId}/submissions/batch.",
        OperationId = "Grading_BatchCreateSubmissions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchCreateSubmissions(
        [FromRoute] Guid sessionId,
        [FromForm] BatchSubmitZipsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new BatchSubmitZipsCommand(sessionId, request, BypassExamWindow: true);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(result.GetHttpStatusCode(), result);
    }

    [HttpGet("semesters/{semesterId:guid}/exam-classes")]
    [SwaggerOperation(Summary = "Danh sách lớp trong học kỳ", OperationId = "Grading_ListExamClasses")]
    [ProducesResponseType(typeof(Result<List<ExamClassListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListExamClasses([FromRoute] Guid semesterId, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgListExamClassesQuery(semesterId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("semesters/{semesterId:guid}/exam-classes")]
    [SwaggerOperation(Summary = "Tạo lớp (SE1830, …)", OperationId = "Grading_CreateExamClass")]
    [ProducesResponseType(typeof(Result<ExamClassListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateExamClass(
        [FromRoute] Guid semesterId,
        [FromBody] CreateExamClassRequest body,
        CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgCreateExamClassCommand(semesterId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPut("exam-classes/{id:guid}")]
    [SwaggerOperation(Summary = "Cập nhật lớp", OperationId = "Grading_UpdateExamClass")]
    [ProducesResponseType(typeof(Result<ExamClassListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateExamClass(
        [FromRoute] Guid id,
        [FromBody] UpdateExamClassRequest body,
        CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgUpdateExamClassCommand(id, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-classes/{id:guid}")]
    [SwaggerOperation(Summary = "Xóa lớp (khi chưa gắn ca thi)", OperationId = "Grading_DeleteExamClass")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteExamClass([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgDeleteExamClassCommand(id), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpGet("exam-sessions/{sessionId:guid}/session-classes")]
    [SwaggerOperation(Summary = "Lớp tham gia ca thi + số bài Ready/Total", OperationId = "Grading_ListExamSessionClasses")]
    [ProducesResponseType(typeof(Result<List<ExamSessionClassListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListExamSessionClasses([FromRoute] Guid sessionId, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgListExamSessionClassesQuery(sessionId), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-sessions/{sessionId:guid}/session-classes")]
    [SwaggerOperation(Summary = "Gắn lớp vào ca (expectedStudentCount ≤ MaxStudents)", OperationId = "Grading_CreateExamSessionClass")]
    [ProducesResponseType(typeof(Result<ExamSessionClassListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateExamSessionClass(
        [FromRoute] Guid sessionId,
        [FromBody] CreateExamSessionClassRequest body,
        CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgCreateExamSessionClassCommand(sessionId, body), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpDelete("exam-session-classes/{id:guid}")]
    [SwaggerOperation(Summary = "Gỡ lớp khỏi ca (khi chưa có bài nộp)", OperationId = "Grading_DeleteExamSessionClass")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteExamSessionClass([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgDeleteExamSessionClassCommand(id), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("exam-session-classes/{id:guid}/start-batch-grading")]
    [SwaggerOperation(
        Summary = "Chấm batch theo lớp (stub tuần tự)",
        Description = "Yêu cầu ca thi bật deferredClassGrading. Mặc định cần đủ số bài Ready = expectedStudentCount.",
        OperationId = "Grading_StartClassBatchGrading")]
    [ProducesResponseType(typeof(Result<StartClassBatchGradingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartClassBatchGrading(
        [FromRoute] Guid id,
        [FromBody] StartClassBatchGradingRequest? body,
        CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(
            new EgStartClassBatchGradingCommand(id, body ?? new StartClassBatchGradingRequest()),
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
        OperationId = "Grading_ReplaceSubmissionFile")]
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

        var r = await _mediator.Send(new EgReplaceSubmissionFileCommand(id, questionLabel, zipFile), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }

    [HttpPost("submissions/{id:guid}/regrade")]
    [SwaggerOperation(
        Summary = "Trigger chấm lại thủ công cho một submission",
        Description = "Tạo GradingJob mới với Trigger=ManualRegrade và enqueue ngay. " +
                      "Nên gọi sau PUT /files để đảm bảo có file mới nhất.",
        OperationId = "Grading_TriggerRegrade")]
    [ProducesResponseType(typeof(Result<TriggerRegradeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TriggerRegrade(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var r = await _mediator.Send(new EgTriggerRegradeCommand(id), cancellationToken);
        return StatusCode(r.GetHttpStatusCode(), r);
    }
}

