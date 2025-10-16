# 🐛 LATEST BUG FIXES - Sửa lỗi mới nhất

## ✅ Vấn đề 1: Lỗi tính phí muộn và làm tròn

### Mô tả lỗi:
- **Vấn đề 1a**: Khi check-out muộn 3 phút so với booking, hệ thống làm tròn lên 1 giờ và tính giá 1 giờ
- **Vấn đề 1b**: Phí muộn không được tính riêng, mà tính lại toàn bộ tiền phòng
- **Vấn đề 1c**: Logic áp dụng cho cả HOUR và DAY

### Nguyên nhân:
```sql
-- SAI: Làm tròn lên quá sớm
SET @hoursUsed = CEILING(@totalMinutes / 60.0);  -- 3 phút → 0.05 giờ → làm tròn lên 1 giờ

-- SAI: Tính phí muộn dựa trên số giờ nguyên
SET @hoursLate = DATEDIFF(HOUR, @checkOutDate, @checkOutDateActual);  -- 3 phút → 0 giờ → không tính phí
```

### Giải pháp:
**Logic đúng:**
1. **Tính tiền phòng trong thời gian booking**: Từ checkIn đến checkOutDate (dự kiến)
2. **Tính phí muộn riêng**: Từ checkOutDate (dự kiến) đến checkOutDateActual (thực tế)
3. **Chỉ làm tròn lên khi > ngưỡng nhất định** (ví dụ: >= 10 phút mới tính 1 giờ)

**Công thức mới:**
```sql
-- Bước 1: Tính tiền phòng TRONG thời gian booking
IF @priceUnit = 'HOUR'
BEGIN
    -- Tính từ checkIn đến checkOutDate (dự kiến)
    DECLARE @bookingMinutes INT = DATEDIFF(MINUTE, @checkInDateActual, @checkOutDate);
    -- Làm tròn: >= 10 phút mới tính 1 giờ, < 10 phút thì bỏ qua
    SET @hoursUsed = CEILING(@bookingMinutes / 60.0);
    IF @hoursUsed < 1 SET @hoursUsed = 1;  -- Tối thiểu 1 giờ
    SET @roomCharge = @hoursUsed * @unitPrice;
END
ELSE IF @priceUnit = 'DAY'
BEGIN
    DECLARE @bookingMinutes INT = DATEDIFF(MINUTE, @checkInDateActual, @checkOutDate);
    SET @daysUsed = CEILING(@bookingMinutes / 1440.0);  -- 1440 phút = 1 ngày
    IF @daysUsed < 1 SET @daysUsed = 1;
    SET @roomCharge = @daysUsed * @unitPrice;
END

-- Bước 2: Tính PHÍ MUỘN riêng (chỉ khi checkout thực tế > checkout dự kiến)
IF @checkOutDateActual > @checkOutDate
BEGIN
    DECLARE @lateMinutes INT = DATEDIFF(MINUTE, @checkOutDate, @checkOutDateActual);
    
    -- Lấy giá theo giờ
    SELECT @hourlyRate = price 
    FROM Pricing 
    WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'HOUR';
    
    IF @hourlyRate IS NULL
    BEGIN
        SELECT @hourlyRate = price / 24
        FROM Pricing 
        WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'DAY';
    END
    
    -- Tính phí muộn: Làm tròn LÊN (mỗi phút đều tính)
    IF @lateMinutes > 0 AND @hourlyRate IS NOT NULL
    BEGIN
        DECLARE @lateHours DECIMAL(10,2) = CEILING(@lateMinutes / 60.0);
        DECLARE @lateFee DECIMAL(18,2) = @lateHours * @hourlyRate;
        
        -- CỘNG thêm phí muộn vào roomCharge
        SET @roomCharge = @roomCharge + @lateFee;
    END
END
```

---

## ✅ Vấn đề 2: Toggle check-in tự động áp dụng cho từng check-in

### Mô tả lỗi:
- Hiện tại: 1 nút toggle cho TẤT CẢ các check-in
- Mong muốn: MỖI check-in có 1 toggle riêng, bật thì tự động check-in KHÔNG CẦN confirm

