# Mô tả chức năng hệ thống quản lý khách sạn

## 4.2. Xây dựng chương trình

**Phát triển hệ thống quản lý khách sạn Hotel Management**

Để hiện thực hóa hệ thống quản lý hoạt động của khách sạn, giao diện chương trình đã được phát triển và thiết kế với tính năng **thân thiện với người dùng**, trực quan và dễ sử dụng. Giao diện được xây dựng bằng ngôn ngữ lập trình **C#** kết hợp với framework **ASP.NET Core MVC**. Hệ thống này cho phép quản lý và vận hành khách sạn một cách hiệu quả và tiện lợi.

### 4.2.1. Thiết kế giao diện tổng

Trang giao diện tổng của người dùng được chia làm hai phần chính:
1. **Phần bên trên:** Hiển thị menu điều hướng các tính năng chính của chương trình.
2. **Phần bên dưới:** Hiển thị nội dung của từng tính năng cụ thể.

Người dùng có thể dễ dàng truy cập vào các chức năng quản lý khác nhau. Thanh điều hướng bên trái bao gồm các mục chức năng sau:
* **Dashboard** - Tổng quan hệ thống
* **Danh mục** - Quản lý phòng, loại phòng, dịch vụ, khách hàng, nhân viên, tài khoản
* **Đặt phòng** - Quản lý đặt phòng
* **Check-in/Check-out** - Quản lý nhận/trả phòng
* **Dịch vụ** - Quản lý dịch vụ khách sạn
* **Hóa đơn** - Quản lý thanh toán
* **Báo cáo** - Thống kê doanh thu

Mỗi mục chức năng này giúp người dùng thực hiện các tác vụ quản lý khác nhau, từ đó cải thiện hiệu suất làm việc và giảm thiểu các thao tác thừa.

*(Tham khảo: Hình 4.1 Giao diện tổng Dashboard)*

### 4.2.2. Giao diện quản lý đặt phòng 

Phần **Đặt phòng** là một module quan trọng, hỗ trợ nhân viên lễ tân quản lý đặt phòng một cách linh hoạt và hiệu quả.

**Các tính năng chính trong module này bao gồm:**

1. **Hiển thị danh sách đặt phòng**:
   * Giao diện liệt kê tất cả các phiếu đặt phòng hiện có.
   * Thông tin hiển thị: Mã đặt phòng, Thông tin khách hàng, Phòng đặt, Ngày nhận/trả phòng, Tiền cọc, Trạng thái.
   * Trạng thái đặt phòng được hiển thị rõ ràng: Chưa check-in, Đã check-in, Đã check-out.

2. **Tạo đặt phòng mới**:
   * **Chọn khách hàng:** Tìm kiếm khách hàng có sẵn hoặc tạo mới.
   * **Chọn phòng:** Hiển thị danh sách phòng trống trong khoảng thời gian đặt.
   * **Nhập thông tin:** Ngày nhận/trả phòng, Tiền cọc, Đơn giá, Hình thức thuê (theo giờ/ngày).

3. **Quản lý đặt phòng**:
   * **Xem chi tiết:** Hiển thị đầy đủ thông tin đặt phòng.
   * **In phiếu xác nhận:** Tạo và in phiếu xác nhận đặt phòng dạng PDF.
   * **Xóa đặt phòng:** Chỉ xóa được đặt phòng chưa check-in.

4. **Tìm kiếm và lọc**:
   * Tìm kiếm theo mã đặt phòng, tên khách hàng, mã phòng.
   * Lọc theo trạng thái đặt phòng.

*(Tham khảo: Hình 4.2 Giao diện quản lý đặt phòng, Hình 4.3 Phiếu xác nhận đặt phòng)*

### 4.2.3. Giao diện Quản lý Check-in/Check-out 

Giao diện **Check-in/Check-out** hỗ trợ nhân viên quản lý quá trình nhận và trả phòng một cách hiệu quả.

**Các chức năng chính bao gồm:**

