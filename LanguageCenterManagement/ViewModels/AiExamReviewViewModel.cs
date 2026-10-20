namespace LanguageCenterManagement.ViewModels
{
    public class AiExamReviewViewModel
    {
        public string OverallReview { get; set; } = string.Empty;

        public List<string> Strengths { get; set; } = new();

        public List<string> AreasToImprove { get; set; } = new();

        public List<string> Recommendations { get; set; } = new();
    }

    public class AiExamReviewQuestionInput
    {
        public string Skill { get; set; } = string.Empty;

        public string QuestionType { get; set; } = string.Empty;

        public string QuestionText { get; set; } = string.Empty;

        public string StudentAnswer { get; set; } = string.Empty;

        public string CorrectAnswer { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public decimal EarnedScore { get; set; }

        public decimal PossibleScore { get; set; }
    }
}
