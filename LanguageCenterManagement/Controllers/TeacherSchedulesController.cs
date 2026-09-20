using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherSchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherSchedulesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? weekStart)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var start = weekStart?.Date ?? GetMonday(DateTime.Today);

            start = GetMonday(start);

            var end = start.AddDays(7);

            var schedules = await _context.Schedules
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Course)
                .Include(s => s.Room)
                .Where(s =>
                    s.Class != null &&
                    s.Class.TeacherId == user.TeacherId.Value &&
                    s.StudyDate >= start &&
                    s.StudyDate < end)
                .OrderBy(s => s.StudyDate)
                .ThenBy(s => s.StartTime)
                .Select(s => new TeacherScheduleItemViewModel
                {
                    ScheduleId = s.ScheduleId,

                    ClassId = s.ClassId,

                    ClassCode = s.Class!.ClassCode,

                    ClassName = s.Class.ClassName,

                    CourseName = s.Class.Course != null
                        ? s.Class.Course.CourseName
                        : "",

                    RoomName = s.Room != null
                        ? s.Room.RoomName
                        : "No room",

                    StudyDate = s.StudyDate,

                    StartTime = s.StartTime,

                    EndTime = s.EndTime,

                    Status = s.Status
                })
                .ToListAsync();

            var model = new TeacherScheduleViewModel
            {
                WeekStart = start,
                Schedules = schedules
            };

            return View(model);
        }

        private static DateTime GetMonday(DateTime date)
        {
            int difference =
                (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;

            return date.AddDays(-difference).Date;
        }
    }
}