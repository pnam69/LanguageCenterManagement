using LanguageCenterManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var startOfDay = now.Date;
            var startOfTomorrow = startOfDay.AddDays(1);

            var todayLogs = _context.AiUsageLogs
                .AsNoTracking()
                .Where(log =>
                    log.StartedAt >= startOfDay &&
                    log.StartedAt < startOfTomorrow);

            ViewBag.AiRequestsToday = await todayLogs.CountAsync();

            ViewBag.AiSuccessfulToday = await todayLogs.CountAsync(
                log => log.Status == "Succeeded");

            ViewBag.AiFailedToday = await todayLogs.CountAsync(
                log => log.Status == "Failed");

            ViewBag.QuestionsUsedToday = await todayLogs
                .Where(log =>
                    log.Feature == "QuestionGeneration" &&
                    (log.Status == "Started" ||
                     log.Status == "Succeeded"))
                .SumAsync(log => (int?)log.RequestedCount) ?? 0;

            ViewBag.PendingIntegrityReviews =
                await _context.WritingIntegrityReviews
                    .AsNoTracking()
                    .CountAsync(r => r.Status == "Pending");

            ViewBag.UpcomingPublishedExams =
                await _context.Exams
                    .AsNoTracking()
                    .CountAsync(e =>
                        e.Status == "Published" &&
                        e.ExamDate >= DateTime.Now);
            // Only use completed, graded exam results.
            var completedResults = await _context.ExamResults
                .AsNoTracking()
                .Where(r =>
                    r.Status == "Completed" &&
                    r.Exam != null &&
                    r.Exam.Class != null &&
                    r.Student != null &&
                    r.Exam.MaxScore > 0)
                .Select(r => new
                {
                    r.StudentId,
                    StudentName = r.Student!.FullName,
                    StudentCode = r.Student.StudentCode,
                    ClassId = r.Exam!.Class!.LanguageClassId,
                    ClassName = r.Exam.Class.ClassName,
                    Score = r.Score,
                    MaxScore = r.Exam.MaxScore
                })
                .ToListAsync();

            // Normalize scores to percentages so exams with different
            // maximum scores can be compared.
            var performanceRows = completedResults
                .Select(r => new
                {
                    r.StudentId,
                    r.StudentName,
                    r.StudentCode,
                    r.ClassId,
                    r.ClassName,
                    Percentage = (double)r.Score / (double)r.MaxScore * 100
                })
                .ToList();

            // Average performance per class.
            ViewBag.ClassPerformance = performanceRows
                .GroupBy(r => new { r.ClassId, r.ClassName })
                .Select(g => new
                {
                    ClassName = g.Key.ClassName,
                    StudentCount = g.Select(r => r.StudentId).Distinct().Count(),
                    ExamCount = g.Count(),
                    AverageScore = g.Average(r => r.Percentage)
                })
                .OrderByDescending(g => g.AverageScore)
                .ToList();

            // Top students by average completed-exam percentage.
            ViewBag.TopStudents = performanceRows
                .GroupBy(r => new
                {
                    r.StudentId,
                    r.StudentName,
                    r.StudentCode
                })
                .Select(g => new
                {
                    StudentName = g.Key.StudentName,
                    StudentCode = g.Key.StudentCode,
                    ExamsCompleted = g.Count(),
                    AverageScore = g.Average(r => r.Percentage)
                })
                .OrderByDescending(s => s.AverageScore)
                .ThenByDescending(s => s.ExamsCompleted)
                .Take(5)
                .ToList();

            // Class performance chart: average percentage from completed exams.
            var classResultsForChart = await _context.ExamResults
                .AsNoTracking()
                .Where(r =>
                    r.Status == "Completed" &&
                    r.Exam != null &&
                    r.Exam.Class != null &&
                    r.Exam.MaxScore > 0)
                .Select(r => new
                {
                    ClassId = r.Exam!.Class!.LanguageClassId,
                    ClassName = r.Exam.Class.ClassName,
                    Score = r.Score,
                    MaxScore = r.Exam.MaxScore
                })
                .ToListAsync();

            ViewBag.ClassPerformanceChart = classResultsForChart
                .GroupBy(r => new { r.ClassId, r.ClassName })
                .Select(g => new
                {
                    ClassName = g.Key.ClassName,
                    AverageScore = Math.Round(
                        g.Average(r =>
                            (double)r.Score / (double)r.MaxScore * 100.0),
                        2)
                })
                .OrderByDescending(x => x.AverageScore)
                .Take(10)
                .ToList();

            // AI usage chart: successful and failed requests over the last 7 days.
            var chartToday = DateTime.UtcNow.Date;
            var chartStartDate = chartToday.AddDays(-6);

            var aiLogsForChart = await _context.AiUsageLogs
                .AsNoTracking()
                .Where(log =>
                    log.StartedAt >= chartStartDate &&
                    log.StartedAt < chartToday.AddDays(1) &&
                    (log.Status == "Succeeded" || log.Status == "Failed"))
                .Select(log => new
                {
                    Day = log.StartedAt.Date,
                    log.Status
                })
                .ToListAsync();

            var aiUsageByDay = aiLogsForChart
                .GroupBy(log => log.Day)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Successful = g.Count(log => log.Status == "Succeeded"),
                        Failed = g.Count(log => log.Status == "Failed")
                    });

            ViewBag.AiUsageChart = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var day = chartStartDate.AddDays(offset);
                    aiUsageByDay.TryGetValue(day, out var usage);

                    return new
                    {
                        Date = day.ToString("MMM dd"),
                        Successful = usage?.Successful ?? 0,
                        Failed = usage?.Failed ?? 0
                    };
                })
                .ToList();

            return View();
        }
    }
}
