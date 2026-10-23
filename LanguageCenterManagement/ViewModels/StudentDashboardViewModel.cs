using System;

namespace LanguageCenterManagement.ViewModels
{
    public class StudentDashboardViewModel
    {
        public int UpcomingExamCount { get; set; }

        public int LearningResultCount { get; set; }

        public decimal? AverageLearningScore { get; set; }

        public List<UpcomingExamDashboardItem> UpcomingExams { get; set; }
            = new();

        public List<StudentLearningResultViewModel> RecentLearningResults
        {
            get;
            set;
        } = new();
    }

    public class UpcomingExamDashboardItem
    {
        public int ExamId { get; set; }

        public string ExamName { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string ClassCode { get; set; } = string.Empty;

        public string ExamType { get; set; } = string.Empty;

        public DateTime ExamDate { get; set; }

        public int Duration { get; set; }

        public decimal MaxScore { get; set; }
    }
}
