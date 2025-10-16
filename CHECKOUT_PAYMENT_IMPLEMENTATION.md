# 🎉 Hoàn thành Triển khai Luồng Thanh toán Mới cho Check-out

## 📅 Ngày hoàn thành: 16/10/2025

---

## ✅ TÓM TẮT

Đã **hoàn tất** việc triển khai 2 luồng thanh toán mới cho tính năng Check-out:

1. **CHECKOUT_THEN_PAY** (Trả phòng và Thanh toán)
2. **PAY_THEN_CHECKOUT** (Thanh toán trước)

---

## 🗂️ CẤU TRÚC THAY ĐỔI

### 1️⃣ **Database Layer** ✅

#### **Bảng Invoice - Thêm 4 cột mới:**
```sql
ALTER TABLE Invoice ADD
    isPaid BIT NOT NULL DEFAULT 0,              -- Trạng thái thanh toán
    paymentDate DATETIME NULL,                  -- Ngày thanh toán thực tế
    paymentMethod NVARCHAR(20) NULL,            -- CASH/CARD/TRANSFER
    checkoutType NVARCHAR(20) NULL;             -- CHECKOUT_THEN_PAY/PAY_THEN_CHECKOUT
```

#### **Bảng HistoryCheckOut - Thêm cột invoiceID:**
```sql
ALTER TABLE HistoryCheckOut ADD
    invoiceID NVARCHAR(15) NULL,
    CONSTRAINT FK_HistoryCheckOut_Invoice 
        FOREIGN KEY (invoiceID) REFERENCES Invoice(invoiceID);
```

#### **4 Stored Procedures mới:**
- ✅ `sp_CreateInvoice_CheckoutThenPay` - Tạo invoice chưa thanh toán, checkout ngay
- ✅ `sp_CreateInvoice_PayThenCheckout` - Tạo invoice đã thanh toán, checkout sau
- ✅ `sp_ConfirmPayment` - Xác nhận thanh toán, giải phóng phòng
- ✅ `sp_ActualCheckout_AfterPrepayment` - Checkout thực tế sau khi đã thanh toán trước

#### **Sửa lỗi SP cũ:**
- ✅ `sp_CreateConfirmationReceipt` - Fix lỗi "invoiceID was not present" bằng `ISNULL(invoiceID, '')`

---

### 2️⃣ **C# Models Layer** ✅

#### **Models/Invoice.cs - Cập nhật:**
```csharp
// 4 properties mới
public bool IsPaid { get; set; } = false;
public DateTime? PaymentDate { get; set; }
public string? PaymentMethod { get; set; }
public string? CheckoutType { get; set; }

// 2 computed properties để dễ dùng trong View
[NotMapped]
public decimal TaxAmount => TotalDue.HasValue ? TotalDue.Value * (TaxRate / 100) : 0;

[NotMapped]
public decimal Deposit => RoomBookingDeposit;
```

#### **Models/HistoryCheckOut.cs - Cập nhật:**
```csharp
// Thêm navigation property
[Column("invoiceID")]
[StringLength(15)]
public string? InvoiceID { get; set; }

[ForeignKey("InvoiceID")]
public virtual Invoice? Invoice { get; set; }
```

---

### 3️⃣ **Data Access Layer** ✅

#### **Data/DatabaseExtensions.cs - Thêm 5 Result Models:**

```csharp
public class CheckoutThenPayResult
{
    public string InvoiceID { get; set; }
    public decimal RoomCharge { get; set; }
    public decimal ServicesCharge { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public string CheckoutType { get; set; }
    public string CheckOutID { get; set; }
    public string Status { get; set; }
}

public class PayThenCheckoutResult
{
    public string InvoiceID { get; set; }
    public decimal RoomCharge { get; set; }
    public decimal ServicesCharge { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; }
    public string CheckoutType { get; set; }
    public string Status { get; set; }
}

public class ConfirmPaymentResult
{
    public string InvoiceID { get; set; }
    public bool IsPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
}

public class ActualCheckoutResult
{
    public string CheckOutID { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime CheckOutExpected { get; set; }
    public decimal AdditionalCharge { get; set; }
    public string CheckoutStatus { get; set; }
    public string InvoiceID { get; set; }
}
```

