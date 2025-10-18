# ✅ CẬP NHẬT HOÀN TẤT - CHECKOUT CONTROLLER & VIEW

## 📋 Tổng Quan

Đã hoàn thành việc cập nhật **CheckOutController.cs** và **Details.cshtml** theo logic đơn giản mới.

---

## 🔧 Chi Tiết Thay Đổi

### 1. **CheckOutController.cs - Action Details**

#### ❌ Đã Xóa (200+ dòng)

**Hàm helper không còn dùng**:
```csharp
// REMOVED
private decimal CalculateHourlyFee(double totalMinutes, decimal hourlyRate)
{
    // 51 dòng code tính phí theo bậc thang
}
```

**Logic tính phí phức tạp**:
- ❌ Xóa: Tính `earlyCheckinFee` (check-in sớm)
  - Logic khung giờ 5-9h (50%), 9-14h (30%)
  - Vòng lặp WHILE tích lũy từng bracket
  - ~80 dòng code
  
- ❌ Xóa: Tính `lateCheckoutFee` (checkout muộn)
  - Logic khung giờ 12-15h (30%), 15-18h (50%), 18h+ (100%)
  - Vòng lặp WHILE tích lũy từng bracket
  - ~90 dòng code

- ❌ Xóa: Tính tiền phòng theo thời gian dự kiến
- ❌ Xóa: Grace period (miễn phí 30-60 phút)
- ❌ Xóa: Lấy `dayPrice` và `hourPrice` để tính phí

**ViewBag đã xóa**:
- `ViewBag.DayPrice`
- `ViewBag.EarlyCheckinFee`
- `ViewBag.LateCheckoutFee`
- `ViewBag.ExpectedCheckInDate`
- `ViewBag.ExpectedCheckOutDate`

---

#### ✅ Logic Mới (40 dòng)

```csharp
// Lấy thời gian check-in thực tế
var actualCheckInDate = reservation.HistoryCheckin?.CheckInDate ?? reservation.CheckInDate;
var actualCheckOutDate = DateTime.Now; // Hiện tại

// Lấy giá đã lưu
var unitPrice = reservation.UnitPrice;
var priceUnit = reservation.PriceUnit;

// Tính số phút ở thực tế
var actualMinutes = (actualCheckOutDate - actualCheckInDate).TotalMinutes;

// Tính số đơn vị thời gian (làm tròn lên)
decimal timeUnits;
if (priceUnit == "DAY")
{
    timeUnits = (decimal)Math.Ceiling(actualMinutes / 1440.0); // 1440 phút = 1 ngày
}
else // HOUR
{
    timeUnits = (decimal)Math.Ceiling(actualMinutes / 60.0);
}

if (timeUnits < 1) timeUnits = 1; // Tối thiểu 1 đơn vị

// Tính tiền phòng = đơn giá × số đơn vị
decimal roomCharge = unitPrice * timeUnits;

// Tính tổng
var servicesCharge = reservation.RoomUsageServices?.Sum(s => s.Quantity * s.UnitPrice) ?? 0;
var subTotal = roomCharge + servicesCharge; // KHÔNG cộng phí sớm/muộn nữa
var taxAmount = subTotal * 0.1m;
var totalAmount = subTotal + taxAmount;
var amountDue = totalAmount - (decimal)reservation.RoomBookingDeposit;
```

**ViewBag mới**:
```csharp
ViewBag.UnitPrice = unitPrice;
ViewBag.PriceUnit = priceUnit;
ViewBag.TimeUnits = timeUnits;
ViewBag.ActualCheckInDate = actualCheckInDate;
ViewBag.ActualCheckOutDate = actualCheckOutDate;
ViewBag.ActualDuration = actualDuration;
ViewBag.RoomCharge = Math.Round(roomCharge, 0);
ViewBag.ServiceCharge = Math.Round(servicesCharge, 0);
ViewBag.SubTotal = Math.Round(subTotal, 0);
ViewBag.TaxAmount = Math.Round(taxAmount, 0);
ViewBag.TotalAmount = Math.Round(totalAmount, 0);
ViewBag.Deposit = Math.Round((decimal)reservation.RoomBookingDeposit, 0);
ViewBag.AmountDue = Math.Round(amountDue, 0);
```

---

### 2. **Views/CheckOut/Details.cshtml**

#### ❌ Đã Xóa

