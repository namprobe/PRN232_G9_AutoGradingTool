# Phân công nhóm 4 người + FE — Auto Grading PRN232

> Căn cứ: base `src` (Clean Architecture + Hangfire + Postgres + Redis), flow ERD thầy (Semester → ExamSession → Topic → Question → TestCase; Submit → QuestionScore → Result), nộp bài dạng **2 zip** (Q1, Q2) theo `PE_PRN232_GivenSolution`.

---

## Nguyên tắc chung

1. **Hợp đồng giữa các role:** Domain (Person 1) định nghĩa entity + enum trước; API (Person 2) và Grader (Person 3) chỉ **dùng** interface/DTO đã merge; FE (Person 4) chờ **OpenAPI / endpoint list** từ Person 2 (có thể stub Swagger trước).
2. **Nhánh Git:** mỗi người 1 feature branch: `feat/domain-core`, `feat/api-submissions`, `feat/grader-engine`, `feat/fe-admin` — merge qua PR nhỏ.
3. **Môi trường:** cả nhóm dùng chung `docker compose` + `.env` theo README.

---

## Person 1 — Domain + Persistence (nền dữ liệu theo ERD thầy)

**Mục tiêu:** Có model DB khớp flow chấm, migration chạy được, repository cơ bản.

### Việc cụ thể

| # | Task | Output |
|---|------|--------|
| 1 | Thiết kế entity: `Semester`, `ExamSession`, `Topic`, `Question`, `TestCase`, `Submit`, `QuestionScore`, `Result` (FK đúng cardinality 1–n như sơ đồ) | `Domain/Entities/*.cs` |
| 2 | Enum: trạng thái `Submit` (Pending/Running/Completed/Failed), `Result` (Pass/Fail/Skipped) | `Domain/Enums/*.cs` |
| 3 | EF configuration + migration **main DB** | `Infrastructure/Migrations/...` |
| 4 | Interface repository (hoặc spec query) cho Submit, Question, TestCase, Result | `Application/Common/Interfaces` hoặc `Domain` tùy chuẩn nhóm |
| 5 | Seed tối thiểu: 1 Semester, 1 ExamSession, 1 Topic, 2 Question (Q1/Q2), vài `TestCase` mẫu (placeholder) | `Infrastructure` seeding hoặc script SQL |

### Không làm

- Không viết HTTP controller.
- Không viết logic chạy `dotnet test` / unzip (để Person 3).

### Handoff cho người khác

- File **ERD-text** hoặc diagram ngắn trong `docs/DATA_MODEL.md` (tên bảng + FK).
- PR merge xong → báo Person 2 + 3 rebase.

---

## Person 2 — API (CMS) + Upload + Orchestration

**Mục tiêu:** API để FE và pipeline chấm dùng: tạo kỳ thi, upload zip, xem trạng thái, xem điểm.

### Việc cụ thể

| # | Task | Output |
|---|------|--------|
| 1 | CQRS Commands: tạo/cập nhật Semester, ExamSession, Topic, Question (metadata link tới zip template nếu cần) | `Application/Features/ExamAdmin/...` |
| 2 | Command: **UploadSubmit** — nhận 1–2 file zip (Q1, Q2), lưu disk/S3 (dùng `FileStorage` sẵn), tạo bản ghi `Submit` + file path | `Application` + `API/Controllers` |
| 3 | Command: **EnqueueGrade** — sau upload, enqueue Hangfire job `GradeSubmitJob(submitId)` (interface gọi Person 3) | `Application` + đăng ký Hangfire |
| 4 | Queries: list submit theo session, chi tiết submit (điểm theo câu + testcase) | `Application/Features/...` |
| 5 | Controller REST + XML doc Swagger; chuẩn `Result<T>` hiện có | `API/Controllers/Cms/...` |
| 6 | CORS: thêm origin FE (ví dụ `http://localhost:5173`) vào config | `.env.example` + `ServiceConfiguration` |

### Hợp đồng với Person 3

- Định nghĩa interface `IGradingOrchestrator` hoặc `ISubmitGrader` trong `Application`: `Task GradeAsync(Guid submitId, CancellationToken ct)`.
- Person 3 implement trong `Infrastructure` (hoặc project riêng nếu sau này tách worker).

### Handoff cho Person 4

- Bảng endpoint (method + path + body mẫu) trong `docs/API_FOR_FE.md` + Swagger chạy được.

---

## Person 3 — Grading Engine + Hangfire Job

**Mục tiêu:** Từ `SubmitId`: giải zip → build/publish (hoặc tối thiểu build) → chạy testcase → ghi `Result` + `QuestionScore`.

