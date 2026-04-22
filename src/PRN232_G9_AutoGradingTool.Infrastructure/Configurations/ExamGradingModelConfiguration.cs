using Microsoft.EntityFrameworkCore;
using PRN232_G9_AutoGradingTool.Domain.Entities;
using PRN232_G9_AutoGradingTool.Domain.Enums;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Configurations;

public static class ExamGradingModelConfiguration
{
    public static void ConfigureExamGrading(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Semester>(e =>
        {
            e.ToTable("semesters");
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<ExamClass>(e =>
        {
            e.ToTable("exam_classes");
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.MaxStudents).IsRequired();
            e.HasOne(x => x.Semester).WithMany(x => x.ExamClasses).HasForeignKey(x => x.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SemesterId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<ExamSession>(e =>
        {
            e.ToTable("exam_sessions");
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Title).HasMaxLength(512).IsRequired();
            e.Property(x => x.StartsAtUtc).HasColumnType("timestamp with time zone");
            e.Property(x => x.ExamDurationMinutes).HasDefaultValue(90);
            e.Property(x => x.EndsAtUtc).HasColumnType("timestamp with time zone");
            e.Property(x => x.DeferredClassGrading).HasDefaultValue(false);
            e.HasOne(x => x.Semester).WithMany(x => x.ExamSessions).HasForeignKey(x => x.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SemesterId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<ExamSessionClass>(e =>
        {
            e.ToTable("exam_session_classes");
            e.Property(x => x.ExpectedStudentCount).IsRequired();
            e.Property(x => x.BatchStatus).HasColumnName("batch_status").HasConversion<int>();
            e.HasOne(x => x.ExamSession).WithMany(x => x.SessionClasses).HasForeignKey(x => x.ExamSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ExamClass).WithMany(x => x.SessionClasses).HasForeignKey(x => x.ExamClassId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ExamSessionId, x.ExamClassId }).IsUnique();
        });

        modelBuilder.Entity<ExamTopic>(e =>
        {
            e.ToTable("exam_topics");
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.HasOne(x => x.ExamSession).WithMany(x => x.Topics).HasForeignKey(x => x.ExamSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamQuestion>(e =>
        {
            e.ToTable("exam_questions");
            e.Property(x => x.Label).HasMaxLength(16).IsRequired();
            e.Property(x => x.Title).HasMaxLength(512).IsRequired();
            e.Property(x => x.MaxScore).HasPrecision(9, 2);
            e.HasOne(x => x.ExamTopic).WithMany(x => x.Questions).HasForeignKey(x => x.ExamTopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamTestCase>(e =>
        {
            e.ToTable("exam_test_cases");
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.MaxPoints).HasPrecision(9, 2);
            e.HasOne(x => x.ExamQuestion).WithMany(x => x.TestCases).HasForeignKey(x => x.ExamQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ExamQuestionId )
                .HasDatabaseName("IX_TestCases_ExamQuestionId");
        });

        modelBuilder.Entity<ExamSubmission>(e =>
        {
            e.ToTable("exam_submissions");
            e.Property(x => x.StudentCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.StudentName).HasMaxLength(256);
            e.Property(x => x.TotalScore).HasPrecision(9, 2);
            e.Property(x => x.SubmittedAtUtc).HasColumnType("timestamp with time zone");
            e.Property(x => x.WorkflowStatus).HasColumnName("workflow_status").HasConversion<int>();
            e.HasOne(x => x.ExamSession).WithMany(x => x.Submissions).HasForeignKey(x => x.ExamSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ExamSessionClass).WithMany(x => x.Submissions).HasForeignKey(x => x.ExamSessionClassId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ExamGradingPack).WithMany().HasForeignKey(x => x.ExamGradingPackId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.ExamSessionId, x.StudentCode });
            e.HasIndex(x => new { x.ExamSessionClassId, x.StudentCode })
                .IsUnique()
                .HasFilter("\"exam_session_class_id\" IS NOT NULL");
            e.HasOne(s => s.Result)
                .WithOne(tr => tr.Submission)
                .HasForeignKey<TestResult>(tr => tr.SubmissionId);
            e.HasIndex(x => x.ExamSessionId)
                .HasDatabaseName("IX_Submissions_ExamSessionId");
        });

        modelBuilder.Entity<ExamQuestionScore>(e =>
        {
            e.ToTable("exam_question_scores");
            e.Property(x => x.Score).HasPrecision(9, 2);
            e.Property(x => x.MaxScore).HasPrecision(9, 2);
            e.Property(x => x.Summary).HasMaxLength(2000);
            e.HasOne(x => x.ExamSubmission).WithMany(x => x.QuestionScores).HasForeignKey(x => x.ExamSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ExamQuestion).WithMany(x => x.QuestionScores).HasForeignKey(x => x.ExamQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ExamSubmissionId, x.ExamQuestionId }).IsUnique();
        });

        modelBuilder.Entity<ExamTestCaseScore>(e =>
        {
            e.ToTable("exam_test_case_scores");
            e.Property(x => x.PointsEarned).HasPrecision(9, 2);
            e.Property(x => x.MaxPoints).HasPrecision(9, 2);
            e.Property(x => x.Message).HasMaxLength(4000);
            e.Property(x => x.Outcome).HasConversion<int>();
            e.Property(x => x.RawOutputJson).HasColumnType("text");
            e.HasOne(x => x.ExamSubmission).WithMany(x => x.TestCaseScores).HasForeignKey(x => x.ExamSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ExamTestCase).WithMany(x => x.Scores).HasForeignKey(x => x.ExamTestCaseId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ExamSubmissionId, x.ExamTestCaseId }).IsUnique();
        });

        modelBuilder.Entity<ExamGradingPack>(e =>
        {
            e.ToTable("exam_grading_packs");
            e.Property(x => x.Label).HasMaxLength(256).IsRequired();
            e.Property(x => x.Version).IsRequired();
            e.HasOne(x => x.ExamSession).WithMany(x => x.GradingPacks).HasForeignKey(x => x.ExamSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ExamSessionId, x.Version }).IsUnique();
            e.HasIndex(x => new { x.ExamSessionId, x.IsActive });
        });

        modelBuilder.Entity<ExamPackAsset>(e =>
        {
            e.ToTable("exam_pack_assets");
            e.Property(x => x.Kind).HasConversion<int>();
            e.Property(x => x.StorageRelativePath).HasMaxLength(2048).IsRequired();
            e.Property(x => x.OriginalFileName).HasMaxLength(512);
            e.HasOne(x => x.Pack).WithMany(x => x.Assets).HasForeignKey(x => x.ExamGradingPackId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GradingTestDefinition>(e =>
        {
            e.ToTable("grading_test_definitions");
            e.Property(x => x.Kind).HasConversion<int>();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType("text");
            e.HasOne(x => x.Pack).WithMany(x => x.TestDefinitions).HasForeignKey(x => x.ExamGradingPackId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ExamTestCase).WithMany(x => x.GradingDefinitions).HasForeignKey(x => x.ExamTestCaseId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.ExamGradingPackId, x.SortOrder });
        });

        modelBuilder.Entity<GradingJob>(e =>
        {
            e.ToTable("grading_jobs");
            e.Property(x => x.JobStatus).HasColumnName("job_status").HasConversion<int>();
            e.Property(x => x.Trigger).HasColumnName("trigger").HasConversion<int>().HasDefaultValue(GradingJobTrigger.SessionEnd);
            e.Property(x => x.HangfireJobId).HasMaxLength(128);
            e.Property(x => x.ErrorMessage).HasMaxLength(4000);
            e.Property(x => x.StartedAtUtc).HasColumnType("timestamp with time zone");
            e.Property(x => x.FinishedAtUtc).HasColumnType("timestamp with time zone");
            e.HasOne(x => x.ExamSubmission).WithMany(x => x.GradingJobs).HasForeignKey(x => x.ExamSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Pack).WithMany(x => x.Jobs).HasForeignKey(x => x.ExamGradingPackId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ExamSubmissionId);
            e.HasIndex(x => new { x.ExamSubmissionId, x.JobStatus });
            e.HasIndex(x => x.HangfireJobId);
        });

        modelBuilder.Entity<ExamSubmissionFile>(e =>
        {
            e.ToTable("exam_submission_files");
            e.Property(x => x.QuestionLabel).HasMaxLength(16).IsRequired();
            e.Property(x => x.StorageRelativePath).HasMaxLength(1024).IsRequired();
            e.Property(x => x.OriginalFileName).HasMaxLength(512);
            e.HasOne(x => x.ExamSubmission).WithMany(x => x.SubmissionFiles).HasForeignKey(x => x.ExamSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ExamSubmissionId, x.QuestionLabel }).IsUnique();
        });

        modelBuilder.Entity<GradingJobLog>(e =>
        {
            e.ToTable("grading_job_logs");
            e.Property(x => x.Phase).HasConversion<int>();
            e.Property(x => x.Level).HasConversion<int>();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            e.Property(x => x.DetailJson).HasColumnType("text");
            e.Property(x => x.OccurredAtUtc).HasColumnType("timestamp with time zone");
            e.HasOne(x => x.GradingJob).WithMany(x => x.Logs).HasForeignKey(x => x.GradingJobId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.GradingJobId, x.Phase });
            e.HasIndex(x => new { x.GradingJobId, x.OccurredAtUtc });
        });

        modelBuilder.Entity<TestResult>(e =>
        {
            e.ToTable("test_results");
            e.HasOne(tr => tr.Submission)
                .WithOne(s => s.Result)
                .HasForeignKey<TestResult>(tr => tr.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TestResultDetail>(e =>
        {
            e.ToTable("test_result_details");
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasOne(x => x.Result).WithMany(x => x.Details).HasForeignKey(x => x.ResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
