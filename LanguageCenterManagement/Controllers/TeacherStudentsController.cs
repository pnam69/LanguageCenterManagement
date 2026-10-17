using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherStudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherStudentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? classId, string? search)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.TeacherId == null)
                return Forbid();

            var teacherId = user.TeacherId.Value;

            var teacherClasses = await _context.Classes
                .AsNoTracking()
                .Where(c => c.TeacherId == teacherId)
                .OrderBy(c => c.ClassName)
                .Select(c => new TeacherStudentClassOption
                {
                    ClassId = c.LanguageClassId,
                    ClassName = c.ClassName,
                    ClassCode = c.ClassCode
                })
                .ToListAsync();

            var allowedClassIds = teacherClasses
                .Select(c => c.ClassId)
                .ToList();

            var model = new TeacherStudentListViewModel
            {
                ClassId = classId,
                Search = search?.Trim(),
                Classes = teacherClasses
            };

            if (classId.HasValue && !allowedClassIds.Contains(classId.Value))
                return Forbid();

            var enrollmentsQuery = _context.Enrollments
                .AsNoTracking()
                .Where(e =>
                    allowedClassIds.Contains(e.ClassId) &&
                    e.Status != "Rejected" &&
                    e.Status != "Cancelled");

            if (classId.HasValue)
                enrollmentsQuery = enrollmentsQuery
                    .Where(e => e.ClassId == classId.Value);

            if (!string.IsNullOrWhiteSpace(model.Search))
            {
                var term = model.Search;

                enrollmentsQuery = enrollmentsQuery.Where(e =>
                    e.Student != null &&
                    (e.Student.FullName.Contains(term) ||
                     e.Student.StudentCode.Contains(term)));
            }

            var enrollments = await enrollmentsQuery
                .Include(e => e.Student)
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Course)
                .OrderBy(e => e.Student!.FullName)
                .ThenBy(e => e.Class!.ClassName)
                .ToListAsync();

            var studentIds = enrollments
                .Where(e => e.Student != null)
                .Select(e => e.StudentId)
                .Distinct()
                .ToList();

            var enrollmentClassIds = enrollments
                .Select(e => e.ClassId)
                .Distinct()
                .ToList();

            var attendanceRows = await _context.Attendances
                .AsNoTracking()
                .Where(a =>
                    studentIds.Contains(a.StudentId) &&
                    enrollmentClassIds.Contains(a.ClassId))
                .Select(a => new
                {
                    a.StudentId,
                    a.ClassId,
                    a.Status
                })
                .ToListAsync();

            model.Students = enrollments
                .Where(e => e.Student != null && e.Class != null)
                .Select(e =>
                {
                    var studentAttendance = attendanceRows.Where(a =>
                        a.StudentId == e.StudentId &&
                        a.ClassId == e.ClassId);

                    return new TeacherStudentRowViewModel
                    {
                        StudentId = e.StudentId,
                        ClassId = e.ClassId,
                        StudentCode = e.Student!.StudentCode,
                        FullName = e.Student.FullName,
                        Email = e.Student.Email,
                        Phone = e.Student.Phone,
                        ClassName = e.Class!.ClassName,
                        CourseName = e.Class.Course?.CourseName ?? "—",
                        EnrollmentStatus = e.Status,
                        PresentCount = studentAttendance.Count(a =>
                            a.Status == "Present"),
                        AbsentCount = studentAttendance.Count(a =>
                            a.Status == "Absent"),
                        LateCount = studentAttendance.Count(a =>
                            a.Status == "Late")
                    };
                })
                .ToList();

            return View(model);
        }

        public async Task<IActionResult> Details(int studentId, int classId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.TeacherId == null)
                return Forbid();

            var teacherId = user.TeacherId.Value;

            // Verify that this class belongs to the signed-in teacher.
            var classInfo = await _context.Classes
                .AsNoTracking()
                .Where(c =>
                    c.LanguageClassId == classId &&
                    c.TeacherId == teacherId)
                .Select(c => new
                {
                    c.LanguageClassId,
                    c.ClassCode,
                    c.ClassName,
                    CourseName = c.Course != null
                        ? c.Course.CourseName
                        : "—"
                })
                .FirstOrDefaultAsync();

            if (classInfo == null)
                return NotFound();

            // Verify that the student is enrolled in this teacher's class.
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Student)
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId &&
                    e.ClassId == classId &&
                    e.Status != "Rejected" &&
                    e.Status != "Cancelled");

            if (enrollment?.Student == null)
                return NotFound();

            var student = enrollment.Student;

            var attendance = await _context.Attendances
                .AsNoTracking()
                .Where(a =>
                    a.StudentId == studentId &&
                    a.ClassId == classId)
                .OrderByDescending(a => a.AttendanceDate)
                .Select(a => new TeacherStudentAttendanceRowViewModel
                {
                    AttendanceDate = a.AttendanceDate,
                    Status = a.Status,
                    Note = a.Note
                })
                .ToListAsync();

            var model = new TeacherStudentDetailsViewModel
            {
                StudentId = student.StudentId,
                ClassId = classId,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                Phone = student.Phone,
                Email = student.Email,
                Address = student.Address,
                StudentStatus = student.Status,
                ClassName = classInfo.ClassName,
                ClassCode = classInfo.ClassCode,
                CourseName = classInfo.CourseName,
                EnrollmentStatus = enrollment.Status,
                AttendanceHistory = attendance,
                TotalLessonsRecorded = attendance.Count,
                PresentCount = attendance.Count(a => a.Status == "Present"),
                AbsentCount = attendance.Count(a => a.Status == "Absent"),
                LateCount = attendance.Count(a => a.Status == "Late")
            };

            return View(model);
        }
    }
}
