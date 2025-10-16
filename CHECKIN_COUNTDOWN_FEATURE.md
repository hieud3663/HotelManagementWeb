# ⏰ Tính năng Kiểm tra Thời gian Check-in & Đếm ngược

## 📋 Tổng quan
Đã thêm tính năng kiểm tra thời gian check-in và hiển thị đếm ngược đến giờ check-in dự kiến.

---

## ✅ Các thay đổi đã thực hiện

### 1. **Database - Trigger kiểm tra thời gian** (`HotelManagement_new.sql`)

#### **Trigger mới: `TR_HistoryCheckin_CheckTime`**
```sql
CREATE OR ALTER TRIGGER TR_HistoryCheckin_CheckTime
ON HistoryCheckin
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Kiểm tra thời gian check-in có sớm hơn thời gian đã đăng ký không
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
        WHERE i.checkInDate < rf.checkInDate
    )
    BEGIN
        RAISERROR(N'Chưa đến giờ check-in. Vui lòng đợi đến thời gian đã đăng ký.', 16, 1);
        RETURN;
    END
    
    -- Nếu hợp lệ, thực hiện INSERT
    INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
    SELECT historyCheckInID, checkInDate, reservationFormID, employeeID
    FROM inserted;
END;
GO
```

**Chức năng:**
- ✅ Ngăn check-in sớm hơn thời gian đã đăng ký
- ✅ Báo lỗi: "Chưa đến giờ check-in. Vui lòng đợi đến thời gian đã đăng ký."
- ✅ Trigger `INSTEAD OF INSERT` để kiểm tra trước khi cho phép check-in

---

### 2. **View Check-in Index** (`Views/CheckIn/Index.cshtml`)

#### **Cập nhật nút Check-in:**
```html
<button type="submit" 
        class="btn btn-success-modern btn-sm checkin-btn" 
        data-checkin-time="@item.CheckInDate.ToString("yyyy-MM-ddTHH:mm:ss")"
        data-customer-name="@item.Customer?.FullName"
        onclick="return confirmCheckIn(this, event);">
    <i class="fas fa-check"></i> 
    <span class="btn-text">Check-in</span>
    <span class="countdown-text" style="display:none;"></span>
</button>
```

#### **JavaScript đếm ngược:**
```javascript
function updateCountdowns() {
    const buttons = document.querySelectorAll('.checkin-btn');
    const now = new Date();
    
    buttons.forEach(btn => {
        const checkInTime = new Date(btn.dataset.checkinTime);
        const timeDiff = checkInTime - now;
        
        if (timeDiff > 0) {
            // Hiển thị đếm ngược
            const days = Math.floor(timeDiff / (1000 * 60 * 60 * 24));
            const hours = Math.floor((timeDiff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const minutes = Math.floor((timeDiff % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((timeDiff % (1000 * 60)) / 1000);
            
            // Nút màu vàng (warning) - chưa đến giờ
            btn.classList.add('btn-warning-modern');
            btn.classList.remove('btn-success-modern');
        } else {
            // Nút màu xanh (success) - đã đến giờ
            btn.classList.add('btn-success-modern');
            btn.classList.remove('btn-warning-modern');
        }
    });
}

// Cập nhật mỗi giây
setInterval(updateCountdowns, 1000);
```

**Tính năng:**
- ✅ Đếm ngược thời gian thực (cập nhật mỗi giây)
- ✅ Hiển thị: `Xd Xh Xm Xs` (ngày/giờ/phút/giây)
- ✅ Nút màu vàng khi chưa đến giờ
- ✅ Nút màu xanh khi đã đến giờ
- ✅ Cảnh báo khi click sớm: "⏰ Chưa đến giờ check-in!"

---

### 3. **View Reservation Details** (`Views/Reservation/Details.cshtml`)

#### **Cập nhật nút Check-in:**
```html
<button type="submit" 
        class="btn btn-success checkin-btn-detail" 
        data-checkin-time="@Model.CheckInDate.ToString("yyyy-MM-ddTHH:mm:ss")"
        data-customer-name="@Model.Customer?.FullName"
        onclick="return confirmCheckInDetail(this, event);">
    <i class="fas fa-sign-in-alt"></i> 
    <span class="btn-text">Check-in</span>
    <span class="countdown-text" style="display:none;"></span>
</button>
```

#### **JavaScript tương tự Index:**
```javascript
function updateCountdownDetail() {
    const button = document.querySelector('.checkin-btn-detail');
    if (!button) return;
    
    const now = new Date();
    const checkInTime = new Date(button.dataset.checkinTime);
    const timeDiff = checkInTime - now;
    
    if (timeDiff > 0) {
        // Hiển thị: "Còn lại: X ngày X giờ X phút X giây"
        btnText.textContent = 'Còn lại: ';
        countdownText.textContent = countdownStr;
        button.classList.add('btn-warning');
    } else {
        btnText.textContent = 'Check-in';
        button.classList.add('btn-success');
    }
}

setInterval(updateCountdownDetail, 1000);
```

**Tính năng:**
- ✅ Đếm ngược chi tiết hơn: "Còn lại: X ngày X giờ X phút X giây"
- ✅ Nút đổi màu tự động (vàng → xanh)
- ✅ Cảnh báo chi tiết khi click sớm

---

## 🎯 Cách hoạt động

### **Kịch bản 1: Chưa đến giờ check-in**
1. **Hiển thị:** Nút màu vàng với đếm ngược: `"2d 5h 30m 15s"`
2. **Khi click:**
   - Alert: "⏰ Chưa đến giờ check-in!"
   - Hiển thị thời gian còn lại
   - **KHÔNG** cho phép submit form

