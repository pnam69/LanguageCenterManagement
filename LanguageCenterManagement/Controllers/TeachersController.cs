using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TeachersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeachersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Teachers
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Teachers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    t.TeacherCode.Contains(search) ||
                    t.FullName.Contains(search) ||
                    (t.Phone != null && t.Phone.Contains(search)) ||
                    (t.Email != null && t.Email.Contains(search)) ||
                    (t.Specialization != null &&
                     t.Specialization.Contains(search)));
            }

            var teachers = await query
                .OrderBy(t => t.TeacherCode)
                .ToListAsync();

            ViewBag.Search = search;

            return View(teachers);
        }

        // GET: Teachers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(t => t.Classes)
                    .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(t => t.TeacherId == id);

            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // GET: Teachers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Teachers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Teacher teacher)
        {
            if (await _context.Teachers
                .AnyAsync(t => t.TeacherCode == teacher.TeacherCode))
            {
                ModelState.AddModelError(
                    "TeacherCode",
                    "Teacher code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(teacher);
            }

            teacher.CreatedAt = DateTime.Now;

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Teacher added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Teachers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .FindAsync(id);

            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // POST: Teachers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Teacher teacher)
        {
            if (id != teacher.TeacherId)
            {
                return NotFound();
            }

            if (await _context.Teachers.AnyAsync(t =>
                t.TeacherCode == teacher.TeacherCode &&
                t.TeacherId != teacher.TeacherId))
            {
                ModelState.AddModelError(
                    "TeacherCode",
                    "Teacher code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(teacher);
            }

            try
            {
                _context.Update(teacher);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeacherExists(teacher.TeacherId))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["Success"] = "Teacher updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Teachers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.TeacherId == id);

            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // POST: Teachers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers
                .FindAsync(id);

            if (teacher == null)
            {
                return NotFound();
            }

            // Don't allow deletion if teacher has assigned classes.
            bool hasClasses = await _context.Classes
                .AnyAsync(c => c.TeacherId == id);

            if (hasClasses)
            {
                TempData["Error"] =
                    "Cannot delete this teacher because they are assigned to one or more classes.";

                return RedirectToAction(nameof(Index));
            }

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Teacher deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool TeacherExists(int id)
        {
            return _context.Teachers
                .Any(t => t.TeacherId == id);
        }
    }
}