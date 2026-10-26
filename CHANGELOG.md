# Nhật ký thay đổi VLAB

## 0.1.22 / code43 — 2026-10-26

- Cửa sổ Quy trình hiển thị đủ bước đo/lặp/so sánh từ tài liệu 32 bài và ví dụ minh họa tách khỏi số đo. [Bằng chứng](docs/ProcedureCoverage/REPORT.md).
- Chuẩn độ có thao tác pipette hút/tráng/chuyển mẫu bằng cảm ứng và kẹp đỡ riêng; giữ lượng mẫu khi thả tay. Kiểm tra lại pha loãng C05 và SDK Hóa. [Bằng chứng và giới hạn](docs/TitrationPipette/REPORT.md).

## 0.1.21 / code42 — 2026-10-26

- Sửa dụng cụ trôi khỏi màn hình khi nhấc tay sau khi lấy; kéo dụng cụ rời tới mặt đỡ để đặt xuống bằng cảm ứng.
- Giữ kiểm tra mặt đỡ/vật cản, tư thế bình và cơ chế lắp ráp. Hành trình kéo–đặt C06 qua khi bật và tắt joystick trong Unity.
- [Bằng chứng và giới hạn](docs/PhonePlacement/REPORT.md). Chưa kiểm tra Android/Cardboard/BLE thật ở lượt này.

## 0.1.20 / code41 — 2026-10-26

- Hiện logo và dòng Made by Ngựa Mán team with love khi khởi động; cửa sổ quy trình riêng và khả năng nhìn gần hơn.
- Thị kính tròn nền đen với chỉnh tiêu cự/vật kính, ẩn bảng điều khiển; ghi và lưu nét vẽ trong sổ ngoại tuyến.
- Khay tương phản thật cho dụng cụ phù hợp, tính cả nắp mở khi bố trí; đặt dụng cụ rời có trọng lực và kiểm tra mặt đỡ/vật cản.
- C05 hút/chuyển bằng pipette, C04 nhỏ giọt, C08 rót từng phần vào cốc cách nhiệt: lượng nguồn hữu hạn, chặn nắp/vật cản và kiểm tra thời gian đo.
- [Bằng chứng và giới hạn](docs/LearnerExperience/REPORT.md). Còn thao tác hỗ trợ; chưa xác minh Android/Cardboard/BLE thật ở lượt này.
## 0.1.19 / code40 — 2026-10-26

- C05: giữ/kéo bình rửa qua cổ bình mở để thêm nước dần; thả dừng dòng.
- Kiểm tra vật cản, thể tích thực, chất tan, vạch và khôi phục lần pha ban đầu dở dang. Nồng độ tính theo thể tích thực.
- [Bằng chứng và giới hạn](docs/ManualWater/REPORT.md). Pha nối tiếp vẫn dùng nút hỗ trợ; chưa có rót hóa chất bất kỳ.

## 0.1.18 / code39 — 2026-10-26

- Xóa liên kết dụng cụ cũ trước khi chuyển phòng, tránh gọi vào đối tượng đã bị gỡ khi trở lại từ khám phá/chuyển chế độ.
- Hướng dẫn hiện việc cần làm, tiêu chí và bước tiếp theo; một vùng đọc, nút cố định trên điện thoại.
- Sửa nội dung BLE thành hai thiết bị, gán nút chung; định dạng chỉ số/mũ trong bài học.
- [Bằng chứng và giới hạn](docs/ExperienceFollowup/REPORT.md). Chưa có thao tác rót tự do trong bài học.

## 0.1.17 / code38 — 2026-10-26

- P01 dùng đúng tay ngang và điểm gắn gốc của giá đỡ, tách dây/quả nặng khỏi trụ đứng.
- Giữ camera khi đổi bước và mở dụng cụ; FOV70–120°, nút thoát ở gần cửa.
- Đặt dụng cụ lên bàn trống; thao tác lam/kẹp/vật kính; B01 dùng10× trước40× và khóa chỉnh thô40×.
- C04 diễn họa kết tủa tiến triển có tạm dừng; âm thanh vòi nước, boot VLAB, lối vào gán nút BLE chung.
- [Kiểm tra, ảnh và các phần chưa hoàn thành](docs/ExperienceRepair/REPORT.md). Chưa nghiệm thu thiết bị thật hoặc đủ32UI journeys.

## 0.1.16 / code36 — 2026-10-25

- Chạm mô hình chưa lắp mở bảng Đặt xuống / Trả về khay; thao tác Quay lại trả vật đang cầm trước khi rời bài.
- Điểm đặt tồn tại trong lúc cầm, kể cả khi tắt gợi ý học tập; nhắc đúng giá đỡ còn thiếu và khóa thao tác đặt không hợp lệ.
- Đặt xuống khi quan sát trả dụng cụ về đúng vị trí cũ; không thay đổi số đo.
- Kiểm tra: [báo cáo và giới hạn](docs/EquipmentPlacement/REPORT.md).


Ghi riêng thay đổi mã nguồn và APK để tránh tải nhầm phiên bản.

## 25/10/2026 — 0.1.15 / code35

- Hướng dẫn làm quen tạm ẩn ở kết quả, sổ, cài đặt và màn chuẩn bị; hiện lại khi trở về màn thuộc tour.
- Tái hiện lỗi trước sửa; kiểm tra hồi quy hai kích thước màn hình và chạy lại con lắc trên APK với hồ sơ sạch. Dữ liệu con lắc còn sau nâng cấp từ code34.
- [Báo cáo, bằng chứng và phạm vi chưa hoàn thành](docs/StudentExperience/REPORT.md). Chưa nghiệm thu điện thoại, kính hoặc tay cầm thật.

