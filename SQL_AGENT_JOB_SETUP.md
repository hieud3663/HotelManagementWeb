# 📋 HƯỚNG DẪN CÀI ĐẶT SQL SERVER AGENT JOB

## 🎯 Mục đích
Tạo SQL Server Agent Job để tự động chạy stored procedure `sp_UpdateRoomStatusToReserved` mỗi 30 phút, cập nhật trạng thái phòng từ `AVAILABLE` sang `RESERVED` khi còn 5 giờ đến check-in.

---

## ✅ YÊU CẦU HỆ THỐNG

- **SQL Server:** Phiên bản có SQL Server Agent (Standard, Enterprise hoặc Developer Edition)
- **Quyền hạn:** Cần quyền `sysadmin` hoặc `SQLAgentUserRole` để tạo job
- **Database:** HotelManagement đã được cài đặt với stored procedure `sp_UpdateRoomStatusToReserved`

> ⚠️ **Lưu ý:** SQL Server Express Edition **KHÔNG** có SQL Server Agent. Nếu dùng Express, xem phần "Giải pháp thay thế" bên dưới.

---

## 🚀 CÁCH 1: TẠO JOB BẰNG T-SQL

### Bước 1: Kiểm tra SQL Server Agent đang chạy

```sql
-- Kiểm tra trạng thái SQL Server Agent
EXEC xp_servicecontrol N'QueryState', N'SQLServerAGENT';
```

**Kết quả mong đợi:** `Running`

Nếu chưa chạy, khởi động bằng:
```sql
EXEC xp_servicecontrol N'START', N'SQLServerAGENT';
```

### Bước 2: Chạy script tạo job

```sql
USE msdb;
GO

-- Bước 1: Tạo Job
EXEC dbo.sp_add_job
    @job_name = N'Update Room Status to Reserved Every 30 Minutes',
    @enabled = 1,
    @description = N'Tự động cập nhật phòng sang RESERVED khi còn 5 giờ đến check-in',
    @category_name = N'Database Maintenance',
    @owner_login_name = N'sa'; -- Đổi thành login của bạn
GO

-- Bước 2: Thêm Job Step (lệnh thực thi)
EXEC dbo.sp_add_jobstep
    @job_name = N'Update Room Status to Reserved Every 30 Minutes',
    @step_name = N'Execute sp_UpdateRoomStatusToReserved',
    @subsystem = N'TSQL',
    @command = N'USE HotelManagement; EXEC sp_UpdateRoomStatusToReserved;',
    @database_name = N'HotelManagement',
    @on_success_action = 1, -- Quit with success
    @on_fail_action = 2,     -- Quit with failure
    @retry_attempts = 3,
    @retry_interval = 5; -- Retry sau 5 phút nếu lỗi
GO

-- Bước 3: Tạo Schedule (lịch chạy mỗi 30 phút)
EXEC dbo.sp_add_schedule
    @schedule_name = N'Every 30 Minutes',
    @freq_type = 4, -- Daily
    @freq_interval = 1, -- Mỗi ngày
    @freq_subday_type = 4, -- Minutes
    @freq_subday_interval = 30, -- Mỗi 30 phút
    @active_start_time = 000000, -- Bắt đầu từ 00:00:00
    @active_end_time = 235959; -- Kết thúc lúc 23:59:59
GO

-- Bước 4: Gắn Schedule vào Job
EXEC dbo.sp_attach_schedule
    @job_name = N'Update Room Status to Reserved Every 30 Minutes',
    @schedule_name = N'Every 30 Minutes';
GO

-- Bước 5: Gán Job cho Local Server
EXEC dbo.sp_add_jobserver
    @job_name = N'Update Room Status to Reserved Every 30 Minutes',
    @server_name = N'(local)';
GO

PRINT 'Job đã được tạo thành công!';
```

### Bước 3: Kiểm tra Job đã tạo

```sql
-- Xem thông tin job
SELECT 
    job_id, 
    name, 
    enabled, 
    description
FROM msdb.dbo.sysjobs
WHERE name = N'Update Room Status to Reserved Every 30 Minutes';

-- Xem schedule
SELECT 
    s.name AS ScheduleName,
    s.enabled,
    s.freq_type,
    s.freq_interval,
    s.freq_subday_type,
    s.freq_subday_interval
FROM msdb.dbo.sysschedules s
WHERE s.name = N'Every 30 Minutes';
```

