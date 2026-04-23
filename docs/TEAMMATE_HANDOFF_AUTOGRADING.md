# Teammate Handoff - Auto Grading Flow and CQRS Refactor Plan

## Related Docs

- [Plan](./PLAN.md)
- [Workflow Overview](./WORKFLOW_OVERVIEW.md)
- [Implementation Guide](./IMPLEMENTATION_GUIDE.md)

## Purpose

This document is a teammate-facing handoff for the current auto grading implementation.
It summarizes:

- what is already implemented
- which APIs and files are involved in each flow
- what has been changed recently
- what still needs to be refactored to follow the CQRS pattern in `docs/IMPLEMENTATION_GUIDE.md`

## Current Big Picture

There are 2 main implemented flows:

1. Create exam session -> schedule Hangfire job -> wake grading jobs after session end
2. Student/CMS batch submit -> store topic-aware submission files -> wait for grading pipeline

The grading pipeline is now topic-aware:

- submission zip files are stored by `session.Code/{topicId}/{studentFolder}/Qx/solution.zip`
- the grading worker resolves `ExamTopic` from `ExamSubmissionFile.StorageRelativePath`
- only the matched topic/question test cases are used when building Newman runs

## Flow 1 - Create Exam Session -> Schedule Summary Job

### API entrypoint

- [GradingController.cs](../src/PRN232_G9_AutoGradingTool.API/Controllers/Cms/GradingController.cs)

Relevant endpoint:

- `POST /api/cms/grading/exam-sessions`

What it does now:

- uses `[AuthorizeRoles()]`
- converts request DTO to `CreateExamSessionCommand`
- sends request through MediatR instead of calling admin service directly

### CQRS handler

- [CreateExamSessionCommand.cs](../src/PRN232_G9_AutoGradingTool.Application/Features/ExamSessions/Commands/CreateExamSession/CreateExamSessionCommand.cs)
- [CreateExamSessionCommandHandler.cs](../src/PRN232_G9_AutoGradingTool.Application/Features/ExamSessions/Commands/CreateExamSession/CreateExamSessionCommandHandler.cs)

What the handler is responsible for:

- validate current user through `ICurrentUserService`
- require role `SystemAdmin` or `Instructor`
- validate semester existence
- validate duplicate session code inside the same semester
- create `ExamSession`
- set audit fields via `InitializeEntity(userId)`
- call job service to schedule the summary job
- save returned Hangfire schedule id into `ExamSession.HangfireScheduleJobId`

### Job scheduling service

- [ExamGradingJobService.cs](../src/PRN232_G9_AutoGradingTool.Infrastructure/Services/ExamGradingJobService.cs)

Implemented method:

- `ScheduleSummarizeExamResultJob(Guid examSessionId, DateTime endsAtUtc)`

What it does:

- calculates delay from `EndsAtUtc`
- schedules `SummarizeExamResultJob.ExecuteAsync(...)`
- returns Hangfire job id

### Related entity

- [ExamSession.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamSession.cs)

Important fields used by this flow:

- `SemesterId`
- `Code`
- `Title`
- `StartsAtUtc`
- `ExamDurationMinutes`
- `EndsAtUtc`
- `DeferredClassGrading`
- `HangfireScheduleJobId`

## Flow 2 - Batch Submit Zips From Student or CMS

### API entrypoints

- [StudentGradingController.cs](../src/PRN232_G9_AutoGradingTool.API/Controllers/Student/StudentGradingController.cs)
- [GradingController.cs](../src/PRN232_G9_AutoGradingTool.API/Controllers/Cms/GradingController.cs)

Relevant endpoints:

- `POST /api/student/grading/exam-sessions/{sessionId}/submissions/batch`
- `POST /api/cms/grading/exam-sessions/{sessionId}/submissions/batch`

Difference:

- student flow checks exam window
- CMS flow bypasses exam window

Both endpoints send the same CQRS command:

- [BatchSubmitZipsCommand.cs](../src/PRN232_G9_AutoGradingTool.Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommand.cs)

### Request DTOs

- [BatchSubmitZipsDtos.cs](../src/PRN232_G9_AutoGradingTool.Application/Common/DTOs/ExamGrading/BatchSubmitZipsDtos.cs)

Important change:

- `StudentZipEntry` now contains `ExamTopicId`

This is the key to topic-aware grading because upload path and later grading resolution both depend on topic id.

### CQRS handler

- [BatchSubmitZipsCommandHandler.cs](../src/PRN232_G9_AutoGradingTool.Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommandHandler.cs)

What the handler does:

- load `ExamSession` with topics
- if student flow, validate `StartsAtUtc <= now <= EndsAtUtc`
- validate that `ExamTopicId` belongs to the session
- prevent duplicate submission per student per session
- build storage directory using:

```text
session.Code/{topicId:N}/{studentFolder}
```

