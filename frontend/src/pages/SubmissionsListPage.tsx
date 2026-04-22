import { Link, useSearchParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { listExamSessions, listSubmissions } from "../api/gradingApi";
import type { ExamSessionListItem, ExamSubmissionListItem } from "../api/gradingTypes";
import { StatusBadge } from "../components/StatusBadge";
import { listItemMaxScore, workflowToQPair } from "../lib/gradingUi";

export function SubmissionsListPage() {
  const { token } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const urlSessionId = searchParams.get("examSessionId");
  const [sessions, setSessions] = useState<ExamSessionListItem[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [rows, setRows] = useState<ExamSubmissionListItem[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const sr = await listExamSessions(token, null);
      if (cancelled) return;
      if (sr.isSuccess && sr.data?.length) {
        setSessions(sr.data);
        const list = sr.data;
        const fromUrl = urlSessionId && list.some((s) => s.id === urlSessionId) ? urlSessionId : null;
        setSessionId((prev) => {
          if (fromUrl) return fromUrl;
          if (prev && list.some((s) => s.id === prev)) return prev;
          return list[0]!.id;
        });
      } else {
        setSessions([]);
        setSessionId("");
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [token, urlSessionId]);

  useEffect(() => {
    if (!sessionId) {
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
  }, [token, sessionId]);

  function onSessionChange(nextId: string) {
    setSessionId(nextId);
    const p = new URLSearchParams(searchParams);
    if (nextId) p.set("examSessionId", nextId);
    else p.delete("examSessionId");
    setSearchParams(p, { replace: true });
  }

  const sessionCode = sessions.find((s) => s.id === sessionId)?.code ?? "—";

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Lọc theo ca thi (GET submissions?examSessionId=…)</p>
          {sessions.length > 0 ? (
            <div className="ag-field" style={{ marginTop: 8, maxWidth: 360 }}>
              <label className="ag-label" htmlFor="filter-session">
                Ca thi
              </label>
              <select
                id="filter-session"
                className="ag-input"
                value={sessionId}
                onChange={(e) => onSessionChange(e.target.value)}
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
          <Link to="/submissions/upload" className="ag-btn ag-btn--primary">
            Tải lên zip mới
          </Link>
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
              {loading ? (
                <tr>
                  <td colSpan={8} className="ag-table__muted">
                    Đang tải…
                  </td>
                </tr>
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={8} className="ag-table__muted">
                    Chưa có bài nộp cho ca này
                  </td>
                </tr>
              ) : (
                rows.map((s) => {
                  const { q1, q2 } = workflowToQPair(s.status);
                  const max = listItemMaxScore();
                  return (
                    <tr key={s.id}>
                      <td>
                        <code className="ag-code">{s.studentCode}</code>
                      </td>
                      <td className="ag-table__strong">{s.studentName ?? "—"}</td>
                      <td>
                        <code className="ag-code ag-code--sm">{sessionCode}</code>
                      </td>
                      <td className="ag-table__muted">{new Date(s.submittedAtUtc).toLocaleString("vi-VN")}</td>
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
    </div>
  );
}
