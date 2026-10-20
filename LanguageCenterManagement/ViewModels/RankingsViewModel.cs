namespace LanguageCenterManagement.ViewModels
{
    public class RankingsViewModel
    {
        public int? SelectedClassId { get; set; }

        public List<RankingClassOptionViewModel> AvailableClasses { get; set; }
            = new();

        public List<StudentRankingItemViewModel> Students { get; set; }
            = new();

        public List<ClassRankingItemViewModel> Classes { get; set; }
            = new();

        public List<TeacherRankingItemViewModel> Teachers { get; set; }
            = new();
    }

    public class RankingClassOptionViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }

    public class StudentRankingItemViewModel
    {
        public int Rank { get; set; }
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public decimal AveragePercentage { get; set; }
        public int CompletedExams { get; set; }
    }

    public class ClassRankingItemViewModel
    {
        public int Rank { get; set; }
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public decimal AveragePercentage { get; set; }
        public int StudentCount { get; set; }
        public int CompletedResults { get; set; }
    }

    public class TeacherRankingItemViewModel
    {
        public int Rank { get; set; }
        public int TeacherId { get; set; }
        public string TeacherCode { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public decimal AveragePercentage { get; set; }
        public int ClassCount { get; set; }
        public int StudentCount { get; set; }
        public int CompletedResults { get; set; }
    }
}
