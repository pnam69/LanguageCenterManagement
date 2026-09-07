using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public int TuitionId { get; set; }
        public Tuition? Tuition { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [StringLength(30)]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(100)]
        public string? TransactionCode { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}