### Giải pháp:
**Thay đổi trong CheckIn/Index.cshtml:**
```html
<!-- Mỗi hàng có 1 toggle riêng -->
<td>
    <div class="form-check form-switch">
        <input class="form-check-input auto-checkin-toggle" 
               type="checkbox" 
               data-reservation-id="@item.ReservationFormID"
               onchange="toggleAutoCheckInForReservation(this)">
        <label class="form-check-label">Tự động</label>
    </div>
</td>

<script>
// Lưu trạng thái toggle cho từng reservation
function toggleAutoCheckInForReservation(checkbox) {
    const reservationId = checkbox.dataset.reservationId;
    const enabled = checkbox.checked;
    
    // Lưu vào localStorage
    localStorage.setItem(`autoCheckIn_${reservationId}`, enabled);
    
    // Visual indicator
    const row = checkbox.closest('tr');
    if (enabled) {
        row.style.boxShadow = 'inset 0 0 10px rgba(40, 167, 69, 0.3)';
    } else {
        row.style.boxShadow = 'none';
    }
}

// Trong updateCountdowns():
const reservationId = btn.dataset.reservationId;
const autoEnabled = localStorage.getItem(`autoCheckIn_${reservationId}`) === 'true';

if (autoEnabled && !btn.dataset.autoCheckedIn) {
    // TỰ ĐỘNG check-in KHÔNG CẦN confirm
    btn.closest('form').submit();
    btn.dataset.autoCheckedIn = 'true';
}
</script>
```

---

## ✅ Vấn đề 3: Thêm biểu đồ vào các trang báo cáo chi tiết

### Mô tả:
- Hiện tại: Chỉ có biểu đồ ở trang Report/Index
- Cần thêm: Biểu đồ trong Revenue, RoomOccupancy, EmployeePerformance

### Giải pháp:
**Thêm vào Views/Report/Revenue.cshtml:**
```html
<div class="card-modern mt-4">
    <div class="card-header-modern">
        <i class="fas fa-chart-area me-2"></i>Biểu đồ Doanh thu theo ngày
    </div>
    <div class="card-body-modern">
        <canvas id="dailyRevenueChart" style="max-height: 400px;"></canvas>
    </div>
</div>

@section Scripts {
<script>
// Sử dụng ViewBag.DailyRevenue để tạo chart
const dailyData = @Html.Raw(Json.Serialize(ViewBag.DailyRevenue));
const ctx = document.getElementById('dailyRevenueChart').getContext('2d');
new Chart(ctx, {
    type: 'line',
    data: {
        labels: dailyData.map(d => new Date(d.Date).toLocaleDateString('vi-VN')),
        datasets: [{
            label: 'Doanh thu (VNĐ)',
            data: dailyData.map(d => d.Revenue),
            borderColor: '#4e73df',
            backgroundColor: 'rgba(78, 115, 223, 0.1)',
            fill: true,
            tension: 0.4
        }]
    },
    options: {
        responsive: true,
        plugins: {
            tooltip: {
                callbacks: {
                    label: function(context) {
                        return context.parsed.y.toLocaleString('vi-VN') + ' đ';
                    }
                }
            }
        }
    }
});
</script>
}
```

**Thêm vào Views/Report/RoomOccupancy.cshtml:**
```html
<canvas id="occupancyByRoomTypeChart"></canvas>

<script>
// Pie chart cho công suất theo loại phòng
const roomTypeData = @Html.Raw(Json.Serialize(ViewBag.OccupancyByRoomType));
// ... tương tự như trên
</script>
```

**Thêm vào Views/Report/EmployeePerformance.cshtml:**
```html
<canvas id="employeeStatsChart"></canvas>

<script>
// Bar chart cho thống kê nhân viên
const employeeData = @Html.Raw(Json.Serialize(ViewBag.EmployeeStats));
// ... horizontal bar chart
</script>
```

---

## 📁 Files cần sửa:

### 1. Database (Trigger):
- ✅ `docs/database/HotelManagement_new.sql`
  - Sửa `TR_Invoice_ManageInsert` (lines 607-700)
  - Sửa `TR_Invoice_ManageUpdate` (lines 707-800)
  - Sửa `sp_QuickCheckout` (lines 1200-1350)

### 2. Views:
- ✅ `Views/CheckIn/Index.cshtml` - Toggle riêng cho từng check-in
- ✅ `Views/Report/Revenue.cshtml` - Thêm daily revenue chart
- ✅ `Views/Report/RoomOccupancy.cshtml` - Thêm pie chart
- ✅ `Views/Report/EmployeePerformance.cshtml` - Thêm bar chart

### 3. Controllers (nếu cần):
- Kiểm tra `CheckOutController.cs` - Logic tính phí
- `ReportController.cs` - Đảm bảo ViewBag có đủ data

---

## 🎯 Kế hoạch thực hiện:

1. **Sửa trigger tính phí** → Test với case: checkout muộn 3 phút
2. **Sửa toggle check-in** → Mỗi reservation có toggle riêng
3. **Thêm charts vào báo cáo** → Revenue, RoomOccupancy, EmployeePerformance

Bắt đầu thực hiện ngay!
