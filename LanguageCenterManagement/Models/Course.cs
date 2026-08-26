using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Course
    {
        public int CourseId { get; set; }

        [Required]
        [StringLength(20)]
        public string CourseCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CourseName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TuitionFee { get; set; }

        public int Duration { get; set; }

        [StringLength(50)]
        public string? Level { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public ICollection<LanguageClass> Classes { get; set; }
            = new List<LanguageClass>();
    }
}