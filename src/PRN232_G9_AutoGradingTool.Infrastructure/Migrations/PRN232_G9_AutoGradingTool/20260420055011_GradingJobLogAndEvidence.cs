using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRN232_G9_AutoGradingTool.Infrastructure.Migrations.PRN232_G9_AutoGradingTool
{
    /// <inheritdoc />
    public partial class GradingJobLogAndEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "q1zip_relative_path",
                table: "exam_submissions");

            migrationBuilder.DropColumn(
                name: "q2zip_relative_path",
                table: "exam_submissions");

            migrationBuilder.RenameColumn(
                name: "scheduled_at_utc",
                table: "exam_sessions",
                newName: "starts_at_utc");

            migrationBuilder.AddColumn<string>(
                name: "hangfire_job_id",
                table: "grading_jobs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retry_count",
                table: "grading_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "raw_output_json",
                table: "exam_test_case_scores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ends_at_utc",
                table: "exam_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "exam_duration_minutes",
                table: "exam_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.CreateTable(
                name: "exam_submission_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_label = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    storage_relative_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exam_submission_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_submission_files_exam_submissions_exam_submission_id",
                        column: x => x.exam_submission_id,
                        principalTable: "exam_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grading_job_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grading_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    detail_json = table.Column<string>(type: "text", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grading_job_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_grading_job_logs_grading_jobs_grading_job_id",
                        column: x => x.grading_job_id,
                        principalTable: "grading_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_grading_jobs_hangfire_job_id",
                table: "grading_jobs",
                column: "hangfire_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_submission_files_exam_submission_id_question_label",
                table: "exam_submission_files",
                columns: new[] { "exam_submission_id", "question_label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_examsubmissionfile_is_deleted",
                table: "exam_submission_files",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examsubmissionfile_is_deleted_status_updated_at",
                table: "exam_submission_files",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examsubmissionfile_status",
                table: "exam_submission_files",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examsubmissionfile_status_is_deleted",
                table: "exam_submission_files",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examsubmissionfile_updated_at",
                table: "exam_submission_files",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_grading_job_logs_grading_job_id_occurred_at_utc",
                table: "grading_job_logs",
                columns: new[] { "grading_job_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_grading_job_logs_grading_job_id_phase",
                table: "grading_job_logs",
                columns: new[] { "grading_job_id", "phase" });

            migrationBuilder.CreateIndex(
                name: "ix_gradingjoblog_is_deleted",
                table: "grading_job_logs",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_gradingjoblog_is_deleted_status_updated_at",
                table: "grading_job_logs",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_gradingjoblog_status",
                table: "grading_job_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_gradingjoblog_status_is_deleted",
                table: "grading_job_logs",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_gradingjoblog_updated_at",
                table: "grading_job_logs",
                column: "updated_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exam_submission_files");

            migrationBuilder.DropTable(
                name: "grading_job_logs");

            migrationBuilder.DropIndex(
                name: "ix_grading_jobs_hangfire_job_id",
                table: "grading_jobs");

            migrationBuilder.DropColumn(
                name: "hangfire_job_id",
                table: "grading_jobs");

            migrationBuilder.DropColumn(
                name: "retry_count",
                table: "grading_jobs");

            migrationBuilder.DropColumn(
                name: "raw_output_json",
                table: "exam_test_case_scores");

            migrationBuilder.DropColumn(
                name: "ends_at_utc",
                table: "exam_sessions");

            migrationBuilder.DropColumn(
                name: "exam_duration_minutes",
                table: "exam_sessions");

            migrationBuilder.RenameColumn(
                name: "starts_at_utc",
                table: "exam_sessions",
                newName: "scheduled_at_utc");

            migrationBuilder.AddColumn<string>(
                name: "q1zip_relative_path",
                table: "exam_submissions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "q2zip_relative_path",
                table: "exam_submissions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }
    }
}
