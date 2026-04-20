# Các luồng chính — PRN232 Auto Grading Tool

> Tài liệu này mô tả **ba luồng nghiệp vụ chính** theo thứ tự thời gian thực tế:  
> 1. Proctor chuẩn bị ca thi  
> 2. Sinh viên nộp bài (qua hệ thống nộp bài riêng)  
> 3. Hệ thống tự chấm sau khi ca thi kết thúc  

---

## Tổng quan hệ thống

```
┌─────────────────────┐     ┌────────────────────────────┐
│  Hệ thống nộp bài   │     │  Auto Grading Tool (dự án) │
│  (của trường/FPT)   │     │                            │
│                     │     │  ┌─────────┐  ┌─────────┐  │
│  SV nộp zip qua     │────▶│  │ Storage │  │Postgres │  │
│  portal thi         │     │  └────┬────┘  └────┬────┘  │
│  (ngoài scope dự án)│     │       │             │       │
└─────────────────────┘     │  ┌────▼─────────────▼────┐  │
                             │  │   API + Hangfire Worker│  │
                             │  └───────────────────────┘  │
                             └────────────────────────────┘
```

**Lưu ý phạm vi:** Dự án này là **hệ thống chấm bài**, không phải hệ thống nộp bài.  
- Sinh viên nộp bài qua portal thi của FPT (hoặc một ứng dụng giả lập).  
- Auto Grading Tool nhận file zip đã có sẵn trên storage, rồi chạy pipeline chấm.

---

## Luồng 1 — Proctor chuẩn bị ca thi

**Actor:** Giảng viên / Proctor  
**Kết quả:** Ca thi sẵn sàng, GradingPack active, hệ thống biết giờ bắt đầu/kết thúc.

```
Proctor
  │
  ├─① Tạo ExamSession
  │     POST /api/cms/grading/exam-sessions
  │     {
  │       semesterId, code, title,
  │       startsAtUtc:      "2026-05-26T08:00:00Z",  ← giờ SV bắt đầu làm
  │       examDurationMinutes: 90,                   ← thời lượng làm bài (hiển thị cho SV)
  │       endsAtUtc:        "2026-05-26T09:50:00Z"   ← giờ ĐÓNG nộp (proctor tự set,
  │     }                                               có thể gia hạn khi SV gặp lỗi)
  │
  ├─② Tạo ExamTopic + ExamQuestion + ExamTestCase
  │     Mỗi câu (Q1, Q2, ...) có nhiều TestCase với MaxPoints
  │
  ├─③ Upload GradingPack (bộ chấm)
  │     Gồm: Newman collection JSON, GivenAPI binary (nếu cần), script môi trường
  │     POST /api/cms/grading/exam-sessions/{id}/grading-packs
  │     → Hệ thống tạo ExamGradingPack với IsActive=true
  │
  └─④ (Tuỳ chọn) Proctor postpone ca thi → PATCH endsAtUtc
        Khi SV báo server lỗi, proctor gia hạn EndsAtUtc
        → Hangfire job SummarizeExamResultJob được reschedule tự động
```

**Database sau bước này:**

| Bảng | Trạng thái |
|------|-----------|
| `exam_sessions` | 1 row, `starts_at_utc`, `exam_duration_minutes`, `ends_at_utc` |
| `exam_topics` + `exam_questions` + `exam_test_cases` | cây đề thi |
| `exam_grading_packs` + `exam_pack_assets` | bộ chấm `IsActive=true` |

---

## Luồng 2 — Sinh viên nộp bài

**Actor:** Sinh viên (qua hệ thống nộp bài FPT / app giả lập)  
**Kết quả:** File zip được lưu storage, bản ghi `ExamSubmission` + `ExamSubmissionFile` ở DB với `WorkflowStatus=Pending`.

### 2a. Luồng thực tế (Production)

```
SV
  │
  ├─① Publish dự án TRƯỚC khi nộp
  │     dotnet publish -c Release -o ./Q1_SE123456
  │     dotnet publish -c Release -o ./Q2_SE123456
  │     zip toàn bộ thư mục → Q1_SE123456.zip, Q2_SE123456.zip
  │
  ├─② Nộp qua portal thi FPT
  │     Portal lưu zip vào shared storage (NFS / S3-compatible / SFTP)
  │     Cấu trúc storage:
  │       uploads/
  │         {examSessionId}/
  │           {studentCode}/
  │             Q1_SE123456.zip
  │             Q2_SE123456.zip
  │
  └─③ (Ngoài scope dự án — hệ thống nộp bài tự xử lý)
```

### 2b. Luồng giả lập (Demo / Testing)

Vì hệ thống nộp bài FPT không có sẵn, **CMS API cung cấp endpoint giả lập nộp bài**:

```
POST /api/cms/grading/submissions          ← multipart/form-data
{
  examSessionId: "b1000000-...-0002",
  studentCode:   "SE123456",
  studentName:   "Nguyen Van A",           ← optional
  q1Zip: [file: Q1_SE123456.zip],
  q2Zip: [file: Q2_SE123456.zip]
}
```

**Xử lý phía server:**

