import { useEffect, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { getExamSession, listExamSessions, listSemesters } from "../api/gradingApi";
import type { ExamSessionDetail, ExamSessionListItem, SemesterListItem } from "../api/gradingTypes";
import { SessionStatusBadge } from "../components/StatusBadge";
import { inferSessionStatus } from "../lib/gradingUi";

export function ExamSessionsPage() {
  const { token } = useAuth();
  const [semesters, setSemesters] = useState<SemesterListItem[]>([]);
  const [rows, setRows] = useState<ExamSessionListItem[]>([]);
  const [detail, setDetail] = useState<ExamSessionDetail | null>(null);
  const [detailErr, setDetailErr] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setErr(null);
      const [semRes, exRes] = await Promise.all([listSemesters(token), listExamSessions(token, null)]);
      if (cancelled) return;
      if (semRes.isSuccess && semRes.data) setSemesters(semRes.data);
      if (!exRes.isSuccess || !exRes.data) setErr(exRes.message ?? "Không tải được danh sách ca thi");
      else setRows(exRes.data);
      setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [token]);

  useEffect(() => {
    const id = rows[0]?.id;
    if (!id) {
      setDetail(null);
      return;
    }
    let cancelled = false;
    (async () => {
      setDetailErr(null);
      const r = await getExamSession(token, id);
      if (cancelled) return;
      if (!r.isSuccess || !r.data) setDetailErr(r.message ?? "Không tải chi tiết ca thi");
      else setDetail(r.data);
    })();
    return () => {
      cancelled = true;
    };
  }, [token, rows]);

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-toolbar">
        <div>
          <p className="ag-toolbar__lead">Semester → ExamSession → Topic → Question</p>
          {semesters.length > 0 ? (
            <p className="ag-table__muted" style={{ marginTop: 6 }}>
              GET semesters:{" "}
              {semesters.map((s) => (
                <code key={s.id} className="ag-code ag-code--sm" style={{ marginRight: 6 }}>
                  {s.code}
                </code>
              ))}
            </p>
          ) : null}
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
                    <td className="ag-table__muted">{new Date(row.startsAtUtc).toLocaleString("vi-VN")}</td>
                    <td>
                      {row.topicCount} / {row.questionCount}
                    </td>
                    <td>{row.submissionCount}</td>
                    <td>
                      <SessionStatusBadge status={inferSessionStatus(row.startsAtUtc, row.endsAtUtc)} />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {detailErr ? (
        <div className="ag-alert ag-alert--err" role="alert">
          {detailErr}
        </div>
      ) : null}

      {detail ? (
        <section className="ag-card ag-animate-in">
          <div className="ag-card__head">
            <h2 className="ag-card__title">Cấu trúc đề (GET exam-sessions/{"{id}"})</h2>
            <p className="ag-card__desc">
              <code className="ag-code">{detail.code}</code> · {detail.title}
            </p>
          </div>
          <ul className="ag-stack ag-stack--sm" style={{ listStyle: "none", padding: 0, margin: 0 }}>
            {detail.topics.map((t) => (
              <li key={t.id} className="ag-card" style={{ padding: "0.75rem 1rem" }}>
                <strong>{t.title}</strong>
                <span className="ag-table__muted"> · thứ tự {t.sortOrder}</span>
                <ul className="ag-stack ag-stack--sm" style={{ marginTop: "0.5rem", paddingLeft: "1.1rem" }}>
                  {t.questions.map((q) => (
                    <li key={q.id}>
                      <span className="ag-qtag">{q.label}</span> {q.title}{" "}
                      <span className="ag-table__muted">(tối đa {q.maxScore} điểm)</span>
                      <ul style={{ marginTop: "0.25rem", paddingLeft: "1rem", fontSize: "0.88rem" }}>
                        {q.testCases.map((tc) => (
                          <li key={tc.id}>
                            {tc.name} — {tc.maxPoints} điểm
                          </li>
                        ))}
                      </ul>
                    </li>
                  ))}
                </ul>
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </div>
  );
}
