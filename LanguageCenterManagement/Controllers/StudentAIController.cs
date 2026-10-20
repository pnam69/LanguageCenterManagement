using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.Services.Ai;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentAIController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QuestionGenerationService _generationService;
        private readonly AiAnswerReviewService _answerReviewService;
        private readonly IMemoryCache _cache;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ILogger<StudentAIController> _logger;

        public StudentAIController(
            ApplicationDbContext context,
            QuestionGenerationService generationService,
            AiAnswerReviewService answerReviewService,
            IMemoryCache cache,
            UserManager<ApplicationUser> userManager,
            ILogger<StudentAIController> logger)
        {
            _context = context;
            _generationService = generationService;
            _answerReviewService = answerReviewService;
            _cache = cache;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Practice(int? sourceExamResultId)
        {
            var model = new StudentPracticeViewModel
            {
                SourceExamResultId = sourceExamResultId,
                Difficulty = "Medium",
                Count = 5
            };

            if (sourceExamResultId.HasValue)
            {
                model.Topic =
                    $"Practise mistakes from exam result #{sourceExamResultId.Value}";
            }

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
            string? examMistakeContext = null;

            if (model.SourceExamResultId.HasValue)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user?.StudentId == null)
                    return Forbid();

                _logger.LogInformation(
                    "Practice generation: loading exam result {ResultId}",
                    model.SourceExamResultId);

                var result = await _context.ExamResults
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(r => r.Exam)
                    .Include(r => r.ExamAnswers)
                        .ThenInclude(a => a.Question)
                            .ThenInclude(q => q!.Answers)
                    .FirstOrDefaultAsync(
                        r => r.ExamResultId == model.SourceExamResultId.Value
                             && r.StudentId == user.StudentId.Value
                             && (r.Status == "Completed"
                                || r.Status == "Passed"
                                || r.Status == "Failed"),
                        cancellationToken);

                _logger.LogInformation(
                    "Practice generation: exam result loaded. Found result: {Found}",
                    result != null);
                if (result == null)
                    return NotFound();


                var mistakes = result.ExamAnswers
                    .Where(a =>
                        a.Question != null &&
                        a.IsCorrect == false &&
                        (a.Question.QuestionType == "MultipleChoice" ||
                         a.Question.QuestionType == "TrueFalse" ||
                         a.Question.QuestionType == "FillInTheBlank"))
                    .Take(20)
                    .ToList();

                _logger.LogInformation(
                    "Practice generation: result {ResultId} has {AnswerCount} saved answers and {MistakeCount} eligible mistakes.",
                    result.ExamResultId,
                    result.ExamAnswers.Count,
                    mistakes.Count);

                if (mistakes.Count == 0)
                {
                    var examQuestions = await _context.ExamQuestions
                        .AsNoTracking()
                        .Where(eq => eq.ExamId == result.ExamId)
                        .Select(eq => new
                        {
                            eq.Question!.Skill,
                            eq.Question.QuestionText,
                            eq.Question.QuestionType,
                            CorrectAnswers = eq.Question.Answers
                                .Where(a => a.IsCorrect)
                                .Select(a => a.AnswerText)
                                .ToList()
                        })
                        .Take(20)
                        .ToListAsync(cancellationToken);

                    if (examQuestions.Count == 0)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "This exam has no questions available for practice.");

                        return View("Practice", model);
                    }

                    var fallbackLines = new List<string>
                        {
                            $"Original exam: {result.Exam?.ExamName}",
                            "No saved individual answers were available.",
                            "Use these original exam questions to identify the topics and concepts for NEW practice questions.",
                            "Do not copy the original questions verbatim."
                        };

                    foreach (var question in examQuestions)
                    {
                        fallbackLines.Add($"Skill: {question.Skill}");
                        fallbackLines.Add($"Question type: {question.QuestionType}");
                        fallbackLines.Add($"Question: {question.QuestionText}");
                        fallbackLines.Add(
                            $"Configured correct answers: {string.Join(" / ", question.CorrectAnswers)}");
                        fallbackLines.Add("");
                    }

                    examMistakeContext = string.Join(
                        Environment.NewLine,
                        fallbackLines);
                }
                else
                {
                    var fallbackLines = new List<string>
                        {
                            $"Original exam: {result.Exam?.ExamName}",
                            $"Original score: {result.Score} / {result.Exam?.MaxScore}",
                            "Generate NEW questions that practise the concepts the student got wrong.",
                            "Do not copy the original questions verbatim."
                        };

                    foreach (var answer in mistakes)
                    {
                        var question = answer.Question!;

                        var selectedAnswer = question.Answers?
                            .FirstOrDefault(a => a.AnswerId == answer.AnswerId)
                            ?.AnswerText;

                        var correctAnswers = question.Answers?
                            .Where(a => a.IsCorrect)
                            .Select(a => a.AnswerText)
                            .ToList() ?? new List<string>();

                        fallbackLines.Add($"Skill: {question.Skill}");
                        fallbackLines.Add($"Question: {question.QuestionText}");
                        fallbackLines.Add(
                            $"Student answer: {answer.TextAnswer ?? selectedAnswer ?? "(No answer recorded)"}");
                        fallbackLines.Add(
                            $"Correct answer: {string.Join(" / ", correctAnswers)}");
                        fallbackLines.Add("");
                    }

                    examMistakeContext = string.Join(
                        Environment.NewLine,
                        fallbackLines);
                }

                if (examMistakeContext.Length > 10000)
                {
                    examMistakeContext = examMistakeContext[..10000];
                }

                var lines = new List<string>
                    {
                        $"Original exam: {result.Exam?.ExamName}",
                        $"Original score: {result.Score} / {result.Exam?.MaxScore}",
                        "Generate NEW questions that practise the concepts the student got wrong.",
                        "Do not copy the original questions verbatim."
                    };

                foreach (var answer in mistakes)
                {
                    var question = answer.Question!;

                    var selectedAnswer = question.Answers?
                        .FirstOrDefault(a => a.AnswerId == answer.AnswerId)
                        ?.AnswerText;

                    var correctAnswers = question.Answers?
                        .Where(a => a.IsCorrect)
                        .Select(a => a.AnswerText)
                        .ToList() ?? new List<string>();

                    var studentAnswer = !string.IsNullOrWhiteSpace(answer.TextAnswer)
                        ? answer.TextAnswer
                        : selectedAnswer ?? "(No answer recorded)";

                    lines.Add($"Skill: {question.Skill}");
                    lines.Add($"Question: {question.QuestionText}");
                    lines.Add($"Student answer: {studentAnswer}");
                    lines.Add(
                        $"Correct answer: {(correctAnswers.Count > 0 ? string.Join(" / ", correctAnswers) : "(No correct answer configured)")}");
                    lines.Add("");
                }

                examMistakeContext = string.Join(
                    Environment.NewLine,
                    lines);

                if (examMistakeContext.Length > 10000)
                {
                    examMistakeContext = examMistakeContext[..10000];
                }

                // Use the skill from the mistakes instead of the form's default.
                var mostCommonSkill = mistakes
                    .GroupBy(a => a.Question!.Skill)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(mostCommonSkill) &&
                    new[] { "Reading", "Listening", "Speaking", "Writing" }
                        .Contains(mostCommonSkill))
                {
                    model.Skill = mostCommonSkill;
                }

            }

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
            else if (string.IsNullOrWhiteSpace(model.Topic) &&
                string.IsNullOrWhiteSpace(examMistakeContext))
            {
                ModelState.AddModelError(
                    nameof(model.Topic),
                    "Enter a topic, select a reading passage, or practise mistakes from an exam.");
            }

            if (!ModelState.IsValid)
            {
                return View("Practice", model);
            }

            try
            {
                string? uploadedText = null;

                if (model.MaterialFile is { Length: > 0 } file)
                {
                    if (!string.Equals(
                            Path.GetExtension(file.FileName),
                            ".txt",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(
                            nameof(model.MaterialFile),
                            "Only .txt files are supported.");
                    }
                    else if (file.Length > 100 * 1024)
                    {
                        ModelState.AddModelError(
                            nameof(model.MaterialFile),
                            "The uploaded file must be 100 KB or smaller.");
                    }
                    else
                    {
                        using var reader = new StreamReader(file.OpenReadStream());
                        uploadedText = await reader.ReadToEndAsync(cancellationToken);

                        if (string.IsNullOrWhiteSpace(uploadedText))
                        {
                            ModelState.AddModelError(
                                nameof(model.MaterialFile),
                                "The uploaded file contains no readable text.");
                        }
                        else if (uploadedText.Length > 15000)
                        {
                            uploadedText = uploadedText[..15000];
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(uploadedText) &&
                    model.Skill != "Reading")
                {
                    ModelState.AddModelError(
                        nameof(model.Skill),
                        "Uploaded materials can currently be used for Reading practice only.");
                }

                if (!ModelState.IsValid)
                {
                    await LoadPassagesAsync(model);
                    return View("Practice", model);
                }

                var request = new QuestionGenerationRequest
                {
                    Topic = examMistakeContext != null
                        ? $"Generate new {model.Skill} practice questions at {model.Difficulty} difficulty based on these exam mistakes:\n{examMistakeContext}"
                        : passage != null
                            ? "Reading comprehension"
                            : model.Topic?.Trim() ?? string.Empty,

                    Skill = model.Skill,
                    Difficulty = model.Difficulty,
                    QuestionType = "MultipleChoice",
                    Count = model.Count,

                    ReadingContentId = uploadedText == null
                        ? passage?.ReadingContentId
                        : null,

                    PassageText = uploadedText ?? passage?.Passage
                };

                _logger.LogInformation(
                    "Practice generation: calling AI service. Skill={Skill}, Count={Count}",
                    model.Skill,
                    model.Count);

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
                    Passage = uploadedText ?? passage?.Passage,
                    Questions = generatedQuestions.Select(q =>
                    {
                        var answers = q.Answers
                            .Select(a => a.AnswerText)
                            .ToList();

                        var correctIndex = q.Answers.FindIndex(a => a.IsCorrect);

                        if (correctIndex < 0 ||
                            q.Answers.Count(a => a.IsCorrect) != 1)
                        {
                            throw new InvalidOperationException(
                                "A generated question must have exactly one correct answer.");
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
                    selectedIndex.HasValue &&
                    selectedIndex.Value == question.CorrectAnswerIndex;

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
