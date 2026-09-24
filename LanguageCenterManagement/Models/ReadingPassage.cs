using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class ReadingPassage
    {
        public int ReadingPassageId { get; set; }

        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Passage { get; set; } = string.Empty;
    }
}