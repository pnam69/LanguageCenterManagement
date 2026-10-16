using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class WritingIntegrityReview
    {
        public int WritingIntegrityReviewId { get; set; }

        public int ExamAnswerId { get; set; }
        public ExamAnswer? ExamAnswer { get; set; }

        public int? MatchedExamAnswerId { get; set; }

        [Range(0, 100)]
        public decimal SimilarityPercent { get; set; }

        public string? MatchingText { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(1000)]
        public string? ReviewNote { get; set; }

        public DateTime CheckedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }
    }
}