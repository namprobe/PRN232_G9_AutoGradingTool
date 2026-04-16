using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Seeding;

public static class ExamGradingSeeder
{
    public static async Task SeedAsync(PRN232_G9_AutoGradingToolDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await db.Semesters.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Exam grading seed skipped (already present).");
            return;
        }

        var now = DateTime.UtcNow;

        var semesterId = Guid.Parse("b1000000-0000-4000-8000-000000000001");
        var sessionId = Guid.Parse("b1000000-0000-4000-8000-000000000002");
        var topicId = Guid.Parse("b1000000-0000-4000-8000-000000000003");
        var q1Id = Guid.Parse("b1000000-0000-4000-8000-000000000011");
        var q2Id = Guid.Parse("b1000000-0000-4000-8000-000000000012");
        var tcQ1a = Guid.Parse("b1000000-0000-4000-8000-000000000021");
        var tcQ1b = Guid.Parse("b1000000-0000-4000-8000-000000000022");
        var tcQ2a = Guid.Parse("b1000000-0000-4000-8000-000000000031");
        var tcQ2b = Guid.Parse("b1000000-0000-4000-8000-000000000032");
        var sampleSubId = Guid.Parse("b1000000-0000-4000-8000-00000000feed");

        var semester = new Semester
        {
            Id = semesterId,
            Code = "SPRING2026",
            Name = "Spring 2026",
            StartDateUtc = now,
            EndDateUtc = now.AddMonths(5),
            CreatedAt = now,
            Status = EntityStatusEnum.Active
        };

        var session = new ExamSession
        {
            Id = sessionId,
            SemesterId = semesterId,
            Code = "PRN232-DEMO-PE",
            Title = "Practical Exam — PRN232 (demo)",
            ScheduledAtUtc = now.AddDays(14),
            CreatedAt = now,
            Status = EntityStatusEnum.Active
        };

        var topic = new ExamTopic
        {
            Id = topicId,
            ExamSessionId = sessionId,
            Title = "Đề thi thực hành",
            SortOrder = 1,
            CreatedAt = now,
            Status = EntityStatusEnum.Active
        };

        var q1 = new ExamQuestion
        {
            Id = q1Id,
            ExamTopicId = topicId,
            Label = "Q1",
            Title = "REST + EF (zip)",
            MaxScore = 5,
            CreatedAt = now,
            Status = EntityStatusEnum.Active
        };

        var q2 = new ExamQuestion
        {
            Id = q2Id,
            ExamTopicId = topicId,
            Label = "Q2",
            Title = "MVC + GivenAPI (zip)",
            MaxScore = 5,
            CreatedAt = now,
            Status = EntityStatusEnum.Active
        };

        var tcs = new[]
        {
            new ExamTestCase { Id = tcQ1a, ExamQuestionId = q1Id, Name = "Build & migrations", MaxPoints = 2.5m, SortOrder = 1, CreatedAt = now, Status = EntityStatusEnum.Active },
            new ExamTestCase { Id = tcQ1b, ExamQuestionId = q1Id, Name = "API copies endpoints", MaxPoints = 2.5m, SortOrder = 2, CreatedAt = now, Status = EntityStatusEnum.Active },
            new ExamTestCase { Id = tcQ2a, ExamQuestionId = q2Id, Name = "Views & routing", MaxPoints = 2.5m, SortOrder = 1, CreatedAt = now, Status = EntityStatusEnum.Active },
            new ExamTestCase { Id = tcQ2b, ExamQuestionId = q2Id, Name = "HttpClient integration", MaxPoints = 2.5m, SortOrder = 2, CreatedAt = now, Status = EntityStatusEnum.Active },
        };

        var sample = new ExamSubmission
        {
            Id = sampleSubId,
            ExamSessionId = sessionId,
            StudentCode = "HE186501",
            StudentName = "Bài mẫu (seed)",
            WorkflowStatus = ExamSubmissionStatus.Completed,
            Q1ZipRelativePath = "seed/demo-q1.zip",
            Q2ZipRelativePath = "seed/demo-q2.zip",
            TotalScore = 8.5m,
            SubmittedAtUtc = now.AddMinutes(-30),
            CreatedAt = now,
            Status = EntityStatusEnum.Active
        };

        db.Semesters.Add(semester);
        db.ExamSessions.Add(session);
        db.ExamTopics.Add(topic);
        db.ExamQuestions.AddRange(q1, q2);
        db.ExamTestCases.AddRange(tcs);
        db.ExamSubmissions.Add(sample);

        await db.SaveChangesAsync(cancellationToken);

        // Điểm mẫu cho GET chi tiết (Swagger)
        db.ExamQuestionScores.AddRange(
            new ExamQuestionScore
            {
                Id = Guid.NewGuid(),
                ExamSubmissionId = sampleSubId,
                ExamQuestionId = q1Id,
                Score = 4.25m,
                MaxScore = 5,
                Summary = "Seed",
                CreatedAt = now,
                Status = EntityStatusEnum.Active
            },
            new ExamQuestionScore
            {
                Id = Guid.NewGuid(),
                ExamSubmissionId = sampleSubId,
                ExamQuestionId = q2Id,
                Score = 4.25m,
                MaxScore = 5,
                Summary = "Seed",
                CreatedAt = now,
                Status = EntityStatusEnum.Active
            });

        db.ExamTestCaseScores.AddRange(
            new ExamTestCaseScore { Id = Guid.NewGuid(), ExamSubmissionId = sampleSubId, ExamTestCaseId = tcQ1a, PointsEarned = 2.13m, MaxPoints = 2.5m, Outcome = ExamTestCaseOutcome.Pass, Message = "Seed", CreatedAt = now, Status = EntityStatusEnum.Active },
            new ExamTestCaseScore { Id = Guid.NewGuid(), ExamSubmissionId = sampleSubId, ExamTestCaseId = tcQ1b, PointsEarned = 2.12m, MaxPoints = 2.5m, Outcome = ExamTestCaseOutcome.Pass, Message = "Seed", CreatedAt = now, Status = EntityStatusEnum.Active },
            new ExamTestCaseScore { Id = Guid.NewGuid(), ExamSubmissionId = sampleSubId, ExamTestCaseId = tcQ2a, PointsEarned = 2.13m, MaxPoints = 2.5m, Outcome = ExamTestCaseOutcome.Pass, Message = "Seed", CreatedAt = now, Status = EntityStatusEnum.Active },
            new ExamTestCaseScore { Id = Guid.NewGuid(), ExamSubmissionId = sampleSubId, ExamTestCaseId = tcQ2b, PointsEarned = 2.12m, MaxPoints = 2.5m, Outcome = ExamTestCaseOutcome.Pass, Message = "Seed", CreatedAt = now, Status = EntityStatusEnum.Active });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded exam grading demo (semester + session + sample submission).");
    }
}
