# Tài liệu kỹ thuật RTSgame2026

Các tài liệu này phục vụ phát triển tiếp dự án, bàn giao và giải thích kiến trúc trong khóa luận tốt nghiệp. **Logic thực thể đã có DOTS; các lớp Unit MonoBehaviour hiện có cả vai trò adapter/presentation.** Đọc trạng thái tích hợp trước khi thêm logic vào script cũ.

## Bắt đầu từ đâu?

| Nhu cầu | Tài liệu nên đọc |
|---|---|
| Quay lại dự án hoặc bàn giao cho người khác | [Tech Overview](TechnicalOverview.md) — bản đồ code, nguồn dữ liệu, luồng chạy, giới hạn |
| Tiếp tục tech tree và procedural mở rộng | [Tiến độ/bàn giao](Modules/Progression/Progress.md) — đọc trước; [Build plan S0–S9](Modules/Progression/BuildPlan.md) — phạm vi, hợp đồng, nghiệm thu từng lát cắt |
| Thêm quân, sửa AI/spawner hoặc làm module nối với DOTS | [DOTS Development Guide](Modules/DOTS/DevelopmentGuide.md) — API, điểm mở rộng, lifecycle và chẩn đoán |
| Chạy game/demo, kiểm tra Entity hoặc build | Usage của module tương ứng trong bảng bên dưới |
| Trích kết quả cho khóa luận | Validation và artifact Evidence, kèm ngày và điều kiện thực thi |

## Tài liệu module

**Progression + Procedural mở rộng:** [Build plan](Modules/Progression/BuildPlan.md) và [sổ tiến độ](Modules/Progression/Progress.md). Mới lập kế hoạch ngày 05/11/2026, chưa triển khai gameplay mới; trạng thái từng lát cắt được cập nhật trong sổ tiến độ.

S0 có [hợp đồng kiến trúc](Modules/Progression/Architecture.md), [cách chạy](Modules/Progression/Usage.md) và [kiểm chứng](Modules/Progression/Validation.md); baseline đã chốt, D0 đang triển khai.