#### **4 Extension Methods mới:**
```csharp
Task<CheckoutThenPayResult?> CreateInvoice_CheckoutThenPay(
    string reservationFormID, string employeeID)

Task<PayThenCheckoutResult?> CreateInvoice_PayThenCheckout(
    string reservationFormID, string employeeID, string paymentMethod)

Task<ConfirmPaymentResult?> ConfirmPaymentSP(
    string invoiceID, string paymentMethod, string employeeID)

Task<ActualCheckoutResult?> ActualCheckout_AfterPrepayment(
    string reservationFormID, string employeeID)
```

---

### 4️⃣ **Controller Layer** ✅

#### **Controllers/CheckOutController.cs - 6 Actions mới:**

```csharp
// LUỒNG 1: CHECKOUT_THEN_PAY
[HttpPost] CheckoutThenPay(string reservationFormID)
    → Trả phòng ngay, tạo invoice chưa thanh toán
    → Redirect đến Payment page

[HttpGet] Payment(string invoiceID)
    → Hiển thị trang thanh toán
    → Show invoice details và form chọn phương thức

[HttpPost] ConfirmPayment(string invoiceID, string paymentMethod)
    → Xác nhận thanh toán
    → Giải phóng phòng (UNAVAILABLE → AVAILABLE)
    → Redirect đến Invoice Details

// LUỒNG 2: PAY_THEN_CHECKOUT
[HttpPost] PayThenCheckout(string reservationFormID, string paymentMethod)
    → Thanh toán trước ngay
    → Khách vẫn ở phòng (ON_USE)
    → Redirect đến Invoice Details

[HttpPost] ActualCheckout(string reservationFormID)
    → Checkout thực tế
    → Tính phí phụ thu nếu muộn
    → Redirect đến Payment nếu có phí phụ thu
```

#### **Index() - Cập nhật Include:**
```csharp
.Include(h => h.ReservationForm)
    .ThenInclude(r => r!.Invoices)
.Include(h => h.ReservationForm)
    .ThenInclude(r => r!.HistoryCheckOut)
```

---

### 5️⃣ **View Layer** ✅

#### **Views/CheckOut/Index.cshtml - Cập nhật Logic:**

**Trạng thái 1: Chưa có Invoice**
```razor
@if (invoice == null)
{
    <form asp-action="CheckoutThenPay">
        <button>Trả phòng</button>
    </form>
    <button data-bs-toggle="modal">Thanh toán trước</button>
    
    <!-- Modal chọn phương thức thanh toán -->
}
```

**Trạng thái 2: Có Invoice CHƯA thanh toán**
```razor
@else if (invoice.IsPaid == false)
{
    <a asp-action="Payment">Cần thanh toán</a>
    <span>@invoice.TotalAmount VNĐ</span>
}
```

**Trạng thái 3: Đã thanh toán NHƯNG CHƯA checkout**
```razor
@else if (invoice.IsPaid && !hasCheckout && 
          invoice.CheckoutType == "PAY_THEN_CHECKOUT")
{
    <form asp-action="ActualCheckout">
        <button>Trả phòng</button>
    </form>
    <span>Đã thanh toán</span>
}
```

**Trạng thái 4: Đã hoàn tất**
```razor
@else
{
    <span>Hoàn tất</span>
}
```

#### **Views/CheckOut/Payment.cshtml - Trang mới:**
```razor
@model HotelManagement.Models.Invoice

<!-- Hiển thị thông tin hóa đơn -->
<table>
    <tr><td>Tiền phòng</td><td>@Model.RoomCharge</td></tr>
    <tr><td>Tiền dịch vụ</td><td>@Model.ServicesCharge</td></tr>
    @if (Model.EarlyCheckinFee > 0)
    {
        <tr><td>Phí check-in sớm</td><td>@Model.EarlyCheckinFee</td></tr>
    }
    @if (Model.LateCheckoutFee > 0)
    {
        <tr><td>Phí checkout muộn</td><td>@Model.LateCheckoutFee</td></tr>
    }
    <tr><td>VAT</td><td>@Model.TaxAmount</td></tr>
    <tr><td>Tiền cọc</td><td>-@Model.Deposit</td></tr>
    <tr><th>TỔNG CỘNG</th><th>@Model.TotalAmount</th></tr>
</table>

<!-- Form thanh toán -->
<form asp-action="ConfirmPayment">
    <input type="hidden" name="invoiceID" value="@Model.InvoiceID" />
    
    <input type="radio" name="paymentMethod" value="CASH" checked />
    <input type="radio" name="paymentMethod" value="CARD" />
    <input type="radio" name="paymentMethod" value="TRANSFER" />
    
    <button type="submit">Xác nhận thanh toán</button>
</form>
```

