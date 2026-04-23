# Workflow Overview - PRN232 Auto Grading Tool

This document summarizes the workflow we completed for the grading pipeline and presents it in several diagram styles for quick review.

## Scope

The implemented workflow covers these steps:

1. Extract submission zip files.
2. Detect `Q1_*` and `Q2_*` project folders.
3. Run the student application with `dotnet`.
4. Load test cases from the database.
5. Build Newman request items.
6. Build Newman test scripts.
7. Build the Postman collection JSON.
8. Run Newman against the student app.
9. Capture Newman stdout/stderr output.
10. Parse Newman JSON into structured result details.
11. Calculate total score from passed test cases.
12. Save `TestResult` and `TestResultDetail` to the database.
13. Clean up temporary folders and processes.

## State Diagram

The submission grading state is a simple lifecycle:

```mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> Queued: trigger grading
    Queued --> Running: worker starts
    Running --> Completed: all steps succeed
    Running --> Failed: any step fails
    Completed --> [*]
    Failed --> [*]
```

## Logical Diagram

This diagram shows the logical components and how the grading pipeline moves data between them:

```mermaid
flowchart LR
    Student[Student Zip Files] --> Extract[ZipExtractionHelper\nExtractZip]
    Extract --> Detect[DetectProjects]
    Detect --> RunApp[RunApp\nStudent API]
    RunApp --> Newman[RunNewman]
    Newman --> Capture[CaptureProcessOutputAsync]
    Capture --> Parse[ParseNewmanTestResults]
    Parse --> Score[CalculateTotalScore]
    Score --> Save[SaveGradingResultAsync]
    Save --> DB[(Database)]
    Save --> Cleanup[CleanupResources]
```

## Use Case Diagram

Main actors and their responsibilities:

```mermaid
flowchart LR
    Admin[Admin]
    Proctor[Proctor]
    Student[Student]
    Worker[Grading Worker]

    UC1((Prepare exam session))
    UC2((Upload grading pack))
    UC3((Submit Q1/Q2 zip))
    UC4((Run grading pipeline))
    UC5((Review result))
    UC6((Cleanup resources))

    Proctor --> UC1
    Proctor --> UC2
    Student --> UC3
    Worker --> UC4
    Worker --> UC6
    Admin --> UC5
```

## Activity Diagram

This diagram shows the operational activity sequence inside the grader:

```mermaid
flowchart TD
    A([Start]) --> B[Extract zip]
    B --> C[Detect Q1/Q2 folders]
    C --> D[Launch student app]
    D --> E[Run Newman]
    E --> F[Capture Newman output]
    F --> G[Parse test results]
    G --> H[Compute score]
    H --> I[Persist TestResult / TestResultDetail]
    I --> J[Kill process and delete temp folder]
    J --> K([End])
```

## Start-to-Finish Sequence Diagram

This is the diagram type that shows how the workflow starts and completes across components. It is the clearest view of the whole pipeline from trigger to cleanup.

```mermaid
sequenceDiagram
    participant Worker as Grading Worker
    participant FS as File Storage
    participant App as Student App
    participant Newman as Newman
    participant DB as Database

    Worker->>FS: Extract submission zip
    Worker->>Worker: Detect projects and prepare temp folder
    Worker->>App: Start student application
    Worker->>Newman: Run collection against baseUrl
    Newman->>App: HTTP requests
    App-->>Newman: HTTP responses
    Newman-->>Worker: JSON report
    Worker->>Worker: Parse results and calculate total score
    Worker->>DB: Save TestResult + TestResultDetail
    Worker->>Worker: Kill process and delete temp folder
    Worker-->>DB: Mark grading completed
```

## Result Model

The completed backend now stores grading output using the result model:

- `TestResult` stores the submission-level total score and overall test status.
- `TestResultDetail` stores each parsed test case result, including pass/fail, score, response time, and raw output.
- The submission read API returns `resultDetails` to the frontend.

## Notes

- The workflow is task-driven and completed in order from T01 to T13.
- The grader is designed to be data-driven from database test cases, not hardcoded per exam.
- Cleanup is best-effort so temporary files and processes do not leak across runs.