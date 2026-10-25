# Trải nghiệm học sinh mô phỏng — lượt 25/10/2026

Đây là lượt mô phỏng có cấu trúc trên APK Android trong máy giả lập API35, không phải nghiên cứu với học sinh thật hoặc kiểm chứng điện thoại/kính/tay cầm thật.

## Kết quả quan sát trước sửa

Đi qua menu → hướng dẫn nhanh → Vật lí → Con lắc đơn → mục tiêu → trạm → kiểm tra giá đỡ, gắn dây/quả nặng → chạm mốc cân bằng → kéo/thả quả nặng → đo đủ10chu kỳ → kết quả → sổ. Toàn bộ đoạn này dùng UI, không đọc code để tìm đáp án hay gọi hoàn thành thí nghiệm bằng debug. Kết quả: chu kỳ1,556s,10chu kỳ, thời gian15,556s và gia tốc suy ra9,789m/s².

Bài tách DNA có thao tác ngoài lượt điều khiển ở đầu nên không tính là bằng chứng hoàn thành độc lập. Bước cuốn nhẹ cuối bài, kết quả và sổ được quan sát trực tiếp; dữ liệu DNA vẫn hiện trong sổ sau khi đóng/mở app.

## High — Hướng dẫn làm quen che các nút kết quả

- Tái hiện: mở hướng dẫn nhanh, vào con lắc, thực hiện bài đến kết quả mà không bấm Tiếp qua hết các thẻ giải thích.
- Mong đợi: nút Làm lại, Danh sách thí nghiệm và Xem trong sổ luôn đọc và bấm được.
- Thực tế: thanh hướng dẫn vẫn đè lên hàng nút kết quả. Phải bỏ qua hướng dẫn để nhìn rõ các nút.
- Bản sửa: tour tạm ẩn ngoài màn chính, chọn bài và thực hành; giữ tiến độ tour để trở lại khi phù hợp.
- Kiểm tra hồi quy dùng dữ liệu kết quả mẫu riêng, không thay cho lượt học sinh: trước sửa thất bại; sau sửa qua ở1280×720 và2340×1080. Nút mở sổ nhận được sự kiện chạm thật của Input System; tour không che sổ và hiện lại ở trang chủ.
- [Ảnh trước sửa](before-result-overlap.png), [ảnh sau sửa](after-result-2340.png), [baseline](tour-baseline-checks.txt), [retest](tour-fixed-checks.txt).

## Quan sát cần giữ đúng phạm vi

Thông báo đặt dụng cụ ở đoạn DNA bị gián đoạn chưa đủ để xác nhận một lỗi mới. Kiểm tra độc lập chọn → lấy → trả về khay → đặt bộ phóng trong bài ném xiên thành công, nên không sửa kiến trúc input theo suy đoán này.

Cài đặt → Điều khiển → Đổi nút tay cầm mở được hình tay cầm, chọn tay, góc trước/bên và lựa chọn hành động. Đổi B sang Cầm/thả phản hồi trên UI và đã bấm Áp dụng. Chưa suy ra tín hiệu phần cứng, hiệu quả calibration hay profile hoạt động từ việc nhìn thấy màn hình.

## Retest đúng APK code35

Cài đè code35 giữ nguyên lượt con lắc từ code34 trong sổ: [ảnh sau nâng cấp](code35-upgrade-keeps-notebook.png). Sau đó tạo hồ sơ thử nghiệm sạch và thực hiện lại toàn bộ đoạn con lắc qua UI, giữ tour chưa hoàn tất: chu kỳ 1,554 s, 10 chu kỳ, 15,54 s và g suy ra 9,809 m/s². [Kết quả trên APK](code35-result-fixed.png) không còn thanh tour che các nút; chạm Xem trong sổ mở [đúng số đo](code35-notebook-fixed.png). Đây là PASS giả lập, không phải PASS điện thoại thật.

## Phần chưa hoàn thành của goal

Chưa có lượt học sinh đầy đủ cho32bài, kiểm tra đặt sai/hint-off đầy đủ, nghiệm thu controller/calibration/profiles, retest toàn bộ trên bản cuối hoặc album/video sản phẩm hoàn chỉnh. Video hiện tại là bằng chứng QA thô, không phải video quảng cáo. Không dùng các kiểm tra developer cũ để thay thế các hạng mục này.

Bằng chứng QA thô lưu tại `E:\VLAB\4 LABS\work\student-experience-20261025`. Giới hạn ngân sách lượt này: bắt đầu74% đã dùng, dừng trước khi vượt79%.
