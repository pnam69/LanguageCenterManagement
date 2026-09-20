using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class ExamFormViewModel
    {
        public int ExamId { get; set; }

        [Required]
        [Display(Name = "Class")]
        public int ClassId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Exam Name")]
        public string ExamName { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Exam Type")]
        public string ExamType { get; set; } = "Test";

        [Display(Name = "Exam Date")]
        public DateTime ExamDate { get; set; } = DateTime.Now;

        [Range(1, 600)]
        [Display(Name = "Duration (minutes)")]
        public int Duration { get; set; } = 60;

        [Range(0, 10)]
        [Display(Name = "Maximum Score")]
        public decimal MaxScore { get; set; } = 10;

        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        [StringLength(500)]
        public string? Description { get; set; }

        public List<QuestionSelectionViewModel> Questions { get; set; }
            = new List<QuestionSelectionViewModel>();
    }

    public class QuestionSelectionViewModel
    {
        public int QuestionId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string QuestionType { get; set; } = string.Empty;

        public int Score { get; set; }

        public bool Selected { get; set; }
    }
}