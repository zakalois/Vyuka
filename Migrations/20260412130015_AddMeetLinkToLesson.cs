using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vyuka.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetLinkToLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetLink",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_SubjectTopicId",
                table: "Lessons",
                column: "SubjectTopicId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_SubjectTopics_SubjectTopicId",
                table: "Lessons",
                column: "SubjectTopicId",
                principalTable: "SubjectTopics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_SubjectTopics_SubjectTopicId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_SubjectTopicId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "MeetLink",
                table: "Lessons");
        }
    }
}
