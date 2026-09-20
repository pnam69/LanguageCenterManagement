using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentClassesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentClassesController(
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

            var enrollments = await _context.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Course)
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Teacher)
                .Where(e =>
                    e.StudentId == user.StudentId.Value &&
                    e.Status != "Cancelled")
                .OrderBy(e => e.Class!.ClassName)
                .ToListAsync();

            return View(enrollments);
        }
    }
}