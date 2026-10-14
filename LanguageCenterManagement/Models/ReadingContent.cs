
using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class ReadingContent
    {
        public int ReadingContentId { get; set; }

        [Required]
        public string Passage { get; set; } = string.Empty;

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
