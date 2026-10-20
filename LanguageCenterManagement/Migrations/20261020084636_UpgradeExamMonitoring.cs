using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageCenterManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeExamMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "ExamMonitoringEvents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExamResultId",
                table: "ExamMonitoringEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "ExamMonitoringEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "ExamMonitoringEvents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "ExamMonitoringEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "ExamMonitoringEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamMonitoringEvents_ExamResultId",
                table: "ExamMonitoringEvents",
                column: "ExamResultId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamMonitoringEvents_ExamResults_ExamResultId",
                table: "ExamMonitoringEvents",
                column: "ExamResultId",
                principalTable: "ExamResults",
                principalColumn: "ExamResultId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamMonitoringEvents_ExamResults_ExamResultId",
                table: "ExamMonitoringEvents");

            migrationBuilder.DropIndex(
                name: "IX_ExamMonitoringEvents_ExamResultId",
                table: "ExamMonitoringEvents");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "ExamMonitoringEvents");

            migrationBuilder.DropColumn(
                name: "ExamResultId",
                table: "ExamMonitoringEvents");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "ExamMonitoringEvents");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "ExamMonitoringEvents");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ExamMonitoringEvents");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "ExamMonitoringEvents");
        }
    }
}
