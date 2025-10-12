# Hướng dẫn Khởi động Nhanh

## Các bước thực hiện

### 1. Cài đặt SQL Server
- Đảm bảo SQL Server đã được cài đặt và đang chạy
- Ghi nhớ tên Server (thường là `localhost` hoặc `.\SQLEXPRESS`)

### 2. Tạo Database

**Cách 1: Sử dụng SQL Server Management Studio (SSMS)**
1. Mở SSMS và kết nối đến SQL Server
2. Mở file `docs\database\HotelManagement_new.sql`
3. Nhấn Execute (F5) để chạy script
4. Mở file `docs\database\CreateUser.sql`
5. Nhấn Execute (F5) để tạo tài khoản

**Cách 2: Sử dụng sqlcmd**
```powershell
sqlcmd -S localhost -i "docs\database\HotelManagement_new.sql"
sqlcmd -S localhost -i "docs\database\CreateUser.sql"
```

### 3. Cấu hình Connection String

Mở file `appsettings.json` và kiểm tra/sửa connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HotelManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Lưu ý:**
- Thay `localhost` bằng tên SQL Server của bạn
- Nếu dùng SQL Authentication, thay `Trusted_Connection=True` bằng `User Id=sa;Password=yourpassword`

### 4. Chạy ứng dụng

**Cách 1: Sử dụng script PowerShell (Khuyến nghị)**
```powershell
.\start.ps1
```

**Cách 2: Chạy thủ công**
```powershell
dotnet restore
dotnet build
dotnet run
```

### 5. Truy cập ứng dụng

Mở trình duyệt và truy cập:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000

### 6. Đăng nhập

**Tài khoản Admin:**
- Username: `admin`
- Password: `admin123`

**Tài khoản Nhân viên:**
- Username: `employee`
- Password: `employee123`

## Quy trình sử dụng cơ bản

### A. Đặt phòng mới
1. Thêm khách hàng mới (nếu chưa có): **Danh mục → Khách hàng → Thêm mới**
2. Đặt phòng: **Đặt phòng → Thêm mới**
   - Chọn khách hàng
   - Chọn thời gian check-in/out
   - Chọn phòng (hệ thống tự lọc phòng trống)
   - Nhập tiền cọc

### B. Check-in
1. Vào menu **Check-in**
2. Chọn phiếu đặt phòng cần check-in
3. Nhấn nút **Check-in**

### C. Thêm dịch vụ
1. Vào menu **Đặt phòng**
2. Nhấn **Chi tiết** phiếu đang ở
3. Nhấn **Thêm dịch vụ**
4. Chọn dịch vụ và số lượng

### D. Check-out & Thanh toán
1. Vào menu **Check-out**
2. Chọn phòng cần check-out
3. Xem chi tiết hóa đơn
4. Nhấn **Check-out**
5. Hệ thống tự động:
   - Tính tiền phòng
   - Tính tiền dịch vụ
   - Cộng thuế VAT 10%
   - Trừ tiền đặt cọc
   - Tạo hóa đơn

### E. Xem hóa đơn
1. Vào menu **Hóa đơn**
2. Chọn hóa đơn cần xem
3. Có thể in hoặc xuất PDF

## Xử lý lỗi thường gặp

### Lỗi: "Cannot connect to SQL Server"
**Giải pháp:**
1. Kiểm tra SQL Server đã chạy chưa
2. Kiểm tra tên Server trong connection string
3. Kiểm tra SQL Server cho phép remote connection

### Lỗi: "Login failed"
**Giải pháp:**
1. Kiểm tra đã chạy file `CreateUser.sql` chưa
2. Kiểm tra password đúng: `admin123`
3. Kiểm tra user có `isActivate = 'ACTIVATE'`

### Lỗi: "Invalid object name"
**Giải pháp:**
1. Chạy lại file `HotelManagement_new.sql`
2. Kiểm tra database name = `HotelManagement`

## Tính năng chính

✅ Quản lý nhân viên, khách hàng  
✅ Quản lý phòng và loại phòng  
✅ Quản lý giá thuê (theo ngày/giờ)  
✅ Đặt phòng với kiểm tra trùng lịch  
✅ Check-in/Check-out tự động  
✅ Quản lý dịch vụ sử dụng  
✅ Tính toán hóa đơn tự động  
✅ Phụ phí trả phòng muộn  
✅ Thuế VAT 10%  
✅ Báo cáo và thống kê  

## Liên hệ & Hỗ trợ

Nếu gặp vấn đề, vui lòng xem file `README.md` để biết thêm chi tiết hoặc liên hệ hỗ trợ.

---

**Chúc bạn sử dụng thành công! 🎉**
