import { Link } from "react-router-dom";
import { MOCK_EXAM_SESSIONS, MOCK_SUBMISSIONS } from "../mock/cmsMockData";

export function DashboardPage() {
  const activeSessions = MOCK_EXAM_SESSIONS.filter((e) => e.status === "active").length;
  const totalSubs = MOCK_SUBMISSIONS.length;
  const graded = MOCK_SUBMISSIONS.filter((s) => s.q1Status === "graded" && s.q2Status === "graded").length;
  const inQueue = MOCK_SUBMISSIONS.filter((s) => s.q1Status === "grading" || s.q2Status === "grading" || s.q1Status === "pending").length;

  return (
    <div className="ag-stack ag-stack--lg">
      <div className="ag-stat-grid">
        <div className="ag-stat">
          <div className="ag-stat__label">Ca thi đang mở</div>
          <div className="ag-stat__value">{activeSessions}</div>
          <div className="ag-stat__hint">Theo dữ liệu mẫu</div>
        </div>
        <div className="ag-stat ag-stat--accent">
          <div className="ag-stat__label">Bài nộp (mẫu)</div>
          <div className="ag-stat__value">{totalSubs}</div>
          <div className="ag-stat__hint">Zip Q1 + Q2</div>
        </div>
        <div className="ag-stat">
          <div className="ag-stat__label">Đã chấm xong</div>
          <div className="ag-stat__value">{graded}</div>
          <div className="ag-stat__hint">Q1 & Q2 graded</div>
        </div>
        <div className="ag-stat">
          <div className="ag-stat__label">Trong hàng đợi</div>
          <div className="ag-stat__value">{inQueue}</div>
          <div className="ag-stat__hint">Hangfire (sau này)</div>
        </div>
      </div>

      <div className="ag-grid2">
        <section className="ag-card ag-animate-in">
          <div className="ag-card__head">
            <h2 className="ag-card__title">Thao tác nhanh</h2>
            <p className="ag-card__desc">Luồng giảng viên thường dùng</p>
          </div>
          <div className="ag-quick-actions">
            <Link className="ag-tile" to="/submissions/upload">
              <span className="ag-tile__icon" aria-hidden>
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                  <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" />
                </svg>
              </span>
              <span className="ag-tile__text">
                <span className="ag-tile__title">Tải zip Q1 & Q2</span>
                <span className="ag-tile__sub">Gửi bài thi thử nghiệm</span>
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
                <span className="ag-tile__sub">Xem phiên & chủ đề</span>
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
                <span className="ag-tile__sub">Trạng thái & điểm</span>
              </span>
            </Link>
          </div>
        </section>

        <section className="ag-card ag-animate-in ag-animate-in--delay">
          <div className="ag-card__head">
            <h2 className="ag-card__title">Hoạt động gần đây</h2>
            <p className="ag-card__desc">Bản ghi mẫu — API sẽ thay thế</p>
          </div>
          <ul className="ag-timeline">
            <li className="ag-timeline__item">
              <span className="ag-timeline__dot ag-timeline__dot--ok" />
              <div>
                <strong>Chấm xong</strong> · HE186501 · PRN232-PE-2026
                <div className="ag-timeline__meta">15/04/2026 — 8.5 / 10</div>
              </div>
            </li>
            <li className="ag-timeline__item">
              <span className="ag-timeline__dot ag-timeline__dot--info" />
              <div>
                <strong>Đang chấm Q2</strong> · HE186502
                <div className="ag-timeline__meta">Hangfire job #mock-2042</div>
              </div>
            </li>
            <li className="ag-timeline__item">
              <span className="ag-timeline__dot ag-timeline__dot--err" />
              <div>
                <strong>Lỗi build Q1</strong> · HE186503
                <div className="ag-timeline__meta">dotnet restore — exit code 1 (mẫu)</div>
              </div>
            </li>
          </ul>
        </section>
      </div>
    </div>
  );
}