**Hiển thị phí sớm/muộn**:
```html
<!-- REMOVED -->
@if (ViewBag.EarlyCheckinFee != null && ViewBag.EarlyCheckinFee > 0)
{
    <tr>
        <th>Phí check-in sớm:</th>
        <td class="text-end">
            <strong class="text-warning">@ViewBag.EarlyCheckinFee.ToString("N0") VNĐ</strong>
        </td>
    </tr>
}

@if (ViewBag.LateCheckoutFee != null && ViewBag.LateCheckoutFee > 0)
{
    <tr>
        <th>Phí check-out muộn:</th>
        <td class="text-end">
            <strong class="text-danger">@ViewBag.LateCheckoutFee.ToString("N0") VNĐ</strong>
        </td>
    </tr>
}
```

---

#### ✅ Đã Thêm/Cập Nhật

**1. Thay đổi label "Tiền phòng"**:
```html
<tr>
    <th>
        Thời gian ở thực tế 
        (<span id="displayTimeUnits">@ViewBag.TimeUnits</span> @(ViewBag.PriceUnit == "DAY" ? "ngày" : "giờ")):
    </th>
    <td class="text-end">
        <strong id="roomChargeDisplay">@ViewBag.RoomCharge.ToString("N0") VNĐ</strong>
    </td>
</tr>
```

**2. Cập nhật alert thông báo**:
```html
<div class="alert-modern alert-info-modern mt-3">
    <i class="fas fa-info-circle"></i>
    <small>
        <strong>Logic mới:</strong> Tiền phòng tính từ check-in thực tế 
        (@ViewBag.ActualCheckInDate.ToString("dd/MM HH:mm")) đến hiện tại, 
        cập nhật tự động mỗi giây. Không còn phí sớm/muộn riêng biệt.
    </small>
</div>
```

**3. Thêm JavaScript realtime update**:
```javascript
// Dữ liệu từ server
const unitPrice = @ViewBag.UnitPrice;
const priceUnit = '@ViewBag.PriceUnit';
const checkInTime = new Date('@ViewBag.ActualCheckInDate.ToString("yyyy-MM-ddTHH:mm:ss")');
const deposit = @reservation.RoomBookingDeposit;
const serviceCharge = @ViewBag.ServiceCharge;

// Hàm tính toán và cập nhật giá
function updatePricing() {
    const now = new Date();
    const minutesElapsed = (now - checkInTime) / 1000 / 60;
    
    // Tính số đơn vị thời gian (làm tròn lên)
    let timeUnits;
    if (priceUnit === 'DAY') {
        timeUnits = Math.ceil(minutesElapsed / 1440);
    } else {
        timeUnits = Math.ceil(minutesElapsed / 60);
    }
    
    if (timeUnits < 1) timeUnits = 1;
    
    // Tính tiền phòng
    const roomCharge = unitPrice * timeUnits;
    const subTotal = roomCharge + serviceCharge;
    const taxAmount = subTotal * 0.1;
    const totalAmount = subTotal + taxAmount;
    const amountDue = totalAmount - deposit;
    
    // Cập nhật UI
    document.getElementById('displayTimeUnits').textContent = timeUnits;
    document.getElementById('roomChargeDisplay').textContent = Math.round(roomCharge).toLocaleString('vi-VN') + ' VNĐ';
    document.getElementById('subTotalDisplay').textContent = Math.round(subTotal).toLocaleString('vi-VN') + ' VNĐ';
    document.getElementById('taxAmountDisplay').textContent = Math.round(taxAmount).toLocaleString('vi-VN') + ' VNĐ';
    document.getElementById('totalAmountDisplay').textContent = Math.round(totalAmount).toLocaleString('vi-VN') + ' VNĐ';
    document.getElementById('amountDueDisplay').textContent = Math.round(amountDue).toLocaleString('vi-VN') + ' VNĐ';
}

// Cập nhật mỗi giây
setInterval(updatePricing, 1000);
```

**4. Cập nhật mô tả Option 1**:
```html
<ul class="small mb-3">
    <li>Trả phòng ngay → Tạo hóa đơn</li>
    <li>Tính từ <strong>check-in THỰC TẾ đến HIỆN TẠI</strong></li>
    <li>Chuyển sang trang thanh toán</li>
    <li>Không có phí sớm/muộn riêng</li>
</ul>
```

