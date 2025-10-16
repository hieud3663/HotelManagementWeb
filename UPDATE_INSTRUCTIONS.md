# Hướng dẫn cập nhật Logic tính phí Check-in/Check-out

## 📋 Tổng quan thay đổi

Hệ thống đã được cập nhật logic tính phí theo quy định thực tế của khách sạn:

### ✅ **Phí Check-in sớm**
- **Miễn phí**: ≤ 30 phút (giá GIỜ) hoặc ≤ 1 tiếng (giá NGÀY)
- **05:00-09:00**: Phụ thu **50%** giá phòng/ngày
- **09:00-14:00**: Phụ thu **30%** giá phòng/ngày
- **≥ 14:00**: Không tính phí

### ✅ **Phí Check-out muộn**
- **Miễn phí**: ≤ 30 phút (giá GIỜ) hoặc ≤ 1 tiếng (giá NGÀY)
- **12:00-15:00**: Phụ thu **30%** giá phòng/ngày
- **15:00-18:00**: Phụ thu **50%** giá phòng/ngày
- **≥ 18:00**: Phụ thu **100%** giá phòng/ngày

---

## 🔧 Các file đã thay đổi

### 1. **Backend (Controller)**
- ✅ `Controllers/CheckOutController.cs` - Đã cập nhật logic tính phí

### 2. **Database**
- ✅ `docs/database/HotelManagement_new.sql` - Đã cập nhật Triggers và Stored Procedures
- ✅ `docs/database/UPDATE_PRICING_LOGIC.sql` - Script cập nhật riêng (dùng để chạy lại nếu cần)

### 3. **Tài liệu**
- ✅ `CHECKIN_CHECKOUT_PRICING_LOGIC.md` - Chi tiết logic và ví dụ tính toán

---

## 🚀 Cách chạy cập nhật

### **Bước 1: Cập nhật Database**

Chạy script SQL để cập nhật triggers:

```powershell
# Mở SQL Server Management Studio (SSMS) hoặc dùng sqlcmd
sqlcmd -S localhost -d HotelManagement -E -i "docs\database\UPDATE_PRICING_LOGIC.sql"
```

**Hoặc** copy nội dung file `UPDATE_PRICING_LOGIC.sql` và chạy trong SSMS.

### **Bước 2: Build lại project**

```powershell
dotnet build
```

### **Bước 3: Chạy ứng dụng**

```powershell
dotnet run
# Hoặc
.\start.ps1
```

---

## 🧪 Kiểm tra hoạt động

### **Test Case 1: Check-in sớm 07:00 (trong khung 05:00-09:00)**
- Giá phòng: 2.000.000 đ/ngày
- Check-in dự kiến: 14:00
- Check-in thực tế: 07:00 (sớm 7 tiếng)
- **Kỳ vọng**: Phụ thu 50% × 2.000.000 = **1.000.000 đ**

### **Test Case 2: Check-out muộn 16:30 (trong khung 15:00-18:00)**
- Giá phòng: 2.000.000 đ/ngày
- Check-out dự kiến: 12:00
- Check-out thực tế: 16:30 (muộn 4.5 tiếng)
- **Kỳ vọng**: Phụ thu 50% × 2.000.000 = **1.000.000 đ**

### **Test Case 3: Check-out muộn 17:25 (giá theo GIỜ)**
- Giá phòng: 500.000 đ/giờ
- Giá ngày: 2.000.000 đ/ngày
- Check-out dự kiến: 17:05
- Check-out thực tế: 17:25 (muộn 20 phút)
- **Kỳ vọng**: Miễn phí (< 30 phút với giá GIỜ)

### **Test Case 4: Check-out muộn 12:45 (giá theo NGÀY)**
- Giá phòng: 2.000.000 đ/ngày
- Check-out dự kiến: 12:00
- Check-out thực tế: 12:45 (muộn 45 phút)
- **Kỳ vọng**: Miễn phí (< 1 tiếng với giá NGÀY)

---

## 📊 Công thức tính tổng tiền

```
Tổng tiền = Tiền phòng chuẩn + Phí check-in sớm + Phí check-out muộn + Tiền dịch vụ + VAT 10%
```

**Trong đó:**
- **Tiền phòng chuẩn** = Giá × số đơn vị (ngày/giờ) từ check-in dự kiến → check-out dự kiến
- **Phí check-in sớm** = Giá phòng/ngày × Tỷ lệ phụ thu (0%, 30%, 50%)
- **Phí check-out muộn** = Giá phòng/ngày × Tỷ lệ phụ thu (0%, 30%, 50%, 100%)

---

## 📝 Lưu ý quan trọng

1. **Miễn phí được tính theo đơn vị giá:**
   - Giá theo GIỜ: Miễn phí ≤ 30 phút
   - Giá theo NGÀY: Miễn phí ≤ 1 tiếng

2. **Phí phụ thu luôn tính theo giá NGÀY:**
   - Nếu không có giá NGÀY, sẽ dùng giá GIỜ × 24

3. **Logic áp dụng cho:**
   - ✅ CheckOutController.cs (View/Preview)
   - ✅ Trigger TR_Invoice_ManageInsert (Khi tạo hóa đơn)
   - ✅ Trigger TR_Invoice_ManageUpdate (Khi cập nhật hóa đơn)
   - ✅ Stored Procedure sp_CheckoutRoom (Khi check-out)

4. **Nếu chưa có check-out thực tế:**
   - Hệ thống sẽ tính phí DỰ KIẾN dựa trên thời gian hiện tại
   - Sau khi check-out, trigger sẽ tính lại chính xác

---

## 🆘 Troubleshooting

### **Lỗi: Trigger không chạy**
```sql
-- Kiểm tra trigger có tồn tại không
SELECT name, is_disabled 
FROM sys.triggers 
WHERE name LIKE 'TR_Invoice%';

-- Enable trigger nếu bị disable
ENABLE TRIGGER TR_Invoice_ManageInsert ON Invoice;
ENABLE TRIGGER TR_Invoice_ManageUpdate ON Invoice;
```

### **Lỗi: Giá phòng không đúng**
```sql
-- Kiểm tra giá phòng trong Pricing
SELECT * FROM Pricing WHERE priceUnit IN ('DAY', 'HOUR');

-- Kiểm tra giá đã lưu trong ReservationForm
SELECT reservationFormID, priceUnit, unitPrice FROM ReservationForm;
```

### **Lỗi: ViewBag không có dữ liệu**
- Kiểm tra CheckOutController.cs đã cập nhật chưa
- Build lại project: `dotnet build`
- Restart ứng dụng

---

## 📞 Hỗ trợ

Nếu gặp vấn đề, kiểm tra:
1. File `CHECKIN_CHECKOUT_PRICING_LOGIC.md` - Chi tiết logic
2. File `SESSION_BUGFIX_SUMMARY.md` - Tổng kết các thay đổi
3. Database logs - Kiểm tra trigger có chạy không

---

**Cập nhật lần cuối:** 15/10/2025  
**Người thực hiện:** GitHub Copilot  
**Trạng thái:** ✅ Ready for Testing
