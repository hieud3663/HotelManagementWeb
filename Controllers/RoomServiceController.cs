using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class RoomServiceController : BaseController
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

            ViewBag.ReservationFormID = reservationFormID;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(string reservationFormID, string hotelServiceId, int quantity)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(hotelServiceId) || quantity <= 0)
            {
                TempData["Error"] = "Vui lòng chọn dịch vụ và nhập số lượng hợp lệ!";
                return RedirectToAction(nameof(Index), new { reservationFormID });
            }

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");
                
                var result = await _context.AddRoomServiceSP(
                    reservationFormID,
                    hotelServiceId,
                    quantity,
                    employeeID!
                );

                if (result != null)
                {
                    if (result.Action == "UPDATED")
                    {
                        TempData["Success"] = $" Cập nhật dịch vụ thành công! " +
                            $"SL cũ: {result.PreviousQuantity} → SL mới: {result.Quantity} (+{result.QuantityAdded}). " +
                            $"Tổng tiền: {result.TotalPrice:N0} VNĐ";
                    }
                    else
                    {
                        TempData["Success"] = $" Thêm dịch vụ mới thành công! " +
                            $"SL: {result.Quantity}. Tổng tiền: {result.TotalPrice:N0} VNĐ";
                    }
                }
                else
                {
                    TempData["Error"] = "Không thể thêm dịch vụ. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index), new { reservationFormID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateService(string id, int quantity, string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0!";
                return RedirectToAction(nameof(Index), new { reservationFormID });
            }

            var roomService = await _context.RoomUsageServices.FindAsync(id);
            if (roomService != null)
            {
                roomService.Quantity = quantity;
                _context.Update(roomService);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật số lượng thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy dịch vụ!";
            }

            return RedirectToAction(nameof(Index), new { reservationFormID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(string id, string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                // Sử dụng stored procedure để xóa dịch vụ
                var result = await _context.DeleteRoomServiceSP(id);
                
                if (result != null)
                {
                    TempData["Success"] = $"Đã xóa dịch vụ: {result.ServiceName} (SL: {result.Quantity}, Tổng: {result.TotalPrice:N0} VNĐ)";
                }
                else
                {
                    TempData["Error"] = "Không thể xóa dịch vụ. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
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
