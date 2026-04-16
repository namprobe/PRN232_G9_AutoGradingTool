using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;
using PRN232_G9_AutoGradingTool.Infrastructure.Context;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Seeding;

/// <summary>Gói chấm mặc định cho ca demo — chạy kể cả khi ExamGradingSeeder đã skip (DB đã có semester).</summary>
public static class ExamGradingPackSeeder
{
    public static async Task SeedAsync(PRN232_G9_AutoGradingToolDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.Parse("b1000000-0000-4000-8000-000000000002");
        if (!await db.ExamSessions.AnyAsync(x => x.Id == sessionId, cancellationToken))
        {
            logger.LogInformation("Exam grading pack seed skipped (demo exam session missing).");
            return;
        }

        if (await db.ExamGradingPacks.AnyAsync(x => x.ExamSessionId == sessionId, cancellationToken))
        {
            logger.LogInformation("Exam grading pack seed skipped (pack already exists for demo session).");
            return;
        }

        var tcQ1a = Guid.Parse("b1000000-0000-4000-8000-000000000021");
        var tcQ1b = Guid.Parse("b1000000-0000-4000-8000-000000000022");
        var tcQ2a = Guid.Parse("b1000000-0000-4000-8000-000000000031");
        var tcQ2b = Guid.Parse("b1000000-0000-4000-8000-000000000032");
        var requiredTestCases = new[] { tcQ1a, tcQ1b, tcQ2a, tcQ2b };
        var found = await db.ExamTestCases.CountAsync(t => requiredTestCases.Contains(t.Id), cancellationToken);
        if (found != requiredTestCases.Length)
        {
            logger.LogInformation(
                "Exam grading pack seed skipped (expected {Expected} demo test cases, found {Found}).",
                requiredTestCases.Length,
                found);
            return;
        }

        var now = DateTime.UtcNow;
        var packId = Guid.Parse("b1000000-0000-4000-8000-00000000a701");

        var pack = new ExamGradingPack
        {
            Id = packId,
            ExamSessionId = sessionId,
            Version = 1,
            Label = "Demo pack v1 (stub definitions)",
            IsActive = true,
            CreatedAt = now,
            Status = EntityStatusEnum.Active
        };

        db.ExamGradingPacks.Add(pack);

        var defs = new[]
        {
            new GradingTestDefinition
            {
                Id = Guid.NewGuid(),
                ExamGradingPackId = packId,
                ExamTestCaseId = tcQ1a,
                SortOrder = 1,
                Kind = GradingTestDefinitionKind.Stub,
                Name = "Build & migrations",
                PayloadJson = """{"runner":"stub"}""",
                CreatedAt = now,
                Status = EntityStatusEnum.Active
            },
            new GradingTestDefinition
            {
                Id = Guid.NewGuid(),
                ExamGradingPackId = packId,
                ExamTestCaseId = tcQ1b,
                SortOrder = 2,
                Kind = GradingTestDefinitionKind.Stub,
                Name = "API copies endpoints",
                PayloadJson = """{"runner":"stub"}""",
                CreatedAt = now,
                Status = EntityStatusEnum.Active
            },
            new GradingTestDefinition
            {
                Id = Guid.NewGuid(),
                ExamGradingPackId = packId,
                ExamTestCaseId = tcQ2a,
                SortOrder = 3,
                Kind = GradingTestDefinitionKind.Stub,
                Name = "Views & routing",
                PayloadJson = """{"runner":"stub"}""",
                CreatedAt = now,
                Status = EntityStatusEnum.Active
            },
            new GradingTestDefinition
            {
                Id = Guid.NewGuid(),
                ExamGradingPackId = packId,
                ExamTestCaseId = tcQ2b,
                SortOrder = 4,
                Kind = GradingTestDefinitionKind.Stub,
                Name = "HttpClient integration",
                PayloadJson = """{"runner":"stub"}""",
                CreatedAt = now,
                Status = EntityStatusEnum.Active
            }
        };

        db.GradingTestDefinitions.AddRange(defs);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded default exam grading pack + stub test definitions for demo session.");
    }
}
