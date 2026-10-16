using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class AiQuestionGenerationViewModel
    {
        [Required]
        [StringLength(200)]
        public string Topic { get; set; } = string.Empty;

        [Required]
        public string Skill { get; set; } = "Reading";

        [Required]
        public string Difficulty { get; set; } = "Medium";

        [Range(1, 10)]
        public int Count { get; set; } = 5;
        [Required]
        public string QuestionType { get; set; } = "MultipleChoice";
        public int? ReadingContentId { get; set; }

        public List<AiGeneratedQuestionViewModel> Questions { get; set; } = new();
    }

    public class AiGeneratedQuestionViewModel
    {
        [Required]
        [StringLength(2000)]
        public string QuestionText { get; set; } = string.Empty;

        public List<AiGeneratedAnswerViewModel> Answers { get; set; } = new();
    }

    public class AiGeneratedAnswerViewModel
    {
        [Required]
        [StringLength(1000)]
        public string AnswerText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}