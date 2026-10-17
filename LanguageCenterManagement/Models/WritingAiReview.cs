
using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class WritingAiReview
    {
        public int WritingAiReviewId { get; set; }

        public int ExamAnswerId { get; set; }
        public ExamAnswer? ExamAnswer { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(2000)]
        public string? Indicators { get; set; }

        [StringLength(2000)]
        public string? Explanation { get; set; }

        public DateTime CheckedAt { get; set; } = DateTime.Now;
    }
}
