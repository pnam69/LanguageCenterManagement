using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentExamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentExamsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            var model = exams.Select(e => new StudentExamListViewModel
            {
                ExamId = e.ExamId,
                ExamName = e.ExamName,
                ClassName = e.Class?.ClassName ?? "",
                ExamType = e.ExamType,
                ExamDate = e.ExamDate,
                Duration = e.Duration,
                MaxScore = e.MaxScore,
                Status = e.Status,
                HasSubmitted = e.ExamResults.Any(
                    r => r.StudentId == studentId)
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
                .Where(r => questionIds.Contains(r.QuestionId))
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
                            .FirstOrDefault(r => r.QuestionId == eq.QuestionId);

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
        public async Task<IActionResult> Take(
            StudentExamSubmitViewModel model)
        {
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
                .FirstOrDefaultAsync(e => e.ExamId == model.ExamId);

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

                return RedirectToAction(nameof(Index));
            }

            if (exam.ExamResults.Any(r => r.StudentId == studentId))
            {
                TempData["ErrorMessage"] =
                    "You have already submitted this exam.";

                return RedirectToAction(nameof(Index));
            }

            decimal totalQuestionPoints = exam.ExamQuestions
                .Where(eq => eq.Question != null)
                .Sum(eq => eq.Question!.Score);

            if (totalQuestionPoints <= 0)
            {
                TempData["ErrorMessage"] =
                    "This exam has no valid questions.";

                return RedirectToAction(nameof(Index));
            }

            decimal earnedPoints = 0;

            foreach (var examQuestion in exam.ExamQuestions)
            {
                var submission = model.Answers
                    .FirstOrDefault(a =>
                        a.QuestionId == examQuestion.QuestionId);

                if (submission?.AnswerId == null)
                {
                    continue;
                }

                var correctAnswer = examQuestion.Question?.Answers
                    .FirstOrDefault(a =>
                        a.AnswerId == submission.AnswerId &&
                        a.IsCorrect);

                if (correctAnswer != null)
                {
                    earnedPoints += examQuestion.Question!.Score;
                }
            }

            decimal finalScore =
                earnedPoints / totalQuestionPoints * exam.MaxScore;

            finalScore = Math.Round(finalScore, 2);

            var result = new ExamResult
            {
                ExamId = exam.ExamId,
                StudentId = studentId,
                Score = finalScore,
                Status = "Completed",
                SubmittedAt = DateTime.Now
            };

            _context.ExamResults.Add(result);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Exam submitted successfully. Your score is {finalScore}/{exam.MaxScore}.";

            return RedirectToAction(
                nameof(Result),
                new { id = result.ExamResultId });
        }

        // GET: StudentExams/Result/5
        public async Task<IActionResult> Result(int? id)
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

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .FirstOrDefaultAsync(r =>
                    r.ExamResultId == id &&
                    r.StudentId == user.StudentId.Value);

            if (result == null)
            {
                return NotFound();
            }

            return View(result);
        }
    }
}