### Bước 4: Test chạy Job thủ công

```sql
-- Chạy job ngay lập tức để test
EXEC msdb.dbo.sp_start_job 
    @job_name = N'Update Room Status to Reserved Every 30 Minutes';

-- Kiểm tra kết quả
EXEC sp_help_jobhistory 
    @job_name = N'Update Room Status to Reserved Every 30 Minutes';
```

---

## 🖥️ CÁCH 2: TẠO JOB BẰNG SQL SERVER MANAGEMENT STUDIO (SSMS)

### Bước 1: Mở SSMS và kết nối

1. Mở **SQL Server Management Studio**
2. Kết nối đến SQL Server Instance

### Bước 2: Tạo Job mới

1. Expand **SQL Server Agent** (nếu không thấy, nhấn chuột phải vào nó và chọn **Start**)
2. Nhấn chuột phải vào **Jobs** → chọn **New Job...**

### Bước 3: Cấu hình General

1. **Name:** `Update Room Status to Reserved Every 30 Minutes`
2. **Owner:** `sa` (hoặc login của bạn)
3. **Category:** `Database Maintenance`
4. **Description:** `Tự động cập nhật phòng sang RESERVED khi còn 5 giờ đến check-in`
5. **Enabled:** ✅ Check

### Bước 4: Thêm Steps

1. Chọn tab **Steps** → Click **New...**
2. **Step name:** `Execute sp_UpdateRoomStatusToReserved`
3. **Type:** `Transact-SQL script (T-SQL)`
4. **Database:** `HotelManagement`
5. **Command:**
   ```sql
   EXEC sp_UpdateRoomStatusToReserved;
   ```
6. **On success action:** `Quit the job reporting success`
7. **On failure action:** `Quit the job reporting failure`
8. **Retry attempts:** `3`
9. **Retry interval (minutes):** `5`
10. Click **OK**

### Bước 5: Tạo Schedule

1. Chọn tab **Schedules** → Click **New...**
2. **Name:** `Every 30 Minutes`
3. **Schedule type:** `Recurring`
4. **Frequency:**
   - Occurs: `Daily`
   - Recurs every: `1` day(s)
5. **Daily frequency:**
   - Occurs every: `30` minutes
   - Starting at: `12:00:00 AM`
   - Ending at: `11:59:59 PM`
6. **Start date:** Ngày hiện tại
7. Click **OK**

### Bước 6: Cấu hình Notifications (Tùy chọn)

1. Chọn tab **Notifications**
2. Nếu muốn nhận email khi job lỗi:
   - ✅ **Email**
   - Select operator: (tạo operator trước)
   - When the job: `fails`

### Bước 7: Lưu Job

Click **OK** để tạo job.

### Bước 8: Test Job

1. Nhấn chuột phải vào job → chọn **Start Job at Step...**
2. Xem kết quả trong **Job History**:
   - Nhấn chuột phải vào job → **View History**

---

## 📊 GIÁM SÁT VÀ QUẢN LÝ JOB

### Xem lịch sử chạy job

```sql
SELECT 
    h.run_date,
    h.run_time,
    h.run_duration,
    CASE h.run_status
        WHEN 0 THEN 'Failed'
        WHEN 1 THEN 'Succeeded'
        WHEN 2 THEN 'Retry'
        WHEN 3 THEN 'Canceled'
    END AS Status,
    h.message
FROM msdb.dbo.sysjobhistory h
JOIN msdb.dbo.sysjobs j ON h.job_id = j.job_id
WHERE j.name = N'Update Room Status to Reserved Every 30 Minutes'
ORDER BY h.run_date DESC, h.run_time DESC;
```

### Xem job đang chạy

```sql
SELECT 
    j.name AS JobName,
    ja.start_execution_date,
    DATEDIFF(SECOND, ja.start_execution_date, GETDATE()) AS DurationSeconds
FROM msdb.dbo.sysjobactivity ja 
JOIN msdb.dbo.sysjobs j ON ja.job_id = j.job_id
WHERE ja.stop_execution_date IS NULL
AND ja.start_execution_date IS NOT NULL;
```

### Tắt/Bật job

```sql
-- Tắt job
EXEC msdb.dbo.sp_update_job 
    @job_name = N'Update Room Status to Reserved Every 30 Minutes',
    @enabled = 0;

-- Bật job
EXEC msdb.dbo.sp_update_job 
    @job_name = N'Update Room Status to Reserved Every 30 Minutes',
    @enabled = 1;
```

