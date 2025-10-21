# Tính năng Tìm kiếm và Lọc nhanh

## Tóm tắt
Tài liệu này mô tả các tính năng tìm kiếm và lọc nhanh đã được thêm vào hệ thống quản lý khách sạn.

## 1. Tìm kiếm và Lọc Hóa đơn (Invoice Search & Filter)

### Vị trí
- **Trang**: `/Invoice/Index`
- **File**: `Views\Invoice\Index.cshtml`
- **Controller**: `Controllers\InvoiceController.cs`

### Tính năng

#### 1.1. Tìm kiếm theo từ khóa
Thanh tìm kiếm cho phép tìm kiếm hóa đơn theo:
- **Mã Hóa đơn** (Invoice ID)
- **Mã Đặt phòng** (Reservation Form ID)
- **Mã Phòng** (Room ID)
- **Tên Khách hàng** (Customer Name)

#### 1.2. Lọc theo trạng thái thanh toán
Dropdown cho phép lọc hóa đơn theo trạng thái:
- **Tất cả** - Hiển thị tất cả hóa đơn
- **Đã thanh toán** - Chỉ hiển thị hóa đơn đã thanh toán
- **Chưa thanh toán** - Chỉ hiển thị hóa đơn chưa thanh toán

#### 1.3. Thống kê nâng cao
Hiển thị 4 card thống kê:
1. **Tổng hóa đơn** - Tổng số hóa đơn trong danh sách
2. **Đã thanh toán** - Số lượng và doanh thu đã thanh toán
3. **Chưa thanh toán** - Số lượng và doanh thu chưa thanh toán  
4. **Tổng doanh thu** - Tổng doanh thu của tất cả hóa đơn

### Cách sử dụng
1. Vào trang "Quản lý Hóa đơn" (`/Invoice/Index`)
2. Nhập từ khóa vào ô tìm kiếm (nếu muốn)
3. Chọn trạng thái thanh toán từ dropdown (nếu muốn)
4. Nhấn nút "Lọc"
5. Kết quả sẽ hiển thị các hóa đơn phù hợp

### Cài đặt kỹ thuật
- Tìm kiếm không phân biệt chữ hoa/thường
- Tìm kiếm theo kiểu "contains" (chứa từ khóa)
- Tìm kiếm trên cả 4 trường: InvoiceID, ReservationFormID, RoomID, CustomerName
- Lọc theo trạng thái sử dụng thuộc tính `IsPaid` của Invoice
- Có thể kết hợp cả tìm kiếm và lọc trạng thái cùng lúc

## 2. Lọc nhanh theo Thời gian (Quick Time Filters)

### Vị trí
Tính năng này được thêm vào **TẤT CẢ** các trang báo cáo:
1. **Báo cáo Doanh thu** (`/Report/Revenue`)
2. **Báo cáo Công suất Phòng** (`/Report/RoomOccupancy`)
3. **Báo cáo Hiệu suất Nhân viên** (`/Report/EmployeePerformance`)

### Các tùy chọn lọc nhanh

| Nút lọc | Mô tả | Khoảng thời gian |
|---------|-------|------------------|
| **Hôm nay** | Dữ liệu của ngày hôm nay | Từ 00:00 hôm nay → 23:59 hôm nay |
| **Hôm qua** | Dữ liệu của ngày hôm qua | Từ 00:00 hôm qua → 23:59 hôm qua |
| **7 ngày** | Dữ liệu 7 ngày gần nhất | Từ 6 ngày trước → hôm nay |
| **30 ngày** | Dữ liệu 30 ngày gần nhất | Từ 29 ngày trước → hôm nay |
| **30 ngày trước** | Dữ liệu từ 60-30 ngày trước | Từ 60 ngày trước → 30 ngày trước |
| **3 tháng trước** | Dữ liệu 3 tháng gần nhất | Từ 3 tháng trước → hôm nay |

### Cách sử dụng

#### Phương pháp 1: Sử dụng nút lọc nhanh
1. Vào một trong các trang báo cáo
2. Nhấn vào một trong các nút lọc nhanh (Hôm nay, Hôm qua, 7 ngày,...)
3. Trang sẽ tự động tải lại với dữ liệu của khoảng thời gian được chọn

#### Phương pháp 2: Tùy chỉnh khoảng thời gian
1. Vào một trong các trang báo cáo
2. Chọn "Từ ngày" và "Đến ngày" theo ý muốn
3. Nhấn nút "Lọc dữ liệu"
4. Trang sẽ hiển thị dữ liệu trong khoảng thời gian tùy chỉnh

### Giao diện

#### Thanh tìm kiếm và lọc Hóa đơn
```
Tìm kiếm:                                     Trạng thái thanh toán:
┌────────────────────────────────────────┐   ┌──────────────────┐
│ 🔍 Tìm kiếm theo Mã hóa đơn, Mã phòng, │   │ [Tất cả ▼]       │  [Lọc]
│    Tên khách hàng...                   │   │  Đã thanh toán   │
└────────────────────────────────────────┘   │  Chưa thanh toán │
                                             └──────────────────┘

Thống kê:
┌────────────┬────────────────┬──────────────────┬─────────────────┐
│ Tổng HĐ: 50│ Đã TT: 35      │ Chưa TT: 15      │ Tổng DT:        │
│            │ 350,000,000 đ  │ 150,000,000 đ    │ 500,000,000 đ   │
└────────────┴────────────────┴──────────────────┴─────────────────┘
```

