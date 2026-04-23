# Plan: Grading Pipeline — Flow 1 (Auto SessionEnd) + Flow 2 (Manual Regrade) + Batch Upload Redesign

## Context

### Đã có sẵn (KHÔNG cần tạo/sửa):
- Entity: ExamSession (HangfireScheduleJobId), GradingJob (HangfireJobId, Trigger), GradingJobLog, GradingJobLogPhase enum, GradingJobLogLevel enum, GradingJobStatus, ExamSubmissionStatus
- ZipExtractionHelper: ExtractZip, DetectProjects, RunApp, RunNewman, CaptureProcessOutputAsync, CleanupResources, ParseNewmanTestResults — đều đã implement
- LocalFileService: GetFileContentAsync trả (byte[], ContentType) — đã implement
- Hangfire: AddHangfire + AddHangfireServer đã cấu hình, IBackgroundJobClient auto-registered
- IGradingProcessService, IGradingResultParser — đã registered trong DI (ZipExtractionHelper)
- ExamPackAssetKind.PostmanCollection = 2 — collection JSON lưu trên storage

### Cần tạo mới:
1. `Application/Common/DTOs/ExamGrading/BatchSubmitZipsDtos.cs`
2. `Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommand.cs`
3. `Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommandValidator.cs`
4. `Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommandHandler.cs`
5. `Infrastructure/Jobs/GradeSubmissionJob.cs`
6. `Infrastructure/Jobs/SummarizeExamResultJob.cs`

### Cần sửa:
1. `API/Controllers/Student/StudentGradingController.cs` — thay toàn bộ bằng endpoint batch mới, đổi inject từ `IExamGradingAppService` → `IMediator`
2. `API/Controllers/Cms/GradingController.cs` — thay `CreateSubmission` bằng `BatchCreateSubmissions`, thêm `IMediator`
3. `Application/Common/Interfaces/IExamGradingAppService.cs` — xóa `CreateSubmissionWithZipAsync`
4. `Infrastructure/Services/ExamGradingAppService.cs` — xóa `CreateSubmissionWithZipAsync` + `ApplyStubGradingAsync`, inject `IBackgroundJobClient`, enqueue trong `TriggerRegradeAsync`
5. `Infrastructure/Services/ExamGradingAdminService.cs` — inject `IBackgroundJobClient`, schedule/reschedule trong Create/UpdateExamSession
6. `Infrastructure/InfrastructureDependencyInjection.cs` — register 2 job classes

### KHÔNG cần migration — schema entity không thay đổi

### KHÔNG cần migration — schema entity không thay đổi

---

## Phase 0 — Batch Upload Redesign (vi phạm convention hiện tại cần fix trước)

### Vấn đề hiện tại
| Vi phạm | Chi tiết |
|---------|---------|
| Bypass MediatR | `StudentGradingController` inject `IExamGradingAppService` trực tiếp — sai convention |
| Single-student | 1 request = 1 SV; không đáp ứng yêu cầu batch |
| Storage path sai | `exam-submissions/{submissionId:N}/q1.zip` — không có cấu trúc session/topic/student |
| Stub grading inline | Upload và chấm stub trộn lẫn trong cùng method service |
| ExamSessionClass coupling | Upload flow kéo theo ExamSessionClass không cần thiết |

### Naming convention thư mục sinh viên
```
Format thư mục: {nameInitials}{studentCode} — viết thường
Ví dụ: namnhse161728
  nam  = tên (Nam)
  nh   = chữ cái đầu họ + tên đệm (Nguyễn Hoài)
  se161728 = mã sinh viên (SE161728)

Tên đầy đủ theo thứ tự Việt Nam: Nguyễn Hoài Nam
StudentCode trong DB: SE161728
```
> **Trong code**: dùng `studentCode` (SE161728) làm folder name chính để đảm bảo uniqueness.  
> Format `namnhse161728` là convention đặt tên folder thân thiện — build bằng cách lowercase `studentCode` ghép với name initials nếu `studentName` có sẵn, fallback về `studentCode.ToLowerInvariant()`.

