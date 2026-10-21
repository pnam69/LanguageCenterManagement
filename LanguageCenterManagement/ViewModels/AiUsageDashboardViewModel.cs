namespace LanguageCenterManagement.ViewModels
{
    public class AiUsageDashboardViewModel
    {
        public int DailyQuestionLimit { get; set; }
        public int QuestionsUsedToday { get; set; }
        public int SuccessfulRequestsToday { get; set; }
        public int FailedRequestsToday { get; set; }
        public int CancelledRequestsToday { get; set; }
        public int InProgressRequestsToday { get; set; }

        public List<AiUserUsageViewModel> UserUsage { get; set; } = new();
        public List<AiRecentUsageViewModel> RecentLogs { get; set; } = new();
    }

    public class AiUserUsageViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int QuestionsUsed { get; set; }
        public int QuestionsGenerated { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public int InProgressRequests { get; set; }
    }

    public class AiRecentUsageViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Feature { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public int GeneratedCount { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
