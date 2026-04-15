import { Link } from "react-router-dom";
import { MOCK_SUBMISSIONS } from "../mock/cmsMockData";
import { StatusBadge } from "../components/StatusBadge";

export function SubmissionsListPage() {
  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Lọc theo ca thi, MSSV, trạng thái — sẽ nối API</p>
        </div>
        <div className="ag-toolbar__actions">
          <Link to="/submissions/upload" className="ag-btn ag-btn--primary">
            Tải lên zip mới
          </Link>
        </div>
      </div>

      <div className="ag-card ag-card--flush ag-animate-in">
        <div className="ag-table-wrap">
          <table className="ag-table">
            <thead>
              <tr>
                <th>MSSV</th>
                <th>Họ tên</th>
                <th>Ca thi</th>
                <th>Thời điểm nộp</th>
                <th>Q1</th>
                <th>Q2</th>
                <th>Điểm</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {MOCK_SUBMISSIONS.map((s) => (
                <tr key={s.id}>
                  <td>
                    <code className="ag-code">{s.studentCode}</code>
                  </td>
                  <td className="ag-table__strong">{s.studentName}</td>
                  <td>
                    <code className="ag-code ag-code--sm">{s.examSessionCode}</code>
                  </td>
                  <td className="ag-table__muted">{new Date(s.submittedAt).toLocaleString("vi-VN")}</td>
                  <td>
                    <StatusBadge status={s.q1Status} />
                  </td>
                  <td>
                    <StatusBadge status={s.q2Status} />
                  </td>
                  <td>
                    {s.totalScore != null ? (
                      <span className="ag-score">
                        {s.totalScore}
                        <span className="ag-score__max">/{s.maxScore}</span>
                      </span>
                    ) : (
                      <span className="ag-table__muted">—</span>
                    )}
                  </td>
                  <td className="ag-table__actions">
                    <Link className="ag-linkbtn" to={`/submissions/${s.id}`}>
                      Chi tiết
                    </Link>
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
