# Bàn giao sửa Controller V6 — 24/10/2026

Đích sửa: `E:\VLAB\4 LABS\Ngua Man VLAB`, Unity6000.6.4f1. APK mới: **VLAB0.1.11/code31**, tên gói `com.nguaman.vlab`. Dự án thật đã được sửa trực tiếp; `unity-overlay/` là các file nguồn để đối chiếu/chuyển sang đúng bản dự án, không phải dự án Unity độc lập.

## Đã sửa theo phạm vi được yêu cầu

| Mục | Thay đổi |
|---|---|
| F01 | Phát hiện chuyển trạng thái sau từng packet được chấp nhận. Queue FIFO riêng từng tay giữ bitmask nút/cò, sequence, host-arrival, tay và epoch ngữ cảnh. Không OR mất số lần nhấn. Snapshot pose/analog vẫn cập nhật theo packet mới nhất. |
| F02 | Kiểm tra khoảng cách hai arrival hợp lệ trước khi thay mốc cũ, đồng thời phát hiện gameplay Tick ngừng quá300ms dù native vẫn có backlog. Loss được giữ lại để phát cancel/release một lần; phải có neutral mới sau fence mới được rearm. |
| F03 | Chuyển nguồn con trỏ chỉ hủy pointer capture cũ và chặn Select đến khi thả Select. Không gọi hủy toàn bộ state của tay mới. Per-hand loss không trở thành mất toàn hệ khi vẫn còn một stream Fresh. Grip và vị trí hỗ trợ của tay còn tốt được giữ. |
| F04 | `EquipmentHandle.Owner` là nguồn sự thật cho None/LeftHand/RightHand/Legacy/Flagship. Các đường cầm, lấy, kéo, cập nhật, lắp và trả vật kiểm tra owner. Chỉ owner đúng được release; xem thông tin không tự chiếm quyền. Sắp xếp đồ rời bỏ qua vật đang có owner. Không thêm giữ chung một vật/physics ném tự do. |
| F05 | Vẫn bật cử chỉ gia tốc. Mỗi mẫu IMU mới được xử lý tại Submit bằng delta timestamp hợp lệ; tích phân cử chỉ có damping theo mẫu, không theo FPS. Timestamp trùng không tích phân lại; delta lùi/quá100ms không tạo impulse cho khoảng thiếu dữ liệu. Invalid/recovery reset baseline. Render chỉ đọc/giảm tiếp trạng thái theo thời gian, giữ giới hạn vận tốc/vị trí và về neo khi đứng yên. |
| F08 | `ControllerV6State.Frame.pointer` quyết định effective ray một lần: ray tay khi IMU hợp lệ, gaze ray khi invalid. Laser, UI, world raycast và bắt đầu Grip đều dùng ray đó. Pose bàn tay có thể đóng băng trong khi ray bắt đầu ở camera theo gaze fallback. |

## Chính sách queue sự kiện được chốt

- Mỗi tay tiêu thụ tối đa **một transition cho mỗi Tick**. Nhấn/thả hoặc hai click đến dồn được phát theo thứ tự qua các Tick tiếp theo; không gộp mất số lượng. Đây là quy tắc chiếu event stream vào `ActionFrame` boolean hiện có.
- Queue tối đa32 transition/tay. Transition quá300ms hoặc queue tràn thì cancel/rearm, không phát lại vô hạn các thao tác tồn đọng. Đây cũng là giới hạn độ trễ của dữ liệu được phép replay, không phải số đo latency thiết bị.
- Disconnect/generation mới, đổi mapping, đổi trang/modal và đổi trạng thái chặn gameplay xóa các event thuộc ngữ cảnh cũ. Neutral dùng để rearm phải mới hơn fence của lần cancel/loss đó.
- Các lệnh hệ thống trùng nhau từ hai tay vẫn được hợp nhất theo `ActionFrame` hiện có. Không thay UX menu/SYS hoặc tạo một event bus mới.
- `Raw`/`Trigger` là snapshot analog gần nhất. `ActionTrigger` giữ mức cò gắn với transition đang được xử lý, để cạnh cò ngắn không bị giá trị của packet thả sau đó xóa mất.

