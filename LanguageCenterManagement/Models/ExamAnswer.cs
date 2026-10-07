namespace LanguageCenterManagement.Models
{
    public class ExamAnswer
    {
        public int ExamAnswerId { get; set; }

        public int ExamResultId { get; set; }
        public ExamResult? ExamResult { get; set; }

        public int QuestionId { get; set; }
        public Question? Question { get; set; }

        public int? AnswerId { get; set; }
        public Answer? Answer { get; set; }

        public string? TextAnswer { get; set; }

        public string? AudioAnswerUrl { get; set; }

        public bool? IsCorrect { get; set; }

        public decimal Score { get; set; }
    }
}