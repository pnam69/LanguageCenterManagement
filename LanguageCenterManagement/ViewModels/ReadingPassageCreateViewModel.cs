using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class ReadingPassageCreateViewModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Passage")]
        public string Passage { get; set; } = string.Empty;
    }
}