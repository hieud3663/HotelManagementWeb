# Real-time Invoice & Pricing Display Enhancements

## Ngày cập nhật
**2024-01-XX** - Cải tiến hiển thị giá và tính toán hóa đơn thời gian thực

---

## 📋 Tổng quan

Đã triển khai 2 tính năng chính để cải thiện trải nghiệm người dùng và độ chính xác của hệ thống:

1. **Hiển thị đơn giá trong danh sách đặt phòng**
2. **Tính toán hóa đơn thời gian thực khi check-out**

---

## ✨ Tính năng 1: Hiển thị Đơn giá trong Danh sách Đặt phòng

### Mô tả
Thêm cột "Đơn giá" vào bảng danh sách đặt phòng để người dùng có thể xem ngay giá phòng đã được xác định khi đặt.

### Files đã sửa
- **Views/Reservation/Index.cshtml**

### Thay đổi cụ thể

#### 1. Thêm cột header
```html
<th><i class="fas fa-money-bill-wave"></i> Đơn giá</th>
```
Vị trí: Sau cột "Loại phòng", trước cột "Check-in"

#### 2. Thêm dữ liệu trong tbody
```html
<td>
    <strong class="text-success">@item.UnitPrice.ToString("N0")đ/@(item.PriceUnit == "HOUR" ? "giờ" : "ngày")</strong>
</td>
```

### Định dạng hiển thị
- **Giá theo giờ**: `40,000đ/giờ`
- **Giá theo ngày**: `100,000đ/ngày`
- Màu xanh lá (text-success) để nhất quán với cột "Tiền cọc"
- Định dạng số với dấu phẩy phân cách hàng nghìn

### Lợi ích
- ✅ Người dùng xem ngay đơn giá khi đặt phòng
- ✅ Dễ dàng so sánh giá giữa các đặt phòng
- ✅ Minh bạch thông tin giá cả
- ✅ Không cần vào chi tiết để xem giá

---

## ✨ Tính năng 2: Tính toán Hóa đơn Thời gian Thực

### Mô tả
Hóa đơn check-out được cập nhật tự động mỗi giây dựa trên thời gian thực tế khách ở, sử dụng giá đã lưu từ lúc đặt phòng.

### Files đã sửa
1. **Controllers/CheckOutController.cs** - Logic tính toán backend
2. **Views/CheckOut/Details.cshtml** - Giao diện và JavaScript real-time

---

### 2.1. Cập nhật Controller Logic

#### File: `Controllers/CheckOutController.cs`

#### Thay đổi chính

**TRƯỚC** (Sai - dùng giá hiện tại từ bảng Pricing):
```csharp
var dayPrice = reservation.Room?.RoomCategory?.Pricings?.FirstOrDefault(p => p.PriceUnit == "DAY")?.Price ?? 0;
var hourPrice = reservation.Room?.RoomCategory?.Pricings?.FirstOrDefault(p => p.PriceUnit == "HOUR")?.Price ?? 0;
```

**SAU** (Đúng - dùng giá đã lưu khi đặt phòng):
```csharp
var unitPrice = reservation.UnitPrice;
var priceUnit = reservation.PriceUnit;
```

#### Logic tính toán mới

```csharp
decimal roomCharge = 0;
decimal timeUnits = 0;

if (priceUnit == "DAY")
{
    var daysDiff = (checkOutDate - checkInDate).TotalDays;
    timeUnits = (decimal)Math.Ceiling(daysDiff);
    roomCharge = unitPrice * timeUnits;
}
else // HOUR
{
    var hoursDiff = (checkOutDate - checkInDate).TotalHours;
    timeUnits = (decimal)Math.Ceiling(hoursDiff);
    roomCharge = unitPrice * timeUnits;
}
```

#### Tính phụ phí trả muộn (cải tiến)

