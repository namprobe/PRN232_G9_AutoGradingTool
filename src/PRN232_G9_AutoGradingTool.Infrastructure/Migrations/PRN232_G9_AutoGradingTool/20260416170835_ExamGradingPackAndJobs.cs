using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRN232_G9_AutoGradingTool.Infrastructure.Migrations.PRN232_G9_AutoGradingTool
{
    /// <inheritdoc />
    public partial class ExamGradingPackAndJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "exam_grading_pack_id",
                table: "exam_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "exam_grading_packs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_exam_grading_packs", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_grading_packs_exam_sessions_exam_session_id",
                        column: x => x.exam_session_id,
                        principalTable: "exam_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_pack_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_grading_pack_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    storage_relative_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
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
                    table.PrimaryKey("pk_exam_pack_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_pack_assets_exam_grading_packs_exam_grading_pack_id",
                        column: x => x.exam_grading_pack_id,
                        principalTable: "exam_grading_packs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grading_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_grading_pack_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_status = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_grading_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_grading_jobs_exam_grading_packs_exam_grading_pack_id",
                        column: x => x.exam_grading_pack_id,
                        principalTable: "exam_grading_packs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_grading_jobs_exam_submissions_exam_submission_id",
                        column: x => x.exam_submission_id,
                        principalTable: "exam_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grading_test_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_grading_pack_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_test_case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_grading_test_definitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_grading_test_definitions_exam_grading_packs_exam_grading_pa",
                        column: x => x.exam_grading_pack_id,
                        principalTable: "exam_grading_packs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_grading_test_definitions_exam_test_cases_exam_test_case_id",
                        column: x => x.exam_test_case_id,
                        principalTable: "exam_test_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_exam_submissions_exam_grading_pack_id",
                table: "exam_submissions",
                column: "exam_grading_pack_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_grading_packs_exam_session_id_is_active",
                table: "exam_grading_packs",
                columns: new[] { "exam_session_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_exam_grading_packs_exam_session_id_version",
                table: "exam_grading_packs",
                columns: new[] { "exam_session_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_examgradingpack_is_deleted",
                table: "exam_grading_packs",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examgradingpack_is_deleted_status_updated_at",
                table: "exam_grading_packs",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examgradingpack_status",
                table: "exam_grading_packs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examgradingpack_status_is_deleted",
                table: "exam_grading_packs",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examgradingpack_updated_at",
                table: "exam_grading_packs",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_exam_pack_assets_exam_grading_pack_id",
                table: "exam_pack_assets",
                column: "exam_grading_pack_id");

            migrationBuilder.CreateIndex(
                name: "ix_exampackasset_is_deleted",
                table: "exam_pack_assets",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_exampackasset_is_deleted_status_updated_at",
                table: "exam_pack_assets",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_exampackasset_status",
                table: "exam_pack_assets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_exampackasset_status_is_deleted",
                table: "exam_pack_assets",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_exampackasset_updated_at",
                table: "exam_pack_assets",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_grading_jobs_exam_grading_pack_id",
                table: "grading_jobs",
                column: "exam_grading_pack_id");

            migrationBuilder.CreateIndex(
                name: "ix_grading_jobs_exam_submission_id",
                table: "grading_jobs",
                column: "exam_submission_id");

            migrationBuilder.CreateIndex(
                name: "ix_grading_jobs_exam_submission_id_job_status",
                table: "grading_jobs",
                columns: new[] { "exam_submission_id", "job_status" });

            migrationBuilder.CreateIndex(
                name: "ix_gradingjob_is_deleted",
                table: "grading_jobs",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_gradingjob_is_deleted_status_updated_at",
                table: "grading_jobs",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_gradingjob_status",
                table: "grading_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_gradingjob_status_is_deleted",
                table: "grading_jobs",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_gradingjob_updated_at",
                table: "grading_jobs",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_grading_test_definitions_exam_grading_pack_id_sort_order",
                table: "grading_test_definitions",
                columns: new[] { "exam_grading_pack_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_grading_test_definitions_exam_test_case_id",
                table: "grading_test_definitions",
                column: "exam_test_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_gradingtestdefinition_is_deleted",
                table: "grading_test_definitions",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_gradingtestdefinition_is_deleted_status_updated_at",
                table: "grading_test_definitions",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_gradingtestdefinition_status",
                table: "grading_test_definitions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_gradingtestdefinition_status_is_deleted",
                table: "grading_test_definitions",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_gradingtestdefinition_updated_at",
                table: "grading_test_definitions",
                column: "updated_at");

            migrationBuilder.AddForeignKey(
                name: "fk_exam_submissions_exam_grading_packs_exam_grading_pack_id",
                table: "exam_submissions",
                column: "exam_grading_pack_id",
                principalTable: "exam_grading_packs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_exam_submissions_exam_grading_packs_exam_grading_pack_id",
                table: "exam_submissions");

            migrationBuilder.DropTable(
                name: "exam_pack_assets");

            migrationBuilder.DropTable(
                name: "grading_jobs");

            migrationBuilder.DropTable(
                name: "grading_test_definitions");

            migrationBuilder.DropTable(
                name: "exam_grading_packs");

            migrationBuilder.DropIndex(
                name: "ix_exam_submissions_exam_grading_pack_id",
                table: "exam_submissions");

            migrationBuilder.DropColumn(
                name: "exam_grading_pack_id",
                table: "exam_submissions");
        }
    }
}
