using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageCenterManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddProposedRoomToScheduleChangeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProposedRoomId",
                table: "ScheduleChangeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleChangeRequests_ProposedRoomId",
                table: "ScheduleChangeRequests",
                column: "ProposedRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleChangeRequests_Rooms_ProposedRoomId",
                table: "ScheduleChangeRequests",
                column: "ProposedRoomId",
                principalTable: "Rooms",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleChangeRequests_Rooms_ProposedRoomId",
                table: "ScheduleChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleChangeRequests_ProposedRoomId",
                table: "ScheduleChangeRequests");

            migrationBuilder.DropColumn(
                name: "ProposedRoomId",
                table: "ScheduleChangeRequests");
        }
    }
}
