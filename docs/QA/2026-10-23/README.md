# Kiểm tra Editor — 23/10/2026

Đợt này cập nhật source sau candidate **0.1.7 / code21**. **Chưa build APK mới.** Bằng chứng dưới đây được thu trong Unity Editor 6000.6.4f1 ở project làm việc trước khi đồng bộ lên repository.

| Phạm vi | Kết quả | Bằng chứng |
|---|---|---|
| Hộp thoại và lớp nền | 36 PASS, 0 FAIL; các nhánh ở lại, bỏ thay đổi, áp dụng; 10 vòng chuyển chế độ mô phỏng | [Biên bản](dialog-checks.txt) |
| Đọc PDF Việt / Anh | 135 PASS, 0 FAIL; tải đủ 47 trang, đối chiếu SHA PDF gốc, chuyển trang, zoom và ẩn/hiện | [Biên bản](legal-checks.txt) |
| Màu tham chiếu UI | Mẫu màu flat/world trùng nhau trong Editor | Chẩn đoán trong biên bản hộp thoại; không chứng minh màu trên Cardboard native |

## Ảnh từ ứng dụng đang chạy trong Editor

![Hộp thoại xác nhận](dialog-stay.png)

![Chính sách bảo mật từ PDF gốc](pdf-privacy_vi.png)

## Giới hạn và chạy lại

Đây là kiểm tra callback và trạng thái UI trong Editor, không thay thế thao tác đa chạm, cảm biến điện thoại, Cardboard native, BLE hay hành trình đầy đủ của 32 thí nghiệm. Không dùng các số PASS trên làm chứng nhận sản phẩm hoàn tất.

Các entry point Editor: `FinalPassUiChecks.Run` và `FinalPassLegalChecks.Run`. Probe dùng hồ sơ QA riêng. Kết quả chạy lại được ghi vào `Evidence/FinalPass/`; không dùng hồ sơ học sinh để chạy thử. Các receipt ở trang này được giữ nguyên từ lần chạy đã ghi nhận, trước thay đổi đường dẫn đầu ra của probe.

Giữ nguyên vị trí và GUID các asset Unity. `docs/archive/README-2026-09-12.md` là tài liệu lịch sử, không phải hướng dẫn mở project hiện tại.