- upload:

```text
.../Q1/solution.zip
.../Q2/solution.zip
```

- create `ExamSubmission`
- create `ExamSubmissionFile` records with `QuestionLabel` and `StorageRelativePath`

### Related entities

- [ExamSubmission.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamSubmission.cs)
- [ExamSubmissionFile.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamSubmissionFile.cs)

## Flow 3 - Session End -> Create Grading Jobs

### Scheduled worker

- [SummarizeExamResultJob.cs](../src/PRN232_G9_AutoGradingTool.Infrastructure/Jobs/SummarizeExamResultJob.cs)

What it does:

1. load `ExamSession`
2. skip if fired too early
3. find active `ExamGradingPack`
4. find pending submissions
5. skip submissions that have no `SubmissionFiles`
6. create one `GradingJob` per ready submission
7. mark submission workflow as `Queued`
8. enqueue `GradeSubmissionJob`
9. save `HangfireJobId` back into `GradingJob`

### Related entities

- [GradingJob.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/GradingJob.cs)
- [GradingJobLog.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/GradingJobLog.cs)

## Flow 4 - Grade Submission -> Persist Results

### Worker

- [GradeSubmissionJob.cs](../src/PRN232_G9_AutoGradingTool.Infrastructure/Jobs/GradeSubmissionJob.cs)

What it does end-to-end:

1. load `GradingJob`, `ExamSubmission`, `SubmissionFiles`, and existing `TestResult`
2. load `ExamSession` with `Topics -> Questions -> TestCases`
3. resolve topic-aware grading plan from file paths
4. mark grading job as running
5. download zip files from file storage
6. extract zip files to temp folder
7. detect published app folder per question label
8. build Newman collection only for the matched `ExamQuestion`
9. start student apps on free localhost ports
10. run Newman against each app
11. parse raw Newman output
12. compute score only for the resolved topic
13. persist result tables
14. mark submission and job completed
15. cleanup temp files and processes

### Topic-aware resolution

Current worker behavior is aligned with [Workflow Overview](./WORKFLOW_OVERVIEW.md):

- resolves `ExamTopic` from `StorageRelativePath`
- validates all files in one submission belong to the same topic
- validates `/Qx/` folder against `QuestionLabel`
- uses only that topic's questions and test cases for grading

### Persisted result entities

- [TestResult.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/TestResult.cs)
- [TestResultDetail.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/TestResultDetail.cs)
- [ExamQuestionScore.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamQuestionScore.cs)
- [ExamTestCaseScore.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamTestCaseScore.cs)

## APIs Already Updated

These APIs are already part of the current grading flow and should be considered the implemented baseline:

### CQRS-based now

- `POST /api/cms/grading/exam-sessions`
- `POST /api/cms/grading/exam-sessions/{sessionId}/submissions/batch`
- `POST /api/student/grading/exam-sessions/{sessionId}/submissions/batch`

### Behavior already aligned with grading pipeline

- create session schedules summary job
- batch submit stores files by topic-aware path
- session-end job fans out grading jobs
- grade worker persists result tables

## Important Files By Responsibility

### Controllers

- [GradingController.cs](../src/PRN232_G9_AutoGradingTool.API/Controllers/Cms/GradingController.cs)
- [StudentGradingController.cs](../src/PRN232_G9_AutoGradingTool.API/Controllers/Student/StudentGradingController.cs)

### Commands and handlers

- [CreateExamSessionCommand.cs](../src/PRN232_G9_AutoGradingTool.Application/Features/ExamSessions/Commands/CreateExamSession/CreateExamSessionCommand.cs)
- [CreateExamSessionCommandHandler.cs](../src/PRN232_G9_AutoGradingTool.Application/Features/ExamSessions/Commands/CreateExamSession/CreateExamSessionCommandHandler.cs)
- [BatchSubmitZipsCommand.cs](../src/PRN232_G9_AutoGradingTool.Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommand.cs)
- [BatchSubmitZipsCommandHandler.cs](../src/PRN232_G9_AutoGradingTool.Application/Features/Submissions/Commands/BatchSubmitZips/BatchSubmitZipsCommandHandler.cs)

### Job orchestration

- [ExamGradingJobService.cs](../src/PRN232_G9_AutoGradingTool.Infrastructure/Services/ExamGradingJobService.cs)
- [SummarizeExamResultJob.cs](../src/PRN232_G9_AutoGradingTool.Infrastructure/Jobs/SummarizeExamResultJob.cs)
- [GradeSubmissionJob.cs](../src/PRN232_G9_AutoGradingTool.Infrastructure/Jobs/GradeSubmissionJob.cs)

### Admin service still owning non-CQRS grading CRUD

- [ExamGradingAdminService.cs](../src/PRN232_G9_AutoGradingTool.Infrastructure/Services/ExamGradingAdminService.cs)

