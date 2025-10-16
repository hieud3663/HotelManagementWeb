# Logic tính phí Check-in sớm và Check-out muộn

## 📋 Quy định thực tế khách sạn

### 1. **Check-in sớm (Early Check-in)**

#### Miễn phí:
- **Giá theo GIỜ**: Check-in sớm **≤ 30 phút** → Miễn phí
- **Giá theo NGÀY**: Check-in sớm **≤ 1 tiếng** → Miễn phí

#### Tính phí (nếu vượt quá miễn phí):
- **05:00 - 09:00**: Phụ thu **50%** giá phòng 1 ngày
- **09:00 - 14:00**: Phụ thu **30%** giá phòng 1 ngày
- **≥ 14:00**: Không tính phí (giờ check-in chuẩn)

**Ví dụ:**
- Giá phòng: 2.000.000 đ/ngày
- Check-in dự kiến: 14:00 ngày 17/10
- Check-in thực tế: 11:00 ngày 17/10
- Phụ thu: 30% × 2.000.000 = **600.000 đ**

---

### 2. **Check-out muộn (Late Check-out)**

#### Miễn phí:
- **Giá theo GIỜ**: Check-out muộn **≤ 30 phút** → Miễn phí
- **Giá theo NGÀY**: Check-out muộn **≤ 1 tiếng** → Miễn phí

#### Tính phí (nếu vượt quá miễn phí):
- **12:00 - 15:00**: Phụ thu **30%** giá phòng 1 ngày
- **15:00 - 18:00**: Phụ thu **50%** giá phòng 1 ngày
- **≥ 18:00**: Phụ thu **100%** giá phòng 1 ngày

**Ví dụ:**
- Giá phòng: 2.000.000 đ/ngày
- Check-out dự kiến: 12:00 ngày 18/10
- Check-out thực tế: 16:30 ngày 18/10
- Phụ thu: 50% × 2.000.000 = **1.000.000 đ**

---

## 🧮 Công thức tính toán

### **Tính phí check-in sớm:**

```csharp
decimal earlyCheckinFee = 0;
TimeSpan earlyTime = expectedCheckinTime - actualCheckinTime;

// Miễn phí nếu trong khoảng cho phép
int freeMinutes = (priceUnit == "HOUR") ? 30 : 60;
if (earlyTime.TotalMinutes <= freeMinutes)
{
    earlyCheckinFee = 0;
}
else
{
    var hour = actualCheckinTime.Hour;
    decimal surchargeRate = 0;
    
    if (hour >= 5 && hour < 9)
        surchargeRate = 0.5m; // 50%
    else if (hour >= 9 && hour < 14)
        surchargeRate = 0.3m; // 30%
    else
        surchargeRate = 0; // >= 14h không tính phí
    
    earlyCheckinFee = dayPrice * surchargeRate;
}
```

### **Tính phí check-out muộn:**

```csharp
decimal lateCheckoutFee = 0;
TimeSpan lateTime = actualCheckoutTime - expectedCheckoutTime;

// Miễn phí nếu trong khoảng cho phép
int freeMinutes = (priceUnit == "HOUR") ? 30 : 60;
if (lateTime.TotalMinutes <= freeMinutes)
{
    lateCheckoutFee = 0;
}
else
{
    var hour = actualCheckoutTime.Hour;
    decimal surchargeRate = 0;
    
    if (hour >= 12 && hour < 15)
        surchargeRate = 0.3m; // 30%
    else if (hour >= 15 && hour < 18)
        surchargeRate = 0.5m; // 50%
    else if (hour >= 18)
        surchargeRate = 1.0m; // 100%
    
    lateCheckoutFee = dayPrice * surchargeRate;
}
```

---

## 📊 Ví dụ tính toán cụ thể

### **Ví dụ 1: Check-in sớm + Check-out muộn**

