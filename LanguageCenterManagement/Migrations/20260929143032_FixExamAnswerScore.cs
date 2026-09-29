using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageCenterManagement.Migrations
{
    /// <inheritdoc />
    public partial class FixExamAnswerScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamAnswer_Answers_AnswerId",
                table: "ExamAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAnswer_ExamResults_ExamResultId",
                table: "ExamAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAnswer_Questions_QuestionId",
                table: "ExamAnswer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamAnswer",
                table: "ExamAnswer");

            migrationBuilder.RenameTable(
                name: "ExamAnswer",
                newName: "ExamAnswers");

            migrationBuilder.RenameIndex(
                name: "IX_ExamAnswer_QuestionId",
                table: "ExamAnswers",
                newName: "IX_ExamAnswers_QuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamAnswer_ExamResultId",
                table: "ExamAnswers",
                newName: "IX_ExamAnswers_ExamResultId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamAnswer_AnswerId",
                table: "ExamAnswers",
                newName: "IX_ExamAnswers_AnswerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamAnswers",
                table: "ExamAnswers",
                column: "ExamAnswerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAnswers_Answers_AnswerId",
                table: "ExamAnswers",
                column: "AnswerId",
                principalTable: "Answers",
                principalColumn: "AnswerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAnswers_ExamResults_ExamResultId",
                table: "ExamAnswers",
                column: "ExamResultId",
                principalTable: "ExamResults",
                principalColumn: "ExamResultId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAnswers_Questions_QuestionId",
                table: "ExamAnswers",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "QuestionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamAnswers_Answers_AnswerId",
                table: "ExamAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAnswers_ExamResults_ExamResultId",
                table: "ExamAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAnswers_Questions_QuestionId",
                table: "ExamAnswers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamAnswers",
                table: "ExamAnswers");

            migrationBuilder.RenameTable(
                name: "ExamAnswers",
                newName: "ExamAnswer");

            migrationBuilder.RenameIndex(
                name: "IX_ExamAnswers_QuestionId",
                table: "ExamAnswer",
                newName: "IX_ExamAnswer_QuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamAnswers_ExamResultId",
                table: "ExamAnswer",
                newName: "IX_ExamAnswer_ExamResultId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamAnswers_AnswerId",
                table: "ExamAnswer",
                newName: "IX_ExamAnswer_AnswerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamAnswer",
                table: "ExamAnswer",
                column: "ExamAnswerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAnswer_Answers_AnswerId",
                table: "ExamAnswer",
                column: "AnswerId",
                principalTable: "Answers",
                principalColumn: "AnswerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAnswer_ExamResults_ExamResultId",
                table: "ExamAnswer",
                column: "ExamResultId",
                principalTable: "ExamResults",
                principalColumn: "ExamResultId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAnswer_Questions_QuestionId",
                table: "ExamAnswer",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "QuestionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
