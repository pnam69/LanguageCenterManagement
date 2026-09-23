using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class WritingContent
    {
        public int WritingContentId { get; set; }

        public int QuestionId { get; set; }

        public Question? Question { get; set; }

        [Required]
        public string Prompt { get; set; } = string.Empty;

        [Range(0, 10000)]
        public int MinimumWords { get; set; } = 100;

        [Range(0, 10000)]
        public int MaximumWords { get; set; } = 300;
    }
}