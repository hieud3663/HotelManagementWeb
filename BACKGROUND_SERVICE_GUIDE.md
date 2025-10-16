# Background Service Auto-Update RESERVED Status

## 📋 Tổng quan

Vì SQL Server Express **không hỗ trợ SQL Agent Job**, chúng ta đã implement **ASP.NET Background Service** để tự động cập nhật trạng thái phòng RESERVED mỗi 30 phút.

---

## ✅ Giải pháp đã implement

### 1. Background Service

**File**: `Services/RoomStatusUpdateService.cs`

**Chức năng**:
- Chạy tự động khi ứng dụng khởi động
- Cập nhật trạng thái phòng sang RESERVED khi còn ≤ 5 giờ đến check-in
- Chạy mỗi **30 phút** (có thể tùy chỉnh)
- Ghi log chi tiết vào Console/Logger

**Cách hoạt động**:
```csharp
// Service chạy vô hạn trong background
while (!stoppingToken.IsCancellationRequested)
{
    // 1. Gọi stored procedure
    await ExecuteSqlRawAsync("EXEC sp_UpdateRoomStatusToReserved");
    
    // 2. Log kết quả
    logger.LogInformation("✅ Cập nhật thành công");
    
    // 3. Đợi 30 phút
    await Task.Delay(TimeSpan.FromMinutes(30));
}
```

**Ưu điểm**:
- ✅ Hoạt động với mọi phiên bản SQL Server (bao gồm Express)
- ✅ Tích hợp sẵn trong ứng dụng, không cần cấu hình bên ngoài
- ✅ Tự động khởi động khi app chạy
- ✅ Ghi log chi tiết, dễ debug
- ✅ Có thể điều chỉnh tần suất dễ dàng

**Nhược điểm**:
- ⚠️ Chỉ chạy khi ứng dụng đang chạy
- ⚠️ Nếu restart app, service sẽ restart

---

## 🔧 Cấu hình

### Thay đổi tần suất cập nhật

**File**: `Services/RoomStatusUpdateService.cs`

```csharp
// Dòng 14: Thay đổi interval
private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(30); // Mặc định 30 phút

// Các giá trị gợi ý:
// 15 phút: TimeSpan.FromMinutes(15)
// 1 giờ: TimeSpan.FromHours(1)
// 10 phút: TimeSpan.FromMinutes(10)
```

### Thay đổi thời gian đợi khi khởi động

```csharp
// Dòng 32: Delay khi app start
await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Mặc định 10 giây
```

---

## 📊 Monitoring Service

### Trang Quản lý Service

**URL**: `/ServiceStatus/Check`

**Chức năng**:
1. **Hiển thị trạng thái service**: RUNNING / STOPPED
2. **Thống kê**:
   - Tổng số reservation đang chờ
   - Số phòng sẵn sàng chuyển sang RESERVED (còn ≤ 5h)
   - Thời gian cập nhật gần nhất
3. **Danh sách reservation**: Bảng chi tiết với countdown
4. **Manual Actions**:
   - Nút "Chạy cập nhật ngay": Test thủ công
   - Nút "Làm mới dữ liệu": Reload thông tin
5. **Activity Log**: Lịch sử hoạt động real-time

**Screenshot minh họa**:
```
┌─────────────────────────────────────────────┐
│ Background Service Monitor                  │
├─────────────────────────────────────────────┤
│ Room Status Update Service   [✓ RUNNING]   │
│ Chức năng: Tự động cập nhật...              │
│ Tần suất: Mỗi 30 phút                       │
├─────────────────────────────────────────────┤
│ [Total: 5] [Ready: 2] [Last: 14:30:25]     │
├─────────────────────────────────────────────┤
│ [▶ Chạy cập nhật ngay] [🔄 Làm mới]        │
├─────────────────────────────────────────────┤
│ Upcoming Reservations:                      │
│ Phòng | Khách   | Check-in | Còn lại | ... │
│ P101  | Nguyễn  | 15:00    | 4h 30m  | ... │
│ P203  | Trần    | 16:30    | 6h 0m   | ... │
└─────────────────────────────────────────────┘
```

