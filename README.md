# RTSgame2026

Dự án Unity RTS 2D phục vụ khóa luận tốt nghiệp. Unity Editor đang khai báo: **6000.3.11f1**; render pipeline URP 2D.

**Dự án đã triển khai DOTS cho logic thực thể gameplay**, gồm quái/đạn và dân, quân người chơi, cừu, công trình, khai thác, xây dựng, huấn luyện. Prefab/GameObject còn giữ vai trò authoring, hiển thị, chọn đối tượng và nối UI với Entity.

## Đọc trước khi phát triển tiếp

1. [Tech Overview và bàn giao dự án](Docs/TechnicalOverview.md): trạng thái kiến trúc, bản đồ code, nguồn dữ liệu và luồng chạy.
2. [Hướng dẫn phát triển DOTS](Docs/Modules/DOTS/DevelopmentGuide.md): API spawn/command, nơi sửa AI, thêm loại quân và tích hợp module sau này.
3. [Danh mục tài liệu module](Docs/README.md): Architecture, Usage, Validation và bằng chứng của từng phần.
4. [Quy ước làm việc](AGENTS.md): yêu cầu cập nhật docs cùng implementation.

## Chạy dự án

- Gameplay: mở `Assets/Scenes/SampleScene.unity` và Play sau khi compile xong.
- Mô phỏng quái/đạn: `Assets/Scenes/SwarmSimulation.unity` hoặc menu `RTS/DOTS/Open swarm simulation`.
- [Chạy gameplay và kiểm tra Entity/view](Docs/Modules/DOTSGameplay/Usage.md).
- [Chạy Swarm, test, build và benchmark](Docs/Modules/DOTS/Usage.md).

Các kết quả test/hiệu năng đã lưu có ngày và phạm vi riêng trong Validation. Không dùng benchmark quái/đạn làm kết quả tải của toàn bộ nền kinh tế RTS. Các module tương lai như procedural generation cần tài liệu và bằng chứng riêng khi được triển khai.
