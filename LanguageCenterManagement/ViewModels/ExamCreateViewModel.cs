using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class ExamCreateViewModel
    {
        [Required]
        public int ClassId { get; set; }

        [Required]
        [StringLength(100)]
        public string ExamName { get; set; } = string.Empty;

        [StringLength(20)]
        public string ExamType { get; set; } = "Test";

        [Required]
        public DateTime ExamDate { get; set; } = DateTime.Now;

        [Range(1, 600)]
        public int Duration { get; set; } = 60;

        [Range(0, 100)]
        public decimal MaxScore { get; set; } = 10;

        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        [StringLength(500)]
        public string? Description { get; set; }

        public List<QuestionSelectionViewModel> Questions { get; set; }
            = new List<QuestionSelectionViewModel>();
    }
}