```csharp
decimal lateFee = 0;
if (checkOutDate > reservation.CheckOutDate)
{
    var lateHours = (checkOutDate - reservation.CheckOutDate).TotalHours;
    if (priceUnit == "DAY")
    {
        // Đối với giá theo ngày
        if (lateHours <= 2)
            lateFee = unitPrice * 0.25m; // 25% giá ngày cho 2h đầu
        else if (lateHours <= 6)
            lateFee = unitPrice * 0.5m;  // 50% giá ngày cho 2-6h
        else
            lateFee = unitPrice;         // Full giá ngày nếu quá 6h
    }
    else // HOUR
    {
        // Đối với giá theo giờ, tính theo số giờ trễ
        lateFee = unitPrice * (decimal)Math.Ceiling(lateHours);
    }
}
```

#### ViewBag mới cho JavaScript
```csharp
ViewBag.UnitPrice = unitPrice;
ViewBag.PriceUnit = priceUnit;
ViewBag.TimeUnits = timeUnits;
ViewBag.CheckInDate = checkInDate;
ViewBag.ExpectedCheckOutDate = reservation.CheckOutDate;
```

---

### 2.2. Cập nhật View với Real-time JavaScript

#### File: `Views/CheckOut/Details.cshtml`

#### A. Thêm thông tin thời gian chi tiết

**Thông tin phòng cũ:**
```html
<tr>
    <th>Check-in:</th>
    <td>@(checkin?.CheckInDate.ToString("dd/MM/yyyy HH:mm") ?? "N/A")</td>
</tr>
<tr>
    <th>Check-out DK:</th>
    <td>@reservation.CheckOutDate.ToString("dd/MM/yyyy HH:mm")</td>
</tr>
```

**Thông tin phòng MỚI (chi tiết hơn):**
```html
<tr>
    <th><i class="fas fa-sign-in-alt"></i> Check-in:</th>
    <td><strong class="text-success">@(checkin?.CheckInDate.ToString("dd/MM/yyyy HH:mm") ?? "N/A")</strong></td>
</tr>
<tr>
    <th><i class="fas fa-calendar"></i> Check-out dự kiến:</th>
    <td>@reservation.CheckOutDate.ToString("dd/MM/yyyy HH:mm")</td>
</tr>
<tr>
    <th><i class="fas fa-clock"></i> Thời gian hiện tại:</th>
    <td>
        <strong class="text-primary" id="currentTime">@DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")</strong>
    </td>
</tr>
<tr>
    <th><i class="fas fa-hourglass-half"></i> Thời gian thực tế:</th>
    <td>
        <strong class="text-info" id="actualDuration">
            <span id="timeValue">@ViewBag.TimeUnits</span> 
            <span id="timeUnit">@(ViewBag.PriceUnit == "DAY" ? "ngày" : "giờ")</span>
        </strong>
    </td>
</tr>
```

#### B. Cập nhật bảng hóa đơn với IDs

**Thêm hiển thị đơn giá:**
```html
<tr>
    <th>Đơn giá phòng:</th>
    <td class="text-end">
        <strong>@ViewBag.UnitPrice.ToString("N0") VNĐ/@(ViewBag.PriceUnit == "DAY" ? "ngày" : "giờ")</strong>
    </td>
</tr>
```

**Tiền phòng với số đơn vị động:**
```html
<tr>
    <th>Tiền phòng (<span id="displayTimeUnits">@ViewBag.TimeUnits</span> @(ViewBag.PriceUnit == "DAY" ? "ngày" : "giờ")):</th>
    <td class="text-end"><strong id="roomChargeDisplay">@ViewBag.RoomCharge.ToString("N0") VNĐ</strong></td>
</tr>
```

**Các dòng khác với IDs để JavaScript cập nhật:**
```html
<td class="text-end"><strong id="serviceChargeDisplay">...</strong></td>
<td class="text-end"><strong id="lateFeeDisplay">...</strong></td>
<td class="text-end"><strong id="subTotalDisplay">...</strong></td>
<td class="text-end"><strong id="taxAmountDisplay">...</strong></td>
<td class="text-end"><h4 id="totalAmountDisplay">...</h4></td>
<td class="text-end"><h3 id="amountDueDisplay">...</h3></td>
```

**Thông báo cập nhật tự động:**
```html
<div class="alert-modern alert-info-modern mt-3">
    <i class="fas fa-info-circle"></i>
    <small>Hóa đơn được cập nhật tự động theo thời gian thực tế</small>
</div>
```

#### C. JavaScript Real-time Update

