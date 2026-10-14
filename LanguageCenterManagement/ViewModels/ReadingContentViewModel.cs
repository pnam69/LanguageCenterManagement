
using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class ReadingContentViewModel
    {
        public int ReadingContentId { get; set; }

        [Required]
        [Display(Name = "Passage")]
        public string Passage { get; set; } = string.Empty;
    }
}
