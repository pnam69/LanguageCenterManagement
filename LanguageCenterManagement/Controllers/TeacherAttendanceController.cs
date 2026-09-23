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

        // GET: TeacherAttendance
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var classes = await _context.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == user.TeacherId.Value)
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            return View(classes);
        }

        // GET: TeacherAttendance/Take
        [HttpGet]
        public async Task<IActionResult> Take(
            int classId,
            DateTime? attendanceDate)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var selectedDate = attendanceDate?.Date ?? DateTime.Today;

            var languageClass = await _context.Classes
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c =>
                    c.LanguageClassId == classId &&
                    c.TeacherId == user.TeacherId.Value);

            if (languageClass == null)
            {
                return NotFound();
            }

            var students = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e =>
                    e.ClassId == classId &&
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

            var existingAttendance = await _context.Attendances
                .Where(a =>
                    a.ClassId == classId &&
                    a.AttendanceDate.Date == selectedDate)
                .ToListAsync();

            foreach (var student in students)
            {
                var existing = existingAttendance.FirstOrDefault(a =>
                    a.StudentId == student.StudentId);

                if (existing != null)
                {
                    student.Status = existing.Status;
                    student.Note = existing.Note;
                }
            }

            var model = new TeacherAttendanceViewModel
            {
                ClassId = classId,
                AttendanceDate = selectedDate,
                ClassName = languageClass.ClassName,
                Students = students
            };

            return View(model);
        }

        // POST: TeacherAttendance/Take
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(
            TeacherAttendanceViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var languageClass = await _context.Classes
                .FirstOrDefaultAsync(c =>
                    c.LanguageClassId == model.ClassId &&
                    c.TeacherId == user.TeacherId.Value);

            if (languageClass == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.ClassName = languageClass.ClassName;
                return View(model);
            }

            var studentIds = await _context.Enrollments
                .Where(e =>
                    e.ClassId == model.ClassId &&
                    e.Status != "Rejected" &&
                    e.Status != "Cancelled")
                .Select(e => e.StudentId)
                .ToListAsync();

            var invalidStudent = model.Students
                .FirstOrDefault(s => !studentIds.Contains(s.StudentId));

            if (invalidStudent != null)
            {
                return BadRequest();
            }

            var attendanceDate = model.AttendanceDate.Date;

            var existingAttendance = await _context.Attendances
                .Where(a =>
                    a.ClassId == model.ClassId &&
                    a.AttendanceDate.Date == attendanceDate)
                .ToListAsync();

            foreach (var student in model.Students)
            {
                var attendance = existingAttendance.FirstOrDefault(a =>
                    a.StudentId == student.StudentId);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        StudentId = student.StudentId,
                        ClassId = model.ClassId,
                        AttendanceDate = attendanceDate
                    };

                    _context.Attendances.Add(attendance);
                }

                attendance.Status = student.Status;
                attendance.Note = student.Note;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Attendance for {languageClass.ClassName} on {attendanceDate:dd/MM/yyyy} was saved successfully.";

            return RedirectToAction(nameof(Take), new
            {
                classId = model.ClassId,
                attendanceDate
            });
        }
    }
}