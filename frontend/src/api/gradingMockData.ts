import type { ApiResult } from "./types";
import type {
  ExamGradingPackListItem,
  ExamPackAssetListItem,
  ExamQuestionDetail,
  ExamSessionDetail,
  ExamSessionListItem,
  ExamSubmissionDetail,
  ExamSubmissionListItem,
  ExamTestCaseDetail,
  ExamTopicDetail,
  SemesterListItem,
  TriggerRegradeResponse,
} from "./gradingTypes";

/** Trùng ExamGradingSeeder + docs GRADING_REST_API.md */
export const DEMO_SEMESTER_ID = "b1000000-0000-4000-8000-000000000001";
export const DEMO_EXAM_SESSION_ID = "b1000000-0000-4000-8000-000000000002";
export const DEMO_SAMPLE_SUBMISSION_ID = "b1000000-0000-4000-8000-00000000feed";
const MOCK_SUB2_ID = "b1000000-0000-4000-8000-00000000ab01";

const topicId = "b1000000-0000-4000-8000-000000000003";
const q1Id = "b1000000-0000-4000-8000-000000000011";
const q2Id = "b1000000-0000-4000-8000-000000000012";
const tcQ1a = "b1000000-0000-4000-8000-000000000021";
const tcQ1b = "b1000000-0000-4000-8000-000000000022";
const tcQ2a = "b1000000-0000-4000-8000-000000000031";
const tcQ2b = "b1000000-0000-4000-8000-000000000032";

function ok<T>(data: T, message = "OK"): ApiResult<T> {
  return { isSuccess: true, message, data };
}

function fail<T>(message: string, errorCode = "NotFound"): ApiResult<T> {
  return { isSuccess: false, message, errorCode, errors: [message] };
}

const scheduledAt = "2026-05-26T08:00:00.000Z";
const endsAt = "2026-05-26T09:50:00.000Z"; // 90p làm bài + 20p dự phòng

function deepClone<T>(x: T): T {
  return JSON.parse(JSON.stringify(x)) as T;
}

let mockSemesterRows: SemesterListItem[] = [
  {
    id: DEMO_SEMESTER_ID,
    code: "SPRING2026",
    name: "Spring 2026",
    startDateUtc: "2026-01-15T00:00:00.000Z",
    endDateUtc: "2026-06-30T00:00:00.000Z",
  },
];

let mockExamSessionRows: ExamSessionListItem[] = [
  {
    id: DEMO_EXAM_SESSION_ID,
    code: "PRN232-DEMO-PE",
    title: "Practical Exam — PRN232 (demo)",
    semesterId: DEMO_SEMESTER_ID,
    semesterCode: "SPRING2026",
    startsAtUtc: scheduledAt,
    examDurationMinutes: 90,
    endsAtUtc: endsAt,
    deferredClassGrading: false,
    topicCount: 1,
    questionCount: 2,
    submissionCount: 2,
  },
];

export function getMockSemestersResult(): ApiResult<SemesterListItem[]> {
  return ok(mockSemesterRows.map((x) => ({ ...x })));
}

export function getMockExamSessionsResult(semesterId?: string | null): ApiResult<ExamSessionListItem[]> {
  let rows = mockExamSessionRows.map((x) => ({ ...x }));
  if (semesterId) rows = rows.filter((x) => x.semesterId === semesterId);
  return ok(rows);
}

function syncDemoSessionCountsFromTree(detail: ExamSessionDetail) {
  const topicCount = detail.topics.length;
  const questionCount = detail.topics.reduce((a, t) => a + t.questions.length, 0);
  const idx = mockExamSessionRows.findIndex((x) => x.id === DEMO_EXAM_SESSION_ID);
  if (idx >= 0) {
    mockExamSessionRows[idx] = {
      ...mockExamSessionRows[idx]!,
      topicCount,
      questionCount,
    };
  }
}

