# Hệ thống Quản lý Khách sạn - ASP.NET MVC

## Mô tả dự án

Hệ thống quản lý khách sạn hoàn chỉnh được xây dựng bằng ASP.NET MVC 9.0, Entity Framework Core và SQL Server. Hệ thống cung cấp đầy đủ các chức năng quản lý khách sạn theo sơ đồ BFD và DFD đã được thiết kế.

## Tính năng chính

### 1. Quản lý Danh mục nền
- **Quản lý Nhân viên**: Thêm, sửa, xóa, tìm kiếm nhân viên
- **Quản lý Khách hàng**: Quản lý thông tin khách hàng
- **Quản lý Phòng**: 
  - Quản lý Loại phòng
  - Quản lý thông tin Phòng
  - Quản lý Giá thuê phòng (theo ngày/giờ)
- **Quản lý Dịch vụ**:
  - Quản lý Loại dịch vụ
  - Quản lý Dịch vụ khách sạn

### 2. Quản lý Đặt phòng
- Tiếp nhận đặt phòng
- Điều chỉnh / Hủy đặt phòng
- Tra cứu phòng trống & tính cọc gợi ý

### 3. Quy trình Check-in / Check-out
- Thực hiện Check-in
- Thực hiện Check-out
- Xử lý đổi phòng
- Quản lý trạng thái phòng

### 4. Quản lý Dịch vụ sử dụng
- Thêm dịch vụ cho phòng
- Cập nhật / Xóa dịch vụ
- Tính phí dịch vụ

### 5. Quản lý Hóa đơn & Thanh toán
- Tạo & cập nhật hóa đơn
- Tính phụ phí muộn & thuế VAT 10%
- Thanh toán, đối trừ tiền cọc
- In và gửi hóa đơn

## Cấu trúc dự án

```
HotelManagement/
├── Controllers/          # Các controller xử lý logic
│   ├── AuthController.cs
│   ├── DashboardController.cs
│   ├── EmployeeController.cs
│   ├── CustomerController.cs
│   ├── RoomController.cs
│   ├── ReservationController.cs
│   ├── CheckInController.cs
│   ├── CheckOutController.cs
│   ├── RoomServiceController.cs
│   └── InvoiceController.cs
├── Models/               # Các entity models
│   ├── User.cs
│   ├── Employee.cs
│   ├── Customer.cs
│   ├── Room.cs
│   ├── RoomCategory.cs
│   ├── Pricing.cs
│   ├── HotelService.cs
│   ├── ServiceCategory.cs
│   ├── ReservationForm.cs
│   ├── HistoryCheckin.cs
│   ├── HistoryCheckOut.cs
│   ├── RoomChangeHistory.cs
│   ├── RoomUsageService.cs
│   └── Invoice.cs
├── Data/                 # DbContext
│   └── HotelManagementContext.cs
├── Views/                # Razor views
│   ├── Auth/
│   ├── Dashboard/
│   ├── Employee/
│   ├── Customer/
│   ├── Room/
│   ├── Reservation/
│   ├── CheckIn/
│   ├── CheckOut/
│   ├── RoomService/
│   ├── Invoice/
│   └── Shared/
└── wwwroot/              # Static files
```

## Yêu cầu hệ thống

- .NET 9.0 SDK
- SQL Server 2019 trở lên
- Visual Studio 2022 hoặc VS Code

## Hướng dẫn cài đặt

### 1. Clone dự án hoặc mở folder hiện tại

### 2. Cài đặt các package NuGet

Mở terminal tại thư mục dự án và chạy:

```powershell
dotnet restore
```

### 3. Cấu hình Connection String

Mở file `appsettings.json` và cập nhật connection string cho SQL Server của bạn:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HotelManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Lưu ý**: Thay `localhost` bằng tên server SQL Server của bạn.

### 4. Tạo Database

Chạy file SQL đã có sẵn `docs/database/HotelManagement_new.sql` trong SQL Server Management Studio để:
- Tạo database HotelManagement
- Tạo các bảng và quan hệ
- Thêm dữ liệu mẫu
- Tạo triggers, procedures, functions

```sql
-- Mở file docs/database/HotelManagement_new.sql trong SSMS và chạy
```

### 5. Tạo tài khoản Admin

Sau khi chạy xong file SQL, cần tạo tài khoản admin để đăng nhập:

```sql
USE HotelManagement;
GO

-- Hash password bằng BCrypt (bạn có thể dùng tool online hoặc code C#)
-- Password mẫu: "admin123"
-- Hash BCrypt: $2a$11$... (tạo bằng code C# hoặc tool online)

INSERT INTO [User] (userID, username, passwordHash, role, isActivate)
VALUES ('USER-000001', 'admin', '$2a$11$your-bcrypt-hash-here', 'ADMIN', 'ACTIVATE');
```

**Hoặc tạo bằng code C#:**

Tạo file tạm để hash password:

```csharp
using BCrypt.Net;

var password = "admin123";
var hash = BCrypt.Net.BCrypt.HashPassword(password);
Console.WriteLine(hash);
```

### 6. Chạy ứng dụng

```powershell
dotnet run
```

