# Thiết kế lại Luồng Thanh toán và Trả phòng

## 📋 Tổng quan

Thay đổi lớn trong luồng thanh toán với **2 tùy chọn** cho khách hàng:

### **Option 1: Trả phòng và Thanh toán** ⏰
- Khách checkout → Tính tiền theo **thời gian thực tế** (từ check-in thực tế đến check-out thực tế)
- Tạo hóa đơn nhưng **chưa thanh toán ngay**
- Chuyển sang bước "Xác nhận thanh toán"
- Phòng **chưa được giải phóng** cho đến khi thanh toán xong

### **Option 2: Thanh toán trước** 💰
- Khách thanh toán **trước khi** trả phòng
- Tính tiền từ **check-in thực tế** đến **check-out dự kiến**
- Tạo hóa đơn và **xác nhận thanh toán ngay**
- Khách vẫn ở lại phòng cho đến check-out dự kiến
- Khi thực sự checkout, nếu checkout muộn sẽ tính phụ phí bổ sung

---

## 🔄 Luồng mới

```
┌─────────────────────────────────────────────────────────────────┐
│                   KHÁCH HÀNG Đ CHECK-IN                        │
│                   (Phòng = ON_USE)                              │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 v
      ┌──────────┴──────────┐
      │   KHÁCH HÀNG QUYẾT ĐỊNH   │
      └──────────┬──────────┘
                 │
        ┌────────┴────────┐
        │                 │
        v                 v
┌───────────────┐  ┌──────────────────┐
│  OPTION 1:    │  │   OPTION 2:      │
│  TRẢ PHÒNG &  │  │  THANH TOÁN TRƯỚC│
│  THANH TOÁN   │  │                  │
└───────┬───────┘  └────────┬─────────┘
        │                   │
        │                   │
        v                   v
┌─────────────────┐  ┌──────────────────┐
│ 1. Checkout NOW │  │ 1. Tính tiền:    │
│ 2. Tính tiền:   │  │    checkInActual │
│    checkInActual│  │    -> checkOut   │
│    -> checkOut  │  │    EXPECTED      │
│    ACTUAL       │  │ 2. Tạo Invoice   │
│ 3. Tạo Invoice  │  │ 3. THANH TOÁN    │
│    (UNPAID)     │  │    NGAY          │
│ 4. Phòng LOCKED │  │ 4. Phòng vẫn     │
└────────┬────────┘  │    ON_USE        │
         │           └────────┬─────────┘
         v                    │
┌─────────────────┐           │
│ XÁC NHẬN        │           │
│ THANH TOÁN      │           │
│ (Payment Page)  │           │
└────────┬────────┘           │
         │                    │
         v                    v
┌─────────────────┐  ┌──────────────────┐
│ Invoice.isPaid  │  │ Khách ở tiếp     │
│ = TRUE          │  │ đến checkout     │
│ Phòng AVAILABLE │  │ dự kiến          │
│ Hoàn tất        │  └────────┬─────────┘
└─────────────────┘           │
                              v
                     ┌──────────────────┐
                     │ Checkout thực tế │
                     │ - Nếu đúng giờ:  │
                     │   Done           │
                     │ - Nếu muộn:      │
                     │   Tính phụ phí   │
                     │   bổ sung        │
                     │ Phòng AVAILABLE  │
                     └──────────────────┘
```

---

## 📊 Thay đổi Database Schema

### 1. Bảng Invoice - Thêm trường trạng thái

```sql
ALTER TABLE Invoice
ADD isPaid BIT NOT NULL DEFAULT 0,  -- 0 = Chưa thanh toán, 1 = Đã thanh toán
    paymentDate DATETIME NULL,       -- Ngày thanh toán thực tế
    paymentMethod NVARCHAR(20) NULL, -- CASH, CARD, TRANSFER
    checkoutType NVARCHAR(20) NULL;  -- 'CHECKOUT_THEN_PAY' hoặc 'PAY_THEN_CHECKOUT'
GO
```