const seedSessionDetail: ExamSessionDetail = {
  id: DEMO_EXAM_SESSION_ID,
  code: "PRN232-DEMO-PE",
  title: "Practical Exam — PRN232 (demo)",
  semesterId: DEMO_SEMESTER_ID,
  semesterCode: "SPRING2026",
  startsAtUtc: scheduledAt,
  examDurationMinutes: 90,
  endsAtUtc: endsAt,
  deferredClassGrading: false,
  topics: [
    {
      id: topicId,
      title: "Đề thi thực hành",
      sortOrder: 1,
      questions: [
        {
          id: q1Id,
          label: "Q1",
          title: "REST + EF (zip)",
          maxScore: 5,
          testCases: [
            { id: tcQ1a, name: "Build & migrations", maxPoints: 2.5, sortOrder: 1 },
            { id: tcQ1b, name: "API copies endpoints", maxPoints: 2.5, sortOrder: 2 },
          ],
        },
        {
          id: q2Id,
          label: "Q2",
          title: "MVC + GivenAPI (zip)",
          maxScore: 5,
          testCases: [
            { id: tcQ2a, name: "Views & routing", maxPoints: 2.5, sortOrder: 1 },
            { id: tcQ2b, name: "HttpClient integration", maxPoints: 2.5, sortOrder: 2 },
          ],
        },
      ],
    },
  ],
};

let mockSessionDetailMutable: ExamSessionDetail = deepClone(seedSessionDetail);
syncDemoSessionCountsFromTree(mockSessionDetailMutable);

const mockPackRowsBySession = new Map<string, ExamGradingPackListItem[]>();
const mockPackAssetsByPack = new Map<string, ExamPackAssetListItem[]>();

mockPackRowsBySession.set(DEMO_EXAM_SESSION_ID, [
  { id: "b1000000-0000-4000-8000-00000000pack1", version: 1, label: "Gói chấm minh họa", isActive: true, assetCount: 0 },
]);

/** Ca thi tạo trong mock (ngoài DEMO) — cây topic / câu / testcase. */
const mockNonDemoSessionTopics = new Map<string, ExamTopicDetail[]>();

function syncNonDemoSessionCountsInRow(sessionKey: string) {
  const topics = mockNonDemoSessionTopics.get(sessionKey) ?? [];
  const topicCount = topics.length;
  const questionCount = topics.reduce((a, t) => a + t.questions.length, 0);
  mockExamSessionRows = mockExamSessionRows.map((s) =>
    s.id.toLowerCase() === sessionKey ? { ...s, topicCount, questionCount } : s
  );
}

function findNonDemoTopicByTopicId(topicId: string): { sessionKey: string; topic: ExamTopicDetail } | null {
  for (const [sessionKey, topics] of mockNonDemoSessionTopics.entries()) {
    const topic = topics.find((t) => t.id === topicId);
    if (topic) return { sessionKey, topic };
  }
  return null;
}

function findNonDemoQuestionLocation(questionId: string): {
  sessionKey: string;
  topicId: string;
  question: ExamQuestionDetail;
} | null {
  for (const [sessionKey, topics] of mockNonDemoSessionTopics.entries()) {
    for (const t of topics) {
      const q = t.questions.find((x) => x.id === questionId);
      if (q) return { sessionKey, topicId: t.id, question: q };
    }
  }
  return null;
}

export function mockGetExamSession(id: string): ApiResult<ExamSessionDetail> {
  if (id.toLowerCase() === DEMO_EXAM_SESSION_ID.toLowerCase()) return ok(deepClone(mockSessionDetailMutable));
  const row = mockExamSessionRows.find((x) => x.id.toLowerCase() === id.toLowerCase());
  if (!row) return fail("Không tìm thấy ca thi.");
  const topics = mockNonDemoSessionTopics.get(row.id.toLowerCase()) ?? [];
  return ok({
    id: row.id,
    code: row.code,
    title: row.title,
    semesterId: row.semesterId,
    semesterCode: row.semesterCode,
    startsAtUtc: row.startsAtUtc,
    examDurationMinutes: row.examDurationMinutes,
    endsAtUtc: row.endsAtUtc,
    deferredClassGrading: row.deferredClassGrading,
    topics: deepClone(topics),
  });
}

