using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class SpeakingContent
    {
        public int SpeakingContentId { get; set; }

        public int QuestionId { get; set; }

        public Question? Question { get; set; }

        [Required]
        public string Prompt { get; set; } = string.Empty;

        [Range(0, 60)]
        public int PreparationTime { get; set; } = 30;

        [Range(0, 600)]
        public int ResponseTime { get; set; } = 120;
    }
}