# VLAB 0.1.20 / code41 — phạm vi bàn giao

Đây là biên bản code41. Xem [đối chiếu đầy đủ goal tại code44](GOAL_AUDIT_CODE44.md) để phân biệt phần đã có bằng chứng và phần còn thiếu.

## Thay đổi

- Boot VLAB có dòng **Made by Ngựa Mán team with love**.
- Cửa sổ quy trình riêng; hướng dẫn việc hiện tại, điều kiện hoàn thành và bước tiếp theo. Nút nhìn gần trên điện thoại có mức gần hơn.
- Thị kính tròn nền đen, chỉnh tiêu cự/vật kính và ẩn điều khiển. Sổ ghi chú có bút vẽ, xóa/hoàn tác, lưu ngoại tuyến và giữ dữ liệu khi lưu thất bại.
- Dụng cụ rời phù hợp rơi thẳng xuống mặt đỡ đã kiểm tra. Bộ phận lắp cố định vẫn cần đúng vị trí. Khay tương phản dùng mô hình thật; bố trí bình định mức tính cả nắp mở.
- C05: hút/tráng/chuyển pipette với lượng nguồn hữu hạn. C04: nhỏ giọt theo thao tác giữ/kéo. C08: rót từng phần, thả dừng dòng; kiểm tra nắp, miệng nhận, vật cản, bảo toàn lượng và trạng thái nhiệt; phải đo đủ thời gian mới ghi kết quả.

## Bằng chứng và giới hạn

- `guide-checks.txt`: cửa sổ quy trình, nhìn gần; độ phủ nội dung 32 bài, không phải hoàn thành tương tác cả 32 bài.
- `batch-checks.txt`: sổ vẽ/lưu/khôi phục khi lỗi, đầu vào VR mô phỏng, thị kính B01, pipette C05 và rơi có trọng lực.
- `review-checks.txt`: boot/sổ/thị kính B01/B02/B08 qua; lượt cũ dừng ở lỗi khay, được kiểm tra lại riêng.
- `final-checks.txt`: nhỏ giọt C04 qua; lượt cũ dừng ở lỗi phần nắp bình C05 làm vượt vùng đỡ.
- `trays-checks.txt`: lượt mới sau sửa bố trí nắp mở, qua mặt đỡ và tháo dọn khay cho đủ 16 bài Hóa/Sinh.
- `pour-checks.txt`: đầu vào cảm ứng trong Editor, rót từng phần/lặp lại, nắp đóng/mở, bảo toàn nguồn và cốc nhận, nhiệt và thời gian quan sát.
- `placement-checks.txt`: kết quả lượt kiểm tra đặt dụng cụ cuối; vị trí thả do fixture thiết lập, nút dùng sự kiện cảm ứng thật trong Editor. Không diễn giải thành thao tác tay cầm thật.
- `android-build.txt`, `apk-signature.txt`, `ARTIFACT.json`: kết quả đóng gói, danh tính, chữ ký và hash APK.

Đây là prototype. Chưa kiểm tra APK này trên điện thoại, Cardboard hoặc BLE thật. Các tốc độ dòng, đơn vị nhỏ giọt định tính C04 và tham số trộn/thất thoát nhiệt C08 là mô hình minh họa, không phải động học đã hiệu chuẩn. Trên điện thoại vẫn có hỗ trợ độ cao/độ nghiêng và vị trí nhiệt kế. Dụng cụ rơi theo phương đứng; chưa mô phỏng lật, đổ tràn hay ném. Một số bước chuẩn bị, pha nối tiếp và các đường chuyển hóa chất ngoài C04/C05/C08 còn dùng hỗ trợ. Trạng thái nhiệt C08 bắt đầu lại khi mở lại bài; số đo và lịch sử đã lưu vẫn ở sổ.

Chưa tuyên bố hoàn thành toàn bộ goal hoặc nghiệm thu đủ 32 thí nghiệm trên thiết bị thật.

Ảnh chụp thực tế trong Editor: [boot](images/startup-team-credit.png), [thị kính](images/B01-eyepiece-phone.png), [sổ vẽ](images/44a-so-ve-tren-trang.png), [đặt dụng cụ](images/C06-loose-put-down.png), [rót C08](images/C08-manual-base-pour.png).