### 2. Bảng HistoryCheckOut - Thêm trường invoice

```sql
ALTER TABLE HistoryCheckOut
ADD invoiceID NVARCHAR(15) NULL,
FOREIGN KEY (invoiceID) REFERENCES Invoice(invoiceID);
GO
```

---

## 🔧 Stored Procedures Mới

### 1. `sp_CreateInvoice_CheckoutThenPay` - Trả phòng rồi thanh toán

```sql
CREATE OR ALTER PROCEDURE sp_CreateInvoice_CheckoutThenPay
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra đã check-in chưa
        IF NOT EXISTS (SELECT 1 FROM HistoryCheckin WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Phiếu đặt phòng chưa check-in', 16, 1);
            ROLLBACK; RETURN -1;
        END
        
        -- Kiểm tra đã checkout chưa
        IF EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Phiếu đặt phòng đã checkout', 16, 1);
            ROLLBACK; RETURN -1;
        END
        
        -- Lấy thông tin
        DECLARE @roomID NVARCHAR(15);
        SELECT @roomID = roomID FROM ReservationForm WHERE reservationFormID = @reservationFormID;
        
        -- Tạo HistoryCheckOut với thời gian HIỆN TẠI
        DECLARE @checkOutID NVARCHAR(15) = dbo.fn_GenerateID('CO-', 'HistoryCheckOut', 'historyCheckOutID', 6);
        
        INSERT INTO HistoryCheckOut (historyCheckOutID, checkOutDate, reservationFormID, employeeID)
        VALUES (@checkOutID, GETDATE(), @reservationFormID, @employeeID);
        
        -- Tạo Invoice (Trigger sẽ tự động tính toán dựa trên checkOutActual)
        DECLARE @invoiceID NVARCHAR(15) = dbo.fn_GenerateID('INV-', 'Invoice', 'invoiceID', 6);
        
        INSERT INTO Invoice (invoiceID, invoiceDate, roomCharge, servicesCharge, reservationFormID, isPaid, checkoutType)
        VALUES (@invoiceID, GETDATE(), 0, 0, @reservationFormID, 0, 'CHECKOUT_THEN_PAY');
        
        -- Cập nhật invoiceID vào HistoryCheckOut
        UPDATE HistoryCheckOut SET invoiceID = @invoiceID WHERE historyCheckOutID = @checkOutID;
        
        -- Cập nhật trạng thái phòng thành LOCKED (đợi thanh toán)
        UPDATE Room SET roomStatus = 'UNAVAILABLE' WHERE roomID = @roomID;
        
        -- Trả về thông tin invoice
        SELECT 
            inv.invoiceID,
            inv.roomCharge,
            inv.servicesCharge,
            inv.totalAmount,
            inv.isPaid,
            inv.checkoutType,
            @checkOutID AS checkOutID
        FROM Invoice inv
        WHERE inv.invoiceID = @invoiceID;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
```

### 2. `sp_CreateInvoice_PayThenCheckout` - Thanh toán trước

```sql
CREATE OR ALTER PROCEDURE sp_CreateInvoice_PayThenCheckout
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15),
    @paymentMethod NVARCHAR(20) = 'CASH'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra đã check-in chưa
        IF NOT EXISTS (SELECT 1 FROM HistoryCheckin WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Phiếu đặt phòng chưa check-in', 16, 1);
            ROLLBACK; RETURN -1;
        END
        
        -- Kiểm tra đã có invoice chưa
        IF EXISTS (SELECT 1 FROM Invoice WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Hóa đơn đã tồn tại', 16, 1);
            ROLLBACK; RETURN -1;
        END
        
        -- Tạo Invoice với checkOutDate = Expected (chưa checkout thực tế)
        -- Trigger sẽ tính dựa trên checkInActual -> checkOutExpected
        DECLARE @invoiceID NVARCHAR(15) = dbo.fn_GenerateID('INV-', 'Invoice', 'invoiceID', 6);
        
        INSERT INTO Invoice (
            invoiceID, invoiceDate, roomCharge, servicesCharge, 
            reservationFormID, isPaid, paymentDate, paymentMethod, checkoutType
        )
        VALUES (
            @invoiceID, GETDATE(), 0, 0, 
            @reservationFormID, 1, GETDATE(), @paymentMethod, 'PAY_THEN_CHECKOUT'
        );
        
        -- Phòng vẫn ON_USE, khách tiếp tục ở
        
        -- Trả về thông tin invoice
        SELECT 
            inv.invoiceID,
            inv.roomCharge,
            inv.servicesCharge,
            inv.totalAmount,
            inv.isPaid,
            inv.paymentDate,
            inv.paymentMethod,
            inv.checkoutType
        FROM Invoice inv
        WHERE inv.invoiceID = @invoiceID;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
```

