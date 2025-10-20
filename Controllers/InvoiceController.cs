using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;

namespace HotelManagement.Controllers
{
    public class InvoiceController : BaseController
    {
        private readonly HotelManagementContext _context;

        public InvoiceController(HotelManagementContext context)
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

            var invoices = await _context.Invoices
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Room)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return View(invoices);
        }

        public async Task<IActionResult> Invoice(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckin)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckOut)
                .FirstOrDefaultAsync(m => m.InvoiceID == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // Alias for Invoice action to support /Invoice/Details/{id} route
        public async Task<IActionResult> Details(string id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Auth");
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Customer)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.Room)
                .ThenInclude(ro => ro!.RoomCategory)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.RoomUsageServices!)
                .ThenInclude(rus => rus.HotelService)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckin)
                .Include(i => i.ReservationForm)
                .ThenInclude(r => r!.HistoryCheckOut)
                .FirstOrDefaultAsync(m => m.InvoiceID == id);

            if (invoice == null) return NotFound();

            return View("Invoice", invoice);
        }
    }
}