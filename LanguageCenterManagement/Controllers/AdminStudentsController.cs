
using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminStudentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminStudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var query = _context.Students
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(s =>
                    s.FullName.Contains(search) ||
                    s.StudentCode.Contains(search) ||
                    (s.Email != null && s.Email.Contains(search)) ||
                    (s.Phone != null && s.Phone.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status);
            }

            var students = await query
                .OrderBy(s => s.FullName)
                .Select(s => new AdminStudentRowViewModel
                {
                    StudentId = s.StudentId,
                    StudentCode = s.StudentCode,
                    FullName = s.FullName,
                    Email = s.Email,
                    Phone = s.Phone,
                    Status = s.Status,
                    EnrollmentCount = s.Enrollments.Count(e =>
                        e.Status != "Rejected" &&
                        e.Status != "Cancelled")
                })
                .ToListAsync();

            var model = new AdminStudentListViewModel
            {
                Search = search,
                Status = status,
                Students = students
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == id)
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Course)
                .OrderByDescending(e => e.ClassId)
                .Select(e => new AdminStudentEnrollmentRowViewModel
                {
                    ClassId = e.ClassId,
                    ClassCode = e.Class != null
                        ? e.Class.ClassCode : "—",
                    ClassName = e.Class != null
                        ? e.Class.ClassName : "Unknown class",
                    CourseName = e.Class != null && e.Class.Course != null
                        ? e.Class.Course.CourseName : "—",
                    EnrollmentStatus = e.Status
                })
                .ToListAsync();

            var attendance = await _context.Attendances
                .AsNoTracking()
                .Where(a => a.StudentId == id)
                .Include(a => a.Class)
                .OrderByDescending(a => a.AttendanceDate)
                .Select(a => new AdminStudentAttendanceRowViewModel
                {
                    AttendanceDate = a.AttendanceDate,
                    ClassName = a.Class != null
                        ? a.Class.ClassName : "Unknown class",
                    Status = a.Status,
                    Note = a.Note
                })
                .ToListAsync();

            var model = new AdminStudentDetailsViewModel
            {
                StudentId = student.StudentId,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                Phone = student.Phone,
                Email = student.Email,
                Address = student.Address,
                Status = student.Status,
                CreatedAt = student.CreatedAt,
                Enrollments = enrollments,
                AttendanceHistory = attendance
            };

            return View(model);
        }
    }
}
