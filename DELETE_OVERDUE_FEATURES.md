# Chức năng Xóa phiếu đặt phòng & Đánh dấu Quá hạn

## 📋 Tổng quan

Đã triển khai 2 chức năng mới cho hệ thống quản lý đặt phòng:

1. **Xóa phiếu đặt phòng** (Soft Delete)
2. **Đánh dấu phiếu đặt phòng quá hạn** (Visual Indicator)

✅ **Áp dụng cho 2 trang:**
- **Reservation/Index** - Quản lý Đặt phòng
- **CheckIn/Index** - Danh sách chờ Check-in

---

## ✅ 1. Chức năng Xóa phiếu đặt phòng

### A. Trang Quản lý Đặt phòng (Reservation/Index)

#### Backend - ReservationController.cs

**Vị trí:** `Controllers/ReservationController.cs` (sau method `CalculateDeposit`)

```csharp
[HttpPost]
public async Task<IActionResult> Delete(string id)
{
    if (!CheckAuth()) return RedirectToAction("Login", "Auth");

    try
    {
        var reservation = await _context.ReservationForms
            .Include(r => r.HistoryCheckin)
            .FirstOrDefaultAsync(r => r.ReservationFormID == id);

        if (reservation == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy phiếu đặt phòng!";
            return RedirectToAction("Index");
        }

        // Kiểm tra xem đã check-in chưa
        if (reservation.HistoryCheckin != null)
        {
            TempData["ErrorMessage"] = "Không thể xóa phiếu đặt phòng đã check-in!";
            return RedirectToAction("Index");
        }

        // Soft delete
        reservation.IsActivate = "DEACTIVATE";
        _context.Update(reservation);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã xóa phiếu đặt phòng {id} thành công!";
        return RedirectToAction("Index");
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = $"Lỗi khi xóa phiếu đặt phòng: {ex.Message}";
        return RedirectToAction("Index");
    }
}
```

### B. Trang Danh sách chờ Check-in (CheckIn/Index)

#### Backend - CheckInController.cs

**Vị trí:** `Controllers/CheckInController.cs` (sau method `CheckIn`)

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(string id)
{
    if (!CheckAuth()) return RedirectToAction("Login", "Auth");

    try
    {
        var reservation = await _context.ReservationForms
            .FirstOrDefaultAsync(r => r.ReservationFormID == id);

        if (reservation == null)
        {
            TempData["Error"] = "Không tìm thấy phiếu đặt phòng!";
            return RedirectToAction("Index");
        }

        // Kiểm tra xem đã check-in chưa
        var hasCheckedIn = await _context.HistoryCheckins
            .AnyAsync(h => h.ReservationFormID == id);

        if (hasCheckedIn)
        {
            TempData["Error"] = "Không thể xóa phiếu đặt phòng đã check-in!";
            return RedirectToAction("Index");
        }

        // Soft delete
        reservation.IsActivate = "DEACTIVATE";
        _context.Update(reservation);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa phiếu đặt phòng {id} thành công!";
        return RedirectToAction("Index");
    }
    catch (Exception ex)
    {
        TempData["Error"] = $"Lỗi khi xóa phiếu đặt phòng: {ex.Message}";
        return RedirectToAction("Index");
    }
}
```

### Frontend - Nút Xóa

**Reservation/Index.cshtml:**
```razor
@if (item.HistoryCheckin == null)
{
    <form asp-action="Delete" asp-route-id="@item.ReservationFormID" method="post" class="d-inline" 
          onsubmit="return confirm('Bạn có chắc muốn xóa phiếu đặt phòng này?');">
        @Html.AntiForgeryToken()
        <button type="submit" class="btn btn-danger-modern btn-sm" title="Xóa đặt phòng">
            <i class="fas fa-trash"></i>
        </button>
    </form>
}
```

**CheckIn/Index.cshtml:**
```razor
<form asp-action="Delete" asp-route-id="@item.ReservationFormID" method="post" class="d-inline" 
      onsubmit="return confirm('Bạn có chắc muốn xóa phiếu đặt phòng này?');">
    @Html.AntiForgeryToken()
    <button type="submit" class="btn btn-danger-modern btn-sm" title="Xóa đặt phòng">
        <i class="fas fa-trash"></i>
    </button>
