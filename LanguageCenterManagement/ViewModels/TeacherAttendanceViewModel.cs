using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class TeacherAttendanceScheduleViewModel
    {
        public int ScheduleId { get; set; }
        public string ClassCode { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string RoomName { get; set; } = "";
        public DateTime StudyDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; } = "";
        public int StudentCount { get; set; }
        public int RecordedCount { get; set; }
    }

    public class TeacherAttendanceViewModel
    {
        public int ScheduleId { get; set; }

        public string ClassName { get; set; } = "";
        public string ClassCode { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string RoomName { get; set; } = "";

        [Required]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public List<TeacherAttendanceStudentViewModel> Students { get; set; }
            = new();
    }

    public class TeacherAttendanceStudentViewModel
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = "";
        public string FullName { get; set; } = "";

        [Required]
        public string Status { get; set; } = "Present";

        public string? Note { get; set; }
    }
}
