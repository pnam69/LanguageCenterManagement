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
    public class ScheduleChangeRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScheduleChangeRequestsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================
        // TEACHER
        // =========================

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var requests = await _context.ScheduleChangeRequests
                .Include(r => r.Schedule)
                    .ThenInclude(s => s!.Class)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s!.Room)
                 .Include(r => r.MakeUpSchedule)
                    .ThenInclude(s => s!.Room)
                .Where(r => r.TeacherId == user.TeacherId.Value)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> Create(int? scheduleId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var schedules = await GetTeacherSchedulesAsync(
                user.TeacherId.Value);

            ViewBag.Schedules = schedules;

            var model = new ScheduleChangeRequestViewModel();

            if (scheduleId.HasValue)
            {
                var selectedSchedule = schedules
                    .FirstOrDefault(s =>
                        s.ScheduleId == scheduleId.Value);

                if (selectedSchedule == null)
                {
                    return NotFound();
                }

                model.ScheduleId = selectedSchedule.ScheduleId;
                model.TeacherId = user.TeacherId.Value;
                model.ClassName = selectedSchedule.Class?.ClassName;
                model.StudyDate = selectedSchedule.StudyDate;
                model.StartTime = selectedSchedule.StartTime;
                model.EndTime = selectedSchedule.EndTime;
                model.RoomName = selectedSchedule.Room?.RoomName;
            }

            return View(model);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ScheduleChangeRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var teacherId = user.TeacherId.Value;

            var schedule = await _context.Schedules
                .Include(s => s.Class)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s =>
                    s.ScheduleId == model.ScheduleId);

            if (schedule == null)
            {
                ModelState.AddModelError(
                    "ScheduleId",
                    "The selected schedule does not exist.");
            }
            else if (schedule.Class == null ||
                     schedule.Class.TeacherId != teacherId)
            {
                return Forbid();
            }
            else if (schedule.Status != "Scheduled")
            {
                ModelState.AddModelError(
                    "ScheduleId",
                    "This schedule is no longer available for a change request.");
            }

            if (model.ProposedDate.HasValue &&
                model.ProposedDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "ProposedDate",
                    "The proposed date cannot be in the past.");
            }

            if (model.ProposedStartTime.HasValue !=
                model.ProposedEndTime.HasValue)
            {
                ModelState.AddModelError(
                    "",
                    "Both proposed start and end times must be provided together.");
            }

            if (model.ProposedStartTime.HasValue &&
                model.ProposedEndTime.HasValue &&
                model.ProposedEndTime.Value <=
                model.ProposedStartTime.Value)
            {
                ModelState.AddModelError(
                    "ProposedEndTime",
                    "Proposed end time must be later than proposed start time.");
            }
            if ((model.ProposedStartTime.HasValue || model.ProposedEndTime.HasValue) && !model.ProposedDate.HasValue)
            {
                ModelState.AddModelError(
                    "ProposedDate",
                    "A proposed make-up date is required when a make-up time is provided.");
            }

            if (model.ProposedDate.HasValue &&
                (!model.ProposedStartTime.HasValue ||
                 !model.ProposedEndTime.HasValue))
            {
                ModelState.AddModelError(
                    "",
                    "Proposed date, start time, and end time must all be provided together.");
            }
            if (!ModelState.IsValid)
            {
                await LoadSchedulesAsync(teacherId);
                LoadScheduleDisplayData(model, schedule);

                return View(model);
            }

            var pendingRequestExists =
                await _context.ScheduleChangeRequests
                    .AnyAsync(r =>
                        r.ScheduleId == model.ScheduleId &&
                        r.TeacherId == teacherId &&
                        r.Status == "Pending");

            if (pendingRequestExists)
            {
                ModelState.AddModelError(
                    "",
                    "There is already a pending request for this schedule.");

                await LoadSchedulesAsync(teacherId);
                LoadScheduleDisplayData(model, schedule);

                return View(model);
            }

            var request = new ScheduleChangeRequest
            {
                ScheduleId = schedule!.ScheduleId,
                TeacherId = teacherId,
                Reason = model.Reason,
                ProposedDate = model.ProposedDate,
                ProposedStartTime = model.ProposedStartTime,
                ProposedEndTime = model.ProposedEndTime,
                Status = "Pending",
                RequestDate = DateTime.Now
            };

            _context.ScheduleChangeRequests.Add(request);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Schedule change request submitted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ADMIN
        // =========================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var requests = await _context.ScheduleChangeRequests
                .Include(r => r.Teacher)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s!.Class)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s!.Room)
                .Include(r => r.MakeUpSchedule)
                    .ThenInclude(s => s!.Room)
                .OrderBy(r => r.Status == "Pending" ? 0 : 1)
                .ThenByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var request = await _context.ScheduleChangeRequests
                .Include(r => r.Teacher)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s!.Class)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s!.Room)
                .FirstOrDefaultAsync(r =>
                    r.ScheduleChangeRequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
    int id,
    string? adminNote)
        {
            var request = await _context.ScheduleChangeRequests
                .Include(r => r.Schedule)
                    .ThenInclude(s => s!.Class)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s!.Room)
                .FirstOrDefaultAsync(r =>
                    r.ScheduleChangeRequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This request has already been processed.";

                return RedirectToAction(nameof(AdminIndex));
            }

            var originalSchedule = request.Schedule;

            if (originalSchedule == null ||
                originalSchedule.Class == null)
            {
                TempData["ErrorMessage"] =
                    "The original schedule could not be found.";

                return RedirectToAction(nameof(AdminIndex));
            }

            var teacherId = originalSchedule.Class.TeacherId;

            // =========================================================
            // NO MAKE-UP DATE
            // =========================================================

            if (!request.ProposedDate.HasValue)
            {
                originalSchedule.Status = "TeacherLeave";

                request.Status = "Approved";
                request.AdminNote = adminNote;
                request.DecisionDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Request approved. The original schedule was marked as teacher leave.";

                return RedirectToAction(nameof(AdminIndex));
            }

            // =========================================================
            // VALIDATE MAKE-UP TIME
            // =========================================================

            if (!request.ProposedStartTime.HasValue ||
                !request.ProposedEndTime.HasValue)
            {
                TempData["ErrorMessage"] =
                    "The proposed make-up schedule has incomplete time information.";

                return RedirectToAction(nameof(Review), new { id });
            }

            var makeUpDate = request.ProposedDate.Value.Date;
            var makeUpStart = request.ProposedStartTime.Value;
            var makeUpEnd = request.ProposedEndTime.Value;

            if (makeUpDate < DateTime.Today)
            {
                TempData["ErrorMessage"] =
                    "The proposed make-up date cannot be in the past.";

                return RedirectToAction(nameof(Review), new { id });
            }

            if (makeUpEnd <= makeUpStart)
            {
                TempData["ErrorMessage"] =
                    "The proposed make-up end time must be later than the start time.";

                return RedirectToAction(nameof(Review), new { id });
            }

            // =========================================================
            // CHECK TEACHER CONFLICT
            // =========================================================

            var teacherConflict = await _context.Schedules
                .Include(s => s.Class)
                .AnyAsync(s =>
                    s.Status != "Cancelled" &&
                    s.Status != "TeacherLeave" &&
                    s.Class != null &&
                    s.Class.TeacherId == teacherId &&
                    s.StudyDate == makeUpDate &&
                    s.StartTime < makeUpEnd &&
                    makeUpStart < s.EndTime);

            if (teacherConflict)
            {
                TempData["ErrorMessage"] =
                    "The teacher already has another class during the proposed make-up time.";

                return RedirectToAction(nameof(Review), new { id });
            }

            // =========================================================
            // DETERMINE REQUIRED ROOM CAPACITY
            // =========================================================

            var studentCount = await _context.Enrollments
                .CountAsync(e =>
                    e.ClassId == originalSchedule.ClassId &&
                    e.Status != "Cancelled");

            // =========================================================
            // TRY TO KEEP ORIGINAL ROOM
            // =========================================================

            Room? selectedRoom = null;

            if (originalSchedule.Room != null &&
                originalSchedule.Room.Status != "Maintenance" &&
                originalSchedule.Room.Status != "Inactive" &&
                originalSchedule.Room.Capacity >= studentCount)
            {
                var originalRoomConflict = await _context.Schedules
                    .AnyAsync(s =>
                        s.Status != "Cancelled" &&
                        s.Status != "TeacherLeave" &&
                        s.RoomId == originalSchedule.RoomId &&
                        s.StudyDate == makeUpDate &&
                        s.StartTime < makeUpEnd &&
                        makeUpStart < s.EndTime);

                if (!originalRoomConflict)
                {
                    selectedRoom = originalSchedule.Room;
                }
            }

            // =========================================================
            // FIND ANOTHER ROOM IF NECESSARY
            // =========================================================

            if (selectedRoom == null)
            {
                var availableRooms = await _context.Rooms
                    .Where(r =>
                        r.Status != "Maintenance" &&
                        r.Status != "Inactive" &&
                        r.Capacity >= studentCount)
                    .OrderBy(r => r.Capacity)
                    .ToListAsync();

                foreach (var room in availableRooms)
                {
                    var roomConflict = await _context.Schedules
                        .AnyAsync(s =>
                            s.Status != "Cancelled" &&
                            s.Status != "TeacherLeave" &&
                            s.RoomId == room.RoomId &&
                            s.StudyDate == makeUpDate &&
                            s.StartTime < makeUpEnd &&
                            makeUpStart < s.EndTime);

                    if (!roomConflict)
                    {
                        selectedRoom = room;
                        break;
                    }
                }
            }

            if (selectedRoom == null)
            {
                TempData["ErrorMessage"] =
                    "No suitable room is available for the proposed make-up schedule.";

                return RedirectToAction(nameof(Review), new { id });
            }

            // =========================================================
            // CREATE NEW MAKE-UP SCHEDULE
            // =========================================================

            var makeUpSchedule = new Schedule
            {
                ClassId = originalSchedule.ClassId,
                RoomId = selectedRoom.RoomId,
                StudyDate = makeUpDate,
                StartTime = makeUpStart,
                EndTime = makeUpEnd,
                Status = "MakeUp"
            };

            _context.Schedules.Add(makeUpSchedule);

            // Original schedule is preserved but marked as teacher leave.
            originalSchedule.Status = "TeacherLeave";

            request.Status = "Approved";
            request.AdminNote = adminNote;
            request.DecisionDate = DateTime.Now;

            await _context.SaveChangesAsync();

            // Link the request to the newly created schedule.
            request.MakeUpScheduleId = makeUpSchedule.ScheduleId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Request approved. A make-up class was created for " +
                $"{makeUpDate:dd/MM/yyyy} " +
                $"{makeUpStart:hh\\:mm}-{makeUpEnd:hh\\:mm} " +
                $"in room {selectedRoom.RoomName}.";

            return RedirectToAction(nameof(AdminIndex));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? adminNote)
        {
            var request = await _context.ScheduleChangeRequests
                .FirstOrDefaultAsync(r =>
                    r.ScheduleChangeRequestId == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "This request has already been processed.";

                return RedirectToAction(nameof(AdminIndex));
            }

            request.Status = "Rejected";
            request.AdminNote = adminNote;
            request.DecisionDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Schedule change request rejected.";

            return RedirectToAction(nameof(AdminIndex));
        }

        // =========================
        // HELPERS
        // =========================

        private async Task<List<Schedule>> GetTeacherSchedulesAsync(
            int teacherId)
        {
            return await _context.Schedules
                .Include(s => s.Class)
                .Include(s => s.Room)
                .Where(s =>
                    s.Class != null &&
                    s.Class.TeacherId == teacherId &&
                    s.Status == "Scheduled")
                .OrderBy(s => s.StudyDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        private async Task LoadSchedulesAsync(int teacherId)
        {
            ViewBag.Schedules =
                await GetTeacherSchedulesAsync(teacherId);
        }

        private void LoadScheduleDisplayData(
            ScheduleChangeRequestViewModel model,
            Schedule? schedule)
        {
            if (schedule == null)
            {
                return;
            }

            model.TeacherId = schedule.Class?.TeacherId ?? 0;
            model.ClassName = schedule.Class?.ClassName;
            model.StudyDate = schedule.StudyDate;
            model.StartTime = schedule.StartTime;
            model.EndTime = schedule.EndTime;
            model.RoomName = schedule.Room?.RoomName;
        }
    }
}