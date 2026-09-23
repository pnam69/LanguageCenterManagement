using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
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
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Questions
                .Include(q => q.Answers)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(q =>
                    q.QuestionText.Contains(search) ||
                    q.QuestionType.Contains(search) ||
                    q.Skill.Contains(search));
            }

            var questions = await query
                .OrderByDescending(q => q.QuestionId)
                .ToListAsync();

            ViewBag.Search = search;

            return View(questions);
        }

        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.ExamQuestions)
                    .ThenInclude(eq => eq.Exam)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // GET: Questions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Questions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Question question,
            string[]? answerTexts,
            int? correctAnswer)
        {
            if (question.QuestionType == "MultipleChoice")
            {
                if (answerTexts == null || answerTexts.Length < 2)
                {
                    ModelState.AddModelError(
                        "",
                        "Multiple choice questions need at least 2 answers.");
                }
                else
                {
                    var validAnswers = answerTexts
                        .Where(a => !string.IsNullOrWhiteSpace(a))
                        .ToList();

                    if (validAnswers.Count < 2)
                    {
                        ModelState.AddModelError(
                            "",
                            "Please provide at least 2 answers.");
                    }

                    if (!correctAnswer.HasValue ||
                        correctAnswer.Value < 0 ||
                        correctAnswer.Value >= answerTexts.Length ||
                        string.IsNullOrWhiteSpace(answerTexts[correctAnswer.Value]))
                    {
                        ModelState.AddModelError(
                            "",
                            "Please select the correct answer.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(question);
            }

            _context.Questions.Add(question);

            await _context.SaveChangesAsync();

            if (question.QuestionType == "MultipleChoice" &&
                answerTexts != null)
            {
                for (int i = 0; i < answerTexts.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(answerTexts[i]))
                    {
                        continue;
                    }

                    _context.Answers.Add(new Answer
                    {
                        QuestionId = question.QuestionId,
                        AnswerText = answerTexts[i],
                        IsCorrect = correctAnswer == i
                    });
                }

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Question created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Questions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // POST: Questions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Question question,
            string[]? answerTexts,
            int? correctAnswer)
        {
            if (id != question.QuestionId)
            {
                return NotFound();
            }

            var existingQuestion = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (existingQuestion == null)
            {
                return NotFound();
            }

            if (question.QuestionType == "MultipleChoice")
            {
                if (answerTexts == null ||
                    answerTexts.Count(a => !string.IsNullOrWhiteSpace(a)) < 2)
                {
                    ModelState.AddModelError(
                        "",
                        "Multiple choice questions need at least 2 answers.");
                }

                if (!correctAnswer.HasValue ||
                    answerTexts == null ||
                    correctAnswer.Value < 0 ||
                    correctAnswer.Value >= answerTexts.Length ||
                    string.IsNullOrWhiteSpace(answerTexts[correctAnswer.Value]))
                {
                    ModelState.AddModelError(
                        "",
                        "Please select the correct answer.");
                }
            }

            if (!ModelState.IsValid)
            {
                question.Answers = existingQuestion.Answers;
                return View(question);
            }

            existingQuestion.QuestionText = question.QuestionText;
            existingQuestion.QuestionType = question.QuestionType;
            existingQuestion.Score = question.Score;
            existingQuestion.Skill = question.Skill;

            _context.Answers.RemoveRange(existingQuestion.Answers);

            if (question.QuestionType == "MultipleChoice" &&
                answerTexts != null)
            {
                for (int i = 0; i < answerTexts.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(answerTexts[i]))
                    {
                        continue;
                    }

                    _context.Answers.Add(new Answer
                    {
                        QuestionId = existingQuestion.QuestionId,
                        AnswerText = answerTexts[i],
                        IsCorrect = correctAnswer == i
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Question updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Questions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.ExamQuestions)
                    .ThenInclude(eq => eq.Exam)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // POST: Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.ExamQuestions)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            if (question.ExamQuestions.Any())
            {
                TempData["Error"] =
                    "Cannot delete this question because it is being used in an exam.";

                return RedirectToAction(nameof(Index));
            }

            _context.Answers.RemoveRange(question.Answers);
            _context.Questions.Remove(question);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Question deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}