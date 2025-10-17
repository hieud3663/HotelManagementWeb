# 📋 Hướng dẫn: Cách tính Phí Check-in Sớm và Check-out Muộn

## 📌 Tổng quan

Hệ thống tính phí check-in sớm và check-out muộn dựa trên **2 loại giá**:
- **Giá GIỜ (HOUR)**: Tính theo nấc bậc thang
- **Giá NGÀY (DAY)**: Tính theo khung giờ tích lũy

---

## 🕐 GIẢI THÍCH KHUNG GIỜ

### Phí Check-in Sớm (Early Check-in Fee)
Khi khách check-in **trước giờ dự kiến** vượt quá **thời gian miễn phí** (30 phút cho giá GIỜ, 60 phút cho giá NGÀY):

| Khung giờ | Mức phí | Áp dụng |
|-----------|--------|--------|
| 05:00 - 09:00 | **50% giá ngày** | Sáng sớm |
| 09:00 - 14:00 | **30% giá ngày** | Sáng chiều |
| Khác (14:00-05:00) | **0%** | Miễn phí |

**Ví dụ 1:**
```
- Dự kiến check-in: 14/10 14:00
- Thực tế check-in: 14/10 07:00 (sớm 7 tiếng)
- Miễn phí: 60 phút
- Tính phí: 6 tiếng 60 phút
  - 07:00-09:00 (2h): 2/24 × 500k × 50% = ~20.8k
  - 09:00-14:00 (5h): 5/24 × 500k × 30% = ~31.25k
  - Tổng phí sớm: ~52k VNĐ
```

---

### Phí Check-out Muộn (Late Checkout Fee)
Khi khách check-out **sau giờ dự kiến** vượt quá **thời gian miễn phí** (30 phút cho giá GIỜ, 60 phút cho giá NGÀY):

| Khung giờ | Mức phí | Áp dụng |
|-----------|--------|--------|
| 12:00 - 15:00 | **30% giá ngày** | Trưa |
| 15:00 - 18:00 | **50% giá ngày** | Chiều |
| 18:00+ | **100% giá ngày** | Tối (gấp đôi) |

**Ví dụ 2:**
```
- Dự kiến check-out: 16/10 12:00
- Thực tế check-out: 16/10 16:30 (muộn 4h 30 phút)
- Miễn phí: 60 phút
- Tính phí: 3 giờ 30 phút
  - 13:00-15:00 (2h): 2/24 × 500k × 30% = ~12.5k
  - 15:00-16:30 (1h30): 1.5/24 × 500k × 50% = ~15.625k
  - Tổng phí muộn: ~28.1k VNĐ
```

---

## 💻 CODE TRONG CheckOutController

### 1️⃣ Hàm `CalculateHourlyFee()` - Tính phí theo nấc bậc (Giá GIỜ)

**Vị trí:** Dòng 21-46

```csharp
private decimal CalculateHourlyFee(double totalMinutes, decimal hourlyRate)
{
    if (totalMinutes <= 0) return 0;

    var totalHours = Math.Ceiling(totalMinutes / 60.0);
    decimal totalFee = 0;

    // 2 giờ đầu: 100% giá
    var first2Hours = Math.Min(totalHours, 2);
    totalFee += (decimal)first2Hours * hourlyRate;

    // Từ giờ 3-6: 80% giá
    if (totalHours > 2)
    {
        var next4Hours = Math.Min(totalHours - 2, 4);
        totalFee += (decimal)next4Hours * hourlyRate * 0.8m;
    }

    // Từ giờ thứ 7 trở đi: 80% giá
    if (totalHours > 6)
    {
        var remainingHours = totalHours - 6;
        totalFee += (decimal)remainingHours * hourlyRate * 0.8m;
    }

    return totalFee;
}
```

**Cách tính cho giá GIỜ:**
```
Ví dụ: Checkout muộn 5 giờ, giá giờ = 100k

Nấc 1: 2 giờ đầu × 100k = 200k (100%)
Nấc 2: 3 giờ tiếp theo × 100k × 80% = 240k (80%)
────────────────
Tổng: 440k
```

