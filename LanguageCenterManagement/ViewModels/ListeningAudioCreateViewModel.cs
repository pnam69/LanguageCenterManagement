using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class ListeningAudioCreateViewModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Audio File")]
        public IFormFile? AudioFile { get; set; }

        [Display(Name = "Transcript")]
        public string? Transcript { get; set; }

        public int ListeningContentId { get; set; }
    }
}