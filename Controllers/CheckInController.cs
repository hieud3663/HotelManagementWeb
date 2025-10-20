using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class CheckInController : BaseController
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

            // Đánh dấu phiếu đặt phòng quá hạn
            ViewBag.OverdueReservations = pendingReservations
                .Where(r => r.CheckOutDate < DateTime.Now)
                .Select(r => r.ReservationFormID)
                .ToHashSet();

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
                    // Tạo phiếu xác nhận nhận phòng tự động
                    try
                    {
                        var receipt = await _context.CreateConfirmationReceiptSP(
                            "CHECKIN",
                            reservationFormID,
                            null, // Chưa có invoice khi check-in
                            employeeID!
                        );
                        
                        if (receipt != null)
                        {
                            TempData["Success"] = $"Check-in thành công! Mã check-in: {result.HistoryCheckInID}<br/>Phiếu xác nhận: {receipt.ReceiptID}";
                            TempData["ReceiptID"] = receipt.ReceiptID;
                        }
                    }
                    catch (Exception exReceipt)
                    {
                        // Nếu tạo phiếu lỗi thì vẫn báo check-in thành công
                        TempData["Success"] = $"Check-in thành công! Mã check-in: {result.HistoryCheckInID}. {result.CheckinStatus}";
                        TempData["Warning"] = $"Không thể tạo phiếu xác nhận: {exReceipt.Message}";
                    }
                    
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

        // Xóa phiếu đặt phòng trong danh sách chờ check-in (soft delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var reservation = await _context.ReservationForms
                    .FirstOrDefaultAsync(r => r.ReservationFormID == id);

                if (reservation == null)
                {
                    TempData["Error"] = "Không tìm thấy phiếu đặt phòng!";
                    return RedirectToAction("Index");
                }

                // Kiểm tra xem đã check-in chưa
                var hasCheckedIn = await _context.HistoryCheckins
                    .AnyAsync(h => h.ReservationFormID == id);

                if (hasCheckedIn)
                {
                    TempData["Error"] = "Không thể xóa phiếu đặt phòng đã check-in!";
                    return RedirectToAction("Index");
                }

                // Soft delete
                reservation.IsActivate = "DEACTIVATE";
                _context.Update(reservation);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã xóa phiếu đặt phòng {id} thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa phiếu đặt phòng: {ex.Message}";
                return RedirectToAction("Index");
            }
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