```javascript
@section Scripts {
<script>
    // Lấy dữ liệu từ server
    const checkInDate = new Date('@ViewBag.CheckInDate.ToString("yyyy-MM-ddTHH:mm:ss")');
    const expectedCheckOutDate = new Date('@ViewBag.ExpectedCheckOutDate.ToString("yyyy-MM-ddTHH:mm:ss")');
    const unitPrice = @ViewBag.UnitPrice;
    const priceUnit = '@ViewBag.PriceUnit';
    const serviceCharge = @ViewBag.ServiceCharge;
    const deposit = @ViewBag.Deposit;

    function updateInvoice() {
        const currentTime = new Date();
        
        // 1. Cập nhật hiển thị thời gian hiện tại
        const timeStr = currentTime.toLocaleDateString('vi-VN') + ' ' + 
                       currentTime.toLocaleTimeString('vi-VN');
        document.getElementById('currentTime').textContent = timeStr;

        // 2. Tính số đơn vị thời gian (ngày/giờ)
        const diffMs = currentTime - checkInDate;
        let timeUnits = 0;
        
        if (priceUnit === 'DAY') {
            const diffDays = diffMs / (1000 * 60 * 60 * 24);
            timeUnits = Math.ceil(diffDays);
        } else { // HOUR
            const diffHours = diffMs / (1000 * 60 * 60);
            timeUnits = Math.ceil(diffHours);
        }

        // 3. Cập nhật số đơn vị thời gian
        document.getElementById('timeValue').textContent = timeUnits;
        document.getElementById('displayTimeUnits').textContent = timeUnits;

        // 4. Tính tiền phòng
        const roomCharge = unitPrice * timeUnits;

        // 5. Tính phụ phí trả muộn
        let lateFee = 0;
        if (currentTime > expectedCheckOutDate) {
            const lateMs = currentTime - expectedCheckOutDate;
            const lateHours = lateMs / (1000 * 60 * 60);
            
            if (priceUnit === 'DAY') {
                if (lateHours <= 2) {
                    lateFee = unitPrice * 0.25;
                } else if (lateHours <= 6) {
                    lateFee = unitPrice * 0.5;
                } else {
                    lateFee = unitPrice;
                }
            } else { // HOUR
                lateFee = unitPrice * Math.ceil(lateHours);
            }
        }

        // 6. Tính tổng hợp
        const subTotal = roomCharge + serviceCharge + lateFee;
        const taxAmount = subTotal * 0.1;
        const totalAmount = subTotal + taxAmount;
        const amountDue = totalAmount - deposit;

        // 7. Cập nhật tất cả các giá trị trên màn hình
        document.getElementById('roomChargeDisplay').textContent = 
            Math.round(roomCharge).toLocaleString('vi-VN') + ' VNĐ';
        document.getElementById('lateFeeDisplay').textContent = 
            Math.round(lateFee).toLocaleString('vi-VN') + ' VNĐ';
        document.getElementById('subTotalDisplay').textContent = 
            Math.round(subTotal).toLocaleString('vi-VN') + ' VNĐ';
        document.getElementById('taxAmountDisplay').textContent = 
            Math.round(taxAmount).toLocaleString('vi-VN') + ' VNĐ';
        document.getElementById('totalAmountDisplay').textContent = 
            Math.round(totalAmount).toLocaleString('vi-VN') + ' VNĐ';
        document.getElementById('amountDueDisplay').textContent = 
            Math.round(amountDue).toLocaleString('vi-VN') + ' VNĐ';
    }

    // Cập nhật mỗi giây
    setInterval(updateInvoice, 1000);
    
    // Cập nhật ngay lập tức khi load trang
    updateInvoice();
</script>
}
```

---

## 🔄 Quy trình hoạt động

### Tính toán hóa đơn Real-time