## Giao thức và firmware

Giữ nguyên packet56byte, version1, UUID GATT, CRC, hand ID, sequence và firmware ESP32-S3. Không đổi package Unity, cảnh/prefab, GPIO, hiệu chỉnh NVS hoặc định dạng dữ liệu học viên. Firmware trong gói là bản direct-v6 đã có, được kiểm tra byte/hash với bàn giao trước. Nếu hai tay đang dùng đúng bản đó thì đợt sửa app này không yêu cầu nạp lại firmware.

**F06 và F07 chưa được triển khai đầy đủ.** Bộ lọc ADC theo nhịp gửi được giữ nguyên. Với F07, chỉ có guard delta timestamp cần thiết để F05 không tích phân dữ liệu lùi/gián đoạn; logic freshness orientation vẫn có nhánh `timestamp != previous` và còn cần đợt sửa riêng. Không coi F07 đã hoàn tất.

## Kiểm tra và bằng chứng

- Trên bản gốc: đã viết/chạy assertion tái hiện trước khi sửa; receipt `evidence/before-regressions.txt` có25FAIL và12PASS. F03/F04 được chạy trong Unity Play mode với rig/dụng cụ thật của dự án. Fixture flagship dùng chính hàm tạo collider của ứng dụng.
- Bản sửa: **38 assertion hồi quy PASS, 0FAIL**; thêm một dòng đo trace và ba quan sát R, không tính các dòng đó là test PASS. Ca promote tay phải chờ frame neutral mới sau loss để không dùng dữ liệu trước fence làm điều kiện kiểm thử.
- **32 kiểm tra lõi cũ +34 kiểm tra tích hợp cũ PASS**; gồm HID/điện thoại, cầm/xoay/thả trong bốn lab, C04 analog dropper và UI chẩn đoán. Các receipt/log có trong `evidence/`.
- Trace IMU giống nhau tại25/30/60/90FPS, packet đều/dồn: max deviation được in là0,00000000m, ngưỡng đặt trước0,5mm. Test riêng xác nhận gesture vẫn bật và thực sự tạo dịch chuyển khác0. Resampling20/50/100Hz đều trong ngưỡng5mm. Đây là replay bằng phần mềm, không phải phép đo tracking hay latency trên phần cứng.
- Chữ ký, version và kết quả build Android ghi trong `apk-signature.txt`, `apk-metadata.txt`, `evidence/android-build.txt`. `SHA256.json` đối chiếu file trong gói; `changes.patch` đối chiếu bảy file runtime trước/sau.

Android build cuối: **Succeeded, 0 errors, 44 warnings**; ARM64+ARMv7, minSDK26/targetSDK36; APK127.723.108byte. Chữ ký v2 đã xác minh và cùng certificate với APKcode30. SHA256: `F6686039B2ED5B24F23102503264301979F062749132F5B0EE1BCC2E7A8501D1`. Các cảnh báo build vẫn được giữ trong log; không tuyên bố bản build không có warning.

Đọc `R-OBSERVATIONS.md` để xem các mục R có bằng chứng. Chỉ kiểm tra/báo lại; chưa tự thêm ACK, watchdog, pairing, preset mapping hay thiết kế lại native queue.

## Dùng và giới hạn

Cài APKcode31 thay code30 bằng cùng khóa ký VLAB; không cần xóa dữ liệu ứng dụng để dùng bản sửa. Giữ bản cũ để đối chiếu. Test sử dụng hồ sơ tạm riêng, không phải dữ liệu học viên thật.

Chưa nạp ESP32, chưa chạy trên điện thoại/Google Cardboard, chưa thử BLE hai tay vật lý, focus/permission Android, latency/jitter hoặc hiệu năng thiết bị. Tia được so sánh bằng `LineRenderer` và ray thực trong Unity; chưa có kiểm tra thị giác qua kính. Chưa chạy lại toàn bộ32 hành trình bài học hoặc tất cả luồng lưu dữ liệu. IMU+joystick hỗ trợ vị trí có giới hạn vẫn không phải tracking vị trí6DoF tuyệt đối.
