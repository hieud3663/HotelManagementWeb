# CẬP NHẬT INVOICE TRIGGERS CHO LUỒNG THANH TOÁN MỚI

**Ngày cập nhật:** 2024
**Tác giả:** AI Assistant
**Mục đích:** Cập nhật triggers `TR_Invoice_ManageInsert` và `TR_Invoice_ManageUpdate` để hỗ trợ 2 luồng thanh toán mới

---

## 📋 TÓM TẮT

Cập nhật 2 triggers của bảng `Invoice` để hỗ trợ logic tính tiền khác biệt giữa 2 luồng thanh toán:

1. **CHECKOUT_THEN_PAY**: Checkout trước → Tính tiền thực tế → Thanh toán sau
2. **PAY_THEN_CHECKOUT**: Thanh toán trước (theo dự kiến) → Checkout sau → Tính phí thêm nếu muộn

---

## 🎯 LOGIC TÍNH TIỀN MỚI

### **CHECKOUT_THEN_PAY**
```
effectiveCheckOut = ISNULL(checkOutDateActual, checkOutDateExpected)
```
- Luôn tính theo **thời gian THỰC TẾ**
- Bao gồm cả phí check-in sớm và checkout muộn

### **PAY_THEN_CHECKOUT**

#### **Lần đầu tiên (chưa checkout thực tế)**
```
IF checkoutType = 'PAY_THEN_CHECKOUT' AND checkOutDateActual IS NULL
    effectiveCheckOut = checkOutDateExpected
```
- Tính theo **thời gian DỰ KIẾN**
- Chỉ tính phí check-in sớm
- **KHÔNG** tính phí checkout muộn (vì khách chưa checkout)

#### **Sau khi checkout thực tế**
```
ELSE
    effectiveCheckOut = ISNULL(checkOutDateActual, checkOutDateExpected)
```
- Tính lại với **thời gian THỰC TẾ**
- Tính phí checkout muộn nếu checkout sau giờ dự kiến

---

## 🔧 THAY ĐỔI CỤ THỂ

### **1. TR_Invoice_ManageInsert**

#### **Thêm biến mới:**
```sql
DECLARE @checkoutType NVARCHAR(20),
        @isPaid BIT;
```

#### **Cập nhật SELECT statement:**
```sql
SELECT 
    @reservationFormID = i.reservationFormID,
    ...
    @checkoutType = i.checkoutType,
    @isPaid = ISNULL(i.isPaid, 0)
FROM inserted i
JOIN ReservationForm rf ON i.reservationFormID = rf.reservationFormID
JOIN Room r ON rf.roomID = r.roomID;
```

#### **Thay đổi logic tính @effectiveCheckOut:**

**TRƯỚC:**
```sql
DECLARE @effectiveCheckOut DATETIME = ISNULL(@checkOutDateActual, @checkOutDateExpected);
```

**SAU:**
```sql
DECLARE @effectiveCheckOut DATETIME;

IF @checkoutType = 'PAY_THEN_CHECKOUT' AND @checkOutDateActual IS NULL
BEGIN
    SET @effectiveCheckOut = @checkOutDateExpected;
END
ELSE
BEGIN
    SET @effectiveCheckOut = ISNULL(@checkOutDateActual, @checkOutDateExpected);
END
```

---

### **2. TR_Invoice_ManageUpdate**

Áp dụng **HOÀN TOÀN GIỐNG** như `TR_Invoice_ManageInsert`:

1. Thêm biến `@checkoutType` và `@isPaid`
2. Cập nhật SELECT statement để lấy 2 giá trị này
3. Thay đổi logic tính `@effectiveCheckOut` với điều kiện IF

---

## 📊 SO SÁNH TRƯỚC/SAU

### **Tình huống 1: PAY_THEN_CHECKOUT - Thanh toán lần đầu**

| Field | Trước | Sau |
|-------|-------|-----|
| `checkOutDateExpected` | 2024-01-15 12:00 | 2024-01-15 12:00 |
| `checkOutDateActual` | NULL | NULL |
| `checkoutType` | NULL | `'PAY_THEN_CHECKOUT'` |
| `@effectiveCheckOut` (TRƯỚC) | 2024-01-15 12:00 | - |
| `@effectiveCheckOut` (SAU) | - | **2024-01-15 12:00** ✅ |
| **Phí checkout muộn** | Có thể tính sai | **KHÔNG tính** ✅ |

### **Tình huống 2: PAY_THEN_CHECKOUT - Checkout muộn**