```
┌─────────────────────────────────────────────────────┐
│ 1. Load trang CheckOut/Details                      │
│    - Server tính toán giá trị ban đầu               │
│    - Truyền dữ liệu qua ViewBag                     │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│ 2. JavaScript khởi chạy                              │
│    - Lấy checkInDate, expectedCheckOutDate          │
│    - Lấy unitPrice, priceUnit từ reservation        │
│    - Lấy serviceCharge, deposit cố định             │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│ 3. setInterval chạy mỗi giây (1000ms)               │
│    - Lấy thời gian hiện tại: new Date()             │
│    - Tính timeUnits: ceiling((now - checkIn) / ...)  │
│    - Tính roomCharge = unitPrice × timeUnits        │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│ 4. Tính phụ phí trả muộn                             │
│    if (now > expectedCheckOutDate):                  │
│      - lateHours = (now - expected) / 3600000       │
│      - if DAY: 25%/50%/100% theo lateHours          │
│      - if HOUR: ceiling(lateHours) × unitPrice      │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│ 5. Tính tổng hợp                                     │
│    subTotal = roomCharge + serviceCharge + lateFee  │
│    taxAmount = subTotal × 0.1 (VAT 10%)             │
│    totalAmount = subTotal + taxAmount                │
│    amountDue = totalAmount - deposit                 │
└─────────────────┬───────────────────────────────────┘
                  │
                  v
┌─────────────────────────────────────────────────────┐
│ 6. Cập nhật DOM                                      │
│    - currentTime: "31/01/2024 14:35:22"             │
│    - timeValue: "2" (ngày hoặc giờ)                 │
│    - roomChargeDisplay: "200,000 VNĐ"               │
│    - lateFeeDisplay: "50,000 VNĐ"                   │
│    - subTotalDisplay, taxAmountDisplay, ...         │
│    - amountDueDisplay: "225,000 VNĐ" (sau trừ cọc) │
└─────────────────────────────────────────────────────┘
```

---

## 📊 So sánh TRƯỚC và SAU

### Danh sách Đặt phòng (Reservation/Index)

| Aspect | TRƯỚC | SAU |
|--------|-------|-----|
| **Số cột** | 9 cột | 10 cột |
| **Thông tin giá** | Không có | Có cột "Đơn giá" |
| **Xem giá** | Phải vào Details | Xem ngay trong danh sách |
| **Định dạng giá** | N/A | "40,000đ/giờ" hoặc "100,000đ/ngày" |

### Check-out Details (Hóa đơn)

| Aspect | TRƯỚC | SAU |
|--------|-------|-----|
| **Nguồn giá** | ❌ Bảng Pricing (giá hiện tại) | ✅ Reservation (giá đã lưu) |
| **Cập nhật** | ❌ Tĩnh (chỉ khi reload) | ✅ Real-time (mỗi giây) |
| **Thời gian hiện tại** | ❌ Không hiển thị | ✅ Cập nhật theo giây |
| **Thời gian thực tế** | ❌ Không rõ ràng | ✅ Hiển thị số ngày/giờ |
| **Đơn giá** | ❌ Không hiển thị | ✅ Hiển thị rõ ràng |
| **Phụ phí trả muộn** | ⚠️ Logic cũ (dùng giá hiện tại) | ✅ Logic mới (dùng giá đã lưu) |
| **Độ chính xác** | ⚠️ Sai nếu giá thay đổi | ✅ Chính xác 100% |

---

## ⚙️ Công thức tính toán

### Tiền phòng
```
if (PriceUnit == "DAY"):
    timeUnits = ceiling((currentTime - checkInTime) / 24 hours)
    roomCharge = UnitPrice × timeUnits
else if (PriceUnit == "HOUR"):
    timeUnits = ceiling((currentTime - checkInTime) / 1 hour)
    roomCharge = UnitPrice × timeUnits
```

### Phụ phí trả muộn

**Giá theo NGÀY:**
```
lateHours = (currentTime - expectedCheckOutDate) / 1 hour

if lateHours <= 2:
    lateFee = UnitPrice × 0.25     (25%)
else if lateHours <= 6:
    lateFee = UnitPrice × 0.5      (50%)
else:
    lateFee = UnitPrice            (100%)
```

**Giá theo GIỜ:**
```
lateHours = (currentTime - expectedCheckOutDate) / 1 hour
lateFee = UnitPrice × ceiling(lateHours)
```

### Tổng hợp
```
subTotal = roomCharge + serviceCharge + lateFee
taxAmount = subTotal × 0.1                (VAT 10%)
totalAmount = subTotal + taxAmount
amountDue = totalAmount - deposit
```

