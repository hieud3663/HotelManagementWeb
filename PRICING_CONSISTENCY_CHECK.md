# KIỂM TRA TÍNH NHẤT QUÁN LOGIC TÍNH PHÍ

## Mục đích
Đảm bảo logic tính phí check-in sớm và check-out muộn giống hệt nhau giữa:
- **CheckOutController.cs** (Details action)
- **TR_Invoice_ManageInsert** trigger
- **TR_Invoice_ManageUpdate** trigger
- **sp_CheckoutRoom** stored procedure

---

## 1. CẤU TRÚC LOGIC

### Bước 1: Tính tiền phòng CHUẨN
- **DAY**: `CEILING(phút / 1440) × unitPrice`
- **HOUR**: `CEILING(phút / 60) × unitPrice`

### Bước 2: Tính PHÍ CHECK-IN SỚM

#### Giá GIỜ (HOUR):
```
chargeableMinutes = earlyMinutes - 30 (miễn phí 30 phút)
totalHours = CEILING(chargeableMinutes / 60)

Phí = (MIN(totalHours, 2) × hourPrice × 100%) +
      (MIN(MAX(totalHours - 2, 0), 4) × hourPrice × 80%) +
      (MAX(totalHours - 6, 0) × hourPrice × 80%)
```

**Ví dụ:**
- 1 giờ sớm: `1 × 200k = 200k`
- 3 giờ sớm: `2 × 200k + 1 × 160k = 560k`
- 7 giờ sớm: `2 × 200k + 4 × 160k + 1 × 160k = 1,200k`

#### Giá NGÀY (DAY):
```
Lặp qua từng khung giờ từ actualCheckIn → expectedCheckIn:
- 5-9h: 50% × (phút_trong_khung / 1440) × dayPrice
- 9-14h: 30% × (phút_trong_khung / 1440) × dayPrice
- ≥14h: 0%

Tổng phí = TỔNG tất cả các khung giờ
```

**Ví dụ:**
- Check-in 8:00, expected 10:00 (2 giờ sớm):
  * 8-9h (1 giờ = 60 phút): `60/1440 × 500k × 50% = 10,416đ`
  * 9-10h (1 giờ = 60 phút): `60/1440 × 500k × 30% = 6,250đ`
  * **Tổng: 16,666đ**

### Bước 3: Tính PHÍ CHECK-OUT MUỘN

#### Giá GIỜ (HOUR):
```
chargeableMinutes = lateMinutes - 30 (miễn phí 30 phút)
totalHours = CEILING(chargeableMinutes / 60)

Phí = (MIN(totalHours, 2) × hourPrice × 100%) +
      (MIN(MAX(totalHours - 2, 0), 4) × hourPrice × 80%) +
      (MAX(totalHours - 6, 0) × hourPrice × 80%)
```

**Ví dụ:**
- 2 giờ muộn: `2 × 200k = 400k`
- 5 giờ muộn: `2 × 200k + 3 × 160k = 880k`

#### Giá NGÀY (DAY):
```
Lặp qua từng khung giờ từ expectedCheckOut + freeMinutes → actualCheckOut:
- 12-15h: 30% × (phút_trong_khung / 1440) × dayPrice
- 15-18h: 50% × (phút_trong_khung / 1440) × dayPrice
- ≥18h: 100% × (phút_trong_khung / 1440) × dayPrice

Tổng phí = TỔNG tất cả các khung giờ
```

**Ví dụ:**
- Check-out 16:00, expected 12:00 (4 giờ muộn, miễn phí 1h):
  * Thực tế tính từ 13:00 → 16:00 (3 giờ)
  * 13-15h (2 giờ = 120 phút): `120/1440 × 500k × 30% = 12,500đ`
  * 15-16h (1 giờ = 60 phút): `60/1440 × 500k × 50% = 10,416đ`
  * **Tổng: 22,916đ**

---

## 2. ĐIỂM QUAN TRỌNG CẦN KIỂM TRA

### ✅ Controller (CheckOutController.cs)
- [x] Có helper method `CalculateHourlyFee()` cho logic bậc thang
- [x] Phí HOUR: Dùng bậc thang 100%/80%/80%
- [x] Phí DAY: Dùng vòng lặp while để tích lũy theo khung giờ
- [x] Miễn phí 30 phút (HOUR), 60 phút (DAY)

### ✅ Trigger INSERT (TR_Invoice_ManageInsert)
- [x] Logic giống hệt Controller
- [x] Khai báo đủ biến: `@hourPrice`, `@chargeableEarlyMinutes`, `@chargeableLateMinutes`
- [x] Vòng lặp WHILE cho DAY pricing
- [x] Tính bậc thang cho HOUR pricing

### ✅ Trigger UPDATE (TR_Invoice_ManageUpdate)
- [x] Logic giống hệt Trigger INSERT
- [x] Khác duy nhất: Dùng UPDATE thay vì INSERT ở cuối

### 🔲 Stored Procedure (sp_CheckoutRoom)
- [ ] Cần cập nhật với logic giống hệt trên
- [ ] Hiện tại vẫn dùng logic cũ (chỉ áp dụng 1 mức phí)

