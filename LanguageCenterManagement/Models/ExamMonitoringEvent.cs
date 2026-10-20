using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class ExamMonitoringEvent
    {
        public int ExamMonitoringEventId { get; set; }

        public int ExamId { get; set; }
        public Exam? Exam { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        // Links the event to a specific exam attempt/result.
        public int? ExamResultId { get; set; }
        public ExamResult? ExamResult { get; set; }

        [Required]
        [StringLength(40)]
        public string EventType { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        // Optional context for the incident.
        [StringLength(500)]
        public string? Details { get; set; }

        // Pending, Reviewed, or Resolved.
        [StringLength(20)]
        public string ReviewStatus { get; set; } = "Pending";

        public string? ReviewedByUserId { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(1000)]
        public string? ReviewNotes { get; set; }
        [Range(0, 86400)]
        public int? DurationSeconds { get; set; }
    }
}