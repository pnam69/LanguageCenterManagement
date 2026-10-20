using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LanguageCenterManagement.Services.Ai;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentExamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AiAnswerReviewService _aiAnswerReviewService;
        private readonly WritingIntegrityService _writingIntegrityService;
        private readonly ILogger<StudentExamsController> _logger;
        private readonly WritingAiAssessmentService _writingAiAssessmentService;
        private readonly AiExamReviewService _aiExamReviewService;

        public StudentExamsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AiAnswerReviewService aiAnswerReviewService,
            WritingIntegrityService writingIntegrityService,
            WritingAiAssessmentService writingAiAssessmentService,
            AiExamReviewService aiExamReviewService,
            ILogger<StudentExamsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _aiAnswerReviewService = aiAnswerReviewService;
            _writingIntegrityService = writingIntegrityService;
            _writingAiAssessmentService = writingAiAssessmentService;
            _aiExamReviewService = aiExamReviewService;
            _logger = logger;
        }

        // GET: StudentExams
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
            {
                return Forbid();
            }

            int studentId = user.StudentId.Value;

            var exams = await _context.Exams
                .Include(e => e.Class)
                .Include(e => e.ExamResults)
                .Where(e =>
                    e.Class != null &&
                    e.Class.Enrollments.Any(enrollment =>
                        enrollment.StudentId == studentId &&
                        enrollment.Status != "Cancelled"))
                .OrderBy(e => e.ExamDate)
                .ToListAsync();

            var model = exams.Select(e =>
            {
                var result = e.ExamResults
                    .FirstOrDefault(r => r.StudentId == studentId);

                return new StudentExamListViewModel
                {
                    ExamId = e.ExamId,
                    ExamName = e.ExamName,
                    ClassName = e.Class?.ClassName ?? "",
                    ExamType = e.ExamType,
                    ExamDate = e.ExamDate,
                    Duration = e.Duration,
                    MaxScore = e.MaxScore,
                    Status = e.Status,

                    HasSubmitted = result != null,
                    ExamResultId = result?.ExamResultId,
                    Score = result?.Score,
                    SubmittedAt = result?.SubmittedAt
                };
            }).ToList();

            return View(model);
        }

        // GET: StudentExams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
            {
                return Forbid();
            }

            int studentId = user.StudentId.Value;

            var exam = await _context.Exams
                .Include(e => e.Class)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q!.Answers)
                .Include(e => e.ExamResults)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
            }

            bool enrolled = exam.Class != null &&
                await _context.Enrollments.AnyAsync(e =>
                    e.ClassId == exam.ClassId &&
                    e.StudentId == studentId &&
                    e.Status != "Cancelled");

            if (!enrolled)
            {
                return Forbid();
            }

            bool submitted = exam.ExamResults
                .Any(r => r.StudentId == studentId);

            ViewBag.HasSubmitted = submitted;

            var model = new StudentExamViewModel
            {
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                ClassName = exam.Class?.ClassName ?? "",
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                Duration = exam.Duration,
                MaxScore = exam.MaxScore,
                Description = exam.Description,

                Questions = exam.ExamQuestions
                    .OrderBy(eq => eq.QuestionOrder)
                    .Select(eq => new StudentExamQuestionViewModel
                    {
                        ExamQuestionId = eq.ExamQuestionId,
                        QuestionId = eq.QuestionId,
                        QuestionOrder = eq.QuestionOrder,
                        QuestionText = eq.Question?.QuestionText ?? "",
                        QuestionType = eq.Question?.QuestionType ?? "",
                        Skill = eq.Question?.Skill ?? "",
                        Score = eq.Question?.Score ?? 1,
                        Answers = eq.Question?.Answers
                            .Select(a => new StudentExamAnswerViewModel
                            {
                                AnswerId = a.AnswerId,
                                AnswerText = a.AnswerText
                            })
                            .ToList() ?? new()
                    })
                    .ToList()
            };

            return View(model);
        }

        // GET: StudentExams/Take/5
        public async Task<IActionResult> Take(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
            {
                return Forbid();
            }

            int studentId = user.StudentId.Value;

            var exam = await _context.Exams
                .Include(e => e.Class)
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q!.Answers)
                .Include(e => e.ExamResults)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
            }

            bool enrolled = exam.Class != null &&
                await _context.Enrollments.AnyAsync(e =>
                    e.ClassId == exam.ClassId &&
                    e.StudentId == studentId &&
                    e.Status != "Cancelled");

            if (!enrolled)
            {
                return Forbid();
            }

            if (exam.Status != "Published")
            {
                TempData["ErrorMessage"] =
                    "This exam is not currently available.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (exam.ExamDate > DateTime.Now)
            {
                TempData["ErrorMessage"] =
                    "This exam has not started yet.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (exam.ExamResults.Any(r => r.StudentId == studentId))
            {
                TempData["ErrorMessage"] =
                    "You have already submitted this exam.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var questionIds = exam.ExamQuestions
                .Select(eq => eq.QuestionId)
                .ToList();

            // Load Reading content
            var readingContents = await _context.ReadingContents
                .Where(r => r.Questions.Any(q => questionIds.Contains(q.QuestionId)))
                .Include(r => r.Questions)
                .ToListAsync();

            // Load Speaking content
            var speakingContents = await _context.SpeakingContents
                .Where(s => questionIds.Contains(s.QuestionId))
                .ToListAsync();

            // Load Writing content
            var writingContents = await _context.WritingContents
                .Where(w => questionIds.Contains(w.QuestionId))
                .ToListAsync();

            // Load Listening content.
            // ListeningContent owns a collection of Questions,
            // so we load the content and its questions.
            var listeningContents = await _context.ListeningContents
                .Include(l => l.Questions)
                .Where(l => l.Questions.Any(q => questionIds.Contains(q.QuestionId)))
                .ToListAsync();

            var model = new StudentExamViewModel
            {
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                ClassName = exam.Class?.ClassName ?? "",
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                Duration = exam.Duration,
                MaxScore = exam.MaxScore,
                Description = exam.Description,

                Questions = exam.ExamQuestions
                    .OrderBy(eq => eq.QuestionOrder)
                    .Select(eq =>
                    {
                        var question = eq.Question;

                        var reading = readingContents
                            .FirstOrDefault(r => r.Questions.Any(q => q.QuestionId == eq.QuestionId));

                        var speaking = speakingContents
                            .FirstOrDefault(s => s.QuestionId == eq.QuestionId);

                        var writing = writingContents
                            .FirstOrDefault(w => w.QuestionId == eq.QuestionId);

                        var listening = listeningContents
                            .FirstOrDefault(l =>
                                l.Questions.Any(q =>
                                    q.QuestionId == eq.QuestionId));

                        return new StudentExamQuestionViewModel
                        {
                            ExamQuestionId = eq.ExamQuestionId,
                            QuestionId = eq.QuestionId,
                            QuestionOrder = eq.QuestionOrder,

                            QuestionText = question?.QuestionText ?? "",
                            QuestionType = question?.QuestionType ?? "",
                            Skill = question?.Skill ?? "",
                            Score = question?.Score ?? 1,

                            Answers = question?.Answers
                                .Select(a => new StudentExamAnswerViewModel
                                {
                                    AnswerId = a.AnswerId,
                                    AnswerText = a.AnswerText
                                })
                                .ToList() ?? new(),

                            // Reading
                            ReadingPassage = reading?.Passage,

                            // Listening
                            ListeningAudioUrl = listening?.AudioUrl,

                            // Speaking
                            SpeakingPrompt = speaking?.Prompt,
                            SpeakingPreparationTime =
                                speaking?.PreparationTime,
                            SpeakingResponseTime =
                                speaking?.ResponseTime,

                            // Writing
                            WritingPrompt = writing?.Prompt
                        };
                    })
                    .ToList()
            };

            if (!model.Questions.Any())
            {
                TempData["ErrorMessage"] =
                    "This exam does not have any questions.";

                return RedirectToAction(nameof(Details), new { id });
            }

            return View(model);
        }

        // POST: StudentExams/Take
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(int id, StudentExamSubmissionViewModel model)
        {
            if (!User.IsInRole("Student"))
                return Forbid();

            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            var studentId = user.StudentId.Value;

            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers)
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
                return NotFound();

            var enrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.StudentId == studentId &&
                    e.ClassId == exam.ClassId);

            if (!enrolled)
                return Forbid();

            if (exam.Status != "Published")
            {
                TempData["ErrorMessage"] = exam.Status == "Closed" ? "This exam is closed." : "This exam is not currently available.";

                return RedirectToAction(nameof(Index));
            }

            if (DateTime.Now < exam.ExamDate)
            {
                TempData["ErrorMessage"] = $"This exam will be available on {exam.ExamDate:dd/MM/yyyy HH:mm}.";

                return RedirectToAction(nameof(Index));
            }

            var existingResult = await _context.ExamResults
                .FirstOrDefaultAsync(r =>
                    r.ExamId == id &&
                    r.StudentId == studentId);

            if (existingResult != null)
            {
                return RedirectToAction(nameof(Result), new
                {
                    id = existingResult.ExamResultId
                });
            }

            var questions = exam.ExamQuestions
                .OrderBy(eq => eq.QuestionOrder)
                .ToList();

            if (!questions.Any())
            {
                TempData["ErrorMessage"] = "This exam has no questions.";
                return RedirectToAction(nameof(Index));
            }

            var submittedAnswers = model.Answers ?? new List<StudentExamAnswerSubmissionViewModel>();

            decimal rawScore = 0;

            var examResult = new ExamResult
            {
                ExamId = exam.ExamId,
                StudentId = studentId,
                Status = "Completed",
                SubmittedAt = DateTime.Now,
                Score = 0
            };

            _context.ExamResults.Add(examResult);

            foreach (var examQuestion in questions)
            {
                var question = examQuestion.Question;

                if (question == null)
                    continue;

                var submitted = submittedAnswers
                    .FirstOrDefault(a => a.QuestionId == question.QuestionId);

                var examAnswer = new ExamAnswer
                {
                    ExamResult = examResult,
                    QuestionId = question.QuestionId,
                    AnswerId = submitted?.AnswerId,
                    TextAnswer = submitted?.TextAnswer,
                    IsCorrect = null,
                    Score = 0
                };

                if (submitted?.AudioFile != null &&
                    submitted.AudioFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "exam-answers");

                    Directory.CreateDirectory(uploadsFolder);

                    var extension =
                        Path.GetExtension(submitted.AudioFile.FileName);

                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        extension = ".webm";
                    }

                    var fileName =
                        Guid.NewGuid().ToString("N") +
                        extension;

                    var filePath =
                        Path.Combine(
                            uploadsFolder,
                            fileName);

                    using (var stream = new FileStream(
                        filePath,
                        FileMode.Create))
                    {
                        await submitted.AudioFile.CopyToAsync(stream);
                    }

                    examAnswer.AudioAnswerUrl =
                        "/uploads/exam-answers/" + fileName;
                }

                /*
                 * Multiple choice questions can be automatically graded.
                 */

                if (question.QuestionType == "FillInTheBlank")
                {
                    var correctAnswer = question.Answers
                        .FirstOrDefault(a => a.IsCorrect);

                    var submittedText = submitted?.TextAnswer?.Trim();
                    var correctText = correctAnswer?.AnswerText?.Trim();

                    bool isCorrect =
                        !string.IsNullOrWhiteSpace(submittedText) &&
                        !string.IsNullOrWhiteSpace(correctText) &&
                        string.Equals(
                            submittedText,
                            correctText,
                            StringComparison.OrdinalIgnoreCase);

                    examAnswer.IsCorrect = isCorrect;

                    if (isCorrect)
                    {
                        examAnswer.Score = question.Score;
                        rawScore += question.Score;
                    }
                }
                else if (submitted?.AnswerId != null)
                {
                    var selectedAnswer = question.Answers
                        .FirstOrDefault(a => a.AnswerId == submitted.AnswerId);

                    if (selectedAnswer != null)
                    {
                        examAnswer.IsCorrect = selectedAnswer.IsCorrect;

                        if (selectedAnswer.IsCorrect)
                        {
                            examAnswer.Score = question.Score;
                            rawScore += question.Score;
                        }
                    }
                }
                /*
                 * Writing and speaking/text questions are stored,
                 * but not automatically marked correct.
                 *
                 * IsCorrect remains null and Score remains 0
                 * until a teacher grades them.
                 */

                _context.ExamAnswers.Add(examAnswer);
            }

            var totalPossibleScore = questions.Sum(q => q.Question?.Score ?? 0);

            if (totalPossibleScore > 0)
            {
                examResult.Score = Math.Round(
                    (rawScore / totalPossibleScore) * exam.MaxScore,
                    2);
            }
            else
            {
                examResult.Score = 0;
            }

            await _context.SaveChangesAsync();

            // Check submitted writing answers after the exam is safely saved.
            // Integrity-check failures must not discard a student's submission.

            var writingAnswerIds = await _context.ExamAnswers
                .Where(a =>
                    a.ExamResultId == examResult.ExamResultId &&
                    a.TextAnswer != null &&
                    a.TextAnswer.Trim() != "" &&
                    a.Question != null &&
                    a.Question.Skill == "Writing")
                .Select(a => a.ExamAnswerId)
                .ToListAsync();

            // Run both checks independently.
            // Neither failure should affect the saved exam submission.
            foreach (var answerId in writingAnswerIds)
            {
                try
                {
                    await _writingIntegrityService.CheckAnswerAsync(answerId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Writing similarity check failed for answer {AnswerId}",
                        answerId);
                }

                try
                {
                    await _writingAiAssessmentService.CheckAnswerAsync(answerId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Ollama writing assessment failed for answer {AnswerId}",
                        answerId);
                }
            }

            return RedirectToAction(nameof(Result), new
            {
                id = examResult.ExamResultId
            });
        }

        // GET: StudentExams/Result/5
        public async Task<IActionResult> Result(int id)
        {
            if (!User.IsInRole("Student"))
                return Forbid();

            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            var studentId = user.StudentId.Value;

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Class)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(r =>
                    r.ExamResultId == id &&
                    r.StudentId == studentId);

            if (result == null)
                return NotFound();

            var exam = result.Exam;

            if (exam == null)
                return NotFound();

            var examQuestions = await _context.ExamQuestions
                .Where(eq => eq.ExamId == exam.ExamId)
                .OrderBy(eq => eq.QuestionOrder)
                .ToListAsync();

            var model = new StudentExamResultViewModel
            {
                ExamResultId = result.ExamResultId,
                ExamId = result.ExamId,
                ExamName = exam.ExamName,
                ClassName = exam.Class?.ClassName ?? "-",
                Score = result.Score,
                MaxScore = exam.MaxScore,
                Status = result.Status,
                SubmittedAt = result.SubmittedAt
            };

            foreach (var examQuestion in examQuestions)
            {
                var question = await _context.Questions
                    .Include(q => q.Answers)
                    .FirstOrDefaultAsync(q => q.QuestionId == examQuestion.QuestionId);

                if (question == null)
                    continue;

                var studentAnswer = result.ExamAnswers
                    .FirstOrDefault(a => a.QuestionId == question.QuestionId);

                var item = new StudentExamResultAnswerViewModel
                {
                    QuestionId = question.QuestionId,
                    QuestionOrder = examQuestion.QuestionOrder,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    Skill = question.Skill,
                    QuestionScore = question.Score,
                    IsCorrect = studentAnswer?.IsCorrect,
                    Score = studentAnswer?.Score ?? 0,
                    AnswerOptions = question.Answers
                        .Select(a => a.AnswerText)
                        .ToList()
                };

                if (studentAnswer?.AnswerId != null)
                {
                    var selectedAnswer = question.Answers
                        .FirstOrDefault(a => a.AnswerId == studentAnswer.AnswerId);

                    item.StudentAnswer = selectedAnswer?.AnswerText;
                }
                else
                {
                    item.StudentAnswer = studentAnswer?.TextAnswer;
                }

                item.CorrectAnswer = question.Answers
                    .FirstOrDefault(a => a.IsCorrect)
                    ?.AnswerText;

                model.Answers.Add(item);
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ReviewEntireExam(
            int resultId,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            var examResult = await _context.ExamResults
                .AsNoTracking()
                .Where(r =>
                    r.ExamResultId == resultId &&
                    r.StudentId == user.StudentId.Value &&
                    (r.Status == "Completed"
                    || r.Status == "Passed"
                    || r.Status == "Failed"))
                .Include(r => r.Exam)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Answers)
                .AsSplitQuery()
                .FirstOrDefaultAsync(cancellationToken);

            if (examResult == null)
                return NotFound();

            var allAnswers = examResult.ExamAnswers
                .Where(a => a.Question != null)
                .ToList();

            var gradedAnswers = allAnswers
                .Where(a =>
                    a.IsCorrect.HasValue &&
                    a.Question!.Score > 0)
                .ToList();

            if (gradedAnswers.Count == 0)
            {
                return Ok(new AiExamReviewViewModel
                {
                    OverallReview =
                        "There are not enough objectively graded answers to produce a reliable exam review yet.",

                    Strengths = new List<string>(),

                    AreasToImprove = new List<string>(),

                    Recommendations = new List<string>
            {
                "Review your submitted answers and any feedback already available.",
                "Check this review again after your answers have been graded."
            }
                });
            }

            var questionInputs = gradedAnswers.Select(a =>
            {
                var question = a.Question!;

                var studentAnswer = question.Answers?
                    .FirstOrDefault(option => option.AnswerId == a.AnswerId)
                    ?.AnswerText;

                var correctAnswers = question.Answers?
                    .Where(option => option.IsCorrect)
                    .Select(option => option.AnswerText)
                    .ToList() ?? new List<string>();

                return new AiExamReviewQuestionInput
                {
                    Skill = question.Skill ?? "Unknown",
                    QuestionType = question.QuestionType ?? "Unknown",
                    QuestionText = question.QuestionText ?? string.Empty,
                    StudentAnswer = !string.IsNullOrWhiteSpace(a.TextAnswer)
                        ? a.TextAnswer
                        : studentAnswer ?? "(No answer recorded)",
                    CorrectAnswer = correctAnswers.Count > 0
                        ? string.Join(" / ", correctAnswers)
                        : "(Not available)",
                    IsCorrect = a.IsCorrect!.Value,
                    EarnedScore = a.Score,
                    PossibleScore = question.Score
                };
            }).ToList();

            try
            {
                var review = await _aiExamReviewService.GenerateAsync(
                    examResult.Score,
                    examResult.Exam?.MaxScore ?? 10,
                    allAnswers.Count - gradedAnswers.Count,
                    questionInputs,
                    cancellationToken);

                return Ok(review);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to generate full exam review for result {ResultId}.",
                    resultId);

                return StatusCode(503, new
                {
                    message = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              == "Development"
                        ? ex.Message
                        : "The AI exam review is temporarily unavailable. Please try again."
                });
            }
        }

        // GET: StudentExams/DownloadTeacherReview/5
        [HttpGet]
        public async Task<IActionResult> DownloadTeacherReview(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            var result = await _context.ExamResults
                .AsNoTracking()
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
                .Where(r =>
                    r.ExamResultId == id &&
                    r.StudentId == user.StudentId.Value)
                .FirstOrDefaultAsync();

            if (result?.Exam == null)
                return NotFound();

            var lines = new List<string>
    {
        "TEACHER EXAM REVIEW",
        $"Exam: {result.Exam.ExamName}",
        $"Class: {result.Exam.Class?.ClassName ?? "-"}",
        $"Submitted: {result.SubmittedAt:dd/MM/yyyy HH:mm}",
        $"Score: {result.Score} / {result.Exam.MaxScore}",
        "",
        "TEACHER FEEDBACK:",
        string.IsNullOrWhiteSpace(result.Note)
            ? "No teacher feedback has been recorded."
            : result.Note,
        "",
        "QUESTION RESULTS:"
    };

            foreach (var answer in result.ExamAnswers
                         .OrderBy(a => a.QuestionId))
            {
                lines.Add("");
                lines.Add($"Question: {answer.Question?.QuestionText ?? "-"}");
                lines.Add($"Score: {answer.Score}");

                lines.Add(answer.IsCorrect switch
                {
                    true => "Result: Correct",
                    false => "Result: Incorrect",
                    null => "Result: Awaiting manual grading"
                });
            }

            var content = string.Join(Environment.NewLine, lines);
            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(content))
                .ToArray();

            return File(
                bytes,
                "text/plain; charset=utf-8",
                $"TeacherReview_{result.Exam.ExamName}.txt");
        }

        // GET: StudentExams/DownloadAiReview/5
        [HttpGet]
        public async Task<IActionResult> DownloadAiReview(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            var result = await _context.ExamResults
                .AsNoTracking()
                .Include(r => r.Exam)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q!.Answers)
                .Where(r =>
                    r.ExamResultId == id &&
                    r.StudentId == user.StudentId.Value &&
                    r.Status == "Completed")
                .AsSplitQuery()
                .FirstOrDefaultAsync(cancellationToken);

            if (result?.Exam == null)
                return NotFound();

            var allAnswers = result.ExamAnswers
                .Where(a => a.Question != null)
                .ToList();

            var gradedAnswers = allAnswers
                .Where(a => a.IsCorrect.HasValue && a.Question!.Score > 0)
                .ToList();

            if (gradedAnswers.Count == 0)
                return BadRequest(
                    "There are not enough graded answers to generate an AI review.");

            var inputs = gradedAnswers.Select(a =>
            {
                var question = a.Question!;

                var selectedAnswer = question.Answers?
                    .FirstOrDefault(x => x.AnswerId == a.AnswerId)?.AnswerText;

                var correctAnswers = question.Answers?
                    .Where(x => x.IsCorrect)
                    .Select(x => x.AnswerText)
                    .ToList() ?? new List<string>();

                return new AiExamReviewQuestionInput
                {
                    Skill = question.Skill ?? "Unknown",
                    QuestionType = question.QuestionType ?? "Unknown",
                    QuestionText = question.QuestionText ?? "",
                    StudentAnswer = !string.IsNullOrWhiteSpace(a.TextAnswer)
                        ? a.TextAnswer
                        : selectedAnswer ?? "(No answer recorded)",
                    CorrectAnswer = correctAnswers.Count > 0
                        ? string.Join(" / ", correctAnswers)
                        : "(Not available)",
                    IsCorrect = a.IsCorrect!.Value,
                    EarnedScore = a.Score,
                    PossibleScore = question.Score
                };
            }).ToList();

            var review = await _aiExamReviewService.GenerateAsync(
                result.Score,
                result.Exam.MaxScore,
                allAnswers.Count - gradedAnswers.Count,
                inputs,
                cancellationToken);

            var lines = new List<string>
    {
        "AI EXAM REVIEW",
        $"Exam: {result.Exam.ExamName}",
        $"Score: {result.Score} / {result.Exam.MaxScore}",
        "",
        "OVERALL REVIEW",
        review.OverallReview,
        "",
        "STRENGTHS",
        string.Join(Environment.NewLine,
            review.Strengths.Select(x => "- " + x)),
        "",
        "AREAS TO IMPROVE",
        string.Join(Environment.NewLine,
            review.AreasToImprove.Select(x => "- " + x)),
        "",
        "RECOMMENDATIONS",
        string.Join(Environment.NewLine,
            review.Recommendations.Select(x => "- " + x))
    };

            var content = string.Join(Environment.NewLine, lines);
            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(content))
                .ToArray();

            return File(
                bytes,
                "text/plain; charset=utf-8",
                $"AIReview_{result.Exam.ExamId}_{result.ExamResultId}.txt");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogMonitoringEvent(
            int examId,
            string eventType,
            int? durationSeconds,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            // Accept only events reported by the exam monitoring script.
            if (eventType != "TabHidden" &&
                eventType != "WindowBlurred" &&
                eventType != "FocusRegained")
            {
                return BadRequest();
            }

            // Duration is recorded when the student returns to the exam.
            if (eventType == "FocusRegained")
            {
                if (!durationSeconds.HasValue ||
                    durationSeconds.Value < 0 ||
                    durationSeconds.Value > 86400)
                {
                    return BadRequest();
                }
            }
            else if (durationSeconds.HasValue)
            {
                return BadRequest();
            }

            var studentId = user.StudentId.Value;

            var exam = await _context.Exams
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.ExamId == examId,
                    cancellationToken);

            if (exam == null)
                return NotFound();

            if (exam.Status != "Published" ||
                exam.ExamDate > DateTime.Now)
            {
                return BadRequest();
            }

            var enrolled = await _context.Enrollments.AnyAsync(e =>
                e.StudentId == studentId &&
                e.ClassId == exam.ClassId &&
                e.Status != "Cancelled",
                cancellationToken);

            if (!enrolled)
                return Forbid();

            // Do not accept monitoring events after the student submits.
            var alreadySubmitted = await _context.ExamResults.AnyAsync(r =>
                r.ExamId == examId &&
                r.StudentId == studentId,
                cancellationToken);

            if (alreadySubmitted)
                return BadRequest();

            // Do not discard legitimate repeated tab switches.
            // The client already prevents duplicate blur/visibility events.
            _context.ExamMonitoringEvents.Add(new ExamMonitoringEvent
            {
                ExamId = examId,
                StudentId = studentId,
                EventType = eventType,
                DurationSeconds = durationSeconds,
                OccurredAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewAnswer(
            int resultId,
            int questionId,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            var result = await _context.ExamResults
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q!.Answers)
                .FirstOrDefaultAsync(r =>
                    r.ExamResultId == resultId &&
                    r.StudentId == user.StudentId.Value,
                    cancellationToken);

            if (result == null)
                return NotFound();

            if (result.Status != "Completed")
                return BadRequest(new { error = "This exam has not been completed." });

            var examAnswer = result.ExamAnswers
                .FirstOrDefault(a => a.QuestionId == questionId);

            var question = examAnswer?.Question;

            if (examAnswer == null || question == null)
                return NotFound();

            // AI review is for automatically graded objective questions only.
            var supportedTypes = new[]
            {
                "MultipleChoice",
                "TrueFalse",
                "FillInTheBlank"
            };

            if (!supportedTypes.Contains(
                question.QuestionType,
                StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    error = "AI review is not available for this question type."
                });
            }

            if (examAnswer.IsCorrect == true)
            {
                return BadRequest(new
                {
                    error = "AI review is only available for incorrect or unanswered questions."
                });
            }

            var correctAnswer = question.Answers
                .FirstOrDefault(a => a.IsCorrect)?.AnswerText;

            if (string.IsNullOrWhiteSpace(correctAnswer))
            {
                return BadRequest(new
                {
                    error = "A correct answer has not been configured for this question."
                });
            }

            var studentAnswer = examAnswer.AnswerId.HasValue
                ? question.Answers
                    .FirstOrDefault(a => a.AnswerId == examAnswer.AnswerId.Value)
                    ?.AnswerText ?? string.Empty
                : examAnswer.TextAnswer ?? string.Empty;

            try
            {
                var explanation = await _aiAnswerReviewService.ExplainAsync(
                    question.QuestionText,
                    studentAnswer,
                    correctAnswer,
                    question.Answers.Select(a => a.AnswerText),
                    cancellationToken);

                return Json(new { explanation });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return StatusCode(503, new
                {
                    error = "AI review is temporarily unavailable. Make sure Ollama is running and try again."
                });
            }
        }

    }
}