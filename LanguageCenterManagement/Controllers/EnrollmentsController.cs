using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize]
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Enrollments
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.Student != null &&
                    (
                        e.Student.StudentCode.Contains(search) ||
                        e.Student.FullName.Contains(search) ||
                        e.Student.Phone.Contains(search) ||
                        e.Student.Email.Contains(search)
                    )
                    ||
                    e.Class != null &&
                    (
                        e.Class.ClassCode.Contains(search) ||
                        e.Class.ClassName.Contains(search)
                    )
                );
            }

            var enrollments = await query
                .OrderByDescending(e => e.RegistrationDate)
                .ToListAsync();

            ViewBag.Search = search;

            return View(enrollments);
        }

        // GET: Enrollments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        // GET: Enrollments/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? classId)
        {
            await LoadDropdowns(
                null, classId);
            return View();
        }

        // POST: Enrollments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Enrollment enrollment)
        {
            // Check that the student exists.
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == enrollment.StudentId);

            if (student == null)
            {
                ModelState.AddModelError(
                    "StudentId",
                    "Selected student does not exist.");
            }

            // Check that the class exists.
            var languageClass = await _context.Classes
                .FirstOrDefaultAsync(c => c.LanguageClassId == enrollment.ClassId);

            if (languageClass == null)
            {
                ModelState.AddModelError(
                    "ClassId",
                    "Selected class does not exist.");
            }

            // Don't allow duplicate enrollment.
            bool alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.StudentId == enrollment.StudentId &&
                    e.ClassId == enrollment.ClassId &&
                    e.Status != "Cancelled");

            if (alreadyEnrolled)
            {
                ModelState.AddModelError(
                    "",
                    "This student is already enrolled in this class.");
            }

            // Check class capacity.
            if (languageClass != null)
            {
                int currentStudents = await _context.Enrollments
                    .CountAsync(e =>
                        e.ClassId == enrollment.ClassId &&
                        e.Status != "Cancelled");

                if (currentStudents >= languageClass.MaxStudents)
                {
                    ModelState.AddModelError(
                        "",
                        "This class is already full.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(
                    enrollment.StudentId,
                    enrollment.ClassId);

                return View(enrollment);
            }

            enrollment.RegistrationDate = DateTime.Now;

            _context.Enrollments.Add(enrollment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Student enrolled successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Enrollments/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .FindAsync(id);

            if (enrollment == null)
            {
                return NotFound();
            }

            await LoadDropdowns(
                enrollment.StudentId,
                enrollment.ClassId);

            return View(enrollment);
        }

        // POST: Enrollments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
            int id,
            Enrollment enrollment)
        {
            if (id != enrollment.EnrollmentId)
            {
                return NotFound();
            }

            bool duplicate = await _context.Enrollments
                .AnyAsync(e =>
                    e.EnrollmentId != enrollment.EnrollmentId &&
                    e.StudentId == enrollment.StudentId &&
                    e.ClassId == enrollment.ClassId &&
                    e.Status != "Cancelled");

            if (duplicate)
            {
                ModelState.AddModelError(
                    "",
                    "This student is already enrolled in this class.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(
                    enrollment.StudentId,
                    enrollment.ClassId);

                return View(enrollment);
            }

            var existingEnrollment = await _context.Enrollments
                .FindAsync(id);

            if (existingEnrollment == null)
            {
                return NotFound();
            }

            existingEnrollment.StudentId = enrollment.StudentId;
            existingEnrollment.ClassId = enrollment.ClassId;
            existingEnrollment.Status = enrollment.Status;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Enrollment updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Enrollments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .FirstOrDefaultAsync(
                    e => e.EnrollmentId == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        // POST: Enrollments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var enrollment = await _context.Enrollments
                .FindAsync(id);

            if (enrollment == null)
            {
                return NotFound();
            }

            _context.Enrollments.Remove(enrollment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Enrollment deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns(
            int? selectedStudentId = null,
            int? selectedClassId = null)
        {
            var students = await _context.Students
                .OrderBy(s => s.FullName)
                .ToListAsync();

            var classes = await _context.Classes
                .Include(c => c.Course)
                .Where(c => c.Status == "Active")
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            ViewBag.StudentId = new SelectList(
                students,
                "StudentId",
                "FullName",
                selectedStudentId);

            ViewBag.ClassId = classes
                .Select(c => new SelectListItem
                {
                    Value = c.LanguageClassId.ToString(),
                    Text = c.ClassCode + " - " + c.ClassName,
                    Selected = c.LanguageClassId == selectedClassId
                })
                .ToList();
        }
    }
}