using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models.ViewModels
{
    public class TeacherStudentListViewModel
    {
        public int? ClassId { get; set; }
        public string? Search { get; set; }

        public List<TeacherStudentClassOption> Classes { get; set; } = new();
        public List<TeacherStudentRowViewModel> Students { get; set; } = new();
    }

    public class TeacherStudentClassOption
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string ClassCode { get; set; } = string.Empty;
    }

    public class TeacherStudentRowViewModel
    {
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string EnrollmentStatus { get; set; } = string.Empty;
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
    }

    public class TeacherStudentDetailsViewModel
    {
        public int StudentId { get; set; }
        public int ClassId { get; set; }

        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string StudentStatus { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;
        public string ClassCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string EnrollmentStatus { get; set; } = string.Empty;

        public int TotalLessonsRecorded { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }

        public List<TeacherStudentAttendanceRowViewModel> AttendanceHistory { get; set; } = new();
    }

    public class TeacherStudentAttendanceRowViewModel
    {
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
