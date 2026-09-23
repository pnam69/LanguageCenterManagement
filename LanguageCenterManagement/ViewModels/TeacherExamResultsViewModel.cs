using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class TeacherExamResultsViewModel
    {
        public int ExamId { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public decimal MaxScore { get; set; }

        [DataType(DataType.Date)]
        public DateTime ExamDate { get; set; }

        public List<TeacherExamResultStudentViewModel> Students { get; set; }
            = new List<TeacherExamResultStudentViewModel>();
    }

    public class TeacherExamResultStudentViewModel
    {
        public int StudentId { get; set; }

        public string StudentCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        [Range(0, 10)]
        public decimal Score { get; set; }

        public string Status { get; set; } = "Completed";

        public DateTime? SubmittedAt { get; set; }

        public string? Note { get; set; }
    }
}