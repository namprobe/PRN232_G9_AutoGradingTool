using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRN232_G9_AutoGradingTool.Infrastructure.Migrations.PRN232_G9_AutoGradingTool
{
    /// <inheritdoc />
    public partial class UpdateTestCaseAndTestResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_exam_test_cases_exam_question_id",
                table: "exam_test_cases",
                newName: "IX_TestCases_ExamQuestionId");

            migrationBuilder.AddColumn<string>(
                name: "expected_body",
                table: "exam_test_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "expected_status",
                table: "exam_test_cases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_hidden",
                table: "exam_test_cases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "method",
                table: "exam_test_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "order_index",
                table: "exam_test_cases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "request_body",
                table: "exam_test_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "exam_test_cases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "url_template",
                table: "exam_test_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "weight",
                table: "exam_test_cases",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "test_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_score = table.Column<double>(type: "double precision", nullable: false),
                    test_status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_test_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_results_exam_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "exam_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_result_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    score = table.Column<double>(type: "double precision", nullable: false),
                    response_time = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
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
                    table.PrimaryKey("pk_test_result_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_result_details_exam_test_cases_test_case_id",
                        column: x => x.test_case_id,
                        principalTable: "exam_test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_test_result_details_test_results_result_id",
                        column: x => x.result_id,
                        principalTable: "test_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ExamSessionId",
                table: "exam_submissions",
                column: "exam_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_result_details_result_id",
                table: "test_result_details",
                column: "result_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_result_details_test_case_id",
                table: "test_result_details",
                column: "test_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_testresultdetail_is_deleted",
                table: "test_result_details",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_testresultdetail_is_deleted_status_updated_at",
                table: "test_result_details",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_testresultdetail_status",
                table: "test_result_details",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_testresultdetail_status_is_deleted",
                table: "test_result_details",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_testresultdetail_updated_at",
                table: "test_result_details",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_test_results_submission_id",
                table: "test_results",
                column: "submission_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_testresult_is_deleted",
                table: "test_results",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_testresult_is_deleted_status_updated_at",
                table: "test_results",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_testresult_status",
                table: "test_results",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_testresult_status_is_deleted",
                table: "test_results",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_testresult_updated_at",
                table: "test_results",
                column: "updated_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_result_details");

            migrationBuilder.DropTable(
                name: "test_results");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_ExamSessionId",
                table: "exam_submissions");

            migrationBuilder.DropColumn(
                name: "expected_body",
                table: "exam_test_cases");

            migrationBuilder.DropColumn(
                name: "expected_status",
                table: "exam_test_cases");

            migrationBuilder.DropColumn(
                name: "is_hidden",
                table: "exam_test_cases");

            migrationBuilder.DropColumn(
                name: "method",
                table: "exam_test_cases");

            migrationBuilder.DropColumn(
                name: "order_index",
                table: "exam_test_cases");

            migrationBuilder.DropColumn(
                name: "request_body",
                table: "exam_test_cases");

            migrationBuilder.DropColumn(
                name: "type",
                table: "exam_test_cases");

            migrationBuilder.DropColumn(
                name: "url_template",
                table: "exam_test_cases");

            migrationBuilder.DropColumn(
                name: "weight",
                table: "exam_test_cases");

            migrationBuilder.RenameIndex(
                name: "IX_TestCases_ExamQuestionId",
                table: "exam_test_cases",
                newName: "ix_exam_test_cases_exam_question_id");
        }
    }
}
