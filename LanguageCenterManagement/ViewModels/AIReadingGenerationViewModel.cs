using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class AIReadingGenerationViewModel
    {
        [Required]
        [StringLength(150)]
        [Display(Name = "Topic")]
        public string Topic { get; set; } = string.Empty;

        [Required]
        [Display(Name = "English Level")]
        public string Level { get; set; } = "A2";

        [Range(100, 1000)]
        [Display(Name = "Target Word Count")]
        public int WordCount { get; set; } = 300;

        [StringLength(200)]
        [Display(Name = "Vocabulary or Grammar Focus")]
        public string? Focus { get; set; }

        [Required]
        [Display(Name = "Generated Passage")]
        public string Passage { get; set; } = string.Empty;
    }
}