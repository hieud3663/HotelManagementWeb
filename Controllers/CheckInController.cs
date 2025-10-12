using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class CheckInController : Controller
    {
        private readonly HotelManagementContext _context;

        public CheckInController(HotelManagementContext context)
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
            
            // Lấy danh sách phòng đã đặt nhưng chưa check-in
            var pendingReservations = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Where(r => r.IsActivate == "ACTIVATE" && 
                            !_context.HistoryCheckins.Any(h => h.ReservationFormID == r.ReservationFormID))
                .OrderBy(r => r.CheckInDate)
                .ToListAsync();

            return View(pendingReservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var reservation = await _context.ReservationForms
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationFormID);

            if (reservation == null)
            if (reservation == null || reservation.IsActivate == "DEACTIVATE")
            {
                TempData["Error"] = "Không tìm thấy phiếu đặt phòng!";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");
                
                // Gọi stored procedure để check-in
                // SP sẽ tự động kiểm tra điều kiện, tạo ID, cập nhật trạng thái phòng
                var result = await _context.CheckInRoomSP(reservationFormID, employeeID!);

                if (result != null)
                {
                    TempData["Success"] = $"Check-in thành công! Mã check-in: {result.HistoryCheckInID}. {result.CheckinStatus}";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Không thể check-in. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                // Lỗi từ stored procedure (ví dụ: đã check-in rồi, phòng không available)
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CheckedInList()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            // Lấy danh sách phòng đã check-in nhưng chưa check-out
            var checkedInReservations = await _context.HistoryCheckins
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(h => h.Employee)
                .Where(h => !_context.HistoryCheckOuts.Any(co => co.ReservationFormID == h.ReservationFormID))
                .OrderByDescending(h => h.CheckInDate)
                .ToListAsync();

            return View(checkedInReservations);
        }
    }
}
