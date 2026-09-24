using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class QuestionManagementViewModel
    {
        public int QuestionId { get; set; }

        [Required]
        [Display(Name = "Question")]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Question Type")]
        public string QuestionType { get; set; } = "MultipleChoice";

        [Required]
        [StringLength(20)]
        public string Skill { get; set; } = "Reading";

        [Range(1, 100)]
        public int Score { get; set; } = 1;

        public List<AnswerManagementViewModel> Answers { get; set; }
            = new List<AnswerManagementViewModel>();
    }

    public class AnswerManagementViewModel
    {
        public int AnswerId { get; set; }

        public string AnswerText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}