### 3. `sp_ConfirmPayment` - Xác nhận thanh toán (cho Option 1)

```sql
CREATE OR ALTER PROCEDURE sp_ConfirmPayment
    @invoiceID NVARCHAR(15),
    @paymentMethod NVARCHAR(20) = 'CASH',
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra invoice tồn tại
        IF NOT EXISTS (SELECT 1 FROM Invoice WHERE invoiceID = @invoiceID)
        BEGIN
            RAISERROR('Không tìm thấy hóa đơn', 16, 1);
            ROLLBACK; RETURN -1;
        END
        
        -- Kiểm tra đã thanh toán chưa
        DECLARE @isPaid BIT;
        SELECT @isPaid = isPaid FROM Invoice WHERE invoiceID = @invoiceID;
        
        IF @isPaid = 1
        BEGIN
            RAISERROR('Hóa đơn đã được thanh toán', 16, 1);
            ROLLBACK; RETURN -1;
        END
        
        -- Cập nhật trạng thái thanh toán
        UPDATE Invoice
        SET isPaid = 1,
            paymentDate = GETDATE(),
            paymentMethod = @paymentMethod
        WHERE invoiceID = @invoiceID;
        
        -- Lấy roomID để giải phóng phòng
        DECLARE @roomID NVARCHAR(15);
        SELECT @roomID = rf.roomID
        FROM Invoice inv
        JOIN ReservationForm rf ON inv.reservationFormID = rf.reservationFormID
        WHERE inv.invoiceID = @invoiceID;
        
        -- Giải phóng phòng
        UPDATE Room SET roomStatus = 'AVAILABLE' WHERE roomID = @roomID;
        
        -- Trả về thông tin
        SELECT 
            invoiceID,
            isPaid,
            paymentDate,
            paymentMethod,
            totalAmount
        FROM Invoice
        WHERE invoiceID = @invoiceID;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
```

### 4. `sp_ActualCheckout_AfterPrepayment` - Checkout thực tế sau khi thanh toán trước