**Thông tin:**
- Giá phòng: 2.200.000 đ/ngày
- Check-in dự kiến: 14:00 ngày 17/10
- Check-in thực tế: 11:00 ngày 17/10 (sớm 3 tiếng)
- Check-out dự kiến: 12:00 ngày 18/10
- Check-out thực tế: 18:00 ngày 18/10 (muộn 6 tiếng)

**Tính toán:**
1. **Tiền phòng chuẩn:** 2.200.000 đ (14:00 → 12:00)
2. **Phí check-in sớm:** 30% × 2.200.000 = 660.000 đ (11:00 trong khung 09:00-14:00)
3. **Phí check-out muộn:** 50% × 2.200.000 = 1.100.000 đ (18:00 trong khung 15:00-18:00)
4. **Tổng cộng:** 2.200.000 + 660.000 + 1.100.000 = **3.960.000 đ**

---

### **Ví dụ 2: Check-out muộn trong giới hạn miễn phí**

**Thông tin:**
- Giá phòng: 500.000 đ/giờ
- Check-out dự kiến: 17:05
- Check-out thực tế: 17:25 (muộn 20 phút)

**Tính toán:**
1. Muộn 20 phút < 30 phút (miễn phí với giá theo GIỜ)
2. **Phí check-out muộn:** 0 đ
3. **Tổng tiền:** Chỉ tính tiền phòng chuẩn

---

### **Ví dụ 3: Check-out muộn vượt miễn phí**

**Thông tin:**
- Giá phòng: 2.000.000 đ/ngày
- Check-out dự kiến: 12:00
- Check-out thực tế: 12:45 (muộn 45 phút)

**Tính toán:**
1. Muộn 45 phút < 1 tiếng → **Vẫn miễn phí** (với giá theo NGÀY)
2. **Phí check-out muộn:** 0 đ

---

## 🔧 Implementation Plan

### **1. Thêm cột vào bảng Invoice**

```sql
ALTER TABLE Invoice
ADD EarlyCheckinFee MONEY DEFAULT 0,
    LateCheckoutFee MONEY DEFAULT 0;
```

### **2. Sửa Trigger TR_Invoice_ManageInsert**

Thêm logic tính phí check-in sớm và check-out muộn theo quy định mới.

### **3. Sửa Stored Procedure sp_QuickCheckout**

Áp dụng logic tính phí tương tự trigger.

### **4. Sửa CheckOutController.cs**

Thêm logic tính phí check-in sớm và check-out muộn, truyền ViewBag ra view.

### **5. Sửa View CheckOut/Details.cshtml**

Hiển thị:
- Phí check-in sớm (nếu có)
- Phí check-out muộn (nếu có)
- Tổng tiền = Tiền phòng + Phí sớm + Phí muộn + Dịch vụ

---

## 📝 Testing Scenarios

### Test 1: Check-in sớm 05:00-09:00
- ✅ Phụ thu 50% giá ngày
- ✅ Hiển thị đúng trong hóa đơn

### Test 2: Check-in sớm 09:00-14:00
- ✅ Phụ thu 30% giá ngày
- ✅ Hiển thị đúng trong hóa đơn

### Test 3: Check-out muộn 12:00-15:00
- ✅ Phụ thu 30% giá ngày
- ✅ Hiển thị đúng trong hóa đơn

### Test 4: Check-out muộn 15:00-18:00
- ✅ Phụ thu 50% giá ngày
- ✅ Hiển thị đúng trong hóa đơn

### Test 5: Check-out muộn >= 18:00
- ✅ Phụ thu 100% giá ngày
- ✅ Hiển thị đúng trong hóa đơn

### Test 6: Trong giới hạn miễn phí
- ✅ Không tính phí
- ✅ Hiển thị "Miễn phí" trong hóa đơn

---

**Lưu ý:**
- Logic này áp dụng cho cả **ReservationForm** (đặt trước) và **HistoryCheckin** (check-in thực tế).
- Phí check-in sớm chỉ tính khi có **HistoryCheckin** với thời gian sớm hơn dự kiến.
- Phí check-out muộn chỉ tính khi có **HistoryCheckOut** với thời gian muộn hơn dự kiến.
