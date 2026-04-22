import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { listExamSessions, listSemesters } from "../api/gradingApi";
import { cmsCreateExamSession } from "../api/gradingCmsApi";
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
  const [cmsMsg, setCmsMsg] = useState<string | null>(null);
  const [createSemId, setCreateSemId] = useState("");
  const [createForm, setCreateForm] = useState({
    code: "",
    title: "",
    startsLocal: "",
    endsLocal: "",
    duration: 90,
  });

  const semesterFilter = useMemo(() => (semesterId || null) as string | null, [semesterId]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const sr = await listSemesters(token);
      if (cancelled) return;
      if (sr.isSuccess && sr.data) {
        setSemesters(sr.data);
        setCreateSemId((prev) => prev || sr.data![0]?.id || "");
      }
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

  async function onCreateSession(e: React.FormEvent) {
    e.preventDefault();
    setCmsMsg(null);
    if (!createSemId) {
      setCmsMsg("Chọn học kỳ.");
      return;
    }
    if (!createForm.startsLocal || !createForm.endsLocal) {
      setCmsMsg("Nhập thời gian bắt đầu và đóng nộp.");
      return;
    }
    const r = await cmsCreateExamSession(token, {
      semesterId: createSemId,
      code: createForm.code.trim(),
      title: createForm.title.trim(),
      startsAtUtc: new Date(createForm.startsLocal).toISOString(),
      examDurationMinutes: createForm.duration,
      endsAtUtc: new Date(createForm.endsLocal).toISOString(),
    });
    if (!r.isSuccess) setCmsMsg(r.message ?? "Lỗi");
    else {
      setCmsMsg(r.message ?? "Đã tạo ca — tải lại danh sách.");
      setCreateForm((f) => ({ ...f, code: "", title: "" }));
      const exRes = await listExamSessions(token, semesterFilter);
      if (exRes.isSuccess && exRes.data) setRows(exRes.data);
    }
  }

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Học kỳ → ca thi (GET / POST exam-sessions)</p>
          <p className="ag-table__muted" style={{ marginTop: 6 }}>
            Tạo ca mới bên dưới; cấu trúc đề (topic / question / testcase) trong trang chi tiết ca.
          </p>
        </div>
        <div className="ag-toolbar__actions">
          <Link to="/semesters" className="ag-btn ag-btn--secondary">
            Học kỳ
          </Link>
          <Link to="/grading-pack" className="ag-btn ag-btn--secondary">
            Pack (tài liệu)
          </Link>
        </div>
      </div>

      <section className="ag-card ag-animate-in" style={{ padding: "1rem 1.25rem" }}>
        <h3 className="ag-card__title" style={{ marginTop: 0 }}>
          Tạo ca thi
        </h3>
        {cmsMsg ? <p className="ag-table__muted">{cmsMsg}</p> : null}
        <form className="ag-stack ag-stack--sm" onSubmit={onCreateSession}>
          <div className="ag-field" style={{ maxWidth: 420 }}>
            <label className="ag-label" htmlFor="create-sem">
              Học kỳ
            </label>
            <select
              id="create-sem"
              className="ag-input"
              value={createSemId}
              onChange={(e) => setCreateSemId(e.target.value)}
            >
              {semesters.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.code} — {s.name}
                </option>
              ))}
            </select>
          </div>
          <div className="ag-upload-grid">
            <div className="ag-field">
              <label className="ag-label" htmlFor="sess-code">
                Mã ca
              </label>
              <input
                id="sess-code"
                className="ag-input"
                value={createForm.code}
                onChange={(e) => setCreateForm((f) => ({ ...f, code: e.target.value }))}
                required
              />
            </div>
            <div className="ag-field">
              <label className="ag-label" htmlFor="sess-title">
                Tên
              </label>
              <input
                id="sess-title"
                className="ag-input"
                value={createForm.title}
                onChange={(e) => setCreateForm((f) => ({ ...f, title: e.target.value }))}
                required
              />
            </div>
          </div>
          <div className="ag-upload-grid">
            <div className="ag-field">
              <label className="ag-label" htmlFor="sess-start">
                Bắt đầu
              </label>
              <input
                id="sess-start"
                type="datetime-local"
                className="ag-input"
                value={createForm.startsLocal}
                onChange={(e) => setCreateForm((f) => ({ ...f, startsLocal: e.target.value }))}
                required
              />
            </div>
            <div className="ag-field">
              <label className="ag-label" htmlFor="sess-end">
                Đóng nộp
              </label>
              <input
                id="sess-end"
                type="datetime-local"
                className="ag-input"
                value={createForm.endsLocal}
                onChange={(e) => setCreateForm((f) => ({ ...f, endsLocal: e.target.value }))}
                required
              />
            </div>
            <div className="ag-field">
              <label className="ag-label" htmlFor="sess-dur">
                Phút làm bài
              </label>
              <input
                id="sess-dur"
                type="number"
                min={1}
                className="ag-input"
                value={createForm.duration}
                onChange={(e) => setCreateForm((f) => ({ ...f, duration: Number(e.target.value) }))}
              />
            </div>
          </div>
          <button type="submit" className="ag-btn ag-btn--primary">
            POST tạo ca
          </button>
        </form>
      </section>

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
                      <div style={{ display: "flex", gap: 8, flexWrap: "wrap", justifyContent: "flex-end" }}>
                        <Link
                          to={`/submissions?examSessionId=${encodeURIComponent(row.id)}`}
                          className="ag-btn ag-btn--ghost"
                          style={{ whiteSpace: "nowrap" }}
                        >
                          Bài nộp
                        </Link>
                        <Link
                          to={`/exam-sessions/${row.id}`}
                          className="ag-btn ag-btn--ghost"
                          style={{ whiteSpace: "nowrap" }}
                        >
                          Chi tiết đề
                        </Link>
                      </div>
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