1. **Quản lý Check-in**:
   * **Danh sách đặt phòng chờ check-in:** Hiển thị các đặt phòng đã đến ngày nhận phòng.
   * **Thực hiện check-in:** Xác nhận khách hàng nhận phòng, cập nhật trạng thái phòng.
   * **In phiếu check-in:** Tạo và in phiếu xác nhận check-in cho khách hàng.
   * **Cảnh báo quá hạn:** Hiển thị các đặt phòng đã quá hạn check-in.

2. **Quản lý Check-out**:
   * **Danh sách phòng đang sử dụng:** Hiển thị các phòng có khách đang ở.
   * **Tính toán thanh toán:** Tự động tính tiền phòng theo thời gian thực tế.
   * **Quản lý dịch vụ:** Thêm các dịch vụ khách sử dụng (ăn uống, giặt ủi, spa...).
   * **Thanh toán:** Hỗ trợ thanh toán tiền mặt và chuyển khoản.
   * **In phiếu check-out:** Tạo và in phiếu xác nhận check-out.

3. **Theo dõi trạng thái phòng**:
   * **Phòng trống:** Sẵn sàng cho đặt phòng mới.
   * **Phòng đang sử dụng:** Có khách đang ở.

*(Tham khảo: Hình 4.4 Giao diện check-in, Hình 4.5 Giao diện check-out, Hình 4.6 Phiếu xác nhận check-in/check-out)*

### 4.2.4. Giao diện quản lý phòng và loại phòng 

Giao diện này hỗ trợ người dùng quản lý thông tin phòng, loại phòng và giá cả một cách hiệu quả.

**Các chức năng chính bao gồm:**

1. **Quản lý loại phòng**:
   * **Hiển thị danh sách loại phòng:** Tên loại, Mô tả, Số lượng phòng.
   * **Thêm/sửa/xóa loại phòng:** Quản lý các loại phòng (Standard, Deluxe, Suite...).
   * **Quản lý giá:** Đặt giá theo giờ/ngày cho từng loại phòng.

2. **Quản lý phòng**:
   * **Danh sách phòng:** Mã phòng, Loại phòng, Trạng thái, Tầng.
   * **Thêm phòng mới:** Tạo phòng mới với thông tin chi tiết.
   * **Cập nhật trạng thái:** Trống, Đang sử dụng, Bảo trì.
   * **Xóa phòng:** Chỉ xóa được phòng không có lịch sử sử dụng.

3. **Tìm kiếm và lọc**:
   * Tìm kiếm theo mã phòng, loại phòng.
   * Lọc theo trạng thái, tầng.

*(Tham khảo: Hình 4.7 Giao diện quản lý phòng, Hình 4.8 Giao diện quản lý loại phòng)*

### 4.2.5. Giao diện quản lý dịch vụ khách sạn

Chức năng này giúp người dùng quản lý các dịch vụ bổ sung của khách sạn, tăng cường trải nghiệm khách hàng.

**Các chức năng chính bao gồm:**

1. **Hiển thị danh sách dịch vụ**:
   * Giao diện hiển thị danh sách dịch vụ dưới dạng bảng, bao gồm: Tên dịch vụ, Loại dịch vụ, Giá, Mô tả, Trạng thái hoạt động.

2. **Quản lý dịch vụ**:
   * **Thêm dịch vụ mới:** Tên dịch vụ, Loại (Ăn uống, Giặt ủi, Spa, Vận chuyển...), Giá, Mô tả.
   * **Chỉnh sửa thông tin:** Cập nhật giá, mô tả, trạng thái hoạt động.
   * **Xóa dịch vụ:** Chỉ xóa được dịch vụ không liên quan đến hóa đơn nào.

3. **Sử dụng dịch vụ**:
   * **Thêm dịch vụ vào phòng:** Khách hàng có thể sử dụng dịch vụ trong quá trình ở.
   * **Tính phí tự động:** Hệ thống tự động tính phí dịch vụ vào hóa đơn.
   * **Quản lý đơn hàng:** Theo dõi các dịch vụ đã đặt và trạng thái phục vụ.

4. **Báo cáo dịch vụ**:
   * Thống kê doanh thu từ các dịch vụ.
   * Danh sách dịch vụ phổ biến nhất.

*(Tham khảo: Hình 4.9 Giao diện quản lý dịch vụ, Hình 4.10 Giao diện sử dụng dịch vụ)*

