using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageCenterManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddExamMonitoringEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamMonitoringEvents",
                columns: table => new
                {
                    ExamMonitoringEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamMonitoringEvents", x => x.ExamMonitoringEventId);
                    table.ForeignKey(
                        name: "FK_ExamMonitoringEvents_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "ExamId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamMonitoringEvents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamMonitoringEvents_ExamId",
                table: "ExamMonitoringEvents",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMonitoringEvents_StudentId",
                table: "ExamMonitoringEvents",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamMonitoringEvents");
        }
    }
}
