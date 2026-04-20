import type { ReactElement } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { useApiMock } from "../config/env";

function useHeaderMeta(): { title: string; subtitle: string } {
  const { pathname } = useLocation();
  if (pathname === "/") return { title: "Tổng quan", subtitle: "Thống kê nhanh và trạng thái pipeline chấm bài" };
  if (pathname === "/semesters") return { title: "Học kỳ", subtitle: "Danh sách semester từ API CMS" };
  if (pathname === "/system-flows")
    return { title: "Luồng hệ thống", subtitle: "Entity + bốn luồng chính (SYSTEM_FLOWS.md)" };
  if (pathname === "/grading-pack") return { title: "Pack chấm & asset", subtitle: "Tài liệu — REST pack sắp có" };
  if (pathname.startsWith("/exam-sessions/")) return { title: "Chi tiết ca thi", subtitle: "Topic, câu hỏi và testcase" };
  if (pathname === "/exam-sessions") return { title: "Ca thi", subtitle: "Kỳ thi — phiên — chủ đề & câu hỏi" };
  if (pathname === "/submissions") return { title: "Bài nộp", subtitle: "Danh sách zip đã gửi và trạng thái chấm" };
  if (pathname === "/submissions/upload") return { title: "Tải lên bài thi", subtitle: "Hai file zip riêng cho Q1 và Q2" };
  if (/^\/submissions\/[^/]+$/.test(pathname))
    return { title: "Chi tiết bài nộp", subtitle: "WorkflowStatus, submissionFiles, điểm câu & testcase" };
  return { title: "CMS", subtitle: "Auto Grading Tool" };
}

type NavItem = {
  to: string;
  end?: boolean;
  label: string;
  icon: () => ReactElement;
  /** Khi có: dùng thay vì mặc định của NavLink (vd highlight cả chi tiết bài nộp) */
  isActive?: (pathname: string) => boolean;
};

const nav: NavItem[] = [
  { to: "/", end: true, label: "Tổng quan", icon: IconHome },
  { to: "/system-flows", end: true, label: "Luồng hệ thống", icon: IconFlow },
  { to: "/semesters", end: true, label: "Học kỳ", icon: IconBook },
  {
    to: "/exam-sessions",
    end: false,
    label: "Ca thi",
    icon: IconCalendar,
    isActive: (p) => p === "/exam-sessions" || p.startsWith("/exam-sessions/"),
  },
  { to: "/grading-pack", end: true, label: "Pack & asset", icon: IconBox },
  {
    to: "/submissions",
    end: true,
    label: "Bài nộp",
    icon: IconInbox,
    isActive: (p) => p === "/submissions" || (p.startsWith("/submissions/") && !p.startsWith("/submissions/upload")),
  },
  { to: "/submissions/upload", end: false, label: "Tải lên zip", icon: IconUpload },
];

export function AppShell() {
  const { user, logout } = useAuth();
  const apiMock = useApiMock();
  const { pathname } = useLocation();
  const { title, subtitle } = useHeaderMeta();
  const exp = user?.expiresAt
    ? new Date(user.expiresAt).toLocaleString("vi-VN", { dateStyle: "medium", timeStyle: "short" })
    : "—";

  return (
    <div className="ag-app">
      <aside className="ag-sidebar" aria-label="Điều hướng chính">
        <div className="ag-sidebar__brand">
          <span className="ag-logo" aria-hidden />
          <div>
            <div className="ag-sidebar__title">Auto Grading</div>
            <div className="ag-sidebar__sub">PRN232 · G9 CMS</div>
          </div>
        </div>
        <nav className="ag-sidebar__nav">
          {nav.map(({ to, end, label, icon: Icon, isActive: customActive }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              title={label}
              className={({ isActive }) => {
                const on = customActive ? customActive(pathname) : isActive;
                return "ag-navlink" + (on ? " ag-navlink--active" : "");
              }}
            >
              <Icon />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="ag-sidebar__foot">
          <span className="ag-chip ag-chip--muted">{apiMock ? "Mock API (dev)" : "API thật"}</span>
          <p className="ag-sidebar__hint">
            {apiMock
              ? "VITE_USE_API_MOCK=true — auth + grading trả dữ liệu local."
              : "Gọi backend theo VITE_API_BASE_URL / cùng origin."}
          </p>
        </div>
      </aside>
      <div className="ag-app__main">
        <header className="ag-header">
          <div className="ag-header__titles">
            <h1 className="ag-header__page">{title}</h1>
            <p className="ag-header__crumb">{subtitle}</p>
          </div>
          <div className="ag-header__meta">
            <div className="ag-session-pill" title="JWT hết hạn">
              <span className="ag-session-pill__k">Phiên</span>
              <span className="ag-session-pill__v">{exp}</span>
            </div>
            <button type="button" className="ag-btn ag-btn--ghost" onClick={logout}>
              Đăng xuất
            </button>
          </div>
        </header>
        <div className="ag-content">
          <Outlet />
        </div>
      </div>
    </div>
  );
}

function IconHome() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M3 10.5L12 3l9 7.5V20a1 1 0 01-1 1h-5v-6H9v6H4a1 1 0 01-1-1v-9.5z" strokeLinejoin="round" />
    </svg>
  );
}

function IconFlow() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <circle cx="5" cy="12" r="2.5" />
      <circle cx="12" cy="6" r="2.5" />
      <circle cx="19" cy="12" r="2.5" />
      <circle cx="12" cy="18" r="2.5" />
      <path d="M7 11l3-3M14 8l3 3M17 13l-3 3M10 16l-3-3" />
    </svg>
  );
}

function IconBook() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M4 19.5A2.5 2.5 0 016.5 17H20" />
      <path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z" />
    </svg>
  );
}

function IconBox() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z" />
      <path d="M3.27 6.96L12 12.01l8.73-5.05M12 22.08V12" />
    </svg>
  );
}

function IconCalendar() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <rect x="3" y="4" width="18" height="18" rx="2" />
      <path d="M16 2v4M8 2v4M3 10h18" />
    </svg>
  );
}

function IconInbox() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M22 12h-6l-2 3H10L8 12H2" />
      <path d="M5.45 5.11L2 12v6a2 2 0 002 2h16a2 2 0 002-2v-6l-3.45-6.89A2 2 0 0016.76 4H7.24a2 2 0 00-1.79 1.11z" />
    </svg>
  );
}

function IconUpload() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden>
      <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" />
    </svg>
  );
}