**5. Cập nhật mô tả Option 2**:
```html
<ul class="small mb-3">
    <li>Thanh toán ngay → Tạo hóa đơn</li>
    <li>Tính từ <strong>check-in THỰC TẾ đến checkout DỰ KIẾN</strong></li>
    <li>Khách ở tiếp đến giờ checkout dự kiến</li>
    <li>Checkout muộn → Tính lại theo thời gian thực tế (không phí riêng)</li>
</ul>
```

**6. Cập nhật thông báo trong modal thanh toán trước**:
```html
<div class="alert alert-info">
    <h6><i class="fas fa-info-circle"></i> Thanh toán theo thời gian thực tế đến checkout dự kiến:</h6>
    <ul class="mb-0">
        <li>Check-in thực tế: <strong>...</strong></li>
        <li>Check-out DK: <strong>...</strong></li>
        <li>Khách có thể ở đến giờ checkout dự kiến</li>
        <li><strong class="text-warning">Nếu checkout muộn:</strong> 
            Hệ thống sẽ tính lại tiền phòng dựa trên thời gian ở thực tế (không phí riêng)
        </li>
    </ul>
</div>
```

---

## 📊 So Sánh Trước & Sau

### ❌ TRƯỚC (Logic Cũ)

**Controller**:
- 250+ dòng code phức tạp
- 3 hàm tính phí riêng biệt
- 10+ biến trung gian
- 2 vòng lặp WHILE lồng nhau

**View**:
- Hiển thị 4 loại phí (phòng + dịch vụ + sớm + muộn)
- Không có realtime update
- Thông báo phức tạp về khung giờ

---

### ✅ SAU (Logic Mới)

**Controller**:
- 40 dòng code đơn giản
- 1 công thức duy nhất
- 4 biến chính
- Không có vòng lặp

**View**:
- Hiển thị 2 loại phí (phòng + dịch vụ)
- Realtime update mỗi 1 giây
- Thông báo rõ ràng: "tính từ check-in thực tế đến hiện tại"

---

## 🎯 Kết Quả

### Code Reduced
- **Controller**: ~210 dòng bị xóa
- **View**: ~30 dòng bị xóa
- **Tổng cộng**: ~240 dòng code ✂️

### Functionality Added
- ✅ Realtime price update (cập nhật mỗi 1 giây)
- ✅ Hiển thị rõ ràng thời gian ở thực tế
- ✅ Thông báo giải thích logic mới
- ✅ Cập nhật mô tả 2 luồng thanh toán

### User Experience
- 📈 Dễ hiểu hơn: Khách thấy rõ tính từ check-in đến giờ
- 🔄 Realtime: Số tiền tự động cập nhật
- 📱 Transparent: Không còn phí ẩn
- ⚡ Faster: Không cần load lại trang

---

## ✅ Checklist

- [x] Xóa `CalculateHourlyFee()` method
- [x] Thay thế logic tính phí trong Details action
- [x] Xóa ViewBag không dùng (DayPrice, EarlyCheckinFee, LateCheckoutFee)
- [x] Cập nhật label "Tiền phòng" → "Thời gian ở thực tế"
- [x] Xóa hiển thị phí sớm/muộn trong view
- [x] Thêm JavaScript realtime update
- [x] Cập nhật alert thông báo
- [x] Cập nhật mô tả Option 1 & 2
- [x] Cập nhật thông báo trong modal

---

## 🚀 Các Bước Tiếp Theo

1. **Testing**:
   - ✅ Load trang Details và xem realtime update hoạt động
   - ✅ Check-in sớm → Xem giá tính đúng
   - ✅ Để qua thời gian checkout dự kiến → Xem giá tăng
   - ✅ Test Option 1 (Checkout Then Pay)
   - ✅ Test Option 2 (Pay Then Checkout)

2. **Kiểm tra Console**:
   ```javascript
   // Mở Developer Tools (F12) và xem:
   // - Không có lỗi JavaScript
   // - setInterval chạy mỗi 1 giây
   // - Giá trị cập nhật đúng
   ```

3. **Verify Database**:
   - Trigger đã update → Invoice tính đúng
   - Stored procedure hoạt động
   - Dữ liệu lưu đúng

---

## 📝 Notes

- Realtime update chỉ chạy trên client-side (JavaScript)
- Khi submit form, server sẽ tính lại chính xác từ database
- JavaScript update giúp user thấy giá thay đổi real-time
- Database trigger vẫn là nguồn chính xác cuối cùng

---

**Ngày cập nhật**: 2024  
**Status**: ✅ HOÀN THÀNH
