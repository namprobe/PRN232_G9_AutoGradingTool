import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { listExamSessions, listSubmissions } from "../api/gradingApi";
import type { ExamSessionListItem, ExamSubmissionListItem } from "../api/gradingTypes";
import { WorkflowBreadcrumb, crumbsForSubmissions } from "../components/WorkflowBreadcrumb";
import { examSessionDetailPath, examSessionSubmissionsPath, examSessionUploadPath } from "../lib/workflowRoutes";
import { StatusBadge } from "../components/StatusBadge";
import { listItemMaxScore, workflowToQPair } from "../lib/gradingUi";
import { formatDateTime } from "../lib/format";

export function SubmissionsListPage() {
  const { token } = useAuth();
  const navigate = useNavigate();
  const { sessionId: routeSessionId } = useParams<{ sessionId?: string }>();
  const [searchParams] = useSearchParams();
  const querySessionId = searchParams.get("examSessionId");

  const [sessions, setSessions] = useState<ExamSessionListItem[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [rows, setRows] = useState<ExamSubmissionListItem[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [sessionsLoading, setSessionsLoading] = useState(true);
  const [sessionsErr, setSessionsErr] = useState<string | null>(null);

  useEffect(() => {
    if (querySessionId && !routeSessionId) {
      navigate(`/exam-sessions/${encodeURIComponent(querySessionId)}/submissions`, { replace: true });
    }
  }, [querySessionId, routeSessionId, navigate]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setSessionsLoading(true);
      setSessionsErr(null);
      const sr = await listExamSessions(token, null);
      if (cancelled) return;
      if (sr.isSuccess && sr.data) {
        setSessions(sr.data);
        setSessionsErr(null);
      } else {
        setSessions([]);
        setSessionsErr(sr.message ?? "Không tải được danh sách ca thi.");
      }
      setSessionsLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [token]);

  useEffect(() => {
    const fromRoute = routeSessionId?.trim() ?? "";
    if (fromRoute) {
      setSessionId(fromRoute);
      return;
    }
    setSessionId("");
  }, [routeSessionId]);

  useEffect(() => {
    if (!sessionId) {
      setRows([]);
      setLoading(false);
      setErr(null);
      return;
    }
    if (sessionsLoading) return;
    if (sessions.length > 0 && !sessions.some((s) => s.id === sessionId)) {
      setErr("Ca thi không tồn tại hoặc đã bị xoá.");
      setRows([]);
      setLoading(false);
      return;
    }
    let cancelled = false;
    (async () => {
      setLoading(true);
      setErr(null);
      const r = await listSubmissions(token, sessionId);
      if (cancelled) return;
      if (!r.isSuccess || !r.data) setErr(r.message ?? "Không tải được bài nộp");
      else setRows(r.data);
      setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [token, sessionId, sessions, sessionsLoading]);

  const sessionMeta = sessions.find((s) => s.id === sessionId);
  const sessionCode = sessionMeta?.code ?? "—";
  const isHub = !routeSessionId;

  return (
    <div className="ag-stack ag-stack--lg">
      {sessionsErr ? (
        <div className="ag-alert ag-alert--err" role="alert">
          {sessionsErr}
        </div>
      ) : null}
      {!isHub && sessionMeta ? (
        <WorkflowBreadcrumb items={crumbsForSubmissions(sessionCode, sessionId)} />
      ) : (
        <WorkflowBreadcrumb
          items={[
            { label: "Tổng quan", to: "/" },
            { label: "Bài nộp" },
          ]}
        />
      )}

      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">
            {isHub
              ? "Chọn ca thi để xem danh sách bài nộp (hai tệp zip mỗi bài)"
              : `Bài nộp của ca ${sessionCode}`}
          </p>
          {!isHub && sessions.length > 0 ? (
            <div className="ag-field" style={{ marginTop: 8, maxWidth: 420 }}>
              <label className="ag-label" htmlFor="jump-session-sub">
                Đổi nhanh ca thi
              </label>
              <select
                id="jump-session-sub"
                className="ag-input"
                value={sessionId}
                onChange={(e) => navigate(examSessionSubmissionsPath(e.target.value))}
              >
                {sessions.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.code} — {s.title}
                  </option>
                ))}
              </select>
            </div>
          ) : null}
        </div>
        <div className="ag-toolbar__actions">
          {!isHub && sessionId ? (
            <>
              <Link to={examSessionDetailPath(sessionId)} className="ag-btn ag-btn--secondary">
                Cấu hình ca
              </Link>
              <Link to={examSessionUploadPath(sessionId)} className="ag-btn ag-btn--primary">
                Nộp ZIP
              </Link>
            </>
          ) : (
            <Link to="/exam-sessions" className="ag-btn ag-btn--secondary">
              Danh sách ca thi
            </Link>
          )}
        </div>
      </div>

      {isHub && !sessionsLoading && sessions.length === 0 ? (
        <div className="ag-card ag-animate-in" style={{ padding: "1.25rem" }}>
          <p className="ag-empty__text">Chưa có ca thi nào. Tạo học kỳ và ca thi trước.</p>
          <div className="ag-stack ag-stack--sm" style={{ marginTop: 12 }}>
            <Link className="ag-btn ag-btn--secondary" to="/semesters">
              Học kỳ
            </Link>
            <Link className="ag-btn ag-btn--primary" to="/exam-sessions">
              Ca thi
            </Link>
          </div>
        </div>
      ) : null}

      {isHub && sessions.length > 0 ? (
        <section className="ag-card ag-animate-in" style={{ padding: "1rem 1.25rem" }}>
          <h3 className="ag-card__title" style={{ marginTop: 0 }}>
            Chọn ca thi
          </h3>
          <p className="ag-card__desc">Mỗi ca có danh sách bài nộp và trang nộp ZIP riêng — tránh nhầm phiên.</p>
          <ul className="ag-stack ag-stack--sm" style={{ listStyle: "none", padding: 0, margin: "0.75rem 0 0" }}>
            {sessions.map((s) => (
              <li key={s.id}>
                <Link className="ag-linkbtn" to={examSessionSubmissionsPath(s.id)} style={{ fontWeight: 600 }}>
                  {s.code}
                </Link>
                <span className="ag-table__muted" style={{ marginLeft: 8 }}>
                  {s.title} · {s.submissionCount} bài
                </span>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {!isHub && err ? (
        <div className="ag-alert ag-alert--err" role="alert">
          {err}
        </div>
      ) : null}

      {!isHub ? (
        <div className="ag-card ag-card--flush ag-animate-in">
          <div className="ag-table-wrap">
            <table className="ag-table">
              <thead>
                <tr>
                  <th>MSSV</th>
                  <th>Họ tên</th>
                  <th>Ca thi</th>
                  <th>Thời điểm nộp</th>
                  <th>Câu 1</th>
                  <th>Câu 2</th>
                  <th>Điểm</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={8} className="ag-table__muted">
                      Đang tải…
                    </td>
                  </tr>
                ) : rows.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="ag-table__muted">
                      Chưa có bài nộp cho ca này — dùng nút «Nộp ZIP» hoặc cổng dành cho thí sinh (nếu được bật).
                    </td>
                  </tr>
                ) : (
                  rows.map((s) => {
                    const { q1, q2 } = workflowToQPair(s.status);
                    const max = listItemMaxScore();
                    return (
                      <tr key={s.id}>
                        <td>
                          <span className="ag-table__strong">{s.studentCode}</span>
                        </td>
                        <td className="ag-table__strong">{s.studentName ?? "—"}</td>
                        <td>
                          <span className="ag-table__strong">{sessionCode}</span>
                        </td>
                        <td className="ag-table__muted">{formatDateTime(s.submittedAtUtc)}</td>
                        <td>
                          <StatusBadge status={q1} />
                        </td>
                        <td>
                          <StatusBadge status={q2} />
                        </td>
                        <td>
                          {s.totalScore != null ? (
                            <span className="ag-score">
                              {s.totalScore}
                              <span className="ag-score__max">/{max}</span>
                            </span>
                          ) : (
                            <span className="ag-table__muted">—</span>
                          )}
                        </td>
                        <td className="ag-table__actions">
                          <Link
                            className="ag-linkbtn"
                            to={`/submissions/${s.id}`}
                            state={{ fromSessionId: sessionId }}
                          >
                            Chi tiết
                          </Link>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </div>
  );
}
