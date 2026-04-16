import type { FlowStatus, SessionStatus } from "../components/StatusBadge";
import type { ExamSubmissionDetail } from "../api/gradingTypes";

export type { FlowStatus };

/** Ca thi API không có trạng thái — hiển thị demo theo lịch. */
export function inferSessionStatus(scheduledAtUtc: string): SessionStatus {
  const t = new Date(scheduledAtUtc).getTime();
  const now = Date.now();
  if (t > now + 7 * 86400000) return "draft";
  if (t < now - 86400000) return "closed";
  return "active";
}

/** Pipeline một lần nộp → hai cột Q1/Q2 (UI cũ). */
export function workflowToQPair(status: string): { q1: FlowStatus; q2: FlowStatus } {
  const s = (status || "").trim();
  if (s === "Completed") return { q1: "graded", q2: "graded" };
  if (s === "Running") return { q1: "graded", q2: "grading" };
  if (s === "Failed") return { q1: "error", q2: "error" };
  if (s === "Queued") return { q1: "pending", q2: "pending" };
  return { q1: "pending", q2: "pending" };
}

export function submissionMaxScore(detail: ExamSubmissionDetail): number {
  const qs = detail.questionScores;
  if (!qs.length) return 10;
  return qs.reduce((a, q) => a + q.maxScore, 0);
}

export function listItemMaxScore(): number {
  return 10;
}
