import { MOCK_EXAM_SESSIONS } from "../mock/cmsMockData";
import { SessionStatusBadge } from "../components/StatusBadge";

export function ExamSessionsPage() {
  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Semester → ExamSession → Topic → Question</p>
        </div>
        <div className="ag-toolbar__actions">
          <button type="button" className="ag-btn ag-btn--secondary" disabled title="Chờ API">
            + Tạo ca thi
          </button>
        </div>
      </div>

      <div className="ag-card ag-card--flush ag-animate-in">
        <div className="ag-table-wrap">
          <table className="ag-table">
            <thead>
              <tr>
                <th>Mã</th>
                <th>Tên hiển thị</th>
                <th>Học kỳ</th>
                <th>Lịch thi (UTC)</th>
                <th>Chủ đề / Câu</th>
                <th>Bài nộp</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {MOCK_EXAM_SESSIONS.map((row) => (
                <tr key={row.id}>
                  <td>
                    <code className="ag-code">{row.code}</code>
                  </td>
                  <td>
                    <span className="ag-table__strong">{row.title}</span>
                  </td>
                  <td>{row.semester}</td>
                  <td className="ag-table__muted">{new Date(row.scheduledAt).toLocaleString("vi-VN")}</td>
                  <td>
                    {row.topicCount} / {row.questionCount}
                  </td>
                  <td>{row.submissionCount}</td>
                  <td>
                    <SessionStatusBadge status={row.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
