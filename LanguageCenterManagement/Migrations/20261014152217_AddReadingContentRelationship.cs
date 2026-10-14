using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageCenterManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingContentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the new nullable relationship column.
            migrationBuilder.AddColumn<int>(
                name: "ReadingContentId",
                table: "Questions",
                type: "int",
                nullable: true);

            // 2. Preserve existing links before dropping the old column.
            migrationBuilder.Sql(@"
        UPDATE q
        SET q.ReadingContentId = rc.ReadingContentId
        FROM Questions q
        INNER JOIN ReadingContent rc
            ON rc.QuestionId = q.QuestionId;
    ");

            // 3. Remove the old ReadingContent -> Question relationship.
            migrationBuilder.DropForeignKey(
                name: "FK_ReadingContent_Questions_QuestionId",
                table: "ReadingContent");

            migrationBuilder.DropIndex(
                name: "IX_ReadingContent_QuestionId",
                table: "ReadingContent");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "ReadingContent");

            // 4. Remove the legacy ReadingPassage relationship.
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions");

            // 5. Create the new ReadingContent relationship.
            migrationBuilder.CreateIndex(
                name: "IX_Questions_ReadingContentId",
                table: "Questions",
                column: "ReadingContentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ReadingContent_ReadingContentId",
                table: "Questions",
                column: "ReadingContentId",
                principalTable: "ReadingContent",
                principalColumn: "ReadingContentId",
                onDelete: ReferentialAction.SetNull);

            // Keep this only if your Question model still has ReadingPassageId.
            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions",
                column: "ReadingPassageId",
                principalTable: "ReadingPassages",
                principalColumn: "ReadingPassageId");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ReadingContent_ReadingContentId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ReadingContentId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ReadingContentId",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "QuestionId",
                table: "ReadingContent",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingContent_QuestionId",
                table: "ReadingContent",
                column: "QuestionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ReadingPassages_ReadingPassageId",
                table: "Questions",
                column: "ReadingPassageId",
                principalTable: "ReadingPassages",
                principalColumn: "ReadingPassageId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingContent_Questions_QuestionId",
                table: "ReadingContent",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "QuestionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
