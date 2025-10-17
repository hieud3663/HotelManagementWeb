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

        // Helper method: Tính phí theo nấc bậc thang cho giá GIỜ
        private decimal CalculateHourlyFee(double totalMinutes, decimal hourlyRate)
        {
            if (totalMinutes <= 0) return 0;

            var totalHours = Math.Ceiling(totalMinutes / 60.0);
            decimal totalFee = 0;

            // 2 giờ đầu: 100% giá
            var first2Hours = Math.Min(totalHours, 2);
            totalFee += (decimal)first2Hours * hourlyRate;

            // Từ giờ 3-6: 80% giá
            if (totalHours > 2)
            {
                var next4Hours = Math.Min(totalHours - 2, 4);
                totalFee += (decimal)next4Hours * hourlyRate * 0.8m;
            }

            // Từ giờ thứ 7 trở đi: Chuyển sang tính theo ngày (nếu cần)
            // Ở đây ta vẫn tính 80% cho đơn giản
            if (totalHours > 6)
            {
                var remainingHours = totalHours - 6;
                totalFee += (decimal)remainingHours * hourlyRate * 0.8m;
            }

            return totalFee;
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
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.Invoices) // Thêm Invoices để kiểm tra trạng thái thanh toán
                .Include(h => h.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckOut) // Thêm HistoryCheckOut để kiểm tra đã checkout chưa
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

            // **LOGIC MỚI**: Tính phí check-in sớm và check-out muộn theo quy định thực tế
            var actualCheckInDate = reservation.HistoryCheckin?.CheckInDate ?? reservation.CheckInDate;
            var expectedCheckInDate = reservation.CheckInDate;
            var actualCheckOutDate = DateTime.Now;
            var expectedCheckOutDate = reservation.CheckOutDate;
            
            // Sử dụng giá đã lưu từ lúc đặt phòng
            var unitPrice = reservation.UnitPrice;
            var priceUnit = reservation.PriceUnit;

            // Lấy giá theo ngày để tính phí phụ thu
            var dayPrice = await _context.Pricings
                .Where(p => p.RoomCategoryID == reservation.Room!.RoomCategoryID && p.PriceUnit == "DAY")
                .Select(p => p.Price)
                .FirstOrDefaultAsync();

            var hourPrice = await _context.Pricings
                .Where(p => p.RoomCategoryID == reservation.Room!.RoomCategoryID && p.PriceUnit == "HOUR")
                .Select(p => p.Price)
                .FirstOrDefaultAsync();

            if (dayPrice == 0)
            {
                dayPrice = unitPrice; // Fallback nếu không có giá theo ngày
            }
            if (hourPrice == 0)
            {
                hourPrice = unitPrice; // Fallback nếu không có giá theo giờ
            }

            decimal roomCharge = 0;
            decimal timeUnits = 0;
            decimal earlyCheckinFee = 0;
            decimal lateCheckoutFee = 0;

            // Bước 1: Tính tiền phòng CHUẨN (expectedCheckIn → expectedCheckOut)
            if (priceUnit == "DAY")
            {
                var bookingMinutes = (expectedCheckOutDate - expectedCheckInDate).TotalMinutes;
                timeUnits = (decimal)Math.Ceiling(bookingMinutes / 1440.0); // 1440 phút = 1 ngày
                if (timeUnits < 1) timeUnits = 1;
                roomCharge = unitPrice * timeUnits;
            }
            else // HOUR
            {
                var bookingMinutes = (expectedCheckOutDate - expectedCheckInDate).TotalMinutes;
                timeUnits = (decimal)Math.Ceiling(bookingMinutes / 60.0);
                if (timeUnits < 1) timeUnits = 1;
                roomCharge = unitPrice * timeUnits;
            }

            // Bước 2: Tính PHÍ CHECK-IN SỚM (nếu check-in thực tế < dự kiến)
            if (actualCheckInDate < expectedCheckInDate)
            {
                var earlyMinutes = (expectedCheckInDate - actualCheckInDate).TotalMinutes;
                int freeMinutes = (priceUnit == "HOUR") ? 30 : 60;

                if (earlyMinutes > freeMinutes)
                {
                    var chargeableMinutes = earlyMinutes - freeMinutes;

                    if (priceUnit == "HOUR")
                    {
                        // Giá GIỜ: Tính theo nấc bậc thang
                        earlyCheckinFee = CalculateHourlyFee(chargeableMinutes, hourPrice);
                    }
                    else // DAY
                    {
                        // Giá NGÀY: Tính theo khung giờ (tích lũy)
                        decimal totalFee = 0;
                        DateTime currentTime = actualCheckInDate;
                        DateTime endTime = expectedCheckInDate;

                        while (currentTime < endTime)
                        {
                            int hour = currentTime.Hour;
                            decimal surchargeRate = 0;

                            // Xác định mức phí theo khung giờ
                            if (hour >= 5 && hour < 9)
                                surchargeRate = 0.5m;
                            else if (hour >= 9 && hour < 14)
                                surchargeRate = 0.3m;

                            // Tính biên của khung giờ hiện tại
                            DateTime bracketEnd;
                            if (hour >= 5 && hour < 9)
                                bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 9, 0, 0);
                            else if (hour >= 9 && hour < 14)
                                bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 14, 0, 0);
                            else
                                bracketEnd = currentTime.AddHours(1); // Vượt khung miễn phí

                            // Tính phút trong khung giờ này
                            DateTime actualBracketEnd = bracketEnd < endTime ? bracketEnd : endTime;
                            var minutesInBracket = (actualBracketEnd - currentTime).TotalMinutes;

                            // Cộng phí cho khung giờ này
                            if (surchargeRate > 0)
                            {
                                totalFee += (decimal)(minutesInBracket / 1440.0) * dayPrice * surchargeRate;
                            }

                            currentTime = actualBracketEnd;
                        }

                        earlyCheckinFee = totalFee;
                    }
                }
            }

            // Bước 3: Tính PHÍ CHECK-OUT MUỘN (nếu check-out thực tế > dự kiến)
            if (actualCheckOutDate > expectedCheckOutDate)
            {
                var lateMinutes = (actualCheckOutDate - expectedCheckOutDate).TotalMinutes;
                int freeMinutes = (priceUnit == "HOUR") ? 30 : 60;

                if (lateMinutes > freeMinutes)
                {
                    var chargeableMinutes = lateMinutes - freeMinutes;

                    if (priceUnit == "HOUR")
                    {
                        // Giá GIỜ: Tính theo nấc bậc thang
                        lateCheckoutFee = CalculateHourlyFee(chargeableMinutes, hourPrice);
                    }
                    else // DAY
                    {
                        // Giá NGÀY: Tính theo khung giờ (tích lũy)
                        decimal totalFee = 0;
                        DateTime currentTime = expectedCheckOutDate.AddMinutes(freeMinutes);
                        DateTime endTime = actualCheckOutDate;

                        while (currentTime < endTime)
                        {
                            int hour = currentTime.Hour;
                            decimal surchargeRate = 0;

                            // Xác định mức phí theo khung giờ
                            if (hour >= 12 && hour < 15)
                                surchargeRate = 0.3m;
                            else if (hour >= 15 && hour < 18)
                                surchargeRate = 0.5m;
                            else if (hour >= 18)
                                surchargeRate = 1.0m;

                            // Tính biên của khung giờ hiện tại
                            DateTime bracketEnd;
                            if (hour >= 12 && hour < 15)
                                bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 15, 0, 0);
                            else if (hour >= 15 && hour < 18)
                                bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 18, 0, 0);
                            else if (hour >= 18)
                                bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 23, 59, 59);
                            else
                                bracketEnd = currentTime.AddHours(1); // Vượt khung miễn phí

                            // Tính phút trong khung giờ này
                            DateTime actualBracketEnd = bracketEnd < endTime ? bracketEnd : endTime;
                            var minutesInBracket = (actualBracketEnd - currentTime).TotalMinutes;

                            // Cộng phí cho khung giờ này
                            if (surchargeRate > 0)
                            {
                                totalFee += (decimal)(minutesInBracket / 1440.0) * dayPrice * surchargeRate;
                            }

                            currentTime = actualBracketEnd;
                        }

                        lateCheckoutFee = totalFee;
                    }
                }
            }

            var ActualDuration = 0.0;
            if (priceUnit == "HOUR")
                ActualDuration = Math.Ceiling((DateTime.Now - actualCheckInDate).TotalHours);
            else
                ActualDuration = Math.Ceiling((DateTime.Now - actualCheckInDate).TotalDays);
            
            // Pass data for real-time calculation in view
            ViewBag.UnitPrice = unitPrice;
            ViewBag.PriceUnit = priceUnit;
            ViewBag.TimeUnits = timeUnits;
            ViewBag.DayPrice = dayPrice;
            ViewBag.ActualCheckInDate = actualCheckInDate;
            ViewBag.ExpectedCheckInDate = expectedCheckInDate;
            ViewBag.ActualCheckOutDate = actualCheckOutDate;
            ViewBag.ExpectedCheckOutDate = expectedCheckOutDate;
            ViewBag.EarlyCheckinFee = earlyCheckinFee;
            ViewBag.LateCheckoutFee = lateCheckoutFee;
            ViewBag.ActualDuration = ActualDuration;

            // Tính tiền dịch vụ
            var servicesCharge = reservation.RoomUsageServices?.Sum(s => s.Quantity * s.UnitPrice) ?? 0;
            var subTotal = roomCharge + servicesCharge + earlyCheckinFee + lateCheckoutFee;
            var taxAmount = subTotal * 0.1m; // VAT 10%
            var totalAmount = subTotal + taxAmount;
            var amountDue = totalAmount - (decimal)reservation.RoomBookingDeposit;

            ViewBag.RoomCharge = Math.Round(roomCharge, 0);
            ViewBag.ServiceCharge = Math.Round(servicesCharge, 0);
            ViewBag.EarlyCheckinFee = Math.Round(earlyCheckinFee, 0);
            ViewBag.LateCheckoutFee = Math.Round(lateCheckoutFee, 0);
            ViewBag.SubTotal = Math.Round(subTotal, 0);
            ViewBag.TaxAmount = Math.Round(taxAmount, 0);
            ViewBag.TotalAmount = Math.Round(totalAmount, 0);
            ViewBag.Deposit = Math.Round((decimal)reservation.RoomBookingDeposit, 0);
            ViewBag.AmountDue = Math.Round(amountDue, 0);

            return View(reservation);
        }

        // LUỒNG 1: TRẢ PHÒNG VÀ THANH TOÁN (Checkout Then Pay)
        // Bước 1: Trả phòng và tạo hóa đơn chưa thanh toán (tính theo thời gian THỰC TẾ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutThenPay(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");
                
                // Gọi SP để checkout và tạo invoice CHƯA THANH TOÁN
                // SP tính tiền dựa trên thời gian THỰC TẾ (actual checkout)
                var result = await _context.CreateInvoice_CheckoutThenPay(reservationFormID, employeeID!);

                if (result != null && result.Status == "SUCCESS")
                {
                    TempData["Success"] = "Trả phòng thành công! Vui lòng thanh toán hóa đơn.";
                    return RedirectToAction("Payment", new { invoiceID = result.InvoiceID });
                }
                else
                {
                    TempData["Error"] = result?.Status ?? "Không thể trả phòng. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Bước 2: Hiển thị trang thanh toán cho invoice chưa thanh toán
        public async Task<IActionResult> Payment(string invoiceID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var invoice = await _context.Invoices
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceID);

            if (invoice == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            if (invoice.IsPaid)
            {
                TempData["Warning"] = "Hóa đơn này đã được thanh toán.";
                return RedirectToAction("Details", "Invoice", new { id = invoiceID });
            }

            return View(invoice);
        }

        // Bước 3: Xác nhận thanh toán cho invoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(string invoiceID, string paymentMethod)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");
                
                // Gọi SP để xác nhận thanh toán
                // SP sẽ cập nhật isPaid = 1, paymentDate, paymentMethod
                // và chuyển trạng thái phòng từ UNAVAILABLE → AVAILABLE
                var result = await _context.ConfirmPaymentSP(invoiceID, paymentMethod, employeeID!);

                if (result != null && result.Status == "PAYMENT_CONFIRMED")
                {
                    TempData["Success"] = "Thanh toán thành công! Phòng đã được giải phóng.";
                    return RedirectToAction("Details", "Invoice", new { id = invoiceID });
                }
                else
                {
                    TempData["Error"] = result?.Status ?? "Không thể xác nhận thanh toán. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction("Payment", new { invoiceID });
        }

        // LUỒNG 2: THANH TOÁN TRƯỚC (Pay Then Checkout)
        // Bước 1: Thanh toán ngay (tính theo thời gian DỰ KIẾN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayThenCheckout(string reservationFormID, string paymentMethod)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");
                
                // Gọi SP để tạo invoice ĐÃ THANH TOÁN
                // SP tính tiền dựa trên thời gian DỰ KIẾN (expected checkout)
                // Phòng vẫn giữ trạng thái ON_USE
                var result = await _context.CreateInvoice_PayThenCheckout(reservationFormID, employeeID!, paymentMethod);

                if (result != null && result.Status == "SUCCESS")
                {
                    TempData["Success"] = $"Thanh toán trước thành công! Khách có thể ở đến {result.PaymentDate:dd/MM/yyyy HH:mm}.";
                    return RedirectToAction("Details", "Invoice", new { id = result.InvoiceID });
                }
                else
                {
                    TempData["Error"] = result?.Status ?? "Không thể thanh toán trước. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Bước 2: Trả phòng thực tế sau khi đã thanh toán trước
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualCheckout(string reservationFormID)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            try
            {
                var employeeID = HttpContext.Session.GetString("EmployeeID");
                
                // Gọi SP để checkout thực tế
                // SP sẽ tính phí phụ thu nếu checkout muộn hơn dự kiến
                var result = await _context.ActualCheckout_AfterPrepayment(reservationFormID, employeeID!);

                if (result != null)
                {
                    if (result.AdditionalCharge > 0)
                    {
                        TempData["Warning"] = $"Trả phòng muộn! Phí phụ thu: {result.AdditionalCharge:N0} VNĐ. Vui lòng thanh toán bổ sung.";
                        return RedirectToAction("Payment", new { invoiceID = result.InvoiceID });
                    }
                    else
                    {
                        TempData["Success"] = "Trả phòng đúng giờ! Cảm ơn quý khách.";
                        return RedirectToAction("Details", "Invoice", new { id = result.InvoiceID });
                    }
                }
                else
                {
                    TempData["Error"] = "Không thể trả phòng. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // LEGACY: Giữ lại action cũ để tương thích ngược (nếu cần)
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
