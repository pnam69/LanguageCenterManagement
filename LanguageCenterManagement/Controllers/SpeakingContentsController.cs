using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize]
    public class SpeakingContentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SpeakingContentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SpeakingContents
        public async Task<IActionResult> Index()
        {
            var contents = await _context.SpeakingContents
                .Include(x => x.Question)
                .OrderBy(x => x.SpeakingContentId)
                .ToListAsync();

            return View(contents);
        }

        // GET: SpeakingContents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.SpeakingContents
                .Include(x => x.Question)
                .FirstOrDefaultAsync(x =>
                    x.SpeakingContentId == id);

            if (content == null)
                return NotFound();

            return View(content);
        }

        // GET: SpeakingContents/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SpeakingContents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            SpeakingContentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            /*
			 * SpeakingContent requires a QuestionId in the
			 * current database design.
			 *
			 * The question itself will be created separately
			 * when question management is implemented.
			 */
            var question = new Question
            {
                QuestionText = model.Prompt,
                QuestionType = "Speaking",
                Skill = "Speaking",
                Score = 1
            };

            var content = new SpeakingContent
            {
                Question = question,
                Prompt = model.Prompt,
                PreparationTime = model.PreparationTime,
                ResponseTime = model.ResponseTime
            };

            _context.SpeakingContents.Add(content);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Speaking content created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: SpeakingContents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.SpeakingContents
                .FindAsync(id);

            if (content == null)
                return NotFound();

            var model = new SpeakingContentViewModel
            {
                SpeakingContentId = content.SpeakingContentId,
                Prompt = content.Prompt,
                PreparationTime = content.PreparationTime,
                ResponseTime = content.ResponseTime
            };

            return View(model);
        }

        // POST: SpeakingContents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            SpeakingContentViewModel model)
        {
            if (id != model.SpeakingContentId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var content = await _context.SpeakingContents
                .Include(x => x.Question)
                .FirstOrDefaultAsync(x =>
                    x.SpeakingContentId == id);

            if (content == null)
                return NotFound();

            content.Prompt = model.Prompt;
            content.PreparationTime = model.PreparationTime;
            content.ResponseTime = model.ResponseTime;

            if (content.Question != null)
            {
                content.Question.QuestionText = model.Prompt;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Speaking content updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: SpeakingContents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.SpeakingContents
                .Include(x => x.Question)
                .FirstOrDefaultAsync(x =>
                    x.SpeakingContentId == id);

            if (content == null)
                return NotFound();

            return View(content);
        }

        // POST: SpeakingContents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var content = await _context.SpeakingContents
                .Include(x => x.Question)
                .FirstOrDefaultAsync(x =>
                    x.SpeakingContentId == id);

            if (content == null)
                return NotFound();

            _context.SpeakingContents.Remove(content);

            if (content.Question != null)
            {
                _context.Questions.Remove(content.Question);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Speaking content deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}