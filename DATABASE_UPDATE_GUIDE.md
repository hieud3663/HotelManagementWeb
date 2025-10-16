# Hướng dẫn Cập nhật Database cho Luồng Thanh toán Mới

## ⚠️ LƯU Ý QUAN TRỌNG

**PHẢI thực hiện theo thứ tự sau:**

---

## 📝 BƯỚC 1: Backup Database

```sql
-- Backup database trước khi thay đổi
BACKUP DATABASE HotelManagement
TO DISK = 'C:\Backup\HotelManagement_Backup_BeforePaymentUpdate.bak'
WITH FORMAT, MEDIANAME = 'SQLServerBackups', NAME = 'Full Backup of HotelManagement';
GO
```

---

## 📝 BƯỚC 2: Thêm các cột mới vào bảng Invoice

```sql
USE HotelManagement;
GO

-- Thêm các cột mới cho luồng thanh toán
ALTER TABLE Invoice
ADD isPaid BIT NOT NULL DEFAULT 0,              -- 0 = Chưa thanh toán, 1 = Đã thanh toán
    paymentDate DATETIME NULL,                  -- Ngày thanh toán thực tế
    paymentMethod NVARCHAR(20) NULL CHECK (paymentMethod IN ('CASH', 'CARD', 'TRANSFER', NULL)),
    checkoutType NVARCHAR(20) NULL CHECK (checkoutType IN ('CHECKOUT_THEN_PAY', 'PAY_THEN_CHECKOUT', NULL));
GO
```

**✅ Kiểm tra:**
```sql
SELECT TOP 1 * FROM Invoice;
-- Phải thấy các cột: isPaid, paymentDate, paymentMethod, checkoutType
```

---

## 📝 BƯỚC 3: Thêm cột invoiceID vào HistoryCheckOut

```sql
-- Thêm cột invoiceID vào HistoryCheckOut
ALTER TABLE HistoryCheckOut
ADD invoiceID NVARCHAR(15) NULL;
GO

-- Thêm foreign key constraint
ALTER TABLE HistoryCheckOut
ADD CONSTRAINT FK_HistoryCheckOut_Invoice 
    FOREIGN KEY (invoiceID) REFERENCES Invoice(invoiceID) 
    ON DELETE NO ACTION;
GO
```

**✅ Kiểm tra:**
```sql
SELECT TOP 1 * FROM HistoryCheckOut;
-- Phải thấy cột: invoiceID
```

---

## 📝 BƯỚC 4: Sửa Stored Procedure sp_CreateConfirmationReceipt

```sql
-- Fix lỗi "invoiceID was not present" bằng cách thêm ISNULL
CREATE OR ALTER PROCEDURE sp_CreateConfirmationReceipt
    @receiptType NVARCHAR(15), -- 'RESERVATION' hoặc 'CHECKIN'
    @reservationFormID NVARCHAR(15),
    @invoiceID NVARCHAR(15) = NULL,
    @employeeID NVARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- ... (phần code giữa giữ nguyên) ...
        
        -- Trả về thông tin phiếu (FIX LỖI Ở ĐÂY)
        SELECT 
            receiptID, receiptType, issueDate,
            customerName, 
            ISNULL(invoiceID, '') AS invoiceID,  -- ← FIX: Đảm bảo luôn có giá trị
            customerPhone, customerEmail,
            roomID, roomCategoryName,
            checkInDate, checkOutDate,
            priceUnit, unitPrice, deposit, totalAmount,
            employeeName, qrCode
        FROM ConfirmationReceipt
        WHERE receiptID = @receiptID;
        
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

**✅ Test:**
```sql
-- Test tạo phiếu xác nhận đặt phòng (không có invoiceID)
EXEC sp_CreateConfirmationReceipt 
    @receiptType = 'RESERVATION',
    @reservationFormID = 'RF-000001',
    @invoiceID = NULL,
    @employeeID = 'EMP-000001';
