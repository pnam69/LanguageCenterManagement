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

        [StringLength(20)]
        public string Skill { get; set; } = "Reading";

        public int Score { get; set; } = 1;

        public ICollection<Answer> Answers { get; set; }
            = new List<Answer>();

        public ICollection<ExamQuestion> ExamQuestions { get; set; }
            = new List<ExamQuestion>();

        public ReadingContent? ReadingContent { get; set; }

        public ListeningContent? ListeningContent { get; set; }

        public SpeakingContent? SpeakingContent { get; set; }

        public WritingContent? WritingContent { get; set; }
    }
}