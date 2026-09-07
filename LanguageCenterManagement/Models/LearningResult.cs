using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class LearningResult
    {
        public int LearningResultId { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public int ClassId { get; set; }
        public LanguageClass? Class { get; set; }

        [Range(0, 10)]
        public decimal AverageScore { get; set; }

        [StringLength(20)]
        public string Result { get; set; } = "InProgress";

        [StringLength(500)]
        public string? TeacherComment { get; set; }

        public DateTime? CompletedDate { get; set; }
    }
}