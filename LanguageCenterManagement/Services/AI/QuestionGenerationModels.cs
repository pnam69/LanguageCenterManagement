using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Services.Ai
{
    public class QuestionGenerationRequest
    {

        public string Topic { get; set; } = string.Empty;
        public string Skill { get; set; } = "Reading";
        public string Difficulty { get; set; } = "Medium";
        public int Count { get; set; } = 5;

        [Required]
        public string QuestionType { get; set; } = "MultipleChoice";
    }

    public class GeneratedQuestion
    {
        public string QuestionText { get; set; } = string.Empty;
        public List<GeneratedAnswer> Answers { get; set; } = new();
    }

    public class GeneratedAnswer
    {
        public string AnswerText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}