# 🎯 TÓM TẮT CÁC THAY ĐỔI LỚN - HOTEL MANAGEMENT SYSTEM

## 📅 Ngày cập nhật: 15/10/2025

---

## ✅ CÁC THAY ĐỔI ĐÃ HOÀN THÀNH

### 1. **Cho phép đặt phòng trước - Phòng không chuyển RESERVED ngay** 🏨

#### **Vấn đề cũ:**
- Khi tạo phiếu đặt phòng, phòng ngay lập tức chuyển sang trạng thái `RESERVED`
- Không thể đặt phòng trước nhiều ngày vì phòng bị "khóa" ngay

#### **Giải pháp mới:**
- **Stored Procedure `sp_CreateReservation`:** ✅ ĐÃ SỬA
  - Xóa dòng `UPDATE Room SET RoomStatus = 'RESERVED'`
  - Phòng vẫn giữ trạng thái `AVAILABLE` sau khi đặt
  
- **Stored Procedure `sp_UpdateRoomStatusToReserved`:** ✅ MỚI TẠO
  ```sql
  -- Tự động cập nhật phòng sang RESERVED khi còn 5 giờ đến check-in
  UPDATE Room SET roomStatus = 'RESERVED'
  WHERE roomID IN (
      SELECT rf.roomID FROM ReservationForm rf
      WHERE rf.checkInDate <= DATEADD(HOUR, 5, GETDATE())
      AND rf.checkInDate > GETDATE()
      AND chưa check-in
  )
  ```

- **View `vw_RoomsNearCheckIn`:** ✅ MỚI TẠO
  - Theo dõi các phòng sắp được reserved (trong vòng 6 giờ tới)
  - Hiển thị thông tin khách hàng, thời gian còn lại

#### **Cách hoạt động:**
1. Đặt phòng ngày 10/10 cho ngày 15/10 lúc 14:00
2. Phòng vẫn `AVAILABLE` từ 10/10 → 14/10 đến 09:00
3. Ngày 14/10 lúc 09:00 (còn 5h), hệ thống tự động chuyển phòng sang `RESERVED`
4. Khách đến check-in lúc 14:00

#### **Cần làm thêm:**
- [ ] Tạo SQL Server Agent Job chạy `EXEC sp_UpdateRoomStatusToReserved` mỗi 30 phút
- [ ] Hoặc gọi procedure này khi load trang Dashboard/CheckIn

---

### 2. **Sửa lỗi tính giá khi check-in sớm và check-out sớm** 💰

#### **Vấn đề cũ:**
```
Ví dụ lỗi:
- Đặt phòng: 11:00 → 12:00 (1 giờ dự kiến)
- Check-in sớm: 10:00 (sớm 1h)
- Check-out: 11:30 (sau 1.5h thực tế)

Tính cũ (SAI):
- Tiền phòng = 1 giờ (từ 11:00 → 12:00 Expected)
- Phí check-in sớm = 1 giờ
- TỔNG = 2 giờ ❌ (SAI vì khách chỉ ở 1.5h)
```

#### **Giải pháp mới:**
- **Trigger `TR_Invoice_ManageInsert`:** ✅ ĐÃ SỬA
- **Trigger `TR_Invoice_ManageUpdate`:** ✅ ĐÃ SỬA

```sql
-- TRƯỚC (SAI):
SET @bookingMinutes = DATEDIFF(MINUTE, @checkInDateExpected, @checkOutDateExpected);

-- SAU (ĐÚNG):
DECLARE @effectiveCheckIn = ISNULL(@checkInDateActual, @checkInDateExpected);
DECLARE @effectiveCheckOut = ISNULL(@checkOutDateActual, @checkOutDateExpected);
SET @bookingMinutes = DATEDIFF(MINUTE, @effectiveCheckIn, @effectiveCheckOut);
```

#### **Cách tính mới:**
```
Ví dụ đúng:
- Đặt phòng: 11:00 → 12:00
- Check-in sớm: 10:00
- Check-out: 11:30

Tính mới (ĐÚNG):
- Tiền phòng = DATEDIFF(10:00, 11:30) = 90 phút → làm tròn 2 giờ
- Phí check-in sớm = 0 (vì đã tính trong thời gian thực tế)
- TỔNG = 2 giờ ✅ (ĐÚNG)
```

