using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LanguageClassesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LanguageClassesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LanguageClasses
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.ClassCode.Contains(search) ||
                    c.ClassName.Contains(search) ||
                    (c.Course != null &&
                     c.Course.CourseName.Contains(search)) ||
                    (c.Teacher != null &&
                     c.Teacher.FullName.Contains(search)));
            }

            var classes = await query
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            ViewBag.Search = search;

            return View(classes);
        }

        // GET: LanguageClasses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var languageClass = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(
                    c => c.LanguageClassId == id);

            if (languageClass == null)
            {
                return NotFound();
            }

            return View(languageClass);
        }

        // GET: LanguageClasses/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        // POST: LanguageClasses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LanguageClass languageClass)
        {
            if (await _context.Classes
                .AnyAsync(c => c.ClassCode == languageClass.ClassCode))
            {
                ModelState.AddModelError(
                    "ClassCode",
                    "Class code already exists.");
            }

            if (!await _context.Courses
                .AnyAsync(c => c.CourseId == languageClass.CourseId))
            {
                ModelState.AddModelError(
                    "CourseId",
                    "Selected course does not exist.");
            }

            if (!await _context.Teachers
                .AnyAsync(t => t.TeacherId == languageClass.TeacherId))
            {
                ModelState.AddModelError(
                    "TeacherId",
                    "Selected teacher does not exist.");
            }

            if (languageClass.MaxStudents <= 0)
            {
                ModelState.AddModelError(
                    "MaxStudents",
                    "Maximum students must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(
                    languageClass.CourseId,
                    languageClass.TeacherId);

                return View(languageClass);
            }

            _context.Classes.Add(languageClass);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Class added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: LanguageClasses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var languageClass = await _context.Classes
                .FindAsync(id);

            if (languageClass == null)
            {
                return NotFound();
            }

            await LoadDropdowns(
                languageClass.CourseId,
                languageClass.TeacherId);

            return View(languageClass);
        }

        // POST: LanguageClasses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            LanguageClass languageClass)
        {
            if (id != languageClass.LanguageClassId)
            {
                return NotFound();
            }

            if (await _context.Classes.AnyAsync(c =>
                c.ClassCode == languageClass.ClassCode &&
                c.LanguageClassId != languageClass.LanguageClassId))
            {
                ModelState.AddModelError(
                    "ClassCode",
                    "Class code already exists.");
            }

            if (languageClass.MaxStudents <= 0)
            {
                ModelState.AddModelError(
                    "MaxStudents",
                    "Maximum students must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(
                    languageClass.CourseId,
                    languageClass.TeacherId);

                return View(languageClass);
            }

            try
            {
                _context.Update(languageClass);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClassExists(languageClass.LanguageClassId))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["Success"] = "Class updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: LanguageClasses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var languageClass = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(
                    c => c.LanguageClassId == id);

            if (languageClass == null)
            {
                return NotFound();
            }

            return View(languageClass);
        }

        // POST: LanguageClasses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var languageClass = await _context.Classes
                .FindAsync(id);

            if (languageClass == null)
            {
                return NotFound();
            }

            _context.Classes.Remove(languageClass);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Class deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns(
            int? selectedCourseId = null,
            int? selectedTeacherId = null)
        {
            var courses = await _context.Courses
                .Where(c => c.Status == "Active")
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            var teachers = await _context.Teachers
                .Where(t => t.Status == "Active")
                .OrderBy(t => t.FullName)
                .ToListAsync();

            ViewBag.CourseId = new SelectList(
                courses,
                "CourseId",
                "CourseName",
                selectedCourseId);

            ViewBag.TeacherId = new SelectList(
                teachers,
                "TeacherId",
                "FullName",
                selectedTeacherId);
        }
        // GET: LanguageClasses/Students/5
        public async Task<IActionResult> Students(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var languageClass = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.LanguageClassId == id);

            if (languageClass == null)
            {
                return NotFound();
            }

            return View(languageClass);
        }
        private bool ClassExists(int id)
        {
            return _context.Classes
                .Any(c => c.LanguageClassId == id);
        }
    }
}