-- Không được báo lỗi "invoiceID was not present"
```

---

## 📝 BƯỚC 5: Tạo Stored Procedure Mới

### 5.1. sp_CreateInvoice_CheckoutThenPay

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
            RAISERROR(N'Phiếu đặt phòng chưa check-in', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã checkout chưa
        IF EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Phiếu đặt phòng đã checkout', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã có invoice chưa
        IF EXISTS (SELECT 1 FROM Invoice WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Hóa đơn đã tồn tại', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
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
        
        INSERT INTO Invoice (
            invoiceID, invoiceDate, roomCharge, servicesCharge, reservationFormID, 
            isPaid, checkoutType
        )
        VALUES (
            @invoiceID, GETDATE(), 0, 0, @reservationFormID, 
            0, 'CHECKOUT_THEN_PAY'
        );
        
        -- Cập nhật invoiceID vào HistoryCheckOut
        UPDATE HistoryCheckOut SET invoiceID = @invoiceID WHERE historyCheckOutID = @checkOutID;
        
        -- Cập nhật trạng thái phòng thành UNAVAILABLE (đợi thanh toán)
        UPDATE Room SET roomStatus = 'UNAVAILABLE' WHERE roomID = @roomID;
        
        -- Trả về thông tin invoice
        SELECT 
            inv.invoiceID,
            inv.roomCharge,
            inv.servicesCharge,
            inv.totalAmount,
            inv.isPaid,
            inv.checkoutType,
            @checkOutID AS checkOutID,
            'CHECKOUT_THEN_PAY' AS status
        FROM Invoice inv
        WHERE inv.invoiceID = @invoiceID;
        
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

### 5.2. sp_CreateInvoice_PayThenCheckout

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
            RAISERROR(N'Phiếu đặt phòng chưa check-in', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã có invoice chưa
        IF EXISTS (SELECT 1 FROM Invoice WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Hóa đơn đã tồn tại', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Tạo Invoice với checkOutDate = Expected (chưa checkout thực tế)
        DECLARE @invoiceID NVARCHAR(15) = dbo.fn_GenerateID('INV-', 'Invoice', 'invoiceID', 6);
        
        INSERT INTO Invoice (
            invoiceID, invoiceDate, roomCharge, servicesCharge, 
            reservationFormID, isPaid, paymentDate, paymentMethod, checkoutType
        )
        VALUES (
            @invoiceID, GETDATE(), 0, 0, 
            @reservationFormID, 1, GETDATE(), @paymentMethod, 'PAY_THEN_CHECKOUT'
        );
        
        -- Trả về thông tin invoice
        SELECT 
            inv.invoiceID,
            inv.roomCharge,
            inv.servicesCharge,
            inv.totalAmount,
            inv.isPaid,
            inv.paymentDate,
            inv.paymentMethod,
            inv.checkoutType,
            'PAY_THEN_CHECKOUT' AS status
        FROM Invoice inv
        WHERE inv.invoiceID = @invoiceID;
        
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

### 5.3. sp_ConfirmPayment

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

### 5.4. sp_ActualCheckout_AfterPrepayment

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
            RAISERROR(N'Phải thanh toán trước khi checkout', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
        END
        
        -- Kiểm tra đã checkout chưa
        IF EXISTS (SELECT 1 FROM HistoryCheckOut WHERE reservationFormID = @reservationFormID)
        BEGIN
            RAISERROR(N'Đã checkout rồi', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN -1;
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
        DECLARE @checkoutStatus NVARCHAR(20) = 'ON_TIME';
        
        IF @checkOutActual > @checkOutExpected
        BEGIN
            SET @checkoutStatus = 'LATE_CHECKOUT';
            
            -- Trigger sẽ tự động tính lại lateCheckoutFee
            UPDATE Invoice 
            SET invoiceDate = GETDATE()
            WHERE invoiceID = @invoiceID;
            
            SELECT @additionalCharge = ISNULL(lateCheckoutFee, 0)
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
            @checkoutStatus AS checkoutStatus,
            @invoiceID AS invoiceID;
        
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

---

## ✅ BƯỚC 6: Kiểm tra toàn bộ

```sql
-- 1. Kiểm tra cấu trúc bảng Invoice
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Invoice'
ORDER BY ORDINAL_POSITION;

-- 2. Kiểm tra stored procedures đã tạo
SELECT name, create_date, modify_date
FROM sys.procedures
WHERE name LIKE 'sp_%Payment%' OR name LIKE 'sp_%Checkout%'
ORDER BY name;

-- 3. Test tạo invoice checkout then pay
EXEC sp_CreateInvoice_CheckoutThenPay 
    @reservationFormID = 'RF-000001',
    @employeeID = 'EMP-000001';

-- 4. Test xác nhận thanh toán
EXEC sp_ConfirmPayment 
    @invoiceID = 'INV-XXXX',  -- Thay bằng invoice vừa tạo
    @paymentMethod = 'CASH',
    @employeeID = 'EMP-000001';
```

---

## 🎯 Kết quả

Sau khi hoàn thành, bạn có thể:

✅ Trả phòng rồi thanh toán (Checkout Then Pay)  
✅ Thanh toán trước rồi trả phòng (Pay Then Checkout)  
✅ Xác nhận thanh toán cho hóa đơn chưa thanh toán  
✅ Checkout sau khi đã thanh toán trước (với tính phụ phí nếu muộn)  
✅ Không còn lỗi "invoiceID was not present"  

---

## 📌 Next Steps

Tiếp theo cần:
1. Cập nhật **DatabaseExtensions.cs** để thêm các methods gọi SPs mới
2. Cập nhật **CheckOutController.cs** với 4 actions mới
3. Tạo **Views** cho Payment page và Checkout options
4. Test toàn bộ luồng

Xem chi tiết trong file `CHECKOUT_PAYMENT_REDESIGN.md`
