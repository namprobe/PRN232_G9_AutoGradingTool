using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRN232_G9_AutoGradingTool.Infrastructure.Migrations.PRN232_G9_AutoGradingTool
{
    /// <inheritdoc />
    public partial class ExamClassDeferredGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "exam_session_class_id",
                table: "exam_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "deferred_class_grading",
                table: "exam_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "exam_classes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    semester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    max_students = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_exam_classes", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_classes_semesters_semester_id",
                        column: x => x.semester_id,
                        principalTable: "semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exam_session_classes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expected_student_count = table.Column<int>(type: "integer", nullable: false),
                    batch_status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_exam_session_classes", x => x.id);
                    table.ForeignKey(
                        name: "fk_exam_session_classes_exam_classes_exam_class_id",
                        column: x => x.exam_class_id,
                        principalTable: "exam_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exam_session_classes_exam_sessions_exam_session_id",
                        column: x => x.exam_session_id,
                        principalTable: "exam_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_exam_submissions_exam_session_class_id_student_code",
                table: "exam_submissions",
                columns: new[] { "exam_session_class_id", "student_code" },
                unique: true,
                filter: "\"exam_session_class_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_exam_classes_semester_id_code",
                table: "exam_classes",
                columns: new[] { "semester_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_examclass_is_deleted",
                table: "exam_classes",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examclass_is_deleted_status_updated_at",
                table: "exam_classes",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examclass_status",
                table: "exam_classes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examclass_status_is_deleted",
                table: "exam_classes",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examclass_updated_at",
                table: "exam_classes",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_exam_session_classes_exam_class_id",
                table: "exam_session_classes",
                column: "exam_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_session_classes_exam_session_id_exam_class_id",
                table: "exam_session_classes",
                columns: new[] { "exam_session_id", "exam_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_examsessionclass_is_deleted",
                table: "exam_session_classes",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_examsessionclass_is_deleted_status_updated_at",
                table: "exam_session_classes",
                columns: new[] { "is_deleted", "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_examsessionclass_status",
                table: "exam_session_classes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_examsessionclass_status_is_deleted",
                table: "exam_session_classes",
                columns: new[] { "status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_examsessionclass_updated_at",
                table: "exam_session_classes",
                column: "updated_at");

            migrationBuilder.AddForeignKey(
                name: "fk_exam_submissions_exam_session_classes_exam_session_class_id",
                table: "exam_submissions",
                column: "exam_session_class_id",
                principalTable: "exam_session_classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_exam_submissions_exam_session_classes_exam_session_class_id",
                table: "exam_submissions");

            migrationBuilder.DropTable(
                name: "exam_session_classes");

            migrationBuilder.DropTable(
                name: "exam_classes");

            migrationBuilder.DropIndex(
                name: "ix_exam_submissions_exam_session_class_id_student_code",
                table: "exam_submissions");

            migrationBuilder.DropColumn(
                name: "exam_session_class_id",
                table: "exam_submissions");

            migrationBuilder.DropColumn(
                name: "deferred_class_grading",
                table: "exam_sessions");
        }
    }
}
