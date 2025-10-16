# ✅ Background Service - Quick Start

## 🚀 Khởi động nhanh

### 1. Chạy ứng dụng
```powershell
dotnet run
```

### 2. Kiểm tra log
Console sẽ hiển thị:
```
info: HotelManagement.Services.RoomStatusUpdateService[0]
      🚀 Room Status Update Service đã khởi động
info: HotelManagement.Services.RoomStatusUpdateService[0]
      ⏰ Cập nhật mỗi 30 phút
```

### 3. Truy cập Monitoring Dashboard
```
URL: http://localhost:5000/ServiceStatus/Check
```

---

## 📊 Monitoring Dashboard

Dashboard hiển thị:
- ✅ Service status (RUNNING)
- ✅ Statistics (Total, Ready, Last Update)
- ✅ Upcoming Reservations table với countdown
- ✅ Activity Log real-time
- ✅ Nút "Chạy cập nhật ngay" để test

---

## ⚙️ Cấu hình

### Thay đổi tần suất cập nhật
**File**: `Services/RoomStatusUpdateService.cs`
```csharp
// Dòng 14
private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(30);

// Thay đổi thành:
// 15 phút: TimeSpan.FromMinutes(15)
// 1 giờ: TimeSpan.FromHours(1)
```

---

## 🧪 Test thủ công

### Cách 1: Từ Dashboard
```
1. Vào /ServiceStatus/Check
2. Click "Chạy cập nhật ngay"
3. Xem kết quả trong Activity Log
```

### Cách 2: Gọi API
```powershell
# PowerShell
Invoke-WebRequest -Uri "http://localhost:5000/ServiceStatus/ManualUpdate" -Method POST
```

### Cách 3: Chạy SQL trực tiếp
```sql
EXEC sp_UpdateRoomStatusToReserved;
```

---

## 🔍 Troubleshooting

### Service không chạy?
**Kiểm tra `Program.cs` có dòng:**
```csharp
builder.Services.AddHostedService<RoomStatusUpdateService>();
```

### Service báo lỗi SP?
**Chạy trong SSMS:**
```sql
-- Kiểm tra SP có tồn tại
SELECT * FROM sys.procedures 
WHERE name = 'sp_UpdateRoomStatusToReserved';
```

### Dashboard không load?
**Mở F12 Console trong browser, check lỗi AJAX**

---

## 📖 Tài liệu chi tiết

Xem file **`BACKGROUND_SERVICE_GUIDE.md`** để biết thêm chi tiết!

---

**Tạo bởi**: GitHub Copilot  
**Ngày**: 2025-10-16
