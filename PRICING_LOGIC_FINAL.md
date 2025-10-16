# HOÀN THIỆN LOGIC TÍNH PHÍ CHECK-IN SỚM & CHECK-OUT MUỘN

## 📋 TỔNG QUAN

Đã cập nhật **hoàn chỉnh** logic tính phí check-in sớm và check-out muộn theo yêu cầu thực tế của khách sạn:

### ✅ Cập nhật thành công:
1. **CheckOutController.cs** - Details action
2. **TR_Invoice_ManageInsert** - Trigger INSERT
3. **TR_Invoice_ManageUpdate** - Trigger UPDATE  
4. **sp_CheckoutRoom** - Stored Procedure

---

## 🎯 LOGIC MỚI

### 1. Giá GIỜ (HOUR) - Tính theo **BẬC THANG**

```
Công thức:
- 2 giờ đầu tiên: 100% × hourPrice
- Từ giờ 3-6: 80% × hourPrice
- Từ giờ thứ 7 trở đi: 80% × hourPrice
```

**Ví dụ thực tế:**
- **Check-out muộn 3 giờ** (hourPrice = 200k):
  * 2 giờ đầu: 2 × 200k = 400k
  * 1 giờ tiếp: 1 × 160k = 160k
  * **Tổng: 560,000đ**

- **Check-in sớm 5 giờ** (hourPrice = 200k):
  * 2 giờ đầu: 2 × 200k = 400k
  * 3 giờ tiếp: 3 × 160k = 480k
  * **Tổng: 880,000đ**

### 2. Giá NGÀY (DAY) - Tính theo **KHUNG GIỜ TÍCH LŨY**

#### Check-in sớm:
```
Khung giờ:
- 5-9h:  50% × (phút_trong_khung / 1440) × dayPrice
- 9-14h: 30% × (phút_trong_khung / 1440) × dayPrice
- ≥14h:  0% (miễn phí)
```

**Ví dụ:** Check-in 8:00, expected 10:00 (dayPrice = 500k)
  * 8-9h (60 phút): 60/1440 × 500k × 50% = 10,416đ
  * 9-10h (60 phút): 60/1440 × 500k × 30% = 6,250đ
  * **Tổng: 16,666đ → làm tròn 17,000đ**

#### Check-out muộn:
```
Khung giờ:
- 12-15h: 30% × (phút_trong_khung / 1440) × dayPrice
- 15-18h: 50% × (phút_trong_khung / 1440) × dayPrice
- ≥18h:  100% × (phút_trong_khung / 1440) × dayPrice
```

**Ví dụ:** Check-out 16:00, expected 12:00 (dayPrice = 500k, miễn phí 1h)
  * Tính từ 13:00 → 16:00 (3 giờ)
  * 13-15h (120 phút): 120/1440 × 500k × 30% = 12,500đ
  * 15-16h (60 phút): 60/1440 × 500k × 50% = 10,416đ
  * **Tổng: 22,916đ → làm tròn 23,000đ**

### 3. Miễn phí grace period
- **HOUR**: 30 phút
- **DAY**: 60 phút

---

## 📝 DANH SÁCH FILE ĐÃ CẬP NHẬT

### 1. Controller
**File:** `Controllers/CheckOutController.cs`

**Thay đổi:**
- Thêm helper method `CalculateHourlyFee()`
- Cập nhật logic tính `earlyCheckinFee`:
  * HOUR: Dùng bậc thang
  * DAY: Vòng lặp while tích lũy theo khung giờ
- Cập nhật logic tính `lateCheckoutFee`:
  * HOUR: Dùng bậc thang
  * DAY: Vòng lặp while tích lũy theo khung giờ

### 2. Database Script
**File:** `docs/database/UPDATE_PRICING_LOGIC_COMPLETE.sql`

**Nội dung:**
- `TR_Invoice_ManageInsert` - Logic hoàn chỉnh cho INSERT
- `TR_Invoice_ManageUpdate` - Logic hoàn chỉnh cho UPDATE
- `sp_CheckoutRoom` - Logic hoàn chỉnh cho stored procedure

**Cách chạy:**
```sql
-- Trong SQL Server Management Studio:
1. Mở file UPDATE_PRICING_LOGIC_COMPLETE.sql
2. Chọn database HotelManagement
3. Nhấn F5 để chạy
```

### 3. Tài liệu kiểm tra
**File:** `PRICING_CONSISTENCY_CHECK.md`

**Nội dung:**
- Giải thích chi tiết logic
- Ví dụ tính toán
- Test cases để kiểm tra
- Checklist cuối cùng

---

## 🔧 HƯỚNG DẪN TRIỂN KHAI

