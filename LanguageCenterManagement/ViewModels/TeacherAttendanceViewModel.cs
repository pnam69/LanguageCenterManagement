using System.ComponentModel.DataAnnotations;
using LanguageCenterManagement.Models;

namespace LanguageCenterManagement.ViewModels
{
    public class TeacherAttendanceViewModel
    {
        [Required]
        public int ClassId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; } = DateTime.Today;

        public string? ClassName { get; set; }

        public List<TeacherAttendanceStudentViewModel> Students { get; set; }
            = new List<TeacherAttendanceStudentViewModel>();
    }

    public class TeacherAttendanceStudentViewModel
    {
        public int StudentId { get; set; }

        public string StudentCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Status { get; set; } = "Present";

        public string? Note { get; set; }
    }
}