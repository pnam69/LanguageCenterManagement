namespace LanguageCenterManagement.Models
{
    public class ExamQuestion
    {
        public int ExamQuestionId { get; set; }

        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        public int QuestionId { get; set; }
        public Question? Question { get; set; }

        public int QuestionOrder { get; set; }
    }
}