---

### 2️⃣ Tính phí Check-in Sớm - Giá NGÀY

**Vị trí:** Dòng 146-190

```csharp
// TÍNH PHÍ CHECK-IN SỚM (khung giờ)
decimal totalFee = 0;
DateTime currentTime = actualCheckInDate;
DateTime endTime = expectedCheckInDate;

while (currentTime < endTime)
{
    int hour = currentTime.Hour;
    decimal surchargeRate = 0;

    // Xác định mức phí theo khung giờ
    if (hour >= 5 && hour < 9)
        surchargeRate = 0.5m;           // 5-9h: 50% giá ngày
    else if (hour >= 9 && hour < 14)
        surchargeRate = 0.3m;           // 9-14h: 30% giá ngày

    // Tính biên của khung giờ hiện tại
    DateTime bracketEnd;
    if (hour >= 5 && hour < 9)
        bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 9, 0, 0);
    else if (hour >= 9 && hour < 14)
        bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 14, 0, 0);
    else
        bracketEnd = currentTime.AddHours(1);

    // Tính phút trong khung giờ này
    DateTime actualBracketEnd = bracketEnd < endTime ? bracketEnd : endTime;
    var minutesInBracket = (actualBracketEnd - currentTime).TotalMinutes;

    // Cộng phí cho khung giờ này
    if (surchargeRate > 0)
    {
        totalFee += (decimal)(minutesInBracket / 1440.0) * dayPrice * surchargeRate;
    }

    currentTime = actualBracketEnd;
}

earlyCheckinFee = totalFee;
```

**Luồng tính toán:**
1. Vòng lặp qua từng phút từ `actualCheckInDate` → `expectedCheckInDate`
2. Xác định khung giờ của từng phút
3. Áp dụng mức phí tương ứng (50%, 30%, hoặc 0%)
4. Tính phần trăm của ngày: `phút / 1440` (1440 phút = 1 ngày)
5. Cộng dồn: `giá ngày × % ngày × mức phí`

**Ví dụ chi tiết:**
```
Dữ liệu:
- Check-in thực tế: 07:00 (7 tiếng sớm)
- Check-in dự kiến: 14:00
- Giá ngày: 500k
- Miễn phí: 60 phút

Vòng lặp 1 (07:00-09:00): 2 giờ
  - Khung 5-9h: 50% phí
  - Phí = 2/24 × 500k × 50% = 20.83k

Vòng lặp 2 (09:00-14:00): 5 giờ
  - Khung 9-14h: 30% phí
  - Phí = 5/24 × 500k × 30% = 31.25k

Tổng: 20.83k + 31.25k = 52.08k VNĐ
(Đã trừ 60 phút miễn phí)
```

---

### 3️⃣ Tính phí Check-out Muộn - Giá NGÀY

**Vị trị:** Dòng 200-244

```csharp
// TÍNH PHÍ CHECK-OUT MUỘN (khung giờ)
decimal totalFee = 0;
DateTime currentTime = expectedCheckOutDate.AddMinutes(freeMinutes);
DateTime endTime = actualCheckOutDate;

while (currentTime < endTime)
{
    int hour = currentTime.Hour;
    decimal surchargeRate = 0;

    // Xác định mức phí theo khung giờ
    if (hour >= 12 && hour < 15)
        surchargeRate = 0.3m;    // 12-15h: 30%
    else if (hour >= 15 && hour < 18)
        surchargeRate = 0.5m;    // 15-18h: 50%
    else if (hour >= 18)
        surchargeRate = 1.0m;    // 18h+: 100% (gấp đôi)

    // Tính biên của khung giờ hiện tại
    DateTime bracketEnd;
    if (hour >= 12 && hour < 15)
        bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 15, 0, 0);
    else if (hour >= 15 && hour < 18)
        bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 18, 0, 0);
    else if (hour >= 18)
        bracketEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 23, 59, 59);
    else
        bracketEnd = currentTime.AddHours(1);

    // Tính phút trong khung giờ này
    DateTime actualBracketEnd = bracketEnd < endTime ? bracketEnd : endTime;
    var minutesInBracket = (actualBracketEnd - currentTime).TotalMinutes;

    // Cộng phí cho khung giờ này
    if (surchargeRate > 0)
    {
        totalFee += (decimal)(minutesInBracket / 1440.0) * dayPrice * surchargeRate;
    }

    currentTime = actualBracketEnd;
}

lateCheckoutFee = totalFee;
```