### Việc cụ thể

| # | Task | Output |
|---|------|--------|
| 1 | Implement `IGradingOrchestrator.GradeAsync`: load Submit + Question + TestCase từ DB | `Infrastructure/Services/Grading/` |
| 2 | **Sandbox thư mục tạm** mỗi lần chấm (extract zip Q1/Q2 vào `Path.Combine(temp, submitId, ...)`) | Service + cleanup `finally` |
| 3 | **Executor tối thiểu:** `dotnet build` trên solution trong zip; ghi log stdout/stderr vào `Result` hoặc bảng log | 1 testcase kiểu `BuildSuccess` |
| 4 | **HTTP testcase (sau):** chạy API student + curl/HttpClient so status/body (cho Q1 copies, Q2 cần GivenAPI mock — có thể phase 2) | TestCase type enum + strategy pattern |
| 5 | Cập nhật `Submit` status; insert `Result` từng testcase; aggregate `QuestionScore` | Transaction rõ ràng |
| 6 | Đăng ký Hangfire: `RecurringJob` hoặc `BackgroundJob.Enqueue` từ Person 2 gọi vào | `Infrastructure/Configurations/Hangfire` |

### Không làm

- Không chỉnh UI FE.
- Không tạo entity mới nếu Person 1 đã định nghĩa — chỉ mở rộng nếu thiếu field (bàn Person 1).

### Tiêu chí “xong sprint 1”

- Upload zip giả → job chạy → DB có `Result` ít nhất 1 dòng + `Submit.Status = Completed` hoặc `Failed` có lý do.

---

## Person 4 — Frontend (FE) + hợp nhất UX

**Mục tiêu:** Web cho giảng viên/nhóm: đăng nhập (dùng API auth sẵn), quản lý tối thiểu: xem session, upload zip, xem trạng thái chấm, xem điểm.

### Stack gợi ý (khớp CORS README)

- **React + Vite** (`http://localhost:5173`) hoặc **Next.js** — README đã liệt kê `5173`.

### Việc cụ thể

| # | Task | Output |
|---|------|--------|
| 1 | Repo FE: `frontend/` hoặc `src/PRN232_G9_AutoGradingTool.WebClient/` (chọn 1, cả nhóm thống nhất) | Project mới |
| 2 | Auth: login JWT, lưu token, refresh (gọi `/api/cms/auth/*`) | Trang Login |
| 3 | Trang: Danh sách ExamSession / Topic (read-only phase 1) | Table + call API Person 2 |
| 4 | Trang: **Upload** 2 zip (Q1, Q2) + chọn ExamSession | `multipart/form-data` |
| 5 | Trang: **Chi tiết Submit** — progress + bảng QuestionScore + expand Result | Polling hoặc SignalR (phase 2) |
| 6 | `README` FE: env `VITE_API_BASE_URL=http://localhost:5000` | `.env.example` |

### Phối hợp

- Person 2 ưu tiên endpoint login + upload + get submit trước khi Person 4 làm upload page.
- Person 4 có thể dùng **mock JSON** trong 2 ngày đầu nếu API chưa xong.

---

## Bảng tổng hợp trách nhiệm (rút gọn)

| Người | Trách nhiệm chính | Phụ thuộc |
|-------|-------------------|-----------|
| **P1** | ERD → Entity + Migration + Seed | — |
| **P2** | API upload + enqueue + query + Swagger + CORS | P1 merge |
| **P3** | Hangfire + unzip + build + ghi Result/Score | P1 merge, contract P2 |
| **P4** | FE (Vite/React) + gọi API | P2 stub API |

---

## Thứ tự merge khuyến nghị (tuần 1)

1. **P1** merge Domain + migration (trống bảng ok).
2. **P2** merge API skeleton (endpoint trả mock).
3. **P4** merge FE login + layout.
4. **P3** merge job “fake pass” 1 testcase → sau đó thay bằng build thật.

---

## Ghi chú “2 service” khi báo cáo thầy

- **Service 1:** `autogradingtool-api` — REST + nhận file + trigger chấm.
- **Service 2:** **Hangfire worker** (cùng process hoặc scale `Hangfire__WorkerCount`) — thực thi chấm bất đồng bộ; sau nếu thầy bắt tách container, tách project `Worker` dùng chung DB + Redis.

---

## File này

- Đường dẫn: `docs/TEAM_WORK_DIVISION.md`
- Cập nhật khi thầy chốt thêm rule (ví dụ chỉ 1 zip, hoặc bắt Docker riêng cho grader).
