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

## Chưa phát hành — tương tác và hướng dẫn

- [34 PASS, 0 FAIL](interaction-checks.txt) qua UI thực của ứng dụng, pose XR-HMD mô phỏng qua Input System/Tracked Pose Driver và cảm ứng được đưa vào Input System. Không gán sẵn trạng thái hoàn thành.
- P01: chọn bằng gaze, kéo quả thật theo ràng buộc chiều dài, chọn Thả; kéo/thả bằng cảm ứng. P02: cầm và hủy. C04: chọn/cầm ống nhỏ giọt và hủy khi mở UI. B01: ba thao tác chuẩn bị riêng. M01: chọn motor để chạy. Đây là phạm vi một phần của năm bài, không phải 32 hành trình hoàn chỉnh.
- Gợi ý tại dụng cụ: nhãn tối giản sau khi chờ, chỉ dẫn mở rộng khi mắc kẹt hoặc sau ba lần sai; thao tác đúng đặt lại mức hỗ trợ. Ô chữ tính đủ khoảng đệm, kiểm tra không cắt chữ.
- Hồi quy hộp thoại trên source cuối: [36 PASS, 0 FAIL](dialog-checks.txt). Sửa cleanup Game View QA để bỏ đúng mục do test tạo; không xóa các mục của người dùng.
- [Hash source tại checkpoint](interaction-source-sha256.json). Chưa có APK mới; Android/Cardboard/BLE phần cứng và kiểm thử đầy đủ 32 bài vẫn chưa đạt nghiệm thu.

![Kéo con lắc bằng gaze mô phỏng](P01-manual-pull.png)

![Phone mode sau kéo/thả cảm ứng](P01-phone-drag-release.png)

## Góc nhìn điện thoại và lượt kiểm tra 32 bài

- [37 PASS, 0 FAIL](visibility-checks.txt) trong probe tương tác/góc nhìn: quả lắc tại viewport 1600 × 900 tăng từ khoảng 17 lên 67 pixel khi tự căn góc theo bước. Không đổi tỉ lệ model; camera giữ vị trí khi đang kéo. Không ép di chuyển origin VR trong fixture XR mô phỏng.
- [Ma trận 32 bài](journeys32-matrix.md) · [Receipt JSON](journeys32-matrix.json): 26 bài lưu và mở lại kết quả, 6 bài bị chặn, không có runtime error được probe ghi nhận. Có hỗ trợ chuẩn bị trong một số bài; kết quả này không xác nhận toàn bộ hành trình tự thao tác của 32 bài.
- C01 vướng điều hướng trang dụng cụ của test; C04 vướng điều kiện điều hướng trong runner; C07 dừng ở nút ghi chưa bật, cần tái hiện. B01/B02/B08 hết thời gian kiểm tra/phân loại bằng mắt. Chưa đủ bằng chứng để quy tất cả blocker này thành lỗi ứng dụng.
- Kiểm tra riêng còn **FAIL** đường viền cho dụng cụ chỉ có LineRenderer. Các lỗi và giới hạn này vẫn mở, không bị loại khỏi báo cáo để đổi kết quả thành PASS.
- Receipt giữ nguyên kết quả; riêng đường dẫn tuyệt đối đến hồ sơ QA được thay bằng `<isolated QA profile>` khi đưa vào tài liệu công khai. Không có hồ sơ người học trong repo.

[SHA256 source tại checkpoint](visibility-source-sha256.json). Đây là các lần chạy Editor đã hoàn tất, không phải kết quả Android/Cardboard/BLE phần cứng hay APK mới.

## Chưa phát hành — bảng kính hiển vi và tương tác

