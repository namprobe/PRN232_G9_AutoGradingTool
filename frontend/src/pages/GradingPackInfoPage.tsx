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
        . Trên CMS đã có{" "}
        <code className="ag-code ag-code--sm">GET/POST …/exam-sessions/{"{sessionId}"}/grading-packs</code>,{" "}
        <code className="ag-code ag-code--sm">PUT/DELETE …/grading-packs/{"{packId}"}</code> và upload asset{" "}
        <code className="ag-code ag-code--sm">POST …/grading-packs/{"{packId}"}/assets</code> (multipart). Vào chi tiết
        một ca thi để tạo pack, bật active và tải file script/rubric.
      </div>

      <section className="ag-card ag-animate-in">
        <div className="ag-card__head">
          <h2 className="ag-card__title">Thao tác trên UI</h2>
          <p className="ag-card__desc">Quản lý pack theo từng ca thi</p>
        </div>
        <ul className="ag-stack ag-stack--sm" style={{ paddingLeft: "1.2rem", margin: 0 }}>
          <li>
            <Link to="/exam-sessions" className="ag-linkbtn">
              Danh sách ca thi
            </Link>{" "}
            → mở một ca → mục grading pack &amp; upload asset.
          </li>
          <li>
            Học sinh nộp zip:{" "}
            <Link to="/submissions/upload" className="ag-linkbtn">
              Tải lên zip
            </Link>{" "}
            (chọn kênh student API hoặc
            CMS).
          </li>
        </ul>
      </section>
    </div>
  );
}
