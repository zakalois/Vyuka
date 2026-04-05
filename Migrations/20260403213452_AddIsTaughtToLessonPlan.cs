using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vyuka.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTaughtToLessonPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTaught",
                table: "LessonPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTaught",
                table: "LessonPlans");
        }
    }
}
