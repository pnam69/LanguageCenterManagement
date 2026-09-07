using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Question
    {
        public int QuestionId { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [StringLength(20)]
        public string QuestionType { get; set; } = "MultipleChoice";

        public int Score { get; set; } = 1;

        public ICollection<Answer> Answers { get; set; }
            = new List<Answer>();

        public ICollection<ExamQuestion> ExamQuestions { get; set; }
            = new List<ExamQuestion>();
    }
}