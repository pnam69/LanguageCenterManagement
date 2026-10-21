using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.Services.Ai;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class AiQuestionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QuestionGenerationService _generationService;
        private readonly AiQuotaService _quotaService;
        private readonly ILogger<AiQuestionsController> _logger;

        public AiQuestionsController(
            ApplicationDbContext context,
            QuestionGenerationService generationService,
            AiQuotaService quotaService,
            ILogger<AiQuestionsController> logger)
        {
            _context = context;
            _generationService = generationService;
            _quotaService = quotaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? readingContentId)
        {
            var model = new AiQuestionGenerationViewModel
            {
                ReadingContentId = readingContentId
            };

            if (readingContentId.HasValue)
            {
                var passage = await _context.ReadingContents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.ReadingContentId == readingContentId.Value);

                if (passage == null)
                    return NotFound();

                model.Skill = "Reading";
                model.Topic = "Reading comprehension";
                ViewBag.SelectedPassage = passage.Passage;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            AiQuestionGenerationViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ValidateOptions(model))
                return View("Index", model);
            int? usageLogId = null;
            try
            {
                ReadingContent? selectedPassage = null;

                if (model.ReadingContentId.HasValue)
                {
                    if (model.Skill != "Reading")
                    {
                        ModelState.AddModelError(
                            nameof(model.Skill),
                            "A reading passage can only be used with Reading questions.");

                        return View("Index", model);
                    }

                    selectedPassage = await _context.ReadingContents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            r => r.ReadingContentId == model.ReadingContentId.Value,
                            cancellationToken);

                    if (selectedPassage == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "The selected reading passage could not be found.");

                        return View("Index", model);
                    }

                    ViewBag.SelectedPassage = selectedPassage.Passage;
                }

                var request = new QuestionGenerationRequest
                {
                    Topic = model.Topic.Trim(),
                    Skill = model.Skill,
                    Difficulty = model.Difficulty,
                    QuestionType = model.QuestionType,
                    Count = model.Count,
                    ReadingContentId = model.ReadingContentId,
                    PassageText = selectedPassage?.Passage
                };

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                    return Forbid();

                var quota = await _quotaService.ReserveTeacherQuotaAsync(
                    userId,
                    request.Count,
                    cancellationToken);

                if (!quota.Allowed)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Daily AI quota exceeded. You have used {quota.Used} of " +
                        $"{quota.Limit} questions today. Try again tomorrow.");

                    ViewBag.SelectedPassage = selectedPassage?.Passage;
                    return View("Index", model);
                }

                usageLogId = quota.LogId;

                var generated = await _generationService.GenerateAsync(
                    request,
                    cancellationToken);

                if (usageLogId.HasValue)
                {
                    await _quotaService.CompleteAsync(
                        usageLogId.Value,
                        "Succeeded",
                        generated.Count,
                        cancellationToken: cancellationToken);
                }

                model.Questions = generated.Select(q => new AiGeneratedQuestionViewModel
                {
                    QuestionText = q.QuestionText,
                    Answers = q.Answers.Select(a =>
                        new AiGeneratedAnswerViewModel
                        {
                            AnswerText = a.AnswerText,
                            IsCorrect = a.IsCorrect
                        }).ToList()
                }).ToList();

                ViewBag.SelectedPassage = selectedPassage?.Passage;
                return View("Preview", model);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                if (usageLogId.HasValue)
                {
                    await _quotaService.CompleteAsync(
                        usageLogId.Value,
                        "Cancelled",
                        0,
                        "Request was cancelled.",
                        CancellationToken.None);
                }

                return new EmptyResult();
            }

            catch (OperationCanceledException ex)
            {
                if (usageLogId.HasValue)
                {
                    await _quotaService.CompleteAsync(
                        usageLogId.Value,
                        "Failed",
                        0,
                        "Question generation failed.",
                        CancellationToken.None);
                }
                _logger.LogWarning(ex, "AI question generation timed out.");

                ModelState.AddModelError(
                    string.Empty,
                    "Question generation took too long. Try again with fewer questions.");
            }

            catch (HttpRequestException ex)
            {
                if (usageLogId.HasValue)
                {
                    await _quotaService.CompleteAsync(
                        usageLogId.Value,
                        "Failed",
                        0,
                        "Question generation failed.",
                        CancellationToken.None);
                }
                _logger.LogError(ex, "Ollama question generation failed.");
                ModelState.AddModelError(
                    "",
                    "Cannot connect to Ollama. Check that Ollama is running " +
                    "and that the qwen2.5:3b model is available.");
            }
            catch (InvalidOperationException ex)
            {
                if (usageLogId.HasValue)
                {
                    await _quotaService.CompleteAsync(
                        usageLogId.Value,
                        "Failed",
                        0,
                        "Question generation failed.",
                        CancellationToken.None);
                }
                _logger.LogWarning(ex, "AI generated invalid questions.");

                ModelState.AddModelError(
                    "",
                    "The AI could not generate valid questions after several attempts. " +
                    "Try again or reduce the number of questions.");
            }
            catch (System.Text.Json.JsonException ex)
            {
                if (usageLogId.HasValue)
                {
                    await _quotaService.CompleteAsync(
                        usageLogId.Value,
                        "Failed",
                        0,
                        "Question generation failed.",
                        CancellationToken.None);
                }
                _logger.LogError(ex, "Could not parse AI response.");
                ModelState.AddModelError(
                    "",
                    "The AI response could not be processed. Please try again.");
            }

            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(AiQuestionGenerationViewModel model)
        {
            ReadingContent? selectedPassage = null;

            if (model.ReadingContentId.HasValue)
            {
                selectedPassage = await _context.ReadingContents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.ReadingContentId == model.ReadingContentId.Value);

                if (selectedPassage == null)
                {
                    ModelState.AddModelError(
                        "",
                        "The selected reading passage no longer exists.");

                    return RedirectToAction("Index", "ReadingContents");
                }

                if (model.Skill != "Reading")
                {
                    ModelState.AddModelError(
                        nameof(model.Skill),
                        "A passage can only be linked to Reading questions.");

                    ViewBag.SelectedPassage = selectedPassage.Passage;
                    return View("Preview", model);
                }
            }

            ViewBag.SelectedPassage = selectedPassage?.Passage;
            if (!ValidateOptions(model))
                return View("Preview", model);

            if (model.Questions == null ||
                model.Questions.Count != model.Count)
            {
                ModelState.AddModelError(
                    "",
                    "The question list is invalid. Generate the questions again.");

                return View("Preview", model);
            }

            foreach (var question in model.Questions)
            {
                int expectedAnswers = model.QuestionType switch
                {
                    "MultipleChoice" => 4,
                    "TrueFalse" => 2,
                    "FillInTheBlank" => 1,
                    _ => 0
                };

                if (string.IsNullOrWhiteSpace(question.QuestionText) ||
                    question.QuestionText.Length > 2000 ||
                    question.Answers == null ||
                    expectedAnswers == 0 ||
                    question.Answers.Count != expectedAnswers ||
                    question.Answers.Any(a =>
                        string.IsNullOrWhiteSpace(a.AnswerText) ||
                        a.AnswerText.Length > 1000) ||
                    question.Answers
                        .Select(a => a.AnswerText.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count() != expectedAnswers ||
                    question.Answers.Count(a => a.IsCorrect) != 1)
                {
                    ModelState.AddModelError(
                        "",
                        $"Each {model.QuestionType} question must have {expectedAnswers} distinct answer(s) and exactly one correct answer.");

                    return View("Preview", model);
                }

                if (model.QuestionType == "TrueFalse" &&
                    !question.Answers
                        .Select(a => a.AnswerText.Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase)
                        .SetEquals(new[] { "True", "False" }))
                {
                    ModelState.AddModelError(
                        "",
                        "True/False questions must have exactly True and False as their answers.");

                    return View("Preview", model);
                }
            }

            if (model.ReadingContentId.HasValue)
            {
                if (model.Skill != "Reading")
                {
                    ModelState.AddModelError(
                        nameof(model.Skill),
                        "A passage can only be linked to Reading questions.");

                    return View("Preview", model);
                }

                bool passageExists = await _context.ReadingContents
                    .AnyAsync(
                        r => r.ReadingContentId == model.ReadingContentId.Value);

                if (!passageExists)
                {
                    ModelState.AddModelError(
                        "",
                        "The selected reading passage no longer exists.");

                    return View("Preview", model);
                }
            }

            foreach (var generated in model.Questions)
            {
                var question = new Question
                {
                    QuestionText = generated.QuestionText.Trim(),
                    QuestionType = model.QuestionType,
                    Skill = model.Skill,
                    Score = 1,
                    ReadingContentId = model.Skill == "Reading" ? model.ReadingContentId : null,
                    Answers = generated.Answers.Select(a => new Answer
                    {
                        AnswerText = a.AnswerText.Trim(),
                        IsCorrect = a.IsCorrect
                    }).ToList()
                };

                _context.Questions.Add(question);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{model.Questions.Count} questions saved successfully.";

            return RedirectToAction("Index", "Questions");
        }

        private bool ValidateOptions(
            AiQuestionGenerationViewModel model)
        {
            if (!ModelState.IsValid)
                return false;

            if (string.IsNullOrWhiteSpace(model.Topic) ||
                model.Topic.Length > 200)
            {
                ModelState.AddModelError(
                    nameof(model.Topic),
                    "Enter a topic of no more than 200 characters.");
            }

            if (!new[] { "Reading", "Listening", "Speaking", "Writing" }
                .Contains(model.Skill))
            {
                ModelState.AddModelError(
                    nameof(model.Skill),
                    "Select a valid skill.");
            }

            if (!new[] { "MultipleChoice", "TrueFalse", "FillInTheBlank" }
            .Contains(model.QuestionType))
            {
                ModelState.AddModelError(
                    nameof(model.QuestionType),
                    "Invalid question type.");
            }

            if (!new[] { "Easy", "Medium", "Hard" }
                .Contains(model.Difficulty))
            {
                ModelState.AddModelError(
                    nameof(model.Difficulty),
                    "Select a valid difficulty.");
            }

            if (model.Count < 1 || model.Count > 10)
            {
                ModelState.AddModelError(
                    nameof(model.Count),
                    "Generate between 1 and 10 questions.");
            }

            return ModelState.IsValid;
        }
    }
}