### Storage path mới
```
uploads/
  {session.Code}/                         ← e.g. "SE1830-2024-2"
    {topic.Id:N}/                         ← GUID của ExamTopic đầu tiên trong session
      {studentFolderName}/                ← e.g. "namnhse161728" hoặc "se161728"
        Q1/
          solution.zip
        Q2/
          solution.zip
```
Q1 và Q2 của cùng một sinh viên nằm dưới **cùng một** `{topic.Id}` và `{studentFolderName}` — dễ dàng enumerate khi Hangfire pull file để chấm.

### Helper tính studentFolderName
```csharp
private static string BuildStudentFolderName(string studentCode, string? studentName)
{
    var code = studentCode.Trim().ToLowerInvariant();           // "se161728"
    if (string.IsNullOrWhiteSpace(studentName)) return code;

    // "Nguyễn Hoài Nam" → split by space → ["Nguyễn","Hoài","Nam"]
    // lấy chữ cái đầu của từng phần trừ phần cuối (tên), ghép với code
    // "Nguyễn" → 'n', "Hoài" → 'h' → initials = "nh"
    // tên = "Nam" → "nam"
    var parts = studentName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < 2) return code;

    var firstName = parts[^1].ToLowerInvariant();               // "nam"
    var initials = string.Concat(parts[..^1]
        .Select(p => char.ToLowerInvariant(p[0])));             // "nh"
    return $"{firstName}{initials}{code}";                      // "namnhse161728"
}
```

### Luồng request mới
```
POST /api/student/grading/exam-sessions/{sessionId}/submissions/batch
    → StudentGradingController.BatchSubmitZips()
    → _mediator.Send(new BatchSubmitZipsCommand(sessionId, request, bypassExamWindow: false))
    → ValidationBehavior (FluentValidation)
    → BatchSubmitZipsCommandHandler.Handle()
    → Result<BatchSubmitZipsResponseDto>

POST /api/cms/grading/exam-sessions/{sessionId}/submissions/batch   (bypass window)
    → GradingController.BatchCreateSubmissions()
    → _mediator.Send(new BatchSubmitZipsCommand(sessionId, request, bypassExamWindow: true))
    → BatchSubmitZipsCommandHandler.Handle()
```

### Multipart form binding
```
examSessionId: (lấy từ route)
entries[0].studentCode: "SE161728"
entries[0].studentName: "Nguyễn Hoài Nam"
entries[0].q1Zip: <file: solution.zip>
entries[0].q2Zip: <file: solution.zip>
entries[1].studentCode: "SE161729"
...
```
ASP.NET Core `[FromForm]` tự bind `List<StudentZipEntry>` với indexed key `entries[i].prop`.

---

### Step 0.1 — `Application/Common/DTOs/ExamGrading/BatchSubmitZipsDtos.cs`

```csharp
// === Request ===
public class StudentZipEntry
{
    public string StudentCode { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public IFormFile Q1Zip { get; set; } = null!;
    public IFormFile Q2Zip { get; set; } = null!;
}

public class BatchSubmitZipsRequest
{
    public List<StudentZipEntry> Entries { get; set; } = new();
}

// === Response ===
public record StudentSubmitResultItem(
    string StudentCode,
    bool Success,
    Guid? SubmissionId,
    string? Error);

public record BatchSubmitZipsResponseDto(
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<StudentSubmitResultItem> Results);
```

---

### Step 0.2 — `Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommand.cs`

```csharp
public record BatchSubmitZipsCommand(
    Guid ExamSessionId,
    BatchSubmitZipsRequest Request,
    bool BypassExamWindow
) : IRequest<Result<BatchSubmitZipsResponseDto>>;
```

---

### Step 0.3 — `BatchSubmitZipsCommandValidator.cs`

Rules:
- `ExamSessionId` — `NotEmpty()`
- `Entries` — `NotEmpty()` với message "Phải có ít nhất 1 bài nộp" + `Must(e => e.Count <= 5)` với message "Tối đa 5 sinh viên/request" + unique `StudentCode` trong batch
- Per entry (via `RuleForEach`): `StudentCode.NotEmpty()`, `Q1Zip not null + Must(IsZip)`, `Q2Zip not null + Must(IsZip)`
- `IsZip`: `entry.FileName.EndsWith(".zip", OrdinalIgnoreCase)` AND ContentType là `application/zip` hoặc `application/octet-stream`

---

### Step 0.4 — `BatchSubmitZipsCommandHandler.cs`