export function mockCreateSemesterRow(body: {
  code: string;
  name: string;
  startDateUtc: string | null;
  endDateUtc: string | null;
}): ApiResult<SemesterListItem> {
  if (mockSemesterRows.some((x) => x.code.toLowerCase() === body.code.trim().toLowerCase())) {
    return fail("Mã học kỳ đã tồn tại (chế độ thử).");
  }
  const row: SemesterListItem = {
    id: crypto.randomUUID(),
    code: body.code.trim(),
    name: body.name.trim(),
    startDateUtc: body.startDateUtc,
    endDateUtc: body.endDateUtc,
  };
  mockSemesterRows = [...mockSemesterRows, row];
  return ok(row, "Đã tạo (chế độ thử).");
}

export function mockUpdateSemesterRow(
  id: string,
  body: { code: string; name: string; startDateUtc: string | null; endDateUtc: string | null }
): ApiResult<SemesterListItem> {
  const idx = mockSemesterRows.findIndex((x) => x.id === id);
  if (idx < 0) return fail("Không tìm thấy (chế độ thử).");
  if (mockSemesterRows.some((x) => x.id !== id && x.code.toLowerCase() === body.code.trim().toLowerCase())) {
    return fail("Trùng mã (chế độ thử).");
  }
  const row: SemesterListItem = {
    id,
    code: body.code.trim(),
    name: body.name.trim(),
    startDateUtc: body.startDateUtc,
    endDateUtc: body.endDateUtc,
  };
  mockSemesterRows = mockSemesterRows.map((x, i) => (i === idx ? row : x));
  const sc = row.code;
  mockExamSessionRows = mockExamSessionRows.map((s) => (s.semesterId === id ? { ...s, semesterCode: sc } : s));
  return ok(row, "Đã cập nhật (chế độ thử).");
}

export function mockDeleteSemesterRow(id: string): ApiResult<boolean> {
  if (mockExamSessionRows.some((x) => x.semesterId === id)) return fail("Còn ca thi (chế độ thử).");
  const next = mockSemesterRows.filter((x) => x.id !== id);
  if (next.length === mockSemesterRows.length) return fail("Không tìm thấy (chế độ thử).");
  mockSemesterRows = next;
  return ok(true, "Đã xóa (chế độ thử).");
}

export function mockCreateExamSessionRow(body: {
  semesterId: string;
  code: string;
  title: string;
  startsAtUtc: string;
  examDurationMinutes: number;
  endsAtUtc: string;
  deferredClassGrading?: boolean;
}): ApiResult<ExamSessionListItem> {
  const sem = mockSemesterRows.find((x) => x.id === body.semesterId);
  if (!sem) return fail("Không có học kỳ (chế độ thử).");
  if (mockExamSessionRows.some((x) => x.semesterId === body.semesterId && x.code === body.code.trim())) {
    return fail("Trùng mã ca (chế độ thử).");
  }
  const row: ExamSessionListItem = {
    id: crypto.randomUUID(),
    code: body.code.trim(),
    title: body.title.trim(),
    semesterId: body.semesterId,
    semesterCode: sem.code,
    startsAtUtc: body.startsAtUtc,
    examDurationMinutes: body.examDurationMinutes,
    endsAtUtc: body.endsAtUtc,
    deferredClassGrading: body.deferredClassGrading ?? false,
    topicCount: 0,
    questionCount: 0,
    submissionCount: 0,
  };
  mockExamSessionRows = [...mockExamSessionRows, row];
  mockPackRowsBySession.set(row.id, []);
  return ok(row, "Đã tạo ca (chế độ thử).");
}

