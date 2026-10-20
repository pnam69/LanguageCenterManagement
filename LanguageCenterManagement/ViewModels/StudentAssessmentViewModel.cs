
namespace LanguageCenterManagement.ViewModels
{
    public class StudentAssessmentViewModel
    {
        public int CompletedExams { get; set; }

        public int GradedAnswers { get; set; }

        public decimal OverallPercentage { get; set; }

        public List<StudentSkillAssessmentViewModel> Skills { get; set; }
            = new();

        public List<StudentRecentExamViewModel> RecentExams { get; set; }
            = new();

        // Progress comparison
        public decimal RecentAveragePercentage { get; set; }

        public decimal PreviousAveragePercentage { get; set; }

        public decimal ProgressDifference { get; set; }

        public bool HasProgressComparison { get; set; }

        // Repeated mistakes across completed exams
        public List<StudentRecurringErrorViewModel> RecurringErrors { get; set; }
            = new();
    }

    public class StudentSkillAssessmentViewModel
    {
        public string Skill { get; set; } = string.Empty;

        public decimal EarnedScore { get; set; }

        public decimal PossibleScore { get; set; }

        public int GradedQuestions { get; set; }

        public decimal Percentage =>
            PossibleScore > 0
                ? Math.Round(EarnedScore / PossibleScore * 100, 1)
                : 0;
    }

    public class StudentRecentExamViewModel
    {
        public int ExamResultId { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public decimal MaxScore { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public decimal Percentage =>
            MaxScore > 0
                ? Math.Round(Score / MaxScore * 100, 1)
                : 0;
    }

    public class StudentRecurringErrorViewModel
    {
        public int QuestionId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string Skill { get; set; } = string.Empty;

        public int IncorrectAttempts { get; set; }

        public int TotalGradedAttempts { get; set; }
    }
}
