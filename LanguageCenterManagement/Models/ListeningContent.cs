using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class ListeningContent
    {
        public int ListeningContentId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string AudioUrl { get; set; } = string.Empty;

        public string? Transcript { get; set; }

        public ICollection<Question> Questions { get; set; }
            = new List<Question>();
    }
}