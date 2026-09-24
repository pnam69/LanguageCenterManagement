using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class ListeningContentViewModel
    {
        public int ListeningContentId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Audio URL")]
        public string AudioUrl { get; set; } = string.Empty;

        [Display(Name = "Transcript")]
        public string? Transcript { get; set; }
    }
}