Constructor inject:
- `PRN232_G9_AutoGradingToolDbContext _db`
- `IFileServiceFactory _fileServiceFactory`
- `ILogger<BatchSubmitZipsCommandHandler> _logger`

`Handle(command, ct)`:
1. **Load session** — `Include(x => x.Topics.OrderBy(t => t.SortOrder))` → `NotFound` nếu null
2. **Window check** — nếu `!BypassExamWindow`: kiểm tra `now < StartsAtUtc` / `now > EndsAtUtc`
3. **Topics guard** — `session.Topics.Count < 1` → Failure `"Ca thi chưa có topic nào"`
4. `topic = topics[0]` (topic đầu tiên làm bucket chung); `fileService = _fileServiceFactory.CreateFileService()`
5. `results = new List<StudentSubmitResultItem>()`
6. **Per entry** (try/catch riêng — lỗi 1 SV không block SV khác):
   ```
   a. code = entry.StudentCode.Trim()
   b. Duplicate check: AnyAsync(x => x.ExamSessionId == id && x.StudentCode == code)
      → nếu trùng: results.Add(Failure "Sinh viên đã nộp"), continue
   c. pack = load active GradingPack (nullable, upload vẫn tiếp tục nếu null)
   d. submission = new ExamSubmission { ExamSessionId, StudentCode, StudentName,
        ExamGradingPackId = pack?.Id, WorkflowStatus = Pending,
        SubmittedAtUtc = now, CreatedAt = now, Status = Active }
   e. submission.Result = new TestResult { SubmissionId = submission.Id,
        TotalScore = 0, TestStatus = Pending, CreatedAt = now, Status = Active }
   f. folderName = BuildStudentFolderName(code, entry.StudentName)
      baseDir  = $"{session.Code}/{topic.Id:N}/{folderName}"
      q1SubDir = $"{baseDir}/Q1"
      q2SubDir = $"{baseDir}/Q2"
   g. p1 = await fileService.UploadFileAsync(entry.Q1Zip, "solution.zip", q1SubDir, ct)
      p2 = await fileService.UploadFileAsync(entry.Q2Zip, "solution.zip", q2SubDir, ct)
   h. files = [
        new ExamSubmissionFile { QuestionLabel="Q1", StorageRelativePath=p1, OriginalFileName=entry.Q1Zip.FileName },
        new ExamSubmissionFile { QuestionLabel="Q2", StorageRelativePath=p2, OriginalFileName=entry.Q2Zip.FileName }
      ]
   i. _db.Add(submission); _db.AddRange(files)
   j. await _db.SaveChangesAsync(ct)   ← per-student save để lỗi 1 SV không rollback batch
   k. results.Add(new StudentSubmitResultItem(code, true, submission.Id, null))
   ```
   Catch ex → results.Add(Failure item), log warning, rollback tracking nếu cần, **continue**

7. `dto = new BatchSubmitZipsResponseDto(results.Count(r=>r.Success), results.Count(r=>!r.Success), results)`
8. Return `Result<BatchSubmitZipsResponseDto>.Success(dto, "{successCount}/{total} bài nộp thành công")`

---

### Step 0.5 — `StudentGradingController.cs` (viết lại)

```csharp
[ApiController]
[Route("api/student/grading")]
public class StudentGradingController : ControllerBase
{
    private readonly IMediator _mediator;
    public StudentGradingController(IMediator mediator) { _mediator = mediator; }

    [HttpPost("exam-sessions/{sessionId:guid}/submissions/batch")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "[SV] Nộp batch tối đa 5 SV — mỗi SV 2 zip Q1+Q2",
        Description = "Kiểm tra StartsAtUtc ≤ now ≤ EndsAtUtc. Max 5 sinh viên/request.",
        OperationId = "Student_BatchSubmitZips")]
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
```

---

### Step 0.6 — `GradingController.cs` (patch)

- Constructor: thêm `IMediator _mediator`
- Xóa action `CreateSubmission` (POST submissions, line ~283), thay bằng:

