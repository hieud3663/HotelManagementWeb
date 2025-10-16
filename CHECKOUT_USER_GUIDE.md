# 🚀 Hướng dẫn Sử dụng Tính năng Thanh toán Mới

## 📋 Tổng quan

Hệ thống checkout hiện hỗ trợ **2 luồng thanh toán** linh hoạt:

1. **Trả phòng và Thanh toán** - Checkout trước, thanh toán sau
2. **Thanh toán trước** - Thanh toán trước, checkout sau

---

## 🎯 LUỒNG 1: TRẢ PHÒNG VÀ THANH TOÁN

### Khi nào dùng?
- Khách muốn trả phòng ngay lập tức
- Tính tiền dựa trên thời gian **THỰC TẾ** đã ở

### Các bước thực hiện:

#### Bước 1: Truy cập trang Check-out
```
Menu → Check-out
```

#### Bước 2: Nhấn nút "Trả phòng"
- Tìm phòng cần checkout trong danh sách
- Nhấn nút **"Trả phòng"** (màu vàng)
- Xác nhận thông báo:
  ```
  "Trả phòng và thanh toán sau?
   Tiền phòng sẽ tính theo THỜI GIAN THỰC TẾ check-in → checkout."
  ```

#### Bước 3: Hệ thống xử lý
- ✅ Tạo hóa đơn (chưa thanh toán)
- ✅ Tính tiền dựa trên:
  - Thời gian check-in **thực tế**
  - Thời gian checkout **hiện tại**
  - Phí check-in sớm (nếu có)
  - Phí checkout muộn (nếu có)
- ✅ Khóa phòng (trạng thái: UNAVAILABLE)
- ✅ Chuyển đến trang thanh toán

#### Bước 4: Thanh toán
Trên trang thanh toán:
1. Kiểm tra chi tiết hóa đơn:
   - Tiền phòng
   - Tiền dịch vụ
   - Phí phụ thu (nếu có)
   - VAT 10%
   - Trừ tiền cọc
   - **TỔNG CỘNG**

2. Chọn phương thức thanh toán:
   - ☑️ Tiền mặt (CASH)
   - ☑️ Thẻ (CARD)
   - ☑️ Chuyển khoản (TRANSFER)

3. Nhấn **"Xác nhận thanh toán"**

#### Bước 5: Hoàn tất
- ✅ Hóa đơn được đánh dấu "Đã thanh toán"
- ✅ Phòng được giải phóng (trạng thái: AVAILABLE)
- ✅ Có thể xem/in hóa đơn

---

## 💰 LUỒNG 2: THANH TOÁN TRƯỚC

### Khi nào dùng?
- Khách muốn thanh toán trước và ở đến giờ dự kiến
- Tính tiền dựa trên thời gian **DỰ KIẾN** (theo đặt phòng)

### Các bước thực hiện:

#### Bước 1: Truy cập trang Check-out
```
Menu → Check-out
```

#### Bước 2: Nhấn nút "Thanh toán trước"
- Tìm phòng cần thanh toán trong danh sách
- Nhấn nút **"Thanh toán trước"** (màu xanh lá)
- Modal hiện lên với thông báo:
  ```
  "Lưu ý: Tiền phòng sẽ tính từ giờ check-in thực tế 
   đến giờ checkout DỰ KIẾN.
   Khách có thể ở đến [Ngày giờ dự kiến].
   Nếu trả phòng muộn sẽ tính phí phụ thu."
  ```

#### Bước 3: Chọn phương thức thanh toán
Trong modal:
1. Chọn phương thức:
   - ☑️ Tiền mặt
   - ☑️ Thẻ
   - ☑️ Chuyển khoản

2. Nhấn **"Xác nhận thanh toán"**

#### Bước 4: Hệ thống xử lý
- ✅ Tạo hóa đơn (đã thanh toán)
- ✅ Tính tiền dựa trên:
  - Thời gian check-in **thực tế**
  - Thời gian checkout **DỰ KIẾN** (theo đặt phòng)
  - Phí check-in sớm (nếu có)
- ✅ Phòng vẫn **ON_USE** (khách tiếp tục ở)
- ✅ Hiển thị hóa đơn đã thanh toán

#### Bước 5: Khách trả phòng thực tế

##### TH1: Trả phòng ĐÚNG GIỜ hoặc SỚM HƠN dự kiến
1. Nhấn nút **"Trả phòng"**
2. Xác nhận
3. ✅ Hoàn tất - Không phí phụ thu
4. ✅ Phòng được giải phóng (AVAILABLE)

##### TH2: Trả phòng MUỘN HƠN dự kiến
1. Nhấn nút **"Trả phòng"**
2. Hệ thống tính phí phụ thu:
   - So sánh thời gian thực tế vs dự kiến
   - Tính phí theo khung giờ checkout muộn
3. Chuyển đến trang thanh toán **PHÍ PHỤ THU**
4. Thanh toán phí bổ sung
5. ✅ Hoàn tất
6. ✅ Phòng được giải phóng (AVAILABLE)

---

## 📊 BẢNG SO SÁNH 2 LUỒNG

| Tiêu chí | Trả phòng → Thanh toán | Thanh toán → Trả phòng |
|----------|------------------------|------------------------|
| **Thời điểm thanh toán** | Sau khi checkout | Trước khi checkout |
| **Tính tiền dựa trên** | Thời gian THỰC TẾ | Thời gian DỰ KIẾN |
| **Trạng thái phòng sau bước 1** | UNAVAILABLE (khóa) | ON_USE (khách vẫn ở) |
| **Phí checkout muộn** | Tính ngay vào hóa đơn | Tính riêng nếu trả muộn |
| **Phù hợp với** | Khách checkout ngay | Khách ở đúng giờ dự kiến |

