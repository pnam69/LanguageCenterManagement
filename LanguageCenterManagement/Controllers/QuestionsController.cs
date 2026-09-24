using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class QuestionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Questions
        public async Task<IActionResult> Index(
            string? search,
            string? skill)
        {
            var query = _context.Questions
                .Include(q => q.Answers)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(q =>
                    q.QuestionText.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(skill))
            {
                query = query.Where(q =>
                    q.Skill == skill);
            }

            var questions = await query
                .OrderBy(q => q.Skill)
                .ThenBy(q => q.QuestionId)
                .ToListAsync();

            return View(questions);
        }

        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var question = await _context.Questions
                .Include(q => q.Answers)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            return View(question);
        }

        // GET: Questions/Create
        public IActionResult Create()
        {
            var model = new QuestionManagementViewModel();

            for (int i = 0; i < 4; i++)
            {
                model.Answers.Add(new AnswerManagementViewModel());
            }

            return View(model);
        }

        // POST: Questions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            QuestionManagementViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.QuestionType == "MultipleChoice")
            {
                var validAnswers = model.Answers
                    .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                    .ToList();

                if (validAnswers.Count < 2)
                {
                    ModelState.AddModelError(
                        "Answers",
                        "Multiple choice questions must have at least 2 answers.");

                    return View(model);
                }

                if (validAnswers.Count(a => a.IsCorrect) != 1)
                {
                    ModelState.AddModelError(
                        "Answers",
                        "Multiple choice questions must have exactly one correct answer.");

                    return View(model);
                }
            }

            var question = new Question
            {
                QuestionText = model.QuestionText,
                QuestionType = model.QuestionType,
                Skill = model.Skill,
                Score = model.Score
            };

            foreach (var answerModel in model.Answers)
            {
                if (string.IsNullOrWhiteSpace(answerModel.AnswerText))
                    continue;

                question.Answers.Add(new Answer
                {
                    AnswerText = answerModel.AnswerText,
                    IsCorrect = answerModel.IsCorrect
                });
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Questions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            var model = new QuestionManagementViewModel
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Skill = question.Skill,
                Score = question.Score,
                Answers = question.Answers.Select(a =>
                    new AnswerManagementViewModel
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect
                    }).ToList()
            };

            while (model.Answers.Count < 4)
            {
                model.Answers.Add(new AnswerManagementViewModel());
            }

            return View(model);
        }

        // POST: Questions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            QuestionManagementViewModel model)
        {
            if (id != model.QuestionId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            if (model.QuestionType == "MultipleChoice")
            {
                var validAnswers = model.Answers
                    .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                    .ToList();

                if (validAnswers.Count < 2)
                {
                    ModelState.AddModelError(
                        "Answers",
                        "Multiple choice questions must have at least 2 answers.");

                    return View(model);
                }

                if (validAnswers.Count(a => a.IsCorrect) != 1)
                {
                    ModelState.AddModelError(
                        "Answers",
                        "Multiple choice questions must have exactly one correct answer.");

                    return View(model);
                }
            }

            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            question.QuestionText = model.QuestionText;
            question.QuestionType = model.QuestionType;
            question.Skill = model.Skill;
            question.Score = model.Score;

            _context.Answers.RemoveRange(question.Answers);

            question.Answers.Clear();

            foreach (var answerModel in model.Answers)
            {
                if (string.IsNullOrWhiteSpace(answerModel.AnswerText))
                    continue;

                question.Answers.Add(new Answer
                {
                    QuestionId = question.QuestionId,
                    AnswerText = answerModel.AnswerText,
                    IsCorrect = answerModel.IsCorrect
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Questions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var question = await _context.Questions
                .Include(q => q.Answers)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            return View(question);
        }

        // POST: Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            var usedInExam = await _context.ExamQuestions
                .AnyAsync(eq => eq.QuestionId == id);

            if (usedInExam)
            {
                TempData["ErrorMessage"] =
                    "This question cannot be deleted because it is already used in an exam.";

                return RedirectToAction(nameof(Index));
            }

            _context.Questions.Remove(question);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}