```
API nhận request
  │
  ├─① Validate: examSession tồn tại, còn trong thời gian nộp (< EndsAtUtc)
  │
  ├─② Lưu file vào storage
  │     uploads/{examSessionId}/{studentCode}/Q1_SE123456.zip
  │     uploads/{examSessionId}/{studentCode}/Q2_SE123456.zip
  │
  ├─③ Upsert ExamSubmission
  │     Nếu SV đã nộp trước → cập nhật bản ghi cũ (hệ thống giữ bài nộp cuối)
  │     WorkflowStatus = Pending  ← KHÔNG chấm ngay
  │     SubmittedAtUtc = now
  │
  ├─④ Xoá ExamSubmissionFile cũ (nếu upsert), tạo mới
  │     exam_submission_files:
  │       { QuestionLabel="Q1", StorageRelativePath="...", OriginalFileName="Q1_SE123456.zip" }
  │       { QuestionLabel="Q2", StorageRelativePath="...", OriginalFileName="Q2_SE123456.zip" }
  │
  └─⑤ Response 200 OK — KHÔNG enqueue Hangfire lúc này
        SV có thể nộp lại nhiều lần, hệ thống chỉ giữ bài nộp cuối cùng
```

**Rule quan trọng:** "Nộp lại" = ghi đè, không tạo submission mới.  
Chấm theo bài nộp **cuối cùng** trước `EndsAtUtc`.

---

## Luồng 3 — Chấm bài tự động (sau khi ca thi kết thúc)

**Actor:** Hangfire Worker (background, không có người kích hoạt thủ công)  
**Trigger:** `EndsAtUtc` của ca thi  
**Kết quả:** Mỗi submission có `WorkflowStatus=Completed/Failed`, điểm ghi vào `exam_question_scores` và `exam_test_case_scores`.

### 3a. Schedule Job lúc tạo ca thi

```
Khi ExamSession được tạo (hoặc EndsAtUtc được cập nhật):
  │
  └─ Hangfire.Schedule<SummarizeExamResultJob>(
         x => x.ExecuteAsync(examSessionId),
         delay = EndsAtUtc - DateTime.UtcNow
     )
     → jobId lưu vào exam_sessions.summarize_hangfire_job_id (để reschedule khi gia hạn)
```

### 3b. SummarizeExamResultJob — Chốt danh sách và enqueue chấm

```
SummarizeExamResultJob(examSessionId)
  │
  ├─① Kiểm tra EndsAtUtc: nếu proctor đã gia hạn → job này đã bị reschedule,
  │     không chạy nữa (idempotency check)
  │
  ├─② Load tất cả ExamSubmission của session có WorkflowStatus=Pending
  │     (đây là bài nộp cuối của từng SV — do upsert đảm bảo)
  │
  ├─③ Load ExamGradingPack đang IsActive
  │
  ├─④ Với mỗi submission:
  │     a. Tạo GradingJob { JobStatus=Queued, ExamGradingPackId=activePackId }
  │     b. BackgroundJob.Enqueue<GradeSubmissionJob>(x => x.ExecuteAsync(gradingJobId))
  │     c. Cập nhật GradingJob.HangfireJobId = jobId trả về
  │     d. Cập nhật submission.WorkflowStatus = Queued
  │
  └─⑤ Commit transaction
```

### 3c. GradeSubmissionJob — Pipeline chấm 1 bài

```
GradeSubmissionJob(gradingJobId)
  │
  ├─── Phase: EXTRACT
  │     Load ExamSubmissionFile[] từ DB (Q1, Q2, ...)
  │     Kéo file zip từ storage về thư mục tạm:
  │       temp/{gradingJobId}/raw/Q1_SE123456.zip
  │       temp/{gradingJobId}/raw/Q2_SE123456.zip
  │     Giải nén từng zip:
  │       temp/{gradingJobId}/Q1/  (chứa Q1_SE123456/ từ dotnet publish)
  │       temp/{gradingJobId}/Q2/
  │     Log: GradingJobLog { Phase=Extract, Level=Info, Message="Extracted 2 files" }
  │
  ├─── Phase: DISCOVER
  │     Tìm thư mục publish trong mỗi câu:
  │       Pattern: Q[n]_[studentCode]/   (ví dụ: Q1_SE123456/)
  │       Tìm file DLL khởi động: Q1_SE123456/App.dll (hoặc *.dll có cùng tên thư mục)
  │     Nếu không tìm thấy → Log Error, Phase thất bại → GradingJob.JobStatus=Failed
  │     Log: GradingJobLog { Phase=Discover, Level=Info, Message="Found Q1: Q1_SE123456/App.dll" }
  │
  ├─── Phase: RUN SERVER  (lặp cho từng câu Q1, Q2, ...)
  │     Khởi động process: dotnet Q1_SE123456/App.dll --urls http://localhost:{port}
  │     Chờ server ready (health check hoặc timeout 10s)
  │     Log: GradingJobLog { Phase=RunServer, Level=Info, Message="Q1 server up at :5001" }
  │
  ├─── Phase: RUN NEWMAN  (lặp cho từng câu)
  │     Load Newman collection từ ExamPackAsset (file JSON)
  │     Chạy: newman run collection.json --env-var baseUrl=http://localhost:{port}
  │     Thu thập kết quả từng test → JSON output
  │     Log: GradingJobLog { Phase=RunNewman, Level=Info, RawOutputJson="{...}" }
  │
  ├─── Phase: GRADE
  │     Parse Newman output, map sang ExamTestCase theo tên
  │     Insert ExamTestCaseScore { IsPass, ActualOutput, RawOutputJson }
  │     Tổng hợp ExamQuestionScore { Score = Σ passed_testcase_points }
  │     Tính TotalScore = Σ QuestionScores
  │     Cập nhật ExamSubmission.TotalScore, WorkflowStatus=Completed
  │     Log: GradingJobLog { Phase=Grade, Level=Info, Message="Q1: 3/4 passed, 3.75đ" }
  │
  └─── Phase: CLEANUP  (always — dù thành công hay thất bại)
        Xoá thư mục temp/{gradingJobId}/
        Kill server process nếu còn sống
        Cập nhật GradingJob.FinishedAtUtc, JobStatus=Completed/Failed
        Log: GradingJobLog { Phase=Cleanup, Level=Info, Message="Temp dir removed" }
```