**Ví dụ chi tiết:**
```
Dữ liệu:
- Check-out dự kiến: 12:00
- Check-out thực tế: 16:30 (muộn 4h 30 phút)
- Giá ngày: 500k
- Miễn phí: 60 phút

Điểm bắt đầu tính: 13:00 (12:00 + 60 phút)

Vòng lặp 1 (13:00-15:00): 2 giờ
  - Khung 12-15h: 30% phí
  - Phí = 2/24 × 500k × 30% = 12.5k

Vòng lặp 2 (15:00-16:30): 1.5 giờ
  - Khung 15-18h: 50% phí
  - Phí = 1.5/24 × 500k × 50% = 15.625k

Tổng: 12.5k + 15.625k = 28.125k VNĐ
```

---

## 🔄 QUY TRÌNH TÍNH TOÁN ĐẦY ĐỦ

### Bước 1: Tính tiền phòng CHUẨN
```csharp
// Tính từ expected check-in → expected check-out
if (priceUnit == "DAY") {
    var bookingMinutes = (expectedCheckOutDate - expectedCheckInDate).TotalMinutes;
    timeUnits = Math.Ceiling(bookingMinutes / 1440.0); // Làm tròn lên số ngày
    roomCharge = unitPrice * timeUnits;
}
```

### Bước 2: Tính phí check-in sớm (nếu có)
```csharp
if (actualCheckInDate < expectedCheckInDate) {
    // Tính phút sớm
    var earlyMinutes = (expectedCheckInDate - actualCheckInDate).TotalMinutes;
    
    // Trừ thời gian miễn phí
    if (earlyMinutes > freeMinutes) {
        // Gọi CalculateHourlyFee hoặc tính khung giờ
        earlyCheckinFee = CalculateFee(...);
    }
}
```

### Bước 3: Tính phí check-out muộn (nếu có)
```csharp
if (actualCheckOutDate > expectedCheckOutDate) {
    // Tính phút muộn
    var lateMinutes = (actualCheckOutDate - expectedCheckOutDate).TotalMinutes;
    
    // Trừ thời gian miễn phí
    if (lateMinutes > freeMinutes) {
        // Gọi CalculateHourlyFee hoặc tính khung giờ
        lateCheckoutFee = CalculateFee(...);
    }
}
```

### Bước 4: Tính tổng
```csharp
var subTotal = roomCharge + servicesCharge + earlyCheckinFee + lateCheckoutFee;
var taxAmount = subTotal * 0.1m; // VAT 10%
var totalAmount = subTotal + taxAmount;
var amountDue = totalAmount - deposit;
```

---

## 📊 BẢNG TÓMS TẮT

| Loại giá | Phí check-in sớm | Phí check-out muộn |
|---------|-----------------|-------------------|
| **GIỜ** | Nấc: 100% (2h) + 80% (h 3+) | Nấc: 100% (2h) + 80% (h 3+) |
| **NGÀY** | Khung: 50% (5-9h) / 30% (9-14h) / 0% (khác) | Khung: 30% (12-15h) / 50% (15-18h) / 100% (18h+) |

---

## 🔍 CÁC BIẾN QUAN TRỌNG TRONG CODE

