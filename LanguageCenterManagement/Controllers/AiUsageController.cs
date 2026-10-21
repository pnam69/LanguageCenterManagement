using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class AiUsageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AiUsageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var startOfDay = now.Date;
            var startOfTomorrow = startOfDay.AddDays(1);

            var settings = await _context.AiQuotaSettings
                .AsNoTracking()
                .OrderBy(s => s.AiQuotaSettingsId)
                .FirstOrDefaultAsync();

            var todayLogs = await _context.AiUsageLogs
                .AsNoTracking()
                .Where(log =>
                    log.StartedAt >= startOfDay &&
                    log.StartedAt < startOfTomorrow)
                .OrderByDescending(log => log.StartedAt)
                .ToListAsync();


            var questionFeatures = new[]
            {
                "QuestionGeneration",
                "StudentPracticeGeneration"
            };

            var generatedQuestionLogs = todayLogs
                .Where(log => questionFeatures.Contains(log.Feature))
                .ToList();

            // Only QuestionGeneration is currently enforced by the teacher quota.
            var quotaLogs = generatedQuestionLogs
                .Where(log => log.Feature == "QuestionGeneration")
                .ToList();

            var activeQuotaLogs = quotaLogs
                .Where(log =>
                    log.Status == "Started" ||
                    log.Status == "Succeeded")
                .ToList();


            var userIds = todayLogs
                .Select(log => log.UserId)
                .Distinct()
                .ToList();

            var users = await _context.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id);

            var model = new AiUsageDashboardViewModel
            {
                DailyQuestionLimit =
                    settings?.TeacherDailyQuestionLimit ?? 100,

                QuestionsUsedToday = activeQuotaLogs.Sum(log => log.RequestedCount),
                SuccessfulRequestsToday = todayLogs.Count(
                    log => log.Status == "Succeeded"),

                FailedRequestsToday = todayLogs.Count(
                    log => log.Status == "Failed"),

                CancelledRequestsToday = todayLogs.Count(
                    log => log.Status == "Cancelled"),

                InProgressRequestsToday = todayLogs.Count(
                    log => log.Status == "Started")
            };

            model.UserUsage = todayLogs
                .GroupBy(log => log.UserId)
                .Select(group =>
                {
                    users.TryGetValue(group.Key, out var user);


                    var userQuestionLogs = group
                        .Where(log => questionFeatures.Contains(log.Feature))
                        .ToList();

                    var billableLogs = userQuestionLogs
                        .Where(log =>
                            log.Feature == "QuestionGeneration" &&
                            (log.Status == "Started" ||
                             log.Status == "Succeeded"));


                    return new AiUserUsageViewModel
                    {
                        UserId = group.Key,
                        UserName = user?.UserName ?? "Unknown user",
                        Email = user?.Email ?? "—",
                        QuestionsUsed = billableLogs.Sum(
                            log => log.RequestedCount),
                        QuestionsGenerated = userQuestionLogs.Sum(
                            log => log.GeneratedCount),
                        SuccessfulRequests = group.Count(
                            log => log.Status == "Succeeded"),
                        FailedRequests = group.Count(
                            log => log.Status == "Failed"),
                        InProgressRequests = group.Count(
                            log => log.Status == "Started")
                    };
                })
                .OrderByDescending(user => user.QuestionsUsed)
                .ToList();

            model.RecentLogs = todayLogs
                .Take(20)
                .Select(log =>
                {
                    users.TryGetValue(log.UserId, out var user);

                    return new AiRecentUsageViewModel
                    {
                        UserName = user?.UserName ?? "Unknown user",
                        Feature = log.Feature,
                        Status = log.Status,
                        RequestedCount = log.RequestedCount,
                        GeneratedCount = log.GeneratedCount,
                        StartedAt = log.StartedAt,
                        CompletedAt = log.CompletedAt,
                        ErrorMessage = log.ErrorMessage
                    };
                })
                .ToList();

            return View(model);
        }
    }
}
