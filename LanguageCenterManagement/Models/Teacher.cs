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

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? Specialization { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<LanguageClass> Classes { get; set; }
            = new List<LanguageClass>();
    }
}