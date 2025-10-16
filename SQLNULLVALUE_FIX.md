# 🐛 Fix Lỗi SqlNullValueException trong sp_ConfirmPayment

## ❌ Lỗi gặp phải:

```
System.Data.SqlTypes.SqlNullValueException: Data is Null. 
This method or property cannot be called on Null values.
at Microsoft.Data.SqlClient.SqlDataReader.GetDateTime(Int32 i)
```

**Khi thực thi:** `EXEC sp_ConfirmPayment @invoiceID, @paymentMethod, @employeeID`

---

## 🔍 Nguyên nhân:

### 1. C# Model không khớp với Database
**C# Model - ConfirmPaymentResult:**
```csharp
public class ConfirmPaymentResult
{
    public DateTime PaymentDate { get; set; } // ❌ KHÔNG NULLABLE
}
```

**SQL Server - Invoice table:**
```sql
paymentDate DATETIME NULL  -- ✅ NULLABLE
```

### 2. Stored Procedure SELECT giá trị cũ
**sp_ConfirmPayment trước khi sửa:**
```sql
-- UPDATE paymentDate
UPDATE Invoice
SET isPaid = 1,
    paymentDate = GETDATE(),  -- Set giá trị mới
    paymentMethod = @paymentMethod
WHERE invoiceID = @invoiceID;

-- SELECT lại từ bảng (có thể lấy giá trị cũ do transaction isolation)
SELECT 
    invoiceID,
    isPaid,
    paymentDate,  -- ← Có thể NULL nếu transaction chưa commit
    paymentMethod,
    totalAmount
FROM Invoice
WHERE invoiceID = @invoiceID;
```

**Vấn đề:**
- Transaction isolation level có thể khiến SELECT đọc giá trị cũ (NULL)
- EF Core cố gắng map NULL → `DateTime` (không nullable) → Exception

---

## ✅ GIẢI PHÁP:

### Fix 1: Sửa C# Model thành Nullable

**File:** `Data/DatabaseExtensions.cs`

```csharp
public class ConfirmPaymentResult
{
    public string InvoiceID { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public DateTime? PaymentDate { get; set; } // ✅ Nullable
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

### Fix 2: Sửa Stored Procedure

**File:** `docs/database/HotelManagement_new.sql`

**Trước:**
```sql
-- Trả về thông tin
SELECT 
    invoiceID,
    isPaid,
    paymentDate,
    paymentMethod,
    totalAmount,
    'PAYMENT_CONFIRMED' AS status
FROM Invoice
WHERE invoiceID = @invoiceID;
```

**Sau:**
```sql
-- Trả về thông tin (sử dụng biến để đảm bảo giá trị đúng)
DECLARE @currentDate DATETIME = GETDATE();
DECLARE @totalAmt DECIMAL(18,2);

SELECT @totalAmt = totalAmount FROM Invoice WHERE invoiceID = @invoiceID;

SELECT 
    @invoiceID AS invoiceID,
    CAST(1 AS BIT) AS isPaid,
    @currentDate AS paymentDate,       -- ✅ Luôn có giá trị
    @paymentMethod AS paymentMethod,
    @totalAmt AS totalAmount,
    'PAYMENT_CONFIRMED' AS status;
```

---

## 🔧 Các bước thực hiện:

### Bước 1: Cập nhật C# Model

```bash
# File: Data/DatabaseExtensions.cs
# Dòng 123: Thêm ? sau DateTime
public DateTime? PaymentDate { get; set; }
```

### Bước 2: Cập nhật Stored Procedure

```sql
-- Chạy trong SQL Server Management Studio hoặc Azure Data Studio
USE HotelManagement;
GO

-- Drop procedure cũ (nếu cần)
DROP PROCEDURE IF EXISTS sp_ConfirmPayment;
GO

