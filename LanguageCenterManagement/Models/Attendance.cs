using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Attendance
    {
        public int AttendanceId { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public int ClassId { get; set; }
        public LanguageClass? Class { get; set; }

        public DateTime AttendanceDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Present";

        [StringLength(500)]
        public string? Note { get; set; }
    }
}