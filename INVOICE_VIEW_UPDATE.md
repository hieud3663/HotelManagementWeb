# CẬP NHẬT VIEWS INVOICE CHO LUỒNG THANH TOÁN MỚI

**Ngày cập nhật:** 2024
**Tác giả:** AI Assistant
**Mục đích:** Cập nhật views Invoice để hiển thị đầy đủ thông tin về 2 luồng thanh toán mới

---

## 📋 TÓM TẮT

Cập nhật 2 views của Invoice để hiển thị:
1. **Trạng thái thanh toán** (Đã thanh toán / Chưa thanh toán)
2. **Loại giao dịch** (CHECKOUT_THEN_PAY / PAY_THEN_CHECKOUT)
3. **Thông tin thanh toán** (Ngày, phương thức)
4. **Trạng thái checkout** (Đã checkout / Chưa checkout)

---

## 🎨 THAY ĐỔI VIEWS

### **1. Views/Invoice/Invoice.cshtml** (Chi tiết hóa đơn)

#### **A. Phần Header - Hiển thị trạng thái thanh toán**

**THÊM MỚI:**
```razor
@* Hiển thị trạng thái thanh toán *@
@if (Model.IsPaid)
{
    <p style="margin: 10px 0 0 0;">
        <span style="background-color: #27ae60; color: white; padding: 5px 15px; border-radius: 20px;">
            <i class="fas fa-check-circle"></i> ĐÃ THANH TOÁN
        </span>
    </p>
    @if (Model.PaymentDate.HasValue)
    {
        <p style="color: #7f8c8d; margin: 5px 0 0 0; font-size: 13px;">
            Thanh toán lúc: <strong>@Model.PaymentDate.Value.ToString("dd/MM/yyyy HH:mm")</strong>
        </p>
    }
    @if (!string.IsNullOrEmpty(Model.PaymentMethod))
    {
        <p style="color: #7f8c8d; margin: 5px 0 0 0; font-size: 13px;">
            Phương thức: <strong>@(Model.PaymentMethod == "CASH" ? "Tiền mặt" : ...)</strong>
        </p>
    }
}
else
{
    <p style="margin: 10px 0 0 0;">
        <span style="background-color: #e74c3c; color: white; padding: 5px 15px; border-radius: 20px;">
            <i class="fas fa-exclamation-circle"></i> CHƯA THANH TOÁN
        </span>
    </p>
}
```

**Kết quả:**
- ✅ Badge màu xanh: "ĐÃ THANH TOÁN" + Ngày + Phương thức
- ❌ Badge màu đỏ: "CHƯA THANH TOÁN"

---

#### **B. Phần Header - Hiển thị loại giao dịch**

**THÊM MỚI:**
```razor
@* Hiển thị loại checkout *@
@if (!string.IsNullOrEmpty(Model.CheckoutType))
{
    <p style="color: #7f8c8d; margin: 10px 0 0 0; font-size: 13px;">
        Loại giao dịch: 
        <strong>
            @(Model.CheckoutType == "CHECKOUT_THEN_PAY" 
                ? "Trả phòng rồi thanh toán" 
                : "Thanh toán trước")
        </strong>
    </p>
}
```

**Kết quả:**
- "Trả phòng rồi thanh toán" → CHECKOUT_THEN_PAY
- "Thanh toán trước" → PAY_THEN_CHECKOUT

---

#### **C. Phần Thông tin phòng - Hiển thị checkout expected/actual**

**SỬA ĐỔI:**
```razor
<tr>
    <td style="padding: 5px 0; color: #7f8c8d;">Ngày trả phòng:</td>
    <td style="padding: 5px 0;">
        @if (checkOut != null)
        {
            <strong>@checkOut.CheckOutDate.ToString("dd/MM/yyyy HH:mm")</strong>
            <br />
            <small style="color: #7f8c8d;">(Thực tế)</small>
        }
        else if (reservation != null)
        {
            <span style="color: #e67e22;">@reservation.CheckOutDate.ToString("dd/MM/yyyy HH:mm")</span>
            <br />
            <small style="color: #e67e22;">(Dự kiến - chưa checkout)</small>
        }
    </td>
</tr>
@if (Model.CheckoutType == "PAY_THEN_CHECKOUT" && checkOut == null)
{
    <tr>
        <td colspan="2" style="padding: 10px 0; color: #3498db; font-size: 12px;">
            <i class="fas fa-info-circle"></i> Đã thanh toán trước, khách vẫn đang ở phòng
        </td>
    </tr>
}
```

