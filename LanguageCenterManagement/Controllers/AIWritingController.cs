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
        private readonly AiUsageTrackingService _usageTracking;

        public AIWritingController(
            AiWritingAssessmentService writingService,
            ILogger<AIWritingController> logger,
            AiUsageTrackingService usageTracking)
        {
            _writingService = writingService;
            _logger = logger;
            _usageTracking = usageTracking;
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

            int? usageLogId = null;
            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    usageLogId = await _usageTracking.StartAsync(
                        userId,
                        "StudentWritingAssessment",
                        1,
                        cancellationToken);
                }

                model.Assessment = await _writingService.AssessAsync(
                    model.TaskPrompt,
                    model.StudentWriting,
                    cancellationToken);

                if (usageLogId.HasValue)
                {
                    await _usageTracking.CompleteAsync(
                        usageLogId.Value,
                        "Succeeded",
                        0);

                    usageLogId = null;
                }

                return View("Results", model);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                if (usageLogId.HasValue)
                {
                    await _usageTracking.CompleteAsync(
                        usageLogId.Value,
                        "Cancelled",
                        0,
                        "The request was cancelled.");
                }

                return new EmptyResult();
            }
            catch (OperationCanceledException ex)
            {
                if (usageLogId.HasValue)
                {
                    await _usageTracking.CompleteAsync(
                        usageLogId.Value,
                        "Failed",
                        0,
                        ex.Message);
                }

                _logger.LogWarning(ex, "AI writing assessment timed out.");

                ModelState.AddModelError(
                    string.Empty,
                    "The assessment took too long. Please try again.");
            }
            catch (Exception ex)
            {
                if (usageLogId.HasValue)
                {
                    await _usageTracking.CompleteAsync(
                        usageLogId.Value,
                        "Failed",
                        0,
                        ex.Message);
                }

                _logger.LogError(ex, "AI student writing assessment failed.");

                ModelState.AddModelError(
                    string.Empty,
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

            int? usageLogId = null;
            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    usageLogId = await _usageTracking.StartAsync(
                        userId,
                        "TeacherWritingAssessment",
                        1,
                        cancellationToken);
                }

                model.Assessment = await _writingService.AssessAsync(
                    model.TaskPrompt,
                    model.StudentWriting,
                    cancellationToken);

                if (usageLogId.HasValue)
                {
                    await _usageTracking.CompleteAsync(
                        usageLogId.Value,
                        "Succeeded",
                        0);

                    usageLogId = null;
                }

                return View("Results", model);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                if (usageLogId.HasValue)
                {
                    await _usageTracking.CompleteAsync(
                        usageLogId.Value,
                        "Cancelled",
                        0,
                        "The request was cancelled.");
                }

                return new EmptyResult();
            }
            catch (OperationCanceledException ex)
            {
                if (usageLogId.HasValue)
                {
                    await _usageTracking.CompleteAsync(
                        usageLogId.Value,
                        "Failed",
                        0,
                        ex.Message);
                }

                _logger.LogWarning(ex, "AI writing assessment timed out.");

                ModelState.AddModelError(
                    string.Empty,
                    "The assessment took too long. Please try again.");
            }
            catch (Exception ex)
            {
                if (usageLogId.HasValue)
                {
                    await _usageTracking.CompleteAsync(
                        usageLogId.Value,
                        "Failed",
                        0,
                        ex.Message);
                }

                _logger.LogError(ex, "AI teacher writing assessment failed.");

                ModelState.AddModelError(
                    string.Empty,
                    "The AI assessment failed. Check that Ollama is running and try again.");
            }

            return View(model);
        }

    }
}