```sql
CREATE OR ALTER PROCEDURE sp_ActualCheckout_AfterPrepayment
    @reservationFormID NVARCHAR(15),
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Kiểm tra đã thanh toán chưa
        DECLARE @invoiceID NVARCHAR(15);
        DECLARE @isPaid BIT;
        
        SELECT @invoiceID = invoiceID, @isPaid = isPaid
        FROM Invoice
        WHERE reservationFormID = @reservationFormID;
        
        IF @invoiceID IS NULL OR @isPaid = 0
        BEGIN
            RAISERROR('Phải thanh toán trước khi checkout', 16, 1);
            ROLLBACK; RETURN -1;
        END
        
        -- Kiểm tra đã checkout chưa
        IF EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR('Đã checkout rồi', 16, 1);
            ROLLBACK; RETURN -1;
        END
        
        -- Lấy thông tin
        DECLARE @roomID NVARCHAR(15);
        DECLARE @checkOutExpected DATETIME;
        DECLARE @checkOutActual DATETIME = GETDATE();
        
        SELECT 
            @roomID = rf.roomID,
            @checkOutExpected = rf.checkOutDate
        FROM ReservationForm rf
        WHERE rf.reservationFormID = @reservationFormID;
        
        -- Tạo HistoryCheckOut
        DECLARE @checkOutID NVARCHAR(15) = dbo.fn_GenerateID('CO-', 'HistoryCheckOut', 'historyCheckOutID', 6);
        
        INSERT INTO HistoryCheckOut (historyCheckOutID, checkOutDate, reservationFormID, employeeID, invoiceID)
        VALUES (@checkOutID, @checkOutActual, @reservationFormID, @employeeID, @invoiceID);
        
        -- Kiểm tra checkout muộn
        DECLARE @additionalCharge DECIMAL(18,2) = 0;
        
        IF @checkOutActual > @checkOutExpected
        BEGIN
            -- Tính phụ phí checkout muộn
            -- Trigger TR_Invoice_ManageUpdate sẽ tự động tính lại lateCheckoutFee
            UPDATE Invoice 
            SET invoiceDate = GETDATE()  -- Force trigger update
            WHERE invoiceID = @invoiceID;
            
            SELECT @additionalCharge = lateCheckoutFee
            FROM Invoice
            WHERE invoiceID = @invoiceID;
        END
        
        -- Giải phóng phòng
        UPDATE Room SET roomStatus = 'AVAILABLE' WHERE roomID = @roomID;
        
        -- Trả về thông tin
        SELECT 
            @checkOutID AS checkOutID,
            @checkOutActual AS checkOutDate,
            @checkOutExpected AS checkOutExpected,
            @additionalCharge AS additionalCharge,
            CASE 
                WHEN @additionalCharge > 0 THEN 'LATE_CHECKOUT'
                ELSE 'ON_TIME'
            END AS checkoutStatus
        FROM Invoice
        WHERE invoiceID = @invoiceID;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
```

---

## 🔄 Sửa Trigger Invoice

### Trigger phải xử lý 2 trường hợp:

**Case 1: CHECKOUT_THEN_PAY**
- Tính từ `checkInActual` → `checkOutActual` (đã có trong HistoryCheckOut)

**Case 2: PAY_THEN_CHECKOUT**
- Tính từ `checkInActual` → `checkOutExpected` (chưa checkout)
- Khi checkout thực tế, nếu muộn → trigger UPDATE sẽ tính thêm phụ phí

```sql
-- Đã có trigger TR_Invoice_ManageInsert và TR_Invoice_ManageUpdate
-- Chúng sẽ tự động xử lý dựa trên:
-- - Nếu có checkOutActual → dùng checkOutActual
-- - Nếu chưa có → dùng checkOutExpected

-- Trigger hiện tại ĐÃ ĐÚNG với logic này:
-- @effectiveCheckOut = ISNULL(@checkOutDateActual, @checkOutDateExpected)
```

---

## 🎨 Controller Changes

### CheckOutController - 2 Actions mới

