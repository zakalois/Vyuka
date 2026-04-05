using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vyuka.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubjectTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectTopics_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    Start = table.Column<TimeSpan>(type: "time", nullable: false),
                    End = table.Column<TimeSpan>(type: "time", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    SubjectTopicId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonPlans_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonPlans_SubjectTopics_SubjectTopicId",
                        column: x => x.SubjectTopicId,
                        principalTable: "SubjectTopics",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LessonPlans_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_StudentId",
                table: "LessonPlans",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_SubjectId",
                table: "LessonPlans",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_SubjectTopicId",
                table: "LessonPlans",
                column: "SubjectTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectTopics_SubjectId",
                table: "SubjectTopics",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonPlans");

            migrationBuilder.DropTable(
                name: "SubjectTopics");
        }
    }
}
