using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class WritingAssessmentInputViewModel
    {
        [Required(ErrorMessage = "Enter the writing task or topic.")]
        [StringLength(500)]
        public string TaskPrompt { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the student's writing.")]
        [StringLength(10000, MinimumLength = 10,
            ErrorMessage = "Writing must be between 10 and 10,000 characters.")]
        public string StudentWriting { get; set; } = string.Empty;

        public WritingAssessmentResultViewModel? Assessment { get; set; }
    }

    public class WritingAssessmentResultViewModel
    {
        public int TotalScore { get; set; }
        public int MaxScore { get; set; } = 20;
        public string OverallFeedback { get; set; } = string.Empty;
        public List<WritingCriterionViewModel> Criteria { get; set; } = new();
        public List<string> Corrections { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public string ImprovedVersion { get; set; } = string.Empty;
    }

    public class WritingCriterionViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public int MaxScore { get; set; } = 5;
        public string Feedback { get; set; } = string.Empty;
    }
}