---

## 🔄 LUỒNG HOẠT ĐỘNG

### **LUỒNG 1: CHECKOUT_THEN_PAY** (Trả phòng → Thanh toán)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Nhân viên nhấn "Trả phòng"                                   │
│    CheckOut/Index → Form CheckoutThenPay                        │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. POST CheckoutThenPay(reservationFormID)                      │
│    → sp_CreateInvoice_CheckoutThenPay                           │
│    → Tạo HistoryCheckOut với checkOutDate = NOW()               │
│    → Tạo Invoice (isPaid = 0, checkoutType = CHECKOUT_THEN_PAY) │
│    → Trigger tính tiền dựa trên checkOutActual (THỰC TẾ)        │
│    → Room status: ON_USE → UNAVAILABLE (khóa phòng)             │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. Redirect → Payment(invoiceID)                                │
│    → Hiển thị chi tiết hóa đơn                                  │
│    → Form chọn phương thức: CASH/CARD/TRANSFER                  │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. POST ConfirmPayment(invoiceID, paymentMethod)                │
│    → sp_ConfirmPayment                                          │
│    → Update Invoice: isPaid = 1, paymentDate = NOW()            │
│    → Room status: UNAVAILABLE → AVAILABLE (giải phóng phòng)    │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
                  ✅ HOÀN TẤT
```

---

### **LUỒNG 2: PAY_THEN_CHECKOUT** (Thanh toán → Trả phòng)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Nhân viên nhấn "Thanh toán trước"                            │
│    CheckOut/Index → Modal chọn phương thức thanh toán           │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. POST PayThenCheckout(reservationFormID, paymentMethod)       │
│    → sp_CreateInvoice_PayThenCheckout                           │
│    → Tạo Invoice (isPaid = 1, checkoutType = PAY_THEN_CHECKOUT) │
│    → Trigger tính tiền dựa trên checkOutExpected (DỰ KIẾN)      │
│    → Room status: ON_USE (khách vẫn ở)                          │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. Khách ở đến thời gian checkout dự kiến                       │
│    → Nếu đúng giờ: Nhấn "Trả phòng" → Hoàn tất                  │
│    → Nếu muộn: Nhấn "Trả phòng" → Tính phí phụ thu              │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. POST ActualCheckout(reservationFormID)                       │
│    → sp_ActualCheckout_AfterPrepayment                          │
│    → Tạo HistoryCheckOut với checkOutDate = NOW()               │
│    → So sánh NOW() với checkOutExpected                         │
│    → Nếu muộn: Tính phí phụ thu, cập nhật Invoice               │
│    → Room status: ON_USE → AVAILABLE (giải phóng phòng)         │
└────────────────────────┬────────────────────────────────────────┘
                         ↓
              ┌──────────┴──────────┐
              ↓                     ↓
      ✅ ĐÚNG GIỜ           ⚠️ TRẢ MUỘN
      (Hoàn tất)           (Redirect Payment
                            để thanh toán
                            phí phụ thu)
```

---

## 🛠️ CÔNG NGHỆ SỬ DỤNG

- **ASP.NET Core MVC 9.0**
- **SQL Server** với Stored Procedures & Triggers
- **Entity Framework Core** (FromSql/SqlQueryRaw)
- **Bootstrap 5** cho UI
- **Font Awesome** cho icons

---

## 📋 CHECKLIST HOÀN THÀNH

### Database ✅
- [x] ALTER TABLE Invoice (4 cột mới)
- [x] ALTER TABLE HistoryCheckOut (invoiceID + FK)
- [x] sp_CreateInvoice_CheckoutThenPay
- [x] sp_CreateInvoice_PayThenCheckout
- [x] sp_ConfirmPayment
- [x] sp_ActualCheckout_AfterPrepayment
- [x] Fix sp_CreateConfirmationReceipt (ISNULL lỗi)

### C# Models ✅
- [x] Invoice.cs - 4 properties mới
- [x] Invoice.cs - 2 computed properties (TaxAmount, Deposit)
- [x] HistoryCheckOut.cs - InvoiceID property

### Data Access Layer ✅
- [x] DatabaseExtensions.cs - 5 result models
- [x] DatabaseExtensions.cs - 4 extension methods

