import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { listSemesters } from "../api/gradingApi";
import type { SemesterListItem } from "../api/gradingTypes";

export function SemestersPage() {
  const { token } = useAuth();
  const [rows, setRows] = useState<SemesterListItem[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setErr(null);
      const r = await listSemesters(token);
      if (cancelled) return;
      if (!r.isSuccess || !r.data) setErr(r.message ?? "Không tải được học kỳ");
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
          <p className="ag-toolbar__lead">Học kỳ (GET /api/cms/grading/semesters)</p>
          <p className="ag-table__muted" style={{ marginTop: 6 }}>
            Backend hiện chỉ hỗ trợ đọc danh sách. Tạo / sửa học kỳ qua migration, seeder hoặc SQL — khi nhóm bổ sung API
            POST/PUT, form tạo sẽ gắn vào đây.
          </p>
        </div>
        <div className="ag-toolbar__actions">
          <Link to="/exam-sessions" className="ag-btn ag-btn--secondary">
            Ca thi
          </Link>
        </div>
      </div>

      <div className="ag-alert ag-alert--info" role="status">
        Luồng CMS: <strong>Học kỳ</strong> → <strong>Ca thi</strong> → chủ đề &amp; câu (chi tiết ca) → bài nộp.
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
                <th>Tên</th>
                <th>Bắt đầu (UTC)</th>
                <th>Kết thúc (UTC)</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={4} className="ag-table__muted">
                    Đang tải…
                  </td>
                </tr>
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={4} className="ag-table__muted">
                    Chưa có học kỳ
                  </td>
                </tr>
              ) : (
                rows.map((s) => (
                  <tr key={s.id}>
                    <td>
                      <code className="ag-code">{s.code}</code>
                    </td>
                    <td className="ag-table__strong">{s.name}</td>
                    <td className="ag-table__muted">
                      {s.startDateUtc ? new Date(s.startDateUtc).toLocaleString("vi-VN") : "—"}
                    </td>
                    <td className="ag-table__muted">
                      {s.endDateUtc ? new Date(s.endDateUtc).toLocaleString("vi-VN") : "—"}
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