### **Kịch bản 2: Đã đến giờ check-in**
1. **Hiển thị:** Nút màu xanh với text: `"Check-in"`
2. **Khi click:**
   - Confirm: "Xác nhận check-in cho khách [Tên]?"
   - Nếu YES → Submit form
   - Backend kiểm tra lại bằng trigger

### **Kịch bản 3: Click check-in sớm (bypass JavaScript)**
1. Form submit đến backend
2. **Trigger `TR_HistoryCheckin_CheckTime` chặn**
3. Báo lỗi: "Chưa đến giờ check-in. Vui lòng đợi đến thời gian đã đăng ký."
4. Transaction rollback, không lưu check-in

---

## 📂 Files đã thay đổi

1. ✅ `docs/database/HotelManagement_new.sql`
   - Thêm trigger `TR_HistoryCheckin_CheckTime`

2. ✅ `Views/CheckIn/Index.cshtml`
   - Thêm data attributes cho nút Check-in
   - Thêm JavaScript đếm ngược
   - Thêm function `confirmCheckIn()`

3. ✅ `Views/Reservation/Details.cshtml`
   - Thêm data attributes cho nút Check-in
   - Thêm JavaScript đếm ngược
   - Thêm function `confirmCheckInDetail()`

---

## 🚀 Cách triển khai

### **Bước 1: Cập nhật Database**
```sql
-- Chạy trigger mới từ file HotelManagement_new.sql
-- Hoặc chạy riêng:
CREATE OR ALTER TRIGGER TR_HistoryCheckin_CheckTime
ON HistoryCheckin
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
        WHERE i.checkInDate < rf.checkInDate
    )
    BEGIN
        RAISERROR(N'Chưa đến giờ check-in. Vui lòng đợi đến thời gian đã đăng ký.', 16, 1);
        RETURN;
    END
    
    INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
    SELECT historyCheckInID, checkInDate, reservationFormID, employeeID
    FROM inserted;
END;
GO
```

### **Bước 2: Build lại project**
```powershell
dotnet build
```

### **Bước 3: Test chức năng**
1. ✅ Tạo phiếu đặt phòng với thời gian check-in trong tương lai
2. ✅ Vào trang Check-in Index → xem đếm ngược
3. ✅ Click nút Check-in → xem cảnh báo
4. ✅ Đợi đến giờ → nút đổi màu xanh
5. ✅ Click Check-in → thành công

---

## 📊 Ưu điểm

### **Bảo mật 2 lớp:**
1. **Frontend (JavaScript):**
   - UX tốt: Người dùng thấy đếm ngược
   - Giảm request: Chặn trước khi gửi lên server

2. **Backend (Trigger):**
   - Bảo mật: Không thể bypass bằng cách tắt JavaScript
   - Chính xác: Kiểm tra theo thời gian server

### **Trải nghiệm người dùng:**
- ✅ Trực quan: Thấy rõ thời gian còn lại
- ✅ Chủ động: Biết khi nào có thể check-in
- ✅ Tránh lỗi: Cảnh báo trước khi submit

### **Hiệu năng:**
- ✅ Real-time: Cập nhật mỗi giây
- ✅ Client-side: Không tốn tài nguyên server
- ✅ Lightweight: JavaScript đơn giản, không thư viện bên ngoài

---

## 🐛 Troubleshooting

### **Lỗi: Trigger không hoạt động**
**Nguyên nhân:** Trigger chưa được tạo hoặc bị vô hiệu hóa.

**Giải pháp:**
```sql
-- Kiểm tra trigger tồn tại
SELECT * FROM sys.triggers WHERE name = 'TR_HistoryCheckin_CheckTime';

-- Kích hoạt lại trigger
ENABLE TRIGGER TR_HistoryCheckin_CheckTime ON HistoryCheckin;
```

### **Lỗi: Đếm ngược không chạy**
**Nguyên nhân:** JavaScript không load hoặc data attribute sai format.

**Giải pháp:**
1. Mở Developer Tools (F12) → Console
2. Kiểm tra lỗi JavaScript
3. Kiểm tra format date: `yyyy-MM-ddTHH:mm:ss`

### **Lỗi: Vẫn check-in được khi chưa đến giờ**
**Nguyên nhân:** Trigger bị bypass hoặc không hoạt động.

**Giải pháp:**
1. Kiểm tra trigger đã được tạo chưa
2. Kiểm tra stored procedure `sp_QuickCheckin` có gọi trigger không
3. Test trực tiếp:
```sql
INSERT INTO HistoryCheckin (historyCheckInID, checkInDate, reservationFormID, employeeID)
VALUES ('HCI-TEST', '2025-01-01', 'RF-000001', 'EMP-000001');
-- Phải báo lỗi nếu checkInDate < reservationForm.checkInDate
```

---

## 📝 Ghi chú

- **Múi giờ:** Đếm ngược sử dụng thời gian client (browser), trigger sử dụng thời gian server.
- **Độ chính xác:** Sai số tối đa 1 giây (do setInterval 1000ms).
- **Tương thích:** Hoạt động trên tất cả trình duyệt hiện đại (Chrome, Firefox, Edge, Safari).

---

✨ **Hoàn thành!** Bây giờ hệ thống có kiểm tra thời gian check-in 2 lớp (frontend + backend) và hiển thị đếm ngược thời gian thực! 🎉