| Biến | Ý nghĩa | Ví dụ |
|-----|---------|-------|
| `actualCheckInDate` | Thời gian check-in thực tế | 14/10 07:00 |
| `expectedCheckInDate` | Thời gian check-in dự kiến | 14/10 14:00 |
| `actualCheckOutDate` | Thời gian check-out thực tế | 16/10 16:30 |
| `expectedCheckOutDate` | Thời gian check-out dự kiến | 16/10 12:00 |
| `unitPrice` | Đơn giá đặt phòng | 500000 VNĐ/ngày |
| `dayPrice` | Giá theo ngày (dùng tính phí) | 500000 VNĐ |
| `priceUnit` | Đơn vị giá | "DAY" hoặc "HOUR" |
| `freeMinutes` | Thời gian miễn phí | 60 phút (ngày) / 30 phút (giờ) |

---

## ⚠️ LƯU Ý QUAN TRỌNG

1. **Miễn phí thời gian:** 
   - Giá GIỜ: 30 phút
   - Giá NGÀY: 60 phút

2. **Khung giờ tích lũy:** 
   - Hệ thống duyệt qua từng phút và áp dụng khung giờ tương ứng
   - Không phải lấy khung giờ của thời gian bắt đầu

3. **Làm tròn giờ/ngày:** 
   - Dùng `Math.Ceiling()` để làm tròn **lên** (2.5 giờ → 3 giờ)

4. **Tính toán real-time:** 
   - Controller tính 1 lần khi load trang
   - JavaScript tính lại mỗi giây nếu checkout muộn hơn dự kiến

---

## 📞 VÍ DỤ TÍNH TOÁN ĐẦY ĐỦ

```
🏨 HOÀN CẢNH
- Loại giá: NGÀY
- Giá ngày: 500,000 VNĐ
- Đơn giá đặt: 500,000 VNĐ/ngày
- Check-in dự kiến: 14/10 14:00
- Check-out dự kiến: 16/10 12:00 (2 ngày)
- Check-in thực tế: 14/10 07:00 (sớm 7h)
- Check-out thực tế: 16/10 16:30 (muộn 4h 30p)
- Tiền cọc: 500,000 VNĐ
- Dịch vụ: 0 VNĐ

📊 TÍNH TOÁN

1️⃣ Tiền phòng:
   = 500,000 × 2 ngày
   = 1,000,000 VNĐ

2️⃣ Phí check-in sớm (07:00-14:00, trừ 60p miễn phí):
   • 07:00-09:00 (2h): 2/24 × 500k × 50% = 20,833 VNĐ
   • 09:00-14:00 (5h): 5/24 × 500k × 30% = 31,250 VNĐ
   = 52,083 VNĐ

3️⃣ Phí check-out muộn (13:00-16:30, trừ 60p miễn phí):
   • 13:00-15:00 (2h): 2/24 × 500k × 30% = 12,500 VNĐ
   • 15:00-16:30 (1.5h): 1.5/24 × 500k × 50% = 15,625 VNĐ
   = 28,125 VNĐ

4️⃣ Tổng trước thuế:
   = 1,000,000 + 52,083 + 28,125
   = 1,080,208 VNĐ

5️⃣ VAT 10%:
   = 1,080,208 × 0.1
   = 108,021 VNĐ

6️⃣ Tổng cộng:
   = 1,080,208 + 108,021
   = 1,188,229 VNĐ

7️⃣ Còn phải thanh toán:
   = 1,188,229 - 500,000 (tiền cọc)
   = 688,229 VNĐ ✅
```

---

## 🔗 LIÊN KẾT CÓ LIÊN QUAN

- `CheckOutController.cs` - Dòng 21-46 (CalculateHourlyFee)
- `CheckOutController.cs` - Dòng 146-190 (Tính phí sớm)
- `CheckOutController.cs` - Dòng 200-244 (Tính phí muộn)
- `Views/CheckOut/Details.cshtml` - Hiển thị hóa đơn real-time

---

**Cập nhật lần cuối: 17/10/2025** ✅