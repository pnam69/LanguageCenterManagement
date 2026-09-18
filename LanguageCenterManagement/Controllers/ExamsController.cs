using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Exams
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Exams
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Course)
                .Include(e => e.ExamQuestions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.ExamName.Contains(search) ||
                    e.ExamType.Contains(search) ||
                    (e.Class != null &&
                        (e.Class.ClassCode.Contains(search) ||
                         e.Class.ClassName.Contains(search))));
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

            return View(exam);
        }

        // GET: Exams/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View(new ExamCreateViewModel
            {
                ExamDate = DateTime.Now,
                Duration = 60,
                MaxScore = 10,
                Status = "Draft",
                ExamType = "Test"
            });
        }

        // POST: Exams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamCreateViewModel model)
        {
            if (model.SelectedQuestionIds == null ||
                !model.SelectedQuestionIds.Any())
            {
                ModelState.AddModelError(
                    "SelectedQuestionIds",
                    "Please select at least one question.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model.ClassId, model.SelectedQuestionIds);
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

            for (int i = 0; i < model.SelectedQuestionIds.Count; i++)
            {
                _context.ExamQuestions.Add(new ExamQuestion
                {
                    ExamId = exam.ExamId,
                    QuestionId = model.SelectedQuestionIds[i],
                    QuestionOrder = i + 1
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Exam created successfully with selected questions.";

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

            var model = new ExamCreateViewModel
            {
                ClassId = exam.ClassId,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                Duration = exam.Duration,
                MaxScore = exam.MaxScore,
                Status = exam.Status,
                Description = exam.Description,
                SelectedQuestionIds = exam.ExamQuestions
                    .OrderBy(eq => eq.QuestionOrder)
                    .Select(eq => eq.QuestionId)
                    .ToList()
            };

            await LoadDropdowns(
                model.ClassId,
                model.SelectedQuestionIds);

            ViewBag.ExamId = exam.ExamId;

            return View(model);
        }

        // POST: Exams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ExamCreateViewModel model)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            if (model.SelectedQuestionIds == null ||
                !model.SelectedQuestionIds.Any())
            {
                ModelState.AddModelError(
                    "SelectedQuestionIds",
                    "Please select at least one question.");
            }

            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(
                    model.ClassId,
                    model.SelectedQuestionIds);

                ViewBag.ExamId = id;

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

            for (int i = 0; i < model.SelectedQuestionIds.Count; i++)
            {
                _context.ExamQuestions.Add(new ExamQuestion
                {
                    ExamId = id,
                    QuestionId = model.SelectedQuestionIds[i],
                    QuestionOrder = i + 1
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Exam updated successfully.";

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
                .Include(e => e.ExamQuestions)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
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

            _context.ExamQuestions.RemoveRange(exam.ExamQuestions);

            _context.ExamResults.RemoveRange(exam.ExamResults);

            _context.Exams.Remove(exam);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Exam deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns(
            int? selectedClassId = null,
            List<int>? selectedQuestionIds = null)
        {
            var classes = await _context.Classes
                .Include(c => c.Course)
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            var questions = await _context.Questions
                .Include(q => q.Answers)
                .OrderBy(q => q.QuestionId)
                .ToListAsync();

            ViewBag.ClassId = new SelectList(
                classes,
                "LanguageClassId",
                "ClassName",
                selectedClassId);

            ViewBag.Questions = questions;

            ViewBag.SelectedQuestionIds =
                selectedQuestionIds ?? new List<int>();
        }
    }
}