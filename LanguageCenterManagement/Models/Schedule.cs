using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Schedule
    {
        public int ScheduleId { get; set; }

        public int ClassId { get; set; }
        public LanguageClass? Class { get; set; }

        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [DataType(DataType.Date)]
        public DateTime StudyDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Scheduled";
    }
}