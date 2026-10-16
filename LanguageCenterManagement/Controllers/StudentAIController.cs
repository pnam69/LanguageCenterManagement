
using System.Text.Json;
using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.Services.Ai;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentAIController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QuestionGenerationService _generationService;
        private readonly AiAnswerReviewService _answerReviewService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<StudentAIController> _logger;

        public StudentAIController(
            ApplicationDbContext context,
            QuestionGenerationService generationService,
            AiAnswerReviewService answerReviewService,
            IMemoryCache cache,
            ILogger<StudentAIController> logger)
        {
            _context = context;
            _generationService = generationService;
            _answerReviewService = answerReviewService;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Practice()
        {
            var model = new StudentPracticeViewModel();
            await LoadPassagesAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            StudentPracticeViewModel model,
            CancellationToken cancellationToken)
        {
            await LoadPassagesAsync(model);

            var validSkills = new[]
            {
                "Reading", "Listening", "Speaking", "Writing"
            };

            var validDifficulties = new[]
            {
                "Easy", "Medium", "Hard"
            };

            if (!validSkills.Contains(model.Skill))
            {
                ModelState.AddModelError(nameof(model.Skill), "Select a valid skill.");
            }

            if (!validDifficulties.Contains(model.Difficulty))
            {
                ModelState.AddModelError(
                    nameof(model.Difficulty),
                    "Select a valid difficulty.");
            }

            if (model.Count < 1 || model.Count > 10)
            {
                ModelState.AddModelError(
                    nameof(model.Count),
                    "Choose between 1 and 10 questions.");
            }

            ReadingContent? passage = null;

            if (model.ReadingContentId.HasValue)
            {
                if (model.Skill != "Reading")
                {
                    ModelState.AddModelError(
                        nameof(model.ReadingContentId),
                        "A reading passage can only be used for Reading practice.");
                }
                else
                {
                    passage = await _context.ReadingContents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            p => p.ReadingContentId == model.ReadingContentId.Value,
                            cancellationToken);

                    if (passage == null)
                    {
                        ModelState.AddModelError(
                            nameof(model.ReadingContentId),
                            "The selected passage could not be found.");
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(model.Topic))
            {
                ModelState.AddModelError(
                    nameof(model.Topic),
                    "Enter a topic or select a reading passage.");
            }

            if (!ModelState.IsValid)
            {
                return View("Practice", model);
            }

            try
            {
                var request = new QuestionGenerationRequest
                {
                    Topic = passage != null
                        ? "Reading comprehension"
                        : model.Topic.Trim(),
                    Skill = model.Skill,
                    Difficulty = model.Difficulty,
                    QuestionType = "MultipleChoice",
                    Count = model.Count,
                    ReadingContentId = passage?.ReadingContentId,
                    PassageText = passage?.Passage
                };

                var generatedQuestions = await _generationService.GenerateAsync(
                    request,
                    cancellationToken);

                if (generatedQuestions == null ||
                    generatedQuestions.Count != model.Count)
                {
                    ModelState.AddModelError(
                        "",
                        "The AI did not generate the requested number of questions. Please try again.");

                    return View("Practice", model);
                }

                var quiz = new CachedPracticeQuiz
                {
                    Skill = model.Skill,
                    Difficulty = model.Difficulty,
                    Passage = passage?.Passage,
                    Questions = generatedQuestions.Select(q =>
                    {
                        var answers = q.Answers
                            .Select(a => a.AnswerText)
                            .ToList();

                        var correctIndex = q.Answers.FindIndex(a => a.IsCorrect);

                        if (correctIndex < 0)
                        {
                            correctIndex = 0;
                        }

                        return new CachedPracticeQuestion
                        {
                            QuestionText = q.QuestionText,
                            Options = answers,
                            CorrectAnswerIndex = correctIndex
                        };
                    }).ToList()
                };

                var quizId = Guid.NewGuid();

                _cache.Set(
                    GetCacheKey(quizId),
                    quiz,
                    TimeSpan.FromMinutes(30));

                var quizViewModel = new StudentPracticeQuizViewModel
                {
                    QuizId = quizId,
                    Skill = quiz.Skill,
                    Difficulty = quiz.Difficulty,
                    Passage = quiz.Passage,
                    Questions = quiz.Questions.Select(q =>
                        new StudentPracticeQuestionViewModel
                        {
                            QuestionText = q.QuestionText,
                            Options = q.Options
                        }).ToList()
                };

                return View("Quiz", quizViewModel);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is InvalidOperationException ||
                ex is JsonException)
            {
                _logger.LogError(ex, "Failed to generate a student practice quiz.");

                ModelState.AddModelError(
                    "",
                    "Could not generate the quiz. Check that Ollama is running and try again.");

                return View("Practice", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            StudentPracticeSubmitViewModel model,
            CancellationToken cancellationToken)
        {
            if (!_cache.TryGetValue(
                    GetCacheKey(model.QuizId),
                    out CachedPracticeQuiz? quiz) ||
                quiz == null)
            {
                TempData["Error"] =
                    "This practice quiz has expired. Please generate a new one.";

                return RedirectToAction(nameof(Practice));
            }

            var result = new StudentPracticeResultViewModel
            {
                TotalQuestions = quiz.Questions.Count,
                Passage = quiz.Passage
            };

            for (var i = 0; i < quiz.Questions.Count; i++)
            {
                var question = quiz.Questions[i];

                int? selectedIndex =
                    i < model.Answers.Count ? model.Answers[i] : null;

                if (selectedIndex.HasValue &&
                    (selectedIndex.Value < 0 ||
                     selectedIndex.Value >= question.Options.Count))
                {
                    selectedIndex = null;
                }

                var isCorrect =
                    selectedIndex == question.CorrectAnswerIndex;

                if (isCorrect)
                {
                    result.Score++;
                }

                var resultQuestion = new StudentPracticeResultQuestionViewModel
                {
                    QuestionText = question.QuestionText,
                    Options = question.Options,
                    SelectedAnswerIndex = selectedIndex,
                    CorrectAnswerIndex = question.CorrectAnswerIndex,
                    IsCorrect = isCorrect
                };

                if (!isCorrect)
                {
                    var studentAnswer = selectedIndex.HasValue
                        ? question.Options[selectedIndex.Value]
                        : "No answer selected";

                    var correctAnswer =
                        question.Options[question.CorrectAnswerIndex];

                    string? explanation = null;

                    try
                    {
                        using var cts = new CancellationTokenSource(
                            TimeSpan.FromSeconds(15));

                        explanation = await _answerReviewService.ExplainAsync(
                            question.QuestionText,
                            studentAnswer,
                            correctAnswer,
                            question.Options,
                            cts.Token);

                        if (string.IsNullOrWhiteSpace(explanation))
                        {
                            explanation = $"The correct answer is: {correctAnswer}.";
                        }
                    }
                    catch (Exception ex) when (
                        ex is OperationCanceledException ||
                        ex is HttpRequestException ||
                        ex is InvalidOperationException ||
                        ex is System.Net.Sockets.SocketException ||
                        ex is System.Text.Json.JsonException)
                    {
                        _logger.LogWarning(ex,
                            "Could not generate an AI explanation for a practice answer.");

                        explanation =
                            $"The correct answer is: {correctAnswer}. The AI explanation is temporarily unavailable.";
                    }

                    resultQuestion.Explanation = explanation;

                    //string? explanation = null;

                    //try
                    //{
                    //    using var cts = new CancellationTokenSource(
                    //        TimeSpan.FromSeconds(10));

                    //    explanation = await _answerReviewService.ExplainAsync(
                    //        question.QuestionText,
                    //        studentAnswer,
                    //        correctAnswer,
                    //        question.Options,
                    //        cts.Token);
                    //}
                    //catch (OperationCanceledException)
                    //{
                    //    explanation = "AI explanation timed out. Please try again later.";
                    //}
                    //catch (HttpRequestException ex)
                    //{
                    //    _logger.LogWarning(ex, "Ollama was unavailable during answer review.");
                    //    explanation = "AI explanation is temporarily unavailable.";
                    //}
                    //catch (System.Net.Sockets.SocketException ex)
                    //{
                    //    _logger.LogWarning(ex, "Connection to Ollama failed during answer review.");
                    //    explanation = "AI explanation is temporarily unavailable.";
                    //}
                    //try
                    //{
                    //    resultQuestion.Explanation =
                    //        await _answerReviewService.ExplainAsync(
                    //            question.QuestionText,
                    //            studentAnswer,
                    //            correctAnswer,
                    //            question.Options,
                    //            cancellationToken);
                    //}
                    //catch (Exception ex) when (
                    //    ex is HttpRequestException ||
                    //    ex is InvalidOperationException ||
                    //    ex is JsonException)
                    //{
                    //    _logger.LogWarning(
                    //        ex,
                    //        "Could not generate an explanation for a practice answer.");

                    //    resultQuestion.Explanation =
                    //        "AI explanation is temporarily unavailable. Review the correct answer above.";
                    //}
                }

                result.Questions.Add(resultQuestion);
            }

            _cache.Remove(GetCacheKey(model.QuizId));

            return View("Results", result);
        }

        private async Task LoadPassagesAsync(StudentPracticeViewModel model)
        {
            var passages = await _context.ReadingContents
                .AsNoTracking()
                .OrderBy(p => p.ReadingContentId)
                .Select(p => new
                {
                    p.ReadingContentId,
                    p.Passage
                })
                .ToListAsync();

            model.Passages = passages.Select(p =>
            {
                var preview = (p.Passage ?? string.Empty)
                    .Replace("\r", " ")
                    .Replace("\n", " ");

                if (preview.Length > 85)
                {
                    preview = preview[..85] + "...";
                }

                return new ReadingPassageOptionViewModel
                {
                    ReadingContentId = p.ReadingContentId,
                    DisplayName = $"Passage #{p.ReadingContentId}: {preview}"
                };
            }).ToList();
        }

        private static string GetCacheKey(Guid quizId)
        {
            return $"StudentPracticeQuiz_{quizId}";
        }

        private sealed class CachedPracticeQuiz
        {
            public string Skill { get; set; } = string.Empty;
            public string Difficulty { get; set; } = string.Empty;
            public string? Passage { get; set; }
            public List<CachedPracticeQuestion> Questions { get; set; } = new();
        }

        private sealed class CachedPracticeQuestion
        {
            public string QuestionText { get; set; } = string.Empty;
            public List<string> Options { get; set; } = new();
            public int CorrectAnswerIndex { get; set; }
        }
    }
}
