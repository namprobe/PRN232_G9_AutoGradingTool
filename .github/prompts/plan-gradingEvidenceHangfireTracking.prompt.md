# Plan: Domain Entity Changes — Grading Evidence & Hangfire Tracking

## TL;DR
Mở rộng domain model để lưu evidence chấm điểm (build log, Newman output), liên kết Hangfire job ID, và track retry. Các thay đổi tập trung ở Domain, Infrastructure config/migration, không động đến Application/API.

---

## ERD
``mermaid
erDiagram
    Semester ||--o{ ExamSession : "has"
    ExamSession ||--o{ ExamTopic : "has"
    ExamSession ||--o{ ExamGradingPack : "has"
    ExamSession ||--o{ ExamSubmission : "receives"
    ExamTopic ||--o{ ExamQuestion : "has"
    ExamQuestion ||--o{ ExamTestCase : "has"
    ExamGradingPack ||--o{ ExamPackAsset : "contains"
    ExamGradingPack ||--o{ GradingTestDefinition : "defines"
    ExamTestCase ||--o{ GradingTestDefinition : "linked to"
    ExamSubmission ||--o{ GradingJob : "triggers"
    ExamSubmission ||--o{ ExamQuestionScore : "scored by"
    ExamSubmission ||--o{ ExamTestCaseScore : "scored by"
    GradingJob ||--o{ GradingJobLog : "produces"
    ExamGradingPack ||--o{ GradingJob : "used by"

    ExamPackAsset {
        enum Kind "PostmanCollection|NewmanEnvironment|Documentation"
        string StorageRelativePath
    }
    GradingTestDefinition {
        enum Kind "HttpRequest|NewmanCollection|Stub"
        string PayloadJson "runner config"
        int SortOrder
    }
    GradingJob {
        string HangfireJobId
        enum JobStatus "Queued|Running|Completed|Failed"
        int RetryCount
    }
    GradingJobLog {
        enum Phase "Extract|Discover|RunServer|RunNewman|Grade|Cleanup"
        enum Level "Info|Warning|Error"
        string Message
        string DetailJson "stdout/stderr/Newman JSON"
    }
    ExamTestCaseScore {
        enum Outcome "Pending|Pass|Fail|Error"
        decimal PointsEarned
        string RawOutputJson "Newman assertion detail"
    }

## Sequence
sequenceDiagram
    actor Lecturer
    actor Student
    participant API
    participant Storage
    participant Hangfire
    participant GradingWorker
    participant StudentServer as StudentApp<br/>(localhost:500x)
    participant Newman
    participant DB

    %% Setup phase (Lecturer)
    rect rgb(230, 240, 255)
        Note over Lecturer,DB: SETUP — Lecturer tạo đề & pack
        Lecturer->>API: POST /exam-sessions (tạo ca thi)
        Lecturer->>API: POST /grading-packs (tạo pack v1)
        Lecturer->>API: POST /pack-assets/upload (upload .postman_collection.json)
        API->>Storage: lưu file collection
        Lecturer->>API: POST /test-definitions (link TestCase → Collection)
        API->>DB: ExamGradingPack(IsActive=true)<br/>ExamPackAsset, GradingTestDefinition
    end

    %% Submission phase (Student)
    rect rgb(230, 255, 230)
        Note over Student,DB: SUBMIT — Student nộp bài
        Note over Student: Sinh viên chạy:<br/>dotnet publish -c Release -o ./Q1_[studentCode]<br/>rồi nén toàn bộ project root thành Q1.zip<br/>(Q[n]_[studentCode]/ là thư mục publish bên trong)
        Student->>API: POST /submissions/upload<br/>(Q1.zip + Q2.zip)
        API->>Storage: lưu ExamSubmissionFile(Q1, Q2)<br/>StorageRelativePath trỏ đến file zip
        API->>DB: ExamSubmission(WorkflowStatus=Pending)
        API->>Hangfire: BackgroundJob.Enqueue<GradeSubmissionJob>(submissionId)
        API->>DB: GradingJob(Status=Queued, HangfireJobId="123")
        API-->>Student: 202 Accepted
    end

    %% Grading phase (background)
    rect rgb(255, 245, 220)
        Note over Hangfire,DB: GRADING — Background worker chấm
        Hangfire->>GradingWorker: dequeue job #123

        GradingWorker->>DB: GradingJob(Status=Running)

        %% Phase: Extract
        GradingWorker->>Storage: download Q1.zip, Q2.zip
        GradingWorker->>GradingWorker: unzip vào temp dir
        GradingWorker->>DB: GradingJobLog(Phase=Extract, Level=Info)

        %% Phase: Discover (tìm published output)
        GradingWorker->>GradingWorker: tìm thư mục Q[n]_* trong zip đã giải nén<br/>(pattern: QuestionLabel + "_" + StudentCode)
        alt Không tìm thấy thư mục publish hoặc DLL
            GradingWorker->>DB: GradingJobLog(Phase=Discover, Level=Error,<br/>Message="Published folder not found")
            GradingWorker->>DB: ExamTestCaseScore(Outcome=Error) cho tất cả TC
            GradingWorker->>DB: GradingJob(Status=Failed)
            Note over GradingWorker: Hangfire retry (tối đa 3 lần)<br/>RetryCount++
        else Tìm thấy published folder
            GradingWorker->>DB: GradingJobLog(Phase=Discover, Level=Info,<br/>Message="Found Q1_xxx/App.dll")
        end

        %% Phase: RunServer
        GradingWorker->>StudentServer: dotnet Q[n]_[studentCode]/App.dll<br/>--urls=http://localhost:500x
        GradingWorker->>StudentServer: health check (curl /health hoặc nc)
        GradingWorker->>DB: GradingJobLog(Phase=RunServer, Level=Info)

        %% Phase: RunNewman (mỗi TestCase)
        loop mỗi GradingTestDefinition (theo SortOrder)
            GradingWorker->>Newman: newman run collection.json<br/>--env-var baseUrl=localhost:500x<br/>--reporters json
            Newman->>StudentServer: HTTP requests (GET/POST/PUT/DELETE)
            StudentServer-->>Newman: responses
            Newman-->>GradingWorker: JSON report<br/>(assertions passed/failed)
            GradingWorker->>DB: GradingJobLog(Phase=RunNewman, DetailJson=newman_report)
            GradingWorker->>DB: ExamTestCaseScore(<br/>  Outcome=Pass/Fail,<br/>  PointsEarned,<br/>  RawOutputJson=assertions<br/>)
        end

        %% Phase: Grade (tổng hợp điểm)
        GradingWorker->>DB: ExamQuestionScore (tổng TestCase của Q1, Q2)
        GradingWorker->>DB: ExamSubmission(TotalScore, WorkflowStatus=Completed)
        GradingWorker->>DB: GradingJob(Status=Completed, FinishedAtUtc)
        GradingWorker->>DB: GradingJobLog(Phase=Grade, Level=Info)

        %% Cleanup
        GradingWorker->>GradingWorker: xóa temp dir
        GradingWorker->>DB: GradingJobLog(Phase=Cleanup)
    end

    %% View results
    rect rgb(240, 230, 255)
        Note over Lecturer,DB: REVIEW — Xem kết quả
        Lecturer->>API: GET /submissions/{id}
        API-->>Lecturer: TotalScore + QuestionScores<br/>+ TestCaseScores + Logs
    end

## Steps

### Phase 1 — Enum mới (không có dependency)
1. Tạo `Domain/Enums/GradingJobLogPhase.cs` — Extract, **Discover**, RunServer, RunNewman, Grade, Cleanup
   > **Discover** thay thế Build: sinh viên đã publish trước, worker chỉ cần tìm thư mục `Q[n]_[studentCode]/` và locate DLL.
2. Tạo `Domain/Enums/GradingJobLogLevel.cs` — Info, Warning, Error

### Phase 2 — Entity mới + sửa entity hiện có (depends on Phase 1)
3. Tạo `Domain/Entities/GradingJobLog.cs` — FK GradingJobId, Phase, Level, Message, DetailJson (text), OccurredAtUtc
4. Tạo `Domain/Entities/ExamSubmissionFile.cs` — FK ExamSubmissionId, QuestionLabel (e.g. "Q1"), StorageRelativePath, OriginalFileName
5. Sửa `Domain/Entities/ExamSubmission.cs`:
   - Xóa `Q1ZipRelativePath`, `Q2ZipRelativePath`
   - Thêm navigation `ICollection<ExamSubmissionFile> SubmissionFiles`
6. Sửa `Domain/Entities/GradingJob.cs`:
   - Thêm `HangfireJobId` (string?, nullable) — Hangfire's string job ID để gọi lại/cancel
   - Thêm `RetryCount` (int, default 0)
   - Thêm navigation `ICollection<GradingJobLog> Logs`
7. Sửa `Domain/Entities/ExamTestCaseScore.cs`:
   - Thêm `RawOutputJson` (string?, nullable) — raw Newman assertion JSON

### Phase 3 — EF Config + DbContext (depends on Phase 2)
8. Sửa `Infrastructure/Configurations/ExamGradingModelConfiguration.cs`:
   - Configure bảng `grading_job_logs` cho GradingJobLog
   - Configure bảng `exam_submission_files` cho ExamSubmissionFile, index `(ExamSubmissionId, QuestionLabel)` unique
   - Index trên `(GradingJobId, Phase)` và `(GradingJobId, OccurredAtUtc)`
   - Thêm `HasColumnType("text")` cho DetailJson (có thể lớn)
   - Map HangfireJobId, RetryCount trên grading_jobs
9. Sửa `Infrastructure/Context/PRN232_G9_AutoGradingToolDbContext.cs`:
   - Thêm `DbSet<GradingJobLog> GradingJobLogs`
   - Thêm `DbSet<ExamSubmissionFile> ExamSubmissionFiles`

### Phase 4 — Migration (depends on Phase 3)
10. Chạy `dotnet ef migrations add GradingJobLogAndEvidence --context PRN232_G9_AutoGradingToolDbContext --project Infrastructure --startup-project API`

---

## Relevant files
- `src/PRN232_G9_AutoGradingTool.Domain/Entities/GradingJob.cs`
- `src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamSubmission.cs`
- `src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamTestCaseScore.cs`
- `src/PRN232_G9_AutoGradingTool.Domain/Enums/` (2 file mới)
- `src/PRN232_G9_AutoGradingTool.Domain/Entities/GradingJobLog.cs` (mới)
- `src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamSubmissionFile.cs` (mới)
- `src/PRN232_G9_AutoGradingTool.Infrastructure/Configurations/ExamGradingModelConfiguration.cs`
- `src/PRN232_G9_AutoGradingTool.Infrastructure/Context/PRN232_G9_AutoGradingToolDbContext.cs`
- `src/PRN232_G9_AutoGradingTool.Infrastructure/Migrations/PRN232_G9_AutoGradingTool/` (migration mới)

## Decisions
- Q1/Q2 hardcode **thay bằng `ExamSubmissionFile`** (1-n) — linh hoạt với bất kỳ số câu nào
- BaseEntity pattern: GradingJobLog, ExamSubmissionFile đều kế thừa BaseEntity
- HangfireJobId: string? vì Hangfire dùng string ID (không phải Guid)
- DetailJson: column type "text" thay vì varchar vì build log có thể rất dài
- OuterDb (Hangfire DB) không thay đổi