### Bước 1: Cập nhật Database
```bash
# Chạy script SQL trong SSMS
1. Mở SQL Server Management Studio
2. Connect vào server
3. Chọn database HotelManagement
4. Mở file: docs/database/UPDATE_PRICING_LOGIC_COMPLETE.sql
5. Nhấn F5 để execute
```

### Bước 2: Build lại Project
```bash
# Trong PowerShell tại thư mục project
dotnet build
```

### Bước 3: Kiểm tra
```bash
# Chạy ứng dụng
dotnet run

# Hoặc nhấn F5 trong Visual Studio
```

---

## ✅ KIỂM TRA TÍNH NHẤT QUÁN

### Test Case 1: Giá GIỜ - Check-out muộn 3 giờ
```
Input:
- priceUnit = "HOUR"
- hourPrice = 200,000đ
- expectedCheckOut = "2024-01-15 12:00"
- actualCheckOut = "2024-01-15 15:00"

Expected Output:
- lateMinutes = 180 phút
- chargeableMinutes = 150 phút (miễn phí 30 phút)
- totalHours = 3 giờ
- Phí = 2 × 200k + 1 × 160k = 560,000đ
```

**Cách kiểm tra:**
1. Tạo reservation với giá GIỜ
2. Check-in đúng giờ
3. Check-out muộn 3 giờ
4. Xem hóa đơn trong:
   - `/CheckOut/Details?id=RF001` (Controller)
   - Bảng `Invoice` trong database (Trigger/Proc)
5. **Kết quả phải giống nhau!**

### Test Case 2: Giá NGÀY - Check-out muộn 4 giờ
```
Input:
- priceUnit = "DAY"
- dayPrice = 500,000đ
- expectedCheckOut = "2024-01-15 12:00"
- actualCheckOut = "2024-01-15 16:00"

Expected Output:
- lateMinutes = 240 phút
- Tính từ 13:00 (sau miễn phí 1h)
- Khung 13-15h: 120/1440 × 500k × 30% = 12,500đ
- Khung 15-16h: 60/1440 × 500k × 50% = 10,416đ
- Tổng: 22,916đ → làm tròn 23,000đ
```

**Cách kiểm tra:** Tương tự Test Case 1

---

## 📊 SO SÁNH LOGIC CŨ VS MỚI

| Tiêu chí | Logic CŨ | Logic MỚI |
|----------|----------|-----------|
| **Giá GIỜ** | Chỉ tính 1 mức % dựa trên giờ cuối | Tính bậc thang: 100% → 80% → 80% |
| **Giá NGÀY** | Chỉ tính 1 mức % dựa trên giờ cuối | Tích lũy từng khung giờ |
| **Check-out 16h** (expected 12h, DAY) | Chỉ tính 50% × dayPrice | Tính 30% (12-15h) + 50% (15-16h) |
| **Check-out 5h muộn** (HOUR) | Chỉ tính 1 lần hourPrice | Tính 2h × 100% + 3h × 80% |

---

## ⚠️ LƯU Ý QUAN TRỌNG

1. **Phải chạy script SQL trước** khi test trong Controller
2. **Build lại project** sau khi thay đổi Controller
3. **Kiểm tra cả 4 components:** Controller, INSERT Trigger, UPDATE Trigger, Stored Procedure
4. **Kết quả phải giống hệt nhau** giữa Controller và Database

---

## 🎉 KẾT QUẢ

✅ **CheckOutController.cs**: Đã cập nhật logic hoàn chỉnh  
✅ **TR_Invoice_ManageInsert**: Đã cập nhật logic hoàn chỉnh  
✅ **TR_Invoice_ManageUpdate**: Đã cập nhật logic hoàn chỉnh  
✅ **sp_CheckoutRoom**: Đã cập nhật logic hoàn chỉnh  

**Tất cả 4 components đều dùng CÙNG LOGIC:**
- Giá GIỜ: Bậc thang 100% / 80% / 80%
- Giá NGÀY: Tích lũy theo khung giờ
- Miễn phí grace period: 30 phút (HOUR), 60 phút (DAY)

---

## 📚 TÀI LIỆU THAM KHẢO

1. `PRICING_CONSISTENCY_CHECK.md` - Hướng dẫn kiểm tra chi tiết
2. `UPDATE_PRICING_LOGIC_COMPLETE.sql` - Script SQL cập nhật database
3. `CHECKIN_CHECKOUT_PRICING_LOGIC.md` - Tài liệu quy tắc nghiệp vụ ban đầu

---

**Ngày cập nhật:** 2024-01-15  
**Trạng thái:** ✅ HOÀN THÀNH  
**Người thực hiện:** GitHub Copilot + User
