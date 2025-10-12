using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class RoomServiceController : Controller
    {
        private readonly HotelManagementContext _context;

        public RoomServiceController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        public async Task<IActionResult> Index(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationFormID);

            if (reservation == null)
            {
                return NotFound();
            }

            var services = await _context.RoomUsageServices
                .Include(r => r.HotelService)
                .ThenInclude(hs => hs!.ServiceCategory)
                .Include(r => r.Employee)
                .Where(r => r.ReservationFormID == reservationFormID)
                .OrderByDescending(r => r.DateAdded)
                .ToListAsync();

            ViewBag.ReservationForm = reservation;
            ViewBag.ServiceCategories = await _context.ServiceCategories
                .Where(sc => sc.IsActivate == "ACTIVATE")
                .ToListAsync();
            ViewBag.HotelServices = await _context.HotelServices
                .Include(hs => hs.ServiceCategory)
                .Where(hs => hs.IsActivate == "ACTIVATE")
                .ToListAsync();
            
            return View(services);
        }

        public async Task<IActionResult> Create(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationFormID);

            if (reservation == null)
            {
                return NotFound();
            }

            // Kiểm tra đã check-in chưa
            var checkedIn = await _context.HistoryCheckins
                .AnyAsync(h => h.ReservationFormID == reservationFormID);

            if (!checkedIn)
            {
                TempData["Error"] = "Chỉ có thể thêm dịch vụ cho phòng đã check-in!";
                return RedirectToAction("Index", "Reservation");
            }

            // Kiểm tra đã check-out chưa
            var checkedOut = await _context.HistoryCheckOuts
                .AnyAsync(h => h.ReservationFormID == reservationFormID);

            if (checkedOut)
            {
                TempData["Error"] = "Không thể thêm dịch vụ cho phòng đã check-out!";
                return RedirectToAction("Index", "Reservation");
            }

            ViewBag.ReservationForm = reservation;
            ViewData["HotelServiceId"] = new SelectList(
                await _context.HotelServices.Where(s => s.IsActivate == "ACTIVATE").ToListAsync(), 
                "HotelServiceId", "ServiceName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("AddService")]
        public async Task<IActionResult> Create(string reservationFormID, [Bind("HotelServiceId,Quantity")] RoomUsageService roomService)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                // Lấy giá dịch vụ từ database
                var service = await _context.HotelServices.FindAsync(roomService.HotelServiceId);
                if (service == null)
                {
                    TempData["Error"] = "Dịch vụ không tồn tại!";
                    return RedirectToAction(nameof(Index), new { reservationFormID });
                }

                // Sử dụng fn_GenerateID từ database
                roomService.RoomUsageServiceId = await _context.GenerateID("RUS-", "RoomUsageService");
                roomService.ReservationFormID = reservationFormID;
                roomService.DateAdded = DateTime.Now;
                roomService.UnitPrice = service.ServicePrice;
                roomService.EmployeeID = HttpContext.Session.GetString("EmployeeID");

                _context.Add(roomService);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm dịch vụ thành công!";
                return RedirectToAction(nameof(Index), new { reservationFormID });
            }

            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationFormID);

            ViewBag.ReservationForm = reservation;
            ViewData["HotelServiceId"] = new SelectList(
                await _context.HotelServices.Where(s => s.IsActivate == "ACTIVATE").ToListAsync(), 
                "HotelServiceId", "ServiceName", roomService.HotelServiceId);

            return View(roomService);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var roomService = await _context.RoomUsageServices.FindAsync(id);
            if (roomService != null)
            {
                _context.RoomUsageServices.Remove(roomService);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa dịch vụ thành công!";
            }

            return RedirectToAction(nameof(Index), new { reservationFormID });
        }

        [HttpGet]
        public async Task<JsonResult> GetServicePrice(string serviceId)
        {
            var service = await _context.HotelServices.FindAsync(serviceId);
            if (service != null)
            {
                return Json(new { success = true, price = service.ServicePrice });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public async Task<JsonResult> GetServicesByCategory(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId))
            {
                return Json(new List<object>());
            }

            var services = await _context.HotelServices
                .Where(s => s.ServiceCategoryID == categoryId && s.IsActivate == "ACTIVATE")
                .Select(s => new
                {
                    hotelServiceId = s.HotelServiceId,
                    hotelServiceName = s.ServiceName,
                    servicePrice = s.ServicePrice
                })
                .ToListAsync();

            return Json(services);
        }
    }
}
