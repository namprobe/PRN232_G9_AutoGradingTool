type FlowStatus = "pending" | "grading" | "graded" | "error";

const config: Record<
  FlowStatus,
  { className: string; label: string }
> = {
  pending: { className: "ag-badge ag-badge--muted", label: "Chờ" },
  grading: { className: "ag-badge ag-badge--info", label: "Đang chấm" },
  graded: { className: "ag-badge ag-badge--ok", label: "Xong" },
  error: { className: "ag-badge ag-badge--err", label: "Lỗi" },
};

export function StatusBadge({ status }: { status: FlowStatus }) {
  const c = config[status];
  return <span className={c.className}>{c.label}</span>;
}

type SessionStatus = "draft" | "active" | "closed";

const sessionCfg: Record<SessionStatus, { className: string; label: string }> = {
  draft: { className: "ag-badge ag-badge--muted", label: "Nháp" },
  active: { className: "ag-badge ag-badge--ok", label: "Đang mở" },
  closed: { className: "ag-badge ag-badge--info", label: "Đã đóng" },
};

export function SessionStatusBadge({ status }: { status: SessionStatus }) {
  const c = sessionCfg[status];
  return <span className={c.className}>{c.label}</span>;
}
