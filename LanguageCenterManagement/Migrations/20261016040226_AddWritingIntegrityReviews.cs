using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageCenterManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddWritingIntegrityReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WritingIntegrityReviews",
                columns: table => new
                {
                    WritingIntegrityReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamAnswerId = table.Column<int>(type: "int", nullable: false),
                    MatchedExamAnswerId = table.Column<int>(type: "int", nullable: true),
                    SimilarityPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MatchingText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WritingIntegrityReviews", x => x.WritingIntegrityReviewId);
                    table.ForeignKey(
                        name: "FK_WritingIntegrityReviews_ExamAnswers_ExamAnswerId",
                        column: x => x.ExamAnswerId,
                        principalTable: "ExamAnswers",
                        principalColumn: "ExamAnswerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WritingIntegrityReviews_ExamAnswerId",
                table: "WritingIntegrityReviews",
                column: "ExamAnswerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WritingIntegrityReviews");
        }
    }
}
