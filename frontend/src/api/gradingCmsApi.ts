import { useApiMock } from "../config/env";
import { apiFetch, baseUrl, fetchAsApiResult } from "./client";
import type { ApiResult } from "./types";
import type {
  CreateExamQuestionBody,
  CreateExamSessionBody,
  CreateExamTestCaseBody,
  CreateExamTopicBody,
  CreateGradingPackBody,
  CreateSemesterBody,
  ExamGradingPackListItem,
  ExamPackAssetListItem,
  ExamQuestionDetail,
  ExamSessionListItem,
  ExamTestCaseDetail,
  ExamTopicDetail,
  SemesterListItem,
  BatchSubmitZipsResponse,
  UpdateExamQuestionBody,
  UpdateExamSessionBody,
  UpdateExamTestCaseBody,
  UpdateExamTopicBody,
  UpdateGradingPackBody,
  UpdateSemesterBody,
} from "./gradingTypes";
import {
  delay,
  mockCreateExamSessionRow,
  mockCreateGradingPack,
  mockCreatePackAsset,
  mockCreateQuestionForTopic,
  mockCreateSemesterRow,
  mockCreateSubmission,
  mockCreateTestCaseForQuestion,
  mockCreateTopicForSession,
  mockDeleteSemesterRow,
  mockListGradingPacks,
  mockUpdateSemesterRow,
} from "./gradingMockData";

async function mDelay(): Promise<void> {
  if (useApiMock()) await delay(160);
}

export async function cmsCreateSemester(token: string | null, body: CreateSemesterBody): Promise<ApiResult<SemesterListItem>> {
  if (useApiMock()) {
    await mDelay();
    return mockCreateSemesterRow(body);
  }
  return apiFetch<SemesterListItem>("/api/cms/grading/semesters", { method: "POST", token, body: JSON.stringify(body) });
}

export async function cmsUpdateSemester(
  token: string | null,
  id: string,
  body: UpdateSemesterBody
): Promise<ApiResult<SemesterListItem>> {
  if (useApiMock()) {
    await mDelay();
    return mockUpdateSemesterRow(id, body);
  }
  return apiFetch<SemesterListItem>(`/api/cms/grading/semesters/${id}`, { method: "PUT", token, body: JSON.stringify(body) });
}

export async function cmsDeleteSemester(token: string | null, id: string): Promise<ApiResult<boolean>> {
  if (useApiMock()) {
    await mDelay();
    return mockDeleteSemesterRow(id);
  }
  return apiFetch<boolean>(`/api/cms/grading/semesters/${id}`, { method: "DELETE", token });
}