### Xóa job (nếu cần)

```sql
EXEC msdb.dbo.sp_delete_job 
    @job_name = N'Update Room Status to Reserved Every 30 Minutes';
```

---

## 🔧 GIẢI PHÁP THAY THẾ (CHO SQL EXPRESS)

Nếu dùng **SQL Server Express** (không có SQL Agent), có 3 lựa chọn:

### Giải pháp 1: Gọi từ Application (ĐÃ TRIỂN KHAI)

✅ Procedure đã được gọi tự động trong `DashboardController.cs`:

```csharp
await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateRoomStatusToReserved");
```

**Ưu điểm:** Không cần SQL Agent, tự động chạy khi user load Dashboard

**Nhược điểm:** Chỉ chạy khi có người vào Dashboard

### Giải pháp 2: Windows Task Scheduler

Tạo file `.bat` chạy sqlcmd:

**update_room_status.bat:**
```batch
@echo off
sqlcmd -S localhost -d HotelManagement -E -Q "EXEC sp_UpdateRoomStatusToReserved"
```

Sau đó tạo Task trong **Windows Task Scheduler**:
- Trigger: Repeat every 30 minutes
- Action: Start a program → Chọn file `.bat`

### Giải pháp 3: Background Service trong ASP.NET Core

Tạo `Hosted Service` chạy trong background:

```csharp
public class RoomStatusUpdateService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateRoomStatusToReserved");
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
```

Đăng ký trong `Program.cs`:
```csharp
builder.Services.AddHostedService<RoomStatusUpdateService>();
```

---

## ⚡ KIỂM TRA HOẠT ĐỘNG

### Test stored procedure trực tiếp

```sql
USE HotelManagement;
GO

-- Chạy procedure
EXEC sp_UpdateRoomStatusToReserved;

-- Kiểm tra kết quả
SELECT * FROM vw_RoomsNearCheckIn;

-- Kiểm tra phòng đã chuyển sang RESERVED
SELECT 
    r.roomID,
    r.roomStatus,
    rf.checkInDate,
    DATEDIFF(MINUTE, GETDATE(), rf.checkInDate) / 60.0 AS HoursUntilCheckIn
FROM Room r
JOIN ReservationForm rf ON r.roomID = rf.roomID
WHERE r.roomStatus = 'RESERVED'
AND rf.isActivate = 'ACTIVATE';
```

---

## 📞 TROUBLESHOOTING

### Lỗi: "SQL Server Agent is not currently running"

**Giải pháp:**
1. Mở **SQL Server Configuration Manager**
2. Chọn **SQL Server Services**
3. Nhấn chuột phải **SQL Server Agent** → **Start**

### Lỗi: "The specified @owner_login_name does not exist"

**Giải pháp:**
Đổi `@owner_login_name` trong script thành login hiện tại:
```sql
@owner_login_name = N'sa' -- Hoặc domain\username của bạn
```

### Job chạy nhưng không thấy hiệu ứng

**Kiểm tra:**
1. Xem log trong Job History
2. Chạy thủ công procedure để debug:
   ```sql
   EXEC sp_UpdateRoomStatusToReserved;
   ```
3. Kiểm tra có phòng nào satisfy điều kiện không:
   ```sql
   SELECT * FROM vw_RoomsNearCheckIn;
   ```

---

## ✅ CHECKLIST HOÀN THÀNH

- [ ] SQL Server Agent đang chạy
- [ ] Stored procedure `sp_UpdateRoomStatusToReserved` đã tạo
- [ ] Job đã được tạo và enabled
- [ ] Schedule mỗi 30 phút đã được gắn vào job
- [ ] Test chạy job thủ công thành công
- [ ] Kiểm tra Job History không có lỗi
- [ ] Giải pháp backup (Dashboard auto-run) đã triển khai

---

## 📚 TÀI LIỆU THAM KHẢO

- [SQL Server Agent Jobs Documentation](https://learn.microsoft.com/en-us/sql/ssms/agent/create-a-job)
- [Scheduling Jobs in SQL Server](https://learn.microsoft.com/en-us/sql/ssms/agent/schedule-a-job)
- [Background tasks with hosted services in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)

---

**Cập nhật:** 15/10/2025  
**Version:** 1.0.0
