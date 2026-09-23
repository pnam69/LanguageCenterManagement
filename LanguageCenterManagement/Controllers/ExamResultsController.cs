using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ExamResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamResultsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .Include(r => r.Student)
                .AsQueryable();

            if (User.IsInRole("Teacher"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null || !user.TeacherId.HasValue)
                {
                    return Forbid();
                }

                query = query.Where(r =>
                    r.Exam != null &&
                    r.Exam.Class != null &&
                    r.Exam.Class.TeacherId == user.TeacherId.Value);
            }

            var results = await query
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            return View(results);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.ExamResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            return View(result);
        }

        public async Task<IActionResult> Create(int? examId)
        {
            if (examId.HasValue)
            {
                var exam = await _context.Exams
                    .Include(e => e.Class)
                    .FirstOrDefaultAsync(e => e.ExamId == examId);

                if (exam == null)
                {
                    return NotFound();
                }

                if (!await CanAccessExam(exam))
                {
                    return Forbid();
                }
            }

            var model = new ExamResultFormViewModel
            {
                ExamId = examId ?? 0,
                Status = "Completed",
                SubmittedAt = DateTime.Now
            };

            await LoadDropdowns(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ExamResultFormViewModel model)
        {
            var exam = await _context.Exams
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e => e.ExamId == model.ExamId);

            if (exam == null)
            {
                return NotFound();
            }

            if (!await CanAccessExam(exam))
            {
                return Forbid();
            }

            if (model.Score > exam.MaxScore)
            {
                ModelState.AddModelError(
                    nameof(model.Score),
                    $"Score cannot be greater than {exam.MaxScore}.");
            }

            bool enrolled = await _context.Enrollments.AnyAsync(e =>
                e.ClassId == exam.ClassId &&
                e.StudentId == model.StudentId &&
                e.Status != "Cancelled");

            if (!enrolled)
            {
                ModelState.AddModelError(
                    nameof(model.StudentId),
                    "This student is not enrolled in the exam's class.");
            }

            bool duplicate = await _context.ExamResults.AnyAsync(r =>
                r.ExamId == model.ExamId &&
                r.StudentId == model.StudentId);

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(model.StudentId),
                    "This student already has a result for this exam.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model);
                return View(model);
            }

            var result = new ExamResult
            {
                ExamId = model.ExamId,
                StudentId = model.StudentId,
                Score = model.Score,
                Status = model.Status,
                SubmittedAt = model.SubmittedAt ?? DateTime.Now,
                Note = model.Note
            };

            _context.ExamResults.Add(result);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Exam result created successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .FirstOrDefaultAsync(r => r.ExamResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            var model = new ExamResultFormViewModel
            {
                ExamResultId = result.ExamResultId,
                ExamId = result.ExamId,
                StudentId = result.StudentId,
                Score = result.Score,
                Status = result.Status,
                SubmittedAt = result.SubmittedAt,
                Note = result.Note
            };

            await LoadDropdowns(model, result.Exam?.ClassId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ExamResultFormViewModel model)
        {
            if (id != model.ExamResultId)
            {
                return NotFound();
            }

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .FirstOrDefaultAsync(r => r.ExamResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            var exam = await _context.Exams
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e => e.ExamId == model.ExamId);

            if (exam == null)
            {
                return NotFound();
            }

            if (model.Score > exam.MaxScore)
            {
                ModelState.AddModelError(
                    nameof(model.Score),
                    $"Score cannot be greater than {exam.MaxScore}.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model, exam.ClassId);
                return View(model);
            }

            result.ExamId = model.ExamId;
            result.StudentId = model.StudentId;
            result.Score = model.Score;
            result.Status = model.Status;
            result.SubmittedAt = model.SubmittedAt;
            result.Note = model.Note;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Exam result updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.ExamResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            return View(result);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .FirstOrDefaultAsync(r => r.ExamResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            _context.ExamResults.Remove(result);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Exam result deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns(
            ExamResultFormViewModel model,
            int? classId = null)
        {
            var examsQuery = _context.Exams
                .Include(e => e.Class)
                .AsQueryable();

            if (User.IsInRole("Teacher"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user?.TeacherId != null)
                {
                    examsQuery = examsQuery.Where(e =>
                        e.Class != null &&
                        e.Class.TeacherId == user.TeacherId.Value);
                }
            }

            var exams = await examsQuery
                .OrderByDescending(e => e.ExamDate)
                .ToListAsync();

            ViewBag.Exams = new SelectList(
                exams,
                "ExamId",
                "ExamName",
                model.ExamId);

            var studentsQuery = _context.Students
                .AsQueryable();

            if (classId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s =>
                    _context.Enrollments.Any(e =>
                        e.StudentId == s.StudentId &&
                        e.ClassId == classId.Value &&
                        e.Status != "Cancelled"));
            }

            var students = await studentsQuery
                .OrderBy(s => s.FullName)
                .ToListAsync();

            ViewBag.Students = new SelectList(
                students,
                "StudentId",
                "FullName",
                model.StudentId);
        }

        private async Task<bool> CanAccessExam(Exam exam)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            var user = await _userManager.GetUserAsync(User);

            return user?.TeacherId != null &&
                   exam.Class != null &&
                   exam.Class.TeacherId == user.TeacherId.Value;
        }

        private async Task<bool> CanAccessResult(ExamResult result)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            var user = await _userManager.GetUserAsync(User);

            return user?.TeacherId != null &&
                   result.Exam?.Class != null &&
                   result.Exam.Class.TeacherId == user.TeacherId.Value;
        }
    }
}