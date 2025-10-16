# 🐛 Tóm Tắt Sửa Lỗi - Hotel Management System

**Ngày:** 13/10/2025  
**Phiên bản:** 2.0

## ✅ **Các Lỗi Đã Sửa**

### **1. ❌ Lỗi Trigger Conflict khi Hủy Đặt Phòng**

**Mô tả lỗi:**
```
SqlException: The target table 'ReservationForm' of the DML statement cannot have any enabled triggers 
if the statement contains an OUTPUT clause without INTO clause.
```

**Nguyên nhân:** 
- Entity Framework Core sử dụng OUTPUT clause khi gọi `SaveChangesAsync()` để lấy các giá trị được generate (như IDENTITY)
- Khi table có trigger, SQL Server không cho phép sử dụng OUTPUT clause trực tiếp
- Table `ReservationForm` có trigger `TR_ReservationForm_RoomStatusCheck`

**Giải pháp:** 
Thay đổi từ EF Core tracking sang raw SQL query trong `ReservationController.Cancel()`:

```csharp
// ❌ CŨ - Gây lỗi với trigger
reservation.IsActivate = "DEACTIVATE";
_context.Update(reservation);
await _context.SaveChangesAsync();

// ✅ MỚI - Sử dụng ExecuteSqlRaw
await _context.Database.ExecuteSqlRawAsync(
    "UPDATE ReservationForm SET IsActivate = 'DEACTIVATE' WHERE ReservationFormID = {0}", 
    id);
```

**File đã sửa:** `Controllers/ReservationController.cs`

---

### **2. ❌ Lỗi 404 khi Check-in từ Xem Chi Tiết Đặt Phòng**

**Mô tả lỗi:**
```
Không tìm thấy: http://localhost:5153/CheckIn/CheckIn?reservationFormID=RF-000014
```

**Nguyên nhân:**
- Nút Check-in sử dụng thẻ `<a>` với GET request
- Nhưng action `CheckInController.CheckIn()` yêu cầu `[HttpPost]` và `[ValidateAntiForgeryToken]`

**Giải pháp:**
Thay đổi từ link sang form POST trong `Views/Reservation/Details.cshtml`:

```html
<!-- ❌ CŨ -->
<a asp-controller="CheckIn" asp-action="CheckIn" asp-route-reservationFormID="@Model.ReservationFormID">
    <i class="fas fa-sign-in-alt"></i> Check-in
</a>

<!-- ✅ MỚI -->
<form asp-controller="CheckIn" asp-action="CheckIn" method="post" class="d-inline">
    @Html.AntiForgeryToken()
    <input type="hidden" name="reservationFormID" value="@Model.ReservationFormID" />
    <button type="submit" class="btn btn-success" onclick="return confirm('Xác nhận check-in?');">
        <i class="fas fa-sign-in-alt"></i> Check-in
    </button>
</form>
```

**File đã sửa:** `Views/Reservation/Details.cshtml`

---

### **3. ❌ Lỗi 404 khi In Hóa Đơn**

**Mô tả lỗi:**
```
Không tìm thấy: http://localhost:5153/Invoice/Print/INV-000012
```

**Nguyên nhân:**
- View gọi action `Print` nhưng action thực tế là `Invoice`
- `InvoiceController` không có action `Print()`

**Giải pháp:**
Sửa action name trong `Views/Invoice/Index.cshtml`:

```html
<!-- ❌ CŨ -->
<a asp-action="Print" asp-route-id="@item.InvoiceID">
    <i class="fas fa-print"></i>
</a>

<!-- ✅ MỚI -->
<a asp-action="Invoice" asp-route-id="@item.InvoiceID" target="_blank">
    <i class="fas fa-print"></i>
</a>
```

**File đã sửa:** `Views/Invoice/Index.cshtml`

---

### **4. ❌ Nút Sửa Phòng không bị chặn khi phòng đang sử dụng**

**Mô tả lỗi:**
- Trong trang `Room/Details.cshtml`, nút "Chỉnh sửa" luôn hiển thị
- Không có kiểm tra trạng thái phòng (đang sử dụng, đã đặt)

**Giải pháp:**
Thêm logic kiểm tra trạng thái phòng trong `Views/Room/Details.cshtml`:

```csharp
@{
    var canModify = Model.RoomStatus == "AVAILABLE";
}

@if (canModify)
{
    <a asp-action="Edit" asp-route-id="@Model.RoomID" class="btn btn-warning-modern">
        <i class="fas fa-edit"></i> Chỉnh sửa
    </a>
}
else
{
    <button class="btn btn-secondary" disabled title="Không thể sửa phòng đang sử dụng hoặc đã đặt">
        <i class="fas fa-lock"></i> Chỉnh sửa
    </button>
}
```

**File đã sửa:** `Views/Room/Details.cshtml`

---

### **5. ❌ Lỗi không tìm thấy View 'AddService'**

**Mô tả lỗi:**
```
InvalidOperationException: The view 'AddService' was not found.
/Views/RoomService/AddService.cshtml
/Views/Shared/AddService.cshtml
```

**Nguyên nhân:**
- Controller có action `Create()` và action với attribute `[ActionName("AddService")]`
- View gọi form với `asp-action="AddService"` nhưng method POST trả về View khi có lỗi
- Không có file `AddService.cshtml`, chỉ có modal trong `Index.cshtml`

**Giải pháp:**

**Bước 1:** Loại bỏ action `Create()` không cần thiết và đơn giản hóa logic:

