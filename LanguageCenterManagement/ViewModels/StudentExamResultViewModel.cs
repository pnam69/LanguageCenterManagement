namespace LanguageCenterManagement.ViewModels
{
    public class StudentExamResultViewModel
    {
        public int ExamResultId { get; set; }

        public int ExamId { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public decimal MaxScore { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? SubmittedAt { get; set; }

        public List<StudentExamResultAnswerViewModel> Answers { get; set; } = new();
    }
}