### 4.2.6. Giao diện quản lý hóa đơn

Chức năng này giúp người dùng theo dõi và quản lý các hóa đơn thanh toán, đảm bảo tính minh bạch trong tài chính.

**Các chức năng chính bao gồm:**

1. **Hiển thị danh sách hóa đơn**:
   * Giao diện hiển thị danh sách hóa đơn với thông tin: Mã hóa đơn, Mã đặt phòng, Khách hàng, Ngày tạo, Tiền phòng, Tiền dịch vụ, Tổng tiền, Trạng thái thanh toán.

2. **Tìm kiếm và lọc hóa đơn**:
   * **Tìm kiếm:** Theo mã hóa đơn, mã phòng, tên khách hàng.
   * **Lọc theo trạng thái:** Đã thanh toán, Chưa thanh toán, Tất cả.
   * **Thống kê nhanh:** Tổng hóa đơn, Tổng doanh thu, Số hóa đơn đã/chưa thanh toán.

3. **Chi tiết hóa đơn**:
   * **Xem chi tiết:** Thông tin đầy đủ về hóa đơn, dịch vụ sử dụng.
   * **In hóa đơn:** Xuất hóa đơn dạng PDF.
   * **Thanh toán:** Xử lý thanh toán cho hóa đơn chưa thanh toán.

4. **Quản lý thanh toán**:
   * **Phương thức thanh toán:** Tiền mặt, Chuyển khoản.
   * **Xác nhận thanh toán:** Cập nhật trạng thái đã thanh toán.
   * **Lịch sử thanh toán:** Theo dõi các giao dịch đã thực hiện.

*(Tham khảo: Hình 4.11 Giao diện quản lý hóa đơn, Hình 4.12 Chi tiết hóa đơn)*

### 4.2.7. Giao diện báo cáo và thống kê

Chức năng này giúp người dùng theo dõi hiệu quả kinh doanh và đưa ra các quyết định quản lý dựa trên dữ liệu thực tế.

**Các chức năng chính bao gồm:**

1. **Báo cáo doanh thu**:
   * **Lọc theo thời gian:** Hôm nay, Hôm qua, 7 ngày, 30 ngày, 30 ngày trước, 3 tháng trước.
   * **Biểu đồ doanh thu:** Hiển thị doanh thu theo thời gian.
   * **Phân tích theo loại phòng:** Doanh thu từng loại phòng.
   * **Xuất báo cáo:** Tải báo cáo dạng PDF/Excel.

2. **Báo cáo công suất phòng**:
   * **Tỷ lệ lấp đầy:** Phần trăm phòng được sử dụng.
   * **Thống kê theo loại phòng:** Công suất từng loại phòng.
   * **Phân tích xu hướng:** Biểu đồ công suất theo thời gian.

3. **Báo cáo hiệu suất nhân viên**:
   * **Thống kê nhân viên:** Số lượng check-in/check-out xử lý.
   * **Đánh giá hiệu suất:** Dựa trên số lượng giao dịch.
   * **Báo cáo theo nhân viên:** Chi tiết hoạt động từng nhân viên.

4. **Dashboard tổng quan**:
   * **Số liệu tổng quan:** Tổng phòng, Tổng khách, Doanh thu hôm nay.
   * **Biểu đồ trực quan:** Hiển thị các chỉ số quan trọng.
   * **Cảnh báo:** Phòng cần dọn dẹp, Khách sắp check-out.

*(Tham khảo: Hình 4.13 Dashboard tổng quan, Hình 4.14 Báo cáo doanh thu, Hình 4.15 Báo cáo công suất phòng)*

### 4.2.8. Giao diện quản lý tài khoản

Giao diện này hỗ trợ quản lý người dùng hệ thống với các vai trò khác nhau và bảo mật thông tin.

**Các chức năng chính bao gồm:**

1. **Quản lý tài khoản nhân viên**:
   * **Hiển thị danh sách nhân viên:** Thông tin nhân viên, Vai trò, Trạng thái hoạt động.
   * **Thêm nhân viên mới:** Thông tin cá nhân, Vai trò (ADMIN, MANAGER, EMPLOYEE).
   * **Chỉnh sửa thông tin:** Cập nhật thông tin nhân viên.
   * **Quản lý trạng thái:** Kích hoạt/vô hiệu hóa tài khoản.