```csharp
// Option 1: Checkout rồi thanh toán
[HttpPost]
public async Task<IActionResult> CheckoutThenPay(string reservationFormID)
{
    var result = await _context.CreateInvoice_CheckoutThenPay(reservationFormID, employeeID);
    
    if (result != null)
    {
        TempData["Success"] = "Checkout thành công. Vui lòng thanh toán.";
        return RedirectToAction("Payment", new { invoiceID = result.InvoiceID });
    }
    
    TempData["Error"] = "Checkout thất bại";
    return RedirectToAction("Index");
}

// Option 2: Thanh toán trước
[HttpPost]
public async Task<IActionResult> PayThenCheckout(string reservationFormID, string paymentMethod)
{
    var result = await _context.CreateInvoice_PayThenCheckout(reservationFormID, employeeID, paymentMethod);
    
    if (result != null)
    {
        TempData["Success"] = "Thanh toán thành công. Quý khách có thể ở đến " + checkOutExpected;
        return RedirectToAction("Receipt", new { invoiceID = result.InvoiceID });
    }
    
    TempData["Error"] = "Thanh toán thất bại";
    return RedirectToAction("Index");
}

// Trang xác nhận thanh toán (cho Option 1)
[HttpGet]
public async Task<IActionResult> Payment(string invoiceID)
{
    var invoice = await _context.Invoices
        .Include(i => i.ReservationForm)
        .ThenInclude(rf => rf.Customer)
        .FirstOrDefaultAsync(i => i.InvoiceID == invoiceID);
    
    return View(invoice);
}

[HttpPost]
public async Task<IActionResult> ConfirmPayment(string invoiceID, string paymentMethod)
{
    var result = await _context.ConfirmPayment(invoiceID, paymentMethod, employeeID);
    
    if (result != null)
    {
        TempData["Success"] = "Thanh toán thành công!";
        return RedirectToAction("Receipt", new { invoiceID });
    }
    
    TempData["Error"] = "Thanh toán thất bại";
    return RedirectToAction("Payment", new { invoiceID });
}

// Checkout thực tế sau khi đã thanh toán trước
[HttpPost]
public async Task<IActionResult> ActualCheckout(string reservationFormID)
{
    var result = await _context.ActualCheckout_AfterPrepayment(reservationFormID, employeeID);
    
    if (result != null)
    {
        if (result.CheckoutStatus == "LATE_CHECKOUT" && result.AdditionalCharge > 0)
        {
            TempData["Warning"] = $"Checkout muộn. Phụ phí: {result.AdditionalCharge:N0} VNĐ";
            return RedirectToAction("AdditionalPayment", new { invoiceID = result.InvoiceID });
        }
        
        TempData["Success"] = "Checkout thành công!";
        return RedirectToAction("Index");
    }
    
    TempData["Error"] = "Checkout thất bại";
    return RedirectToAction("Index");
}
```

---

## 📱 Views Changes

### CheckOut/Index.cshtml - 2 nút cho mỗi phòng

```razor
@foreach (var item in Model)
{
    <tr>
        <!-- ... thông tin phòng ... -->
        <td>
            @if (item.Invoice == null)
            {
                <!-- Chưa có invoice -> hiển thị 2 nút -->
                <div class="btn-group">
                    <!-- Nút 1: Thanh toán trước -->
                    <button type="button" class="btn btn-success btn-sm" 
                            onclick="showPaymentMethodModal('@item.ReservationFormID', 'prepay')">
                        <i class="fas fa-money-bill-wave"></i> Thanh toán trước
                    </button>
                    
                    <!-- Nút 2: Trả phòng & Thanh toán -->
                    <form asp-action="CheckoutThenPay" method="post" class="d-inline">
                        <input type="hidden" name="reservationFormID" value="@item.ReservationFormID" />
                        <button type="submit" class="btn btn-warning btn-sm"
                                onclick="return confirm('Xác nhận trả phòng?')">
                            <i class="fas fa-sign-out-alt"></i> Trả phòng & Thanh toán
                        </button>
                    </form>
                </div>
            }
            else if (!item.Invoice.IsPaid)
            {
                <!-- Đã checkout nhưng chưa thanh toán -->
                <a asp-action="Payment" asp-route-invoiceID="@item.Invoice.InvoiceID" 
                   class="btn btn-danger btn-sm">
                    <i class="fas fa-exclamation-triangle"></i> Chờ thanh toán
                </a>
            }
            else if (item.HistoryCheckOut == null)
            {
                <!-- Đã thanh toán nhưng chưa checkout -->
                <form asp-action="ActualCheckout" method="post" class="d-inline">
                    <input type="hidden" name="reservationFormID" value="@item.ReservationFormID" />
                    <button type="submit" class="btn btn-info btn-sm"
                            onclick="return confirm('Xác nhận trả phòng?')">
                        <i class="fas fa-door-open"></i> Trả phòng
                    </button>
                </form>
            }
            else
            {
                <span class="badge bg-success">Hoàn tất</span>
            }
        </td>
    </tr>
}
```

### CheckOut/Payment.cshtml - Trang xác nhận thanh toán

