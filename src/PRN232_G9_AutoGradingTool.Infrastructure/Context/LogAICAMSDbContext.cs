using PRN232_G9_AutoGradingTool.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PRN232_G9_AutoGradingTool.Infrastructure.Configurations;
using System.Linq;

namespace PRN232_G9_AutoGradingTool.Infrastructure.Context;

public class PRN232_G9_AutoGradingToolDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
    public DbSet<ExamTopic> ExamTopics => Set<ExamTopic>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<ExamTestCase> ExamTestCases => Set<ExamTestCase>();
    public DbSet<ExamSubmission> ExamSubmissions => Set<ExamSubmission>();
    public DbSet<ExamQuestionScore> ExamQuestionScores => Set<ExamQuestionScore>();
    public DbSet<ExamTestCaseScore> ExamTestCaseScores => Set<ExamTestCaseScore>();
    public DbSet<ExamGradingPack> ExamGradingPacks => Set<ExamGradingPack>();
    public DbSet<ExamPackAsset> ExamPackAssets => Set<ExamPackAsset>();
    public DbSet<GradingTestDefinition> GradingTestDefinitions => Set<GradingTestDefinition>();
    public DbSet<GradingJob> GradingJobs => Set<GradingJob>();

    public PRN232_G9_AutoGradingToolDbContext(DbContextOptions<PRN232_G9_AutoGradingToolDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Đổi tên bảng Identity sang lowercase để nhất quán với UseSnakeCaseNamingConvention()
        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<AppRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        // AppUser
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.Id).IsUnique();

            // Performance indexes
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.JoiningAt);
            entity.HasIndex(x => x.LastLoginAt).HasFilter("\"last_login_at\" IS NOT NULL");
            entity.HasIndex(x => new { x.Status, x.JoiningAt });
        });

        // AppRole
        builder.Entity<AppRole>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<int>();
        });

        // Configure the base IdentityUserRole<Guid> key
        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
        });

        // Gọi cấu hình chung cho BaseEntity
        BaseEntityConfigurationHelper.ConfigureBaseEntities(builder);

        builder.ConfigureExamGrading();
    }
}
