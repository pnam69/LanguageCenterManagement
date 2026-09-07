using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Tuition
    {
        public int TuitionId { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public int ClassId { get; set; }
        public LanguageClass? Class { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }

        public DateTime DueDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Unpaid";

        [StringLength(500)]
        public string? Note { get; set; }
    }
}