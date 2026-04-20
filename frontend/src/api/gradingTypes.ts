/** Khớp DTO backend (JSON camelCase). */

export type SemesterListItem = {
  id: string;
  code: string;
  name: string;
  startDateUtc: string | null;
  endDateUtc: string | null;
};

export type ExamSessionListItem = {
  id: string;
  code: string;
  title: string;
  semesterId: string;
  semesterCode: string;
  startsAtUtc: string;
  examDurationMinutes: number;
  endsAtUtc: string;
  topicCount: number;
  questionCount: number;
  submissionCount: number;
};

export type ExamTestCaseDetail = {
  id: string;
  name: string;
  maxPoints: number;
  sortOrder: number;
};

export type ExamQuestionDetail = {
  id: string;
  label: string;
  title: string;
  maxScore: number;
  testCases: ExamTestCaseDetail[];
};

export type ExamTopicDetail = {
  id: string;
  title: string;
  sortOrder: number;
  questions: ExamQuestionDetail[];
};

export type ExamSessionDetail = {
  id: string;
  code: string;
  title: string;
  semesterId: string;
  semesterCode: string;
  startsAtUtc: string;
  examDurationMinutes: number;
  endsAtUtc: string;
  topics: ExamTopicDetail[];
};

export type ExamSubmissionListItem = {
  id: string;
  examSessionId: string;
  studentCode: string;
  studentName: string | null;
  status: string;
  submittedAtUtc: string;
  totalScore: number | null;
};

export type ExamQuestionScore = {
  examQuestionId: string;
  questionLabel: string;
  score: number;
  maxScore: number;
  summary: string | null;
};

export type ExamTestCaseScore = {
  examTestCaseId: string;
  questionLabel: string;
  testCaseName: string;
  pointsEarned: number;
  maxPoints: number;
  outcome: string;
  message: string | null;
};

export type SubmissionFile = {
  questionLabel: string;
  storageRelativePath: string;
  originalFileName: string | null;
};

export type ExamSubmissionDetail = {
  id: string;
  examSessionId: string;
  examSessionCode: string;
  studentCode: string;
  studentName: string | null;
  status: string;
  submittedAtUtc: string;
  totalScore: number | null;
  submissionFiles: SubmissionFile[];
  questionScores: ExamQuestionScore[];
  testCaseScores: ExamTestCaseScore[];
};

export type TriggerRegradeResponse = {
  gradingJobId: string;
  trigger: string;
  jobStatus: string;
  message: string;
};