export function mockCreateTopicForSession(
  sessionId: string,
  body: { title: string; sortOrder: number }
): ApiResult<ExamTopicDetail> {
  const key = sessionId.toLowerCase();
  if (key !== DEMO_EXAM_SESSION_ID.toLowerCase()) {
    const row = mockExamSessionRows.find((x) => x.id.toLowerCase() === key);
    if (!row) return fail("Không tìm thấy ca thi (chế độ thử).");
    const t: ExamTopicDetail = {
      id: crypto.randomUUID(),
      title: body.title.trim(),
      sortOrder: body.sortOrder,
      questions: [],
    };
    const list = mockNonDemoSessionTopics.get(key) ?? [];
    mockNonDemoSessionTopics.set(key, [...list, t]);
    syncNonDemoSessionCountsInRow(key);
    return ok(t, "Đã tạo chủ đề (chế độ thử).");
  }
  const t: ExamTopicDetail = {
    id: crypto.randomUUID(),
    title: body.title.trim(),
    sortOrder: body.sortOrder,
    questions: [],
  };
  mockSessionDetailMutable = {
    ...mockSessionDetailMutable,
    topics: [...mockSessionDetailMutable.topics, t],
  };
  syncDemoSessionCountsFromTree(mockSessionDetailMutable);
  return ok(t, "Đã tạo chủ đề (chế độ thử).");
}

export function mockCreateQuestionForTopic(
  topicId: string,
  body: { label: string; title: string; maxScore: number }
): ApiResult<ExamQuestionDetail> {
  const topic = mockSessionDetailMutable.topics.find((x) => x.id === topicId);
  if (topic) {
    if (topic.questions.some((q) => q.label.toLowerCase() === body.label.trim().toLowerCase())) {
      return fail("Trùng mã câu trên đề (chế độ thử).");
    }
    const q: ExamQuestionDetail = {
      id: crypto.randomUUID(),
      label: body.label.trim(),
      title: body.title.trim(),
      maxScore: body.maxScore,
      testCases: [],
    };
    mockSessionDetailMutable = {
      ...mockSessionDetailMutable,
      topics: mockSessionDetailMutable.topics.map((x) =>
        x.id === topicId ? { ...x, questions: [...x.questions, q] } : x
      ),
    };
    syncDemoSessionCountsFromTree(mockSessionDetailMutable);
    return ok(q, "Đã tạo câu (chế độ thử).");
  }

  const hit = findNonDemoTopicByTopicId(topicId);
  if (!hit) return fail("Không tìm thấy chủ đề (chế độ thử).");
  if (hit.topic.questions.some((q) => q.label.toLowerCase() === body.label.trim().toLowerCase())) {
    return fail("Trùng mã câu trên đề (chế độ thử).");
  }
  const q: ExamQuestionDetail = {
    id: crypto.randomUUID(),
    label: body.label.trim(),
    title: body.title.trim(),
    maxScore: body.maxScore,
    testCases: [],
  };
  const { sessionKey } = hit;
  const topics = mockNonDemoSessionTopics.get(sessionKey)!;
  mockNonDemoSessionTopics.set(
    sessionKey,
    topics.map((t) => (t.id !== topicId ? t : { ...t, questions: [...t.questions, q] }))
  );
  syncNonDemoSessionCountsInRow(sessionKey);
  return ok(q, "Đã tạo câu (chế độ thử).");
}

