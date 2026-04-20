import { Link } from "react-router-dom";

export function GradingPackInfoPage() {
  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Exam grading pack &amp; asset</p>
          <p className="ag-table__muted" style={{ marginTop: 6 }}>
            Mỗi ca thi có thể có một <strong>ExamGradingPack</strong> (phiên bản, trạng thái active) và nhiều{" "}
            <strong>ExamPackAsset</strong> (script chấm, rubric, v.v.). Luồng nộp bài và chấm lại (regrade) backend đã
            gắn pack theo <code className="ag-code ag-code--sm">ExamSessionId</code>.
          </p>
        </div>
        <div className="ag-toolbar__actions">
          <Link to="/exam-sessions" className="ag-btn ag-btn--secondary">
            Ca thi
          </Link>
        </div>
      </div>

      <div className="ag-alert ag-alert--info" role="status">
        Xem thêm bối cảnh luồng nghiệp vụ:{" "}
        <Link to="/system-flows" className="ag-linkbtn">
          Luồng hệ thống
        </Link>
        . Hiện <strong>chưa có</strong> endpoint REST trong nhóm <code className="ag-code ag-code--sm">CMS_Grading</code>{" "}
        để liệt kê pack hay upload asset qua UI. Dữ liệu mẫu đến từ seeder (
        <code className="ag-code ag-code--sm">ExamGradingPackSeeder</code>). Khi nhóm thêm GET/POST pack, trang này sẽ
        thay bằng bảng và form thật.
      </div>

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h2 className="ag-card__title">Việc cần làm phía backend (gợi ý)</h2>
          <p className="ag-card__desc">Để khớp backlog “pack + asset” trên CMS</p>
        </div>
        <ul className="ag-stack ag-stack--sm" style={{ paddingLeft: "1.2rem", margin: 0 }}>
          <li>
            <code className="ag-code">GET …/exam-sessions/{"{id}"}/grading-pack</code> — pack active + danh sách asset
          </li>
          <li>
            <code className="ag-code">POST …/grading-pack/{"{id}"}/assets</code> — multipart upload
          </li>
          <li>Swagger tag ví dụ <code className="ag-code">CMS_GradingPack</code></li>
        </ul>
      </section>
    </div>
  );
}