**Kết quả:**
- Nếu đã checkout → Hiển thị ngày THỰC TẾ (màu đen)
- Nếu chưa checkout → Hiển thị ngày DỰ KIẾN (màu cam) + ghi chú "chưa checkout"
- Nếu PAY_THEN_CHECKOUT + chưa checkout → Thêm dòng thông báo xanh

---

#### **D. Phần Payment Summary - Hiển thị trạng thái hoàn tất**

**THÊM MỚI:**
```razor
@* Nếu đã thanh toán, hiển thị số tiền đã thanh toán *@
@if (Model.IsPaid && Model.TotalAmount.HasValue)
{
    <tr style="background-color: #d4edda; border-top: 2px solid #27ae60;">
        <td style="padding: 12px 0; font-size: 16px;">
            <strong style="color: #27ae60;">Đã thanh toán:</strong>
        </td>
        <td style="padding: 12px 0; text-align: right; font-size: 20px; color: #27ae60;">
            <strong>@Math.Round(Model.TotalAmount.Value, 0).ToString("N0") đ</strong>
        </td>
    </tr>
    <tr style="background-color: #d4edda;">
        <td style="padding: 8px 0; font-size: 14px;">
            <strong>Trạng thái:</strong>
        </td>
        <td style="padding: 8px 0; text-align: right; color: #27ae60;">
            <strong>✓ ĐÃ HOÀN TẤT</strong>
        </td>
    </tr>
}
```

**Kết quả:**
- Nếu đã thanh toán → 2 dòng xanh lá với số tiền + trạng thái "ĐÃ HOÀN TẤT"

---

### **2. Views/Invoice/Index.cshtml** (Danh sách hóa đơn)

#### **A. Thêm cột "Trạng thái" (Loại giao dịch)**

**THÊM MỚI:**
```razor
<th><i class="fas fa-info-circle"></i> Trạng thái</th>
```

**Trong tbody:**
```razor
<td>
    @if (!string.IsNullOrEmpty(item.CheckoutType))
    {
        @if (item.CheckoutType == "CHECKOUT_THEN_PAY")
        {
            <span class="badge-modern badge-warning-modern" title="Trả phòng rồi thanh toán">
                <i class="fas fa-sign-out-alt"></i> Checkout → Pay
            </span>
        }
        else
        {
            <span class="badge-modern badge-info-modern" title="Thanh toán trước">
                <i class="fas fa-money-bill-wave"></i> Pay → Checkout
            </span>
        }
    }
    else
    {
        <span class="badge-modern badge-secondary-modern">
            <i class="fas fa-file-invoice"></i> Legacy
        </span>
    }
</td>
```

**Kết quả:**
- Badge vàng: "Checkout → Pay" (CHECKOUT_THEN_PAY)
- Badge xanh dương: "Pay → Checkout" (PAY_THEN_CHECKOUT)
- Badge xám: "Legacy" (hóa đơn cũ không có checkoutType)

---

#### **B. Thêm cột "Thanh toán" (Trạng thái thanh toán)**

**THÊM MỚI:**
```razor
<th><i class="fas fa-credit-card"></i> Thanh toán</th>
```

**Trong tbody:**
```razor
<td>
    @if (item.IsPaid)
    {
        <span class="badge-modern badge-success-modern">
            <i class="fas fa-check-circle"></i> Đã thanh toán
        </span>
        @if (item.PaymentDate.HasValue)
        {
            <br /><small class="text-muted">@item.PaymentDate.Value.ToString("dd/MM HH:mm")</small>
        }
    }
    else
    {
        <span class="badge-modern badge-danger-modern">
            <i class="fas fa-exclamation-circle"></i> Chưa thanh toán
        </span>
    }
</td>
```

**Kết quả:**
- Badge xanh: "Đã thanh toán" + ngày giờ
- Badge đỏ: "Chưa thanh toán"

---

#### **C. Sửa cột "Tổng tiền" để dùng TotalAmount**

**SỬA ĐỔI:**
```razor
<!-- TRƯỚC -->
<strong class="text-success">@(item.NetDue?.ToString("N0") ?? "0") đ</strong>

<!-- SAU -->
<strong class="text-success">@((item.TotalAmount ?? item.NetDue ?? 0).ToString("N0")) đ</strong>
```

**Lý do:** `TotalAmount` là số tiền thực tế phải trả sau khi trừ deposit

---

## 📊 SO SÁNH TRƯỚC/SAU

### **Tình huống 1: CHECKOUT_THEN_PAY - Chưa thanh toán**

