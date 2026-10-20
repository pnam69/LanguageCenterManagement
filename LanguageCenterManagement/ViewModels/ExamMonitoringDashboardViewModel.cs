using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LanguageCenterManagement.ViewModels
{
    public class ExamMonitoringDashboardViewModel
    {
        public int? SelectedExamId { get; set; }

        public IEnumerable<SelectListItem> Exams { get; set; }
            = new List<SelectListItem>();

        public int TotalIncidents { get; set; }
        public int StudentsFlagged { get; set; }
        public int PendingReviews { get; set; }
        public int ReviewedIncidents { get; set; }

        public List<StudentMonitoringSummary> Students { get; set; }
            = new();

        public List<ExamMonitoringEvent> RecentEvents { get; set; }
            = new();
    }

    public class StudentMonitoringSummary
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public int TotalIncidents { get; set; }
        public int TabHiddenCount { get; set; }
        public int FocusLostCount { get; set; }

        public DateTime LastIncident { get; set; }
        public string ReviewStatus { get; set; } = "Pending";

        public string RiskLevel =>
            TotalIncidents >= 6 ? "High" :
            TotalIncidents >= 3 ? "Medium" : "Low";
    }
}
