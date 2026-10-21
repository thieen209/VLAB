# VLAB 0.1.6 / code17 — bàn giao prototype

APK: `VLAB-0.1.6-flagship-r9-candidate.apk` (r9/code17, tên ứng dụng VLAB). 124374343 bytes. SHA256: `4687d74810741feab528b9966dcac5622aa85e90d6344041a32e3103d34cd1e4`.
Build0 lỗi/42 cảnh báo; chữ ký cũ đã xác minh; Android8/API26+, ARMv7/ARM64, target36. Cài nâng cấp bằng cùng package `com.nguaman.vlab`; không xóa dữ liệu. Chỉ bàn giao code17, không dùng r8/code16 diagnostic.

## Đã kiểm tra
- r31:88 mục kiểm tra component đạt,0 fail, cùng bộ Input System mở rộng. Đây không phải88 quy trình thí nghiệm trên điện thoại thật.
- Bốn bài trọng tâm P01 Con lắc đơn, C04 Nhận biết chloride, B01 Biểu bì hành, M01 Truyền bánh răng đã có lượt thao tác/quan sát/kết quả riêng và sổ trên giả lập Android code12. P01 có kéo trái thực, đủ chu kỳ; B01 tự nhìn sơ đồ để phân loại3 cấu trúc, không đọc đáp án ẩn; M01 dùng góc trục SDK; C04 kết tủa trắng cùng hai đối chứng.
- Code14: chạm chính giữa bánh36 chọn được, motor chạy/tạm dừng/tiếp tục, kết quả60→20rpm được ghi. Inventories nhỏ ở trái đưa mô hình về vùng trống bên phải. Đóng quan sát trả đủ vị trí/góc/FOV camera.
- Code17 cuối: kéo cần ngắn1.5giây sang phải, trái và lùi đều dịch cảnh thực (`230`–`234-r9-*`); nhìn tự do235, trở về bàn236. Ba lần lấy/kéo/thả ống nhỏ giọt nhận đủ mẫu237–238, kết quả riêng239, Làm lại240 trả bộ dụng cụ mới và bỏ cặn cũ.
- Code17: từ hoạt động về Home rồi bật Cardboard, menu đứng đúng và không có đồ bàn đè lên241. Logo VLAB gốc hiện ở launcher sau khi cài242. Không thay logo bằng hình tự vẽ; chỉ render thêm khoảng trống an toàn cho adaptive icon.
- Hồ sơ checksum hợp lệ sau nâng cấp và tắt/mở process: P01=4,P02=1,C04=4,M01=3,B01=2; giữ mức đồ họa Cân bằng. Không đưa hồ sơ cá nhân vào Git/album.

## Những sửa đổi chính
Đầu vào giữ đúng điểm chạm đầu khi Android gộp bắt đầu với di chuyển/kết thúc trong một nhịp. Cùng sửa này áp dụng cần ảo và lấy dụng cụ; UI-started touch không chuyển thành thao tác thế giới, ngón đang giữ qua modal vẫn bị chặn. Capsule đi lại thích ứng với sàn khi camera thấp sát bàn, vẫn chặn bàn/tường và đi men cạnh. Bộ kiểm tra có sàn thường, sàn nâng, va chạm và ba trường hợp input gộp.

Khung nhìn thí nghiệm chừa HUD/chân nút, có gần/xa và quay về bàn; không tự dịch đầu người dùng trong VR. Gợi ý theo bước, lỗi có hành động phục hồi cụ thể bằng tiếng Việt. Lắp sẵn dụng cụ được ghi là hỗ trợ, không tự tạo số đo/hoàn thành khoa học. C04 dùng mặt bàn sáng và tấm quan sát tối để thấy cặn; B01 là sơ đồ minh họa có ghi rõ giới hạn hiệu chuẩn.

Ánh sáng dùng ambient/fill hiện có; không tăng exposure hoặc thêm hậu kỳ. Có ảnh gameplay trước/sau cùng camera/FOV ở Low/High qua4phòng; cặp Android201/202 xác nhận thay mức trực tiếp, hình học camera giữ nguyên. Render scale/MSAA/bóng/lọc thay đổi thật, nhưng không có chứng nhận FPS/nhiệt trên điện thoại thật.

## Giới hạn cần biết
Ảnh VR nghiêng90° ban đầu là test giả lập có hướng trọng lực portrait trong khi màn hình landscape. Hướng cảm biến nhất quán cho cảnh đứng đúng; gyro mô phỏng làm scene/UI xoay188. Điều đó chưa xác nhận xoay đầu, zero/recenter, hiệu chuẩn kính và độ trễ trên điện thoại/Cardboard thật. Không có BLE/tay cầm thật để nghiệm thu. ADB nhấnX thời lượng gần0 đôi lúc không thoát; nhấn giữ600ms hoạt động. Cần kiểm tra cảm giác chạm thực trên máy demo.

28 bài còn lại vẫn có trong danh mục, nhưng chưa qua toàn bộ quy trình thực hành native. Kiểm tra vào32bài/dụng cụ trước đây không được coi là32bài hoàn chỉnh. Chưa có đánh giá độc lập của học sinh mới dùng hay chấp thuận mỹ thuật cuối; không tuyên bố10/10 hoặc sẵn sàng production.

Có cảnh báo optional PlayAssetPackManager khi khởi động giả lập; các hành trình tiếp tục chạy. Không thấy fatal AndroidRuntime trong phạm vi log đã kiểm tra. Lần cold start trên giả lập ARM translation có ảnh đen tạm thời; tốc độ khởi động máy thật chưa đo.

Lịch sử lỗi/kiểm tra không đạt giữ trong ACCEPTANCE.vi.md: r27/r30 assertion phục hồi preview bị race với hover/thời điểm render; r31 chờ trạng thái preview thay vì ngủ cố định. Không che hoặc đổi tên kết quả fail thành pass. r8 có log tạm phục vụ điều tra; APK code17 đã kiểm tra không chứa log VLABMOVE hoặc các lớp QA/diagnostic Editor.

## Mã nguồn và giao nhận
Mã nguồn đầy đủ: `E:/VLAB/4 LABS/Ngua Man VLAB`. Bản Git để review ở `C:/Users/busyt/Documents/Codex/2026-10-19/rea/publish-vlab`. Commit cục bộ được ghi trong ledger khi hoàn tất; chưa push/phát hành công khai. File APK nằm cục bộ; tạo/upload artifact không đồng nghĩa người dùng đã nhận hoặc tải được file.