```csharp
[HttpPost("exam-sessions/{sessionId:guid}/submissions/batch")]
[Consumes("multipart/form-data")]
[SwaggerOperation(
    Summary = "[CMS] Nộp hộ batch tối đa 5 SV — bypass khung giờ",
    OperationId = "Grading_BatchCreateSubmissions")]
public async Task<IActionResult> BatchCreateSubmissions(
    [FromRoute] Guid sessionId,
    [FromForm] BatchSubmitZipsRequest request,
    CancellationToken cancellationToken)
{
    var command = new BatchSubmitZipsCommand(sessionId, request, BypassExamWindow: true);
    var result = await _mediator.Send(command, cancellationToken);
    return StatusCode(result.GetHttpStatusCode(), result);
}
```

---

### Dependencies Phase 0:
```
0.1 (DTOs) → 0.2 (Command) → 0.3 (Validator) → 0.4 (Handler)
0.4 → 0.5 (StudentController)
0.4 → 0.6 (GradingController patch)
Sau khi 0.5 + 0.6 compile: xóa CreateSubmissionWithZipAsync khỏi interface + service
```

---

## Plan: Grading Pipeline — Flow 1 (SessionEnd) + Flow 2 (ManualRegrade)

TL;DR: Tạo 2 Hangfire job class (`GradeSubmissionJob` và `SummarizeExamResultJob`), wire `IBackgroundJobClient` vào admin/app service, thay thế stub grading bằng pipeline thực tế (zip → dotnet run → Newman → DB save).

---

## Phase 1 — Job Classes (2 file mới)

### Step 1: `Infrastructure/Jobs/GradeSubmissionJob.cs`

Constructor inject:
- `PRN232_G9_AutoGradingToolDbContext _db`
- `IGradingProcessService _processSvc`
- `IGradingResultParser _resultParser`
- `IFileServiceFactory _fileFactory`
- `ILogger<GradeSubmissionJob> _logger`

`ExecuteAsync(Guid gradingJobId, CancellationToken ct)`:
1. **LOAD**: Query GradingJob với includes: `Submission.SubmissionFiles`, `Pack.Assets`, `Submission.Result`
2. **MARK RUNNING**: job.JobStatus=Running, job.StartedAtUtc=now, submission.WorkflowStatus=Running → SaveChanges + Log(Extract, Info, "Starting...")
3. `tempDir = Path.Combine(Path.GetTempPath(), "autograde", gradingJobId.ToString("N"))` + `processes = List<(string label, Process, int port)>`
4. **try**:
   - **Phase Extract**: Per SubmissionFile → GetFileContentAsync → write bytes to `tempDir/raw/{label}.zip` → ExtractZip(rawZip, tempDir) → `extractedPaths[label] = path`. Log.
   - **Phase Discover**: Per extracted path → DetectProjects(extracted) → `appFolders["Q1"]=q1, appFolders["Q2"]=q2`. Throw if null. Log.
   - **Phase RunServer**: Per folder → GetFreePort() → RunApp(folder, port) → WaitForAppReadyAsync(port, ct, 30s) → processes.Add. Log.
   - **Phase RunNewman**: Load PostmanCollection asset → GetFileContentAsync → write to `tempDir/collection.json`. Per running app → RunNewman(collPath, `http://localhost:{port}`, tempDir) → CaptureProcessOutputAsync → `rawOutputs[label]=json`. Log with DetailJson.
   - **Phase Grade**: Load ExamTestCases từ DB (via session). Per label → ParseNewmanTestResults(rawJson) → match ResultDetail.TestName → ExamTestCase.Name → compute ExamQuestionScore → save ExamTestCaseScore, ExamQuestionScore → tổng hợp TotalScore → submission.TotalScore, WorkflowStatus=Completed, Result.TotalScore. Log.
   - job.JobStatus=Completed, job.FinishedAtUtc=now → SaveChanges
5. **catch**: log Error → job.JobStatus=Failed, job.ErrorMessage, submission.WorkflowStatus=Failed → SaveChanges
6. **finally**: CleanupResources per process + delete tempDir

Private helpers:
- `GetFreePort()` — TcpListener(Loopback, 0) → get port → stop
- `WaitForAppReadyAsync(port, ct, timeoutSec=30)` — HttpClient GET; retry on HttpRequestException, return on any HTTP response (kể cả 404 = server up); throw TimeoutException
- `AddLog(jobId, phase, level, msg, detail?)` — add GradingJobLog, SaveChanges per phase

