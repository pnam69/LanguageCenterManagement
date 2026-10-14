using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageCenterManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingPassageQuestionRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReadingPassageId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ReadingPassageId",
                table: "Questions",
                column: "ReadingPassageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions",
                column: "ReadingPassageId",
                principalTable: "ReadingPassages",
                principalColumn: "ReadingPassageId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ReadingPassageId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ReadingPassageId",
                table: "Questions");
        }
    }
}
