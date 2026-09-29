namespace LanguageCenterManagement.ViewModels
{
    public class StudentExamResultAnswerViewModel
    {
        public int QuestionId { get; set; }

        public int QuestionOrder { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string QuestionType { get; set; } = string.Empty;

        public string Skill { get; set; } = string.Empty;

        public int QuestionScore { get; set; }

        public string? StudentAnswer { get; set; }

        public string? CorrectAnswer { get; set; }

        public bool? IsCorrect { get; set; }

        public decimal Score { get; set; }

        public List<string> AnswerOptions { get; set; } = new();
    }
}