### Step 2: `Infrastructure/Jobs/SummarizeExamResultJob.cs`

Constructor inject:
- `PRN232_G9_AutoGradingToolDbContext _db`
- `IBackgroundJobClient _client`
- `ILogger<SummarizeExamResultJob> _logger`

`ExecuteAsync(Guid examSessionId, CancellationToken ct)`:
1. Load ExamSession, if null return
2. **Idempotency**: if `DateTime.UtcNow < session.EndsAtUtc.AddSeconds(-30)` → log "Fired early, skipping" → return
3. Load `pendingSubmissions` (WorkflowStatus=Pending, ExamSessionId==id)
4. Load active GradingPack; if null → log warning, return
5. If no pending → log + return
6. **Transaction**:
   - Per submission: new GradingJob {Trigger=SessionEnd, Queued, ExamGradingPackId, ExamSubmissionId} + sub.WorkflowStatus=Queued → _db.Add
   - SaveChanges
   - Per job: `hangfireId = _client.Enqueue<GradeSubmissionJob>(x => x.ExecuteAsync(job.Id, CancellationToken.None))` + job.HangfireJobId=hangfireId
   - SaveChanges
   - Commit
7. Log "Enqueued {count} jobs for session {session.Code}"

---

## Phase 2 — Modify ExamGradingAdminService

File: `src/PRN232_G9_AutoGradingTool.Infrastructure/Services/ExamGradingAdminService.cs`

Changes:
- Constructor: add `IBackgroundJobClient _backgroundJobClient`
- `CreateExamSessionAsync` — sau SaveChanges:
  ```csharp
  var delay = entity.EndsAtUtc - DateTimeOffset.UtcNow;
  var jobId = _backgroundJobClient.Schedule<SummarizeExamResultJob>(x => x.ExecuteAsync(entity.Id, CancellationToken.None), delay);
  entity.HangfireScheduleJobId = jobId;
  await _db.SaveChangesAsync(cancellationToken);
  ```
- `UpdateExamSessionAsync` — trước SaveChanges, nếu EndsAtUtc thay đổi:
  ```csharp
  if (entity.HangfireScheduleJobId != null) BackgroundJob.Delete(entity.HangfireScheduleJobId);
  var delay = req.EndsAtUtc - DateTimeOffset.UtcNow;
  entity.HangfireScheduleJobId = _backgroundJobClient.Schedule<SummarizeExamResultJob>(..., delay);
  ```

---

## Phase 3 — Modify ExamGradingAppService

File: `src/PRN232_G9_AutoGradingTool.Infrastructure/Services/ExamGradingAppService.cs`

Changes:
- Constructor: add `IBackgroundJobClient _backgroundJobClient`
- `TriggerRegradeAsync` — sau SaveChanges (job đã được tạo):
  ```csharp
  var hangfireId = _backgroundJobClient.Enqueue<GradeSubmissionJob>(x => x.ExecuteAsync(job.Id, CancellationToken.None));
  job.HangfireJobId = hangfireId;
  await _db.SaveChangesAsync(cancellationToken);
  ```
  Xóa TODO comment và toàn bộ khối stub grading bên dưới.
- Xóa method `CreateSubmissionWithZipAsync` (đã migrate sang `BatchSubmitZipsCommandHandler` ở Phase 0)
- Xóa method `ApplyStubGradingAsync` (chỉ dùng bởi các stub đã xóa)
- Xóa `CreateSubmissionWithZipAsync` khỏi `IExamGradingAppService` interface

---

## Phase 4 — DI Registration

File: `src/PRN232_G9_AutoGradingTool.Infrastructure/InfrastructureDependencyInjection.cs`

Thêm sau dòng đăng ký IGradingResultParser (line ~151):
```csharp
services.AddScoped<GradeSubmissionJob>();
services.AddScoped<SummarizeExamResultJob>();
```

---

## Relevant Files

### Phase 0 — Batch Upload
- `Application/Common/DTOs/ExamGrading/BatchSubmitZipsDtos.cs` — TẠO MỚI
- `Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommand.cs` — TẠO MỚI
- `Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommandValidator.cs` — TẠO MỚI
- `Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommandHandler.cs` — TẠO MỚI
- `API/Controllers/Student/StudentGradingController.cs` — VIẾT LẠI
- `API/Controllers/Cms/GradingController.cs` — patch thêm IMediator + thay CreateSubmission
- `Application/Common/Interfaces/IExamGradingAppService.cs` — xóa CreateSubmissionWithZipAsync
- `Infrastructure/Services/ExamGradingAppService.cs` — xóa CreateSubmissionWithZipAsync + ApplyStubGradingAsync

