using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class AiQuotaSettings
    {
        public int AiQuotaSettingsId { get; set; }

        [Range(1, 1000)]
        public int TeacherDailyQuestionLimit { get; set; } = 100;

        [Range(1, 1000)]
        public int StudentDailyQuestionLimit { get; set; } = 20;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
