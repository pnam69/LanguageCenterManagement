using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class ScheduleChangeRequest
    {
        public int ScheduleChangeRequestId { get; set; }

        public int ScheduleId { get; set; }
        public Schedule? Schedule { get; set; }

        public int TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? ProposedDate { get; set; }

        public TimeSpan? ProposedStartTime { get; set; }

        public TimeSpan? ProposedEndTime { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? AdminNote { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public DateTime? DecisionDate { get; set; }

        // When an admin approves a make-up schedule, link to the created Schedule record
        // Nullable because the request may not yet have an associated make-up schedule.
        public int? MakeUpScheduleId { get; set; }
        public Schedule? MakeUpSchedule { get; set; }
    }
}