import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { listExamSessions, listSemesters } from "../api/gradingApi";
import type { ExamSessionListItem, SemesterListItem } from "../api/gradingTypes";
import { SessionStatusBadge } from "../components/StatusBadge";
import { inferSessionStatus } from "../lib/gradingUi";

export function ExamSessionsPage() {
  const { token } = useAuth();
  const [semesters, setSemesters] = useState<SemesterListItem[]>([]);
  const [semesterId, setSemesterId] = useState<string>("");
  const [rows, setRows] = useState<ExamSessionListItem[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const semesterFilter = useMemo(() => (semesterId || null) as string | null, [semesterId]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const sr = await listSemesters(token);
      if (cancelled) return;
      if (sr.isSuccess && sr.data) setSemesters(sr.data);
    })();
    return () => {
      cancelled = true;
    };
  }, [token]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setErr(null);
      const exRes = await listExamSessions(token, semesterFilter);
      if (cancelled) return;
      if (!exRes.isSuccess || !exRes.data) setErr(exRes.message ?? "Không tải được danh sách ca thi");
      else setRows(exRes.data);
      setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [token, semesterFilter]);

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Học kỳ → ca thi (GET exam-sessions?semesterId=…)</p>
          <p className="ag-table__muted" style={{ marginTop: 6 }}>
            Chọn một dòng rồi mở <strong>Chi tiết cấu trúc đề</strong> (topic / question / testcase). API tạo ca thi
            chưa có — dữ liệu từ seed / SQL.
          </p>
        </div>
        <div className="ag-toolbar__actions">
          <Link to="/semesters" className="ag-btn ag-btn--secondary">
            Học kỳ
          </Link>
          <Link to="/grading-pack" className="ag-btn ag-btn--secondary">
            Pack &amp; asset
          </Link>
        </div>
      </div>

      {semesters.length > 0 ? (
        <div className="ag-card ag-animate-in" style={{ padding: "1rem 1.25rem" }}>
          <div className="ag-field" style={{ maxWidth: 400 }}>
            <label className="ag-label" htmlFor="filter-semester">
              Lọc theo học kỳ
            </label>
            <select
              id="filter-semester"
              className="ag-input"
              value={semesterId}
              onChange={(e) => setSemesterId(e.target.value)}
            >
              <option value="">Tất cả học kỳ</option>
              {semesters.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.code} — {s.name}
                </option>
              ))}
            </select>
          </div>
        </div>
      ) : null}

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
                <th>Bắt đầu (UTC)</th>
                <th>Đóng nộp (UTC)</th>
                <th>Chủ đề / Câu</th>
                <th>Bài nộp</th>
                <th>Trạng thái (ước lượng)</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={9} className="ag-table__muted">
                    Đang tải…
                  </td>
                </tr>
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={9} className="ag-table__muted">
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
                    <td className="ag-table__muted">{new Date(row.startsAtUtc).toLocaleString("vi-VN")}</td>
                    <td className="ag-table__muted">{new Date(row.endsAtUtc).toLocaleString("vi-VN")}</td>
                    <td>
                      {row.topicCount} / {row.questionCount}
                    </td>
                    <td>{row.submissionCount}</td>
                    <td>
                      <SessionStatusBadge status={inferSessionStatus(row.startsAtUtc, row.endsAtUtc)} />
                    </td>
                    <td>
                      <Link to={`/exam-sessions/${row.id}`} className="ag-btn ag-btn--ghost" style={{ whiteSpace: "nowrap" }}>
                        Chi tiết đề
                      </Link>
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
