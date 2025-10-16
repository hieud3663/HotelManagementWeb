# 🔧 Hướng dẫn cập nhật tính giá đặt phòng

## ⚠️ Vấn đề đã phát hiện

Hệ thống hiện tại **KHÔNG LƯU** thông tin hình thức thuê (theo ngày/giờ) và đơn giá tại thời điểm đặt phòng, dẫn đến các vấn đề:

1. ❌ Không biết khách chọn thuê theo ngày hay theo giờ
2. ❌ Khi giá thay đổi, tính tiền checkout sẽ SAI
3. ❌ Hóa đơn chưa trừ tiền đặt cọc

## ✅ Giải pháp đã thực hiện

### 1. **Database** ✔️
- Đã thêm 2 cột vào bảng `ReservationForm`:
  - `priceUnit` (NVARCHAR(15)): Lưu hình thức thuê ('DAY' hoặc 'HOUR')
  - `unitPrice` (MONEY): Lưu đơn giá tại thời điểm đặt

### 2. **Model** ✔️
- Đã thêm 2 property vào `ReservationForm.cs`:
  ```csharp
  public string PriceUnit { get; set; } = "DAY";
  public decimal UnitPrice { get; set; }
  ```

### 3. **Stored Procedure** ✔️
- **sp_CreateReservation**: Thêm 2 tham số `@priceUnit` và `@unitPrice`
- **sp_CheckoutRoom**: 
  - Sử dụng `priceUnit` và `unitPrice` đã lưu thay vì lấy giá mới từ Pricing
  - Trừ tiền đặt cọc khỏi tổng tiền phải thanh toán

### 4. **DatabaseExtensions** ✔️
- Cập nhật method `CreateReservationSP` để nhận 2 tham số mới

### 5. **Controller** ✔️
- Cập nhật `ReservationController.Create`:
  - Thêm `PriceUnit` và `UnitPrice` vào `[Bind]`
  - Truyền 2 giá trị này vào stored procedure

### 6. **View** ✔️
- Thêm 2 hidden input:
  ```html
  <input type="hidden" asp-for="PriceUnit" id="priceUnitHidden" />
  <input type="hidden" asp-for="UnitPrice" id="unitPriceHidden" />
  ```
- Cập nhật JavaScript `calculateDeposit()` để populate các hidden field

## 🚀 Các bước triển khai

### Bước 1: Cập nhật Database
```sql
-- Chạy script SQL mới (đã có sẵn trong HotelManagement_new.sql)
-- Hoặc chỉ chạy lệnh ALTER TABLE nếu database đã tồn tại:

ALTER TABLE ReservationForm
ADD priceUnit NVARCHAR(15) NOT NULL DEFAULT 'DAY' CHECK (priceUnit IN ('DAY', 'HOUR')),
    unitPrice MONEY NOT NULL DEFAULT 0;
```

### Bước 2: Build lại project
```powershell
dotnet build
```

### Bước 3: Chạy migrations (nếu sử dụng EF Migrations)
```powershell
dotnet ef database update
```

### Bước 4: Test chức năng
1. ✅ Tạo phiếu đặt phòng mới
   - Chọn phòng
   - Chọn hình thức thuê (theo ngày hoặc theo giờ)
   - Kiểm tra tiền cọc tự động tính

2. ✅ Checkout
   - Kiểm tra tiền phòng tính đúng theo đơn giá đã lưu
   - Kiểm tra hóa đơn đã trừ tiền đặt cọc

3. ✅ Thay đổi giá trong bảng Pricing
   - Tạo đặt phòng mới với giá mới
   - Checkout đặt phòng cũ → vẫn tính theo giá cũ ✔️

## 📋 Checklist kiểm tra

- [ ] Database có 2 cột `priceUnit` và `unitPrice` trong bảng `ReservationForm`
- [ ] Tạo phiếu đặt phòng mới → kiểm tra DB có lưu `priceUnit` và `unitPrice`
- [ ] Checkout → hóa đơn tính đúng giá đã lưu (không lấy giá mới từ Pricing)
- [ ] Hóa đơn đã trừ tiền đặt cọc
- [ ] Thay đổi giá → đặt phòng cũ vẫn giữ giá cũ

## 🐛 Troubleshooting

### Lỗi: "Invalid column name 'priceUnit'"
**Giải pháp**: Chạy lại script SQL để thêm 2 cột mới vào database.

### Lỗi: "No overload for method 'CreateReservationSP'"
**Giải pháp**: Build lại project để cập nhật `DatabaseExtensions.cs`.

### Lỗi: Hidden fields không có giá trị
**Giải pháp**: 
1. Mở Developer Tools (F12)
2. Kiểm tra Console có lỗi JavaScript không
3. Kiểm tra function `calculateDeposit()` đã chạy chưa

## 📊 Lợi ích sau khi sửa

1. ✅ **Chính xác**: Giá luôn đúng với thời điểm đặt phòng
2. ✅ **Minh bạch**: Biết chính xác khách thuê theo ngày hay giờ
3. ✅ **Hợp lý**: Hóa đơn trừ tiền đặt cọc, khách chỉ trả phần còn thiếu
4. ✅ **Linh hoạt**: Có thể thay đổi giá mà không ảnh hưởng đặt phòng cũ

## 📝 Ghi chú quan trọng

- **Dữ liệu cũ**: Các phiếu đặt phòng cũ sẽ có `priceUnit = 'DAY'` và `unitPrice = 0` (giá trị mặc định). Cần cập nhật thủ công nếu cần.
- **Migration**: Nếu đã có dữ liệu, cân nhắc viết script để cập nhật `unitPrice` từ bảng `Pricing`.

---

✨ **Đã hoàn thành tất cả các thay đổi!** Bây giờ hệ thống sẽ lưu và sử dụng đúng giá tại thời điểm đặt phòng.
