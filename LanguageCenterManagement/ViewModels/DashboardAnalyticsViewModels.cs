namespace LanguageCenterManagement.ViewModels
{
    public class ClassPerformanceAnalyticsViewModel
    {
        public string ClassName { get; set; } = "";
        public int ExamCount { get; set; }
        public int StudentCount { get; set; }
        public double AverageScorePercent { get; set; }
        public double PassRatePercent { get; set; }
    }

    public class AiUsageDailyAnalyticsViewModel
    {
        public string DateLabel { get; set; } = "";
        public int Requests { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
    }

    public class StudentPerformanceAnalyticsViewModel
    {
        public string StudentName { get; set; } = "";
        public string StudentCode { get; set; } = "";
        public int ExamCount { get; set; }
        public double AverageScorePercent { get; set; }
    }

    public class ExamPerformanceAnalyticsViewModel
    {
        public string ExamName { get; set; } = "";
        public int ResultCount { get; set; }
        public double AverageScorePercent { get; set; }
    }
}
