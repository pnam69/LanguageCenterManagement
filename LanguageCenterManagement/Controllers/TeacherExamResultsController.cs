using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherExamResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherExamResultsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: TeacherExamResults
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var exams = await _context.Exams
                .Include(e => e.Class)
                .Where(e =>
                    e.Class != null &&
                    e.Class.TeacherId == user.TeacherId.Value)
                .OrderByDescending(e => e.ExamDate)
                .ToListAsync();

            return View(exams);
        }

        // GET: TeacherExamResults/Enter/5
        [HttpGet]
        public async Task<IActionResult> Enter(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var exam = await _context.Exams
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e =>
                    e.ExamId == id &&
                    e.Class != null &&
                    e.Class.TeacherId == user.TeacherId.Value);

            if (exam == null)
            {
                return NotFound();
            }

            var students = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e =>
                    e.ClassId == exam.ClassId &&
                    e.Student != null &&
                    e.Status != "Rejected" &&
                    e.Status != "Cancelled")
                .OrderBy(e => e.Student!.FullName)
                .Select(e => new TeacherExamResultStudentViewModel
                {
                    StudentId = e.Student!.StudentId,
                    StudentCode = e.Student.StudentCode,
                    FullName = e.Student.FullName,
                    Score = 0,
                    Status = "Completed"
                })
                .ToListAsync();

            var existingResults = await _context.ExamResults
                .Where(r => r.ExamId == id)
                .ToListAsync();

            foreach (var student in students)
            {
                var existing = existingResults.FirstOrDefault(r =>
                    r.StudentId == student.StudentId);

                if (existing != null)
                {
                    student.Score = existing.Score;
                    student.Status = existing.Status;
                    student.SubmittedAt = existing.SubmittedAt;
                    student.Note = existing.Note;
                }
            }

            var model = new TeacherExamResultsViewModel
            {
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                ClassName = exam.Class?.ClassName ?? string.Empty,
                MaxScore = exam.MaxScore,
                ExamDate = exam.ExamDate,
                Students = students
            };

            return View(model);
        }

        // POST: TeacherExamResults/Enter
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enter(
            TeacherExamResultsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var exam = await _context.Exams
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e =>
                    e.ExamId == model.ExamId &&
                    e.Class != null &&
                    e.Class.TeacherId == user.TeacherId.Value);

            if (exam == null)
            {
                return NotFound();
            }

            ModelState.Clear();

            var validStudentIds = await _context.Enrollments
                .Where(e =>
                    e.ClassId == exam.ClassId &&
                    e.Status != "Rejected" &&
                    e.Status != "Cancelled")
                .Select(e => e.StudentId)
                .ToListAsync();

            foreach (var student in model.Students)
            {
                if (!validStudentIds.Contains(student.StudentId))
                {
                    return BadRequest();
                }

                if (student.Score < 0 ||
                    student.Score > exam.MaxScore)
                {
                    ModelState.AddModelError(
                        $"Students[{model.Students.IndexOf(student)}].Score",
                        $"Score must be between 0 and {exam.MaxScore}.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.ExamName = exam.ExamName;
                model.ClassName = exam.Class?.ClassName ?? string.Empty;
                model.MaxScore = exam.MaxScore;
                model.ExamDate = exam.ExamDate;

                return View(model);
            }

            var existingResults = await _context.ExamResults
                .Where(r => r.ExamId == exam.ExamId)
                .ToListAsync();

            foreach (var student in model.Students)
            {
                var result = existingResults.FirstOrDefault(r =>
                    r.StudentId == student.StudentId);

                if (result == null)
                {
                    result = new ExamResult
                    {
                        ExamId = exam.ExamId,
                        StudentId = student.StudentId
                    };

                    _context.ExamResults.Add(result);
                }

                result.Score = student.Score;
                result.Status = student.Status;
                result.SubmittedAt =
                    student.SubmittedAt ?? DateTime.Now;
                result.Note = student.Note;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Results for {exam.ExamName} were saved successfully.";

            return RedirectToAction(
                nameof(Enter),
                new { id = exam.ExamId });
        }
    }
}