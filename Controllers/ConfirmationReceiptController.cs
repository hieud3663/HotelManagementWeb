using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models;

namespace HotelManagement.Controllers
{
    public class ConfirmationReceiptController : BaseController
    {
        private readonly HotelManagementContext _context;

        public ConfirmationReceiptController(HotelManagementContext context)
        {
            _context = context;
        }

        private bool CheckAuth()
        {
            return HttpContext.Session.GetString("UserID") != null;
        }

        // GET: ConfirmationReceipt/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var receipt = await _context.ConfirmationReceipts
                .Include(r => r.ReservationForm)
                    .ThenInclude(rf => rf!.Customer)
                .Include(r => r.ReservationForm)
                    .ThenInclude(rf => rf!.Room)
                        .ThenInclude(ro => ro!.RoomCategory)
                .Include(r => r.Invoice)
                .FirstOrDefaultAsync(m => m.ReceiptID == id);

            if (receipt == null)
            {
                return NotFound();
            }

            return View(receipt);
        }

        // GET: ConfirmationReceipt/List
        public async Task<IActionResult> Index(string? type = null)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");

            var receiptsQuery = _context.ConfirmationReceipts
                .Include(r => r.ReservationForm)
                .OrderByDescending(r => r.IssueDate).AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                receiptsQuery = receiptsQuery.Where(r => r.ReceiptType == type);
            }

            var receipts = await receiptsQuery.ToListAsync();

            ViewBag.FilterType = type;
            return View(receipts);
        }

        // GET: ConfirmationReceipt/Print/Reservation/{reservationId}
        public async Task<IActionResult> PrintReservation(string reservationId)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (string.IsNullOrEmpty(reservationId)) return NotFound();

            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                    .ThenInclude(ro => ro!.RoomCategory)
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationId);

            if (reservation == null) return NotFound();

            // Tạo phiếu xác nhận đặt phòng
            var receipt = new ConfirmationReceipt
            {
                ReceiptID = await _context.GenerateID("CR-", "ConfirmationReceipt"),
                ReceiptType = "RESERVATION",
                IssueDate = DateTime.Now,
                ReservationFormID = reservation.ReservationFormID,
                CustomerName = reservation.Customer?.FullName ?? "",
                CustomerPhone = reservation.Customer?.PhoneNumber ?? "",
                CustomerEmail = reservation.Customer?.Email,
                RoomID = reservation.RoomID ?? "",
                RoomCategoryName = reservation.Room?.RoomCategory?.RoomCategoryName ?? "",
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                PriceUnit = reservation.PriceUnit,
                UnitPrice = reservation.UnitPrice,
                Deposit = (decimal)reservation.RoomBookingDeposit,
                TotalAmount = null, // Sẽ tính sau khi có invoice
                EmployeeName = reservation.Employee?.FullName,
                Notes = $"Phiếu xác nhận đặt phòng - {reservation.ReservationFormID}",
                QrCode = $"RESERVATION_{reservation.ReservationFormID}_{DateTime.Now:yyyyMMddHHmmss}"
            };

            // Lưu phiếu vào database
            _context.ConfirmationReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            return View("Print", receipt);
        }

        // GET: ConfirmationReceipt/Print/CheckIn/{reservationId}
        public async Task<IActionResult> PrintCheckIn(string reservationId)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (string.IsNullOrEmpty(reservationId)) return NotFound();

            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                    .ThenInclude(ro => ro!.RoomCategory)
                .Include(r => r.Employee)
                .Include(r => r.HistoryCheckin)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationId);

            if (reservation == null || reservation.HistoryCheckin == null) return NotFound();

            // Tạo phiếu xác nhận check-in
            var receipt = new ConfirmationReceipt
            {
                ReceiptID = await _context.GenerateID("CR-", "ConfirmationReceipt"),
                ReceiptType = "CHECKIN",
                IssueDate = DateTime.Now,
                ReservationFormID = reservation.ReservationFormID,
                CustomerName = reservation.Customer?.FullName ?? "",
                CustomerPhone = reservation.Customer?.PhoneNumber ?? "",
                CustomerEmail = reservation.Customer?.Email,
                RoomID = reservation.RoomID ?? "",
                RoomCategoryName = reservation.Room?.RoomCategory?.RoomCategoryName ?? "",
                CheckInDate = reservation.HistoryCheckin.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                PriceUnit = reservation.PriceUnit,
                UnitPrice = reservation.UnitPrice,
                Deposit = (decimal)reservation.RoomBookingDeposit,
                TotalAmount = null, // Sẽ tính sau khi có invoice
                EmployeeName = reservation.Employee?.FullName,
                Notes = $"Phiếu xác nhận check-in - {reservation.ReservationFormID}",
                QrCode = $"CHECKIN_{reservation.ReservationFormID}_{DateTime.Now:yyyyMMddHHmmss}"
            };

            // Lưu phiếu vào database
            _context.ConfirmationReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            return View("Print", receipt);
        }

        // GET: ConfirmationReceipt/Print/CheckOut/{reservationId}
        public async Task<IActionResult> PrintCheckOut(string reservationId)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (string.IsNullOrEmpty(reservationId)) return NotFound();

            var reservation = await _context.ReservationForms
                .Include(r => r.Customer)
                .Include(r => r.Room)
                    .ThenInclude(ro => ro!.RoomCategory)
                .Include(r => r.Employee)
                .Include(r => r.HistoryCheckOut)
                .Include(r => r.Invoices)
                .FirstOrDefaultAsync(r => r.ReservationFormID == reservationId);

            if (reservation == null || reservation.HistoryCheckOut == null) return NotFound();

            var invoice = reservation.Invoices?.FirstOrDefault();

            // Tạo phiếu xác nhận check-out
            var receipt = new ConfirmationReceipt
            {
                ReceiptID = await _context.GenerateID("CR-", "ConfirmationReceipt"),
                ReceiptType = "CHECKOUT",
                IssueDate = DateTime.Now,
                ReservationFormID = reservation.ReservationFormID,
                InvoiceID = invoice?.InvoiceID,
                CustomerName = reservation.Customer?.FullName ?? "",
                CustomerPhone = reservation.Customer?.PhoneNumber ?? "",
                CustomerEmail = reservation.Customer?.Email,
                RoomID = reservation.RoomID ?? "",
                RoomCategoryName = reservation.Room?.RoomCategory?.RoomCategoryName ?? "",
                CheckInDate = reservation.HistoryCheckin?.CheckInDate ?? reservation.CheckInDate,
                CheckOutDate = reservation.HistoryCheckOut.CheckOutDate,
                PriceUnit = reservation.PriceUnit,
                UnitPrice = reservation.UnitPrice,
                Deposit = (decimal)reservation.RoomBookingDeposit,
                TotalAmount = invoice?.NetDue,
                EmployeeName = reservation.Employee?.FullName,
                Notes = $"Phiếu xác nhận check-out - {reservation.ReservationFormID}",
                QrCode = $"CHECKOUT_{reservation.ReservationFormID}_{DateTime.Now:yyyyMMddHHmmss}"
            };

            // Lưu phiếu vào database
            _context.ConfirmationReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            return View("Print", receipt);
        }
    }
}