### 3d. Xử lý lỗi & Retry

```
Hangfire retry policy (3 lần):
  Attempt 1 → thất bại → chờ 60s  → Attempt 2
  Attempt 2 → thất bại → chờ 5m   → Attempt 3
  Attempt 3 → thất bại → chờ 10m  → Dead (Hangfire Dashboard)

Mỗi lần retry:
  GradingJob.RetryCount++
  GradingJob.ErrorMessage = exception.Message
  GradingJob.JobStatus = Failed (cuối cùng)
```

---

## ERD tóm tắt (các bảng liên quan)

```
exam_sessions
  id, code, title
  starts_at_utc       ← giờ SV bắt đầu làm
  exam_duration_minutes ← thời lượng quy định (90p)
  ends_at_utc         ← giờ đóng nộp (proctor set, có thể gia hạn)
  │
  ├──▶ exam_topics ──▶ exam_questions ──▶ exam_test_cases
  │
  ├──▶ exam_grading_packs ──▶ exam_pack_assets
  │          └──▶ grading_test_definitions
  │
  └──▶ exam_submissions (per student, upsert = bài nộp cuối)
         workflow_status: Pending → Queued → Running → Completed/Failed
         total_score
         │
         ├──▶ exam_submission_files      ← đường dẫn zip trên storage
         │      question_label (Q1/Q2)
         │      storage_relative_path
         │
         ├──▶ exam_question_scores       ← điểm tổng hợp theo câu
         ├──▶ exam_test_case_scores      ← điểm từng testcase + raw output
         │
         └──▶ grading_jobs              ← lần chạy chấm
                hangfire_job_id
                job_status: Queued → Running → Completed/Failed
                retry_count
                │
                └──▶ grading_job_logs   ← log từng phase
                       phase: Extract/Discover/RunServer/RunNewman/Grade/Cleanup
                       level: Info/Warning/Error
                       message, detail_json, occurred_at_utc
```

---

## Giả lập cho demo / báo cáo

Vì hệ thống FPT không có sẵn, nhóm dùng **Postman / Swagger** để giả lập toàn bộ luồng:

| Bước | Hành động | Endpoint |
|------|-----------|----------|
| 1 | Tạo ca thi với giờ kết thúc 5 phút nữa | `POST /api/cms/grading/exam-sessions` |
| 2 | Nộp bài (SV 1) với 2 file zip | `POST /api/cms/grading/submissions` |
| 3 | Nộp lại (SV 1 nộp đè) | `POST /api/cms/grading/submissions` (upsert) |
| 4 | Nộp bài (SV 2) | `POST /api/cms/grading/submissions` |
| 5 | Chờ `EndsAtUtc` — Hangfire tự trigger | Quan sát Hangfire Dashboard `/hangfire` |
| 6 | Xem kết quả | `GET /api/cms/grading/submissions/{id}` |

**Seed data sẵn có** (bật `DataSeeding__EnableSeeding=true`):
- Học kỳ: `SPRING2026`
- Ca thi: `PRN232-DEMO-PE` — id `b1000000-0000-4000-8000-000000000002`
- Bài nộp mẫu: id `b1000000-0000-4000-8000-00000000feed`

---

## Ghi chú triển khai

- **Storage hiện tại:** Local disk (`wwwroot/uploads/`) — mount qua Docker volume, đủ dùng cho lab.
- **Storage production:** Thay bằng MinIO / Azure Blob — chỉ cần đổi implementation `IFileStorageService`.
- **Newman:** Phải cài `npm install -g newman` trong Docker image (Dockerfile đã có).
- **dotnet runtime:** Worker chạy `dotnet App.dll` — cần .NET runtime trong container; hoặc dùng `ProcessStartInfo` với `dotnet` trên PATH.
- **Port isolation:** Mỗi GradingJob dùng port động (`TcpListener` lấy port free) để tránh xung đột khi chạy song song nhiều submission.