</form>
```

### Tính năng:
✅ **Soft Delete** - Đặt `IsActivate = "DEACTIVATE"` thay vì xóa vật lý  
✅ **Kiểm tra điều kiện** - Chỉ cho xóa nếu chưa check-in  
✅ **Xác nhận trước khi xóa** - JavaScript confirm dialog  
✅ **Thông báo kết quả** - TempData success/error messages  
✅ **Ẩn nút xóa** - Không hiển thị nếu đã check-in (chỉ ở Reservation/Index)  

---

## 🔴 2. Chức năng Đánh dấu Quá hạn

### A. Trang Quản lý Đặt phòng (Reservation/Index)

#### Backend - ReservationController.Index()

```csharp
public async Task<IActionResult> Index()
{
    if (!CheckAuth()) return RedirectToAction("Login", "Auth");
    
    var reservations = await _context.ReservationForms
        .Include(r => r.Customer)
        .Include(r => r.Room)
        .ThenInclude(ro => ro!.RoomCategory)
        .Include(r => r.Employee)
        .Include(r => r.HistoryCheckin)  // ← Thêm Include
        .Where(r => r.IsActivate == "ACTIVATE")
        .OrderByDescending(r => r.ReservationDate)
        .ToListAsync();
    
    // Đánh dấu phiếu đặt phòng quá hạn
    ViewBag.OverdueReservations = reservations
        .Where(r => r.CheckOutDate < DateTime.Now && r.HistoryCheckin == null)
        .Select(r => r.ReservationFormID)
        .ToHashSet();
    
    return View(reservations);
}
```

**Điều kiện Quá hạn (Reservation):**
📌 **CheckOutDate < DateTime.Now** (Quá giờ check-out)  
📌 **HistoryCheckin == null** (Chưa check-in)  

### B. Trang Danh sách chờ Check-in (CheckIn/Index)

#### Backend - CheckInController.Index()

```csharp
public async Task<IActionResult> Index()
{
    if (!CheckAuth()) return RedirectToAction("Login", "Auth");
    
    // Lấy danh sách phòng đã đặt nhưng chưa check-in
    var pendingReservations = await _context.ReservationForms
        .Include(r => r.Customer)
        .Include(r => r.Room)
        .ThenInclude(ro => ro!.RoomCategory)
        .Where(r => r.IsActivate == "ACTIVATE" && 
                    !_context.HistoryCheckins.Any(h => h.ReservationFormID == r.ReservationFormID))
        .OrderBy(r => r.CheckInDate)
        .ToListAsync();

    // Đánh dấu phiếu đặt phòng quá hạn
    ViewBag.OverdueReservations = pendingReservations
        .Where(r => r.CheckOutDate < DateTime.Now)
        .Select(r => r.ReservationFormID)
        .ToHashSet();

    return View(pendingReservations);
}
```

**Điều kiện Quá hạn (CheckIn):**
📌 **CheckOutDate < DateTime.Now** (Quá giờ check-out)  
📌 Đã nằm trong danh sách chưa check-in (điều kiện sẵn có)

### Frontend - Visual Styling (Giống nhau cho cả 2 trang)

**1. Tô màu đỏ toàn bộ row:**

```razor
@{
    var isOverdue = ViewBag.OverdueReservations != null && 
                   ((HashSet<string>)ViewBag.OverdueReservations).Contains(item.ReservationFormID ?? "");
    var rowClass = isOverdue ? "table-danger" : "";
}

