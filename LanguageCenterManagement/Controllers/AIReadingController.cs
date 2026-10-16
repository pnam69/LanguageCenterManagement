using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.Services.Ai;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class AIReadingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReadingGenerationService _ollamaService;

        public AIReadingController(
            ApplicationDbContext context,
            ReadingGenerationService ollamaService)
        {
            _context = context;
            _ollamaService = ollamaService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new AIReadingGenerationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            AIReadingGenerationViewModel model)
        {
            // Passage is empty before generation, so validate the inputs first.
            ModelState.Remove(nameof(model.Passage));

            if (!ModelState.IsValid)
                return View("Index", model);

            try
            {
                model.Passage = await _ollamaService.GeneratePassageAsync(
                    model.Topic,
                    model.Level,
                    model.WordCount,
                    model.Focus);
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is TaskCanceledException ||
                ex is InvalidOperationException ||
                ex is System.Text.Json.JsonException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Could not generate the passage. " +
                    "Check that Ollama is running and the configured model " +
                    "exists. Details: " + ex.Message);

                return View("Index", model);
            }

            ViewBag.Generated = true;
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            AIReadingGenerationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Passage))
            {
                ModelState.AddModelError(
                    nameof(model.Passage),
                    "Generate or enter a passage before saving.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Generated = true;
                return View("Index", model);
            }

            var readingContent = new ReadingContent
            {
                Passage = model.Passage.Trim()
            };

            _context.ReadingContents.Add(readingContent);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Reading passage saved successfully.";

            return RedirectToAction(
                "Details",
                "ReadingContents",
                new { id = readingContent.ReadingContentId });
        }
    }
}
