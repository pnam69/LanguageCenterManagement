
using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ReadingContentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReadingContentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var contents = await _context.ReadingContents
                .Include(x => x.Questions)
                .OrderBy(x => x.ReadingContentId)
                .ToListAsync();

            return View(contents);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var readingContent = await _context.ReadingContents
                .Include(r => r.Questions)
                .FirstOrDefaultAsync(r => r.ReadingContentId == id);

            if (readingContent == null)
                return NotFound();

            var availableQuestions = await _context.Questions
                .Where(q => q.Skill == "Reading"
                    && q.ReadingContentId == null)
                .OrderBy(q => q.QuestionId)
                .Select(q => new
                {
                    q.QuestionId,
                    q.QuestionText
                })
                .ToListAsync();

            ViewBag.AvailableReadingQuestions = availableQuestions
                .Select(q => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = q.QuestionId.ToString(),
                    Text = $"#{q.QuestionId}: " +
                        (q.QuestionText.Length > 70
                            ? q.QuestionText.Substring(0, 70) + "..."
                            : q.QuestionText)
                })
                .ToList();

            return View(readingContent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkQuestion(int id, int questionId)
        {
            var passage = await _context.ReadingContents
                .FirstOrDefaultAsync(r => r.ReadingContentId == id);

            if (passage == null)
                return NotFound();

            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
            {
                TempData["ErrorMessage"] = "Question not found.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (question.Skill != "Reading")
            {
                TempData["ErrorMessage"] =
                    "Only Reading questions can be linked to a passage.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (question.ReadingContentId != null)
            {
                TempData["ErrorMessage"] =
                    "This question is already linked to a passage.";
                return RedirectToAction(nameof(Details), new { id });
            }

            question.ReadingContentId = passage.ReadingContentId;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question linked to the passage.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public IActionResult Create()
        {
            return View(new ReadingContentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ReadingContentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var content = new ReadingContent
            {
                Passage = model.Passage
            };

            _context.ReadingContents.Add(content);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Reading passage created successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.ReadingContents
                .FirstOrDefaultAsync(x =>
                    x.ReadingContentId == id);

            if (content == null)
                return NotFound();

            return View(new ReadingContentViewModel
            {
                ReadingContentId = content.ReadingContentId,
                Passage = content.Passage
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ReadingContentViewModel model)
        {
            if (id != model.ReadingContentId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var content = await _context.ReadingContents
                .FirstOrDefaultAsync(x =>
                    x.ReadingContentId == id);

            if (content == null)
                return NotFound();

            content.Passage = model.Passage;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Reading passage updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.ReadingContents
                .Include(x => x.Questions)
                .FirstOrDefaultAsync(x =>
                    x.ReadingContentId == id);

            if (content == null)
                return NotFound();

            return View(content);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var content = await _context.ReadingContents
                .Include(x => x.Questions)
                .FirstOrDefaultAsync(x =>
                    x.ReadingContentId == id);

            if (content == null)
                return NotFound();

            // Keep the questions. Only unlink them from this passage.
            foreach (var question in content.Questions)
            {
                question.ReadingContentId = null;
            }

            _context.ReadingContents.Remove(content);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Reading passage deleted successfully. Associated questions were kept.";

            return RedirectToAction(nameof(Index));
        }
    }
}
