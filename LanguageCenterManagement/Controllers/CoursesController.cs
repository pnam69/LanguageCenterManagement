using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Courses
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Courses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.CourseCode.Contains(search) ||
                    c.CourseName.Contains(search) ||
                    (c.Level != null && c.Level.Contains(search)));
            }

            var courses = await query
                .OrderBy(c => c.CourseCode)
                .ToListAsync();

            ViewBag.Search = search;

            return View(courses);
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Classes)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            if (await _context.Courses
                .AnyAsync(c => c.CourseCode == course.CourseCode))
            {
                ModelState.AddModelError(
                    "CourseCode",
                    "Course code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(course);
            }

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }

            if (await _context.Courses.AnyAsync(c =>
                c.CourseCode == course.CourseCode &&
                c.CourseId != course.CourseId))
            {
                ModelState.AddModelError(
                    "CourseCode",
                    "Course code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(course);
            }

            try
            {
                _context.Update(course);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(course.CourseId))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["Success"] = "Course updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses
                .FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            bool hasClasses = await _context.Classes
                .AnyAsync(c => c.CourseId == id);

            if (hasClasses)
            {
                TempData["Error"] =
                    "Cannot delete this course because it is assigned to one or more classes.";

                return RedirectToAction(nameof(Index));
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses
                .Any(c => c.CourseId == id);
        }
    }
}