export async function cmsCreateExamSession(
  token: string | null,
  body: CreateExamSessionBody
): Promise<ApiResult<ExamSessionListItem>> {
  if (useApiMock()) {
    await mDelay();
    return mockCreateExamSessionRow(body);
  }
  return apiFetch<ExamSessionListItem>("/api/cms/grading/exam-sessions", {
    method: "POST",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsUpdateExamSession(
  token: string | null,
  id: string,
  body: UpdateExamSessionBody
): Promise<ApiResult<ExamSessionListItem>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật để cập nhật ca thi.", errorCode: "InvalidOperation" };
  }
  return apiFetch<ExamSessionListItem>(`/api/cms/grading/exam-sessions/${id}`, {
    method: "PUT",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsDeleteExamSession(token: string | null, id: string): Promise<ApiResult<boolean>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật để xóa ca.", errorCode: "InvalidOperation" };
  }
  return apiFetch<boolean>(`/api/cms/grading/exam-sessions/${id}`, { method: "DELETE", token });
}

export async function cmsCreateTopic(
  token: string | null,
  sessionId: string,
  body: CreateExamTopicBody
): Promise<ApiResult<ExamTopicDetail>> {
  if (useApiMock()) {
    await mDelay();
    return mockCreateTopicForSession(sessionId, body);
  }
  return apiFetch<ExamTopicDetail>(`/api/cms/grading/exam-sessions/${sessionId}/topics`, {
    method: "POST",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsUpdateTopic(
  token: string | null,
  topicId: string,
  body: UpdateExamTopicBody
): Promise<ApiResult<ExamTopicDetail>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật.", errorCode: "InvalidOperation" };
  }
  return apiFetch<ExamTopicDetail>(`/api/cms/grading/exam-topics/${topicId}`, {
    method: "PUT",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsDeleteTopic(token: string | null, topicId: string): Promise<ApiResult<boolean>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật.", errorCode: "InvalidOperation" };
  }
  return apiFetch<boolean>(`/api/cms/grading/exam-topics/${topicId}`, { method: "DELETE", token });
}

export async function cmsCreateQuestion(
  token: string | null,
  topicId: string,
  body: CreateExamQuestionBody
): Promise<ApiResult<ExamQuestionDetail>> {
  if (useApiMock()) {
    await mDelay();
    return mockCreateQuestionForTopic(topicId, body);
  }
  return apiFetch<ExamQuestionDetail>(`/api/cms/grading/exam-topics/${topicId}/questions`, {
    method: "POST",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsUpdateQuestion(
  token: string | null,
  questionId: string,
  body: UpdateExamQuestionBody
): Promise<ApiResult<ExamQuestionDetail>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật.", errorCode: "InvalidOperation" };
  }
  return apiFetch<ExamQuestionDetail>(`/api/cms/grading/exam-questions/${questionId}`, {
    method: "PUT",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsDeleteQuestion(token: string | null, questionId: string): Promise<ApiResult<boolean>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật.", errorCode: "InvalidOperation" };
  }
  return apiFetch<boolean>(`/api/cms/grading/exam-questions/${questionId}`, { method: "DELETE", token });
}

export async function cmsCreateTestCase(
  token: string | null,
  questionId: string,
  body: CreateExamTestCaseBody
): Promise<ApiResult<ExamTestCaseDetail>> {
  if (useApiMock()) {
    await mDelay();
    return mockCreateTestCaseForQuestion(questionId, body);
  }
  return apiFetch<ExamTestCaseDetail>(`/api/cms/grading/exam-questions/${questionId}/test-cases`, {
    method: "POST",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsUpdateTestCase(
  token: string | null,
  testCaseId: string,
  body: UpdateExamTestCaseBody
): Promise<ApiResult<ExamTestCaseDetail>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật.", errorCode: "InvalidOperation" };
  }
  return apiFetch<ExamTestCaseDetail>(`/api/cms/grading/exam-test-cases/${testCaseId}`, {
    method: "PUT",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsDeleteTestCase(token: string | null, testCaseId: string): Promise<ApiResult<boolean>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật.", errorCode: "InvalidOperation" };
  }
  return apiFetch<boolean>(`/api/cms/grading/exam-test-cases/${testCaseId}`, { method: "DELETE", token });
}

export async function cmsListGradingPacks(
  token: string | null,
  sessionId: string
): Promise<ApiResult<ExamGradingPackListItem[]>> {
  if (useApiMock()) {
    await mDelay();
    return mockListGradingPacks(sessionId);
  }
  return apiFetch<ExamGradingPackListItem[]>(`/api/cms/grading/exam-sessions/${sessionId}/grading-packs`, {
    method: "GET",
    token,
  });
}

export async function cmsCreateGradingPack(
  token: string | null,
  sessionId: string,
  body: CreateGradingPackBody
): Promise<ApiResult<ExamGradingPackListItem>> {
  if (useApiMock()) {
    await mDelay();
    return mockCreateGradingPack(sessionId, body);
  }
  return apiFetch<ExamGradingPackListItem>(`/api/cms/grading/exam-sessions/${sessionId}/grading-packs`, {
    method: "POST",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsUpdateGradingPack(
  token: string | null,
  packId: string,
  body: UpdateGradingPackBody
): Promise<ApiResult<ExamGradingPackListItem>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật.", errorCode: "InvalidOperation" };
  }
  return apiFetch<ExamGradingPackListItem>(`/api/cms/grading/grading-packs/${packId}`, {
    method: "PUT",
    token,
    body: JSON.stringify(body),
  });
}

export async function cmsDeleteGradingPack(token: string | null, packId: string): Promise<ApiResult<boolean>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật.", errorCode: "InvalidOperation" };
  }
  return apiFetch<boolean>(`/api/cms/grading/grading-packs/${packId}`, { method: "DELETE", token });
}

export async function cmsUploadPackAsset(
  token: string | null,
  packId: string,
  kind: number,
  file: File
): Promise<ApiResult<ExamPackAssetListItem>> {
  if (useApiMock()) {
    await delay(300);
    return mockCreatePackAsset(packId, kind);
  }
  const formData = new FormData();
  formData.append("kind", String(kind));
  formData.append("file", file);
  return fetchAsApiResult<ExamPackAssetListItem>(`${baseUrl()}/api/cms/grading/grading-packs/${packId}/assets`, {
    method: "POST",
    body: formData,
    token,
  });
}

export async function cmsDeletePackAsset(token: string | null, assetId: string): Promise<ApiResult<boolean>> {
  if (useApiMock()) {
    await mDelay();
    return { isSuccess: false, message: "Mock: dùng API thật.", errorCode: "InvalidOperation" };
  }
  return apiFetch<boolean>(`/api/cms/grading/pack-assets/${assetId}`, { method: "DELETE", token });
}

/** Giả lập SV — cùng multipart, khác base path. */
export async function studentSubmitZip(token: string | null, formData: FormData): Promise<ApiResult<string>> {
  if (useApiMock()) {
    await delay(400);
    const sessionId = String(formData.get("examSessionId") ?? "");
    const studentCode = String(formData.get("entries[0].studentCode") ?? "");
    const sn = formData.get("entries[0].studentName");
    return mockCreateSubmission(sessionId, studentCode, sn != null ? String(sn) : undefined);
  }
  const sessionId = String(formData.get("examSessionId") ?? "");
  const batchResult = await fetchAsApiResult<BatchSubmitZipsResponse>(
    `${baseUrl()}/api/student/grading/exam-sessions/${encodeURIComponent(sessionId)}/submissions/batch`,
    {
      method: "POST",
      body: formData,
      token,
    }
  );

  if (!batchResult.isSuccess || !batchResult.data) {
    return { isSuccess: false, message: batchResult.message, errors: batchResult.errors, errorCode: batchResult.errorCode };
  }

  const firstOk = batchResult.data.results.find((x) => x.success && x.submissionId);
  if (!firstOk?.submissionId) {
    return {
      isSuccess: false,
      message: batchResult.data.results.find((x) => !x.success)?.error ?? batchResult.message ?? "Không tạo được bài nộp.",
      errorCode: "BusinessRuleViolation",
    };
  }

  return {
    isSuccess: true,
    message: batchResult.message,
    data: firstOk.submissionId,
  };
}
