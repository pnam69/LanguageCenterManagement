
namespace LanguageCenterManagement.ViewModels
{
    public class StudentPracticeQuizViewModel
    {
        public Guid QuizId { get; set; }

        public string Skill { get; set; } = string.Empty;

        public string Difficulty { get; set; } = string.Empty;

        public string? Passage { get; set; }

        public List<StudentPracticeQuestionViewModel> Questions { get; set; } = new();
    }

    public class StudentPracticeQuestionViewModel
    {
        public string QuestionText { get; set; } = string.Empty;

        public List<string> Options { get; set; } = new();
    }
}
