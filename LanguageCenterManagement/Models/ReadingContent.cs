using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class ReadingContent
    {
        public int ReadingContentId { get; set; }

        public int QuestionId { get; set; }

        public Question? Question { get; set; }

        [Required]
        public string Passage { get; set; } = string.Empty;
    }
}