Ứng dụng sẽ chạy tại: `https://localhost:5001` hoặc `http://localhost:5000`

### 7. Đăng nhập

- **Username**: admin
- **Password**: admin123 (hoặc password bạn đã đặt)

## Quy trình sử dụng

### Quy trình đặt phòng và thanh toán

1. **Thêm Khách hàng** (nếu chưa có)
   - Vào menu Danh mục → Khách hàng → Thêm mới

2. **Đặt phòng**
   - Vào menu Đặt phòng → Thêm mới
   - Chọn khách hàng, thời gian check-in/out
   - Hệ thống tự động kiểm tra phòng trống
   - Nhập tiền cọc (có thể dùng chức năng tính tự động)

3. **Check-in**
   - Vào menu Check-in
   - Chọn phiếu đặt phòng cần check-in
   - Hệ thống tự động:
     - Tạo lịch sử check-in
     - Cập nhật trạng thái phòng thành "Đang sử dụng"
     - Ghi nhận lịch sử sử dụng phòng

4. **Thêm dịch vụ** (trong thời gian lưu trú)
   - Vào menu Đặt phòng → Chi tiết phiếu đặt
   - Thêm các dịch vụ khách sử dụng
   - Hệ thống tự động tính thành tiền

5. **Check-out & Thanh toán**
   - Vào menu Check-out
   - Chọn phiếu đặt phòng cần check-out
   - Hệ thống tự động:
     - Tính tiền phòng (theo ngày/giờ thực tế)
     - Tính phụ phí trả phòng muộn (nếu có)
     - Tính tổng tiền dịch vụ
     - Áp dụng thuế VAT 10%
     - Trừ tiền đặt cọc
     - Tạo hóa đơn
     - Cập nhật trạng thái phòng về "Trống"

6. **Xem và in hóa đơn**
   - Vào menu Hóa đơn
   - Chọn hóa đơn cần xem/in

## Các tính năng đặc biệt

### 1. Kiểm tra phòng trống thông minh
- Hệ thống tự động kiểm tra phòng có trống trong khoảng thời gian đặt
- Không cho phép đặt trùng lịch

### 2. Tính toán tiền phòng linh hoạt
- Hỗ trợ tính theo ngày hoặc giờ
- Tự động tính phụ phí trả phòng muộn:
  - Muộn ≤ 2 giờ: Tính theo giá giờ
  - Muộn 2-6 giờ: Tính 50% giá ngày
  - Muộn > 6 giờ: Tính thêm 1 ngày

### 3. Quản lý trạng thái phòng
- **AVAILABLE**: Phòng trống, sẵn sàng cho thuê
- **RESERVED**: Phòng đã được đặt
- **ON_USE**: Phòng đang có khách sử dụng
- **UNAVAILABLE**: Phòng không khả dụng (bảo trì...)
- **OVERDUE**: Phòng quá hạn trả

### 4. Bảo mật
- Sử dụng BCrypt để hash password
- Session authentication
- AntiForgery token cho forms
- Kiểm tra quyền truy cập tất cả actions

## Cấu trúc Database

Database được thiết kế theo các nguyên tắc:
- Chuẩn hóa 3NF
- Ràng buộc toàn vẹn dữ liệu
- Triggers tự động cập nhật trạng thái
- Stored procedures cho các nghiệp vụ phức tạp
- Computed columns cho các trường tính toán

### Các bảng chính:
1. User - Tài khoản đăng nhập
2. Employee - Nhân viên
3. Customer - Khách hàng
4. RoomCategory - Loại phòng
5. Room - Phòng
6. Pricing - Bảng giá
7. ServiceCategory - Loại dịch vụ
8. HotelService - Dịch vụ
9. ReservationForm - Phiếu đặt phòng
10. HistoryCheckin - Lịch sử nhận phòng
11. HistoryCheckOut - Lịch sử trả phòng
12. RoomChangeHistory - Lịch sử đổi phòng
13. RoomUsageService - Dịch vụ sử dụng
14. Invoice - Hóa đơn

## Troubleshooting

### Lỗi kết nối database
- Kiểm tra SQL Server đã chạy chưa
- Kiểm tra connection string trong appsettings.json
- Kiểm tra tài khoản SQL Server có quyền truy cập

### Lỗi đăng nhập
- Kiểm tra đã tạo user trong database chưa
- Kiểm tra password hash đúng định dạng BCrypt
- Kiểm tra isActivate = 'ACTIVATE'

### Lỗi khi đặt phòng
- Kiểm tra phòng có trạng thái AVAILABLE
- Kiểm tra không trùng lịch đặt phòng khác
- Kiểm tra thời gian check-in < check-out

## Tác giả

Hệ thống được xây dựng dựa trên:
- Database schema: `docs/database/HotelManagement_new.sql`
- Business Flow Diagram (BFD): `docs/BFD.wsd`
- Data Flow Diagram (DFD): `docs/DFD_*.xml`
- Mô tả nghiệp vụ: `docs/description.txt`

## License

Dự án học tập - Quản lý Khách sạn

---

**Chúc bạn sử dụng hệ thống thành công! 🎉**
