using Microsoft.AspNetCore.Http;

namespace LanguageCenterManagement.ViewModels
{
    public class StudentExamSubmissionViewModel
    {
        public int ExamId { get; set; }

        public List<StudentExamAnswerSubmissionViewModel> Answers { get; set; }
            = new();
    }

    public class StudentExamAnswerSubmissionViewModel
    {
        public int QuestionId { get; set; }

        public int? AnswerId { get; set; }

        public string? TextAnswer { get; set; }

        public IFormFile? AudioFile { get; set; }
    }
}