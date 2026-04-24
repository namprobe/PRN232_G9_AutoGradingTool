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
  deferredClassGrading: boolean;
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

/** Khớp ExamClassListItemDto */
export type ExamClassListItem = {
  id: string;
  semesterId: string;
  code: string;
  name: string;
  maxStudents: number;
};

/** Khớp ExamSessionClassListItemDto */
export type ExamSessionClassListItem = {
  id: string;
  examSessionId: string;
  examClassId: string;
  examClassCode: string;
  examClassName: string;
  expectedStudentCount: number;
  batchStatus: string;
  readySubmissionCount: number;
  totalSubmissionCount: number;
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
  deferredClassGrading: boolean;
  topics: ExamTopicDetail[];
};

export type ExamSubmissionListItem = {
  id: string;
  examSessionId: string;
  examSessionClassId: string | null;
  classCode: string | null;
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
  examSessionClassId: string | null;
  classCode: string | null;
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

export type StudentSubmitResultItem = {
  studentCode: string;
  success: boolean;
  submissionId: string | null;
  error: string | null;
};

export type BatchSubmitZipsResponse = {
  successCount: number;
  failureCount: number;
  results: StudentSubmitResultItem[];
};

/** —— CMS admin (JSON body khớp record C# camelCase) —— */

export type CreateSemesterBody = {
  code: string;
  name: string;
  startDateUtc: string | null;
  endDateUtc: string | null;
};

export type UpdateSemesterBody = CreateSemesterBody;

export type CreateExamSessionBody = {
  semesterId: string;
  code: string;
  title: string;
  startsAtUtc: string;
  examDurationMinutes: number;
  endsAtUtc: string;
  deferredClassGrading?: boolean;
};

export type UpdateExamSessionBody = CreateExamSessionBody;

export type CreateExamTopicBody = { title: string; sortOrder: number };
export type UpdateExamTopicBody = CreateExamTopicBody;

export type CreateExamQuestionBody = { label: string; title: string; maxScore: number };
export type UpdateExamQuestionBody = CreateExamQuestionBody;

export type CreateExamTestCaseBody = { name: string; maxPoints: number; sortOrder: number };
export type UpdateExamTestCaseBody = CreateExamTestCaseBody;

export type CreateGradingPackBody = { label: string; version: number | null; isActive: boolean };
export type UpdateGradingPackBody = { label: string; isActive: boolean };

export type ExamGradingPackListItem = {
  id: string;
  version: number;
  label: string;
  isActive: boolean;
  assetCount: number;
};

export type ExamPackAssetListItem = {
  id: string;
  examGradingPackId: string;
  kind: number;
  storageRelativePath: string;
  originalFileName: string | null;
};
