# Nhật ký thay đổi VLAB

Ghi riêng thay đổi mã nguồn và APK để tránh tải nhầm phiên bản.

## 23/10/2026 — cập nhật source, chưa phát hành APK

- Hộp thoại xác nhận trên điện thoại gọn hơn, căn giữa và giữ các nút hành động cùng hàng; lớp nền tối phủ toàn màn hình.
- Trình đọc Chính sách bảo mật / Điều khoản sử dụng dùng nguyên trang PDF Việt / Anh, có chuyển trang, phóng to và cuộn. Tổng cộng 47 trang; PDF gốc được giữ trong `Assets/StreamingAssets/Legal/`.
- Thêm các probe Editor cho hộp thoại và trình đọc tài liệu; giữ biên bản và ảnh trong [QA ngày 23/10](docs/QA/2026-10-23/README.md).
- Giữ README giới thiệu và banner cũ; làm rõ cài APK, mở đúng Unity, build development và khác biệt chữ ký khi cập nhật. Tài liệu lịch sử giữ ở `docs/archive/`, báo cáo và ảnh theo đợt ở `docs/QA/`.

Chưa có APK mới từ những thay đổi này. Không suy ra kết quả cảm biến Cardboard, BLE hay toàn bộ 32 thí nghiệm từ các kiểm tra Editor này.

## 22/10/2026 — khôi phục README và đồng bộ prototype

Khôi phục nội dung giới thiệu tiếng Việt, banner gốc và lưu README tháng 9 để đối chiếu. Đồng bộ source prototype 0.1.7, sửa hướng dẫn cài đặt và lập chỉ mục tài liệu.

## 0.1.7 / code21 — candidate build riêng

[Xác định artifact và phạm vi kiểm thử](docs/QA/Prototype-0.1.7/README.md). APK này được build trước các sửa đổi source ngày 23/10.

## 0.1.4 / code5 — APK công khai gần nhất

[Tải từ GitHub Releases](https://github.com/thieen209/VLAB/releases/tag/v0.1.4-secondtest-20261020) · [Ghi chú phiên bản](docs/PATCH_014.md).
