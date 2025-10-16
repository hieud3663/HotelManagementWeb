# 🚨 FIX LỖI RUNTIME - Hướng dẫn nhanh

## ❌ Lỗi vừa gặp:
```
RuntimeBinderException: Cannot perform runtime binding on a null reference
ViewBag.LateFee - Không tồn tại
```

## ✅ Đã sửa:

### 1. **CheckOut/Details.cshtml** - View đã được cập nhật
- ❌ Cũ: `ViewBag.LateFee` (không còn tồn tại)
- ✅ Mới: `ViewBag.EarlyCheckinFee` + `ViewBag.LateCheckoutFee`

### 2. **JavaScript real-time calculation** - Đã cập nhật logic
- ✅ Tính phí check-in sớm theo khung giờ
- ✅ Tính phí check-out muộn theo khung giờ
- ✅ Miễn phí 30 phút (GIỜ) hoặc 1 tiếng (NGÀY)

---

## 🚀 CHẠY NGAY:

### **Bước 1: Dừng ứng dụng đang chạy**
Nhấn `Ctrl + C` trong terminal hoặc đóng cửa sổ browser.

### **Bước 2: Build lại**
```powershell
dotnet build
```

### **Bước 3: Chạy lại**
```powershell
dotnet run
# hoặc
.\start.ps1
```

### **Bước 4: Test ngay**
1. Vào trang Check-out
2. Chọn một reservation đã check-in
3. Xem chi tiết check-out
4. **Kết quả:** Không còn lỗi runtime, hiển thị đúng phí!

---

## 📊 Hiển thị mới trong hóa đơn:

```
Tiền phòng: 500.000 VNĐ
Tiền dịch vụ: 0 VNĐ
[Phí check-in sớm: XXX VNĐ]  ← CHỈ hiển thị nếu có
[Phí check-out muộn: XXX VNĐ] ← CHỈ hiển thị nếu có
-----------------------------------
Tổng trước thuế: XXX VNĐ
Thuế VAT (10%): XXX VNĐ
Tổng cộng: XXX VNĐ
```

---

## 🧪 Test case của bạn:

**Dữ liệu:**
- Check-in: 15/10/2025 16:11
- Check-out dự kiến: 15/10/2025 17:05
- Check-out thực tế: 15/10/2025 17:09:38 (muộn ~4 phút)

**Kết quả mong đợi:**
- Muộn < 30 phút (với giá GIỜ)
- **Phí check-out muộn = 0 đ** ✅ (MIỄN PHÍ)
- Dòng "Phí check-out muộn" sẽ **KHÔNG hiển thị** vì = 0

---

## ⚠️ Lưu ý:

- Nếu phí = 0, dòng sẽ **ẩn** trong hóa đơn
- Chỉ hiển thị khi có phí thực sự (> 0)
- JavaScript tự động update theo real-time

---

**DONE!** Giờ chạy lại và test nhé! 🎉
