namespace LanguageCenterManagement.ViewModels
{
    public class StudentExamSubmissionViewModel
    {
        public int ExamId { get; set; }

        public List<StudentExamAnswerSubmissionViewModel> Answers { get; set; }
            = new();
    }
}