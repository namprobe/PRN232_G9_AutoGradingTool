import { Link } from "react-router-dom";

/**
 * Nội dung bám `docs/SYSTEM_FLOWS.md` + entity domain — giúp FE/BA cùng ngôn ngữ với BE.
 * (Không thay thế tài liệu repo; chỉ là “bản tóm tắt có liên kết” trong CMS.)
 */
export function SystemFlowsPage() {
  return (
    <div className="ag-stack ag-stack--lg">
      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h2 className="ag-card__title">Entity → giao diện CMS</h2>
          <p className="ag-card__desc">
            Giao diện hiện tại dựa trên các thực thể chính: học kỳ, ca thi, chủ đề/câu/testcase, bài nộp, file theo câu,
            pack chấm, job chấm.
          </p>
        </div>
        <div className="ag-table-wrap">
          <table className="ag-table">
            <thead>
              <tr>
                <th>Entity / nhóm</th>
                <th>Trường / quan hệ quan trọng</th>
                <th>Trên CMS</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>
                  <code className="ag-code">Semester</code>
                </td>
                <td className="ag-table__muted">Code, Name, khoảng thời gian</td>
                <td>
                  <Link to="/semesters" className="ag-linkbtn">
                    Học kỳ
                  </Link>
                </td>
              </tr>
              <tr>
                <td>
                  <code className="ag-code">ExamSession</code>
                </td>
                <td className="ag-table__muted">
                  SemesterId, StartsAtUtc, ExamDurationMinutes, EndsAtUtc → Topics → Questions → TestCases
                </td>
                <td>
                  <Link to="/exam-sessions" className="ag-linkbtn">
                    Danh sách ca
                  </Link>
                  <span className="ag-table__muted"> — bấm “Chi tiết đề” trên một dòng để xem topic / question / testcase.</span>
                </td>
              </tr>
              <tr>
                <td>
                  <code className="ag-code">ExamSubmission</code> + <code className="ag-code">ExamSubmissionFile</code>
                </td>
                <td className="ag-table__muted">
                  WorkflowStatus, ExamGradingPackId?, SubmissionFiles (QuestionLabel, path, tên file)
                </td>
                <td>
                  <Link to="/submissions" className="ag-linkbtn">
                    Bài nộp
                  </Link>{" "}
                  · chi tiết: file + điểm câu + testcase + admin thay file / regrade
                </td>
              </tr>
              <tr>
                <td>
                  <code className="ag-code">ExamGradingPack</code> + <code className="ag-code">ExamPackAsset</code>
                </td>
                <td className="ag-table__muted">Version, IsActive, Label, Assets</td>
                <td>
                  <Link to="/grading-pack" className="ag-linkbtn">
                    Pack &amp; asset
                  </Link>{" "}
                  (REST list/upload theo kế hoạch trong tài liệu)
                </td>
              </tr>
              <tr>
                <td>
                  <code className="ag-code">GradingJob</code>
                </td>
                <td className="ag-table__muted">Trigger SessionEnd | ManualRegrade, JobStatus, liên kết Pack + Submission</td>
                <td className="ag-table__muted">Phản hồi sau POST regrade (job id, trạng thái); theo dõi đầy đủ qua API/Hangfire sau này</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h2 className="ag-card__title">Bốn luồng chính (system flow)</h2>
          <p className="ag-card__desc">Theo thứ tự nghiệp vụ trong `docs/SYSTEM_FLOWS.md`</p>
        </div>
        <ol className="ag-stack ag-stack--md" style={{ margin: 0, paddingLeft: "1.25rem" }}>
          <li>
            <strong>Proctor chuẩn bị ca</strong> — tạo <code className="ag-code ag-code--sm">ExamSession</code>, cây đề,{" "}
            <code className="ag-code ag-code--sm">ExamGradingPack</code> active, có thể gia hạn <code className="ag-code ag-code--sm">EndsAtUtc</code>.
          </li>
          <li>
            <strong>Sinh viên nộp bài</strong> — lưu zip, <code className="ag-code ag-code--sm">ExamSubmission</code> +{" "}
            <code className="ag-code ag-code--sm">ExamSubmissionFile</code>; production: portal ngoài scope; demo:{" "}
            <Link to="/submissions/upload" className="ag-linkbtn">
              POST submissions
            </Link>
            .
          </li>
          <li>
            <strong>Chấm tự động</strong> — sau <code className="ag-code ag-code--sm">EndsAtUtc</code>, trigger{" "}
            <code className="ag-code ag-code--sm">SessionEnd</code>, enqueue <code className="ag-code ag-code--sm">GradingJob</code> hàng loạt.
          </li>
          <li>
            <strong>Chấm thủ công / sự cố</strong> — admin{" "}
            <code className="ag-code ag-code--sm">PUT …/submissions/{"{id}"}/files</code> rồi{" "}
            <code className="ag-code ag-code--sm">POST …/regrade</code> (<code className="ag-code ag-code--sm">ManualRegrade</code>).
          </li>
        </ol>
      </section>

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h2 className="ag-card__title">WorkflowStatus (ExamSubmission)</h2>
          <p className="ag-card__desc">Giá trị API trả về dạng chuỗi enum</p>
        </div>
        <p className="ag-table__muted" style={{ marginTop: 0 }}>
          <code className="ag-code">Pending</code> → <code className="ag-code">Queued</code> → <code className="ag-code">Running</code> →{" "}
          <code className="ag-code">Completed</code> hoặc <code className="ag-code">Failed</code>. Hiển thị trên danh sách và chi tiết bài nộp.
        </p>
      </section>

      <div className="ag-alert ag-alert--info" role="status">
        <strong>Đối chiếu triển khai hiện tại:</strong> Nhóm <code className="ag-code ag-code--sm">CMS_Grading</code> đã có GET semester / exam-session / submission
        và upload + thay file + regrade. Các endpoint mô tả trong tài liệu cho tạo ca, pack, reschedule (POST/PATCH) có thể chưa mở — khi BE bổ sung,
        các trang tương ứng sẽ gọi API thay cho ghi chú “seed / SQL”.
      </div>
    </div>
  );
}