```razor
@model Invoice

<div class="card">
    <div class="card-header bg-warning text-white">
        <h4><i class="fas fa-money-bill"></i> Xác nhận thanh toán</h4>
    </div>
    <div class="card-body">
        <h5>Thông tin khách hàng</h5>
        <p><strong>Khách hàng:</strong> @Model.ReservationForm.Customer.FullName</p>
        <p><strong>Phòng:</strong> @Model.ReservationForm.RoomID</p>
        
        <hr />
        
        <h5>Chi tiết hóa đơn</h5>
        <table class="table">
            <tr>
                <td>Tiền phòng:</td>
                <td class="text-end">@Model.RoomCharge.ToString("N0") VNĐ</td>
            </tr>
            <tr>
                <td>Tiền dịch vụ:</td>
                <td class="text-end">@Model.ServicesCharge.ToString("N0") VNĐ</td>
            </tr>
            <tr>
                <td>Tiền cọc:</td>
                <td class="text-end">-@Model.RoomBookingDeposit.ToString("N0") VNĐ</td>
            </tr>
            <tr class="fw-bold">
                <td>Tổng cộng:</td>
                <td class="text-end text-danger">@Model.TotalAmount.ToString("N0") VNĐ</td>
            </tr>
        </table>
        
        <hr />
        
        <form asp-action="ConfirmPayment" method="post">
            <input type="hidden" name="invoiceID" value="@Model.InvoiceID" />
            
            <div class="mb-3">
                <label>Phương thức thanh toán:</label>
                <select name="paymentMethod" class="form-select" required>
                    <option value="CASH">Tiền mặt</option>
                    <option value="CARD">Thẻ</option>
                    <option value="TRANSFER">Chuyển khoản</option>
                </select>
            </div>
            
            <div class="d-grid gap-2">
                <button type="submit" class="btn btn-success btn-lg">
                    <i class="fas fa-check"></i> Xác nhận thanh toán @Model.TotalAmount.ToString("N0") VNĐ
                </button>
                <a asp-action="Index" class="btn btn-secondary">Quay lại</a>
            </div>
        </form>
    </div>
</div>
```

---

## 📋 Checklist Implementation

### ✅ Database Changes
- [ ] Chạy ALTER TABLE để thêm cột vào Invoice
- [ ] Chạy ALTER TABLE để thêm cột vào HistoryCheckOut
- [ ] Tạo stored procedures mới
- [ ] Test trigger với 2 trường hợp

### ✅ Backend Changes
- [ ] Tạo DatabaseExtensions methods cho SPs mới
- [ ] Cập nhật CheckOutController với 4 actions mới
- [ ] Cập nhật Invoice model với các property mới
- [ ] Test logic thanh toán

### ✅ Frontend Changes
- [ ] Cập nhật CheckOut/Index.cshtml với 2 nút
- [ ] Tạo CheckOut/Payment.cshtml
- [ ] Tạo modal chọn phương thức thanh toán
- [ ] Cập nhật CSS/JS

### ✅ Testing
- [ ] Test Option 1: Checkout → Payment → Done
- [ ] Test Option 2: Prepay → Stay → Checkout on time
- [ ] Test Option 2: Prepay → Stay → Late checkout với phụ phí
- [ ] Test Invoice trigger với cả 2 cases
- [ ] Test room status transitions

---

## 🎯 Kết quả mong đợi

| Luồng | Thời điểm tính tiền | Invoice.isPaid | Room.status | Ghi chú |
|-------|-------------------|----------------|-------------|---------|
| **Option 1: Checkout → Pay** | CheckIn→CheckOut Actual | False → True | UNAVAILABLE → AVAILABLE | Phòng khóa đến khi thanh toán |
| **Option 2: Pay → Checkout** | CheckIn→CheckOut Expected | True | ON_USE → AVAILABLE | Nếu checkout muộn → phụ phí |

✅ **Hoàn thành thiết kế!** Sẵn sàng triển khai.
