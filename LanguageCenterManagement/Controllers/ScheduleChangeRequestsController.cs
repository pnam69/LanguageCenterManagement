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
                .Where(r => r.TeacherId == user.TeacherId.Value)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? scheduleId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var schedules = await _context.Schedules
                .Include(s => s.Class)
                .Include(s => s.Room)
                .Where(s =>
                    s.Class != null &&
                    s.Class.TeacherId == user.TeacherId.Value &&
                    s.Status == "Scheduled")
                .OrderBy(s => s.StudyDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.Schedules = schedules;

            var model = new ScheduleChangeRequestViewModel();

            if (scheduleId.HasValue)
            {
                var selectedSchedule = schedules
                    .FirstOrDefault(s => s.ScheduleId == scheduleId.Value);

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

        private async Task LoadSchedulesAsync(int teacherId)
        {
            ViewBag.Schedules = await _context.Schedules
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

        private void LoadScheduleDisplayData(
            ScheduleChangeRequestViewModel model,
            Schedule? schedule)
        {
            if (schedule == null)
            {
                return;
            }

            model.ClassName = schedule.Class?.ClassName;
            model.StudyDate = schedule.StudyDate;
            model.StartTime = schedule.StartTime;
            model.EndTime = schedule.EndTime;
            model.RoomName = schedule.Room?.RoomName;
        }
    }
}