import { Link } from "react-router-dom";

export function GradingPackInfoPage() {
  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Gói chấm điểm và tệp đính kèm</p>
          <p className="ag-table__muted" style={{ marginTop: 6 }}>
            Mỗi ca thi có thể có một <strong>gói chấm</strong> (phiên bản, có đang dùng hay không) và nhiều{" "}
            <strong>tệp đính kèm</strong> như script chấm, rubric, tài liệu hướng dẫn. Hệ thống luôn gắn gói chấm với
            đúng ca thi tương ứng.
          </p>
        </div>
        <div className="ag-toolbar__actions">
          <Link to="/exam-sessions" className="ag-btn ag-btn--secondary">
            Ca thi
          </Link>
        </div>
      </div>

      <div className="ag-alert ag-alert--info" role="status">
        Để hiểu thêm toàn bộ luồng nghiệp vụ, xem{" "}
        <Link to="/system-flows" className="ag-linkbtn">
          Luồng hệ thống
        </Link>
        . Trên giao diện quản trị, bạn tạo gói, chỉnh phiên bản và tải tệp lên ngay trong trang chi tiết từng ca thi —
        không cần ghi nhớ đường dẫn kỹ thuật.
      </div>

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h2 className="ag-card__title">Cách làm trên giao diện</h2>
          <p className="ag-card__desc">Mọi thao tác đều theo từng ca thi</p>
        </div>
        <ul className="ag-stack ag-stack--sm" style={{ paddingLeft: "1.2rem", margin: 0 }}>
          <li>
            <Link to="/exam-sessions" className="ag-linkbtn">
              Mở danh sách ca thi
            </Link>
            , chọn một ca, kéo xuống phần gói chấm để tạo phiên bản mới và tải tệp kèm theo.
          </li>
          <li>
            Để thí sinh hoặc demo nộp bài: từ{" "}
            <Link to="/exam-sessions" className="ag-linkbtn">
              Ca thi
            </Link>{" "}
            → vào chi tiết ca → bấm <strong>Nộp ZIP</strong>, chọn cổng thí sinh hoặc cổng quản trị tùy kịch bản.
          </li>
        </ul>
      </section>
    </div>
  );
}
