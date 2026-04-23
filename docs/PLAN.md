# Topic-Aware Grading From Submission File Paths

## Summary
Giữ mô hình hiện tại là `SummarizeExamResultJob` tạo `GradingJob` theo từng `ExamSubmission` pending và `GradeSubmissionJob` chấm theo các `ExamSubmissionFile` của submission đó. Điểm thay đổi chính là grading không còn lấy toàn bộ `session.Topics` để build Newman chung, mà sẽ suy `ExamTopic` từ `ExamSubmissionFile.StorageRelativePath`, rồi chỉ map `ExamSubmissionFile.QuestionLabel` với `ExamQuestion.Label` trong đúng topic đó để chọn test cases.

Không đổi schema/domain entity. Không đổi storage contract hiện có: path vẫn là `session.Code/{topicId:N}/{studentFolder}/Qx/solution.zip`.

## Key Changes
### Submission API and upload flow
- Thêm `ExamTopicId` vào `StudentZipEntry` trong `BatchSubmitZipsRequest`.
- Cập nhật validator để yêu cầu `ExamTopicId` khác rỗng.
- Cập nhật `BatchSubmitZipsCommandHandler` để:
  - Load đúng `ExamTopic` theo `entry.ExamTopicId` và xác nhận topic thuộc `command.ExamSessionId`.
  - Bỏ logic lấy `session.Topics.OrderBy(...).First()`.
  - Build `baseDir` bằng `session.Code/{topic.Id:N}/{folderName}`.
  - Vẫn tạo `ExamSubmissionFile` cho từng `Q1`, `Q2`, ... với `QuestionLabel` và `StorageRelativePath` như hiện tại.
- Cập nhật Swagger/API contract cho endpoint batch submit dùng request mới.

### GradeSubmissionJob
- Thêm bước parse `ExamSubmissionFile.StorageRelativePath` thành context nội bộ gồm:
  - `SessionCode`
  - `ExamTopicId`
  - `StudentFolder`
  - `QuestionLabelFromPath`
- Validate mỗi file path phải đúng format `session.Code/{topicId}/{studentFolder}/Qx/solution.zip`.
- Validate toàn bộ file trong một `ExamSubmission` phải cùng `ExamTopicId`; nếu lẫn nhiều topic trong một submission thì fail job và ghi `GradingJobLog`.
- Validate `QuestionLabelFromPath` phải khớp `ExamSubmissionFile.QuestionLabel`; nếu lệch thì fail job.
- Load `ExamSession` kèm `Topics -> Questions -> TestCases`, sau đó chọn đúng `ExamTopic` theo `topicId` parse từ path.
- Build Newman collection chỉ từ `ExamQuestion` của topic đã chọn, map theo `ExamQuestion.Label == ExamSubmissionFile.QuestionLabel`.
- Không hardcode riêng `Q1/Q2`; xử lý theo `Qx` tổng quát, nhưng vẫn tương thích `Q1`, `Q2`.
- Khi tính điểm, chỉ chấm các câu thuộc topic đã chọn; không duyệt các topic khác trong session.
- Giữ nguyên phần persist `TestResult`, `TestResultDetail`, `ExamQuestionScore`, `ExamTestCaseScore`, nhưng dữ liệu đầu vào giờ là kết quả của đúng topic.

### SummarizeExamResultJob and docs
- `SummarizeExamResultJob` giữ 1 `GradingJob` cho mỗi `ExamSubmission` pending.
- Chỉ bổ sung nếu cần các guard/log nhẹ để bỏ qua submission thiếu file hoặc thiếu record cần thiết trước khi enqueue.
- Cập nhật `docs/WORKFLOW_OVERVIEW.md` để phản ánh workflow mới:
  - grading theo `ExamSubmissionFile.StorageRelativePath`
  - detect `ExamTopic` từ path
  - build Newman theo topic đã detect, không còn build từ toàn bộ session.

## Public Interfaces
- `StudentZipEntry` thêm `Guid ExamTopicId`.
- Không thêm cột DB, không thêm FK mới trên entity.
- Trong `GradeSubmissionJob`, thêm helper/parser nội bộ cho submission file path và helper lấy topic/question map theo path.

## Test Plan
- Thêm test project backend tối thiểu nếu repo chưa có.
- Test `BatchSubmitZipsCommandHandler`:
  - lưu file đúng path với `ExamTopicId` được gửi lên
  - reject khi `ExamTopicId` không thuộc `ExamSession`
- Test parser path trong `GradeSubmissionJob`:
  - parse đúng path hợp lệ
  - fail khi thiếu segment, `topicId` không parse được, hoặc folder `Qx` không khớp `QuestionLabel`
- Test grading cho session có nhiều topic:
  - submission path trỏ topic A thì chỉ lấy test cases của topic A
  - không bị lẫn test cases của topic B dù cùng có `Q1/Q2`
- Test failure case:
  - một submission chứa file thuộc nhiều `topicId`
  - `QuestionLabel` record khác folder `Qx`
  - path topic hợp lệ nhưng session không có topic tương ứng
- Test end-to-end job flow:
  - `SummarizeExamResultJob` tạo và enqueue 1 job/submission
  - `GradeSubmissionJob` chấm xong và persist score/detail đúng topic.

## Assumptions
- Mỗi `ExamSubmission` thực tế thuộc đúng một `ExamTopic`, và điều đó được suy ra từ `StorageRelativePath` của toàn bộ `ExamSubmissionFile` trong submission.
- `ExamSubmissionFile.QuestionLabel` vẫn là nguồn chính để map sang `ExamQuestion.Label`; segment `Qx` trong path chỉ dùng để verify nhất quán.
- Path chuẩn tiếp tục là `session.Code/{topicId:N}/{studentFolder}/Qx/solution.zip`; không hỗ trợ `examSessionId/...` trong thay đổi này.
