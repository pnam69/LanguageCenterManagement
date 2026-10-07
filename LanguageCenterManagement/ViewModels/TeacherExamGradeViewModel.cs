namespace LanguageCenterManagement.ViewModels
{
    public class TeacherExamGradeViewModel
    {
        public int ExamResultId { get; set; }

        public int ExamId { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public int StudentId { get; set; }

        public string StudentCode { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public decimal MaxScore { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? SubmittedAt { get; set; }

        public string? Note { get; set; }

        public List<TeacherExamGradeQuestionViewModel> Questions { get; set; }
            = new();
    }

    public class TeacherExamGradeQuestionViewModel
    {
        public int ExamQuestionId { get; set; }

        public int QuestionId { get; set; }

        public int QuestionOrder { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string QuestionType { get; set; } = string.Empty;

        public string Skill { get; set; } = string.Empty;

        public int MaxScore { get; set; }

        public string? StudentAnswer { get; set; }

        public string? CorrectAnswer { get; set; }

        public bool? IsCorrect { get; set; }

        public decimal Score { get; set; }

        public bool RequiresManualGrading { get; set; }

        public string? SpeakingPrompt { get; set; }

        public int? SpeakingPreparationTime { get; set; }

        public int? SpeakingResponseTime { get; set; }

        public string? WritingPrompt { get; set; }
        public string? AudioAnswerUrl { get; set; }
    }
}