#### **Kịch bản check-in sớm + check-out sớm:**
```
Đặt phòng: 14:00 hôm nay → 14:00 ngày mai (24h)
Check-in: 12:00 hôm nay (sớm 2h)
Check-out: 10:00 ngày mai (sớm 4h)

Tính giá:
- Thời gian thực tế ở: 12:00 → 10:00 = 22 giờ
- Làm tròn lên 1 ngày (nếu thuê theo ngày)
- Tiền phòng = 1 ngày x đơn giá
- KHÔNG tính phí check-in sớm/check-out sớm riêng
```

---

### 3. **Thêm bảng ConfirmationReceipt - Phiếu xác nhận đặt phòng/nhận phòng** 🧾

#### **Model mới:** ✅ ĐÃ TẠO
- File: `Models/ConfirmationReceipt.cs`
- Thuộc tính:
  - `ReceiptID`: Mã phiếu
  - `ReceiptType`: "RESERVATION" hoặc "CHECKIN"
  - `IssueDate`: Ngày phát hành
  - `ReservationFormID`: Link đến phiếu đặt phòng
  - `InvoiceID`: Link đến hóa đơn (nếu có)
  - `CustomerName`, `CustomerPhone`, `CustomerEmail`
  - `RoomID`, `RoomCategoryName`
  - `CheckInDate`, `CheckOutDate`
  - `PriceUnit`, `UnitPrice`, `Deposit`, `TotalAmount`
  - `EmployeeName`, `Notes`, `QrCode`

#### **Bảng database:** ✅ ĐÃ TẠO
- File: `docs/database/HotelManagement_new.sql`
- Constraints:
  - Phiếu RESERVATION phải có `reservationFormID`
  - Foreign key đến `ReservationForm` và `Invoice`

#### **DbContext:** ✅ ĐÃ CẬP NHẬT
- Thêm `public DbSet<ConfirmationReceipt> ConfirmationReceipts { get; set; }`

---

## ⏳ CÁC TÍNH NĂNG ĐANG PENDING

### 4. **Đếm ngược "Sắp nhận" trên trang đặt phòng** ⏱️
- [ ] Thêm filter check-in/check-out date vào `Views/Reservation/Create.cshtml`
- [ ] JavaScript hiển thị countdown cho phòng có reservation còn <= 5 giờ
- [ ] Hiển thị badge "Sắp nhận - 3h 25m" trên room card

### 5. **API lọc phòng theo thời gian đặt phòng** 🔍
- [ ] Tạo endpoint: `POST /api/Room/CheckAvailability`
  - Input: `checkInDate`, `checkOutDate`, `roomCategoryID`
  - Output: Danh sách phòng trống (không có reservation overlap)
- [ ] Logic kiểm tra:
  ```sql
  WHERE NOT EXISTS (
      SELECT 1 FROM ReservationForm rf
      WHERE rf.roomID = r.roomID
      AND rf.isActivate = 'ACTIVATE'
      AND (@checkInDate < rf.checkOutDate)
      AND (@checkOutDate > rf.checkInDate)
  )
  ```

### 6. **Logic tạo phiếu xác nhận tự động** 📄
- [ ] **ReservationController.Create:**
  - Sau khi `INSERT ReservationForm` thành công
  - Tự động `INSERT ConfirmationReceipt` với `receiptType = 'RESERVATION'`
  
- [ ] **CheckInController.CheckIn:**
  - Sau khi `INSERT HistoryCheckin` thành công
  - Tự động `INSERT ConfirmationReceipt` với `receiptType = 'CHECKIN'`

### 7. **View hiển thị và in phiếu xác nhận** 🖨️
- [ ] Tạo `Views/ConfirmationReceipt/Details.cshtml`
- [ ] Thiết kế đẹp, có thể in ra giấy
- [ ] Bao gồm:
  - Logo khách sạn
  - Thông tin khách hàng đầy đủ
  - Thông tin phòng, giá, thời gian
  - QR code để quét
  - Chữ ký nhân viên

---

## 📊 KIỂM TRA VÀ TEST

### Test case quan trọng:

#### **Test 1: Đặt phòng trước**
1. Đặt phòng 101 cho ngày 20/10/2025 lúc 14:00
2. Kiểm tra: Phòng 101 vẫn `AVAILABLE` ngay sau khi đặt ✅
3. Đợi đến ngày 20/10 lúc 09:00 (hoặc chạy `sp_UpdateRoomStatusToReserved`)
4. Kiểm tra: Phòng 101 chuyển sang `RESERVED` ✅

