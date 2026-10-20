using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ExamMonitoringController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamMonitoringController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int?> GetTeacherIdAsync()
        {
            if (!User.IsInRole("Teacher"))
                return null;

            var user = await _userManager.GetUserAsync(User);
            return user?.TeacherId;
        }

        private IQueryable<ExamMonitoringEvent> GetEventsQuery(
            int? teacherId)
        {
            var query = _context.ExamMonitoringEvents
                .AsNoTracking()
                .Include(e => e.Exam)
                    .ThenInclude(e => e!.Class)
                .Include(e => e.Student)
                .AsQueryable();

            if (teacherId.HasValue)
            {
                query = query.Where(e =>
                    e.Exam != null &&
                    e.Exam.Class != null &&
                    e.Exam.Class.TeacherId == teacherId.Value);
            }

            return query;
        }

        public async Task<IActionResult> Index(
            int? examId,
            CancellationToken cancellationToken)
        {
            var teacherId = await GetTeacherIdAsync();

            if (User.IsInRole("Teacher") && !teacherId.HasValue)
                return Forbid();

            var examsQuery = _context.Exams
                .AsNoTracking()
                .Include(e => e.Class)
                .AsQueryable();

            if (teacherId.HasValue)
            {
                examsQuery = examsQuery.Where(e =>
                    e.Class != null &&
                    e.Class.TeacherId == teacherId.Value);
            }

            var exams = await examsQuery
                .OrderByDescending(e => e.ExamDate)
                .Select(e => new
                {
                    e.ExamId,
                    e.ExamName
                })
                .ToListAsync(cancellationToken);

            // Do not allow a teacher to filter by another teacher's exam.
            if (examId.HasValue &&
                !exams.Any(e => e.ExamId == examId.Value))
            {
                return NotFound();
            }

            var eventsQuery = GetEventsQuery(teacherId);

            if (examId.HasValue)
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.ExamId == examId.Value);
            }

            // Retrieve all matching events for accurate dashboard counts.
            // If the event table becomes large, move these aggregates to SQL.
            var events = await eventsQuery
                .OrderByDescending(e => e.OccurredAt)
                .ToListAsync(cancellationToken);

            var students = events
                .GroupBy(e => new
                {
                    e.StudentId,
                    StudentName = e.Student != null
                        ? e.Student.FullName
                        : "Unknown student",
                    ExamName = e.Exam != null
                        ? e.Exam.ExamName
                        : "Unknown exam",
                    ClassName = e.Exam != null &&
                                e.Exam.Class != null
                        ? e.Exam.Class.ClassName
                        : "—"
                })
                .Select(group =>
                {
                    var latest = group
                        .OrderByDescending(e => e.OccurredAt)
                        .First();

                    return new StudentMonitoringSummary
                    {
                        StudentId = group.Key.StudentId,
                        StudentName = group.Key.StudentName,
                        ExamName = group.Key.ExamName,
                        ClassName = group.Key.ClassName,
                        TotalIncidents = group.Count(),
                        TabHiddenCount = group.Count(e =>
                            e.EventType == "TabHidden"),
                        FocusLostCount = group.Count(e =>
                            e.EventType != "TabHidden"),
                        LastIncident = latest.OccurredAt,
                        ReviewStatus = group.Any(e =>
                            e.ReviewStatus == "Pending")
                                ? "Pending"
                                : group.Any(e =>
                                    e.ReviewStatus == "Reviewed")
                                    ? "Reviewed"
                                    : "Resolved"
                    };
                })
                .OrderByDescending(s => s.TotalIncidents)
                .ThenByDescending(s => s.LastIncident)
                .ToList();

            var model = new ExamMonitoringDashboardViewModel
            {
                SelectedExamId = examId,
                Exams = exams.Select(e => new SelectListItem
                {
                    Value = e.ExamId.ToString(),
                    Text = e.ExamName
                }).ToList(),

                TotalIncidents = events.Count,
                StudentsFlagged = events
                    .Select(e => e.StudentId)
                    .Distinct()
                    .Count(),
                PendingReviews = events.Count(e =>
                    e.ReviewStatus == "Pending"),
                ReviewedIncidents = events.Count(e =>
                    e.ReviewStatus == "Reviewed" ||
                    e.ReviewStatus == "Resolved"),

                Students = students,
                RecentEvents = events.Take(100).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReview(
            int id,
            string status,
            string? notes,
            int? examId,
            CancellationToken cancellationToken)
        {
            var allowedStatuses = new[]
            {
                "Pending",
                "Reviewed",
                "Resolved"
            };

            if (!allowedStatuses.Contains(status))
                return BadRequest();

            var teacherId = await GetTeacherIdAsync();

            if (User.IsInRole("Teacher") && !teacherId.HasValue)
                return Forbid();

            var query = _context.ExamMonitoringEvents
                .Include(e => e.Exam)
                    .ThenInclude(e => e!.Class)
                .Where(e => e.ExamMonitoringEventId == id);

            if (teacherId.HasValue)
            {
                query = query.Where(e =>
                    e.Exam != null &&
                    e.Exam.Class != null &&
                    e.Exam.Class.TeacherId == teacherId.Value);
            }

            var monitoringEvent = await query
                .FirstOrDefaultAsync(cancellationToken);

            if (monitoringEvent == null)
                return NotFound();

            monitoringEvent.ReviewStatus = status;
            monitoringEvent.ReviewNotes = string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim();

            monitoringEvent.ReviewedByUserId =
                status == "Pending"
                    ? null
                    : _userManager.GetUserId(User);

            monitoringEvent.ReviewedAt =
                status == "Pending"
                    ? null
                    : DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index), new { examId });
        }
    }
}
