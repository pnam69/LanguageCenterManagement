using LanguageCenterManagement.Services.Ai;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanguageCenterManagement.Controllers
{
    [Authorize]
    public class AIWritingController : Controller
    {
        private readonly AiWritingAssessmentService _writingService;
        private readonly ILogger<AIWritingController> _logger;

        public AIWritingController(
            AiWritingAssessmentService writingService,
            ILogger<AIWritingController> logger)
        {
            _writingService = writingService;
            _logger = logger;
        }

        [Authorize(Roles = "Student")]
        [HttpGet]
        public IActionResult Practice()
        {
            return View(new WritingAssessmentInputViewModel());
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Practice(
            WritingAssessmentInputViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                model.Assessment = await _writingService.AssessAsync(
                    model.TaskPrompt,
                    model.StudentWriting,
                    cancellationToken);

                return View("Results", model);
            }

            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // The browser disconnected or the request was cancelled.
                return new EmptyResult();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "AI writing assessment timed out.");

                ModelState.AddModelError(
                    string.Empty,
                    "The assessment took too long. Please try again.");
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "AI student writing assessment failed.");

                ModelState.AddModelError(string.Empty,
                    "The AI assessment failed. Check that Ollama is running and try again.");
            }

            return View(model);
        }

        [Authorize(Roles = "Admin,Teacher")]
        [HttpGet]
        public IActionResult Assess()
        {
            return View(new WritingAssessmentInputViewModel());
        }

        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assess(
            WritingAssessmentInputViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                model.Assessment = await _writingService.AssessAsync(
                    model.TaskPrompt,
                    model.StudentWriting,
                    cancellationToken);

                return View("Results", model);
            }

            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // The browser disconnected or the request was cancelled.
                return new EmptyResult();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "AI writing assessment timed out.");

                ModelState.AddModelError(
                    string.Empty,
                    "The assessment took too long. Please try again.");
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "AI teacher writing assessment failed.");

                ModelState.AddModelError(string.Empty,
                    "The AI assessment failed. Check that Ollama is running and try again.");
            }

            return View(model);
        }
    }
}