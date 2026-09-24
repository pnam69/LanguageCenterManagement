using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class ReadingContentViewModel
    {
        public int ReadingContentId { get; set; }

        public int QuestionId { get; set; }

        [Required]
        [Display(Name = "Question")]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Passage")]
        public string Passage { get; set; } = string.Empty;

        [Range(1, 100)]
        [Display(Name = "Score")]
        public int Score { get; set; } = 1;
    }
}