---

## 🚀 Cách sử dụng

### 1. Khởi động ứng dụng

```powershell
# Chạy ứng dụng
dotnet run
```

**Console log sẽ hiển thị**:
```
info: HotelManagement.Services.RoomStatusUpdateService[0]
      🚀 Room Status Update Service đã khởi động
info: HotelManagement.Services.RoomStatusUpdateService[0]
      ⏰ Cập nhật mỗi 30 phút
info: HotelManagement.Services.RoomStatusUpdateService[0]
      🔄 Bắt đầu cập nhật trạng thái phòng lúc 14:00:15
info: HotelManagement.Services.RoomStatusUpdateService[0]
      ✅ Cập nhật thành công trong 123ms
info: HotelManagement.Services.RoomStatusUpdateService[0]
      📊 Tổng số phòng RESERVED: 3
info: HotelManagement.Services.RoomStatusUpdateService[0]
      ⏳ Đợi 30 phút đến lần cập nhật tiếp theo...
```

### 2. Kiểm tra trạng thái

**Cách 1: Xem Console Log**
- Mở terminal đang chạy `dotnet run`
- Theo dõi log mỗi 30 phút

**Cách 2: Dùng Monitoring Page**
```
1. Đăng nhập vào hệ thống
2. Truy cập: http://localhost:5000/ServiceStatus/Check
3. Xem dashboard real-time
```

### 3. Test thủ công

**Option 1: Từ Monitoring Page**
```
1. Vào /ServiceStatus/Check
2. Click nút "Chạy cập nhật ngay"
3. Xem kết quả trong Activity Log
```

**Option 2: Gọi API trực tiếp**
```powershell
# PowerShell
Invoke-WebRequest -Uri "http://localhost:5000/ServiceStatus/ManualUpdate" -Method POST
```

**Option 3: SQL Server**
```sql
-- Chạy trực tiếp trong SSMS
EXEC sp_UpdateRoomStatusToReserved;
```

---

## 📝 API Endpoints

### 1. GET `/ServiceStatus/Check`
- **Mô tả**: Trang dashboard monitoring
- **Response**: HTML page với real-time status

### 2. POST `/ServiceStatus/ManualUpdate`
- **Mô tả**: Chạy cập nhật thủ công ngay lập tức
- **Response**: JSON
```json
{
    "success": true,
    "message": "✅ Cập nhật thành công trong 123ms",
    "reservedRoomCount": 3,
    "timestamp": "2025-10-16T14:30:45"
}
```

### 3. GET `/ServiceStatus/GetUpcomingReservations`
- **Mô tả**: Lấy danh sách reservation sắp đến check-in
- **Response**: JSON
```json
{
    "success": true,
    "data": [
        {
            "roomID": "P101",
            "roomStatus": "AVAILABLE",
            "reservationFormID": "RF-000001",
            "checkInDate": "2025-10-16T15:00:00",
            "customerName": "Nguyễn Văn A",
            "minutesUntilCheckIn": 270,
            "updateStatus": "READY"
        }
    ],
    "count": 5,
    "readyToUpdate": 2
}
```

---

## 🔍 Troubleshooting

### Vấn đề 1: Service không chạy

**Triệu chứng**: Không thấy log "Room Status Update Service đã khởi động"

**Giải pháp**:
```csharp
// Kiểm tra Program.cs có dòng này:
builder.Services.AddHostedService<RoomStatusUpdateService>();
```

### Vấn đề 2: Service chạy nhưng báo lỗi

**Triệu chứng**: Console log hiển thị "❌ Lỗi khi thực thi sp_UpdateRoomStatusToReserved"

**Nguyên nhân**: Stored procedure không tồn tại hoặc lỗi SQL

