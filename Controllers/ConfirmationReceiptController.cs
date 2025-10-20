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
    }
}
