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
                if (submitted?.AnswerId != null)
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
    }
}