---

## 🎯 Lợi ích

### 1. Độ chính xác
- ✅ **Giá không đổi**: Dùng giá đã lưu khi đặt phòng, không bị ảnh hưởng bởi thay đổi giá sau này
- ✅ **Tính toán real-time**: Hóa đơn chính xác đến từng giây
- ✅ **Phụ phí công bằng**: Tính phụ phí dựa trên giá gốc của booking

### 2. Trải nghiệm người dùng
- ✅ **Minh bạch**: Xem rõ đơn giá, thời gian thực tế, từng khoản phí
- ✅ **Real-time feedback**: Nhân viên thấy số tiền thay đổi theo thời gian
- ✅ **Thông tin đầy đủ**: Không cần đoán hoặc tính tay

### 3. Quản lý
- ✅ **Nhất quán dữ liệu**: Giá được khóa khi đặt phòng
- ✅ **Dễ kiểm tra**: Xem ngay đơn giá trong danh sách
- ✅ **Công bằng**: Khách không bị tính giá cao hơn khi check-out

---

## 🧪 Test Cases

### Test 1: Hiển thị đơn giá trong Index
1. Vào trang **Đặt phòng** (`/Reservation/Index`)
2. Kiểm tra cột "Đơn giá" xuất hiện sau cột "Loại phòng"
3. Xác nhận định dạng: "40,000đ/giờ" hoặc "100,000đ/ngày"
4. Màu chữ phải là xanh lá (text-success)

**Kết quả mong đợi**: ✅ Hiển thị đúng định dạng với dấu phẩy

### Test 2: Real-time calculation với giá GIỜ
1. Tạo reservation với priceUnit = "HOUR", unitPrice = 50,000
2. Check-in khách
3. Vào trang CheckOut/Details
4. Quan sát "Thời gian hiện tại" cập nhật mỗi giây
5. Quan sát "Thời gian thực tế" tăng dần (1 giờ → 2 giờ → 3 giờ)
6. Quan sát "Tiền phòng" tăng dần (50,000 → 100,000 → 150,000)

**Kết quả mong đợi**: ✅ Tất cả số liệu cập nhật mỗi giây

### Test 3: Real-time calculation với giá NGÀY
1. Tạo reservation với priceUnit = "DAY", unitPrice = 500,000
2. Check-in khách
3. Vào trang CheckOut/Details
4. Quan sát "Thời gian thực tế" tính theo ngày
5. Nếu ở dưới 24h: hiển thị "1 ngày", giá = 500,000
6. Nếu ở trên 24h nhưng dưới 48h: "2 ngày", giá = 1,000,000

**Kết quả mong đợi**: ✅ Làm tròn lên (ceiling) đúng cách

### Test 4: Phụ phí trả muộn (giá NGÀY)
1. Tạo reservation với checkOutDate = "2024-01-20 12:00"
2. Giả sử thời gian hiện tại = "2024-01-20 14:00" (trễ 2h)
3. Phụ phí = unitPrice × 0.25
4. Nếu trễ 4h: phụ phí = unitPrice × 0.5
5. Nếu trễ 8h: phụ phí = unitPrice × 1.0

**Kết quả mong đợi**: ✅ Phụ phí tính đúng theo từng mức

### Test 5: Phụ phí trả muộn (giá GIỜ)
1. Tạo reservation với priceUnit = "HOUR", checkOutDate = "14:00"
2. Thời gian hiện tại = "16:30" (trễ 2.5h)
3. Phụ phí = unitPrice × ceiling(2.5) = unitPrice × 3

**Kết quả mong đợi**: ✅ Phụ phí = 3 × unitPrice

### Test 6: Trừ tiền cọc
1. Reservation với roomBookingDeposit = 100,000
2. Tổng hóa đơn (sau VAT) = 550,000
3. "Còn phải thanh toán" = 550,000 - 100,000 = 450,000

**Kết quả mong đợi**: ✅ Tiền cọc được trừ chính xác

### Test 7: Không trả muộn
1. Check-out trước hoặc đúng thời gian dự kiến
2. "Phụ phí trả muộn" = 0 VNĐ
3. Chỉ tính tiền phòng + dịch vụ + VAT

