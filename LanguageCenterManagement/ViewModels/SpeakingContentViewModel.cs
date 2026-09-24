using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class SpeakingContentViewModel
    {
        public int SpeakingContentId { get; set; }

        [Required]
        [Display(Name = "Prompt")]
        public string Prompt { get; set; } = string.Empty;

        [Range(0, 60)]
        [Display(Name = "Preparation Time (seconds)")]
        public int PreparationTime { get; set; } = 30;

        [Range(0, 600)]
        [Display(Name = "Response Time (seconds)")]
        public int ResponseTime { get; set; } = 120;
    }
}