using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Teacher
    {
        public int TeacherId { get; set; }

        [Required]
        [StringLength(20)]
        public string TeacherCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? Specialization { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public ICollection<LanguageClass> Classes { get; set; }
            = new List<LanguageClass>();
    }
}