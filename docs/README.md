# Tài liệu VLAB

[Giới thiệu và cài đặt](../README.md) · [Mã nguồn](https://github.com/thieen209/VLAB)

## Bắt đầu

- [README: cài APK, mở Unity và build development](../README.md#cài-đặt--sử-dụng)
- [Google Cardboard: vào/thoát VR và cấu hình kính](CARDBOARD_ENTRY.md)
- [Tay cầm, kết nối và giới hạn tích hợp](CONTROLLER_CONNECTION.md)
- [Nguồn nghiên cứu và phạm vi sản phẩm](SOURCE_TO_PRODUCT.md)

## Phiên bản và kiểm thử

[Nhật ký thay đổi](../CHANGELOG.md) ghi riêng thay đổi source và bản APK đã build.

Các báo cáo giữ phạm vi và ngày kiểm tra riêng; số PASS của Editor không thay thế kiểm thử Android hoặc phần cứng.

| Phiên bản | Tài liệu |
|---|---|
| 0.1.12 / code32 | [Workbench, tour, âm thanh và kiểm tra](Workbench/HANDOFF.md) |
| Source cập nhật 23/10/2026 — chưa build APK | [Tương tác, góc nhìn, kính hiển vi, hộp thoại và PDF](QA/2026-10-23/README.md), [tổng hợp lượt lưu/mở lại 32 bài](QA/2026-10-23/ui-journeys-aggregate.json) |
| 0.1.7 / code21 — candidate build trước các sửa đổi ngày 23/10 | [Báo cáo](QA/Prototype-0.1.7/README.md), [Cài đặt](QA/Prototype-0.1.7/SETTINGS-LEDGER.md), [APK manifest](QA/Prototype-0.1.7/ARTIFACT.json) |
| 0.1.6 / code17 | [Báo cáo trước](QA/Prototype-0.1.6/REPORT.vi.md) |
| 0.1.4 / code5 — bản cũ trên Releases | [Patch 0.1.4](PATCH_014.md) |
| Các bản trước | [0.1.3](SECONDTEST_013.md), [0.1.2](PATCH1_012.md), [0.1.1](PROTOTYPE_011_CORRECTIONS.md) |

Các biên bản cũ ở [giới hạn nghiên cứu](KNOWN_LIMITS.md), [trạng thái build](BUILD_STATE.md) và [kiểm thử lưu Windows](VERIFICATION_119_WINDOWS_PERSISTENCE.md) vẫn được giữ; đọc mốc phiên bản trước khi áp dụng.

[Ma trận nghiệm thu đang mở](FinalPass/acceptance.json) theo dõi yêu cầu còn lại. Mỗi kết quả phải đọc cùng phạm vi và checkpoint; tổng hợp nhiều lần chạy không thay thế kiểm tra đầy đủ trên một APK.

## Thiết kế và nội dung học

- [Định hướng menu](MENU_ART_DIRECTION.md), [nguồn tài nguyên UI](UI_ASSET_PROVENANCE.md)
- [Độ chính xác so với brief](BRIEF_ACCURACY_AUDIT.md), [phạm vi kế hoạch](PLAN_COVERAGE_AUDIT.md)
- [Hệ thống hướng dẫn](GUIDANCE_QUALITY_AUDIT.md), [nguồn học tập](LEARNING_SOURCE_AUDIT.md), [đối chiếu tài liệu](LEARNING_REFERENCE_CHECKS.md)
- [Kiểm tra thí nghiệm](EXPERIMENT_FINAL_AUDIT.md), [mô hình khoa học dẫn xuất](DERIVED_SCIENCE_FIXTURES.md), [giá đỡ M06](DERIVED_M06_FIXTURE.md)

## Cấu trúc repository

| Thư mục | Nội dung |
|---|---|
| `Assets/VLABProduct/` | Ứng dụng, giao diện, logic thí nghiệm và công cụ build |
| `Assets/Labs/` | Scene và mô hình bốn phòng thí nghiệm |
| `Assets/VLABKit/` | Bộ dụng cụ và thành phần SDK |
| `Assets/XR/`, `Assets/Settings/` | Cấu hình XR và đồ họa |
| `Packages/`, `ProjectSettings/` | Dependency, phiên bản Unity và cấu hình dự án |
| `Assets/StreamingAssets/Legal/` | PDF Chính sách bảo mật / Điều khoản gốc, tiếng Việt và tiếng Anh |
| `Assets/VLABProduct/Resources/LegalPdf/` | Trang PDF đã render và manifest để đọc ngoại tuyến trong ứng dụng |
| `docs/QA/` | Báo cáo, ảnh và nhận diện artifact theo phiên bản |
| `docs/FinalPass/` | Ma trận yêu cầu và checkpoint công việc còn mở |
| `docs/assets/` | Hình dùng trong tài liệu, gồm banner README gốc |
| `docs/archive/` | Tài liệu lịch sử: [README tháng 9](archive/README-2026-09-12.md) được giữ nguyên để đối chiếu; hướng dẫn cũ không áp dụng cho bản hiện tại |
| `Evidence/` | Biên bản kiểm thử đã có trong lịch sử dự án |

Giữ nguyên vị trí asset và file `.meta` để bảo toàn GUID/reference của Unity. Cache, bản build, scene recovery, khóa ký và hồ sơ người học không thuộc mã nguồn công khai. Khóa phát hành của nhóm không cần thiết để mở project hoặc build debug.