## 25/10/2026 — 0.1.14 / code34

- Sửa trạng thái lấy/đặt/đóng bảng dụng cụ, thêm tháo về khay trong bước lắp ban đầu và hành động tiếp theo theo trạng thái bài.
- Lưu số đọc trong lúc thực hành vào cùng lượt, giữ số 0, phân biệt lượt dở, bảo toàn bản cũ khi lỗi ghi; kiểm tra sổ sau khởi động mới.
- Ô hướng dẫn gọn và số đo dễ đọc; thêm âm thao tác, khí và mô-tơ theo trạng thái.
- [Bàn giao và giới hạn kiểm chứng](docs/PracticeRepair/HANDOFF.md).

## 24/10/2026 — 0.1.13 / code33

- Màn chọn bài dùng danh sách gọn, ảnh bàn riêng cho 32 thí nghiệm và một hành động bắt đầu rõ ràng; thích ứng màn hình dọc/ngang, tablet và vùng khuyết màn hình.
- Mặc định tối; thêm Rừng sâu và Xanh biển, giữ Sáng và Xám than. Năm nút tròn ở menu chỉ làm mờ phần nền bên trong vòng tròn.
- Hướng dẫn chuyển bước theo thao tác thật, có xem lại/bỏ qua và vùng riêng tránh che nút chính; mức gợi ý Nhẹ/Chi tiết/Tắt được lưu.
- Panorama phòng rõ hơn; độ nét VR có tuỳ chọn độc lập với cấu hình đồ hoạ, giữ nguyên hệ thống xoay đầu.
- [Bàn giao, ảnh và phạm vi kiểm tra](docs/DesignUpgrade/HANDOFF.md). Chưa kiểm tra APK này trên điện thoại, kính hoặc tay cầm thật.

## 24/10/2026 — 0.1.12 / code32

- Bảng chọn có preview model thật, thao tác dụng cụ không còn bị popup lớn chặn; sửa giữ dụng cụ, collider và camera khi kéo.
- Kéo đầu dò/ma sát/ròng rọc nối với số đo SDK; thêm bảng công thức, giữ phác thảo, tour spotlight và hint theo trạng thái.
- Màn chọn bài gọn hơn; âm thanh thao tác có vị trí và tắt tiếng được; nền sáng hơn.
- Giữ tích hợp controllerV6 hiện có. [Bàn giao và phạm vi kiểm tra](docs/Workbench/HANDOFF.md); chưa xác minh phần cứng thật.

## 23/10/2026 — bảng kính hiển vi và tài liệu hiện hành

- B01/B08 hiện đủ sáu nhãn phân loại; B02 hiện hai lựa chọn. Chọn mẫu giữ nguyên ảnh và bảng, các nút chỉnh kính vẫn truy cập được; sửa caption và nhãn dài bị cắt.
- Khôi phục lớp thành tế bào cố định trong ô quan sát B02 bằng asset có sẵn, sửa gợi ý cho dụng cụ chỉ có đường vẽ và giữ tương thích hàm chọn dụng cụ.
- Kiểm tra Editor: 24 kiểm tra bảng kính hiển vi, 38 kiểm tra tương tác và 36 kiểm tra hộp thoại đạt. Tổng hợp nhiều checkpoint ghi nhận 32/32 bài lưu/mở lại, có bước chuẩn bị hỗ trợ; chưa thay thế một lượt kiểm thử độc lập đủ 32 bài trên source cuối. [Phạm vi và ảnh](docs/QA/2026-10-23/README.md).
- Giữ README giới thiệu/banner gốc, bổ sung nút tải APK công khai và cập nhật trạng thái kiểm thử; tài liệu được lập chỉ mục theo hướng dẫn, thiết kế, QA và lịch sử. Giữ nguyên vị trí asset Unity và `.meta`.
- Chỉ cập nhật mã nguồn; chưa build hoặc phát hành APK mới.

## 23/10/2026 — đồng bộ source và tài liệu

- Giữ README gốc và banner, sửa hướng dẫn cài APK/mở Unity/build Android, thêm mục lục tài liệu và phân biệt source với APK phát hành.
- Căn góc nhìn điện thoại theo bước con lắc; tránh đổi camera khi đang cầm/kéo, bảo toàn model và quyền điều khiển head pose của VR.
- Lưu receipt 37 kiểm tra tương tác/góc nhìn; kết quả lượt chạy 32 bài là 26 lưu/mở lại, 6 bị chặn và một lỗi đường viền trong kiểm tra riêng. [Chi tiết và các giới hạn](docs/QA/2026-10-23/README.md).
- Không build hoặc phát hành APK mới trong đợt đồng bộ này.

## Chưa phát hành — tương tác và hướng dẫn

- Gaze chọn dụng cụ qua chung luồng hành động; thao tác xung giữ dụng cụ đến khi chủ động thả. Con lắc bắt đầu từ vị trí người học đã kéo.
- Giữ vị trí thao tác khi trỏ sang UI; hủy thao tác khi mở bảng, mất focus hoặc ứng dụng pause.
- Gợi ý tại model tăng mức khi chờ/sai, giảm sau tiến bộ; sửa chữ bị cắt và bảo toàn gợi ý đúng dụng cụ sau ba lần chọn sai.
- Kiểm thử Editor:34 kiểm tra tương tác/cảm ứng và36 kiểm tra hồi quy hộp thoại. [Phạm vi và ảnh](docs/QA/2026-10-23/README.md). Chưa có APK hay xác nhận thiết bị thật cho đợt này.

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
