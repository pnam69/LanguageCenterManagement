using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Services.Ai
{
    public class AiUsageTrackingService
    {
        private readonly ApplicationDbContext _context;

        public AiUsageTrackingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> StartAsync(
            string userId,
            string feature,
            int requestedCount = 1,
            CancellationToken cancellationToken = default)
        {
            var log = new AiUsageLog
            {
                UserId = userId,
                Feature = feature,
                Status = "Started",
                RequestedCount = Math.Clamp(requestedCount, 1, 10),
                StartedAt = DateTime.UtcNow
            };

            _context.AiUsageLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            return log.AiUsageLogId;
        }

        public async Task CompleteAsync(
            int logId,
            string status,
            int generatedCount = 0,
            string? errorMessage = null)
        {
            var log = await _context.AiUsageLogs
                .FirstOrDefaultAsync(x => x.AiUsageLogId == logId);

            if (log == null)
                return;

            log.Status = status;
            log.GeneratedCount = generatedCount;
            log.CompletedAt = DateTime.UtcNow;

            log.ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? null
                : errorMessage.Length <= 1000
                    ? errorMessage
                    : errorMessage[..1000];

            await _context.SaveChangesAsync();
        }
    }
}