#### **Test 2: Check-in sớm + Check-out sớm**
1. Đặt phòng: 14:00 hôm nay → 14:00 ngày mai (24h), giá 500k/ngày
2. Check-in sớm: 10:00 hôm nay (sớm 4h)
3. Check-out sớm: 08:00 ngày mai (sớm 6h)
4. Thời gian thực tế: 10:00 → 08:00 = 22 giờ
5. Kiểm tra hóa đơn:
   - Tiền phòng = 500k (làm tròn 1 ngày) ✅
   - Không có phí riêng cho check-in/out sớm ✅

#### **Test 3: Check-in sớm + Check-out đúng giờ**
1. Đặt phòng: 14:00 → 18:00 (4h), giá 100k/giờ
2. Check-in sớm: 13:00 (sớm 1h)
3. Check-out: 18:00 (đúng giờ)
4. Thời gian thực tế: 13:00 → 18:00 = 5 giờ
5. Kiểm tra hóa đơn:
   - Tiền phòng = 5 x 100k = 500k ✅

---

## 🚀 HƯỚNG DẪN CÀI ĐẶT

### Bước 1: Cập nhật Database
```sql
-- Chạy lại script database
USE HotelManagement;
GO

-- Kiểm tra stored procedure mới
SELECT * FROM sys.procedures WHERE name = 'sp_UpdateRoomStatusToReserved';

-- Kiểm tra view mới
SELECT * FROM sys.views WHERE name = 'vw_RoomsNearCheckIn';

-- Kiểm tra bảng mới
SELECT * FROM sys.tables WHERE name = 'ConfirmationReceipt';
```

### Bước 2: Test Stored Procedure
```sql
-- Test cập nhật phòng RESERVED
EXEC sp_UpdateRoomStatusToReserved;

-- Xem kết quả
SELECT * FROM vw_RoomsNearCheckIn;
```

### Bước 3: Tạo SQL Server Agent Job (Tùy chọn)
```sql
-- Tạo job chạy mỗi 30 phút
USE msdb;
GO

EXEC dbo.sp_add_job
    @job_name = N'Update Room Status to Reserved',
    @enabled = 1;

EXEC sp_add_jobstep
    @job_name = N'Update Room Status to Reserved',
    @step_name = N'Execute Procedure',
    @subsystem = N'TSQL',
    @command = N'USE HotelManagement; EXEC sp_UpdateRoomStatusToReserved;',
    @database_name = N'HotelManagement';

EXEC sp_add_schedule
    @schedule_name = N'Every 30 Minutes',
    @freq_type = 4, -- Daily
    @freq_interval = 1,
    @freq_subday_type = 4, -- Minutes
    @freq_subday_interval = 30;

EXEC sp_attach_schedule
    @job_name = N'Update Room Status to Reserved',
    @schedule_name = N'Every 30 Minutes';

EXEC dbo.sp_add_jobserver
    @job_name = N'Update Room Status to Reserved';
```

---

## 📝 GHI CHÚ QUAN TRỌNG

### ⚠️ Breaking Changes:
1. **Logic đặt phòng đã thay đổi hoàn toàn**
   - Phòng không còn chuyển RESERVED ngay nữa
   - Cần chạy procedure định kỳ để cập nhật trạng thái

2. **Cách tính giá phòng đã thay đổi**
   - Giờ tính theo thời gian THỰC TẾ, không phải dự kiến
   - Phí check-in sớm/muộn chỉ áp dụng khi vượt quá thời gian miễn phí

### ✅ Ưu điểm của hệ thống mới:
- Cho phép đặt phòng trước nhiều ngày mà không "khóa" phòng
- Tính giá chính xác hơn theo thời gian thực tế khách ở
- Phiếu xác nhận chuyên nghiệp, có thể in ra
- Dễ dàng theo dõi phòng sắp được nhận

### 🔧 Cần optimize sau:
- [ ] Cache kết quả `vw_RoomsNearCheckIn` để tránh query nhiều
- [ ] Thêm index cho `ReservationForm.checkInDate`
- [ ] Tạo background service (C#) để thay thế SQL Agent Job

---

## 👨‍💻 Người thực hiện
- **Agent:** GitHub Copilot
- **Ngày:** 15/10/2025
- **Version:** 2.0.0-beta

---

## 📞 Liên hệ hỗ trợ
- Nếu có vấn đề, kiểm tra file `PRICING_LOGIC_FINAL.md`
- Xem trigger details trong `HotelManagement_new.sql`
