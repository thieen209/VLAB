# VLAB 0.1.16 / code36 — Cầm, đặt và hướng dẫn dụng cụ

## Lỗi tái hiện và thay đổi

Chạm trực tiếp một mô hình chưa lắp giữ dụng cụ theo con trỏ nhưng không mở bảng thao tác đặt/trả. Bản cũ còn hướng dẫn mở lại Dụng cụ để trả khay; dấu hướng dẫn tự tắt sau 12 giây.

- Chạm dụng cụ mở ngay **Đặt xuống** và **Trả về khay**. Nút đặt là hành động chính; bảng đang cầm chỉ giữ các lựa chọn cần thiết.
- Không đặt được nếu giá đỡ chưa lắp. Hiển thị tên giá đỡ cần lắp trước; vẫn có thể trả dụng cụ về khay.
- Nhãn **Vị trí đặt** chỉ đến nơi lắp và tồn tại trong lúc cầm. Tắt gợi ý học tập vẫn giữ dấu đặt cần cho thao tác.
- Khi nhấc dụng cụ đã lắp để quan sát, **Đặt xuống** trả về đúng vị trí và góc xoay cũ. Quay lại cũng trả vật trước khi rời thao tác.
- Ẩn UI hủy việc cầm bằng con trỏ; không để vật kẹt khi các nút điều khiển biến mất.
- Giữ nguyên bộ mô hình, thông số khoa học, số liệu, giao thức tay cầm và quy tắc chủ sở hữu dụng cụ.

## Bằng chứng

- [Trước sửa](baseline-reproduced.txt): chạm mô hình thật qua Input System ở bài Ném xiên không mở bảng đặt/trả.
- [Sau sửa](fixed-checks.txt): kiểm tra thao tác cầm/trả/lấy lại/đặt tại P02, C01, B07, M02, độ phân giải 1280×720 và 2340×1080.
- C01: thử sai thứ tự lắp, đóng bảng khi đang cầm, tắt gợi ý và chờ hơn 12 giây, nhấc lên rồi đặt lại; kiểm tra vị trí, góc quay, quyền sở hữu và bước quy trình.
- Kiểm tra phụ thuộc và quan sát có dùng dữ liệu bố trí do test thiết lập; đây là kiểm thử hồi quy của lập trình viên, không phải học sinh thực hiện độc lập toàn bộ bài.
- 133 kiểm tra Editor đạt. Sau đó bổ sung một điều kiện giữ đúng trạng thái nút khi mở bảng đầy đủ; APK cuối được đóng lại từ mã này.
- Không kiểm tra chạm trực tiếp trên APK ở lượt này: Computer Use lỗi “foreground window did not report a process id” sau lần thử khôi phục. Không đánh dấu Android runtime PASS.

## Giới hạn

Không đánh giá lại toàn bộ 32 quy trình, chuyển động tự do của mọi dụng cụ hay hiệu năng tất cả điện thoại. Điện thoại thật, Cardboard và BLE thật chưa được kiểm tra trong lượt này. Đây là bản sửa thao tác cầm–đặt và hướng dẫn, không phải chứng nhận sản phẩm hoàn hảo.

## Gói bàn giao

APK cuối build thành công, chữ ký v2 trùng chứng chỉ VLAB trước đó; package com.nguaman.vlab, Android 8+, ARMv7/ARM64. Xem [manifest và SHA256](ARTIFACT.json). Khâu đóng gói vượt mốc 30 phút; không phát hành bản trung gian trước chỉnh sửa cuối.
