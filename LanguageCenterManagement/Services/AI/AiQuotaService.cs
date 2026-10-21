using System.Data;
using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Services.Ai
{
    public class AiQuotaService
    {
        private readonly ApplicationDbContext _context;

        public AiQuotaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Allowed, int Limit, int Used, int? LogId)>
            ReserveTeacherQuotaAsync(
                string userId,
                int requestedCount,
                CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);

            var settings = await _context.AiQuotaSettings
                .AsNoTracking()
                .OrderBy(s => s.AiQuotaSettingsId)
                .FirstOrDefaultAsync(cancellationToken);

            int limit = settings?.TeacherDailyQuestionLimit ?? 100;

            var now = DateTime.UtcNow;
            var startOfDay = now.Date;
            var startOfTomorrow = startOfDay.AddDays(1);

            int used = await _context.AiUsageLogs
                .Where(log =>
                    log.UserId == userId &&
                    log.StartedAt >= startOfDay &&
                    log.StartedAt < startOfTomorrow &&
                    (log.Status == "Started" ||
                     log.Status == "Succeeded"))
                .SumAsync(
                    log => (int?)log.RequestedCount,
                    cancellationToken) ?? 0;

            if (used + requestedCount > limit)
            {
                await transaction.CommitAsync(cancellationToken);
                return (false, limit, used, null);
            }

            var logEntry = new AiUsageLog
            {
                UserId = userId,
                Feature = "QuestionGeneration",
                Status = "Started",
                RequestedCount = requestedCount,
                GeneratedCount = 0,
                StartedAt = now
            };

            _context.AiUsageLogs.Add(logEntry);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (true, limit, used, logEntry.AiUsageLogId);
        }

        public async Task CompleteAsync(
            int logId,
            string status,
            int generatedCount,
            string? errorMessage = null,
            CancellationToken cancellationToken = default)
        {
            var logEntry = await _context.AiUsageLogs
                .FirstOrDefaultAsync(
                    log => log.AiUsageLogId == logId,
                    cancellationToken);

            if (logEntry == null)
                return;

            logEntry.Status = status;
            logEntry.GeneratedCount = generatedCount;
            logEntry.CompletedAt = DateTime.UtcNow;
            logEntry.ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? null
                : errorMessage.Length <= 1000
                    ? errorMessage
                    : errorMessage[..1000];

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
