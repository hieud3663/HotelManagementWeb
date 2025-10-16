using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;

namespace HotelManagement.Controllers
{
    public class ServiceStatusController : Controller
    {
        private readonly HotelManagementContext _context;
        private readonly ILogger<ServiceStatusController> _logger;

        public ServiceStatusController(
            HotelManagementContext context,
            ILogger<ServiceStatusController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// API kiểm tra trạng thái Background Service
        /// GET: /ServiceStatus/Check
        /// </summary>
        public IActionResult Check()
        {
            if (HttpContext.Session.GetString("UserID") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        /// <summary>
        /// API test chạy thủ công sp_UpdateRoomStatusToReserved
        /// POST: /ServiceStatus/ManualUpdate
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ManualUpdate()
        {
            try
            {
                var startTime = DateTime.Now;
                
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_UpdateRoomStatusToReserved"
                );

                var duration = (DateTime.Now - startTime).TotalMilliseconds;

                var reservedRooms = await _context.Rooms
                    .Where(r => r.RoomStatus == "RESERVED")
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    message = $"✅ Cập nhật thành công trong {duration}ms",
                    reservedRoomCount = reservedRooms,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi cập nhật thủ công");
                return Json(new
                {
                    success = false,
                    message = $"❌ Lỗi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// API lấy danh sách phòng sắp chuyển sang RESERVED
        /// GET: /ServiceStatus/GetUpcomingReservations
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUpcomingReservations()
        {
            try
            {
                var upcomingRooms = await _context.Database
                    .SqlQueryRaw<UpcomingReservationDto>(@"
                        SELECT 
                            r.roomID,
                            r.status AS roomStatus,
                            rf.reservationFormID,
                            rf.checkInDate,
                            c.fullName AS customerName,
                            DATEDIFF(MINUTE, GETDATE(), rf.checkInDate) AS minutesUntilCheckIn,
                            CASE 
                                WHEN DATEDIFF(MINUTE, GETDATE(), rf.checkInDate) <= 300 THEN 'READY'
                                ELSE 'PENDING'
                            END AS updateStatus
                        FROM Room r
                        INNER JOIN ReservationForm rf ON r.roomID = rf.roomID
                        INNER JOIN Customer c ON rf.customerID = c.customerID
                        WHERE rf.status = 'PENDING'
                        AND NOT EXISTS (
                            SELECT 1 FROM HistoryCheckin hc 
                            WHERE hc.reservationFormID = rf.reservationFormID
                        )
                        ORDER BY rf.checkInDate
                    ")
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = upcomingRooms,
                    count = upcomingRooms.Count,
                    readyToUpdate = upcomingRooms.Count(x => x.updateStatus == "READY")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi lấy danh sách upcoming reservations");
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }

    // DTO cho upcoming reservations
    public class UpcomingReservationDto
    {
        public string roomID { get; set; } = string.Empty;
        public string roomStatus { get; set; } = string.Empty;
        public string reservationFormID { get; set; } = string.Empty;
        public DateTime checkInDate { get; set; }
        public string customerName { get; set; } = string.Empty;
        public int minutesUntilCheckIn { get; set; }
        public string updateStatus { get; set; } = string.Empty;
    }
}