---

## 🔔 LƯU Ý QUAN TRỌNG

### Về tính giá:
1. **Phí check-in sớm:**
   - Miễn phí: 30 phút (giá GIỜ), 60 phút (giá NGÀY)
   - Vượt quá: Tính theo khung giờ

2. **Phí checkout muộn:**
   - Miễn phí: 30 phút (giá GIỜ), 60 phút (giá NGÀY)
   - Vượt quá: Tính theo khung giờ
   - Khung giờ checkout muộn:
     - 12h-15h: +30% giá ngày
     - 15h-18h: +50% giá ngày
     - Sau 18h: +100% giá ngày

3. **VAT:**
   - Luôn tính 10% trên tổng tiền (phòng + dịch vụ + phí)

4. **Tiền cọc:**
   - Được trừ vào tổng hóa đơn cuối cùng

### Về trạng thái phòng:
- **ON_USE**: Khách đang ở
- **UNAVAILABLE**: Đã checkout, chờ thanh toán
- **AVAILABLE**: Sẵn sàng cho khách mới

### Về hóa đơn:
- Mỗi reservation chỉ có **1 hóa đơn chính**
- Nếu trả phòng muộn sau khi đã thanh toán trước, hóa đơn sẽ được **cập nhật** với phí phụ thu

---

## ❓ CÂU HỎI THƯỜNG GẶP

### Q: Khách đã thanh toán trước nhưng muốn trả phòng sớm?
**A:** Không sao! Khách vẫn có thể checkout sớm hơn dự kiến mà không mất phí. Hệ thống chỉ tính thêm phí khi trả **MUỘN**.

### Q: Khách đã "Trả phòng" nhưng chưa thanh toán, có thể hủy không?
**A:** Không! Sau khi nhấn "Trả phòng", phòng đã bị khóa (UNAVAILABLE). Khách **BẮT BUỘC** phải thanh toán để giải phóng phòng.

### Q: Nếu khách thanh toán trước nhưng không checkout?
**A:** Hóa đơn vẫn hợp lệ. Hệ thống không tự động checkout. Nhân viên cần liên hệ khách và thực hiện checkout thủ công.

### Q: Có thể in hóa đơn không?
**A:** Có! Sau khi thanh toán xong, truy cập:
```
Menu → Hóa đơn → Tìm hóa đơn → Xem chi tiết → In
```

### Q: Làm sao biết khách đã thanh toán chưa?
**A:** Trong danh sách checkout, badge sẽ hiển thị:
- 🔴 **"Cần thanh toán"** - Chưa thanh toán
- 🟢 **"Đã thanh toán"** - Đã thanh toán
- ✅ **"Hoàn tất"** - Đã thanh toán và checkout

---

## 🎓 VÍ DỤ THỰC TẾ

### Ví dụ 1: Khách ở đúng giờ, thanh toán trước

```
Đặt phòng: 15/10 14:00 → 16/10 12:00 (Giá: 500,000 VNĐ/ngày)
Check-in thực tế: 15/10 14:15 (muộn 15 phút - miễn phí)

→ Nhân viên nhấn "Thanh toán trước"
→ Chọn phương thức: Tiền mặt
→ Hóa đơn:
   - Tiền phòng: 500,000 VNĐ (tính từ 14:15 → 12:00)
   - VAT 10%: 50,000 VNĐ
   - Tiền cọc: -100,000 VNĐ
   - TỔNG: 450,000 VNĐ
→ Khách thanh toán, lấy hóa đơn
→ Khách ở đến 16/10 11:50 → Checkout
→ Nhân viên nhấn "Trả phòng"
→ Hoàn tất! Không phí phụ thu
```

### Ví dụ 2: Khách trả phòng muộn, thanh toán sau

```
Đặt phòng: 15/10 14:00 → 16/10 12:00 (Giá: 500,000 VNĐ/ngày)
Check-in thực tế: 15/10 13:30 (sớm 30 phút - miễn phí)
Checkout thực tế: 16/10 16:30 (muộn 4.5 giờ)

→ Nhân viên nhấn "Trả phòng"
→ Hệ thống tính:
   - Tiền phòng: 500,000 VNĐ (từ 13:30 → 16:30)
   - Phí checkout muộn:
     * 12:00-15:00 (3 giờ): 500,000 × 30% × (3/24) = 18,750 VNĐ
     * 15:00-16:30 (1.5 giờ): 500,000 × 50% × (1.5/24) = 15,625 VNĐ
     * Tổng phí muộn: 34,375 VNĐ
   - Tạm tính: 534,375 VNĐ
   - VAT 10%: 53,437 VNĐ
   - Tiền cọc: -100,000 VNĐ
   - TỔNG: 487,812 VNĐ
→ Chuyển đến trang thanh toán
→ Khách thanh toán
→ Hoàn tất!
```

---

## 📞 HỖ TRỢ

Nếu gặp vấn đề, vui lòng liên hệ:
- **Email:** support@hotel.com
- **Hotline:** 1900-xxxx
- **Xem tài liệu:** [CHECKOUT_PAYMENT_REDESIGN.md](./CHECKOUT_PAYMENT_REDESIGN.md)

---

**Cập nhật lần cuối:** 16/10/2025  
**Phiên bản:** 1.0