---

## 3. KỊCH BẢN TEST

### Test Case 1: Giá GIỜ - Check-out muộn 3 giờ
```
Dữ liệu:
- priceUnit = "HOUR"
- hourPrice = 200,000đ
- expectedCheckOut = "2024-01-15 12:00"
- actualCheckOut = "2024-01-15 15:00"

Kết quả mong đợi:
- lateMinutes = 180 phút
- chargeableMinutes = 180 - 30 = 150 phút
- totalHours = CEILING(150 / 60) = 3 giờ
- tier1 = 2 × 200k = 400k
- tier2 = 1 × 160k = 160k
- **Tổng: 560,000đ**
```

### Test Case 2: Giá NGÀY - Check-out muộn 4 giờ
```
Dữ liệu:
- priceUnit = "DAY"
- dayPrice = 500,000đ
- expectedCheckOut = "2024-01-15 12:00"
- actualCheckOut = "2024-01-15 16:00"

Kết quả mong đợi:
- lateMinutes = 240 phút
- Miễn phí 60 phút → tính từ 13:00 → 16:00
- Khung 13:00-15:00 (120 phút): 120/1440 × 500k × 30% = 12,500đ
- Khung 15:00-16:00 (60 phút): 60/1440 × 500k × 50% = 10,416đ
- **Tổng: 22,916đ (làm tròn 23,000đ)**
```

### Test Case 3: Giá GIỜ - Check-in sớm 5 giờ
```
Dữ liệu:
- priceUnit = "HOUR"
- hourPrice = 200,000đ
- expectedCheckIn = "2024-01-15 14:00"
- actualCheckIn = "2024-01-15 09:00"

Kết quả mong đợi:
- earlyMinutes = 300 phút
- chargeableMinutes = 300 - 30 = 270 phút
- totalHours = CEILING(270 / 60) = 5 giờ
- tier1 = 2 × 200k = 400k
- tier2 = 3 × 160k = 480k
- **Tổng: 880,000đ**
```

### Test Case 4: Giá NGÀY - Check-in sớm 3 giờ
```
Dữ liệu:
- priceUnit = "DAY"
- dayPrice = 500,000đ
- expectedCheckIn = "2024-01-15 12:00"
- actualCheckIn = "2024-01-15 09:00"

Kết quả mong đợi:
- earlyMinutes = 180 phút
- Miễn phí 60 phút → tính từ 09:00 → 11:00 (2 giờ)
- Khung 09:00-11:00 (120 phút): 120/1440 × 500k × 30% = 12,500đ
- **Tổng: 12,500đ (làm tròn 13,000đ)**
```

---

## 4. CÁCH KIỂM TRA

### Bước 1: Tạo reservation test
```sql
INSERT INTO ReservationForm (...) VALUES (...);
```

### Bước 2: Check-in/Check-out với thời gian test
```sql
EXEC sp_CheckIn @reservationFormID = 'RF001', @checkInDate = '2024-01-15 09:00';
EXEC sp_CheckoutRoom @reservationFormID = 'RF001', @checkOutDate = '2024-01-15 16:00';
```

### Bước 3: Kiểm tra kết quả trong database
```sql
SELECT earlyCheckinFee, lateCheckoutFee, totalAmount
FROM Invoice
WHERE reservationFormID = 'RF001';
```

### Bước 4: Kiểm tra kết quả trong Controller
- Truy cập trang `/CheckOut/Details?id=RF001`
- Xem giá trị trong ViewBag:
  * `ViewBag.EarlyCheckinFee`
  * `ViewBag.LateCheckoutFee`

### Bước 5: So sánh
- Controller phải trả về ĐÚNG HỆT số tiền như trong Database

---

## 5. TRẠNG THÁI HIỆN TẠI

✅ **CheckOutController.cs**: Đã cập nhật logic hoàn chỉnh
✅ **TR_Invoice_ManageInsert**: Đã cập nhật (file UPDATE_PRICING_LOGIC_COMPLETE.sql)
✅ **TR_Invoice_ManageUpdate**: Đã cập nhật (file UPDATE_PRICING_LOGIC_COMPLETE.sql)
⏳ **sp_CheckoutRoom**: CHƯA cập nhật (vẫn dùng logic cũ)

---

## 6. CHECKLIST CUỐI CÙNG

### Trước khi deploy:
- [ ] Chạy script `UPDATE_PRICING_LOGIC_COMPLETE.sql` trong SQL Server
- [ ] Build lại project để compile controller mới
- [ ] Tạo test case trong database
- [ ] Kiểm tra kết quả Controller vs Database
- [ ] Cập nhật `sp_CheckoutRoom` với logic giống hệt
- [ ] Cập nhật JavaScript trong `Details.cshtml` để tính real-time
- [ ] Test tất cả 4 kịch bản ở trên

### Sau khi deploy:
- [ ] Kiểm tra với dữ liệu thật
- [ ] So sánh số liệu với yêu cầu nghiệp vụ
- [ ] Cập nhật tài liệu hướng dẫn sử dụng
