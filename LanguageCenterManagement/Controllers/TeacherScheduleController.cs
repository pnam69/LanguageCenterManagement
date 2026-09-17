using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherScheduleController(
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

            var selectedDate = weekStart ?? DateTime.Today;

            // Make sure the selected date is Monday.
            var monday = selectedDate.Date.AddDays(
                -(int)selectedDate.DayOfWeek +
                (selectedDate.DayOfWeek == DayOfWeek.Sunday ? -6 : 1)
            );

            var sunday = monday.AddDays(6);

            var schedules = await _context.Schedules
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Course)
                .Include(s => s.Room)
                .Where(s =>
                    s.Class != null &&
                    s.Class.TeacherId == user.TeacherId.Value &&
                    s.StudyDate >= monday &&
                    s.StudyDate <= sunday)
                .OrderBy(s => s.StudyDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.WeekStart = monday;
            ViewBag.WeekEnd = sunday;

            return View(schedules);
        }
    }
}