export function mockCreateTestCaseForQuestion(
  questionId: string,
  body: { name: string; maxPoints: number; sortOrder: number }
): ApiResult<ExamTestCaseDetail> {
  let foundTopic: ExamTopicDetail | undefined;
  for (const t of mockSessionDetailMutable.topics) {
    if (t.questions.some((q) => q.id === questionId)) {
      foundTopic = t;
      break;
    }
  }
  if (foundTopic) {
    const tc: ExamTestCaseDetail = {
      id: crypto.randomUUID(),
      name: body.name.trim(),
      maxPoints: body.maxPoints,
      sortOrder: body.sortOrder,
    };
    mockSessionDetailMutable = {
      ...mockSessionDetailMutable,
      topics: mockSessionDetailMutable.topics.map((t) => ({
        ...t,
        questions: t.questions.map((q) =>
          q.id === questionId ? { ...q, testCases: [...q.testCases, tc] } : q
        ),
      })),
    };
    syncDemoSessionCountsFromTree(mockSessionDetailMutable);
    return ok(tc, "Đã tạo bài kiểm tra (chế độ thử).");
  }

  const hit = findNonDemoQuestionLocation(questionId);
  if (!hit) return fail("Không tìm thấy câu (chế độ thử).");
  const tc: ExamTestCaseDetail = {
    id: crypto.randomUUID(),
    name: body.name.trim(),
    maxPoints: body.maxPoints,
    sortOrder: body.sortOrder,
  };
  const { sessionKey, topicId } = hit;
  const topics = mockNonDemoSessionTopics.get(sessionKey)!;
  mockNonDemoSessionTopics.set(
    sessionKey,
    topics.map((t) =>
      t.id !== topicId
        ? t
        : {
            ...t,
            questions: t.questions.map((q) =>
              q.id !== questionId ? q : { ...q, testCases: [...q.testCases, tc] }
            ),
          }
    )
  );
  return ok(tc, "Đã tạo bài kiểm tra (chế độ thử).");
}

export function mockListGradingPacks(sessionId: string): ApiResult<ExamGradingPackListItem[]> {
  return ok([...(mockPackRowsBySession.get(sessionId) ?? [])]);
}

export function mockCreateGradingPack(
  sessionId: string,
  body: { label: string; version: number | null; isActive: boolean }
): ApiResult<ExamGradingPackListItem> {
  const list = [...(mockPackRowsBySession.get(sessionId) ?? [])];
  const version =
    body.version && body.version > 0 ? body.version : (list.reduce((m, x) => Math.max(m, x.version), 0) || 0) + 1;
  if (list.some((x) => x.version === version)) return fail("Trùng số phiên bản (chế độ thử).");
  const row: ExamGradingPackListItem = {
    id: crypto.randomUUID(),
    version,
    label: body.label.trim(),
    isActive: body.isActive,
    assetCount: 0,
  };
  const next = body.isActive ? list.map((x) => ({ ...x, isActive: false })) : list;
  mockPackRowsBySession.set(sessionId, [...next, row]);
  mockPackAssetsByPack.set(row.id, []);
  return ok(row, "Đã tạo gói chấm (chế độ thử).");
}

export function mockCreatePackAsset(packId: string, kind: number): ApiResult<ExamPackAssetListItem> {
  let sessionId = "";
  for (const [sid, packs] of mockPackRowsBySession.entries()) {
    if (packs.some((p) => p.id === packId)) {
      sessionId = sid;
      break;
    }
  }
  if (!sessionId) return fail("Không tìm thấy gói chấm (chế độ thử).");
  const assetId = crypto.randomUUID();
  const asset: ExamPackAssetListItem = {
    id: assetId,
    examGradingPackId: packId,
    kind,
    storageRelativePath: `exam-pack-assets/mock/${packId}/${assetId}.bin`,
    originalFileName: "mock-upload.bin",
  };
  const assets = [...(mockPackAssetsByPack.get(packId) ?? []), asset];
  mockPackAssetsByPack.set(packId, assets);
  const packs = (mockPackRowsBySession.get(sessionId) ?? []).map((p) =>
    p.id === packId ? { ...p, assetCount: assets.length } : p
  );
  mockPackRowsBySession.set(sessionId, packs);
  return ok(asset, "Đã tải tệp lên (chế độ thử).");
}