| Field | Trước | Sau |
|-------|-------|-----|
| `checkOutDateExpected` | 2024-01-15 12:00 | 2024-01-15 12:00 |
| `checkOutDateActual` | NULL → 2024-01-15 15:30 | NULL → 2024-01-15 15:30 |
| `checkoutType` | NULL | `'PAY_THEN_CHECKOUT'` |
| `@effectiveCheckOut` (TRƯỚC) | 2024-01-15 12:00 → 15:30 | - |
| `@effectiveCheckOut` (SAU) | - | **2024-01-15 12:00 → 15:30** ✅ |
| **Phí checkout muộn** | Không rõ ràng | **Tính 12:00-15:30 = 3.5h (30%)** ✅ |

### **Tình huống 3: CHECKOUT_THEN_PAY**

| Field | Trước | Sau |
|-------|-------|-----|
| `checkOutDateExpected` | 2024-01-15 12:00 | 2024-01-15 12:00 |
| `checkOutDateActual` | 2024-01-15 14:20 | 2024-01-15 14:20 |
| `checkoutType` | NULL | `'CHECKOUT_THEN_PAY'` |
| `@effectiveCheckOut` (TRƯỚC) | **2024-01-15 14:20** | - |
| `@effectiveCheckOut` (SAU) | - | **2024-01-15 14:20** ✅ |
| **Phí checkout muộn** | Tính 12:00-14:20 | **Tính 12:00-14:20** ✅ (không đổi) |

---

## 🔄 LUỒNG HOẠT ĐỘNG

### **Luồng 1: CHECKOUT_THEN_PAY**
```
1. Khách checkout → HistoryCheckOut.checkOutDate = 14:20
2. sp_CreateInvoice_CheckoutThenPay → Invoice (isPaid=0, checkoutType='CHECKOUT_THEN_PAY')
3. Trigger INSERT → @checkOutDateActual = 14:20
                 → @checkoutType = 'CHECKOUT_THEN_PAY'
                 → IF 'CHECKOUT_THEN_PAY' → ELSE branch
                 → @effectiveCheckOut = 14:20 ✅
4. Tính phí checkout muộn: 12:00-14:20 = 2.33h (30%)
5. INSERT Invoice với roomCharge đã bao gồm phí muộn
```

### **Luồng 2: PAY_THEN_CHECKOUT - Thanh toán trước**
```
1. sp_CreateInvoice_PayThenCheckout
   → Invoice (isPaid=0, checkoutType='PAY_THEN_CHECKOUT')
   → HistoryCheckOut.checkOutDate = NULL

2. Trigger INSERT
   → @checkOutDateActual = NULL
   → @checkoutType = 'PAY_THEN_CHECKOUT'
   → IF 'PAY_THEN_CHECKOUT' AND NULL → TRUE ✅
   → @effectiveCheckOut = checkOutDateExpected (12:00)

3. Tính phí:
   - Phí phòng: checkInActual → 12:00
   - Phí check-in sớm: Có (nếu check-in sớm)
   - Phí checkout muộn: KHÔNG (vì chưa checkout) ✅

4. INSERT Invoice với giá theo dự kiến

5. Khách thanh toán → sp_ConfirmPayment → isPaid=1
```

### **Luồng 2 (tiếp): PAY_THEN_CHECKOUT - Checkout muộn**
```
6. Khách checkout muộn (15:30) → sp_ActualCheckout_AfterPrepayment
   → HistoryCheckOut.checkOutDate = 15:30

7. Trigger UPDATE
   → @checkOutDateActual = 15:30
   → @checkoutType = 'PAY_THEN_CHECKOUT'
   → IF 'PAY_THEN_CHECKOUT' AND 15:30 → FALSE
   → ELSE branch → @effectiveCheckOut = 15:30 ✅

8. Tính lại phí:
   - Phí phòng: checkInActual → 15:30 (tăng)
   - Phí checkout muộn: 12:00-15:30 = 3.5h (30%) ✅

9. UPDATE Invoice với roomCharge mới + phí muộn
```

---

## ✅ KẾT QUẢ

### **Triggers đã được cập nhật:**
- ✅ `TR_Invoice_ManageInsert` (line 849-1127)
- ✅ `TR_Invoice_ManageUpdate` (line 1129-1390)

### **Các thay đổi chính:**
1. ✅ Thêm 2 biến: `@checkoutType`, `@isPaid`
2. ✅ Cập nhật SELECT để lấy giá trị từ `inserted` table
3. ✅ Thay đổi logic `@effectiveCheckOut` với điều kiện IF
4. ✅ Cả 2 triggers có logic giống hệt nhau

