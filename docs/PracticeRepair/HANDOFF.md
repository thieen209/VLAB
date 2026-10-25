# Bàn giao VLAB 0.1.14 / code34 — thực hành, số đo và âm thanh

[Tải APK](https://github.com/thieen209/VLAB/releases/download/v0.1.14-practice-code34/VLAB-0.1.14-practice-code34.apk) · [Thông tin APK và SHA256](ARTIFACT.json) · [Tình trạng 32 bài](experiment-status.csv)

## Nguyên nhân và sửa đổi

- Bảng dụng cụ được dựng trước khi cập nhật trạng thái cầm nên nút hiển thị không đúng. Chọn để xem và lấy dụng cụ được tách rõ; nút lấy món tiếp cập nhật trạng thái trước khi dựng bảng. Đóng bảng trả vật đang cầm về khay. Có tháo/lắp lại trong giai đoạn lắp ban đầu, với ràng buộc vật đỡ và vật đang phụ thuộc.
- Trước đây, số đo thường chỉ được đưa vào sổ khi kết thúc bài. Giờ các mốc đọc thực từ cân, burette, khí, nhiệt kế và phân loại ảnh được lưu trong lúc làm; kết quả đủ điều kiện cập nhật cùng mã lượt. Không ghi mỗi frame thành lượt mới; số 0 hợp lệ được giữ. Số đọc bổ sung cập nhật cùng lượt. Thống kê kết quả hoàn chỉnh loại trừ lượt còn dở.
- Sổ mặc định mở phần số đo khi đến từ bài đã có dữ liệu. Lượt còn dở có nhãn riêng. Khi ghi ổ đĩa thất bại, dữ liệu cũ được giữ, bản đo chờ lưu được giữ để thử lại; không báo thành công giả.
- Hành động chính theo trạng thái thật dẫn tới dụng cụ/điều khiển cần dùng. Tắt hint vẫn thao tác được. Ô hướng dẫn nhỏ chừa trung tâm; giá trị và đơn vị nằm trước tên đại lượng dài. Bài ngâm khoai hiển thị thời gian còn lại từ trạng thái thật.
- Thêm âm rót, đọc số, công tắc, khí sinh ra; mô-tơ chạy theo cơ cấu và dừng khi pause/rời bài. Đây là hiệu ứng tổng hợp trong app, không phải bản thu phòng thí nghiệm thật. Không thêm gói âm thanh bên ngoài.

## Bằng chứng

- **32/32 bài** có walkthrough đạt trong Unity Editor: chọn bài, lắp/chuẩn bị, thao tác bằng sự kiện chạm qua UI, đo, kết quả và sổ. Xem từng bài và lượt chạy trong CSV. Đây không phải kết quả kiểm thử học sinh hoặc điện thoại thật.
- Đối chiếu trước/sau tải lại từng mã lượt, giá trị, đơn vị, chi tiết; mở sổ từng bài bằng thao tác UI trong một tiến trình Unity mới. So sánh số thực có dung sai làm tròn 1e-12; nhãn tùy chọn null/rỗng được coi tương đương theo cách Unity lưu JSON.
- Kiểm tra dữ liệu cũ, sao lưu, số 0, lỗi ghi ổ đĩa có chủ đích và thử lưu lại. Kiểm tra lỗi persistence là fixture riêng; không sửa file của người dùng.
- Kiểm tra tháo/lắp lại và hủy cầm; bài ma sát chạy khi tắt hint; điểm đo khí bổ sung không tạo lượt trùng; âm mô-tơ dừng/chạy khi pause/resume.
- Điện phân được chỉnh thời gian 20 giây và ngâm khoai 60 giây qua UI để kiểm tra có giới hạn; không tăng tốc đồng hồ mô phỏng. Walkthrough xác minh luồng với các thông số này, không bao phủ mọi tổ hợp thông số.
- Ô thực hành nằm trong màn hình 1280×720, 2340×1080, 2048×1536 và chừa trung tâm. VR được kiểm tra bố cục world-space trong Editor, không dùng kết quả đó để kết luận cảm biến/head tracking hoạt động.

## Giới hạn

- Chưa cài/chạy APK này trên điện thoại, kính Cardboard hoặc tay cầm BLE thật; chưa đo FPS/nhiệt/pin và chưa thử với học sinh. Các phép phân loại ảnh trong walkthrough dùng lựa chọn từ fixture, không đánh giá khả năng nhận biết của người học.
- Tháo về khay áp dụng lúc lắp ban đầu, trước các bước xử lý mẫu. Không tháo tùy ý giữa phản ứng. Bài đang dở khi mở lại vẫn theo cơ chế chuẩn bị an toàn hiện có; số đọc đã lưu được giữ riêng, không hứa khôi phục phản ứng đang chạy từng khung hình.
- Sửa cùng nguồn trên project hiện tại, giữ save cũ và các tài liệu README trước. APK ký bằng chứng thư VLAB hiện có; không đưa khóa ký, cache hay APK lớn vào Git.
