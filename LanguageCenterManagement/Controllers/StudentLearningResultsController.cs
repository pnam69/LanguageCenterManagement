using ClosedXML.Excel;
using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentLearningResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentLearningResultsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
                return Forbid();

            var results = await GetStudentResultsAsync(
                user.StudentId.Value);

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
                return Forbid();

            var results = await GetStudentResultsAsync(
                user.StudentId.Value,
                cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Learning Results");

            worksheet.Cell("A1").Value = "Student Learning Results";
            worksheet.Range("A1:G1").Merge();
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 18;

            worksheet.Cell("A2").Value = "Student";
            worksheet.Cell("B2").Value =
                User.Identity?.Name ?? "Student";

            worksheet.Cell("A3").Value = "Generated";
            worksheet.Cell("B3").Value = DateTime.Now;
            worksheet.Cell("B3").Style.DateFormat.Format =
                "yyyy-mm-dd HH:mm";

            string[] headers =
            {
                "Class Code",
                "Class Name",
                "Course",
                "Average Score",
                "Result",
                "Completed Date",
                "Teacher Comment"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(5, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor =
                    XLColor.LightBlue;
                cell.Style.Alignment.WrapText = true;
            }

            int row = 6;

            foreach (var result in results)
            {
                worksheet.Cell(row, 1).Value = result.ClassCode;
                worksheet.Cell(row, 2).Value = result.ClassName;
                worksheet.Cell(row, 3).Value = result.CourseName;

                // Store scores as numbers, not text.
                worksheet.Cell(row, 4).Value =
                    (double)result.AverageScore;

                worksheet.Cell(row, 5).Value = result.Result;

                if (result.CompletedDate.HasValue)
                {
                    worksheet.Cell(row, 6).Value =
                        result.CompletedDate.Value;

                    worksheet.Cell(row, 6).Style.DateFormat.Format =
                        "yyyy-mm-dd";
                }

                worksheet.Cell(row, 7).Value =
                    result.TeacherComment ?? string.Empty;

                row++;
            }

            if (row > 6)
                worksheet.Range(5, 1, row - 1, 7).SetAutoFilter();

            worksheet.SheetView.FreezeRows(5);

            worksheet.Column(1).Width = 16;
            worksheet.Column(2).Width = 26;
            worksheet.Column(3).Width = 24;
            worksheet.Column(4).Width = 16;
            worksheet.Column(5).Width = 18;
            worksheet.Column(6).Width = 18;
            worksheet.Column(7).Width = 50;

            worksheet.Columns(1, 6).Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            worksheet.Column(7).Style.Alignment.WrapText = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"learning-results-{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
                return Forbid();

            var results = await GetStudentResultsAsync(
                user.StudentId.Value,
                cancellationToken);

            var studentName = User.Identity?.Name ?? "Student";
            var generatedAt = DateTime.Now;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(style =>
                        style.FontSize(9));

                    page.Header().Column(column =>
                    {
                        column.Item()
                            .Text("Student Learning Results")
                            .FontSize(20)
                            .Bold();

                        column.Item()
                            .Text($"Student: {studentName}");

                        column.Item()
                            .Text($"Generated: {generatedAt:yyyy-MM-dd HH:mm}");

                        column.Item().PaddingBottom(10);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(65);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.2f);
                            columns.ConstantColumn(65);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                            columns.RelativeColumn(2f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Class Code").Bold();
                            header.Cell().Element(HeaderCell).Text("Class Name").Bold();
                            header.Cell().Element(HeaderCell).Text("Course").Bold();
                            header.Cell().Element(HeaderCell).Text("Score").Bold();
                            header.Cell().Element(HeaderCell).Text("Result").Bold();
                            header.Cell().Element(HeaderCell).Text("Completed").Bold();
                            header.Cell().Element(HeaderCell).Text("Teacher Comment").Bold();
                        });

                        foreach (var result in results)
                        {
                            table.Cell().Element(BodyCell).Text(result.ClassCode);
                            table.Cell().Element(BodyCell).Text(result.ClassName);
                            table.Cell().Element(BodyCell).Text(result.CourseName);
                            table.Cell().Element(BodyCell)
                                .Text(result.AverageScore.ToString("0.##"));
                            table.Cell().Element(BodyCell).Text(result.Result);
                            table.Cell().Element(BodyCell)
                                .Text(result.CompletedDate?.ToString("yyyy-MM-dd") ?? "Not completed");
                            table.Cell().Element(BodyCell)
                                .Text(result.TeacherComment ?? "—");
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated by the Language Center Management system | {generatedAt:yyyy-MM-dd}");
                });
            });

            var pdfBytes = document.GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                $"learning-results-{DateTime.Now:yyyyMMdd}.pdf");
        }

        private async Task<List<StudentLearningResultViewModel>>
            GetStudentResultsAsync(
                int studentId,
                CancellationToken cancellationToken = default)
        {
            return await _context.LearningResults
                .AsNoTracking()
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CompletedDate)
                .ThenBy(r => r.Class!.ClassCode)
                .Select(r => new StudentLearningResultViewModel
                {
                    LearningResultId = r.LearningResultId,
                    ClassCode = r.Class!.ClassCode,
                    ClassName = r.Class.ClassName,
                    CourseName = r.Class.Course!.CourseName,
                    AverageScore = r.AverageScore,
                    Result = r.Result,
                    TeacherComment = r.TeacherComment,
                    CompletedDate = r.CompletedDate
                })
                .ToListAsync(cancellationToken);
        }

        //private static void AddHeader(
        //    TableCellDescriptor header,
        //    string text)
        //{
        //    header.Cell()
        //        .Element(HeaderCell)
        //        .Text(text)
        //        .Bold();
        //}

        //private static void AddCell(
        //    TableDescriptor table,
        //    string text)
        //{
        //    table.Cell()
        //        .Element(BodyCell)
        //        .Text(text);
        //}

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Medium)
                .Padding(5);
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5)
                .MinHeight(24);
        }
    }
}
