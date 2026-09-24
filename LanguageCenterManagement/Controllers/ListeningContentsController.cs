using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize]
    public class ListeningContentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ListeningContentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ListeningContents
        public async Task<IActionResult> Index()
        {
            var contents = await _context.ListeningContents
                .Include(x => x.Questions)
                .OrderBy(x => x.Title)
                .ToListAsync();

            return View(contents);
        }

        // GET: ListeningContents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.ListeningContents
                .Include(x => x.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(x => x.ListeningContentId == id);

            if (content == null)
                return NotFound();

            return View(content);
        }

        // GET: ListeningContents/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ListeningContents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListeningContentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var content = new ListeningContent
            {
                Title = model.Title,
                AudioUrl = model.AudioUrl,
                Transcript = model.Transcript
            };

            _context.ListeningContents.Add(content);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Listening content created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: ListeningContents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.ListeningContents
                .FindAsync(id);

            if (content == null)
                return NotFound();

            var model = new ListeningContentViewModel
            {
                ListeningContentId = content.ListeningContentId,
                Title = content.Title,
                AudioUrl = content.AudioUrl,
                Transcript = content.Transcript
            };

            return View(model);
        }

        // POST: ListeningContents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ListeningContentViewModel model)
        {
            if (id != model.ListeningContentId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var content = await _context.ListeningContents
                .FindAsync(id);

            if (content == null)
                return NotFound();

            content.Title = model.Title;
            content.AudioUrl = model.AudioUrl;
            content.Transcript = model.Transcript;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Listening content updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: ListeningContents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.ListeningContents
                .Include(x => x.Questions)
                .FirstOrDefaultAsync(x => x.ListeningContentId == id);

            if (content == null)
                return NotFound();

            return View(content);
        }

        // POST: ListeningContents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var content = await _context.ListeningContents
                .FindAsync(id);

            if (content == null)
                return NotFound();

            _context.ListeningContents.Remove(content);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Listening content deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}