namespace LanguageCenterManagement.ViewModels
{
    public class TeacherScheduleViewModel
    {
        public DateTime WeekStart { get; set; }

        public List<TeacherScheduleItemViewModel> Schedules { get; set; }
            = new List<TeacherScheduleItemViewModel>();
    }

    public class TeacherScheduleItemViewModel
    {
        public int ScheduleId { get; set; }

        public int ClassId { get; set; }

        public string ClassCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string CourseName { get; set; } = string.Empty;

        public string RoomName { get; set; } = string.Empty;

        public DateTime StudyDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}