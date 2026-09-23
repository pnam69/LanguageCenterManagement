using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageCenterManagement.Migrations
{
    /// <inheritdoc />
    public partial class questionsandsuch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListeningContentId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ListeningContent",
                columns: table => new
                {
                    ListeningContentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AudioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Transcript = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListeningContent", x => x.ListeningContentId);
                });

            migrationBuilder.CreateTable(
                name: "ReadingContent",
                columns: table => new
                {
                    ReadingContentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Passage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingContent", x => x.ReadingContentId);
                    table.ForeignKey(
                        name: "FK_ReadingContent_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpeakingContent",
                columns: table => new
                {
                    SpeakingContentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreparationTime = table.Column<int>(type: "int", nullable: false),
                    ResponseTime = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingContent", x => x.SpeakingContentId);
                    table.ForeignKey(
                        name: "FK_SpeakingContent_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WritingContent",
                columns: table => new
                {
                    WritingContentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinimumWords = table.Column<int>(type: "int", nullable: false),
                    MaximumWords = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingContent", x => x.WritingContentId);
                    table.ForeignKey(
                        name: "FK_WritingContent_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ListeningContentId",
                table: "Questions",
                column: "ListeningContentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingContent_QuestionId",
                table: "ReadingContent",
                column: "QuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingContent_QuestionId",
                table: "SpeakingContent",
                column: "QuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingContent_QuestionId",
                table: "WritingContent",
                column: "QuestionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_ListeningContent_ListeningContentId",
                table: "Questions",
                column: "ListeningContentId",
                principalTable: "ListeningContent",
                principalColumn: "ListeningContentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_ListeningContent_ListeningContentId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "ListeningContent");

            migrationBuilder.DropTable(
                name: "ReadingContent");

            migrationBuilder.DropTable(
                name: "SpeakingContent");

            migrationBuilder.DropTable(
                name: "WritingContent");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ListeningContentId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ListeningContentId",
                table: "Questions");
        }
    }
}