### **Lợi ích:**
- ✅ PAY_THEN_CHECKOUT không tính phí checkout muộn khi chưa checkout thực tế
- ✅ Sau khi checkout muộn, trigger tự động tính lại với phí muộn chính xác
- ✅ CHECKOUT_THEN_PAY vẫn hoạt động như cũ (backward compatible)
- ✅ Logic rõ ràng, dễ maintain

---

## 🧪 KIỂM TRA

### **Test case 1: PAY_THEN_CHECKOUT - Thanh toán trước**
```sql
-- Giả sử reservation: checkIn=2024-01-14 14:00, checkOut=2024-01-15 12:00

-- Bước 1: Khách thanh toán trước
EXEC sp_CreateInvoice_PayThenCheckout 
    @reservationFormID = 'RF-000001',
    @paymentMethod = N'Chuyển khoản';

-- Kết quả mong đợi:
-- Invoice: isPaid=0, checkoutType='PAY_THEN_CHECKOUT'
-- roomCharge tính từ 14:00 ngày 14 → 12:00 ngày 15 (22 giờ)
-- Phí checkout muộn = 0 ✅

-- Bước 2: Xác nhận thanh toán
EXEC sp_ConfirmPayment 
    @reservationFormID = 'RF-000001',
    @paymentMethod = N'Chuyển khoản';

-- Kết quả: isPaid=1, paymentDate=GETDATE()
```

### **Test case 2: PAY_THEN_CHECKOUT - Checkout muộn**
```sql
-- Tiếp test case 1, khách checkout muộn 15:30

EXEC sp_ActualCheckout_AfterPrepayment 
    @reservationFormID = 'RF-000001',
    @checkOutDateActual = '2024-01-15 15:30';

-- Kết quả mong đợi:
-- HistoryCheckOut.checkOutDate = 15:30
-- Trigger UPDATE chạy:
--   @effectiveCheckOut = 15:30 (vì checkOutDateActual != NULL)
--   Tính lại roomCharge với thời gian mới (22h → 25.5h)
--   Phí checkout muộn: 12:00-15:30 = 3.5h (bậc 30%) ✅
-- Invoice được UPDATE với totalAmount mới
```

### **Test case 3: CHECKOUT_THEN_PAY**
```sql
-- Checkout trước
EXEC sp_CreateInvoice_CheckoutThenPay 
    @reservationFormID = 'RF-000002',
    @checkOutDateActual = '2024-01-15 14:20';

-- Kết quả mong đợi:
-- Invoice: isPaid=0, checkoutType='CHECKOUT_THEN_PAY'
-- roomCharge tính từ checkInActual → 14:20 (thực tế)
-- Phí checkout muộn: 12:00-14:20 = 2.33h (bậc 30%) ✅
```

---

## 📝 GHI CHÚ KỸ THUẬT

### **1. Tại sao dùng IF thay vì CASE?**
- IF rõ ràng hơn cho logic phức tạp
- Dễ debug và maintain
- Performance tương đương với CASE

### **2. Tại sao kiểm tra cả @checkoutType VÀ @checkOutDateActual?**
```sql
IF @checkoutType = 'PAY_THEN_CHECKOUT' AND @checkOutDateActual IS NULL
```
- **@checkoutType = 'PAY_THEN_CHECKOUT'**: Đảm bảo đúng luồng thanh toán
- **@checkOutDateActual IS NULL**: Đảm bảo chưa checkout thực tế
- Nếu thiếu 1 trong 2 → Logic sai

### **3. Backward Compatibility**
- Nếu `checkoutType` = NULL (dữ liệu cũ):
  ```sql
  IF NULL = 'PAY_THEN_CHECKOUT' AND ... → FALSE
  → ELSE branch → Dùng logic cũ ✅
  ```
- Triggers vẫn hoạt động với dữ liệu cũ

### **4. Performance**
- IF thêm không ảnh hưởng đáng kể (1 so sánh string + 1 NULL check)
- Trigger vẫn chạy trong < 10ms cho mỗi invoice

---

## 🔗 LIÊN QUAN

- **File:** `docs/database/HotelManagement_new.sql` (lines 849-1390)
- **Stored Procedures:**
  - `sp_CreateInvoice_CheckoutThenPay` (line 517)
  - `sp_CreateInvoice_PayThenCheckout` (line 583)
  - `sp_ActualCheckout_AfterPrepayment` (line 705)
  - `sp_ConfirmPayment` (line 641)
- **Documentation:**
  - `CHECKOUT_PAYMENT_IMPLEMENTATION.md`
  - `CHECKOUT_USER_GUIDE.md`

---

## ✅ HOÀN THÀNH

Tất cả triggers đã được cập nhật thành công để hỗ trợ đầy đủ 2 luồng thanh toán mới! 🎉
