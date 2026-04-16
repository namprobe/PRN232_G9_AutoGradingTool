import { useEffect, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { listExamSessions } from "../api/gradingApi";
import type { ExamSessionListItem } from "../api/gradingTypes";
import { SessionStatusBadge } from "../components/StatusBadge";
import { inferSessionStatus } from "../lib/gradingUi";

export function ExamSessionsPage() {
  const { token } = useAuth();
  const [rows, setRows] = useState<ExamSessionListItem[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setErr(null);
      const r = await listExamSessions(token, null);
      if (cancelled) return;
      if (!r.isSuccess || !r.data) setErr(r.message ?? "Không tải được danh sách ca thi");
      else setRows(r.data);
      setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [token]);

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Semester → ExamSession → Topic → Question</p>
        </div>
        <div className="ag-toolbar__actions">
          <button type="button" className="ag-btn ag-btn--secondary" disabled title="Chưa có API tạo ca">
            + Tạo ca thi
          </button>
        </div>
      </div>

      {err ? (
        <div className="ag-alert ag-alert--err" role="alert">
          {err}
        </div>
      ) : null}

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
                <th>Trạng thái (ước lượng)</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} className="ag-table__muted">
                    Đang tải…
                  </td>
                </tr>
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={7} className="ag-table__muted">
                    Không có ca thi
                  </td>
                </tr>
              ) : (
                rows.map((row) => (
                  <tr key={row.id}>
                    <td>
                      <code className="ag-code">{row.code}</code>
                    </td>
                    <td>
                      <span className="ag-table__strong">{row.title}</span>
                    </td>
                    <td>{row.semesterCode}</td>
                    <td className="ag-table__muted">{new Date(row.scheduledAtUtc).toLocaleString("vi-VN")}</td>
                    <td>
                      {row.topicCount} / {row.questionCount}
                    </td>
                    <td>{row.submissionCount}</td>
                    <td>
                      <SessionStatusBadge status={inferSessionStatus(row.scheduledAtUtc)} />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
