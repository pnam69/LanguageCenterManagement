using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LanguageCenterManagement.ViewModels;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedules
        public async Task<IActionResult> Index(DateTime? weekStart)
        {
            var selectedDate = weekStart?.Date ?? DateTime.Today;

            var monday = selectedDate.AddDays(
                -(int)selectedDate.DayOfWeek + (int)DayOfWeek.Monday);

            if (selectedDate.DayOfWeek == DayOfWeek.Sunday)
            {
                monday = selectedDate.AddDays(-6);
            }

            var sunday = monday.AddDays(6);

            var schedules = await _context.Schedules
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Teacher)
                .Include(s => s.Room)
                .Where(s =>
                    s.StudyDate.Date >= monday.Date &&
                    s.StudyDate.Date <= sunday.Date)
                .OrderBy(s => s.StartTime)
                .ThenBy(s => s.StudyDate)
                .ToListAsync();

            ViewBag.Monday = monday;
            ViewBag.Sunday = sunday;

            return View(schedules);
        }

        // GET: Schedules/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();

            var model = new ScheduleCreateViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1),
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(20, 0, 0),
                Status = "Scheduled"
            };

            return View(model);
        }

        // POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ScheduleCreateViewModel model)
        {
            if (model.EndDate.Date < model.StartDate.Date)
            {
                ModelState.AddModelError(
                    "EndDate",
                    "End date must be on or after the start date.");
            }

            if (model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(
                    "EndTime",
                    "End time must be later than start time.");
            }

            var selectedDays = new List<DayOfWeek>();

            if (model.Monday)
                selectedDays.Add(DayOfWeek.Monday);

            if (model.Tuesday)
                selectedDays.Add(DayOfWeek.Tuesday);

            if (model.Wednesday)
                selectedDays.Add(DayOfWeek.Wednesday);

            if (model.Thursday)
                selectedDays.Add(DayOfWeek.Thursday);

            if (model.Friday)
                selectedDays.Add(DayOfWeek.Friday);

            if (model.Saturday)
                selectedDays.Add(DayOfWeek.Saturday);

            if (model.Sunday)
                selectedDays.Add(DayOfWeek.Sunday);

            if (selectedDays.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Select at least one day of the week.");
            }


            var selectedClass = await _context.Classes
    .FirstOrDefaultAsync(c => c.LanguageClassId == model.ClassId);

            var selectedRoom = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);

            if (selectedClass == null)
            {
                ModelState.AddModelError(
                    "ClassId",
                    "The selected class does not exist.");
            }

            if (selectedRoom == null)
            {
                ModelState.AddModelError(
                    "RoomId",
                    "The selected room does not exist.");
            }

            if (selectedRoom != null &&
                (selectedRoom.Status == "Maintenance" ||
                 selectedRoom.Status == "Inactive"))
            {
                ModelState.AddModelError(
                    "RoomId",
                    "This room is not available for scheduling.");
            }

            if (selectedClass != null &&
                selectedRoom != null &&
                selectedClass.MaxStudents > selectedRoom.Capacity)
            {
                ModelState.AddModelError(
                    "RoomId",
                    $"The room capacity ({selectedRoom.Capacity}) is smaller than the class maximum students ({selectedClass.MaxStudents}).");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(model.ClassId, model.RoomId);
                return View(model);
            }

            var schedulesToCreate = new List<Schedule>();

            for (var date = model.StartDate.Date;
                 date <= model.EndDate.Date;
                 date = date.AddDays(1))
            {
                if (!selectedDays.Contains(date.DayOfWeek))
                {
                    continue;
                }

                var roomConflict = await _context.Schedules
                    .AnyAsync(s =>
                        s.RoomId == model.RoomId &&
                        s.StudyDate.Date == date &&
                        model.StartTime < s.EndTime &&
                        model.EndTime > s.StartTime);

                if (roomConflict)
                {
                    ModelState.AddModelError(
                        "",
                        $"Room conflict on {date:dd/MM/yyyy}.");

                    continue;
                }

                var teacherConflict = await _context.Schedules
                    .Include(s => s.Class)
                    .AnyAsync(s =>
                        s.Class != null &&
                        s.Class.TeacherId == selectedClass!.TeacherId &&
                        s.StudyDate.Date == date &&
                        model.StartTime < s.EndTime &&
                        model.EndTime > s.StartTime);

                if (teacherConflict)
                {
                    ModelState.AddModelError(
                        "",
                        $"Teacher conflict on {date:dd/MM/yyyy}.");

                    continue;
                }

                schedulesToCreate.Add(new Schedule
                {
                    ClassId = model.ClassId,
                    RoomId = model.RoomId,
                    StudyDate = date,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    Status = model.Status
                });
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(
                    model.ClassId,
                    model.RoomId);

                return View(model);
            }

            if (schedulesToCreate.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "No schedule sessions could be created.");

                await LoadDropdownsAsync(
                    model.ClassId,
                    model.RoomId);

                return View(model);
            }



            _context.Schedules.AddRange(schedulesToCreate);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{schedulesToCreate.Count} schedule sessions created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Schedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                return NotFound();
            }

            await LoadDropdownsAsync(schedule.ClassId, schedule.RoomId);

            return View(schedule);
        }

        // POST: Schedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Schedule schedule)
        {
            if (id != schedule.ScheduleId)
            {
                return NotFound();
            }

            if (schedule.EndTime <= schedule.StartTime)
            {
                ModelState.AddModelError(
                    "EndTime",
                    "End time must be later than start time.");
            }

            if (ModelState.IsValid)
            {
                var roomConflict = await _context.Schedules
                    .AnyAsync(s =>
                        s.ScheduleId != schedule.ScheduleId &&
                        s.RoomId == schedule.RoomId &&
                        s.StudyDate.Date == schedule.StudyDate.Date &&
                        schedule.StartTime < s.EndTime &&
                        schedule.EndTime > s.StartTime);

                if (roomConflict)
                {
                    ModelState.AddModelError(
                        "",
                        "The selected room is already occupied during this time.");
                }

                var selectedClass = await _context.Classes
    .FirstOrDefaultAsync(c => c.LanguageClassId == schedule.ClassId);

                var selectedRoom = await _context.Rooms
                    .FirstOrDefaultAsync(r => r.RoomId == schedule.RoomId);

                if (selectedClass == null)
                {
                    ModelState.AddModelError(
                        "ClassId",
                        "The selected class does not exist.");
                }
                else
                {
                    var teacherConflict = await _context.Schedules
                        .Include(s => s.Class)
                        .AnyAsync(s =>
                            s.ScheduleId != schedule.ScheduleId &&
                            s.Class != null &&
                            s.Class.TeacherId == selectedClass.TeacherId &&
                            s.StudyDate.Date == schedule.StudyDate.Date &&
                            schedule.StartTime < s.EndTime &&
                            schedule.EndTime > s.StartTime);
                    if (teacherConflict)
                    {
                        ModelState.AddModelError(
                            "",
                            "The teacher of the selected class is already occupied during this time.");
                    }
                }
                if (selectedRoom == null)
                {
                    ModelState.AddModelError(
                        "RoomId",
                        "The selected room does not exist.");
                }
                else if (selectedRoom.Status == "Maintenance" ||
                         selectedRoom.Status == "Inactive")
                {
                    ModelState.AddModelError(
                        "RoomId",
                        "This room is not available for scheduling.");
                }

                if (selectedClass != null &&
                    selectedRoom != null &&
                    selectedClass.MaxStudents > selectedRoom.Capacity)
                {
                    ModelState.AddModelError(
                        "RoomId",
                        $"The room capacity ({selectedRoom.Capacity}) is smaller than the class maximum students ({selectedClass.MaxStudents}).");
                }
            }


            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(
                    schedule.ClassId,
                    schedule.RoomId);

                return View(schedule);
            }

            try
            {
                _context.Update(schedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Schedule updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ScheduleExists(schedule.ScheduleId))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Schedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.Class)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        // POST: Schedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules
                .FindAsync(id);

            if (schedule == null)
            {
                return NotFound();
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Schedule deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdownsAsync(
            int? selectedClassId = null,
            int? selectedRoomId = null)
        {
            var classes = await _context.Classes
                .OrderBy(c => c.ClassName)
                .ToListAsync();

            var rooms = await _context.Rooms
                .Where(r =>
                    r.Status != "Maintenance" &&
                    r.Status != "Inactive")
                .OrderBy(r => r.RoomCode)
                .ToListAsync();

            ViewData["ClassId"] = new SelectList(
                classes,
                "LanguageClassId",
                "ClassName",
                selectedClassId);

            ViewData["RoomId"] = rooms
                .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = r.RoomId.ToString(),
                    Text = $"{r.RoomCode} - {r.RoomName} (Capacity: {r.Capacity})",
                    Selected = r.RoomId == selectedRoomId
                })
                .ToList();
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules
                .Any(s => s.ScheduleId == id);
        }
    }
}