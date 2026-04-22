import { Link } from "react-router-dom";
import { examSessionDetailPath } from "../lib/workflowRoutes";

type Crumb = { label: string; to?: string };

export function WorkflowBreadcrumb({ items }: { items: Crumb[] }) {
  return (
    <nav className="ag-breadcrumb" aria-label="Vị trí trong luồng">
      {items.map((c, i) => (
        <span key={`${c.label}-${i}`} className="ag-breadcrumb__seg">
          {i > 0 ? <span className="ag-breadcrumb__sep" aria-hidden> / </span> : null}
          {c.to ? (
            <Link className="ag-breadcrumb__link" to={c.to}>
              {c.label}
            </Link>
          ) : (
            <span className="ag-breadcrumb__current">{c.label}</span>
          )}
        </span>
      ))}
    </nav>
  );
}

export function crumbsForSessionList(): Crumb[] {
  return [
    { label: "Tổng quan", to: "/" },
    { label: "Ca thi", to: "/exam-sessions" },
  ];
}

export function crumbsForSessionDetail(sessionCode: string, sessionId: string): Crumb[] {
  return [
    { label: "Tổng quan", to: "/" },
    { label: "Ca thi", to: "/exam-sessions" },
    { label: sessionCode, to: examSessionDetailPath(sessionId) },
  ];
}

export function crumbsForSubmissions(sessionCode: string, sessionId: string): Crumb[] {
  return [
    ...crumbsForSessionDetail(sessionCode, sessionId),
    { label: "Bài nộp" },
  ];
}

export function crumbsForUpload(sessionCode: string, sessionId: string): Crumb[] {
  return [
    { label: "Tổng quan", to: "/" },
    { label: "Ca thi", to: "/exam-sessions" },
    { label: sessionCode, to: examSessionDetailPath(sessionId) },
    { label: "Nộp ZIP" },
  ];
}
