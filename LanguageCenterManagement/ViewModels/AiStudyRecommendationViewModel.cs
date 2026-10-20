namespace LanguageCenterManagement.ViewModels
{
    public class AiStudyRecommendationViewModel
    {
        public string MainFocus { get; set; } = string.Empty;

        public List<string> Recommendations { get; set; } = new();

        public string StudyPlan { get; set; } = string.Empty;
    }
}