**Giải pháp**:
```sql
-- 1. Kiểm tra SP có tồn tại không
SELECT * FROM sys.procedures WHERE name = 'sp_UpdateRoomStatusToReserved';

-- 2. Nếu không có, chạy lại script tạo SP trong HotelManagement_new.sql

-- 3. Test SP thủ công
EXEC sp_UpdateRoomStatusToReserved;
```

### Vấn đề 3: Service dừng đột ngột

**Triệu chứng**: Log hiển thị "⛔ Room Status Update Service đã dừng"

**Nguyên nhân**: 
- App bị restart
- Exception không được xử lý
- IIS/Hosting dừng app do idle

**Giải pháp**:
```powershell
# 1. Kiểm tra Application Event Log
Get-EventLog -LogName Application -Source ".NET Runtime" -Newest 10

# 2. Enable detailed logging trong appsettings.json
{
  "Logging": {
    "LogLevel": {
      "HotelManagement.Services": "Debug"
    }
  }
}

# 3. Nếu host trên IIS, disable idle timeout
# IIS Manager → Application Pool → Advanced Settings → Idle Time-out = 0
```

### Vấn đề 4: Monitoring page không load data

**Triệu chứng**: Trang `/ServiceStatus/Check` hiển thị "Đang tải..." mãi

**Nguyên nhân**: API endpoint lỗi hoặc CORS issue

**Giải pháp**:
```javascript
// Mở F12 Console trong browser, check lỗi AJAX

// Hoặc test API trực tiếp:
// http://localhost:5000/ServiceStatus/GetUpcomingReservations
```

---

## 🎯 So sánh với các giải pháp khác

| Giải pháp | Ưu điểm | Nhược điểm | Phù hợp với |
|-----------|---------|------------|-------------|
| **Background Service** (Đã implement) | ✅ Dễ setup<br>✅ Không cần SQL Agent<br>✅ Tích hợp app | ⚠️ Chỉ chạy khi app chạy | SQL Express, Development |
| **SQL Agent Job** | ✅ Độc lập với app<br>✅ Chạy 24/7 | ❌ Cần SQL Standard+<br>❌ Phức tạp hơn | Production, SQL Standard+ |
| **Windows Task Scheduler** | ✅ Chạy độc lập<br>✅ Không cần SQL Agent | ⚠️ Cần PowerShell script<br>⚠️ Chỉ Windows | Production, Windows Server |
| **Dashboard Auto-run** | ✅ Cực đơn giản | ❌ Phụ thuộc người dùng load trang | Development only |

---

## 📦 Files đã tạo/sửa

### Files mới:
1. ✅ `Services/RoomStatusUpdateService.cs` - Background service chính
2. ✅ `Controllers/ServiceStatusController.cs` - API monitoring
3. ✅ `Views/ServiceStatus/Check.cshtml` - Dashboard monitoring

### Files đã sửa:
1. ✅ `Program.cs` - Đăng ký Background Service
2. ✅ `Controllers/DashboardController.cs` - Xóa auto-run (không cần nữa)

---

## ✅ Checklist

- [x] Background Service chạy tự động khi app start
- [x] Service cập nhật mỗi 30 phút
- [x] Log chi tiết trong Console
- [x] Monitoring page với dashboard real-time
- [x] API manual update để test
- [x] API lấy danh sách upcoming reservations
- [x] Tự động refresh data mỗi 2 phút
- [x] Responsive design cho monitoring page
- [x] Error handling đầy đủ

---

## 🚀 Kết luận

**Background Service** là giải pháp tối ưu cho SQL Server Express vì:
1. Không cần SQL Agent Job (Express không hỗ trợ)
2. Tích hợp sẵn trong app ASP.NET Core
3. Dễ setup, dễ maintain
4. Có monitoring page trực quan
5. Production-ready với proper logging

**Lưu ý**: 
- Service chỉ chạy khi app đang chạy
- Nếu cần chạy 24/7 độc lập, hãy xem xét dùng Windows Task Scheduler hoặc nâng cấp SQL lên Standard để dùng SQL Agent

---

**Tác giả**: GitHub Copilot  
**Ngày tạo**: 2025-10-16  
**Version**: 1.0  