#### Bộ lọc nhanh Báo cáo
```
Lọc nhanh:
┌─────────┬──────────┬─────────┬──────────┬────────────────┬───────────────┐
│ Hôm nay │ Hôm qua  │ 7 ngày  │ 30 ngày  │ 30 ngày trước  │ 3 tháng trước │
└─────────┴──────────┴─────────┴──────────┴────────────────┴───────────────┘

Từ ngày: [____/____/____]  Đến ngày: [____/____/____]  [Lọc dữ liệu]
```

## 3. Các file đã chỉnh sửa

### Controllers
1. `Controllers\InvoiceController.cs`
   - Thêm parameter `searchTerm` vào method `Index()`
   - Thêm logic lọc dữ liệu theo từ khóa tìm kiếm

2. `Controllers\ReportController.cs`
   - Thêm parameter `preset` vào 3 methods:
     - `Revenue()`
     - `RoomOccupancy()`
     - `EmployeePerformance()`
   - Thêm switch-case để xử lý các preset filter

### Views
1. `Views\Invoice\Index.cshtml`
   - Thêm thanh tìm kiếm với input text và nút tìm kiếm
   - Hiển thị từ khóa đã tìm trong ô input

2. `Views\Report\Revenue.cshtml`
   - Thêm 6 nút lọc nhanh với icon FontAwesome
   - Giữ nguyên form tùy chỉnh khoảng thời gian

3. `Views\Report\RoomOccupancy.cshtml`
   - Thêm 6 nút lọc nhanh với icon FontAwesome
   - Giữ nguyên form tùy chỉnh khoảng thời gian

4. `Views\Report\EmployeePerformance.cshtml`
   - Thêm 6 nút lọc nhanh với icon FontAwesome
   - Giữ nguyên form tùy chỉnh khoảng thời gian

## 4. Kiểm tra và Test

### Test tìm kiếm và lọc Hóa đơn
1. **Test tìm kiếm**:
   - Thử tìm kiếm với mã hóa đơn đầy đủ
   - Thử tìm kiếm với một phần mã hóa đơn
   - Thử tìm kiếm với tên khách hàng
   - Thử tìm kiếm với mã phòng
   - Thử tìm kiếm với từ khóa không tồn tại

2. **Test lọc trạng thái thanh toán**:
   - Chọn "Tất cả" - Kiểm tra hiển thị tất cả hóa đơn
   - Chọn "Đã thanh toán" - Chỉ hiển thị hóa đơn có IsPaid = true
   - Chọn "Chưa thanh toán" - Chỉ hiển thị hóa đơn có IsPaid = false

3. **Test kết hợp**:
   - Tìm kiếm + lọc "Đã thanh toán"
   - Tìm kiếm + lọc "Chưa thanh toán"
   - Kiểm tra thống kê cập nhật đúng với bộ lọc

### Test lọc nhanh Báo cáo
1. Nhấn từng nút lọc nhanh và kiểm tra khoảng thời gian
2. Kiểm tra dữ liệu hiển thị có đúng với khoảng thời gian không
3. Thử kết hợp lọc nhanh và tùy chỉnh khoảng thời gian
4. Kiểm tra trên cả 3 trang báo cáo

## 5. Lưu ý

- Tất cả các tính năng đều không phân biệt chữ hoa/thường
- Các nút lọc nhanh sử dụng query string parameter `?preset=...`
- Tìm kiếm sử dụng query string parameter `?searchTerm=...`
- Không cần chỉnh sửa database hay stored procedures
- Tương thích với tất cả trình duyệt hiện đại

## 6. Hướng dẫn mở rộng

### Thêm preset filter mới
Để thêm preset filter mới (ví dụ: "Tuần này"), thực hiện:

1. Thêm nút trong View:
```html
<a href="?preset=thisweek" class="btn btn-outline-primary btn-sm">
    <i class="fas fa-calendar-week"></i> Tuần này
</a>
```

2. Thêm case trong Controller:
```csharp
case "thisweek":
    var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
    fromDate = startOfWeek;
    toDate = today;
    break;
```

### Thêm trường tìm kiếm mới cho Hóa đơn
Để thêm trường tìm kiếm mới (ví dụ: số điện thoại), chỉnh sửa trong `InvoiceController.cs`:

```csharp
invoices = invoices.Where(i =>
    i.InvoiceID.ToLower().Contains(searchTerm) ||
    (i.ReservationForm?.RoomID?.ToLower().Contains(searchTerm) ?? false) ||
    (i.ReservationForm?.Customer?.FullName?.ToLower().Contains(searchTerm) ?? false) ||
    (i.ReservationForm?.Customer?.PhoneNumber?.ToLower().Contains(searchTerm) ?? false) || // MỚI
    (i.ReservationFormID?.ToLower().Contains(searchTerm) ?? false)
).ToList();
```

## 7. Hoàn thành

✅ Tất cả tính năng đã được triển khai thành công
✅ Không có lỗi linter
✅ Code clean và dễ bảo trì
✅ Tương thích với cấu trúc hiện tại của ứng dụng