-- Tạo lại với fix mới
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
            RAISERROR(N'Không tìm thấy hóa đơn', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã thanh toán chưa
        DECLARE @isPaid BIT;
        SELECT @isPaid = isPaid FROM Invoice WHERE invoiceID = @invoiceID;
        
        IF @isPaid = 1
        BEGIN
            RAISERROR(N'Hóa đơn đã được thanh toán', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
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
        
        -- Trả về thông tin (sử dụng biến để đảm bảo giá trị đúng)
        DECLARE @currentDate DATETIME = GETDATE();
        DECLARE @totalAmt DECIMAL(18,2);
        
        SELECT @totalAmt = totalAmount FROM Invoice WHERE invoiceID = @invoiceID;
        
        SELECT 
            @invoiceID AS invoiceID,
            CAST(1 AS BIT) AS isPaid,
            @currentDate AS paymentDate,
            @paymentMethod AS paymentMethod,
            @totalAmt AS totalAmount,
            'PAYMENT_CONFIRMED' AS status;
        
        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1;
    END CATCH
END;
GO
```

### Bước 3: Rebuild Project

```powershell
# Trong terminal
dotnet build
```

### Bước 4: Test

```sql
-- Test SP
DECLARE @testInvoiceID NVARCHAR(15) = 'INV-000001';

EXEC sp_ConfirmPayment 
    @invoiceID = @testInvoiceID,
    @paymentMethod = 'CASH',
    @employeeID = 'EMP-000001';
```

---

## 🧪 Kiểm tra kết quả:

### Test Case 1: Thanh toán hóa đơn chưa thanh toán
```
Input: Invoice với isPaid = 0
Expected: 
  - isPaid = 1
  - paymentDate = GETDATE()
  - paymentMethod = 'CASH'
  - Room status = 'AVAILABLE'
  - Không có exception
```

### Test Case 2: Thanh toán hóa đơn đã thanh toán
```
Input: Invoice với isPaid = 1
Expected: 
  - Lỗi: "Hóa đơn đã được thanh toán"
  - ROLLBACK transaction
  - Không thay đổi dữ liệu
```

### Test Case 3: Invoice không tồn tại
```
Input: invoiceID không hợp lệ
Expected:
  - Lỗi: "Không tìm thấy hóa đơn"
  - ROLLBACK transaction
```

---

## 📚 Bài học:

### 1. Luôn dùng Nullable cho DateTime từ Database
```csharp
// ❌ SAI
public DateTime PaymentDate { get; set; }

// ✅ ĐÚNG
public DateTime? PaymentDate { get; set; }
```

### 2. SP nên trả về giá trị từ biến thay vì SELECT từ bảng
```sql
-- ❌ SAI: SELECT từ bảng có thể đọc dirty read
SELECT paymentDate FROM Invoice WHERE ...

-- ✅ ĐÚNG: Dùng biến đảm bảo giá trị chính xác
DECLARE @currentDate DATETIME = GETDATE();
SELECT @currentDate AS paymentDate;
```

### 3. Luôn kiểm tra Nullable trước khi dùng
```csharp
// ❌ SAI
var date = result.PaymentDate.ToString("dd/MM/yyyy");

// ✅ ĐÚNG
var date = result.PaymentDate?.ToString("dd/MM/yyyy") ?? "Chưa thanh toán";
```

---

## ✅ Kết quả sau khi fix:

- ✅ Không còn SqlNullValueException
- ✅ ConfirmPayment hoạt động đúng
- ✅ PaymentDate luôn có giá trị khi thanh toán thành công
- ✅ C# Model khớp với Database schema
- ✅ SP trả về kết quả chính xác

---

## 🔗 Files liên quan:

- `Data/DatabaseExtensions.cs` - Line 123
- `docs/database/HotelManagement_new.sql` - Line 2504-2570
- `Controllers/CheckOutController.cs` - ConfirmPayment action

---

**Ngày fix:** 16/10/2025  
**Phiên bản:** 1.0  
**Severity:** HIGH (Application crash)
