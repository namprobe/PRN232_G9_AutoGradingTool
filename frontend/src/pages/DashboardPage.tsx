import { Link } from "react-router-dom";
import { useEffect, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { listExamSessions, listSubmissions } from "../api/gradingApi";
import { inferSessionStatus, workflowToQPair } from "../lib/gradingUi";

export function DashboardPage() {
  const { token } = useAuth();
  const [activeSessions, setActiveSessions] = useState(0);
  const [totalSubs, setTotalSubs] = useState(0);
  const [graded, setGraded] = useState(0);
  const [inQueue, setInQueue] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      const es = await listExamSessions(token, null);
      const firstSessionId = es.isSuccess && es.data?.length ? es.data[0]!.id : "";
      const subs = firstSessionId ? await listSubmissions(token, firstSessionId) : { isSuccess: false as const, data: undefined };
      if (cancelled) return;
      if (es.isSuccess && es.data) {
        setActiveSessions(es.data.filter((x) => inferSessionStatus(x.startsAtUtc, x.endsAtUtc) === "active").length);
      }
      if (subs.isSuccess && subs.data) {
        setTotalSubs(subs.data.length);
        setGraded(subs.data.filter((s) => s.status === "Completed").length);
        setInQueue(
          subs.data.filter((s) => {
            const { q1, q2 } = workflowToQPair(s.status);
            return q1 === "grading" || q2 === "grading" || q1 === "pending" || q2 === "pending";
          }).length
        );
      }
      setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [token]);

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-stat-grid">
        <div className="ag-stat">
          <div className="ag-stat__label">Ca thi đang mở (ước lượng)</div>
          <div className="ag-stat__value">{loading ? "…" : activeSessions}</div>
          <div className="ag-stat__hint">Theo lịch ca thi</div>
        </div>
        <div className="ag-stat ag-stat--accent">
          <div className="ag-stat__label">Bài nộp (ca đầu tiên)</div>
          <div className="ag-stat__value">{loading ? "…" : totalSubs}</div>
          <div className="ag-stat__hint">Zip Q1 + Q2</div>
        </div>
        <div className="ag-stat">
          <div className="ag-stat__label">Đã chấm xong</div>
          <div className="ag-stat__value">{loading ? "…" : graded}</div>
          <div className="ag-stat__hint">Pipeline Completed</div>
        </div>
        <div className="ag-stat">
          <div className="ag-stat__label">Chờ / đang chấm</div>
          <div className="ag-stat__value">{loading ? "…" : inQueue}</div>
          <div className="ag-stat__hint">Theo cột Q1–Q2</div>
        </div>
      </div>

      <div className="ag-grid2">
        <section className="ag-card ag-animate-in">
          <div className="ag-card__head">
            <h2 className="ag-card__title">Thao tác nhanh</h2>
            <p className="ag-card__desc">Luồng giảng viên thường dùng</p>
          </div>
          <div className="ag-quick-actions">
            <Link className="ag-tile" to="/system-flows">
              <span className="ag-tile__icon" aria-hidden>
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                  <circle cx="5" cy="12" r="2" />
                  <circle cx="12" cy="7" r="2" />
                  <circle cx="19" cy="12" r="2" />
                  <circle cx="12" cy="17" r="2" />
                  <path d="M7 11l3-2.5M14 9.5l3 2.5M17 13l-3 2.5M10 15.5L7 13" />
                </svg>
              </span>
              <span className="ag-tile__text">
                <span className="ag-tile__title">Luồng &amp; entity</span>
                <span className="ag-tile__sub">Đối chiếu SYSTEM_FLOWS</span>
              </span>
            </Link>
            <Link className="ag-tile" to="/submissions/upload">
              <span className="ag-tile__icon" aria-hidden>
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                  <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" />
                </svg>
              </span>
              <span className="ag-tile__text">
                <span className="ag-tile__title">Tải zip Q1 & Q2</span>
                <span className="ag-tile__sub">POST /api/cms/grading/submissions</span>
              </span>
            </Link>
            <Link className="ag-tile" to="/semesters">
              <span className="ag-tile__icon" aria-hidden>
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                  <path d="M4 19.5A2.5 2.5 0 016.5 17H20" />
                  <path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z" />
                </svg>
              </span>
              <span className="ag-tile__text">
                <span className="ag-tile__title">Học kỳ</span>
                <span className="ag-tile__sub">GET semesters</span>
              </span>
            </Link>
            <Link className="ag-tile" to="/exam-sessions">
              <span className="ag-tile__icon" aria-hidden>
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                  <rect x="3" y="4" width="18" height="18" rx="2" />
                  <path d="M16 2v4M8 2v4M3 10h18" />
                </svg>
              </span>
              <span className="ag-tile__text">
                <span className="ag-tile__title">Quản lý ca thi</span>
                <span className="ag-tile__sub">GET exam-sessions</span>
              </span>
            </Link>
            <Link className="ag-tile" to="/submissions">
              <span className="ag-tile__icon" aria-hidden>
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                  <path d="M22 12h-6l-2 3H10L8 12H2" />
                  <path d="M5.45 5.11L2 12v6a2 2 0 002 2h16a2 2 0 002-2v-6l-3.45-6.89A2 2 0 0016.76 4H7.24a2 2 0 00-1.79 1.11z" />
                </svg>
              </span>
              <span className="ag-tile__text">
                <span className="ag-tile__title">Theo dõi bài nộp</span>
                <span className="ag-tile__sub">GET submissions</span>
              </span>
            </Link>
          </div>
        </section>

        <section className="ag-card ag-animate-in ag-animate-in--delay">
          <div className="ag-card__head">
            <h2 className="ag-card__title">Hoạt động gần đây</h2>
            <p className="ag-card__desc">Tóm tắt từ API / mock</p>
          </div>
          <ul className="ag-timeline">
            <li className="ag-timeline__item">
              <span className="ag-timeline__dot ag-timeline__dot--ok" />
              <div>
                <strong>Chấm xong</strong> · HE186501 · PRN232-DEMO-PE
                <div className="ag-timeline__meta">GET submissions + seed</div>
              </div>
            </li>
            <li className="ag-timeline__item">
              <span className="ag-timeline__dot ag-timeline__dot--info" />
              <div>
                <strong>Đang chấm</strong> · HE186502
                <div className="ag-timeline__meta">Pipeline Running (mock)</div>
              </div>
            </li>
            <li className="ag-timeline__item">
              <span className="ag-timeline__dot ag-timeline__dot--muted" />
              <div>
                <strong>Swagger</strong> · nhóm CMS_Grading
                <div className="ag-timeline__meta">http://localhost:5000/swagger</div>
              </div>
            </li>
          </ul>
        </section>
      </div>
    </div>
  );
}
