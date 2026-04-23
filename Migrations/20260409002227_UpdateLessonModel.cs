using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vyuka.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLessonModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Lessons");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "Lessons",
                newName: "Day");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "End",
                table: "Lessons",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Start",
                table: "Lessons",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "SubjectTopicId",
                table: "Lessons",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "End",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Start",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SubjectTopicId",
                table: "Lessons");

            migrationBuilder.RenameColumn(
                name: "Day",
                table: "Lessons",
                newName: "DurationMinutes");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