2. **Quản lý tài khoản lễ tân**:
   * **Tạo tài khoản lễ tân:** Liên kết với nhân viên có sẵn.
   * **Reset mật khẩu:** Đặt lại mật khẩu mặc định.
   * **Khóa/mở khóa tài khoản:** Quản lý quyền truy cập.
   * **Chỉ dành cho MANAGER/ADMIN:** Bảo mật quyền quản lý.

3. **Phân quyền người dùng**:
   * **ADMIN:** Toàn quyền hệ thống.
   * **MANAGER:** Quản lý nhân viên, báo cáo, tài khoản lễ tân.
   * **EMPLOYEE:** Thực hiện check-in/check-out, quản lý đặt phòng.


*(Tham khảo: Hình 4.16 Giao diện quản lý tài khoản, Hình 4.17 Giao diện tạo tài khoản lễ tân)*

### 4.2.9. Giao diện quản lý khách hàng

Chức năng này hỗ trợ quản lý thông tin khách hàng và lịch sử sử dụng dịch vụ.

**Các chức năng chính bao gồm:**

1. **Hiển thị danh sách khách hàng**:
   * Thông tin khách hàng: Họ tên, Số điện thoại, Email, Địa chỉ.
   * Lịch sử đặt phòng: Số lần đặt, Tổng chi tiêu.
   * Trạng thái: Khách VIP, Khách thường.

2. **Quản lý khách hàng**:
   * **Thêm khách hàng mới:** Thông tin cá nhân đầy đủ.
   * **Tìm kiếm khách hàng:** Theo tên, số điện thoại.
   * **Cập nhật thông tin:** Chỉnh sửa thông tin khách hàng.
   * **Lịch sử sử dụng:** Xem lịch sử đặt phòng và dịch vụ.

3. **Phân loại khách hàng**:
   * **Khách VIP:** Ưu tiên đặt phòng, giảm giá đặc biệt.
   * **Khách thường:** Dịch vụ tiêu chuẩn.
   * **Khách doanh nghiệp:** Hợp đồng dài hạn, giá ưu đãi.

*(Tham khảo: Hình 4.18 Giao diện quản lý khách hàng, Hình 4.19 Chi tiết khách hàng)*

### 4.2.10. Tính năng in phiếu xác nhận 

Chức năng này hỗ trợ tạo và in các loại phiếu xác nhận trong quy trình quản lý khách sạn.

**Các tính năng chính bao gồm:**

1. **Phiếu xác nhận đặt phòng**:
   * **Thông tin khách hàng:** Họ tên, Số điện thoại, Email.
   * **Thông tin phòng:** Mã phòng, Loại phòng, Ngày nhận/trả phòng.
   * **Thông tin thanh toán:** Đơn giá, Tiền cọc, Tổng tiền.
   * **QR Code xác thực:** Mã QR để xác minh phiếu.

2. **Phiếu xác nhận check-in**:
   * **Thông tin check-in:** Thời gian thực tế nhận phòng.
   * **Nhân viên phụ trách:** Tên nhân viên thực hiện check-in.
   * **Ghi chú đặc biệt:** Yêu cầu của khách hàng.

3. **Phiếu xác nhận check-out**:
   * **Tổng kết thanh toán:** Tiền phòng, Tiền dịch vụ, Tổng cộng.
   * **Phương thức thanh toán:** Tiền mặt/Chuyển khoản.
   * **Thời gian check-out:** Thời gian thực tế trả phòng.

4. **Tính năng in ấn**:
   * **Thiết kế chuyên nghiệp:** Header khách sạn, Logo, Thông tin liên hệ.
   * **Tự động chuyển PDF:** Khi nhấn Print, tự động chuyển sang PDF.
   * **Responsive design:** Hiển thị đẹp trên mọi thiết bị.
   * **Lưu trữ database:** Tất cả phiếu đều được lưu với mã CR-xxx.

*(Tham khảo: Hình 4.20 Phiếu xác nhận đặt phòng, Hình 4.21 Phiếu xác nhận check-in, Hình 4.22 Phiếu xác nhận check-out)*