<tr class="@rowClass">
```

**2. Badge "Quá hạn" với icon:**

```razor
<td>
    <strong class="text-primary">#@item.ReservationFormID</strong>
    @if (isOverdue)
    {
        <br />
        <span class="badge bg-danger mt-1">
            <i class="fas fa-exclamation-triangle"></i> Quá hạn
        </span>
    }
</td>
```

**3. Thông báo Success/Error:**

**Reservation/Index.cshtml:**
```razor
@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success alert-dismissible fade show fade-in-up" role="alert">
        <i class="fas fa-check-circle me-2"></i>@TempData["SuccessMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
@if (TempData["ErrorMessage"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show fade-in-up" role="alert">
        <i class="fas fa-exclamation-triangle me-2"></i>@TempData["ErrorMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
```

**CheckIn/Index.cshtml:**
```razor
@if (TempData["Success"] != null)
{
    <div class="alert alert-success alert-dismissible fade show fade-in-up" role="alert">
        <i class="fas fa-check-circle me-2"></i>@Html.Raw(TempData["Success"])
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
@if (TempData["Error"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show fade-in-up" role="alert">
        <i class="fas fa-exclamation-triangle me-2"></i>@TempData["Error"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
@if (TempData["Warning"] != null)
{
    <div class="alert alert-warning alert-dismissible fade show fade-in-up" role="alert">
        <i class="fas fa-exclamation-circle me-2"></i>@TempData["Warning"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}
```

### Hiển thị:
🔴 **Row màu đỏ** - Class `table-danger` từ Bootstrap  
🔴 **Badge "Quá hạn"** - Màu đỏ với icon warning  
🔴 **Icon cảnh báo** - `fa-exclamation-triangle`  

---

## 🎯 Kết quả

### Bảng danh sách Đặt phòng sẽ có:

| Trạng thái | Hiển thị | Chức năng (Reservation/Index) | Chức năng (CheckIn/Index) |
|-----------|----------|-----------|-----------|
| **Bình thường** | Row màu trắng | Xem + Xóa | Check-in + Xem + Xóa |
| **Quá hạn** | Row màu đỏ + Badge "Quá hạn" | Xem + Xóa | Check-in + Xem + Xóa |
| **Đã check-in** | Row màu trắng | Chỉ Xem | Không xuất hiện |

### Flow hoạt động:

```
1. Load danh sách → Controller phát hiện quá hạn → Truyền qua ViewBag
2. View render → Kiểm tra từng row → Áp dụng class + badge
3. User click Xóa → Confirm dialog → POST Delete → Set IsActivate
4. Redirect về Index → Hiển thị TempData message → Row biến mất
```

---

## 📝 Files đã chỉnh sửa

### 1. Reservation (Quản lý Đặt phòng)

**Controllers/ReservationController.cs**
- Thêm method `Delete()` với soft delete logic
- Cập nhật `Index()` để phát hiện quá hạn
- Thêm `Include(r => r.HistoryCheckin)`

**Views/Reservation/Index.cshtml**
- Thêm logic phát hiện quá hạn trong view
- Thêm class `table-danger` cho row quá hạn
- Thêm badge "Quá hạn" màu đỏ
- Thay form Cancel thành form Delete
- Ẩn nút xóa nếu đã check-in
- Thêm alert thông báo TempData

### 2. CheckIn (Danh sách chờ Check-in)

**Controllers/CheckInController.cs**
- Thêm method `Delete()` với soft delete logic
- Cập nhật `Index()` để phát hiện quá hạn
- Thêm ViewBag.OverdueReservations

**Views/CheckIn/Index.cshtml**
- Thêm logic phát hiện quá hạn trong view
- Thêm class `table-danger` cho row quá hạn
- Thêm badge "Quá hạn" màu đỏ
- Thêm nút Xóa bên cạnh nút Check-in và Chi tiết
- Thêm alert thông báo Success/Error/Warning
- Rút gọn text "Chi tiết" thành icon

---

## 🧪 Cách test

### Test Xóa phiếu đặt phòng:

#### Từ Reservation/Index:
1. ✅ Tạo phiếu đặt phòng mới (chưa check-in)
2. ✅ Click nút Xóa → Hiện confirm dialog
3. ✅ Confirm → Redirect về Index → Hiện SuccessMessage
4. ✅ Phiếu đặt phòng biến mất khỏi danh sách

#### Từ CheckIn/Index:
1. ✅ Tạo phiếu đặt phòng mới (chưa check-in)
2. ✅ Click nút Xóa → Hiện confirm dialog
3. ✅ Confirm → Redirect về Index → Hiện Success message
4. ✅ Phiếu đặt phòng biến mất khỏi danh sách

### Test không cho xóa đã check-in:

#### Từ Reservation/Index:
1. ✅ Tạo phiếu đặt phòng → Check-in
2. ✅ Nút Xóa không hiện

#### Từ CheckIn/Index:
1. ✅ Tạo phiếu đặt phòng → Check-in
2. ✅ Phiếu đặt phòng biến mất khỏi danh sách chờ (vì đã check-in)

### Test đánh dấu quá hạn:

#### Từ Reservation/Index:
1. ✅ Tạo phiếu đặt phòng với CheckOutDate trong quá khứ
2. ✅ Không check-in
3. ✅ Load Index → Row màu đỏ + Badge "Quá hạn"

#### Từ CheckIn/Index:
1. ✅ Tạo phiếu đặt phòng với CheckOutDate trong quá khứ
2. ✅ Không check-in
3. ✅ Load Index → Row màu đỏ + Badge "Quá hạn"

### Test không đánh dấu quá hạn nếu đã check-in:

#### Từ Reservation/Index:
1. ✅ Tạo phiếu đặt phòng với CheckOutDate trong quá khứ
2. ✅ Check-in
3. ✅ Load Index → Row màu trắng (không quá hạn)

#### Từ CheckIn/Index:
1. ✅ Tạo phiếu đặt phòng với CheckOutDate trong quá khứ
2. ✅ Check-in
3. ✅ Load Index → Không hiện trong danh sách (đã check-in)

---

## 🔧 Lưu ý kỹ thuật

### Soft Delete:
- Không xóa vật lý record khỏi database
- Chỉ đặt `IsActivate = "DEACTIVATE"`
- Vẫn giữ lại dữ liệu lịch sử để báo cáo

### Performance:
- HashSet lookup O(1) cho kiểm tra quá hạn
- Single query để load tất cả reservations
- Include HistoryCheckin để tránh N+1 query (Reservation)
- LINQ subquery để filter chưa check-in (CheckIn)

### Security:
- CheckAuth() ở đầu mỗi action
- ValidateAntiForgeryToken cho POST request
- Kiểm tra null before delete

### UX:
- Confirm dialog trước khi xóa
- Success/Error messages rõ ràng
- Visual indicator (màu đỏ + badge) dễ nhận biết
- Icon rút gọn để tiết kiệm không gian

### Sự khác biệt giữa 2 trang:

| Tính năng | Reservation/Index | CheckIn/Index |
|-----------|------------------|---------------|
| **Điều kiện quá hạn** | CheckOutDate < Now AND không check-in | CheckOutDate < Now (trong danh sách chưa check-in) |
| **Ẩn nút xóa** | Ẩn nếu đã check-in | Luôn hiện (chỉ có phiếu chưa check-in) |
| **TempData key** | SuccessMessage/ErrorMessage | Success/Error/Warning |
| **Các nút khác** | Chi tiết | Check-in + Chi tiết |

---

✅ **Hoàn thành!** Hệ thống đã có đầy đủ chức năng xóa phiếu đặt phòng và đánh dấu phiếu quá hạn cho cả 2 trang: **Quản lý Đặt phòng** và **Danh sách chờ Check-in**.

