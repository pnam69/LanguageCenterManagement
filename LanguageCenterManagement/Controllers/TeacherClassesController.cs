using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherClassesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherClassesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.TeacherId == null)
                return Forbid();

            int teacherId = user.TeacherId.Value;

            var query = _context.Classes
                .AsNoTracking()
                .Where(c => c.TeacherId == teacherId)
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .Include(c => c.Enrollments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(c =>
                    c.ClassCode.Contains(search) ||
                    c.ClassName.Contains(search) ||
                    (c.Course != null &&
                     c.Course.CourseName.Contains(search)));
            }

            ViewBag.Search = search;

            var classes = await query
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            return View(classes);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user?.TeacherId == null)
                return Forbid();

            int teacherId = user.TeacherId.Value;

            var languageClass = await _context.Classes
                .AsNoTracking()
                .Where(c =>
                    c.LanguageClassId == id.Value &&
                    c.TeacherId == teacherId)
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync();

            if (languageClass == null)
                return NotFound();

            return View(languageClass);
        }
    }
}
