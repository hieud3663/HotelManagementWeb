# Sửa Logic Tính Tiền và Phí Trễ trong CheckOut

## Ngày cập nhật
**2024-10-14** - Đồng bộ logic tính tiền giữa Stored Procedure và Controller/View

---

## 📋 Vấn đề

**Cách tính phí trễ không khớp** giữa:
- **Stored Procedure `sp_CheckoutRoom`** (backend - SQL Server)
- **Controller `CheckOutController.cs`** (backend - C#)
- **View `Details.cshtml`** (frontend - JavaScript)

---

## 🔍 Phân tích chi tiết

### **1. Tính tiền phòng (Room Charge)** ✅ **ĐÃ KHỚP**

#### Stored Procedure:
```sql
IF @priceUnit = 'DAY'
BEGIN
    DECLARE @daysUsed INT = CEILING(DATEDIFF(HOUR, @checkInDateActual, @actualCheckOutDate) / 24.0);
    SET @roomCharge = @daysUsed * @unitPrice;
END
ELSE IF @priceUnit = 'HOUR'
BEGIN
    DECLARE @hoursUsed INT = CEILING(DATEDIFF(MINUTE, @checkInDateActual, @actualCheckOutDate) / 60.0);
    SET @roomCharge = @hoursUsed * @unitPrice;
END
```

#### Controller & JavaScript:
```csharp
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

**Kết luận**: ✅ Logic giống nhau - dùng giá đã lưu (`@unitPrice`)

---

### **2. Tính phí trễ (Late Fee)** ❌ **KHÔNG KHỚP - ĐÃ SỬA**

#### Stored Procedure (Logic gốc):
```sql
IF @actualCheckOutDate > @checkOutDate
BEGIN
    DECLARE @hoursLate INT = DATEDIFF(HOUR, @checkOutDate, @actualCheckOutDate);
    DECLARE @hourlyRate MONEY;
    
    -- Lấy giá theo giờ từ Pricing (CHO PHÍ TRỄ)
    SELECT @hourlyRate = price 
    FROM Pricing 
    WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'HOUR';
    
    -- Nếu không có giá theo giờ, tính bằng 1/24 giá theo ngày
    IF @hourlyRate IS NULL
    BEGIN
        SELECT @hourlyRate = price / 24
        FROM Pricing 
        WHERE roomCategoryID = @roomCategoryID AND priceUnit = 'DAY';
    END
    
    -- Áp dụng phí trễ giờ
    IF @hoursLate > 0 AND @hourlyRate IS NOT NULL
    BEGIN
        SET @roomCharge = @roomCharge + (@hoursLate * @hourlyRate);
    END
END
```

**Đặc điểm:**
- Lấy giá từ bảng **Pricing** (giá hiện tại)
- Tính: `số giờ trễ × giá theo giờ`
- Không có logic phân cấp

#### Controller (Logic CŨ - ĐÃ XÓA):
```csharp
// ❌ LOGIC CŨ - KHÔNG KHỚP VỚI PROCEDURE
decimal lateFee = 0;
if (checkOutDate > reservation.CheckOutDate)
{
    var lateHours = (checkOutDate - reservation.CheckOutDate).TotalHours;
    if (priceUnit == "DAY")
    {
        if (lateHours <= 2)
            lateFee = unitPrice * 0.25m; // 25% giá ngày
        else if (lateHours <= 6)
            lateFee = unitPrice * 0.5m;  // 50% giá ngày
        else
            lateFee = unitPrice;         // 100% giá ngày
    }
    else // HOUR
    {
        lateFee = unitPrice * (decimal)Math.Ceiling(lateHours);
    }
}
```

**Vấn đề:**
- Dùng giá đã lưu (`unitPrice`) thay vì giá hiện tại
- Có logic phân cấp (25%/50%/100%) - Procedure không có

---

## ✅ Giải pháp đã áp dụng

### **1. Sửa Controller**

File: `Controllers/CheckOutController.cs`

#### Thay đổi:
```csharp
// ✅ LOGIC MỚI - KHỚP VỚI PROCEDURE
decimal lateFee = 0;
if (checkOutDate > reservation.CheckOutDate)
{
    var hoursLate = (int)Math.Floor((checkOutDate - reservation.CheckOutDate).TotalHours);
    
    if (hoursLate > 0)
    {
        // Lấy giá theo giờ từ Pricing (cho phí trễ giờ)
        var hourlyRate = await _context.Pricings
            .Where(p => p.RoomCategoryID == reservation.Room!.RoomCategoryID && p.PriceUnit == "HOUR")
            .Select(p => p.Price)
            .FirstOrDefaultAsync();
        
        // Nếu không có giá theo giờ, tính bằng 1/24 giá theo ngày
        if (hourlyRate == 0)
        {
            var dayPrice = await _context.Pricings
                .Where(p => p.RoomCategoryID == reservation.Room!.RoomCategoryID && p.PriceUnit == "DAY")
                .Select(p => p.Price)
                .FirstOrDefaultAsync();
            
            if (dayPrice > 0)
            {
                hourlyRate = dayPrice / 24;
            }
        }
        
        // Áp dụng phí trễ giờ
        if (hourlyRate > 0)
        {
            lateFee = hoursLate * hourlyRate;
        }
    }
}
```

#### Thêm ViewBag cho JavaScript:
```csharp
// Lấy giá theo giờ từ Pricing cho phí trễ (để JavaScript tính real-time)
var hourlyRateForLateFee = await _context.Pricings
    .Where(p => p.RoomCategoryID == reservation.Room!.RoomCategoryID && p.PriceUnit == "HOUR")
    .Select(p => p.Price)
    .FirstOrDefaultAsync();

if (hourlyRateForLateFee == 0)
{
    var dayPriceForLateFee = await _context.Pricings
        .Where(p => p.RoomCategoryID == reservation.Room!.RoomCategoryID && p.PriceUnit == "DAY")
        .Select(p => p.Price)
        .FirstOrDefaultAsync();
    
    if (dayPriceForLateFee > 0)
    {
        hourlyRateForLateFee = dayPriceForLateFee / 24;
    }
}

ViewBag.HourlyRateForLateFee = hourlyRateForLateFee;
```

---

### **2. Sửa View (JavaScript)**

File: `Views/CheckOut/Details.cshtml`

#### Thay đổi:
```javascript
// ✅ LOGIC MỚI - KHỚP VỚI PROCEDURE
const hourlyRateForLateFee = @ViewBag.HourlyRateForLateFee; // Giá theo giờ từ Pricing

function updateInvoice() {
    // ... (phần tính tiền phòng giữ nguyên)
    
    // Calculate late fee (giống như trong Procedure)
    let lateFee = 0;
    if (currentTime > expectedCheckOutDate) {
        const lateMs = currentTime - expectedCheckOutDate;
        const lateHours = Math.floor(lateMs / (1000 * 60 * 60)); // Làm tròn xuống như DATEDIFF(HOUR,...)
        
        if (lateHours > 0 && hourlyRateForLateFee > 0) {
            lateFee = lateHours * hourlyRateForLateFee;
        }
    }
    
    // ... (phần tính tổng giữ nguyên)
}
```

**Logic CŨ (đã xóa):**
```javascript
// ❌ LOGIC CŨ - ĐÃ XÓA
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
```

---

## 📊 So sánh TRƯỚC và SAU

| Aspect | TRƯỚC (Controller/View) | SAU (Khớp với Procedure) |
|--------|------------------------|--------------------------|
| **Nguồn giá cho phí trễ** | ❌ Giá đã lưu (`unitPrice`) | ✅ Giá hiện tại từ bảng `Pricing` |
| **Logic phân cấp** | ❌ Có (25%/50%/100%) | ✅ Không có (giống Procedure) |
| **Cách tính** | ❌ Phức tạp, khác Procedure | ✅ Đơn giản: `lateHours × hourlyRate` |
| **Làm tròn số giờ** | ❌ `Math.ceil()` (lên) | ✅ `Math.floor()` (xuống) - giống `DATEDIFF(HOUR,...)` |
| **Real-time update** | ❌ Sử dụng `unitPrice` sai | ✅ Sử dụng `hourlyRateForLateFee` đúng |

---

## ⚙️ Công thức tính (sau khi sửa)

### Tiền phòng (Room Charge)
```
if (PriceUnit == "DAY"):
    timeUnits = CEILING(TotalDays)
    roomCharge = unitPrice × timeUnits
else if (PriceUnit == "HOUR"):
    timeUnits = CEILING(TotalHours)
    roomCharge = unitPrice × timeUnits
```

### Phí trễ (Late Fee)
```
if (checkOutDate > expectedCheckOutDate):
    lateHours = FLOOR((checkOutDate - expectedCheckOutDate) / 1 hour)
    
    // Lấy giá theo giờ từ Pricing
    hourlyRate = SELECT price FROM Pricing WHERE priceUnit = 'HOUR'
    
    // Nếu không có, tính từ giá ngày
    if (hourlyRate == 0):
        dayPrice = SELECT price FROM Pricing WHERE priceUnit = 'DAY'
        hourlyRate = dayPrice / 24
    
    lateFee = lateHours × hourlyRate
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

### 1. Tính nhất quán
- ✅ **3 lớp (Procedure, Controller, JavaScript) dùng CÙNG 1 LOGIC**
- ✅ Kết quả tính toán giống nhau ở mọi nơi
- ✅ Không còn sai lệch giữa hiển thị real-time và kết quả thực tế

### 2. Tính chính xác
- ✅ Phí trễ dựa trên giá hiện tại từ bảng `Pricing` (linh hoạt khi đổi giá)
- ✅ Làm tròng số giờ trễ xuống (`Math.floor`) giống `DATEDIFF(HOUR,...)` của SQL Server
- ✅ Logic đơn giản, dễ kiểm tra và debug

### 3. Bảo trì
- ✅ Chỉ cần sửa logic ở 1 nơi (Procedure), các nơi khác copy theo
- ✅ Dễ hiểu, dễ mở rộng

---

## 🧪 Test Cases

### Test 1: Trả phòng đúng giờ
- **Dự kiến check-out**: 2024-10-14 12:00
- **Thực tế check-out**: 2024-10-14 12:00
- **Kết quả**: `lateFee = 0` ✅

### Test 2: Trả phòng muộn 3 giờ
- **Dự kiến check-out**: 2024-10-14 12:00
- **Thực tế check-out**: 2024-10-14 15:00
- **Giá theo giờ**: 150,000 VNĐ
- **Phí trễ**: `3 × 150,000 = 450,000 VNĐ` ✅

### Test 3: Trả phòng muộn 30 phút
- **Dự kiến check-out**: 2024-10-14 12:00
- **Thực tế check-out**: 2024-10-14 12:30
- **Late hours**: `Math.floor(0.5) = 0`
- **Phí trễ**: `0 × 150,000 = 0 VNĐ` ✅ (không tính phí nếu < 1 giờ)

### Test 4: Không có giá theo giờ
- **Dự kiến check-out**: 2024-10-14 12:00
- **Thực tế check-out**: 2024-10-14 16:00
- **Giá theo ngày**: 1,200,000 VNĐ
- **Hourly rate**: `1,200,000 / 24 = 50,000 VNĐ`
- **Phí trễ**: `4 × 50,000 = 200,000 VNĐ` ✅

---

## 📝 Lưu ý

### Điểm quan trọng
1. **Phí trễ LUÔN lấy giá từ bảng `Pricing`** (giá hiện tại), không dùng giá đã lưu
2. **Tiền phòng dùng giá đã lưu** (`unitPrice`) khi đặt phòng
3. **Làm tròn xuống** (`Math.floor`) số giờ trễ để khớp với `DATEDIFF(HOUR,...)` của SQL

### Hành vi mới
- Nếu trả phòng muộn **dưới 1 giờ** → Không tính phí trễ
- Nếu trả phòng muộn **từ 1 giờ trở lên** → Tính phí theo từng giờ

### So sánh với logic cũ
- **Cũ**: Giá theo ngày có phân cấp (25%/50%/100%) theo thời gian trễ
- **Mới**: Đơn giản, tính theo số giờ × giá giờ (không phân cấp)

---

## 🔄 Quy trình cập nhật

1. ✅ **Sửa Controller** để lấy giá từ `Pricing` thay vì dùng `unitPrice`
2. ✅ **Thêm ViewBag** `HourlyRateForLateFee` cho JavaScript
3. ✅ **Sửa JavaScript** để dùng `hourlyRateForLateFee` và logic mới
4. ✅ **Test** để đảm bảo kết quả khớp giữa 3 lớp

---

## ✅ Checklist hoàn thành

- [x] Phân tích sự khác biệt giữa Procedure và Controller
- [x] Sửa logic tính phí trễ trong Controller
- [x] Thêm ViewBag cho giá theo giờ (phí trễ)
- [x] Sửa JavaScript trong View
- [x] Test với các trường hợp khác nhau
- [x] Viết tài liệu hướng dẫn

---

**Tác giả**: GitHub Copilot  
**Phiên bản**: 1.0  
**Ngày tạo**: 2024-10-14
