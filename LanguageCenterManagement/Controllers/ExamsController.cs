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
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Exams
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Exams
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Course)
                .Include(e => e.ExamQuestions)
                .AsQueryable();

            if (User.IsInRole("Teacher"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null || !user.TeacherId.HasValue)
                {
                    return Forbid();
                }

                query = query.Where(e =>
                    e.Class != null &&
                    e.Class.TeacherId == user.TeacherId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(e =>
                    e.ExamName.Contains(search) ||
                    e.ExamType.Contains(search) ||
                    (e.Class != null && e.Class.ClassName.Contains(search)));
            }

            var exams = await query
                .OrderByDescending(e => e.ExamDate)
                .ToListAsync();

            ViewBag.Search = search;

            return View(exams);
        }

        // GET: Exams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Course)
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Teacher)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q!.Answers)
                .Include(e => e.ExamResults)
                    .ThenInclude(r => r.Student)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
            }

            if (!await CanAccessExam(exam))
            {
                return Forbid();
            }

            return View(exam);
        }

        // GET: Exams/Create
        public async Task<IActionResult> Create()
        {
            if (!await CanCreateExam())
            {
                return Forbid();
            }

            var model = new ExamFormViewModel
            {
                ExamDate = DateTime.Now,
                Duration = 60,
                MaxScore = 10,
                Status = "Draft",
                ExamType = "Test"
            };

            await LoadFormData(model);

            return View(model);
        }

        // POST: Exams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamFormViewModel model)
        {
            if (!await CanCreateClass(model.ClassId))
            {
                ModelState.AddModelError("ClassId", "You cannot create an exam for this class.");
            }

            if (!ModelState.IsValid)
            {
                await LoadFormData(model);
                return View(model);
            }

            var exam = new Exam
            {
                ClassId = model.ClassId,
                ExamName = model.ExamName,
                ExamType = model.ExamType,
                ExamDate = model.ExamDate,
                Duration = model.Duration,
                MaxScore = model.MaxScore,
                Status = model.Status,
                Description = model.Description
            };

            _context.Exams.Add(exam);

            await _context.SaveChangesAsync();

            await SaveExamQuestions(exam.ExamId, model.Questions);

            TempData["SuccessMessage"] = "Exam created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Exams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
            }

            if (!await CanAccessExam(exam))
            {
                return Forbid();
            }

            var model = new ExamFormViewModel
            {
                ExamId = exam.ExamId,
                ClassId = exam.ClassId,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                Duration = exam.Duration,
                MaxScore = exam.MaxScore,
                Status = exam.Status,
                Description = exam.Description
            };

            await LoadFormData(model, exam.ExamQuestions);

            return View(model);
        }

        // POST: Exams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ExamFormViewModel model)
        {
            if (id != model.ExamId)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
            }

            if (!await CanAccessExam(exam))
            {
                return Forbid();
            }

            if (!await CanCreateClass(model.ClassId))
            {
                ModelState.AddModelError("ClassId", "You cannot use this class.");
            }

            if (!ModelState.IsValid)
            {
                await LoadFormData(model, exam.ExamQuestions);
                return View(model);
            }

            exam.ClassId = model.ClassId;
            exam.ExamName = model.ExamName;
            exam.ExamType = model.ExamType;
            exam.ExamDate = model.ExamDate;
            exam.Duration = model.Duration;
            exam.MaxScore = model.MaxScore;
            exam.Status = model.Status;
            exam.Description = model.Description;

            _context.ExamQuestions.RemoveRange(exam.ExamQuestions);

            await _context.SaveChangesAsync();

            await SaveExamQuestions(exam.ExamId, model.Questions);

            TempData["SuccessMessage"] = "Exam updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Exams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exam = await _context.Exams
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Course)
                .Include(e => e.ExamQuestions)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
            }

            if (!await CanAccessExam(exam))
            {
                return Forbid();
            }

            return View(exam);
        }

        // POST: Exams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .Include(e => e.ExamResults)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
            }

            if (!await CanAccessExam(exam))
            {
                return Forbid();
            }

            _context.ExamQuestions.RemoveRange(exam.ExamQuestions);
            _context.ExamResults.RemoveRange(exam.ExamResults);
            _context.Exams.Remove(exam);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Exam deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadFormData(
            ExamFormViewModel model,
            ICollection<ExamQuestion>? existingQuestions = null)
        {
            var classesQuery = _context.Classes
                .Include(c => c.Course)
                .AsQueryable();

            if (User.IsInRole("Teacher"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user?.TeacherId != null)
                {
                    classesQuery = classesQuery.Where(c =>
                        c.TeacherId == user.TeacherId.Value);
                }
            }

            var classes = await classesQuery
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            ViewBag.Classes = new SelectList(
                classes,
                "LanguageClassId",
                "ClassName",
                model.ClassId);

            var questions = await _context.Questions
                .OrderBy(q => q.QuestionId)
                .ToListAsync();

            var selectedIds = existingQuestions?
                .Select(eq => eq.QuestionId)
                .ToHashSet()
                ?? new HashSet<int>();

            model.Questions = questions
                .Select(q => new QuestionSelectionViewModel
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Score = q.Score,
                    Selected = selectedIds.Contains(q.QuestionId)
                })
                .ToList();
        }

        private async Task SaveExamQuestions(
            int examId,
            List<QuestionSelectionViewModel> questions)
        {
            var selectedQuestions = questions
                .Where(q => q.Selected)
                .ToList();

            int order = 1;

            foreach (var question in selectedQuestions)
            {
                _context.ExamQuestions.Add(new ExamQuestion
                {
                    ExamId = examId,
                    QuestionId = question.QuestionId,
                    QuestionOrder = order++
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> CanAccessExam(Exam exam)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            if (!User.IsInRole("Teacher"))
            {
                return false;
            }

            var user = await _userManager.GetUserAsync(User);

            return user?.TeacherId != null &&
                   exam.Class != null &&
                   exam.Class.TeacherId == user.TeacherId.Value;
        }

        private async Task<bool> CanCreateClass(int classId)
        {
            if (User.IsInRole("Admin"))
            {
                return await _context.Classes
                    .AnyAsync(c => c.LanguageClassId == classId);
            }

            if (!User.IsInRole("Teacher"))
            {
                return false;
            }

            var user = await _userManager.GetUserAsync(User);

            if (user?.TeacherId == null)
            {
                return false;
            }

            return await _context.Classes.AnyAsync(c =>
                c.LanguageClassId == classId &&
                c.TeacherId == user.TeacherId.Value);
        }

        private async Task<bool> CanCreateExam()
        {
            return User.IsInRole("Admin") || User.IsInRole("Teacher");
        }
    }
}