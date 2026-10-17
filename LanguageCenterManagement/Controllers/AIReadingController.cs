
using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.Services.Ai;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class AIReadingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReadingGenerationService _ollamaService;
        private readonly ILogger<AIReadingController> _logger;

        public AIReadingController(
            ApplicationDbContext context,
            ReadingGenerationService ollamaService,
            ILogger<AIReadingController> logger)
        {
            _context = context;
            _ollamaService = ollamaService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new AIReadingGenerationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            AIReadingGenerationViewModel model,
            CancellationToken cancellationToken)
        {
            // Passage is generated, so validate the other inputs first.
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

                if (cancellationToken.IsCancellationRequested)
                    return new EmptyResult();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return new EmptyResult();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "AI reading generation timed out.");

                ModelState.AddModelError(
                    string.Empty,
                    "Passage generation took too long. Please try again.");

                return View("Index", model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ollama reading generation failed.");

                ModelState.AddModelError(
                    string.Empty,
                    "Cannot connect to the AI service. Check that Ollama is running and try again.");

                return View("Index", model);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Ollama returned an invalid reading passage.");

                ModelState.AddModelError(
                    string.Empty,
                    "The AI could not generate a valid passage. Please try again.");

                return View("Index", model);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Could not parse the AI reading response.");

                ModelState.AddModelError(
                    string.Empty,
                    "The AI response could not be processed. Please try again.");

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected AI reading generation error.");

                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while generating the passage. Please try again.");

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
