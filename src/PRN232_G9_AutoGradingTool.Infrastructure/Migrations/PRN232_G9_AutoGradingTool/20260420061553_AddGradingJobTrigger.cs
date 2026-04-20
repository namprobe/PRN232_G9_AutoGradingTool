using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRN232_G9_AutoGradingTool.Infrastructure.Migrations.PRN232_G9_AutoGradingTool
{
    /// <inheritdoc />
    public partial class AddGradingJobTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "trigger",
                table: "grading_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trigger",
                table: "grading_jobs");
        }
    }
}
