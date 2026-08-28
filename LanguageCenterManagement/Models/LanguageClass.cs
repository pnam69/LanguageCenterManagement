using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class LanguageClass
    {
        [Key]
        public int LanguageClassId { get; set; }

        [Required]
        [StringLength(20)]
        public string ClassCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ClassName { get; set; } = string.Empty;

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public int TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [Range(1, 500)]
        public int MaxStudents { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public ICollection<Schedule> Schedules { get; set; }
            = new List<Schedule>();

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();
    }
}