const sampleDetail: ExamSubmissionDetail = {
  id: DEMO_SAMPLE_SUBMISSION_ID,
  examSessionId: DEMO_EXAM_SESSION_ID,
  examSessionCode: "PRN232-DEMO-PE",
  examSessionClassId: null,
  classCode: null,
  studentCode: "HE186501",
  studentName: "Bài mẫu (seed)",
  status: "Completed",
  submittedAtUtc: "2026-04-15T07:42:11.000Z",
  totalScore: 8.5,
  submissionFiles: [
    { questionLabel: "Q1", storageRelativePath: "seed/demo-q1.zip", originalFileName: "q1.zip" },
    { questionLabel: "Q2", storageRelativePath: "seed/demo-q2.zip", originalFileName: "q2.zip" },
  ],
  questionScores: [
    { examQuestionId: q1Id, questionLabel: "Q1", score: 4.25, maxScore: 5, summary: "Seed" },
    { examQuestionId: q2Id, questionLabel: "Q2", score: 4.25, maxScore: 5, summary: "Seed" },
  ],
  testCaseScores: [
    {
      examTestCaseId: tcQ1a,
      questionLabel: "Q1",
      testCaseName: "Build & migrations",
      pointsEarned: 2.13,
      maxPoints: 2.5,
      outcome: "Pass",
      message: "Seed",
    },
    {
      examTestCaseId: tcQ1b,
      questionLabel: "Q1",
      testCaseName: "API copies endpoints",
      pointsEarned: 2.12,
      maxPoints: 2.5,
      outcome: "Pass",
      message: "Seed",
    },
    {
      examTestCaseId: tcQ2a,
      questionLabel: "Q2",
      testCaseName: "Views & routing",
      pointsEarned: 2.13,
      maxPoints: 2.5,
      outcome: "Pass",
      message: "Seed",
    },
    {
      examTestCaseId: tcQ2b,
      questionLabel: "Q2",
      testCaseName: "HttpClient integration",
      pointsEarned: 2.12,
      maxPoints: 2.5,
      outcome: "Pass",
      message: "Seed",
    },
  ],
};

const sub2Detail: ExamSubmissionDetail = {
  id: MOCK_SUB2_ID,
  examSessionId: DEMO_EXAM_SESSION_ID,
  examSessionCode: "PRN232-DEMO-PE",
  examSessionClassId: null,
  classCode: null,
  studentCode: "HE186502",
  studentName: "Trần Thị B",
  status: "Running",
  submittedAtUtc: "2026-04-15T07:55:03.000Z",
  totalScore: null,
  submissionFiles: [
    { questionLabel: "Q1", storageRelativePath: "exam-submissions/mock/q1.zip", originalFileName: "q1.zip" },
    { questionLabel: "Q2", storageRelativePath: "exam-submissions/mock/q2.zip", originalFileName: "q2.zip" },
  ],
  questionScores: [],
  testCaseScores: [
    {
      examTestCaseId: tcQ1a,
      questionLabel: "Q1",
      testCaseName: "Build & migrations",
      pointsEarned: 2.5,
      maxPoints: 2.5,
      outcome: "Pass",
      message: "Stub",
    },
    {
      examTestCaseId: tcQ1b,
      questionLabel: "Q1",
      testCaseName: "API copies endpoints",
      pointsEarned: 0,
      maxPoints: 2.5,
      outcome: "Pending",
      message: null,
    },
  ],
};

const mockSubmissionsBySession = new Map<string, ExamSubmissionListItem[]>();
const mockSubmissionDetailsById = new Map<string, ExamSubmissionDetail>();

function initMockSubmissionStore() {
  const k = DEMO_EXAM_SESSION_ID.toLowerCase();
  mockSubmissionsBySession.set(k, [
    {
      id: DEMO_SAMPLE_SUBMISSION_ID,
      examSessionId: DEMO_EXAM_SESSION_ID,
      examSessionClassId: null,
      classCode: null,
      studentCode: "HE186501",
      studentName: "Bài mẫu (seed)",
      status: "Completed",
      submittedAtUtc: "2026-04-15T07:42:11.000Z",
      totalScore: 8.5,
    },
    {
      id: MOCK_SUB2_ID,
      examSessionId: DEMO_EXAM_SESSION_ID,
      examSessionClassId: null,
      classCode: null,
      studentCode: "HE186502",
      studentName: "Trần Thị B",
      status: "Running",
      submittedAtUtc: "2026-04-15T07:55:03.000Z",
      totalScore: null,
    },
  ]);
  mockSubmissionDetailsById.set(DEMO_SAMPLE_SUBMISSION_ID.toLowerCase(), sampleDetail);
  mockSubmissionDetailsById.set(MOCK_SUB2_ID.toLowerCase(), sub2Detail);
}

