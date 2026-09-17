using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class ScheduleChangeRequest
    {
        public int ScheduleChangeRequestId { get; set; }

        // Original schedule
        public int ScheduleId { get; set; }
        public Schedule? Schedule { get; set; }

        // Teacher who submitted the request
        public int TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        // Proposed make-up schedule
        [DataType(DataType.Date)]
        public DateTime? ProposedDate { get; set; }

        public TimeSpan? ProposedStartTime { get; set; }

        public TimeSpan? ProposedEndTime { get; set; }

        // Schedule created after approval
        public int? MakeUpScheduleId { get; set; }
        public Schedule? MakeUpSchedule { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? AdminNote { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public DateTime? DecisionDate { get; set; }

        public int? ProposedRoomId { get; set; }

        public Room? ProposedRoom { get; set; }
    }
}