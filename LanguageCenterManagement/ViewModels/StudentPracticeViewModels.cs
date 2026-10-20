using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class StudentPracticeViewModel
    {
        public string Skill { get; set; } = "Reading";

        public string? Topic { get; set; }

        public int? ReadingContentId { get; set; }

        public string Difficulty { get; set; } = "Medium";

        [Range(1, 10)]
        public int Count { get; set; } = 5;
        public IFormFile? MaterialFile { get; set; }
        public int? SourceExamResultId { get; set; }

        public List<ReadingPassageOptionViewModel> Passages { get; set; } = new();
    }

    public class ReadingPassageOptionViewModel
    {
        public int ReadingContentId { get; set; }

        public string DisplayName { get; set; } = string.Empty;
    }

    public class StudentPracticeSubmitViewModel
    {
        public Guid QuizId { get; set; }

        public List<int?> Answers { get; set; } = new();
    }

    public class StudentPracticeResultViewModel
    {
        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        public string? Passage { get; set; }

        public List<StudentPracticeResultQuestionViewModel> Questions { get; set; } = new();
    }

    public class StudentPracticeResultQuestionViewModel
    {
        public string QuestionText { get; set; } = string.Empty;

        public List<string> Options { get; set; } = new();

        public int? SelectedAnswerIndex { get; set; }

        public int CorrectAnswerIndex { get; set; }

        public string? Explanation { get; set; }

        public bool IsCorrect { get; set; }

    }


}
