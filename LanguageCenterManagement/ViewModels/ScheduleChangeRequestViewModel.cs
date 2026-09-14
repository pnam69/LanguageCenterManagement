using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class ScheduleChangeRequestViewModel
    {
        [Required]
        public int ScheduleId { get; set; }

        public int TeacherId { get; set; }

        public string? ClassName { get; set; }

        public DateTime StudyDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string? RoomName { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Proposed Make-up Date")]
        public DateTime? ProposedDate { get; set; }

        [Display(Name = "Proposed Start Time")]
        public TimeSpan? ProposedStartTime { get; set; }

        [Display(Name = "Proposed End Time")]
        public TimeSpan? ProposedEndTime { get; set; }
    }
}