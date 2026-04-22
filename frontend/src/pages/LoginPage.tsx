import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { useApiMock } from "../config/env";

export function LoginPage() {
  const { login } = useAuth();
  const apiMock = useApiMock();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const r = await login(email.trim(), password);
      if (r.ok) navigate("/", { replace: true });
      else setError(r.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="ag-auth">
      <div className="ag-auth__hero">
        <div className="ag-auth__mesh" aria-hidden />
        <div className="ag-auth__hero-inner">
          <div className="ag-auth__brandmark">
            <span className="ag-logo ag-logo--xl" aria-hidden />
          </div>
          <h1 className="ag-auth__headline">Chấm bài tự động</h1>
          <p className="ag-auth__lede">
            Chấm bài zip PRN232 tự động — theo dõi từng bài kiểm tra, hàng đợi chấm và kết quả theo từng ca thi trong một
            giao diện thống nhất.
          </p>
          <ul className="ag-auth__bullets">
            <li>Hai tệp zip (câu 1 và câu 2) cho mỗi lần nộp</li>
            <li>Trạng thái chấm và điểm theo đúng đề thi</li>
            <li>Đăng nhập bảo mật cho tài khoản quản trị</li>
          </ul>
        </div>
      </div>

      <div className="ag-auth__panel">
        <div className="ag-auth__card">
          <p className="ag-auth__eyebrow">PRN232 · Nhóm 9</p>
          <h2 className="ag-auth__title">Đăng nhập quản trị</h2>
          <p className="ag-auth__hint">Dùng tài khoản seed trên môi trường dev / Docker.</p>

          <form className="ag-auth__form" onSubmit={onSubmit}>
            <div className="ag-field">
              <label className="ag-label" htmlFor="login-email">
                Email
              </label>
              <input
                id="login-email"
                className="ag-input"
                type="email"
                autoComplete="username"
                placeholder="admin@autogradingtool.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div className="ag-field">
              <label className="ag-label" htmlFor="login-password">
                Mật khẩu
              </label>
              <input
                id="login-password"
                className="ag-input"
                type="password"
                autoComplete="current-password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>

            {error ? (
              <div className="ag-alert ag-alert--err" role="alert">
                {error}
              </div>
            ) : null}

            <button type="submit" className="ag-btn ag-btn--primary ag-btn--block ag-btn--lg" disabled={loading}>
              {loading ? "Đang đăng nhập…" : "Đăng nhập"}
            </button>
          </form>

          <p className="ag-auth__fineprint">
            © PRN232 G9 —{" "}
            {apiMock
              ? "Đang bật chế độ thử: mọi email và mật khẩu đúng định dạng đều có thể đăng nhập để xem giao diện."
              : "Dùng tài khoản do quản trị hệ thống cấp."}
          </p>
        </div>
      </div>
    </div>
  );
}
