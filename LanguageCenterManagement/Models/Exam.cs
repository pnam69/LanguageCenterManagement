using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Exam
    {
        public int ExamId { get; set; }

        public int ClassId { get; set; }
        public LanguageClass? Class { get; set; }

        [Required]
        [StringLength(100)]
        public string ExamName { get; set; } = string.Empty;

        [StringLength(20)]
        public string ExamType { get; set; } = "Test";

        public DateTime ExamDate { get; set; }

        public int Duration { get; set; }

        public decimal MaxScore { get; set; } = 10;

        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        [StringLength(500)]
        public string? Description { get; set; }

        public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();

        public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();

    }
}