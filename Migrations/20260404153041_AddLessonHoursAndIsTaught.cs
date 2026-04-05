using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vyuka.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonHoursAndIsTaught : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Hours",
                table: "Lessons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsTaught",
                table: "Lessons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hours",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "IsTaught",
                table: "Lessons");
        }
    }
}
