# HHB Studio

Website thời trang tiếng Việt sử dụng ASP.NET MVC 5, Razor, Bootstrap, Entity Framework 6 và SQL Server. Dự án chuyển các luồng của bài mẫu E‑Shopper sang giao diện HHB Studio và tách trang cửa hàng với khu vực quản trị.

## Chạy trên Windows

Cần PowerShell 7, .NET SDK, .NET Framework 4.8 Developer Pack, IIS Express, SQL Server Express và `sqlcmd` trong PATH. Kết nối mẫu dùng Windows Authentication với instance `.\SQLEXPRESS`.

```powershell
pwsh -File .\scripts\Setup-Database.ps1
pwsh -File .\scripts\Start-Local.ps1
```

Script khởi tạo cơ sở dữ liệu `HhbStudio` với 4 danh mục và 12 sản phẩm mẫu. Khi được hỏi, nhập mật khẩu admin tự chọn từ 12 ký tự. Chạy lại với `-ResetAdminPassword` để đặt mật khẩu mới. Nếu dùng instance khác, truyền `-Server` khi khởi tạo và đổi `Data Source` trong chuỗi kết nối `ShopDb` của `HhbShop/Web.config`.

- Cửa hàng chạy ở `http://localhost:5179/`.
- Quản trị ở `http://localhost:5179/Admin`. Tên đăng nhập mặc định là `admin` và mật khẩu do bạn đặt.
- Có thể đổi cổng bằng `-Port 5180` khi chạy `Start-Local.ps1`.

Nếu đã cài bản trước, chạy lệnh dưới đây để thêm bảng tài khoản và các cột thanh toán, sau đó khởi động lại website. Đơn COD và tài khoản quản trị đã có được giữ lại.

```powershell
sqlcmd -S '.\SQLEXPRESS' -E -C -b -f 65001 -i .\database\Initialize.sql
```

## Chức năng

- Cửa hàng có danh mục, tìm kiếm, sắp xếp, phân trang, chi tiết và sản phẩm liên quan.
- Giỏ hàng theo phiên hỗ trợ thêm, chỉnh số lượng và xóa. Khách có thể đăng ký, đăng nhập và đăng xuất. Đăng nhập trước khi thanh toán vẫn giữ sản phẩm trong giỏ.
- Khách chọn thanh toán mô phỏng hoặc thanh toán khi nhận hàng (COD). Sau khi xác nhận, website lưu đơn, cập nhật tồn kho và mở hóa đơn có nút in. Chỉ chủ đơn xem lại hóa đơn trong mục **Đơn của tôi**. Thanh toán mô phỏng phục vụ bài tập và không thu tiền thật.
- Quản trị có đăng nhập, tổng quan, thêm/sửa/xóa danh mục và sản phẩm, tải ảnh JPG/PNG, theo dõi và cập nhật đơn. Hủy đơn hoàn lại tồn kho. Xóa sản phẩm đã có đơn sẽ ẩn sản phẩm khỏi cửa hàng và giữ lịch sử đơn.

## Áp dụng bài giảng

| Clip | Phần đã triển khai |
| --- | --- |
| Product Index, 08:48–44:00 và 45:28–71:52 | Dùng EF đọc sản phẩm, lọc theo danh mục, đưa tên, giá và ảnh từ SQL vào Razor. |
| Product Details, 01:17–08:59 và 20:32–24:23 | Liên kết từ thẻ sản phẩm tới view chi tiết theo ID. |
| Categories Partial View, 11:48–30:29 | Đọc danh mục từ SQL và tái sử dụng partial trên trang danh sách và chi tiết. |
| Create Shopping Cart, 01:26–12:11 và 14:20–25:05 | Giỏ hàng theo phiên, tổng tiền, cập nhật/xóa và chuyển sang checkout. |
| CRUD Using EF, 01:12–15:00 | Tách Admin Area, quản lý danh mục và sản phẩm với EF. |

## Kiểm thử

- Build trên .NET Framework 4.8 hoàn tất với 0 lỗi và 0 cảnh báo. Chạy thử qua IIS Express và SQL Server Express.
- Kiểm tra trên Edge ở 1.440 px và 390 px. Trang chủ, danh mục và chi tiết sản phẩm ở 390 px không bị tràn ngang. Ảnh tải thành công và chữ tiếng Việt hiển thị đầy đủ ở các trang đã kiểm tra.
- Đã thử đăng nhập quản trị, thêm danh mục, tải ảnh, sửa giá, cập nhật số lượng giỏ hàng và đặt hàng. Gửi lại yêu cầu đặt hàng trả về cùng một mã đơn. Đơn thử 3 sản phẩm có tổng 705.000 đ, tồn kho từ 9 xuống 6 rồi trở lại 9 khi hủy.
- Đã thử đăng ký từ giỏ, đăng nhập/đăng xuất, thanh toán mô phỏng và COD, mở lại lịch sử và chặn tài khoản khác mở hóa đơn. SQL dùng `AUTO_CLOSE OFF` cho riêng cơ sở dữ liệu `HhbStudio`. Nếu kết nối SQL tạm gián đoạn, website trả trang hướng dẫn thử lại.

## Nguồn ảnh và phông chữ

- Ảnh bìa là ảnh chụp của cottonbro studio. [Nguồn ảnh trên Pexels](https://www.pexels.com/photo/a-woman-wearing-a-beige-trench-coat-7760024/)
- Ảnh sản phẩm minh họa lấy từ `Pro_Images.rar`, ảnh giới thiệu và Bootstrap lấy từ bộ E‑Shopper trong tài liệu môn học.
- Dự án dùng Be Vietnam Pro và Lora. Tệp giấy phép phông chữ nằm trong `HhbShop/Content/fonts`.