### Core grading entities

- [Semester.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/Semester.cs)
- [ExamSession.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamSession.cs)
- [ExamTopic.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamTopic.cs)
- [ExamQuestion.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamQuestion.cs)
- [ExamTestCase.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamTestCase.cs)

## Current Architecture Status vs CQRS Guide

### Already following CQRS well

- create exam session
- batch submit zips

### Still not fully following CQRS

Many grading management APIs in `GradingController` still call `_admin` service directly instead of MediatR command/query handlers.

Examples:

- semester create/update/delete
- exam session update/delete
- topic create/update/delete
- question create/update/delete
- test case create/update/delete
- grading pack CRUD
- exam class and session class CRUD

This means the current codebase is mixed:

- some parts already follow `Controller -> Mediator -> Handler`
- some parts still follow `Controller -> AdminService`

## Recommended Next Refactor Backlog

The next APIs to refactor should be grouped by entity and priority.

### Priority 1 - Core grading structure

#### Semester

Entity:

- [Semester.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/Semester.cs)

Needed APIs:

- create semester
- update semester
- delete semester
- get semesters/list semesters

Reason:

- this is the top-level root for exam session management

#### ExamSession

Entity:

- [ExamSession.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamSession.cs)

Needed APIs:

- update exam session
- delete exam session
- get exam session detail
- list exam sessions

Note:

- create exam session is already CQRS-based
- update/delete/list/get should be moved to command/query handlers next

### Priority 2 - Exam content definition

#### ExamTopic

Entity:

- [ExamTopic.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamTopic.cs)

Needed APIs:

- create topic
- update topic
- delete topic
- get topic detail if needed

#### ExamQuestion

Entity:

- [ExamQuestion.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamQuestion.cs)

Needed APIs:

- create question
- update question
- delete question

#### ExamTestCase

Entity:

- [ExamTestCase.cs](../src/PRN232_G9_AutoGradingTool.Domain/Entities/ExamTestCase.cs)

Needed APIs:

- create test case
- update test case
- delete test case

Reason:

- this entity chain defines the actual grading source of truth:

```text
Semester -> ExamSession -> ExamTopic -> ExamQuestion -> ExamTestCase
```

### Priority 3 - Job operation APIs

These endpoints exist but are not fully implemented in the underlying job service:

- `POST /api/cms/grading/exam-session-classes/{id}/start-batch-grading`
- `PUT /api/cms/grading/submissions/{id}/files`
- `POST /api/cms/grading/submissions/{id}/regrade`

Current related file:

- [ExamGradingJobService.cs](../src/PRN232_G9_AutoGradingTool.Infrastructure/Services/ExamGradingJobService.cs)

Current status:

- methods still return `InvalidOperation`
- real implementation is still pending

### Priority 4 - Query/dashboard APIs

These should become query handlers for consistency:

- list exam sessions
- get exam session detail
- list submissions
- get submission detail
- list semesters
- list grading packs
- list exam classes
- list session classes

## Recommended CQRS Folder Structure For Next Work

For each entity, follow the pattern from [Implementation Guide](./IMPLEMENTATION_GUIDE.md):

```text
Application/Features/{Feature}/Commands/{ActionName}/
Application/Features/{Feature}/Queries/{ActionName}/
```

Suggested features:

- `Features/Semesters`
- `Features/ExamSessions`
- `Features/ExamTopics`
- `Features/ExamQuestions`
- `Features/ExamTestCases`
- `Features/GradingPacks`
- `Features/ExamClasses`
- `Features/ExamSessionClasses`

## Suggested Team Split

### Member A

- Semester CQRS
- ExamSession update/delete/list/detail CQRS

### Member B

- ExamTopic CQRS
- ExamQuestion CQRS
- ExamTestCase CQRS

### Member C

- Manual regrade implementation
- Replace submission file implementation
- Start class batch grading implementation

### Member D

- Query cleanup for dashboard/list/detail endpoints
- Swagger cleanup and endpoint consistency review

## Notes For Teammates

- use MediatR for all new APIs
- keep controllers thin
- use `ICurrentUserService` in handlers that require authorization
- use `InitializeEntity`, `UpdateEntity`, and soft delete helpers consistently
- schedule/enqueue jobs only after database save succeeds
- preserve topic-aware path contract introduced in [Plan](./PLAN.md)
- when touching grading flow, re-check [Workflow Overview](./WORKFLOW_OVERVIEW.md)

## Short Status Summary

Implemented and working:

- create exam session
- topic-aware batch submit
- session-end grading fan-out
- topic-aware grading execution
- result persistence

Still pending for full CQRS compliance:

- most grading CRUD endpoints in `GradingController`
- manual regrade and class batch grading implementation in job service
- moving remaining direct admin service calls into command/query handlers