initMockSubmissionStore();

export function mockListSubmissions(examSessionId: string): ApiResult<ExamSubmissionListItem[]> {
  const exists = mockExamSessionRows.some((x) => x.id.toLowerCase() === examSessionId.toLowerCase());
  if (!exists) return fail("Không tìm thấy ca thi.");
  const key = examSessionId.toLowerCase();
  const rows = mockSubmissionsBySession.get(key) ?? [];
  return ok(rows.map((x) => ({ ...x })));
}

export function mockGetSubmission(id: string): ApiResult<ExamSubmissionDetail> {
  const d = mockSubmissionDetailsById.get(id.toLowerCase());
  if (d) return ok(deepClone(d));
  return fail("Không tìm thấy bài nộp.");
}

export function mockCreateSubmission(
  examSessionId: string,
  studentCode: string,
  studentName?: string | null
): ApiResult<string> {
  const row = mockExamSessionRows.find((x) => x.id.toLowerCase() === examSessionId.toLowerCase());
  if (!row) return fail("Không tìm thấy ca thi.");
  const id = crypto.randomUUID();
  const code = studentCode.trim();
  const name = studentName?.trim() ? studentName.trim() : null;
  const detail: ExamSubmissionDetail = {
    id,
    examSessionId: row.id,
    examSessionCode: row.code,
    examSessionClassId: null,
    classCode: null,
    studentCode: code,
    studentName: name,
    status: "Completed",
    submittedAtUtc: new Date().toISOString(),
    totalScore: 8.5,
    submissionFiles: [
      {
        questionLabel: "Q1",
        storageRelativePath: `exam-submissions/mock/${id}/q1.zip`,
        originalFileName: "q1.zip",
      },
      {
        questionLabel: "Q2",
        storageRelativePath: `exam-submissions/mock/${id}/q2.zip`,
        originalFileName: "q2.zip",
      },
    ],
    questionScores: [],
    testCaseScores: [],
  };
  const listItem: ExamSubmissionListItem = {
    id,
    examSessionId: row.id,
    examSessionClassId: null,
    classCode: null,
    studentCode: code,
    studentName: name,
    status: detail.status,
    submittedAtUtc: detail.submittedAtUtc,
    totalScore: detail.totalScore,
  };
  const key = row.id.toLowerCase();
  const next = [...(mockSubmissionsBySession.get(key) ?? []), listItem];
  mockSubmissionsBySession.set(key, next);
  mockSubmissionDetailsById.set(id.toLowerCase(), detail);
  mockExamSessionRows = mockExamSessionRows.map((s) =>
    s.id.toLowerCase() === key ? { ...s, submissionCount: next.length } : s
  );
  return ok(id, "Đã nhận tệp và mô phỏng bước chấm (chế độ thử).");
}

export function mockReplaceSubmissionFile(submissionId: string, questionLabel: string): ApiResult<boolean> {
  if (!mockSubmissionDetailsById.has(submissionId.toLowerCase())) return fail("Không tìm thấy bài nộp.");
  return ok(true, `Đã thay tệp cho ${questionLabel}. Hãy bấm chấm lại để cập nhật điểm.`);
}

export function mockTriggerRegrade(submissionId: string): ApiResult<TriggerRegradeResponse> {
  if (!mockSubmissionDetailsById.has(submissionId.toLowerCase())) return fail("Không tìm thấy bài nộp.");
  return ok(
    {
      gradingJobId: crypto.randomUUID(),
      trigger: "ManualRegrade",
      jobStatus: "Completed",
      message: "Chấm lại thành công (chế độ thử).",
    },
    "Chấm lại thành công (chế độ thử)."
  );
}

export function delay(ms: number): Promise<void> {
  return new Promise((r) => setTimeout(r, ms));
}
