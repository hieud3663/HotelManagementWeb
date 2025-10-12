# Script khởi động hệ thống quản lý khách sạn
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  HỆ THỐNG QUẢN LÝ KHÁCH SẠN" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Bước 1: Kiểm tra .NET SDK
Write-Host "[1/5] Kiểm tra .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "OK .NET SDK version: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "X Không tìm thấy .NET SDK. Vui lòng cài đặt .NET 9.0 SDK!" -ForegroundColor Red
    exit 1
}

# Bước 2: Restore packages
Write-Host ""
Write-Host "[2/5] Cài đặt các package NuGet..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "OK Restore packages thành công!" -ForegroundColor Green
}
else {
    Write-Host "X Lỗi khi restore packages!" -ForegroundColor Red
    exit 1
}

# Bước 3: Build project
Write-Host ""
Write-Host "[3/5] Build project..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -eq 0) {
    Write-Host "OK Build thành công!" -ForegroundColor Green
}
else {
    Write-Host "X Lỗi khi build project!" -ForegroundColor Red
    exit 1
}

# Bước 4: Hướng dẫn setup database
Write-Host ""
Write-Host "[4/5] Thiết lập Database..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Cyan
Write-Host "QUAN TRỌNG: Vui lòng thực hiện các bước sau:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Mở SQL Server Management Studio (SSMS)" -ForegroundColor White
Write-Host "2. Chạy file: docs\database\HotelManagement_new.sql" -ForegroundColor White
Write-Host "   (File này sẽ tạo database và dữ liệu mẫu)" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Sau đó chạy file: docs\database\CreateUser.sql" -ForegroundColor White
Write-Host "   (File này sẽ tạo tài khoản admin/employee)" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Kiểm tra Connection String trong appsettings.json:" -ForegroundColor White
Write-Host "   Server=localhost;Database=HotelManagement;..." -ForegroundColor Gray
Write-Host "   (Thay localhost bằng tên SQL Server của bạn)" -ForegroundColor Gray
Write-Host "----------------------------------------" -ForegroundColor Cyan
Write-Host ""

$confirm = Read-Host "Bạn đã hoàn thành các bước trên chưa? (y/n)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host ""
    Write-Host "Vui lòng hoàn thành setup database trước khi chạy ứng dụng!" -ForegroundColor Yellow
    Write-Host "Chi tiết xem file README.md" -ForegroundColor Cyan
    exit 0
}

# Bước 5: Chạy ứng dụng
Write-Host ""
Write-Host "[5/5] Khởi động ứng dụng..." -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Cyan
Write-Host "Ứng dụng sẽ chạy tại:" -ForegroundColor White
Write-Host "  - HTTPS: https://localhost:5001" -ForegroundColor Green
Write-Host "  - HTTP:  http://localhost:5000" -ForegroundColor Green
Write-Host ""
Write-Host "Tài khoản đăng nhập mặc định:" -ForegroundColor White
Write-Host "  - Admin:    admin / admin123" -ForegroundColor Cyan
Write-Host "  - Employee: employee / employee123" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Cyan
Write-Host ""
Write-Host "Nhấn Ctrl+C để dừng ứng dụng" -ForegroundColor Yellow
Write-Host ""

dotnet run
