using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Rooms
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .OrderBy(r => r.RoomCode)
                .ToListAsync();

            return View(rooms);
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .Include(r => r.Schedules)
                    .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // GET: Rooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            if (await _context.Rooms
                .AnyAsync(r => r.RoomCode == room.RoomCode))
            {
                ModelState.AddModelError(
                    "RoomCode",
                    "A room with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(room);
            }

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Room created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FindAsync(id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // POST: Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Room room)
        {
            if (id != room.RoomId)
            {
                return NotFound();
            }

            if (await _context.Rooms.AnyAsync(r =>
                r.RoomCode == room.RoomCode &&
                r.RoomId != room.RoomId))
            {
                ModelState.AddModelError(
                    "RoomCode",
                    "A room with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(room);
            }

            try
            {
                _context.Update(room);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Room updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomExists(room.RoomId))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Schedules)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            if (room.Schedules.Any())
            {
                TempData["ErrorMessage"] =
                    "This room cannot be deleted because it has schedules assigned to it.";

                return RedirectToAction(nameof(Index));
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Room deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms
                .Any(r => r.RoomId == id);
        }

        // GET: Rooms/Schedule/5
        public async Task<IActionResult> Schedule(
            int? id,
            DateTime? weekStart)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                return NotFound();
            }

            var selectedDate = weekStart?.Date ?? DateTime.Today;

            var monday = selectedDate.AddDays(
                selectedDate.DayOfWeek == DayOfWeek.Sunday
                    ? -6
                    : DayOfWeek.Monday - selectedDate.DayOfWeek);

            var sunday = monday.AddDays(6);

            var schedules = await _context.Schedules
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Teacher)
                .Where(s =>
                    s.RoomId == id &&
                    s.StudyDate.Date >= monday.Date &&
                    s.StudyDate.Date <= sunday.Date)
                .OrderBy(s => s.StudyDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.Room = room;
            ViewBag.Monday = monday;
            ViewBag.Sunday = sunday;

            return View(schedules);
        }
    }
}