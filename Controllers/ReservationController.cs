using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class ReservationController : Controller
    {
        private readonly HotelManagementContext _context;

        public ReservationController(HotelManagementContext context)
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
            
            var reservations = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(r => r.Employee)
                .Where(r => r.IsActivate == "ACTIVATE")
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
            return View(reservations);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(r => r.Employee)
                .Include(r => r.HistoryCheckin)
                .Include(r => r.HistoryCheckOut)
                .Include(r => r.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .FirstOrDefaultAsync(m => m.ReservationFormID == id);
            
            if (reservation == null) return NotFound();

            return View(reservation);
        }

        public async Task<IActionResult> Create()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.Where(c => c.IsActivate == "ACTIVATE").ToListAsync(), 
                "CustomerID", "FullName");
            
            ViewData["RoomCategories"] = new SelectList(
                await _context.RoomCategories
                    .Where(rc => rc.IsActivate == "ACTIVATE")
                    .ToListAsync(), 
                "RoomCategoryID", "RoomCategoryName");
            
            ViewData["RoomID"] = new SelectList(
                await _context.Rooms
                    .Include(r => r.RoomCategory)
                    .Where(r => r.RoomStatus == "AVAILABLE" && r.IsActivate == "ACTIVATE").ToListAsync(), 
                "RoomID", "RoomID");
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CheckInDate,CheckOutDate,RoomID,CustomerID,RoomBookingDeposit")] ReservationForm reservation)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            reservation.EmployeeID = HttpContext.Session.GetString("EmployeeID")!;

            // Kiểm tra ngày nhận và ngày trả
            if (reservation.CheckInDate < DateTime.Now)
            {
                ModelState.AddModelError("CheckInDate", "Ngày nhận phòng phải lớn hơn hoặc bằng ngày hiện tại.");
            }
            if (reservation.CheckOutDate <= reservation.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Ngày trả phòng phải lớn hơn ngày nhận phòng.");
            }

            if (ModelState.IsValid)
            {
                try
                {

                    // Gọi stored procedure để tạo reservation
                    var result = await _context.CreateReservationSP(
                        reservation.CheckInDate,
                        reservation.CheckOutDate,
                        reservation.RoomID,
                        reservation.CustomerID,
                        reservation.EmployeeID,
                        reservation.RoomBookingDeposit
                    );

                    if (result != null)
                    {
                        TempData["Success"] = $"Đặt phòng thành công! Mã đặt phòng: {result.ReservationFormID}";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "Không thể tạo phiếu đặt phòng. Vui lòng thử lại.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
                }
            }

            // Nếu có lỗi, load lại dữ liệu cho dropdown
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.Where(c => c.IsActivate == "ACTIVATE").ToListAsync(),
                "CustomerID", "FullName", reservation.CustomerID);
            ViewData["RoomCategories"] = new SelectList(
                await _context.RoomCategories.Where(rc => rc.IsActivate == "ACTIVATE").ToListAsync(),
                "RoomCategoryID", "RoomCategoryName");

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            var reservation = await _context.ReservationForms
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.ReservationFormID == id);
            
            if (reservation != null)
            {
                // Kiểm tra đã check-in chưa
                var checkedIn = await _context.HistoryCheckins
                    .AnyAsync(h => h.ReservationFormID == id);
                
                if (checkedIn)
                {
                    TempData["Error"] = "Không thể hủy phiếu đặt phòng đã check-in!";
                    return RedirectToAction(nameof(Details), new { id });
                }

                reservation.IsActivate = "DEACTIVATE";
                _context.Update(reservation);

                // Cập nhật trạng thái phòng về AVAILABLE
                if (reservation.Room != null)
                {
                    reservation.Room.RoomStatus = "AVAILABLE";
                    _context.Update(reservation.Room);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Hủy đặt phòng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> CalculateDeposit(string roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .FirstOrDefaultAsync(r => r.RoomID == roomId);

            if (room == null)
                return Json(new { success = false });

            var daysDiff = (checkOutDate - checkInDate).Days;
            var dayPrice = room.RoomCategory?.Pricings?.FirstOrDefault(p => p.PriceUnit == "DAY")?.Price ?? 0;
            var deposit = dayPrice * daysDiff * 0.3m; // 30% tiền phòng

            return Json(new { success = true, deposit = deposit, totalDays = daysDiff, dayPrice = dayPrice });
        }
    }
}
