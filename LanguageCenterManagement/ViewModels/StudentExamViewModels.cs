using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class StudentExamListViewModel
    {
        public int ExamId { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string ExamType { get; set; } = string.Empty;

        public DateTime ExamDate { get; set; }

        public int Duration { get; set; }

        public decimal MaxScore { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool HasSubmitted { get; set; }
    }

    public class StudentExamViewModel
    {
        public int ExamId { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string ExamType { get; set; } = string.Empty;

        public DateTime ExamDate { get; set; }

        public int Duration { get; set; }

        public decimal MaxScore { get; set; }

        public string? Description { get; set; }

        public List<StudentExamQuestionViewModel> Questions { get; set; }
            = new();
    }

    public class StudentExamQuestionViewModel
    {
        public int ExamQuestionId { get; set; }

        public int QuestionId { get; set; }

        public int QuestionOrder { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public int Score { get; set; }

        public List<StudentExamAnswerViewModel> Answers { get; set; }
            = new();

        public int? SelectedAnswerId { get; set; }
    }

    public class StudentExamAnswerViewModel
    {
        public int AnswerId { get; set; }

        public string AnswerText { get; set; } = string.Empty;
    }

    public class StudentExamSubmitViewModel
    {
        public int ExamId { get; set; }

        public List<StudentExamAnswerSubmissionViewModel> Answers { get; set; }
            = new();
    }

    public class StudentExamAnswerSubmissionViewModel
    {
        public int QuestionId { get; set; }

        public int? AnswerId { get; set; }
    }
}