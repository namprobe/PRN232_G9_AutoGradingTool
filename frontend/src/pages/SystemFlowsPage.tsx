import { Link } from "react-router-dom";
import { workflowStatusLabel } from "../lib/gradingUi";

/**
 * Tóm tắt luồng nghiệp vụ (tham chiếu tài liệu trong repo). Diễn đạt bằng ngôn ngữ người dùng, không nhấn mạnh thuật ngữ lập trình.
 */
export function SystemFlowsPage() {
  return (
    <div className="ag-stack ag-stack--lg">
      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h2 className="ag-card__title">Các phần chính trong hệ thống</h2>
          <p className="ag-card__desc">
            Giao diện bám sát nghiệp vụ: học kỳ, ca thi, đề (chủ đề — câu — bài kiểm tra), bài nộp, gói chấm và hàng đợi
            chấm bài.
          </p>
        </div>
        <div className="ag-table-wrap">
          <table className="ag-table">
            <thead>
              <tr>
                <th>Đối tượng</th>
                <th>Vai trò</th>
                <th>Trên ứng dụng</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td className="ag-table__strong">Học kỳ</td>
                <td className="ag-table__muted">Khung thời gian và mã định danh kỳ học</td>
                <td>
                  <Link to="/semesters" className="ag-linkbtn">
                    Trang học kỳ
                  </Link>
                </td>
              </tr>
              <tr>
                <td className="ag-table__strong">Ca thi</td>
                <td className="ag-table__muted">Một phiên thi: lịch mở — đóng, thời lượng, cây đề và gói chấm</td>
                <td>
                  <Link to="/exam-sessions" className="ag-linkbtn">
                    Danh sách ca
                  </Link>
                  <span className="ag-table__muted"> — mở chi tiết để xem chủ đề, câu hỏi và bài kiểm tra.</span>
                </td>
              </tr>
              <tr>
                <td className="ag-table__strong">Bài nộp</td>
                <td className="ag-table__muted">Mỗi lần nộp gồm tệp zip theo câu, trạng thái chấm và điểm</td>
                <td>
                  <Link to="/submissions" className="ag-linkbtn">
                    Bài nộp
                  </Link>
                  <span className="ag-table__muted">
                    {" "}
                    — xem chi tiết: tệp đính kèm, điểm từng câu; quản trị có thể thay tệp và chấm lại.
                  </span>
                </td>
              </tr>
              <tr>
                <td className="ag-table__strong">Gói chấm</td>
                <td className="ag-table__muted">Script và tài liệu phục vụ chấm tự động, gắn với ca thi</td>
                <td>
                  <Link to="/grading-pack" className="ag-linkbtn">
                    Giới thiệu gói chấm
                  </Link>
                  <span className="ag-table__muted"> — thao tác thực tế nằm trong chi tiết ca.</span>
                </td>
              </tr>
              <tr>
                <td className="ag-table__strong">Tác vụ chấm</td>
                <td className="ag-table__muted">Xử lý nền sau khi hết giờ hoặc khi yêu cầu chấm lại</td>
                <td className="ag-table__muted">
                  Trạng thái hiển thị trên bài nộp; theo dõi sâu hơn (hàng đợi, nhật ký) do vận hành cấu hình ngoài giao diện
                  này.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h2 className="ag-card__title">Bốn bước nghiệp vụ chính</h2>
          <p className="ag-card__desc">Thứ tự làm việc thông thường (tham chiếu tài liệu luồng trong repo)</p>
        </div>
        <ol className="ag-stack ag-stack--md" style={{ margin: 0, paddingLeft: "1.25rem" }}>
          <li>
            <strong>Chuẩn bị ca</strong> — tạo ca thi, dựng đề (chủ đề, câu, bài kiểm tra), cấu hình gói chấm đang dùng và
            có thể gia hạn thời điểm đóng nộp.
          </li>
          <li>
            <strong>Thí sinh nộp bài</strong> — hệ thống lưu tệp zip theo từng câu; trên môi trường demo bạn có thể nộp qua{" "}
            <Link to="/exam-sessions" className="ag-linkbtn">
              Ca thi → Nộp ZIP
            </Link>
            .
          </li>
          <li>
            <strong>Chấm tự động</strong> — sau khi đến mốc đóng ca, hệ thống xếp hàng chấm hàng loạt theo gói đã cấu hình.
          </li>
          <li>
            <strong>Xử lý thủ công khi cần</strong> — quản trị thay tệp zip của một câu rồi yêu cầu chấm lại ngay trên trang
            chi tiết bài nộp.
          </li>
        </ol>
      </section>

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h2 className="ag-card__title">Trạng thái trên một bài nộp</h2>
          <p className="ag-card__desc">Diễn giải giá trị hệ thống trả về — hiển thị ở danh sách và trang chi tiết</p>
        </div>
        <p className="ag-table__muted" style={{ marginTop: 0 }}>
          {workflowStatusLabel("Pending")} → {workflowStatusLabel("Queued")} → {workflowStatusLabel("Running")} →{" "}
          {workflowStatusLabel("Completed")} hoặc {workflowStatusLabel("Failed")}.
        </p>
      </section>

      <div className="ag-alert ag-alert--info" role="status">
        <strong>Ghi chú triển khai:</strong> Các chức năng xem học kỳ, ca thi, danh sách bài nộp, nộp zip, thay tệp và chấm
        lại đã có trên giao diện này. Nếu backend bổ sung thêm thao tác (ví dụ lên lịch lại ca), các trang tương ứng sẽ được
        cập nhật sau — không cần bạn đọc tài liệu kỹ thuật để dùng các bước cơ bản ở trên.
      </div>
    </div>
  );
}
