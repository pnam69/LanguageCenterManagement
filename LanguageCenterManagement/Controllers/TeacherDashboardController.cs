using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var teacherId = user.TeacherId.Value;
            var now = DateTime.Now;

            var teacherExams = _context.Exams
                .AsNoTracking()
                .Where(e =>
                    e.Class != null &&
                    e.Class.TeacherId == teacherId);

            ViewBag.UpcomingExams = await teacherExams.CountAsync(e =>
                e.Status == "Published" &&
                e.ExamDate >= now);

            ViewBag.TotalExams = await teacherExams.CountAsync();

            ViewBag.PendingGrading = await _context.ExamResults
                .AsNoTracking()
                .CountAsync(r =>
                    r.Exam != null &&
                    r.Exam.Class != null &&
                    r.Exam.Class.TeacherId == teacherId &&
                    r.Status == "Submitted");

            ViewBag.PendingIntegrityReviews =
                await _context.WritingIntegrityReviews
                    .AsNoTracking()
                    .CountAsync(r =>
                        r.Status == "Pending" &&
                        r.ExamAnswer != null &&
                        r.ExamAnswer.ExamResult != null &&
                        r.ExamAnswer.ExamResult.Exam != null &&
                        r.ExamAnswer.ExamResult.Exam.Class != null &&
                        r.ExamAnswer.ExamResult.Exam.Class.TeacherId
                            == teacherId);

            var today = DateTime.Today;
            var currentTime = DateTime.Now.TimeOfDay;

            ViewBag.UpcomingClasses = await _context.Schedules
                .AsNoTracking()
                .Include(s => s.Class)
                .Include(s => s.Room)
                .Where(s =>
                    s.Class != null &&
                    s.Class.TeacherId == teacherId &&
                    s.StudyDate >= today &&
                    (s.StudyDate > today || s.StartTime >= currentTime) &&
                    s.Status != "Cancelled" &&
                    s.Status != "TeacherLeave")
                .OrderBy(s => s.StudyDate)
                .ThenBy(s => s.StartTime)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
}
