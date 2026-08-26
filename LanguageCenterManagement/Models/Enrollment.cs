using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public int ClassId { get; set; }
        public LanguageClass? Class { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Status { get; set; } = "Pending";
    }
}