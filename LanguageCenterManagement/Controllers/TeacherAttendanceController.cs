using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherAttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherAttendanceController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.TeacherId == null)
                return Forbid();

            var teacherId = user.TeacherId.Value;

            var schedules = await _context.Schedules
                .AsNoTracking()
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Course)
                .Include(s => s.Room)
                .Where(s =>
                    s.Class != null &&
                    s.Class.TeacherId == teacherId)
                .OrderByDescending(s => s.StudyDate)
                .ThenBy(s => s.StartTime)
                .Select(s => new TeacherAttendanceScheduleViewModel
                {
                    ScheduleId = s.ScheduleId,
                    ClassCode = s.Class!.ClassCode,
                    ClassName = s.Class.ClassName,
                    CourseName = s.Class.Course != null
                        ? s.Class.Course.CourseName : "",
                    RoomName = s.Room != null
                        ? s.Room.RoomName : "No room",
                    StudyDate = s.StudyDate,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Status = s.Status,
                    StudentCount = _context.Enrollments.Count(e =>
                        e.ClassId == s.ClassId &&
                        e.Student != null &&
                        e.Status != "Rejected" &&
                        e.Status != "Cancelled"),
                    RecordedCount = _context.Attendances.Count(a =>
                        a.ScheduleId == s.ScheduleId)
                })
                .ToListAsync();

            return View(schedules);
        }

        [HttpGet]
        public async Task<IActionResult> Take(int scheduleId)
        {
            var schedule = await GetTeacherScheduleAsync(scheduleId);

            if (schedule == null)
                return NotFound();

            if (!CanTakeAttendance(schedule.Status))
            {
                TempData["ErrorMessage"] =
                    "Attendance cannot be taken for a cancelled or inactive session.";

                return RedirectToAction(nameof(Index));
            }

            var model = await BuildAttendanceModelAsync(schedule);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(
            TeacherAttendanceViewModel model)
        {
            var schedule = await GetTeacherScheduleAsync(model.ScheduleId);

            if (schedule == null)
                return NotFound();

            if (!CanTakeAttendance(schedule.Status))
                return Forbid();

            var enrolledStudents = await _context.Enrollments
                .Where(e =>
                    e.ClassId == schedule.ClassId &&
                    e.Student != null &&
                    e.Status != "Rejected" &&
                    e.Status != "Cancelled")
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            var postedIds = model.Students
                .Select(s => s.StudentId)
                .ToList();

            // Do not accept missing, duplicate, or unenrolled students.
            if (postedIds.Count != postedIds.Distinct().Count() ||
                !postedIds.OrderBy(id => id)
                    .SequenceEqual(enrolledStudents.OrderBy(id => id)))
            {
                return BadRequest(
                    "The submitted student list does not match this class's enrollment.");
            }

            var validStatuses = new[] { "Present", "Absent", "Late" };

            if (model.Students.Any(s =>
                !validStatuses.Contains(s.Status)))
            {
                ModelState.AddModelError(
                    "", "One or more attendance statuses are invalid.");
            }

            if (!ModelState.IsValid)
            {
                var rebuilt = await BuildAttendanceModelAsync(schedule);

                // Retain the teacher's submitted status and note values.
                foreach (var student in rebuilt.Students)
                {
                    var submitted = model.Students.FirstOrDefault(
                        s => s.StudentId == student.StudentId);

                    if (submitted != null)
                    {
                        student.Status = submitted.Status;
                        student.Note = submitted.Note;
                    }
                }

                return View(rebuilt);
            }

            var existing = await _context.Attendances
                .Where(a => a.ScheduleId == schedule.ScheduleId)
                .ToDictionaryAsync(a => a.StudentId);

            foreach (var student in model.Students)
            {
                if (!existing.TryGetValue(
                    student.StudentId, out var attendance))
                {
                    attendance = new Attendance
                    {
                        StudentId = student.StudentId,
                        ClassId = schedule.ClassId,
                        ScheduleId = schedule.ScheduleId,
                        AttendanceDate = schedule.StudyDate.Date
                    };

                    _context.Attendances.Add(attendance);
                }

                // Keep the class/date consistent with the actual session.
                attendance.ClassId = schedule.ClassId;
                attendance.AttendanceDate = schedule.StudyDate.Date;
                attendance.Status = student.Status;
                attendance.Note = student.Note;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Attendance for {schedule.Class!.ClassName} " +
                $"on {schedule.StudyDate:dd/MM/yyyy} saved successfully.";

            return RedirectToAction(nameof(Take), new
            {
                scheduleId = schedule.ScheduleId
            });
        }

        private async Task<Schedule?> GetTeacherScheduleAsync(int scheduleId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.TeacherId == null)
                return null;

            return await _context.Schedules
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Course)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s =>
                    s.ScheduleId == scheduleId &&
                    s.Class != null &&
                    s.Class.TeacherId == user.TeacherId.Value);
        }

        private async Task<TeacherAttendanceViewModel>
            BuildAttendanceModelAsync(Schedule schedule)
        {
            var students = await _context.Enrollments
                .Where(e =>
                    e.ClassId == schedule.ClassId &&
                    e.Student != null &&
                    e.Status != "Rejected" &&
                    e.Status != "Cancelled")
                .OrderBy(e => e.Student!.FullName)
                .Select(e => new TeacherAttendanceStudentViewModel
                {
                    StudentId = e.Student!.StudentId,
                    StudentCode = e.Student.StudentCode,
                    FullName = e.Student.FullName,
                    Status = "Present"
                })
                .ToListAsync();

            var existing = await _context.Attendances
                .Where(a => a.ScheduleId == schedule.ScheduleId)
                .ToDictionaryAsync(a => a.StudentId);

            foreach (var student in students)
            {
                if (existing.TryGetValue(student.StudentId, out var record))
                {
                    student.Status = record.Status;
                    student.Note = record.Note;
                }
            }

            return new TeacherAttendanceViewModel
            {
                ScheduleId = schedule.ScheduleId,
                ClassName = schedule.Class?.ClassName ?? "",
                ClassCode = schedule.Class?.ClassCode ?? "",
                CourseName = schedule.Class?.Course?.CourseName ?? "",
                RoomName = schedule.Room?.RoomName ?? "No room",
                AttendanceDate = schedule.StudyDate.Date,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Students = students
            };
        }

        private static bool CanTakeAttendance(string? status)
        {
            // Cancelled sessions must never receive attendance.
            return !string.IsNullOrWhiteSpace(status) &&
                   status.Equals(
                       "Scheduled",
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
