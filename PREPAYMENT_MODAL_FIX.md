# Sửa Modal Thanh Toán Trước - Hiển Thị Đúng Số Tiền Dự Kiến

## 🎯 Vấn Đề

Modal "Thanh toán trước" (PAY_THEN_CHECKOUT) trong trang Details.cshtml hiển thị sai số tiền:
- **Trước đây**: Hiển thị số tiền tính theo thời gian thực tế (từ check-in thực tế → hiện tại)
- **Mong muốn**: Hiển thị số tiền tính theo thời gian dự kiến (từ check-in thực tế → checkout dự kiến)

## ✅ Giải Pháp Đã Thực Hiện

### 1. Controller - Tính 2 Scenario Song Song

**File**: `Controllers/CheckOutController.cs`

**Action**: `Details(string reservationFormID)`

#### Scenario 1: Thời Gian Thực Tế (cho CHECKOUT_THEN_PAY)
```csharp
// Tính từ check-in thực tế → hiện tại
var actualMinutes = (actualCheckOutDate - actualCheckInDate).TotalMinutes;
decimal timeUnits = priceUnit == "DAY" 
    ? (decimal)Math.Ceiling(actualMinutes / 1440.0)
    : (decimal)Math.Ceiling(actualMinutes / 60.0);
decimal roomCharge = unitPrice * timeUnits;

// Tính tổng
var subTotal = roomCharge + servicesCharge;
var taxAmount = subTotal * 0.1m;
var totalAmount = subTotal + taxAmount;
var amountDue = totalAmount - (decimal)reservation.RoomBookingDeposit;
```

**ViewBag Properties**:
- `ViewBag.RoomCharge` - Tiền phòng thực tế
- `ViewBag.SubTotal` - Tạm tính thực tế
- `ViewBag.TaxAmount` - VAT thực tế
- `ViewBag.TotalAmount` - Tổng tiền thực tế
- `ViewBag.AmountDue` - Số tiền cần trả (thực tế)

#### Scenario 2: Thời Gian Dự Kiến (cho PAY_THEN_CHECKOUT)
```csharp
// Tính từ check-in thực tế → checkout dự kiến
var expectedCheckOutDate = reservation.CheckOutDate;
var expectedMinutes = (expectedCheckOutDate - actualCheckInDate).TotalMinutes;
decimal expectedTimeUnits = priceUnit == "DAY"
    ? (decimal)Math.Ceiling(expectedMinutes / 1440.0)
    : (decimal)Math.Ceiling(expectedMinutes / 60.0);
decimal expectedRoomCharge = unitPrice * expectedTimeUnits;

// Tính tổng dự kiến
var expectedSubTotal = expectedRoomCharge + servicesCharge;
var expectedTaxAmount = expectedSubTotal * 0.1m;
var expectedTotalAmount = expectedSubTotal + expectedTaxAmount;
var expectedAmountDue = expectedTotalAmount - (decimal)reservation.RoomBookingDeposit;
```

**ViewBag Properties**:
- `ViewBag.ExpectedRoomCharge` - Tiền phòng dự kiến
- `ViewBag.ExpectedTimeUnits` - Số đơn vị thời gian dự kiến
- `ViewBag.ExpectedSubTotal` - Tạm tính dự kiến
- `ViewBag.ExpectedTaxAmount` - VAT dự kiến
- `ViewBag.ExpectedTotalAmount` - Tổng tiền dự kiến
- `ViewBag.ExpectedAmountDue` - **Số tiền cần trả (dự kiến)** ⭐

### 2. View - Cập Nhật Modal Hiển Thị

**File**: `Views/CheckOut/Details.cshtml`

**Modal**: "Thanh toán trước" (Option 2)

#### Thông Tin Hiển Thị
```html
<div class="alert alert-info">
    <h6><i class="fas fa-info-circle"></i> Thanh toán theo thời gian dự kiến:</h6>
    <ul class="mb-0">
        <li>Check-in thực tế: <strong>@(checkin?.CheckInDate.ToString("dd/MM/yyyy HH:mm") ?? "N/A")</strong></li>
        <li>Check-out DK: <strong>@reservation.CheckOutDate.ToString("dd/MM/yyyy HH:mm")</strong></li>
        <li>Thời gian: <strong>@ViewBag.ExpectedTimeUnits @(ViewBag.PriceUnit == "DAY" ? "ngày" : "giờ")</strong></li>
        <li>Tiền phòng: <strong>@ViewBag.ExpectedRoomCharge.ToString("N0") VNĐ</strong></li>
        <li>Tiền dịch vụ: <strong>@ViewBag.ServiceCharge.ToString("N0") VNĐ</strong></li>
        <li><strong class="text-warning">Nếu checkout muộn:</strong> Hệ thống sẽ tính lại tiền phòng dựa trên thời gian ở thực tế</li>
    </ul>
</div>
```