| Module | Tài liệu | Nội dung |
|---|---|---|
| Dependency Injection | [Thiết kế và lộ trình](Modules/DependencyInjection/Architecture.md) | Manual DI, composition root, lifecycle Unity/ECS, chuyển đổi D0–D4 và tiêu chí khóa luận; chưa triển khai |
| Dependency Injection | [Inventory D0](Modules/DependencyInjection/Inventory.md) | Baseline 149 vị trí ứng viên/31 file, owner/lifetime, ngoại lệ và lộ trình D1–D4; D0 hoàn tất |
| Dependency Injection | [ADR-001](Modules/DependencyInjection/ADR-001-ManualDI.md) · [Lookup scan](Modules/DependencyInjection/LookupScan-2026-11-09.txt) | Quyết định manual DI và snapshot D0 ngày 09/11/2026 |
| Dependency Injection | [Usage](Modules/DependencyInjection/Usage.md) · [Kiểm chứng](Modules/DependencyInjection/Validation.md) · [D1 Evidence](Modules/DependencyInjection/D1-Evidence-2026-11-10.md) | D1 root/lifecycle và D2 research service đã có; D3–D4 còn lại |
| Progression / DI D2 | [S1-D2 Evidence](Modules/Progression/S1-D2-Evidence-2026-11-11.md) · [Usage](Modules/Progression/Usage.md) | economy_tools ECS, research service/panel qua DI; S2 đã được người dùng nghiệm thu |
| Progression S3 | [S3 Evidence](Modules/Progression/S3-Evidence-2026-11-11.md) · [Catalog S3a lịch sử](Modules/Progression/S3a-Catalog.md) | Sáu tech, modifiers, gates và UI ba nhánh; 58/58 tests và kiểm tích hợp/ảnh |
| DOTS / Swarm | [Kiến trúc](Modules/DOTS/Architecture.md) | ECS, Jobs, Burst, flow field, spatial hash, collision, rendering và cầu nối gameplay |
| DOTS / Swarm | [Hướng dẫn chạy](Modules/DOTS/Usage.md) | Scene mô phỏng, điều khiển, test và build |
| DOTS / Swarm | [Kiểm thử và đo hiệu năng](Modules/DOTS/Validation.md) | Tiêu chí, bằng chứng thực thi và phương pháp benchmark |
| DOTS / Swarm | [Dữ liệu kiểm chứng](Modules/DOTS/Evidence/README.md) | CSV, JSON, ảnh Player và NUnit XML |
| DOTS / Gameplay | [Kiến trúc](Modules/DOTSGameplay/Architecture.md) | ECS authoritative cho dân, quân, cừu, công trình, tài nguyên, xây và huấn luyện |
| DOTS / Gameplay | [Hướng dẫn chạy](Modules/DOTSGameplay/Usage.md) | Tích hợp command/spawner, kiểm tra Entity/view và Player smoke |
| DOTS / Gameplay | [Kiểm chứng](Modules/DOTSGameplay/Validation.md) | Test gameplay, tích hợp SampleScene, giới hạn bằng chứng và checklist |
| DOTS / Gameplay | [Bằng chứng](Modules/DOTSGameplay/Evidence/README.md) | Đợt 16/10/2026: 32 test EditMode và 18 check tích hợp Player |
| Procedural Generation | [Nghiên cứu và đề xuất](Modules/ProceduralGeneration/Research.md) | Lựa chọn thuật toán sinh cỏ/nước, phân bố cây/vàng/cừu và ràng buộc đường đi |
| Procedural Generation | [Kế hoạch triển khai](Modules/ProceduralGeneration/ImplementationPlan.md) | Sáu mốc và trạng thái lát cắt đầu tiên |
| Procedural Generation | [Kiến trúc](Modules/ProceduralGeneration/Architecture.md) · [Cách chạy](Modules/ProceduralGeneration/Usage.md) · [Kiểm chứng](Modules/ProceduralGeneration/Validation.md) | Core xác định theo seed, scene integration, quét 1.000 seed và phần còn phải kiểm chứng trong Unity |
| Procedural Generation / S4 | [Regions + preview](Modules/ProceduralGeneration/S4-Regions.md) | StrategicRegions v2, graph ba vùng/hai cửa/landmark, preview không sửa scene; 10/10 tests, 128 seed mỗi preset |
| Game Flow | [Kiến trúc](Modules/GameFlow/Architecture.md) · [Cách chạy](Modules/GameFlow/Usage.md) · [Kiểm chứng](Modules/GameFlow/Validation.md) | MainMenu, vào ProceduralGame, tạm dừng, tùy chọn âm lượng và thoát |

`DOTS` và `DOTSGameplay` là hai phần tài liệu của cùng nền runtime tại `Assets/Game/DOTS`: phần đầu tập trung Swarm/quái/đạn, phần sau giải thích gameplay RTS. Không coi chúng là hai implementation tách rời cần tự lắp lại.

## Quy ước cho các module tiếp theo

Mỗi module lớn có thư mục riêng dưới `Docs/Modules`, ví dụ `ProceduralGeneration`. Giai đoạn nghiên cứu có thể bắt đầu bằng `Research.md`; khi triển khai, cấu trúc tối thiểu gồm `Architecture.md`, `Usage.md` và `Validation.md`.

Tài liệu cần trả lời: vấn đề gì được giải quyết; dữ liệu nào được lưu; thuật toán nào được dùng và tại sao; module giao tiếp với phần còn lại thế nào; cách tái lập bài chạy; bằng chứng kiểm thử; giới hạn và hướng phát triển.

Không ghi tính năng dự kiến thành tính năng đã có. Mỗi kết quả hiệu năng phải đi kèm số lượng thực thể, kịch bản, cấu hình máy, loại build, độ phân giải và điều kiện bật/tắt hiển thị.

Khi thêm module hoặc đổi ranh giới kiến trúc, cập nhật trang tổng quan và đường dẫn trong danh mục này cùng tài liệu module. Giữ artifact lịch sử rõ ngày; chỉ ghi test/benchmark mới khi đã thực thi và có bằng chứng.

