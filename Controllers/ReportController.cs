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

            // Thống kê hôm nay
            ViewBag.TodayReservations = await _context.ReservationForms
                .Where(r => r.ReservationDate.Date == today)
                .CountAsync();

            ViewBag.TodayCheckOuts = await _context.HistoryCheckOuts
                .Where(h => h.CheckOutDate.Date == today)
                .CountAsync();

            ViewBag.TodayRevenue = await _context.Invoices
                .Where(i => i.InvoiceDate.Date == today)
                .SumAsync(i => (decimal?)i.NetDue) ?? 0;

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
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            // Đảm bảo toDate là cuối ngày
            toDate = toDate.Value.Date.AddDays(1).AddSeconds(-1);

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.Date.ToString("yyyy-MM-dd");

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
                .Where(i => i.ReservationForm?.Room?.RoomCategory != null)
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

        // API: GetRevenueChartData - Dữ liệu biểu đồ doanh thu theo tháng
        [HttpGet]
        public async Task<JsonResult> GetRevenueChartData(int months = 6)
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-months);

            var invoices = await _context.Invoices
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
                .ToListAsync();

            var monthlyRevenue = invoices
                .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new
                {
                    Month = $"T{g.Key.Month}/{g.Key.Year}",
                    Revenue = g.Sum(i => i.NetDue ?? 0),
                    RoomRevenue = g.Sum(i => i.RoomCharge),
                    ServiceRevenue = g.Sum(i => i.ServicesCharge)
                })
                .OrderBy(x => x.Month)
                .ToList();

            var labels = monthlyRevenue.Select(m => m.Month).ToList();
            var revenueData = monthlyRevenue.Select(m => m.Revenue).ToList();
            var roomRevenueData = monthlyRevenue.Select(m => m.RoomRevenue).ToList();
            var serviceRevenueData = monthlyRevenue.Select(m => m.ServiceRevenue).ToList();

            return Json(new
            {
                labels,
                datasets = new[]
                {
                    new { label = "Tổng doanh thu", data = revenueData, backgroundColor = "#4e73df" },
                    new { label = "Doanh thu phòng", data = roomRevenueData, backgroundColor = "#1cc88a" },
                    new { label = "Doanh thu dịch vụ", data = serviceRevenueData, backgroundColor = "#36b9cc" }
                }
            });
        }

        // API: GetRoomOccupancyChartData - Dữ liệu biểu đồ công suất phòng (Pie chart)
        [HttpGet]
        public async Task<JsonResult> GetRoomOccupancyChartData()
        {
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var checkIns = await _context.HistoryCheckins
                .Include(h => h.ReservationForm)
                    .ThenInclude(r => r!.Room)
                        .ThenInclude(rm => rm!.RoomCategory)
                .Where(h => h.CheckInDate >= firstDayOfMonth && h.CheckInDate <= lastDayOfMonth)
                .ToListAsync();

            var occupancyByRoomType = checkIns
                .Where(h => h.ReservationForm?.Room?.RoomCategory != null)
                .GroupBy(h => h.ReservationForm!.Room!.RoomCategory!.RoomCategoryName)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var labels = occupancyByRoomType.Select(o => o.RoomType).ToList();
            var data = occupancyByRoomType.Select(o => o.Count).ToList();

            // Màu sắc cho từng loại phòng
            var backgroundColors = new[] { "#4e73df", "#1cc88a", "#36b9cc", "#f6c23e", "#e74a3b", "#858796" };

            return Json(new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = "Số lượt check-in",
                        data,
                        backgroundColor = backgroundColors.Take(labels.Count).ToArray()
                    }
                }
            });
        }

        // API: GetBookingTrendChartData - Dữ liệu biểu đồ xu hướng đặt phòng (Line chart)
        [HttpGet]
        public async Task<JsonResult> GetBookingTrendChartData(int days = 30)
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-days);

            var reservations = await _context.ReservationForms
                .Where(r => r.ReservationDate >= startDate && r.ReservationDate <= endDate)
                .ToListAsync();

            var dailyBookings = reservations
                .GroupBy(r => r.ReservationDate.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("dd/MM"),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Tạo danh sách đầy đủ các ngày (bao gồm cả ngày không có booking)
            var allDates = Enumerable.Range(0, days + 1)
                .Select(i => startDate.AddDays(i))
                .Select(date => new
                {
                    Date = date.ToString("dd/MM"),
                    Count = dailyBookings.FirstOrDefault(d => d.Date == date.ToString("dd/MM"))?.Count ?? 0
                })
                .ToList();

            var labels = allDates.Select(d => d.Date).ToList();
            var data = allDates.Select(d => d.Count).ToList();

            return Json(new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = "Số lượt đặt phòng",
                        data,
                        borderColor = "#4e73df",
                        backgroundColor = "rgba(78, 115, 223, 0.1)",
                        fill = true,
                        tension = 0.4
                    }
                }
            });
        }

        // API: GetEmployeePerformanceChartData - Dữ liệu biểu đồ hiệu suất nhân viên (Bar chart)
        [HttpGet]
        public async Task<JsonResult> GetEmployeePerformanceChartData(int topN = 10)
        {
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Lấy số lượt check-in theo nhân viên
            var checkInsByEmployee = await _context.HistoryCheckins
                .Include(h => h.Employee)
                .Where(h => h.CheckInDate >= firstDayOfMonth && h.CheckInDate <= lastDayOfMonth)
                .GroupBy(h => new { h.EmployeeID, h.Employee!.FullName })
                .Select(g => new
                {
                    EmployeeName = g.Key.FullName,
                    CheckIns = g.Count()
                })
                .OrderByDescending(x => x.CheckIns)
                .Take(topN)
                .ToListAsync();

            var labels = checkInsByEmployee.Select(e => e.EmployeeName).ToList();
            var data = checkInsByEmployee.Select(e => e.CheckIns).ToList();

            return Json(new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = "Số lượt check-in",
                        data,
                        backgroundColor = "#4e73df",
                        borderColor = "#2e59d9",
                        borderWidth = 1
                    }
                }
            });
        }
    }
}