- Tách **Phân loại mẫu / Chỉnh kính**: sáu nhãn B01/B08 hiện cùng lúc, B02 có hai lựa chọn; chỉnh nét/vật kính vẫn truy cập được. Chạm ô giữ nguyên bảng và ảnh; sửa caption tab, nhãn dài và phần thống kê bị cắt.
- B02 trước đó chỉ lấy texture chất nguyên sinh, bỏ mất lớp thành tế bào có sẵn. Ô quan sát nay xếp cả hai lớp cùng UV; thành cố định, chất nguyên sinh thay đổi bên trong. Không sửa model, texture hay GUID nguồn.
- [24 kiểm tra UI](classifier-layout-checks.txt), [38 hồi quy tương tác](classifier-input-checks.txt), [36 hồi quy hộp thoại](classifier-dialog-checks.txt): không FAIL trong các receipt này. UI kính hiển vi mới được kiểm tra ở VI1600×900; chưa chứng minh EN, mọi aspect ratio, native Android hay stereo.
- Lượt [C04/C07](classifier-chemistry-matrix.json), [B01/B08 trước sửa typography cuối](classifier-biology-pre-wall-matrix.json), [B02 sau sửa wall](classifier-B02-matrix.json) đã lưu/mở lại kết quả theo phạm vi riêng. B02 của receipt trước-wall vẫn BLOCKED và được giữ nguyên. Một số chuẩn bị có hỗ trợ; không coi đây là chứng nhận32bài tự thao tác hoàn chỉnh.
- Các thao tác phân loại dùng điểm/nhãn quan sát từ PNG runtime rồi đưa vào UGUI bằng touch; không đọc đáp án ẩn hay gán trạng thái hoàn thành. B02 vẫn là các trạng thái hình minh họa từ SDK, chưa phải mô hình động học thẩm thấu đã hiệu chuẩn.
- [SHA256 source](classifier-source-sha256.json). Chưa có APK mới từ batch này.

![B01: đủ nhãn và chữ không bị cắt](microscope-B01.png)

![B08: sáu lựa chọn cùng hiện](microscope-B08.png)

B02 cùng trạng thái muối minh họa1 trong Editor, trước/sau sửa lớp hiển thị (layout field có thay đổi nhỏ):

![Trước: thiếu thành tế bào](B02-wall-before.png)

![Sau: thành cố định và chất nguyên sinh tách bên trong](B02-wall-after.png)

### C01 và phạm vi tổng hợp

[C01](classifier-C01-matrix.json) đã đạt ba lượt hợp lệ và lưu/mở lại kết quả qua UI, không có runtime error hay scoped regression FAIL trong lượt này. Kịch bản dùng van qua UI, trộn, chờ chỉ thị đủ30giây theo đồng hồ mô phỏng, đọc mặt khum và giữ từng lượt. [Ảnh kết quả](C01-results.png) hiện ba lượt hợp lệ.

[Ma trận tổng hợp](ui-journeys-aggregate.json) ghi nhận **32/32 bài có lượt lưu/mở lại** qua nhiều checkpoint Editor. Đây không phải một lượt hồi quy32bài trên cùng source cuối; chuẩn bị có hỗ trợ và các giới hạn khoa học vẫn được giữ. Không dùng số32này thay nghiệm thu Guided Freedom hoặc phần cứng.

Những lần C01 điều hướng nhầm caption/trang hoặc hết bound600giây vẫn nằm trong các thư mục evidence gốc. Lượt thành công dùng bound1200giây; runner cho phép chọn30–1800giây, mặc định180giây. Các điều kiện vật lý, ba lượt chuẩn độ và lưu kết quả không bị nới.

[UI operator C01](drive_c01_ui.py) chỉ dành cho fixture VI mặc định đã kiểm tra(0.1mol/L,10mL). Nó đưa thao tác chạm vào protocol hiện có, dựa trên lưu lượng đã quan sát trên UI; chỉ lặp khi receipt có thao tác giữ lượt/mẫu mới, tối đa ba pulse rồi dừng để rà soát nếu chưa đạt. Không đọc trạng thái hóa học, gán đáp án hay gán hoàn thành; không phải kiểm thử tay cầm/điện thoại thật.

## Đồ họa và Settings responsive

[Bốn mức đồ họa, số đo frame thực và kiểm tra layout Việt/Anh](graphics/README.md). Source checkpoint, chưa có APK mới; phạm vi Editor được ghi riêng cho từng lượt.
