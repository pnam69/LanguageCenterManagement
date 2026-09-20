using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentMaterialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentMaterialsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? classId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
            {
                return Forbid();
            }

            var studentId = user.StudentId.Value;

            var enrolledClassIds = _context.Enrollments
                .Where(e =>
                    e.StudentId == studentId &&
                    e.Status != "Cancelled")
                .Select(e => e.ClassId);

            var query = _context.Materials
                .Include(m => m.Class)
                    .ThenInclude(c => c!.Course)
                .Where(m => enrolledClassIds.Contains(m.ClassId));

            if (classId.HasValue)
            {
                query = query.Where(m => m.ClassId == classId.Value);
            }

            var materials = await query
                .OrderBy(m => m.Class!.ClassCode)
                .ThenByDescending(m => m.CreatedAt)
                .ToListAsync();

            var classes = await _context.Classes
                .Where(c => enrolledClassIds.Contains(c.LanguageClassId))
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            ViewBag.Classes = classes;
            ViewBag.SelectedClassId = classId;

            return View(materials);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
            {
                return Forbid();
            }

            var studentId = user.StudentId.Value;

            var material = await _context.Materials
                .Include(m => m.Class)
                    .ThenInclude(c => c!.Course)
                .FirstOrDefaultAsync(m => m.MaterialId == id);

            if (material == null)
            {
                return NotFound();
            }

            var enrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.StudentId == studentId &&
                    e.ClassId == material.ClassId &&
                    e.Status != "Cancelled");

            if (!enrolled)
            {
                return Forbid();
            }

            return View(material);
        }
    }
}