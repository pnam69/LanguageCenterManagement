using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageCenterManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMakeUpScheduleToChangeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MakeUpScheduleId",
                table: "ScheduleChangeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleChangeRequests_MakeUpScheduleId",
                table: "ScheduleChangeRequests",
                column: "MakeUpScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleChangeRequests_Schedules_MakeUpScheduleId",
                table: "ScheduleChangeRequests",
                column: "MakeUpScheduleId",
                principalTable: "Schedules",
                principalColumn: "ScheduleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleChangeRequests_Schedules_MakeUpScheduleId",
                table: "ScheduleChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleChangeRequests_MakeUpScheduleId",
                table: "ScheduleChangeRequests");

            migrationBuilder.DropColumn(
                name: "MakeUpScheduleId",
                table: "ScheduleChangeRequests");
        }
    }
}