**Kết quả mong đợi**: ✅ Không có phụ phí

---

## 🚀 Cách sử dụng

### Cho Nhân viên

#### Xem giá trong danh sách
1. Vào **Đặt phòng** > **Danh sách Đặt phòng**
2. Xem cột **Đơn giá** để biết giá phòng của từng booking
3. So sánh nhanh giá giữa các booking

#### Check-out với hóa đơn real-time
1. Vào **Check-out** > **Danh sách Check-out**
2. Nhấn nút **Chi tiết** trên booking cần check-out
3. Quan sát:
   - **Thời gian hiện tại**: Cập nhật mỗi giây
   - **Thời gian thực tế**: Số ngày/giờ đã ở
   - **Tiền phòng**: Tăng theo thời gian
   - **Phụ phí trả muộn**: Nếu quá giờ
   - **Tổng cộng**: Cập nhật liên tục
4. Chọn phương thức thanh toán
5. Nhấn **Xác nhận thanh toán**

---

## 🔧 Troubleshooting

### Vấn đề: Hóa đơn không cập nhật
**Nguyên nhân**: JavaScript không chạy  
**Giải pháp**: 
- Mở Console (F12)
- Kiểm tra lỗi JavaScript
- Đảm bảo ViewBag có dữ liệu (CheckInDate, UnitPrice, PriceUnit)

### Vấn đề: Đơn giá không hiển thị trong Index
**Nguyên nhân**: Reservation chưa có priceUnit/unitPrice  
**Giải pháp**:
- Đảm bảo đã chạy migration để thêm 2 cột mới
- Tạo reservation mới để test (reservation cũ có thể null)

### Vấn đề: Số tiền sai
**Nguyên nhân**: Logic tính toán không khớp giữa C# và JavaScript  
**Giải pháp**:
- Kiểm tra công thức ceiling() trong cả 2 bên
- Kiểm tra logic phụ phí trả muộn
- Đảm bảo serviceCharge và deposit được truyền đúng

---

## 📝 Notes

### Điểm quan trọng
1. **Giá khóa khi đặt phòng**: Không bao giờ dùng giá hiện tại từ bảng Pricing cho booking cũ
2. **Ceiling time units**: Luôn làm tròn LÊN (0.1 giờ = 1 giờ, 1.1 ngày = 2 ngày)
3. **Real-time chỉ ở frontend**: Backend vẫn tính lại khi submit form để bảo mật

### Hạn chế
- JavaScript có thể bị tắt bởi người dùng (hiếm)
- Giờ máy client có thể sai (nhưng server vẫn đúng khi submit)

### Cải tiến tương lai
- [ ] Thêm animation khi số tiền thay đổi
- [ ] Highlight màu đỏ khi quá giờ check-out
- [ ] Thông báo âm thanh khi phụ phí tăng
- [ ] Export hóa đơn PDF với snapshot thời điểm hiện tại

---

## ✅ Checklist triển khai

- [x] Thêm cột "Đơn giá" vào Reservation/Index.cshtml
- [x] Cập nhật CheckOutController.cs để dùng giá đã lưu
- [x] Sửa logic tính phụ phí trả muộn
- [x] Thêm ViewBag data cho JavaScript
- [x] Thêm IDs vào các phần tử HTML trong Details.cshtml
- [x] Viết JavaScript real-time update
- [x] Test với giá GIỜ
- [x] Test với giá NGÀY
- [x] Test phụ phí trả muộn
- [x] Test trừ tiền cọc
- [x] Viết tài liệu hướng dẫn

---

## 🎓 Tài liệu tham khảo

- [PRICING_FIX_GUIDE.md](./PRICING_FIX_GUIDE.md) - Hướng dẫn fix hệ thống pricing
- [CHECKIN_COUNTDOWN_FEATURE.md](./CHECKIN_COUNTDOWN_FEATURE.md) - Countdown timer check-in
- [STORED_PROCEDURES_IMPLEMENTATION.md](./STORED_PROCEDURES_IMPLEMENTATION.md) - Stored procedures

---

**Tác giả**: GitHub Copilot  
**Phiên bản**: 1.0  
**Ngày tạo**: 2024-01-XX
