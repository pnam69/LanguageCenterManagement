using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentAttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentAttendanceController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
            {
                return Forbid();
            }


            var attendance = await _context.Attendances
                .AsNoTracking()
                .Include(a => a.Class)
                    .ThenInclude(c => c!.Course)
                .Include(a => a.Schedule)
                    .ThenInclude(s => s!.Room)
                .Where(a => a.StudentId == user.StudentId.Value)
                .OrderByDescending(a => a.AttendanceDate)
                .ThenByDescending(a => a.Schedule!.StartTime)
                .ToListAsync();


            return View(attendance);
        }
    }
}