using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ListeningAudiosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ListeningAudiosController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var audios = await _context.ListeningContents
                .Include(a => a.Questions)
                .OrderByDescending(a => a.ListeningContentId)
                .ToListAsync();

            return View(audios);
        }

        public IActionResult Create()
        {
            return View(new ListeningAudioCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ListeningAudioCreateViewModel model)
        {
            if (model.AudioFile == null || model.AudioFile.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.AudioFile),
                    "Please select an audio file.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "listening");

            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(model.AudioFile!.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";

            var filePath = Path.Combine(
                uploadsFolder,
                fileName);

            await using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await model.AudioFile.CopyToAsync(stream);
            }

            var audio = new ListeningContent
            {
                Title = model.Title,
                AudioUrl = $"/uploads/listening/{fileName}",
                Transcript = model.Transcript
            };

            _context.ListeningContents.Add(audio);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Listening audio created successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}