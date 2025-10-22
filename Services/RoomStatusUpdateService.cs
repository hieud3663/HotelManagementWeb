using HotelManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    /// <summary>
    /// Background service tự động cập nhật trạng thái RESERVED cho phòng
    /// khi còn 5 giờ đến check-in. Chạy mỗi 30 phút.
    /// </summary>
    public class RoomStatusUpdateService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<RoomStatusUpdateService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(30);

        public RoomStatusUpdateService(
            IServiceProvider services,
            ILogger<RoomStatusUpdateService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Room Status Update Service đã khởi động");
            _logger.LogInformation($"⏰ Cập nhật mỗi {_updateInterval.TotalMinutes} phút");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateRoomStatusAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Lỗi khi cập nhật trạng thái phòng");
                }

                _logger.LogInformation($"⏳ Đợi {_updateInterval.TotalMinutes} phút đến lần cập nhật tiếp theo...");
                await Task.Delay(_updateInterval, stoppingToken);
            }

            _logger.LogInformation("⛔ Room Status Update Service đã dừng");
        }

        private async Task UpdateRoomStatusAsync()
        {
            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<HotelManagementContext>();

                try
                {
                    var startTime = DateTime.Now;
                    _logger.LogInformation($"🔄 Bắt đầu cập nhật trạng thái phòng lúc {startTime:HH:mm:ss}");

                    await context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_UpdateRoomStatusToReserved"
                    );

                    var endTime = DateTime.Now;
                    var duration = (endTime - startTime).TotalMilliseconds;

                    _logger.LogInformation($"✅ Cập nhật thành công trong {duration}ms");

                    var reservedRooms = await context.Rooms
                        .Where(r => r.RoomStatus == "RESERVED")
                        .CountAsync();

                    _logger.LogInformation($"📊 Tổng số phòng RESERVED: {reservedRooms}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Lỗi khi thực thi sp_UpdateRoomStatusToReserved");
                    throw;
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 Đang dừng Room Status Update Service...");
            await base.StopAsync(stoppingToken);
        }
    }
}
