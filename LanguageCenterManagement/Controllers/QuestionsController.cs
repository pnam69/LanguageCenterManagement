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
                .Include(q => q.ReadingContent)
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
        public async Task<IActionResult> Create()
        {
            var model = new QuestionManagementViewModel();

            for (int i = 0; i < 4; i++)
            {
                model.Answers.Add(new AnswerManagementViewModel());
            }

            await LoadReadingContents();
            return View(model);
        }

        // POST: Questions/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuestionManagementViewModel model)
        {
            // Writing questions do not need answer choices.
            if (model.Skill == "Writing")
            {
                ModelState.Remove(nameof(model.QuestionType));

                if (model.Answers != null)
                {
                    ModelState.Remove("Answers");
                    for (int i = 0; i < model.Answers.Count; i++)
                    {
                        ModelState.Remove($"Answers[{i}].AnswerText");
                        ModelState.Remove($"Answers[{i}].IsCorrect");
                    }
                }

                model.QuestionType = "ShortAnswer";
            }

            if (!ModelState.IsValid)
            {
                await LoadReadingContents(model.ReadingContentId);
                return View(model);
            }

            if (model.Skill == "Reading" && model.ReadingContentId == null)
            {
                ModelState.AddModelError(
                    nameof(model.ReadingContentId),
                    "Please select a reading passage.");

                await LoadReadingContents(model.ReadingContentId);
                return View(model);
            }

            if (model.Skill == "Writing")
            {
                if (string.IsNullOrWhiteSpace(model.WritingPrompt))
                {
                    ModelState.AddModelError(
                        nameof(model.WritingPrompt),
                        "Please enter a writing prompt.");
                }

                if (model.MinimumWords < 0 ||
                    model.MaximumWords < model.MinimumWords)
                {
                    ModelState.AddModelError(
                        nameof(model.MaximumWords),
                        "Maximum words must be greater than or equal to minimum words.");
                }

                if (!ModelState.IsValid)
                {
                    await LoadReadingContents(model.ReadingContentId);
                    return View(model);
                }
            }
            else if (model.QuestionType == "MultipleChoice")
            {
                var validAnswers = model.Answers?
                    .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                    .ToList() ?? new();

                if (validAnswers.Count < 2)
                {
                    ModelState.AddModelError(
                        "Answers",
                        "Multiple choice questions must have at least 2 answers.");

                    await LoadReadingContents(model.ReadingContentId);
                    return View(model);
                }

                if (validAnswers.Count(a => a.IsCorrect) != 1)
                {
                    ModelState.AddModelError(
                        "Answers",
                        "Multiple choice questions must have exactly one correct answer.");

                    await LoadReadingContents(model.ReadingContentId);
                    return View(model);
                }
            }

            var question = new Question
            {
                QuestionText = model.QuestionText,
                QuestionType = model.Skill == "Writing"
                    ? "ShortAnswer"
                    : model.QuestionType,
                Skill = model.Skill,
                Score = model.Score,
                ReadingContentId = model.Skill == "Reading"
                    ? model.ReadingContentId
                    : null
            };

            if (model.Skill != "Writing" && model.Answers != null)
            {
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
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            if (model.Skill == "Writing")
            {
                var writingContent = new WritingContent
                {
                    QuestionId = question.QuestionId,
                    Prompt = model.WritingPrompt.Trim(),
                    MinimumWords = model.MinimumWords,
                    MaximumWords = model.MaximumWords
                };

                _context.WritingContents.Add(writingContent);
                await _context.SaveChangesAsync();
            }

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

            var writingContent = await _context.WritingContents
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.QuestionId == question.QuestionId);

            var model = new QuestionManagementViewModel
            {
                ReadingContentId = question.ReadingContentId,
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                WritingPrompt = writingContent?.Prompt ?? string.Empty,
                MinimumWords = writingContent?.MinimumWords ?? 100,
                MaximumWords = writingContent?.MaximumWords ?? 300,
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
            await LoadReadingContents(model.ReadingContentId);
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


            if (model.Skill == "Writing")
            {
                ModelState.Remove(nameof(model.QuestionType));

                if (model.Answers != null)
                {
                    ModelState.Remove("Answers");

                    for (int i = 0; i < model.Answers.Count; i++)
                    {
                        ModelState.Remove($"Answers[{i}].AnswerText");
                        ModelState.Remove($"Answers[{i}].IsCorrect");
                    }
                }

                model.QuestionType = "ShortAnswer";

                if (string.IsNullOrWhiteSpace(model.WritingPrompt))
                {
                    ModelState.AddModelError(
                        nameof(model.WritingPrompt),
                        "Please enter a writing prompt.");
                }

                if (model.MinimumWords < 0 ||
                    model.MaximumWords < model.MinimumWords)
                {
                    ModelState.AddModelError(
                        nameof(model.MaximumWords),
                        "Maximum words must be greater than or equal to minimum words.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadReadingContents(model.ReadingContentId);
                return View(model);
            }

            if (model.Skill == "Reading" && model.ReadingContentId == null)
            {
                ModelState.AddModelError(
                    nameof(model.ReadingContentId),
                    "Please select a reading passage.");

                await LoadReadingContents(model.ReadingContentId);
                return View(model);
            }

            if (model.Skill != "Writing" && model.QuestionType == "MultipleChoice")
            {
                var validAnswers = model.Answers
                    .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                    .ToList();

                if (validAnswers.Count < 2)
                {
                    ModelState.AddModelError(
                        "Answers",
                        "Multiple choice questions must have at least 2 answers.");
                    await LoadReadingContents(model.ReadingContentId);
                    return View(model);
                }

                if (validAnswers.Count(a => a.IsCorrect) != 1)
                {
                    ModelState.AddModelError(
                        "Answers",
                        "Multiple choice questions must have exactly one correct answer.");
                    await LoadReadingContents(model.ReadingContentId);
                    return View(model);
                }
            }

            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            question.QuestionText = model.QuestionText;
            //question.QuestionType = model.QuestionType;
            question.QuestionType = model.Skill == "Writing" ? "ShortAnswer" : model.QuestionType;
            question.Skill = model.Skill;
            question.ReadingContentId = model.Skill == "Reading" ? model.ReadingContentId : null;
            question.Score = model.Score;

            var writingContent = await _context.WritingContents.FirstOrDefaultAsync(w => w.QuestionId == question.QuestionId);

            if (model.Skill == "Writing")
            {
                if (writingContent == null)
                {
                    writingContent = new WritingContent
                    {
                        QuestionId = question.QuestionId,
                        Prompt = model.WritingPrompt.Trim(),
                        MinimumWords = model.MinimumWords,
                        MaximumWords = model.MaximumWords
                    };

                    _context.WritingContents.Add(writingContent);
                }
                else
                {
                    writingContent.Prompt = model.WritingPrompt.Trim();
                    writingContent.MinimumWords = model.MinimumWords;
                    writingContent.MaximumWords = model.MaximumWords;
                }
            }
            else if (writingContent != null)
            {
                _context.WritingContents.Remove(writingContent);
            }
            if (model.Skill != "Writing")
            {
                var submittedAnswerIds = model.Answers
                .Where(a => a.AnswerId > 0)
                .Select(a => a.AnswerId)
                .ToHashSet();

                foreach (var answerModel in model.Answers)
                {
                    if (string.IsNullOrWhiteSpace(answerModel.AnswerText))
                        continue;

                    if (answerModel.AnswerId > 0)
                    {
                        var existingAnswer = question.Answers
                            .FirstOrDefault(a => a.AnswerId == answerModel.AnswerId);

                        if (existingAnswer == null)
                            continue;

                        existingAnswer.AnswerText = answerModel.AnswerText;
                        existingAnswer.IsCorrect = answerModel.IsCorrect;
                    }
                    else
                    {
                        question.Answers.Add(new Answer
                        {
                            QuestionId = question.QuestionId,
                            AnswerText = answerModel.AnswerText,
                            IsCorrect = answerModel.IsCorrect
                        });
                    }
                }
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

        private async Task LoadReadingContents(int? selectedId = null)
        {
            var passages = await _context.ReadingContents
                .OrderBy(r => r.ReadingContentId)
                .Select(r => new
                {
                    r.ReadingContentId,
                    r.Passage
                })
                .ToListAsync();

            ViewBag.ReadingContents = passages.Select(r =>
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = r.ReadingContentId.ToString(),
                    Text = $"Passage #{r.ReadingContentId}: " +
                        (r.Passage.Length > 70
                            ? r.Passage.Substring(0, 70) + "..."
                            : r.Passage),
                    Selected = r.ReadingContentId == selectedId
                }).ToList();
        }
    }


}