# REST API — Auto Grading (demo báo cáo)

Base URL local: `http://localhost:5000` (Docker map `API_PORT`). Swagger: `/swagger`.

Chuẩn response: `Result<T>` (`isSuccess`, `message`, `data`, `errors`, `errorCode`).

## Nhóm `CMS_Grading` (mới)

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/api/cms/grading/semesters` | Danh sách học kỳ |
| GET | `/api/cms/grading/exam-sessions` | Danh sách ca thi; query `semesterId` (optional) |
| GET | `/api/cms/grading/exam-sessions/{id}` | Chi tiết ca thi (topic → câu → testcase) |
| GET | `/api/cms/grading/submissions?examSessionId={guid}` | Bài nộp theo ca thi |
| GET | `/api/cms/grading/submissions/{id}` | Chi tiết bài nộp + điểm câu + testcase |
| POST | `/api/cms/grading/submissions` | `multipart/form-data`: `examSessionId`, `studentCode`, `studentName` (optional), `q1Zip`, `q2Zip` — lưu file + **stub chấm** ngay |

## Nhóm Auth (sẵn có)

| Method | Path | Mô tả |
|--------|------|--------|
| POST | `/api/cms/auth/login` | JWT cho Swagger **Authorize** |

## Dữ liệu seed (khi `DataSeeding__EnableSeeding=true`)

- Học kỳ: `SPRING2026`
- Ca thi: `PRN232-DEMO-PE` — `examSessionId` cố định: `b1000000-0000-4000-8000-000000000002`
- Một bài nộp mẫu: `submissionId` = `b1000000-0000-4000-8000-00000000feed`

## Gợi ý chụp Swagger / video

1. Mở `/swagger` → mở rộng **CMS_Grading**.
2. Gọi `GET /api/cms/grading/semesters` → `GET exam-sessions` → `GET exam-sessions/{id}`.
3. `GET submissions?examSessionId=...` → `GET submissions/{id}` (bài mẫu).
4. (Tuỳ chọn) `POST submissions` với 2 file zip bất kỳ + `studentCode` mới.

## Ghi chú triển khai

- **P1:** entity + migration + seed theo ERD tối thiểu.
- **P2:** REST + upload + `Result<T>`.
- **P3 (stub):** sau upload, hệ thống ghi điểm testcase/câu **cố định 85% max** để demo; sau này thay bằng Hangfire + grader thật.
