using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class AiUsageLog
    {
        public int AiUsageLogId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Feature { get; set; } = "QuestionGeneration";

        [Required, StringLength(20)]
        public string Status { get; set; } = "Started";

        [Range(1, 10)]
        public int RequestedCount { get; set; }

        public int GeneratedCount { get; set; } = 0;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }
    }
}