### Phase 1-4 — Grading Pipeline
- `Infrastructure/Jobs/GradeSubmissionJob.cs` — TẠO MỚI
- `Infrastructure/Jobs/SummarizeExamResultJob.cs` — TẠO MỚI
- `Infrastructure/Services/ExamGradingAdminService.cs` — sửa Create/Update ExamSession
- `Infrastructure/Services/ExamGradingAppService.cs` — thêm IBackgroundJobClient, sửa TriggerRegrade
- `Infrastructure/InfrastructureDependencyInjection.cs` — đăng ký 2 job classes

---

## Dependencies giữa steps:
```
Phase 0 (Batch Upload) thực hiện trước Phase 1-4

Phase 0:
  0.1 DTOs → 0.2 Command → 0.3 Validator + 0.4 Handler (parallel)
  0.4 → 0.5 StudentController + 0.6 GradingController (parallel)
  Sau khi build xanh: xóa CreateSubmissionWithZipAsync

Phase 1-4 (sau Phase 0):
  P1 Step1 (GradeSubmissionJob) + P1 Step2 (SummarizeExamResultJob): parallel
  P2 (AdminService) phụ thuộc Step2 (SummarizeExamResultJob tồn tại)
  P3 (AppService) phụ thuộc Step1 (GradeSubmissionJob tồn tại)
  P4 (DI) phụ thuộc cả Step1 và Step2
```

---

## Verification

### Phase 0 — Batch Upload
1. Build thành công không lỗi
2. Swagger: `POST /api/student/grading/exam-sessions/{id}/submissions/batch` với 2 sinh viên → response `{ successCount: 2, failureCount: 0, results: [...] }`
3. Kiểm tra storage: `wwwroot/uploads/{session.Code}/{topicId}/namnhse161728/Q1/solution.zip` và `.../Q2/solution.zip` tồn tại cùng thư mục sinh viên
4. DB: `ExamSubmissions` có 2 row, `WorkflowStatus = Pending`, `ExamSubmissionFiles` có 4 row (Q1+Q2 × 2 SV)
5. Test edge cases: gửi 6 SV → validation error; gửi file .docx → validation error; gửi MSSV trùng → item fail nhưng SV khác vẫn thành công

### Phase 1-4 — Grading Pipeline
1. Build thành công: `dotnet build`
2. Swagger test Flow 1 (Auto SessionEnd):
   - `POST /exam-sessions` với `endsAtUtc = now+3min` → kiểm tra `hangfireScheduleJobId` trong response
   - `POST /submissions/batch` (2 SV) → WorkflowStatus=Pending
   - Chờ 3 phút → `GET /submissions` → WorkflowStatus: Pending→Queued→Running→Completed
   - Hangfire Dashboard `/hangfire` → xem SummarizeExamResultJob + GradeSubmissionJob
3. Swagger test Flow 2 (Manual Regrade):
   - `PUT /submissions/{id}/files` (thay zip)
   - `POST /submissions/{id}/regrade` → response có `gradingJobId` + status `Queued`
   - `GET /submissions/{id}` → theo dõi WorkflowStatus chuyển trạng thái
4. Hangfire Dashboard → xem GradeSubmissionJob logs per phase, retry nếu lỗi

---

## Scope Boundaries

- **IN**: Phase 0 — Batch upload redesign (CQRS, storage path, max 5 SV/request)
- **IN**: Flow 1 — SessionEnd auto grading via Hangfire
- **IN**: Flow 2 — ManualRegrade trigger
- **OUT**: `StartClassBatchGradingAsync` (DeferredClassGrading path) — không thay đổi
- **OUT**: Newman install verification — assume newman đã có trên PATH trong Docker
- **OUT**: ExamSessionClass trong upload flow — bỏ qua hoàn toàn ở Phase 0
- Storage: LocalStorage (`wwwroot/uploads`) — không thay đổi provider
