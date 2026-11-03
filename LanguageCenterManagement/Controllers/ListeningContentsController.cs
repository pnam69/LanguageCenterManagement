using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace LanguageCenterManagement.Controllers
{
    [Authorize]
    public class ListeningContentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ListeningContentsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
            return View(new ListeningAudioCreateViewModel());
        }

        // POST: ListeningContents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListeningAudioCreateViewModel model)
        {
            if (model.AudioFile == null || model.AudioFile.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.AudioFile),
                    "Please select an audio file.");
            }

            if (model.AudioFile != null &&
                model.AudioFile.Length > 20 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    nameof(model.AudioFile),
                    "The audio file cannot exceed 20 MB.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var extension = Path.GetExtension(model.AudioFile!.FileName)
                .ToLowerInvariant();

            var allowedExtensions = new[] { ".mp3" };

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    nameof(model.AudioFile),
                    "Only MP3 files are allowed.");

                return View(model);
            }

            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "listening");

            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            try
            {
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AudioFile.CopyToAsync(stream);
                }

                var content = new ListeningContent
                {
                    Title = model.Title,
                    AudioUrl = $"/uploads/listening/{fileName}",
                    Transcript = model.Transcript
                };

                _context.ListeningContents.Add(content);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Listening content created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                throw;
            }
        }

        // GET: ListeningContents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var content = await _context.ListeningContents.FindAsync(id);

            if (content == null)
                return NotFound();

            var model = new ListeningAudioCreateViewModel
            {
                Title = content.Title,
                Transcript = content.Transcript
            };

            ViewBag.CurrentAudioUrl = content.AudioUrl;
            ViewBag.ListeningContentId = content.ListeningContentId;

            return View(model);
        }

        // POST: ListeningContents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ListeningAudioCreateViewModel model)
        {
            var content = await _context.ListeningContents.FindAsync(id);

            if (content == null)
                return NotFound();

            ViewBag.CurrentAudioUrl = content.AudioUrl;
            ViewBag.ListeningContentId = id;

            if (model.AudioFile != null && model.AudioFile.Length > 0)
            {
                if (model.AudioFile.Length > 20 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(model.AudioFile),
                        "The audio file cannot exceed 20 MB.");
                }
                else
                {
                    var extension = Path.GetExtension(model.AudioFile.FileName)
                        .ToLowerInvariant();

                    var allowedExtensions = new[]
                    {
                ".mp3"
            };

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(
                            nameof(model.AudioFile),
                            "Unsupported audio format.");
                    }
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            string? newFilePath = null;
            string? oldFilePath = null;

            try
            {
                if (model.AudioFile != null && model.AudioFile.Length > 0)
                {
                    var extension = Path.GetExtension(model.AudioFile.FileName)
                        .ToLowerInvariant();

                    var uploadFolder = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "listening");

                    Directory.CreateDirectory(uploadFolder);

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    newFilePath = Path.Combine(uploadFolder, fileName);

                    await using (var stream = new FileStream(
                        newFilePath, FileMode.Create))
                    {
                        await model.AudioFile.CopyToAsync(stream);
                    }

                    oldFilePath = Path.Combine(
                        _environment.WebRootPath,
                        content.AudioUrl.TrimStart('/')
                            .Replace('/', Path.DirectorySeparatorChar));

                    content.AudioUrl = $"/uploads/listening/{fileName}";
                }

                content.Title = model.Title;
                content.Transcript = model.Transcript;

                await _context.SaveChangesAsync();

                if (oldFilePath != null && System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);

                TempData["SuccessMessage"] =
                    "Listening content updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                if (newFilePath != null && System.IO.File.Exists(newFilePath))
                    System.IO.File.Delete(newFilePath);

                throw;
            }
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

        private async Task<string> SaveAudioFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".mp3" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Only MP3 audio files are allowed.");

            if (file.Length == 0 || file.Length > 20 * 1024 * 1024)
                throw new InvalidOperationException("Audio file must be between 1 byte and 20 MB.");

            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "listening");

            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await file.CopyToAsync(stream);

            return $"/uploads/listening/{fileName}";
        }

        private void DeleteAudioFile(string? audioUrl)
        {
            const string prefix = "/uploads/listening/";

            if (string.IsNullOrWhiteSpace(audioUrl) ||
                !audioUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return;

            var fileName = Path.GetFileName(audioUrl);

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var filePath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "listening",
                fileName);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}