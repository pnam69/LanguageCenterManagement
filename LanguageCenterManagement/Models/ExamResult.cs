using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class ExamResult
    {
        public int ExamResultId { get; set; }

        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Range(0, 10)]
        public decimal Score { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Completed";

        public DateTime? SubmittedAt { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}