using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LanguageCenterManagement.ViewModels
{
    public class ListeningAudioCreateViewModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Audio File")]
        public IFormFile? AudioFile { get; set; }

        [Display(Name = "Transcript")]
        public string? Transcript { get; set; }
    }
}