### Controller ✅
- [x] CheckOutController - CheckoutThenPay action
- [x] CheckOutController - Payment action (GET)
- [x] CheckOutController - ConfirmPayment action
- [x] CheckOutController - PayThenCheckout action
- [x] CheckOutController - ActualCheckout action
- [x] CheckOutController - Index cập nhật Include

### Views ✅
- [x] CheckOut/Index.cshtml - 4 trạng thái UI
- [x] CheckOut/Index.cshtml - Modal thanh toán trước
- [x] CheckOut/Payment.cshtml - Trang thanh toán mới

### Bug Fixes ✅
- [x] Fix decimal.ToString("N0") → ToString("N0", CultureInfo.InvariantCulture)
- [x] Fix nullable checks cho EarlyCheckinFee, LateCheckoutFee
- [x] Fix CheckOutDate → InvoiceDate trong Payment view

---

## 🧪 TESTING CHECKLIST

### Luồng 1: Checkout Then Pay
- [ ] Nhấn "Trả phòng" → Hiển thị trang Payment
- [ ] Trang Payment hiển thị đúng chi tiết hóa đơn
- [ ] Tiền phòng tính theo thời gian THỰC TẾ (actual checkout)
- [ ] Room status: ON_USE → UNAVAILABLE
- [ ] Xác nhận thanh toán CASH → Invoice isPaid = 1
- [ ] Xác nhận thanh toán CARD → Invoice isPaid = 1
- [ ] Xác nhận thanh toán TRANSFER → Invoice isPaid = 1
- [ ] Sau thanh toán: Room status UNAVAILABLE → AVAILABLE
- [ ] TempData success message hiển thị

### Luồng 2: Pay Then Checkout
- [ ] Nhấn "Thanh toán trước" → Mở modal
- [ ] Chọn CASH → Tạo invoice đã thanh toán
- [ ] Tiền phòng tính theo thời gian DỰ KIẾN (expected checkout)
- [ ] Invoice có checkoutType = PAY_THEN_CHECKOUT
- [ ] Room status vẫn ON_USE
- [ ] Nhấn "Trả phòng" ĐÚNG GIỜ → Không phí phụ thu
- [ ] Nhấn "Trả phòng" MUỘN → Redirect Payment với phí phụ thu
- [ ] Sau trả phòng: Room status ON_USE → AVAILABLE

### Edge Cases
- [ ] Checkout phòng chưa check-in → Báo lỗi
- [ ] Checkout phòng đã checkout → Báo lỗi
- [ ] Thanh toán invoice đã thanh toán → Báo lỗi
- [ ] Thanh toán invoice không tồn tại → Báo lỗi
- [ ] Checkout muộn với phí cao → Tính đúng phí phụ thu

---

## 📚 TÀI LIỆU THAM KHẢO

- [CHECKOUT_PAYMENT_REDESIGN.md](./CHECKOUT_PAYMENT_REDESIGN.md) - Thiết kế tổng thể
- [DATABASE_UPDATE_GUIDE.md](./DATABASE_UPDATE_GUIDE.md) - Hướng dẫn cập nhật database
- [PRICING_LOGIC_FINAL.md](./PRICING_LOGIC_FINAL.md) - Logic tính giá

---

## 🎯 NEXT STEPS (Tùy chọn)

### Tính năng bổ sung:
1. **Email hóa đơn** - Gửi invoice qua email cho khách
2. **Print invoice** - In hóa đơn PDF
3. **Payment history** - Lịch sử thanh toán
4. **Refund** - Hoàn tiền nếu khách hủy
5. **Partial payment** - Thanh toán từng phần
6. **QR Code payment** - Thanh toán qua QR

### Cải tiến UI/UX:
1. **Real-time countdown** - Đếm ngược đến giờ checkout dự kiến
2. **Payment receipt** - Biên lai thanh toán
3. **Invoice preview** - Xem trước hóa đơn trước khi checkout
4. **SMS notification** - Thông báo SMS cho khách

---

## ✅ KẾT LUẬN

**Đã hoàn thành 100%** việc triển khai 2 luồng thanh toán mới:
- ✅ Database schema updated
- ✅ Stored procedures created
- ✅ C# models updated
- ✅ Data access layer implemented
- ✅ Controller actions created
- ✅ Views designed and implemented
- ✅ All compile errors fixed

**Hệ thống đã sẵn sàng để test!** 🚀

---

**Người thực hiện:** GitHub Copilot  
**Ngày hoàn thành:** 16/10/2025  
**Phiên bản:** 1.0
