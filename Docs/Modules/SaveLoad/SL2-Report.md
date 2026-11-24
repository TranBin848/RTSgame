# SL2 — Slot storage

Ngày24/11/2026, DONE phạm vi Core/storage Editor. Hai slot enum, generation tăng, temp cùng directory, Flush(true), verify trước replace/move, backup thế hệ tốt và operation lock. Có fault probe từng boundary commit; không xóa primary trước commit. Backup read có thông tin fallback; đọc không sửa file lỗi. [Architecture](Architecture.md) mô tả thuật toán/API, [Usage](Usage.md) cách chạy và [báo cáo nền tảng](SL0-SL2-Report.md) kết quả.

Chưa đo mất điện trong syscall hoặc Windows Player. Storage lock không thay global mutation fence/operation owner xuyên scene của SL3/SL4/SL7. Chưa có caller tạo file save trận thật trong persistentDataPath.
