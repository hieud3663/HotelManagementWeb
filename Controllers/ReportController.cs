using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;

namespace HotelManagement.Controllers
{
    public class ReportController : Controller
    {
        private readonly HotelManagementContext _context;

        public ReportController(HotelManagementContext context)
        {
            _context = context;
        }

        // GET: Report/Index
        public async Task<IActionResult> Index()
        {
            // Kiểm tra đăng nhập
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserID")))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Thống kê nhanh cho dashboard báo cáo
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            
            // Doanh thu tháng này
            ViewBag.MonthlyRevenue = await _context.Invoices
                .Where(i => i.InvoiceDate >= firstDayOfMonth)
                .SumAsync(i => i.NetDue ?? 0);
            
            // Check-in hôm nay
            ViewBag.TodayCheckIns = await _context.HistoryCheckins
                .Where(h => h.CheckInDate.Date == today)
                .CountAsync();
            
            // Tổng hóa đơn tháng này
            ViewBag.MonthlyInvoices = await _context.Invoices
                .Where(i => i.InvoiceDate >= firstDayOfMonth)
                .CountAsync();

            return View();
        }

        // GET: Report/Revenue - Báo cáo doanh thu (DFD 6.1)
        public async Task<IActionResult> Revenue(DateTime? fromDate, DateTime? toDate)
        {
            // Kiểm tra đăng nhập
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserID")))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Mặc định: tháng hiện tại
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate ??= fromDate.Value.AddMonths(1).AddDays(-1);

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            // Lấy doanh thu từ hóa đơn
            var invoices = await _context.Invoices
                .Include(i => i.ReservationForm)
                    .ThenInclude(r => r!.Customer)
                .Include(i => i.ReservationForm)
                    .ThenInclude(r => r!.Room)
                        .ThenInclude(rm => rm!.RoomCategory)
                .Where(i => i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate)
                .ToListAsync();

            // Tính toán thống kê
            ViewBag.TotalRevenue = invoices.Sum(i => i.NetDue ?? 0);
            ViewBag.RoomRevenue = invoices.Sum(i => i.RoomCharge);
            ViewBag.ServiceRevenue = invoices.Sum(i => i.ServicesCharge);
            ViewBag.TotalInvoices = invoices.Count;
            ViewBag.AverageRevenue = invoices.Count > 0 ? invoices.Average(i => i.NetDue ?? 0) : 0;

            // Doanh thu theo ngày
            var dailyRevenue = invoices
                .GroupBy(i => i.InvoiceDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(i => i.NetDue ?? 0),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            ViewBag.DailyRevenue = dailyRevenue;

            // Doanh thu theo loại phòng
            var revenueByRoomType = invoices
                .GroupBy(i => i.ReservationForm!.Room!.RoomCategory!.RoomCategoryName)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(i => i.NetDue ?? 0),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            ViewBag.RevenueByRoomType = revenueByRoomType;

            return View(invoices);
        }

        // GET: Report/RoomOccupancy - Báo cáo công suất phòng (DFD 6.2)
        public async Task<IActionResult> RoomOccupancy(DateTime? fromDate, DateTime? toDate)
        {
            // Kiểm tra đăng nhập
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserID")))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Mặc định: tháng hiện tại
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate ??= fromDate.Value.AddMonths(1).AddDays(-1);

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            // Lấy tổng số phòng
            var totalRooms = await _context.Rooms.CountAsync();
            ViewBag.TotalRooms = totalRooms;

            // Lấy lịch sử check-in trong khoảng thời gian
            var checkIns = await _context.HistoryCheckins
                .Include(h => h.ReservationForm)
                    .ThenInclude(r => r!.Room)
                        .ThenInclude(rm => rm!.RoomCategory)
                .Include(h => h.ReservationForm)
                    .ThenInclude(r => r!.Customer)
                .Where(h => h.CheckInDate >= fromDate && h.CheckInDate <= toDate)
                .ToListAsync();

            // Tính toán công suất
            var totalDays = (toDate.Value - fromDate.Value).Days + 1;
            var totalRoomDays = totalRooms * totalDays;
            var occupiedDays = checkIns.Sum(h =>
            {
                var checkout = h.ReservationForm?.CheckOutDate ?? DateTime.Now;
                var days = (checkout - h.CheckInDate).Days;
                return days > 0 ? days : 1;
            });

            ViewBag.OccupancyRate = totalRoomDays > 0 ? (double)occupiedDays / totalRoomDays * 100 : 0;
            ViewBag.TotalCheckIns = checkIns.Count;

            // Công suất theo loại phòng
            var occupancyByRoomType = checkIns
                .GroupBy(h => h.ReservationForm!.Room!.RoomCategory!.RoomCategoryName)
                .Select(g => new
                {
                    RoomType = g.Key,
                    CheckIns = g.Count(),
                    TotalDays = g.Sum(h =>
                    {
                        var checkout = h.ReservationForm?.CheckOutDate ?? DateTime.Now;
                        var days = (checkout - h.CheckInDate).Days;
                        return days > 0 ? days : 1;
                    })
                })
                .OrderByDescending(x => x.CheckIns)
                .ToList();

            ViewBag.OccupancyByRoomType = occupancyByRoomType;

            // Công suất theo ngày
            var dailyOccupancy = checkIns
                .GroupBy(h => h.CheckInDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    CheckIns = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            ViewBag.DailyOccupancy = dailyOccupancy;

            return View(checkIns);
        }

        // GET: Report/EmployeePerformance - Báo cáo hiệu suất nhân viên (DFD 6.3)
        public async Task<IActionResult> EmployeePerformance(DateTime? fromDate, DateTime? toDate)
        {
            // Kiểm tra đăng nhập
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserID")))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Mặc định: tháng hiện tại
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate ??= fromDate.Value.AddMonths(1).AddDays(-1);

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            // Lấy thống kê đặt phòng theo nhân viên
            var reservations = await _context.ReservationForms
                .Include(r => r.Employee)
                .Include(r => r.Customer)
                .Include(r => r.Room)
                .Where(r => r.ReservationDate >= fromDate && r.ReservationDate <= toDate)
                .ToListAsync();

            var employeeStats = reservations
                .Where(r => r.EmployeeID != null)
                .GroupBy(r => new { r.EmployeeID, r.Employee!.FullName, r.Employee.Position })
                .Select(g => new
                {
                    EmployeeID = g.Key.EmployeeID,
                    EmployeeName = g.Key.FullName,
                    Position = g.Key.Position,
                    TotalReservations = g.Count(),
                    TotalDeposit = g.Sum(r => r.RoomBookingDeposit)
                })
                .OrderByDescending(x => x.TotalReservations)
                .ToList();

            ViewBag.EmployeeStats = employeeStats;
            ViewBag.TotalReservations = reservations.Count;
            ViewBag.TopEmployee = employeeStats.FirstOrDefault();

            return View(employeeStats);
        }
    }
}
