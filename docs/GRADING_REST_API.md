# REST API — Auto Grading

Base URL local: `http://localhost:5000` (Docker map `API_PORT`). Swagger: `/swagger`.

Tất cả endpoint trả chuẩn `Result<T>`: `isSuccess`, `message`, `data`, `errors`, `errorCode`.

## Nhóm `CMS_Grading`

### Truy vấn

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/api/cms/grading/semesters` | Danh sách học kỳ |
| GET | `/api/cms/grading/exam-sessions?semesterId={guid?}` | Danh sách ca thi |
| GET | `/api/cms/grading/exam-sessions/{id}` | Chi tiết ca thi (topic → câu → testcase) |
| GET | `/api/cms/grading/submissions?examSessionId={guid}&examSessionClassId={guid?}` | Danh sách bài nộp theo ca thi |
| GET | `/api/cms/grading/submissions/{id}` | Chi tiết bài nộp + điểm câu + testcase |
| GET | `/api/cms/grading/semesters/{semesterId}/exam-classes` | Danh sách lớp theo học kỳ |
| GET | `/api/cms/grading/exam-sessions/{sessionId}/session-classes` | Danh sách lớp trong ca thi |

### Nộp bài batch

| Method | Path | Mô tả |
|--------|------|--------|
| POST | `/api/cms/grading/exam-sessions/{sessionId}/submissions/batch` | CMS nộp batch, bypass khung giờ |
| POST | `/api/student/grading/exam-sessions/{sessionId}/submissions/batch` | Student nộp batch, kiểm tra khung giờ |

`multipart/form-data` cho batch dùng format:

- `entries[0].examTopicId` (required)
- `entries[0].studentCode` (required)
- `entries[0].studentName` (optional)
- `entries[0].q1Zip` (required `.zip`)
- `entries[0].q2Zip` (required `.zip`)
- tối đa 50 entries/request

### CRUD định nghĩa đề thi

| Method | Path |
|--------|------|
| POST/PUT/DELETE | `/api/cms/grading/semesters/*` |
| POST/PUT/DELETE | `/api/cms/grading/exam-sessions/*` |
| POST/PUT/DELETE | `/api/cms/grading/exam-topics/*` |
| POST/PUT/DELETE | `/api/cms/grading/exam-questions/*` |
| POST/PUT/DELETE | `/api/cms/grading/exam-test-cases/*` |
| GET/POST/PUT/DELETE | `/api/cms/grading/*grading-packs*` |
| POST/DELETE | `/api/cms/grading/*pack-assets*` |
| POST/DELETE | `/api/cms/grading/*exam-classes*`, `/api/cms/grading/*session-classes*` |

## Ghi chú queue/worker

- Chấm tự động dùng Hangfire queue `grading`.
- Cần cấu hình: `Hangfire__Queues=notification-system,grading`.

## Auth

| Method | Path | Mô tả |
|--------|------|--------|
| POST | `/api/cms/auth/login` | Lấy JWT để gọi API CMS trên Swagger |
