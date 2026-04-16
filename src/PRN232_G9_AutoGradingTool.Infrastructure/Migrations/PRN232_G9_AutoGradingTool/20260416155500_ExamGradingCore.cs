using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRN232_G9_AutoGradingTool.Infrastructure.Migrations.PRN232_G9_AutoGradingTool
{
    /// <inheritdoc />
    public partial class ExamGradingCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "semesters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    start_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_semesters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exam_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    semester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    scheduled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_exam_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_sessions_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exam_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    student_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    workflow_status = table.Column<int>(type: "integer", nullable: false),
                    q1zip_relative_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    q2zip_relative_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    total_score = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_exam_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_submissions_exam_sessions_exam_session_id",
                        column: x => x.exam_session_id,
                        principalTable: "exam_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_topics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_exam_topics", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_topics_exam_sessions_exam_session_id",
                        column: x => x.exam_session_id,
                        principalTable: "exam_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_topic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    max_score = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
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
                    table.PrimaryKey("pk_exam_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_questions_exam_topics_exam_topic_id",
                        column: x => x.exam_topic_id,
                        principalTable: "exam_topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_question_scores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    max_score = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("pk_exam_question_scores", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_question_scores_exam_questions_exam_question_id",
                        column: x => x.exam_question_id,
                        principalTable: "exam_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exam_question_scores_exam_submissions_exam_submission_id",
                        column: x => x.exam_submission_id,
                        principalTable: "exam_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_test_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    max_points = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_exam_test_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_test_cases_exam_questions_exam_question_id",
                        column: x => x.exam_question_id,
                        principalTable: "exam_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_test_case_scores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points_earned = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    max_points = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("pk_exam_test_case_scores", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_test_case_scores_exam_submissions_exam_submission_id",
                        column: x => x.exam_submission_id,
                        principalTable: "exam_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exam_test_case_scores_exam_test_cases_exam_test_case_id",
                        column: x => x.exam_test_case_id,
                        principalTable: "exam_test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_exam_question_scores_exam_question_id",
                table: "exam_question_scores",
                column: "exam_question_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_question_scores_exam_submission_id_exam_question_id",
                table: "exam_question_scores",
                columns: new[] { "exam_submission_id", "exam_question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_examquestionscore_is_deleted",
                table: "exam_question_scores",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examquestionscore_is_deleted_status_updated_at",
                table: "exam_question_scores",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examquestionscore_status",
                table: "exam_question_scores",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examquestionscore_status_is_deleted",
                table: "exam_question_scores",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examquestionscore_updated_at",
                table: "exam_question_scores",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_exam_questions_exam_topic_id",
                table: "exam_questions",
                column: "exam_topic_id");

            migrationBuilder.CreateIndex(
                name: "ix_examquestion_is_deleted",
                table: "exam_questions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examquestion_is_deleted_status_updated_at",
                table: "exam_questions",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examquestion_status",
                table: "exam_questions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examquestion_status_is_deleted",
                table: "exam_questions",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examquestion_updated_at",
                table: "exam_questions",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_exam_sessions_semester_id_code",
                table: "exam_sessions",
                columns: new[] { "semester_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_examsession_is_deleted",
                table: "exam_sessions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examsession_is_deleted_status_updated_at",
                table: "exam_sessions",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examsession_status",
                table: "exam_sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examsession_status_is_deleted",
                table: "exam_sessions",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examsession_updated_at",
                table: "exam_sessions",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_exam_submissions_exam_session_id_student_code",
                table: "exam_submissions",
                columns: new[] { "exam_session_id", "student_code" });

            migrationBuilder.CreateIndex(
                name: "ix_examsubmission_is_deleted",
                table: "exam_submissions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examsubmission_is_deleted_status_updated_at",
                table: "exam_submissions",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examsubmission_status",
                table: "exam_submissions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examsubmission_status_is_deleted",
                table: "exam_submissions",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examsubmission_updated_at",
                table: "exam_submissions",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_exam_test_case_scores_exam_submission_id_exam_test_case_id",
                table: "exam_test_case_scores",
                columns: new[] { "exam_submission_id", "exam_test_case_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exam_test_case_scores_exam_test_case_id",
                table: "exam_test_case_scores",
                column: "exam_test_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_examtestcasescore_is_deleted",
                table: "exam_test_case_scores",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examtestcasescore_is_deleted_status_updated_at",
                table: "exam_test_case_scores",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examtestcasescore_status",
                table: "exam_test_case_scores",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examtestcasescore_status_is_deleted",
                table: "exam_test_case_scores",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examtestcasescore_updated_at",
                table: "exam_test_case_scores",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_exam_test_cases_exam_question_id",
                table: "exam_test_cases",
                column: "exam_question_id");

            migrationBuilder.CreateIndex(
                name: "ix_examtestcase_is_deleted",
                table: "exam_test_cases",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examtestcase_is_deleted_status_updated_at",
                table: "exam_test_cases",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examtestcase_status",
                table: "exam_test_cases",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examtestcase_status_is_deleted",
                table: "exam_test_cases",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examtestcase_updated_at",
                table: "exam_test_cases",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_exam_topics_exam_session_id",
                table: "exam_topics",
                column: "exam_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_examtopic_is_deleted",
                table: "exam_topics",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examtopic_is_deleted_status_updated_at",
                table: "exam_topics",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examtopic_status",
                table: "exam_topics",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examtopic_status_is_deleted",
                table: "exam_topics",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examtopic_updated_at",
                table: "exam_topics",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_semester_is_deleted",
                table: "semesters",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_semester_is_deleted_status_updated_at",
                table: "semesters",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_semester_status",
                table: "semesters",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_semester_status_is_deleted",
                table: "semesters",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_semester_updated_at",
                table: "semesters",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_semesters_code",
                table: "semesters",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exam_question_scores");

            migrationBuilder.DropTable(
                name: "exam_test_case_scores");

            migrationBuilder.DropTable(
                name: "exam_submissions");

            migrationBuilder.DropTable(
                name: "exam_test_cases");

            migrationBuilder.DropTable(
                name: "exam_questions");

            migrationBuilder.DropTable(
                name: "exam_topics");

            migrationBuilder.DropTable(
                name: "exam_sessions");

            migrationBuilder.DropTable(
                name: "semesters");
        }
    }
}
