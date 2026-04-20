import type { ApiResult } from "./types";
import type {
  ExamSessionDetail,
  ExamSessionListItem,
  ExamSubmissionDetail,
  ExamSubmissionListItem,
  SemesterListItem,
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

export const mockSemesters: ApiResult<SemesterListItem[]> = ok([
  {
    id: DEMO_SEMESTER_ID,
    code: "SPRING2026",
    name: "Spring 2026",
    startDateUtc: "2026-01-15T00:00:00.000Z",
    endDateUtc: "2026-06-30T00:00:00.000Z",
  },
]);

export const mockExamSessions: ApiResult<ExamSessionListItem[]> = ok([
  {
    id: DEMO_EXAM_SESSION_ID,
    code: "PRN232-DEMO-PE",
    title: "Practical Exam — PRN232 (demo)",
    semesterId: DEMO_SEMESTER_ID,
    semesterCode: "SPRING2026",
    startsAtUtc: scheduledAt,
    examDurationMinutes: 90,
    endsAtUtc: endsAt,
    topicCount: 1,
    questionCount: 2,
    submissionCount: 2,
  },
]);

const sessionDetail: ExamSessionDetail = {
  id: DEMO_EXAM_SESSION_ID,
  code: "PRN232-DEMO-PE",
  title: "Practical Exam — PRN232 (demo)",
  semesterId: DEMO_SEMESTER_ID,
  semesterCode: "SPRING2026",
  startsAtUtc: scheduledAt,
  examDurationMinutes: 90,
  endsAtUtc: endsAt,
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

export function mockGetExamSession(id: string): ApiResult<ExamSessionDetail> {
  if (id.toLowerCase() === DEMO_EXAM_SESSION_ID.toLowerCase()) return ok(sessionDetail);
  return fail("Không tìm thấy ca thi.");
}

const sampleDetail: ExamSubmissionDetail = {
  id: DEMO_SAMPLE_SUBMISSION_ID,
  examSessionId: DEMO_EXAM_SESSION_ID,
  examSessionCode: "PRN232-DEMO-PE",
  studentCode: "HE186501",
  studentName: "Bài mẫu (seed)",
  status: "Completed",
  submittedAtUtc: "2026-04-15T07:42:11.000Z",
  totalScore: 8.5,
  q1ZipRelativePath: "seed/demo-q1.zip",
  q2ZipRelativePath: "seed/demo-q2.zip",
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
  studentCode: "HE186502",
  studentName: "Trần Thị B (mock)",
  status: "Running",
  submittedAtUtc: "2026-04-15T07:55:03.000Z",
  totalScore: null,
  q1ZipRelativePath: "exam-submissions/mock/q1.zip",
  q2ZipRelativePath: "exam-submissions/mock/q2.zip",
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

export function mockListSubmissions(examSessionId: string): ApiResult<ExamSubmissionListItem[]> {
  if (examSessionId.toLowerCase() !== DEMO_EXAM_SESSION_ID.toLowerCase()) {
    return fail("Không tìm thấy ca thi.");
  }
  return ok([
    {
      id: DEMO_SAMPLE_SUBMISSION_ID,
      examSessionId: DEMO_EXAM_SESSION_ID,
      studentCode: "HE186501",
      studentName: "Bài mẫu (seed)",
      status: "Completed",
      submittedAtUtc: "2026-04-15T07:42:11.000Z",
      totalScore: 8.5,
    },
    {
      id: MOCK_SUB2_ID,
      examSessionId: DEMO_EXAM_SESSION_ID,
      studentCode: "HE186502",
      studentName: "Trần Thị B (mock)",
      status: "Running",
      submittedAtUtc: "2026-04-15T07:55:03.000Z",
      totalScore: null,
    },
  ]);
}

export function mockGetSubmission(id: string): ApiResult<ExamSubmissionDetail> {
  const x = id.toLowerCase();
  if (x === DEMO_SAMPLE_SUBMISSION_ID.toLowerCase()) return ok(sampleDetail);
  if (x === MOCK_SUB2_ID.toLowerCase()) return ok(sub2Detail);
  return fail("Không tìm thấy bài nộp.");
}

export function mockCreateSubmission(): ApiResult<string> {
  const id = crypto.randomUUID();
  return ok(id, "Đã nhận zip và chạy chấm stub (mock FE).");
}

export function delay(ms: number): Promise<void> {
  return new Promise((r) => setTimeout(r, ms));
}
