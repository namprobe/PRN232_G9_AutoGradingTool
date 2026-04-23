import { useApiMock } from "../config/env";
import { apiFetch, baseUrl, fetchAsApiResult } from "./client";
import type { ApiResult } from "./types";
import type {
  ExamClassListItem,
  ExamSessionClassListItem,
  ExamSessionDetail,
  ExamSessionListItem,
  ExamSubmissionDetail,
  ExamSubmissionListItem,
  SemesterListItem,
  TriggerRegradeResponse,
} from "./gradingTypes";
import {
  delay,
  mockCreateSubmission,
  getMockExamSessionsResult,
  getMockSemestersResult,
  mockGetExamSession,
  mockGetSubmission,
  mockListExamClasses,
  mockListExamSessionClasses,
  mockListSubmissions,
  mockReplaceSubmissionFile,
  mockTriggerRegrade,
} from "./gradingMockData";

async function maybeDelay(): Promise<void> {
  if (useApiMock()) await delay(180);
}

export async function listSemesters(token: string | null): Promise<ApiResult<SemesterListItem[]>> {
  if (useApiMock()) {
    await maybeDelay();
    return getMockSemestersResult();
  }
  return apiFetch<SemesterListItem[]>("/api/cms/grading/semesters", { method: "GET", token });
}

export async function listExamSessions(
  token: string | null,
  semesterId?: string | null
): Promise<ApiResult<ExamSessionListItem[]>> {
  if (useApiMock()) {
    await maybeDelay();
    return getMockExamSessionsResult(semesterId);
  }
  const q = semesterId ? `?semesterId=${encodeURIComponent(semesterId)}` : "";
  return apiFetch<ExamSessionListItem[]>(`/api/cms/grading/exam-sessions${q}`, { method: "GET", token });
}

export async function getExamSession(token: string | null, id: string): Promise<ApiResult<ExamSessionDetail>> {
  if (useApiMock()) {
    await maybeDelay();
    return mockGetExamSession(id);
  }
  return apiFetch<ExamSessionDetail>(`/api/cms/grading/exam-sessions/${id}`, { method: "GET", token });
}

export async function listExamSessionClasses(
  token: string | null,
  sessionId: string
): Promise<ApiResult<ExamSessionClassListItem[]>> {
  if (useApiMock()) {
    await maybeDelay();
    return mockListExamSessionClasses(sessionId);
  }
  return apiFetch<ExamSessionClassListItem[]>(`/api/cms/grading/exam-sessions/${sessionId}/session-classes`, {
    method: "GET",
    token,
  });
}

export async function listExamClasses(
  token: string | null,
  semesterId: string
): Promise<ApiResult<ExamClassListItem[]>> {
  if (useApiMock()) {
    await maybeDelay();
    return mockListExamClasses(semesterId);
  }
  return apiFetch<ExamClassListItem[]>(`/api/cms/grading/semesters/${semesterId}/exam-classes`, {
    method: "GET",
    token,
  });
}

export async function listSubmissions(
  token: string | null,
  examSessionId: string,
  examSessionClassId?: string | null
): Promise<ApiResult<ExamSubmissionListItem[]>> {
  if (useApiMock()) {
    await maybeDelay();
    return mockListSubmissions(examSessionId, examSessionClassId);
  }
  const q = new URLSearchParams();
  q.set("examSessionId", examSessionId);
  if (examSessionClassId) q.set("examSessionClassId", examSessionClassId);
  return apiFetch<ExamSubmissionListItem[]>(`/api/cms/grading/submissions?${q.toString()}`, {
    method: "GET",
    token,
  });
}

export async function getSubmission(token: string | null, id: string): Promise<ApiResult<ExamSubmissionDetail>> {
  if (useApiMock()) {
    await maybeDelay();
    return mockGetSubmission(id);
  }
  return apiFetch<ExamSubmissionDetail>(`/api/cms/grading/submissions/${id}`, { method: "GET", token });
}

export async function createSubmissionZip(
  token: string | null,
  formData: FormData
): Promise<ApiResult<string>> {
  if (useApiMock()) {
    await delay(400);
    const sessionId = String(formData.get("examSessionId") ?? "");
    const studentCode = String(formData.get("studentCode") ?? "");
    const sn = formData.get("studentName");
    return mockCreateSubmission(sessionId, studentCode, sn != null ? String(sn) : undefined);
  }
  return fetchAsApiResult<string>(`${baseUrl()}/api/cms/grading/submissions`, {
    method: "POST",
    body: formData,
    token,
  });
}

export async function replaceSubmissionFile(
  token: string | null,
  submissionId: string,
  questionLabel: string,
  zipFile: File
): Promise<ApiResult<boolean>> {
  if (useApiMock()) {
    await delay(350);
    return mockReplaceSubmissionFile(submissionId, questionLabel);
  }
  const formData = new FormData();
  formData.append("questionLabel", questionLabel);
  formData.append("zipFile", zipFile);
  return fetchAsApiResult<boolean>(`${baseUrl()}/api/cms/grading/submissions/${submissionId}/files`, {
    method: "PUT",
    body: formData,
    token,
  });
}

export async function triggerRegrade(
  token: string | null,
  submissionId: string
): Promise<ApiResult<TriggerRegradeResponse>> {
  if (useApiMock()) {
    await delay(500);
    return mockTriggerRegrade(submissionId);
  }
  return apiFetch<TriggerRegradeResponse>(`/api/cms/grading/submissions/${submissionId}/regrade`, {
    method: "POST",
    token,
  });
}
