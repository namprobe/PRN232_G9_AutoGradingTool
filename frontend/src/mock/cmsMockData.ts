/** Dữ liệu mẫu P4 — thay bằng gọi API khi P1–P3 xong */

export type ExamSessionRow = {
  id: string;
  code: string;
  title: string;
  semester: string;
  scheduledAt: string;
  topicCount: number;
  questionCount: number;
  submissionCount: number;
  status: "draft" | "active" | "closed";
};

export type SubmissionRow = {
  id: string;
  studentCode: string;
  studentName: string;
  examSessionCode: string;
  submittedAt: string;
  q1Status: "pending" | "grading" | "graded" | "error";
  q2Status: "pending" | "grading" | "graded" | "error";
  totalScore: number | null;
  maxScore: number;
};

export type TestCaseResult = {
  id: string;
  questionLabel: string;
  name: string;
  weight: number;
  earned: number | null;
  max: number;
  status: "pass" | "fail" | "pending" | "error";
  message?: string;
};

export const MOCK_EXAM_SESSIONS: ExamSessionRow[] = [
  {
    id: "es-1",
    code: "PRN232-PE-2026",
    title: "Practical Exam — PRN232",
    semester: "Spring 2026",
    scheduledAt: "2026-05-12T08:00:00Z",
    topicCount: 3,
    questionCount: 2,
    submissionCount: 52,
    status: "active",
  },
  {
    id: "es-2",
    code: "PRN232-MID",
    title: "Midterm Auto Lab",
    semester: "Spring 2026",
    scheduledAt: "2026-03-18T13:30:00Z",
    topicCount: 2,
    questionCount: 2,
    submissionCount: 120,
    status: "closed",
  },
  {
    id: "es-3",
    code: "PRN232-DRAFT",
    title: "Summer mock session",
    semester: "Summer 2026",
    scheduledAt: "2026-07-01T09:00:00Z",
    topicCount: 1,
    questionCount: 2,
    submissionCount: 0,
    status: "draft",
  },
];

export const MOCK_SUBMISSIONS: SubmissionRow[] = [
  {
    id: "sub-1",
    studentCode: "HE186501",
    studentName: "Nguyễn Văn A",
    examSessionCode: "PRN232-PE-2026",
    submittedAt: "2026-04-15T07:42:11Z",
    q1Status: "graded",
    q2Status: "graded",
    totalScore: 8.5,
    maxScore: 10,
  },
  {
    id: "sub-2",
    studentCode: "HE186502",
    studentName: "Trần Thị B",
    examSessionCode: "PRN232-PE-2026",
    submittedAt: "2026-04-15T07:55:03Z",
    q1Status: "graded",
    q2Status: "grading",
    totalScore: null,
    maxScore: 10,
  },
  {
    id: "sub-3",
    studentCode: "HE186503",
    studentName: "Lê Văn C",
    examSessionCode: "PRN232-PE-2026",
    submittedAt: "2026-04-15T08:01:44Z",
    q1Status: "error",
    q2Status: "pending",
    totalScore: null,
    maxScore: 10,
  },
];

export const MOCK_TESTCASES_BY_SUBMISSION: Record<string, TestCaseResult[]> = {
  "sub-1": [
    {
      id: "tc-q1-1",
      questionLabel: "Q1",
      name: "Build & migrations",
      weight: 0.25,
      earned: 2.5,
      max: 2.5,
      status: "pass",
    },
    {
      id: "tc-q1-2",
      questionLabel: "Q1",
      name: "POST /api/copies — happy path",
      weight: 0.25,
      earned: 2,
      max: 2.5,
      status: "fail",
      message: "Expected 201, received 400",
    },
    {
      id: "tc-q2-1",
      questionLabel: "Q2",
      name: "MVC views render",
      weight: 0.25,
      earned: 2,
      max: 2.5,
      status: "pass",
    },
    {
      id: "tc-q2-2",
      questionLabel: "Q2",
      name: "HttpClient GivenAPI integration",
      weight: 0.25,
      earned: 2,
      max: 2.5,
      status: "pass",
    },
  ],
  "sub-2": [
    {
      id: "tc-p1",
      questionLabel: "Q1",
      name: "Smoke compile",
      weight: 0.5,
      earned: 2.5,
      max: 2.5,
      status: "pass",
    },
    {
      id: "tc-p2",
      questionLabel: "Q2",
      name: "Integration suite",
      weight: 0.5,
      earned: null,
      max: 2.5,
      status: "pending",
    },
  ],
};

export function getSubmissionById(id: string): SubmissionRow | undefined {
  return MOCK_SUBMISSIONS.find((s) => s.id === id);
}

export function getTestCasesForSubmission(id: string): TestCaseResult[] {
  return MOCK_TESTCASES_BY_SUBMISSION[id] ?? [];
}
