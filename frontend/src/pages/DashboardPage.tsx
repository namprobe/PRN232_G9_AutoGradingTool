import { useAuth } from "../auth/AuthContext";
import { Link } from "react-router-dom";

/**
 * Trang hub sau login. Upload Q1/Q2 sẽ nối API khi Person 2 xong endpoint.
 */
export function DashboardPage() {
  const { user, logout } = useAuth();

  return (
    <div className="shell">
      <header className="topbar">
        <strong>PRN232 G9 — Auto Grading</strong>
        <nav>
          <Link to="/">Trang chủ</Link>
          <Link to="/submissions/upload">Nộp bài (zip)</Link>
        </nav>
        <button type="button" className="btn-ghost" onClick={logout}>
          Đăng xuất
        </button>
      </header>
      <main className="main">
        <h2>Dashboard</h2>
        {user ? (
          <p className="muted">
            Token hết hạn (UTC): <code>{user.expiresAt}</code>
          </p>
        ) : null}
        <section className="card">
          <h3>Tiếp theo (Person 2)</h3>
          <ul>
            <li>API danh sách ExamSession / Submit</li>
            <li>POST upload 2 file zip (Q1, Q2)</li>
            <li>Trang chi tiết submit + điểm theo testcase</li>
          </ul>
        </section>
      </main>
    </div>
  );
}
