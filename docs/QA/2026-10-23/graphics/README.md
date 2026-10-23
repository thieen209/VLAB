# Đồ họa và số đo — source checkpoint 23/10/2026

Chưa build APK từ batch này. Mọi số đo dưới đây là Unity Editor Windows, Direct3D12, RTX 4050 Laptop GPU, 1600 × 900, target 60 FPS. Mỗi bàn được lấy mẫu tĩnh 10 giây sau warm-up; không phải kiểm tra nhiệt, thao tác dài, Cardboard native hay hiệu năng Android.

## Thay đổi

| Mức | Render scale | Shadow map | Cascades | MSAA | Bóng |
|---|---:|---:|---:|---:|---|
| Low | 0,75 | 512 | 1 | 2× | Tắt |
| Medium | 0,85 | 512 | 1 | 2× | Cứng |
| High | 1,0 | 1024 | 2 | 4× | Mềm |
| Ultra | 1,0 | 2048 | 4 | 4× | Mềm, chi tiết hơn |

Ánh sáng và model giữ cùng tỉ lệ/cường độ giữa các mức; Low không làm tối bàn. Cấu hình URP runtime được dùng lại, trả về asset gốc khi app bị hủy. Gợi ý mức là ước tính RAM/GPU, không cam kết 60 FPS; không tự đổi lựa chọn đã lưu.

Bảng FPS đo khoảng cách giữa các LateUpdate bằng đồng hồ thực, cửa sổ tối đa 600 frame; có mean, p50/p95 và CPU/GPU từ FrameTimingManager khi backend cung cấp. `—` là không có số đo, không phải 0 ms. CPU frame time bao gồm thời gian chờ. Không đo motion-to-photon hoặc độ trễ BLE. Toggle thực sự bật/tắt lấy mẫu. Counter hiển thị ở Home/lab, tránh các nút; tạm ẩn khi chọn bài hoặc mở modal, vẫn lấy mẫu khi đang bật.

Ultra dùng `graphics=2` cùng cờ bổ sung để reader cũ vẫn chấp nhận profile schema 2; rollback về app cũ có thể trở lại High và bỏ hai cài đặt mới, không cần xóa tiến độ. Test lưu giữ ghi chú và đọc payload kiểu cũ; chưa phải thử downgrade APK trên máy thật.

## Bằng chứng

- [8 kiểm tra baseline](baseline-checks.txt) trước thay đổi tier; render scale Medium 0,85. Các JSON `graphics-baseline-*.json` chứa khoảng frame thực. Không dùng baseline này để tuyên bố tăng FPS.
- [183 PASS, 0 FAIL](quality-metrics-checks.txt): queued touch chọn/Apply/lưu và đọc lại bốn mức; bốn bàn Low/Ultra, giữ session/bước, không tạo lại pipeline ở Apply không liên quan; gợi ý cần Apply; toggle; 10 vòng mô phỏng stereo↔phone, callback pause. Lượt này trước chỉnh giới hạn chiều cao/underline cuối của PhoneSettingsLayout.
- [95 PASS, 0 FAIL trên layout cuối](layout-final-checks.txt): VI/EN, chữ lớn, 1600×900, 2340×1080, 2400×1080, 1080×2340, vùng notch; bounds/căn giữa, caption không overflow, bốn target bằng nhau, underline trong ô, scroll tới từng lựa chọn, kiểm tra lưu và vị trí counter không che nút ở phone/world simulation. Offscreen render và viewport override trong Editor; thao tác chạm đã thử ở 1600×900, không giả nhận đã thử đa chạm trên từng điện thoại.
- Hồi quy sau thay đổi runtime cuối: [38 kiểm tra tương tác](interaction-checks.txt), [36 hộp thoại](dialog-checks.txt), không FAIL trong receipt. Sensor/BLE thật chưa được chứng minh.
- [Lỗi layout được giữ](layout-before-fix-checks.txt): mô tả tier bị cắt, Appearance tràn ở portrait, nút gợi ý quá thấp. Đã sửa bằng bố cục, không thu nhỏ toàn bộ chữ. [Hash source cuối](source-sha256.json).

## Mẫu số đo Low / Ultra

Cùng vị trí và hướng camera trong từng cặp; raw JSON có mẫu frame, GPU/CPU mẫu cuối, thiết bị và thời lượng. FPS là trung bình cửa sổ; p95 có đơn vị ms. Chênh lệch nhỏ ở FPS giới hạn 60 không chứng minh tối ưu thành công.

| Bàn | Low: FPS / p95 ms | Ultra: FPS / p95 ms |
|---|---:|---:|
| P01 | 59.8 / 19.9 | 59.8 / 20.4 |
| C01 | 59.7 / 20.4 | 59.8 / 19.7 |
| B01 | 59.8 / 19.0 | 59.8 / 19.7 |
| M01 | 59.6 / 20.1 | 59.9 / 19.7 |

Ảnh `graphics-tier0-*.png` và `graphics-tier3-*.png` phủ cả bốn bàn. Ảnh Settings VI/EN có trong thư mục này. Tổng quát hóa sang Android, kiểm tra thermal và đánh giá chữ trong kính vẫn mở ở R11/R02/R01.

Chạy lại từ root Unity bằng Editor 6000.6.4f1, `-batchmode -projectPath <root> -executeMethod FinalPassGraphicsChecks.Run -logFile <log>`. `VLAB_GRAPHICS_LAYOUT_ONLY=1` chạy nhánh layout riêng; nó không chạy lại tám lượt performance. Kết quả ở Evidence/FinalPass, dùng profile QA riêng. Không dùng số PASS để thay nghiệm thu toàn bộ 32 bài.
