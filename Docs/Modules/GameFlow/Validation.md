# Kiểm chứng Game Flow

## Đã thực hiện ngày 05/11/2026 trong Unity Editor

Trong Play Mode, gọi đúng `Button.onClick` của các nút scene để kiểm tra dây nối và trạng thái runtime:

| Bước | Kết quả quan sát |
|---|---|
| Play từ `MainMenu` | Scene đang chạy chuyển sang `ProceduralGame` |
| Pause | Có 1 listener, bảng Pause bật và `Time.timeScale = 0` |
| Options, slider, Quay lại | Options bật, Pause ẩn; slider đổi `AudioListener.volume` và `PlayerPrefs`; Quay lại mở Pause |
| Resume | Pause ẩn, `Time.timeScale = 1` |
| Về Menu | Scene trở về `MainMenu`, `Time.timeScale = 1` |
| Thoát tại Menu trong Editor | Nút có 1 listener và Play Mode dừng |

Ảnh Game View 1920×1080 của `MainMenu` đã được xem trực tiếp; phụ đề được dời khỏi đường nối của nền gỗ. Ảnh gameplay cùng độ phân giải xác nhận nút Pause đã được dời khỏi đồng hồ ngày/đêm. Ảnh được dùng để kiểm tra bố cục trong phiên làm việc và không lưu vào repository.

## Chưa kiểm chứng

Chưa kiểm tra thao tác chuột thật hoặc các độ phân giải khác. Ảnh gameplay chụp từ Editor không phản ánh bảng Pause vừa bật vì Game View chưa cập nhật frame trong lúc gọi công cụ; trạng thái bảng đã được xác nhận bằng component runtime nhưng bố cục Pause cần xem trực tiếp khi Game View đang chạy. Chưa chạy bản build Player hoặc kiểm chứng `Application.Quit` ngoài Editor. Chưa đo CPU/memory của UI. Các kết quả trên là kiểm tra logic qua Unity Editor, không phải số đo hiệu năng hoặc kiểm thử toàn bộ gameplay RTS.
