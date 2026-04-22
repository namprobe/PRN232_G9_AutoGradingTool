/** Đường dẫn thống nhất — luôn gắn bài nộp / upload với một ca thi cụ thể. */

export function examSessionDetailPath(sessionId: string): string {
  return `/exam-sessions/${encodeURIComponent(sessionId)}`;
}

export function examSessionSubmissionsPath(sessionId: string): string {
  return `/exam-sessions/${encodeURIComponent(sessionId)}/submissions`;
}

export function examSessionUploadPath(sessionId: string): string {
  return `/exam-sessions/${encodeURIComponent(sessionId)}/upload`;
}
