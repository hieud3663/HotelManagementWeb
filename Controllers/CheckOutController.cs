using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class CheckOutController : Controller
    {
        private readonly HotelManagementContext _context;

        public CheckOutController(HotelManagementContext context)
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
            
            // Lấy danh sách phòng đã check-in nhưng chưa check-out
            var checkedInReservations = await _context.HistoryCheckins
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .Where(h => !_context.HistoryCheckOuts.Any(co => co.ReservationFormID == h.ReservationFormID))
                .OrderByDescending(h => h.CheckInDate)
                .ToListAsync();

            return View(checkedInReservations);
        }

        public async Task<IActionResult> Details(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .ThenInclude(rc => rc!.Pricings)
                .Include(r => r.HistoryCheckin)
                .Include(r => r.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationFormID);

            if (reservation == null)
            {
                return NotFound();
            }

            // Tính toán tiền phòng
            var checkInDate = reservation.HistoryCheckin?.CheckInDate ?? reservation.CheckInDate;
            var checkOutDate = DateTime.Now;
            var daysDiff = (checkOutDate - checkInDate).Days;
            var hoursDiff = (checkOutDate - checkInDate).TotalHours;

            var dayPrice = reservation.Room?.RoomCategory?.Pricings?.FirstOrDefault(p => p.PriceUnit == "DAY")?.Price ?? 0;
            var hourPrice = reservation.Room?.RoomCategory?.Pricings?.FirstOrDefault(p => p.PriceUnit == "HOUR")?.Price ?? 0;

            decimal roomCharge = 0;
            if (daysDiff > 0)
            {
                roomCharge = dayPrice * daysDiff;
            }
            else if (hoursDiff > 0)
            {
                roomCharge = hourPrice * (decimal)Math.Ceiling(hoursDiff);
            }

            // Tính tiền dịch vụ
            var servicesCharge = reservation.RoomUsageServices?.Sum(s => s.Quantity * s.UnitPrice) ?? 0;

            // Tính phụ phí trả muộn
            decimal lateFee = 0;
            if (checkOutDate > reservation.CheckOutDate)
            {
                var lateHours = (checkOutDate - reservation.CheckOutDate).TotalHours;
                if (lateHours <= 2)
                {
                    lateFee = hourPrice * (decimal)Math.Ceiling(lateHours);
                }
                else if (lateHours <= 6)
                {
                    lateFee = dayPrice * 0.5m;
                }
                else
                {
                    lateFee = dayPrice;
                }
            }

            var subTotal = roomCharge + servicesCharge + lateFee;
            var taxAmount = subTotal * 0.1m; // VAT 10%
            var totalAmount = subTotal + taxAmount;
            var amountDue = totalAmount - (decimal)reservation.RoomBookingDeposit;

            ViewBag.RoomCharge = Math.Round(roomCharge, 0);
            ViewBag.ServiceCharge = Math.Round(servicesCharge, 0);
            ViewBag.LateFee = Math.Round(lateFee, 0);
            ViewBag.SubTotal = Math.Round(subTotal, 0);
            ViewBag.TaxAmount = Math.Round(taxAmount, 0);
            ViewBag.TotalAmount = Math.Round(totalAmount, 0);
            ViewBag.Deposit = Math.Round((decimal)reservation.RoomBookingDeposit, 0);
            ViewBag.AmountDue = Math.Round(amountDue, 0);

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ProcessCheckOut")]
        public async Task<IActionResult> CheckOut(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");
                
                // Gọi stored procedure để check-out
                // SP sẽ tự động:
                // - Kiểm tra điều kiện (đã check-in, chưa check-out)
                // - Tạo ID cho check-out và invoice
                // - Tính toán phí phòng + dịch vụ + phí trả muộn
                // - Cập nhật trạng thái phòng về AVAILABLE
                // - Tạo hoặc cập nhật invoice
                var result = await _context.CheckOutRoomSP(reservationFormID, employeeID!);

                if (result != null)
                {
                    TempData["Success"] = $"Check-out thành công! {result.CheckoutStatus}";
                    
                    // Lấy invoice vừa tạo để redirect
                    var invoice = await _context.Invoices
                        .FirstOrDefaultAsync(i => i.ReservationFormID == reservationFormID);
                    
                    if (invoice != null)
                    {
                        return RedirectToAction("Invoice", "Invoice", new { id = invoice.InvoiceID });
                    }
                    
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Không thể check-out. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                // Lỗi từ stored procedure
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
