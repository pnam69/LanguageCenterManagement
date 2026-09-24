using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize]
    public class ReadingContentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReadingContentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ReadingContents
        public async Task<IActionResult> Index()
        {
            var contents = await _context.ReadingContents
                .Include(x => x.Question)
                .OrderBy(x => x.ReadingContentId)
                .ToListAsync();

            return View(contents);
        }

        // GET: ReadingContents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.ReadingContents
                .Include(x => x.Question)
                .FirstOrDefaultAsync(x =>
                    x.ReadingContentId == id);

            if (content == null)
                return NotFound();

            return View(content);
        }

        // GET: ReadingContents/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ReadingContents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ReadingContentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var question = new Question
            {
                QuestionText = model.QuestionText,
                QuestionType = "Reading",
                Skill = "Reading",
                Score = model.Score
            };

            var content = new ReadingContent
            {
                Question = question,
                Passage = model.Passage
            };

            _context.ReadingContents.Add(content);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Reading content created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: ReadingContents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.ReadingContents
                .Include(x => x.Question)
                .FirstOrDefaultAsync(x =>
                    x.ReadingContentId == id);

            if (content == null)
                return NotFound();

            var model = new ReadingContentViewModel
            {
                ReadingContentId = content.ReadingContentId,
                QuestionId = content.QuestionId,
                QuestionText = content.Question?.QuestionText
                    ?? string.Empty,
                Passage = content.Passage,
                Score = content.Question?.Score ?? 1
            };

            return View(model);
        }

        // POST: ReadingContents/Edit/5
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
                .Include(x => x.Question)
                .FirstOrDefaultAsync(x =>
                    x.ReadingContentId == id);

            if (content == null)
                return NotFound();

            content.Passage = model.Passage;

            if (content.Question != null)
            {
                content.Question.QuestionText =
                    model.QuestionText;

                content.Question.Score =
                    model.Score;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Reading content updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: ReadingContents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.ReadingContents
                .Include(x => x.Question)
                .FirstOrDefaultAsync(x =>
                    x.ReadingContentId == id);

            if (content == null)
                return NotFound();

            return View(content);
        }

        // POST: ReadingContents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var content = await _context.ReadingContents
                .Include(x => x.Question)
                .FirstOrDefaultAsync(x =>
                    x.ReadingContentId == id);

            if (content == null)
                return NotFound();

            _context.ReadingContents.Remove(content);

            if (content.Question != null)
            {
                _context.Questions.Remove(content.Question);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Reading content deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}