# Quy ước làm việc trong dự án khóa luận RTSgame2026

- Đây là dự án Unity RTS phục vụ khóa luận tốt nghiệp. Các thay đổi cần có thể giải thích và kiểm chứng được.
- Khi bắt đầu phát triển tiếp, đọc `Docs/TechnicalOverview.md`. Trước khi sửa thực thể, AI, spawner hoặc tích hợp module mới với DOTS, đọc thêm `Docs/Modules/DOTS/DevelopmentGuide.md` và tài liệu Architecture liên quan.
- Mỗi module lớn mới hoặc thay đổi kiến trúc lớn phải có tài liệu riêng trong `Docs/Modules/<Module>/` và được liên kết từ `Docs/README.md`.
- Tài liệu phải mô tả mục tiêu, phạm vi, dữ liệu, luồng xử lý, thuật toán, tích hợp, cách chạy, kiểm thử, đo hiệu năng, giới hạn và nguồn tham khảo.
- Cập nhật tài liệu cùng implementation. Phân biệt chức năng đã triển khai, kết quả đã đo và hướng phát triển; không viết kết quả hiệu năng chưa được kiểm chứng như một thành tích đã đạt.
- Dùng tiếng Việt cho tài liệu giải thích; giữ tên API, class và thuật toán bằng tiếng Anh khi cần đối chiếu code.
- Module DOTS hiện tại nằm trong `Assets/Game/DOTS`. Runtime assembly `RTS.DOTS` độc lập với các lớp gameplay MonoBehaviour; cầu nối nằm trong `Integration`.
- Logic quái/đạn và gameplay dân, quân, cừu, công trình, khai thác, xây, huấn luyện đã có ECS. Kiểm tra `IsDotsControlled` và command/system tương ứng trước khi thêm behavior vào `Unit`/`AIPawn` cũ; GameObject còn trong Hierarchy có thể chỉ là presentation.
- Giữ các prefab và gameplay đang có khi chuyển từng phần kiến trúc; chạy kiểm thử phù hợp và xác nhận bằng Unity khi có thể.
- Không tự nâng Unity Editor hoặc đổi render pipeline toàn dự án chỉ để triển khai một module.

