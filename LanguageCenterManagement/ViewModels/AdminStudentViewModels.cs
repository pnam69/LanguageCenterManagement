namespace LanguageCenterManagement.Models.ViewModels
{
    public class AdminStudentListViewModel
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public List<AdminStudentRowViewModel> Students { get; set; } = new();
    }

    public class AdminStudentRowViewModel
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = string.Empty;
        public int EnrollmentCount { get; set; }
    }

    public class AdminStudentDetailsViewModel
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<AdminStudentEnrollmentRowViewModel> Enrollments { get; set; } = new();
        public List<AdminStudentAttendanceRowViewModel> AttendanceHistory { get; set; } = new();
    }

    public class AdminStudentEnrollmentRowViewModel
    {
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string EnrollmentStatus { get; set; } = string.Empty;
    }

    public class AdminStudentAttendanceRowViewModel
    {
        public DateTime AttendanceDate { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
