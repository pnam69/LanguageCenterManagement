using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class ExamResultFormViewModel
    {
        public int ExamResultId { get; set; }

        [Required]
        [Display(Name = "Exam")]
        public int ExamId { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Range(0, 10)]
        public decimal Score { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Completed";

        public DateTime? SubmittedAt { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}