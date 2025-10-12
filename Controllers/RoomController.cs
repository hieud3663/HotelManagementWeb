using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class RoomController : Controller
    {
        private readonly HotelManagementContext _context;

        public RoomController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        public async Task<IActionResult> Index()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            var rooms = await _context.Rooms
                .Include(r => r.RoomCategory)
                .Where(r => r.IsActivate == "ACTIVATE")
                .ToListAsync();
            return View(rooms);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.RoomCategory)
                .FirstOrDefaultAsync(m => m.RoomID == id);
            if (room == null) return NotFound();

            return View(room);
        }

        public async Task<IActionResult> Create()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            ViewData["RoomCategoryID"] = new SelectList(
                await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(), 
                "RoomCategoryID", "RoomCategoryName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomCategoryID")] Room room)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                // Lấy thông tin loại phòng để tạo mã phòng
                var roomCategory = await _context.RoomCategories.FindAsync(room.RoomCategoryID);
                if (roomCategory == null)
                {
                    TempData["Error"] = "Loại phòng không tồn tại!";
                    ViewData["RoomCategoryID"] = new SelectList(
                        await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(),
                        "RoomCategoryID", "RoomCategoryName", room.RoomCategoryID);
                    return View(room);
                }

                var prefix = "R";

                if (roomCategory.RoomCategoryName.Contains("VIP", StringComparison.OrdinalIgnoreCase))
                    prefix = "V";
                else if (roomCategory.RoomCategoryName.Contains("Thường", StringComparison.OrdinalIgnoreCase))
                    prefix = "T";
                
                room.RoomID = await _context.GenerateID(prefix, "Room", 4);
                room.IsActivate = "ACTIVATE";
                room.DateOfCreation = DateTime.Now;
                room.RoomStatus = "AVAILABLE";

                _context.Add(room);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Thêm phòng thành công! Mã phòng: {room.RoomID}";
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomCategoryID"] = new SelectList(
                await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(), 
                "RoomCategoryID", "RoomCategoryName", room.RoomCategoryID);
            return View(room);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            
            ViewData["RoomCategoryID"] = new SelectList(
                await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(), 
                "RoomCategoryID", "RoomCategoryName", room.RoomCategoryID);
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("RoomID,RoomStatus,DateOfCreation,RoomCategoryID,IsActivate")] Room room)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id != room.RoomID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật phòng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomID))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomCategoryID"] = new SelectList(
                await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(), 
                "RoomCategoryID", "RoomCategoryName", room.RoomCategoryID);
            return View(room);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.RoomCategory)
                .FirstOrDefaultAsync(m => m.RoomID == id);
            if (room == null) return NotFound();

            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                room.IsActivate = "DEACTIVATE";
                _context.Update(room);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa phòng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(string id)
        {
            return _context.Rooms.Any(e => e.RoomID == id);
        }

        // API để lấy phòng trống theo loại phòng và thời gian
        [HttpGet]
        public async Task<JsonResult> GetAvailableRoomsByCatgory(string categoryId, DateTime checkInDate, DateTime checkOutDate)
        {
            var availableRooms = await _context.Rooms
                .Where(r => r.RoomCategoryID == categoryId && r.RoomStatus == "AVAILABLE" && r.IsActivate == "ACTIVATE")
                .Where(r => !_context.ReservationForms.Any(rf => 
                    rf.RoomID == r.RoomID && 
                    rf.IsActivate == "ACTIVATE" &&
                    ((rf.CheckInDate <= checkInDate && rf.CheckOutDate > checkInDate) ||
                     (rf.CheckInDate < checkOutDate && rf.CheckOutDate >= checkOutDate) ||
                     (rf.CheckInDate >= checkInDate && rf.CheckOutDate <= checkOutDate))))
                .Select(r => new
                {
                    roomID = r.RoomID
                })
                .ToListAsync();

            return Json(availableRooms);
        }

        // API để lấy các tùy chọn giá theo loại phòng
        [HttpGet]
        public async Task<JsonResult> GetPricingOptions(string categoryId)
        {
            var pricings = await _context.Pricings
                .Where(p => p.RoomCategoryID == categoryId)
                .Select(p => new
                {
                    priceUnit = p.PriceUnit,
                    amount = p.Price
                })
                .ToListAsync();

            return Json(pricings);
        }

        // API để lấy phòng trống
        [HttpGet]
        public async Task<JsonResult> GetAvailableRooms(DateTime checkInDate, DateTime checkOutDate)
        {
            var availableRooms = await _context.Rooms
                .Include(r => r.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .Where(r => r.RoomStatus == "AVAILABLE" && r.IsActivate == "ACTIVATE")
                .Where(r => !_context.ReservationForms.Any(rf => 
                    rf.RoomID == r.RoomID && 
                    rf.IsActivate == "ACTIVATE" &&
                    ((rf.CheckInDate <= checkInDate && rf.CheckOutDate > checkInDate) ||
                     (rf.CheckInDate < checkOutDate && rf.CheckOutDate >= checkOutDate) ||
                     (rf.CheckInDate >= checkInDate && rf.CheckOutDate <= checkOutDate))))
                .Select(r => new
                {
                    roomID = r.RoomID,
                    categoryName = r.RoomCategory!.RoomCategoryName,
                    numberOfBed = r.RoomCategory.NumberOfBed,
                    dayPrice = r.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "DAY")!.Price,
                    hourPrice = r.RoomCategory.Pricings!.FirstOrDefault(p => p.PriceUnit == "HOUR")!.Price
                })
                .ToListAsync();

            return Json(availableRooms);
        }
    }
}
