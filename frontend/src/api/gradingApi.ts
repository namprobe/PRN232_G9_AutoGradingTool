import { useApiMock } from "../config/env";
import { apiFetch, baseUrl } from "./client";
import type { ApiResult } from "./types";
import type {
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
  mockExamSessions,
  mockGetExamSession,
  mockGetSubmission,
  mockListSubmissions,
  mockReplaceSubmissionFile,
  mockSemesters,
  mockTriggerRegrade,
} from "./gradingMockData";

async function maybeDelay(): Promise<void> {
  if (useApiMock()) await delay(180);
}

export async function listSemesters(token: string | null): Promise<ApiResult<SemesterListItem[]>> {
  if (useApiMock()) {
    await maybeDelay();
    return mockSemesters;
  }
  return apiFetch<SemesterListItem[]>("/api/cms/grading/semesters", { method: "GET", token });
}

export async function listExamSessions(
  token: string | null,
  semesterId?: string | null
): Promise<ApiResult<ExamSessionListItem[]>> {
  if (useApiMock()) {
    await maybeDelay();
    return mockExamSessions;
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

export async function listSubmissions(
  token: string | null,
  examSessionId: string
): Promise<ApiResult<ExamSubmissionListItem[]>> {
  if (useApiMock()) {
    await maybeDelay();
    return mockListSubmissions(examSessionId);
  }
  const q = `?examSessionId=${encodeURIComponent(examSessionId)}`;
  return apiFetch<ExamSubmissionListItem[]>(`/api/cms/grading/submissions${q}`, { method: "GET", token });
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
    return mockCreateSubmission();
  }
  const headers = new Headers();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  const res = await fetch(`${baseUrl()}/api/cms/grading/submissions`, {
    method: "POST",
    body: formData,
    headers,
  });
  return (await res.json()) as ApiResult<string>;
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
  const headers = new Headers();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  const res = await fetch(`${baseUrl()}/api/cms/grading/submissions/${submissionId}/files`, {
    method: "PUT",
    body: formData,
    headers,
  });
  return (await res.json()) as ApiResult<boolean>;
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