| Field | Hiển thị (TRƯỚC) | Hiển thị (SAU) |
|-------|-----------------|---------------|
| **Header** | Mã HĐ + Ngày | + Badge đỏ "CHƯA THANH TOÁN" ✅ |
| **Loại GD** | - | "Trả phòng rồi thanh toán" ✅ |
| **Ngày checkout** | checkOut.CheckOutDate | + "(Thực tế)" ✅ |
| **Trạng thái** | - | Badge vàng "Checkout → Pay" ✅ |
| **Payment Summary** | Tổng tiền | (Không thay đổi) |

### **Tình huống 2: CHECKOUT_THEN_PAY - Đã thanh toán**

| Field | Hiển thị (TRƯỚC) | Hiển thị (SAU) |
|-------|-----------------|---------------|
| **Header** | Mã HĐ + Ngày | + Badge xanh "ĐÃ THANH TOÁN" ✅ |
| | | + Ngày thanh toán ✅ |
| | | + Phương thức thanh toán ✅ |
| **Payment Summary** | Tổng tiền | + "Đã thanh toán: XXX đ" (xanh) ✅ |
| | | + "Trạng thái: ✓ ĐÃ HOÀN TẤT" ✅ |

### **Tình huống 3: PAY_THEN_CHECKOUT - Đã thanh toán, chưa checkout**

| Field | Hiển thị (TRƯỚC) | Hiển thị (SAU) |
|-------|-----------------|---------------|
| **Header** | Mã HĐ + Ngày | + Badge xanh "ĐÃ THANH TOÁN" ✅ |
| **Loại GD** | - | "Thanh toán trước" ✅ |
| **Ngày checkout** | NULL hoặc Expected | Expected (cam) + "(Dự kiến - chưa checkout)" ✅ |
| | | + "Đã thanh toán trước, khách vẫn đang ở phòng" (xanh) ✅ |
| **Trạng thái** | - | Badge xanh "Pay → Checkout" ✅ |

### **Tình huống 4: PAY_THEN_CHECKOUT - Đã thanh toán, đã checkout**

| Field | Hiển thị (TRƯỚC) | Hiển thị (SAU) |
|-------|-----------------|---------------|
| **Ngày checkout** | checkOut.CheckOutDate | + "(Thực tế)" ✅ |
| **Payment Summary** | Tổng tiền | Nếu checkout muộn: + Phí muộn ✅ |

---

## 🎨 MÀUSẮC VÀ BADGES

### **Trạng thái thanh toán:**
- 🟢 **Đã thanh toán**: `#27ae60` (Xanh lá)
- 🔴 **Chưa thanh toán**: `#e74c3c` (Đỏ)

### **Loại giao dịch:**
- 🟡 **CHECKOUT_THEN_PAY**: `badge-warning-modern` (Vàng)
- 🔵 **PAY_THEN_CHECKOUT**: `badge-info-modern` (Xanh dương)
- ⚪ **Legacy**: `badge-secondary-modern` (Xám)

### **Ngày checkout:**
- ⚫ **Thực tế**: Màu đen (default)
- 🟠 **Dự kiến**: `#e67e22` (Cam)
- 🔵 **Ghi chú**: `#3498db` (Xanh dương)

---

## ✅ HOÀN THÀNH

### **Files đã cập nhật:**
- ✅ `Views/Invoice/Invoice.cshtml` (Chi tiết hóa đơn)
- ✅ `Views/Invoice/Index.cshtml` (Danh sách hóa đơn)

### **Thông tin hiển thị mới:**
1. ✅ Trạng thái thanh toán (IsPaid)
2. ✅ Ngày thanh toán (PaymentDate)
3. ✅ Phương thức thanh toán (PaymentMethod)
4. ✅ Loại giao dịch (CheckoutType)
5. ✅ Ngày checkout (Thực tế / Dự kiến)
6. ✅ Ghi chú đặc biệt cho PAY_THEN_CHECKOUT

### **Lợi ích:**
- ✅ Người dùng nhìn thấy rõ ràng trạng thái thanh toán
- ✅ Phân biệt được 2 luồng thanh toán
- ✅ Biết được khách đã checkout chưa
- ✅ Tương thích ngược với hóa đơn cũ (Legacy)

---

## 🔗 LIÊN QUAN

- **Documentation:**
  - `CHECKOUT_PAYMENT_REDESIGN.md` - Thiết kế tổng thể
  - `CHECKOUT_PAYMENT_IMPLEMENTATION.md` - Chi tiết triển khai
  - `INVOICE_TRIGGERS_UPDATE.md` - Cập nhật triggers

- **Files:**
  - `Views/Invoice/Invoice.cshtml`
  - `Views/Invoice/Index.cshtml`
  - `Models/Invoice.cs`
  - `Controllers/InvoiceController.cs`

---

**Hoàn thành cập nhật views Invoice!** 🎉