#### Chi Tiết Thanh Toán
```html
<table class="table table-sm table-borderless mb-0 mt-2">
    <tr>
        <td>Tiền phòng (dự kiến):</td>
        <td class="text-end"><strong>@ViewBag.ExpectedRoomCharge.ToString("N0") VNĐ</strong></td>
    </tr>
    <tr>
        <td>Tiền dịch vụ:</td>
        <td class="text-end"><strong>@ViewBag.ServiceCharge.ToString("N0") VNĐ</strong></td>
    </tr>
    <tr>
        <td>Tạm tính:</td>
        <td class="text-end"><strong>@ViewBag.ExpectedSubTotal.ToString("N0") VNĐ</strong></td>
    </tr>
    <tr>
        <td>VAT (10%):</td>
        <td class="text-end"><strong>@ViewBag.ExpectedTaxAmount.ToString("N0") VNĐ</strong></td>
    </tr>
    <tr>
        <td>Tiền đặt cọc:</td>
        <td class="text-end text-success"><strong>-@ViewBag.Deposit.ToString("N0") VNĐ</strong></td>
    </tr>
    <tr class="border-top">
        <td><strong>TỔNG CỘNG:</strong></td>
        <td class="text-end"><h5 class="text-danger mb-0">@ViewBag.ExpectedAmountDue.ToString("N0") VNĐ</h5></td>
    </tr>
</table>
```

#### Nút Xác Nhận
```html
<button type="submit" class="btn btn-success" 
        onclick="return confirm('Xác nhận thanh toán @ViewBag.ExpectedAmountDue.ToString("N0") VNĐ?\n\nKhách sẽ có thể ở đến @reservation.CheckOutDate.ToString("dd/MM/yyyy HH:mm")\n\nLưu ý: Nếu checkout muộn hơn, hệ thống sẽ tính thêm tiền phòng theo thời gian thực tế.');">
    <i class="fas fa-check-circle"></i> Xác nhận thanh toán
</button>
```

## 📊 So Sánh Trước/Sau

### Trước Khi Sửa
```
Modal PAY_THEN_CHECKOUT hiển thị:
- Số tiền: @ViewBag.AmountDue (tính theo thực tế → hiện tại)
- Ví dụ: Check-in 10:00, hiện tại 14:00 → tính 4 giờ
- ❌ SAI: Không phản ánh thời gian khách được phép ở
```

### Sau Khi Sửa
```
Modal PAY_THEN_CHECKOUT hiển thị:
- Số tiền: @ViewBag.ExpectedAmountDue (tính theo dự kiến)
- Ví dụ: Check-in thực tế 10:00, checkout DK 22:00 → tính 12 giờ
- ✅ ĐÚNG: Khách thanh toán cho thời gian được phép ở (10:00 → 22:00)
- ⚠️ Nếu khách ở đến 23:00 → hệ thống tính thêm 1 giờ khi checkout thực tế
```

## 🔄 Luồng Xử Lý

### Option 1: CHECKOUT_THEN_PAY (Trả phòng rồi thanh toán)
```
1. Hiển thị số tiền THỰC TẾ trên trang Details
2. Nhân viên nhấn "Trả phòng và thanh toán"
3. SP tạo invoice tính theo actualCheckOut (hiện tại)
4. Chuyển đến trang Payment để thanh toán
```

### Option 2: PAY_THEN_CHECKOUT (Thanh toán trước)
```
1. Hiển thị số tiền THỰC TẾ trên trang Details (để tham khảo)
2. Nhân viên mở modal "Thanh toán trước"
3. Modal hiển thị số tiền DỰ KIẾN (check-in thực tế → checkout dự kiến) ⭐
4. Nhân viên xác nhận
5. SP tạo invoice tính theo expectedCheckOut
6. Chuyển đến trang Payment để thanh toán
7. Khi khách checkout thực tế:
   - Nếu đúng giờ hoặc sớm: Không tính thêm
   - Nếu muộn: sp_ActualCheckout_AfterPrepayment tính thêm tiền
```

## 🎯 Kết Quả

✅ **Modal hiển thị đúng số tiền dự kiến**
✅ **Controller tính 2 scenario song song**
✅ **ViewBag properties phân biệt rõ ràng**
✅ **Thông báo cảnh báo rõ ràng về checkout muộn**
✅ **Database triggers xử lý đúng cả 2 luồng**

## 📝 Lưu Ý Quan Trọng

1. **Modal chỉ là preview**: Số tiền cuối cùng được tính bởi database triggers
2. **Database triggers đã đúng**: Triggers kiểm tra `checkoutType` và sử dụng đúng thời gian
3. **Checkout muộn**: `sp_ActualCheckout_AfterPrepayment` sẽ tính thêm tiền dựa trên `roomCharge` difference
4. **Không có phí riêng**: Tất cả đều tính trong `roomCharge` theo công thức đơn giản

## 🔗 Files Liên Quan

- `Controllers/CheckOutController.cs` - Details action (lines 80-163)
- `Views/CheckOut/Details.cshtml` - Modal PAY_THEN_CHECKOUT (lines 310-365)
- `PRICING_LOGIC_SIMPLE.md` - Tài liệu logic tính giá tổng quan
- `PRICING_SIMPLIFICATION_COMPLETE.md` - Tổng kết đơn giản hóa giá

---
**Ngày cập nhật**: 2024
**Trạng thái**: ✅ Hoàn thành