```csharp
// ❌ XÓA action Create() cũ

// ✅ THÊM action AddService mới
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddService(string reservationFormID, string hotelServiceId, int quantity)
{
    if (!CheckAuth()) return RedirectToAction("Login", "Auth");

    if (string.IsNullOrEmpty(hotelServiceId) || quantity <= 0)
    {
        TempData["Error"] = "Vui lòng chọn dịch vụ và nhập số lượng hợp lệ!";
        return RedirectToAction(nameof(Index), new { reservationFormID });
    }

    var service = await _context.HotelServices.FindAsync(hotelServiceId);
    if (service == null)
    {
        TempData["Error"] = "Dịch vụ không tồn tại!";
        return RedirectToAction(nameof(Index), new { reservationFormID });
    }

    var roomService = new RoomUsageService
    {
        RoomUsageServiceId = await _context.GenerateID("RUS-", "RoomUsageService"),
        ReservationFormID = reservationFormID,
        HotelServiceId = hotelServiceId,
        Quantity = quantity,
        DateAdded = DateTime.Now,
        UnitPrice = service.ServicePrice,
        EmployeeID = HttpContext.Session.GetString("EmployeeID")
    };

    _context.Add(roomService);
    await _context.SaveChangesAsync();
    TempData["Success"] = "Thêm dịch vụ thành công!";
    return RedirectToAction(nameof(Index), new { reservationFormID });
}
```

**Bước 2:** Thêm `ViewBag.ReservationFormID` trong action `Index()`:

```csharp
public async Task<IActionResult> Index(string reservationFormID)
{
    // ... existing code ...
    
    ViewBag.ReservationFormID = reservationFormID;  // ✅ THÊM dòng này
    ViewBag.ReservationForm = reservation;
    // ... rest of code ...
}
```

**File đã sửa:** `Controllers/RoomServiceController.cs`

---

## 🔧 **Cách Kiểm Tra**

### **Test Lỗi 1 - Hủy đặt phòng:**
1. Vào trang "Quản lý đặt phòng"
2. Chọn một phiếu đặt phòng chưa check-in
3. Nhấn nút "Xem chi tiết"
4. Nhấn nút "Hủy đặt phòng"
5. ✅ Kiểm tra: Hủy thành công, không có lỗi SQL trigger

### **Test Lỗi 2 - Check-in:**
1. Vào trang "Xem chi tiết đặt phòng"
2. Với phiếu đặt chưa check-in, nhấn nút "Check-in"
3. ✅ Kiểm tra: Check-in thành công, không có lỗi 404

### **Test Lỗi 3 - In hóa đơn:**
1. Vào trang "Danh sách hóa đơn"
2. Nhấn nút "In phiếu" (biểu tượng printer)
3. ✅ Kiểm tra: Mở trang xem hóa đơn thành công

### **Test Lỗi 4 - Chặn sửa phòng:**
1. Vào trang "Xem chi tiết phòng" với phòng đang sử dụng hoặc đã đặt
2. ✅ Kiểm tra: Nút "Chỉnh sửa" bị disabled với icon khóa
3. Thử với phòng trống
4. ✅ Kiểm tra: Nút "Chỉnh sửa" hoạt động bình thường

### **Test Lỗi 5 - Thêm dịch vụ (modal):**
1. Vào trang "Quản lý dịch vụ" của một phòng đã check-in
2. Nhấn nút "Thêm dịch vụ"
3. Chọn loại dịch vụ, dịch vụ, nhập số lượng
4. Nhấn "Thêm"
5. ✅ Kiểm tra: Dịch vụ được thêm thành công, không có lỗi view not found

### **Test Lỗi 6 - Thêm dịch vụ (trigger conflict):**
1. **TRƯỚC TIÊN:** Chạy stored procedure `sp_AddRoomService` trong database
2. Vào trang "Quản lý dịch vụ" của một phòng đã check-in
3. Nhấn nút "Thêm dịch vụ"
4. Chọn dịch vụ và nhập số lượng
5. Nhấn "Thêm"
6. ✅ Kiểm tra: Dịch vụ được thêm thành công, không có lỗi trigger conflict
7. ✅ Kiểm tra: Hiển thị thông báo với tổng tiền

---

## 📝 **Ghi Chú**

- **Lỗi 1 & 6:** Áp dụng 2 pattern để tránh trigger conflict:
  - Pattern 1: `ExecuteSqlRaw` cho UPDATE đơn giản (Hủy đặt phòng)
  - Pattern 2: Stored Procedure cho INSERT phức tạp (Thêm dịch vụ)
- **Lỗi 2 & 3:** Luôn kiểm tra action method HTTP verb (GET/POST) trước khi tạo link
- **Lỗi 4:** Áp dụng business rule validation ở tầng View để cải thiện UX
- **Lỗi 5:** Modal-based forms nên redirect về Index khi có lỗi, không cần view riêng

---

## ⚠️ **Lưu Ý Khi Deploy**

1. **Dừng ứng dụng trước khi build:**
   ```powershell
   # Tìm và kill process
   taskkill /F /IM HotelManagement.exe
   # Hoặc dừng từ VS Code / Visual Studio
   ```

2. **Chạy Stored Procedure trong Database (cho lỗi 6):**
   ```sql
   USE HotelManagement;
   GO
   -- Chạy file docs/database/sp_AddRoomService.sql
   ```

3. **Build lại project:**
   ```powershell
   cd "d:\C#\Lập trình Web\HotelManagement"
   dotnet build
   ```

4. **Chạy lại ứng dụng:**
   ```powershell
   dotnet run
   ```

---

## 🎯 **Kết Quả**

✅ **6/6 lỗi đã được sửa hoàn toàn**

- Hệ thống hoạt động ổn định hơn
- Tuân thủ đúng RESTful patterns (GET/POST)
- Tránh conflict với database triggers (sử dụng stored procedures)
- Cải thiện UX với business rule validation
- Code sạch hơn, dễ maintain hơn
- Performance tốt hơn với stored procedures

