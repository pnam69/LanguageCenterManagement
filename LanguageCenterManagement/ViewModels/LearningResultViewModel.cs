using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class LearningResultFormViewModel
    {
        public int LearningResultId { get; set; }

        [Required]
        [Display(Name = "Class")]
        public int ClassId { get; set; }

        [Required]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Range(0, 10)]
        [Display(Name = "Average Score")]
        public decimal AverageScore { get; set; }

        [Required]
        [StringLength(20)]
        public string Result { get; set; } = "InProgress";

        [StringLength(500)]
        [Display(Name = "Teacher Comment")]
        public string? TeacherComment { get; set; }

        [Display(Name = "Completed Date")]
        public DateTime? CompletedDate { get; set; }
    }

    public class StudentLearningResultViewModel
    {
        public int LearningResultId { get; set; }

        public string ClassCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string CourseName { get; set; } = string.Empty;

        public decimal AverageScore { get; set; }

        public string Result { get; set; } = string.Empty;

        public string? TeacherComment { get; set; }

        public DateTime? CompletedDate { get; set; }
    }
}