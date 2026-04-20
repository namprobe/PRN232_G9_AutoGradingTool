import type { FlowStatus, SessionStatus } from "../components/StatusBadge";
import type { ExamSubmissionDetail } from "../api/gradingTypes";

export type { FlowStatus };

/** Ca thi API không có trạng thái — hiển thị demo theo lịch. */
export function inferSessionStatus(startsAtUtc: string, endsAtUtc?: string): SessionStatus {
  const start = new Date(startsAtUtc).getTime();
  const end = endsAtUtc ? new Date(endsAtUtc).getTime() : start + 110 * 60000;
  const now = Date.now();
  if (start > now + 7 * 86400000) return "draft";
  if (end < now) return "closed";
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

/** Nhãn câu hỏi để admin chọn khi thay file zip (khớp API questionLabel). */
export function questionLabelsForReplace(detail: ExamSubmissionDetail): string[] {
  const fromFiles = detail.submissionFiles.map((f) => f.questionLabel);
  const fromScores = detail.questionScores.map((q) => q.questionLabel);
  const merged = [...new Set([...fromFiles, ...fromScores])].filter(Boolean);
  if (merged.length) return merged.sort((a, b) => a.localeCompare(b, "vi"));
  return ["Q1", "Q2"];
}

export function listItemMaxScore(): number {
  return 10;
}
