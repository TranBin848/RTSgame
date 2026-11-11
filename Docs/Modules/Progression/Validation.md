# Progression — kiểm chứng S0

Ngày 05/11/2026. Trạng thái S0 **DONE** theo phạm vi baseline; không có tính năng progression mới trong project.

Cập nhật 11/11/2026: S1/D2 có economy_tools trong ECS, config asset và panel được inject. Code cuối đạt **43/43 DOTS EditMode**, service transaction, fake UI lifecycle và accelerated completion PASS. Xem [S1-D2 Evidence](S1-D2-Evidence-2026-11-11.md). S2 chưa nghiệm thu click/hình ảnh và thời gian tự nhiên; không dùng baseline S0 bên dưới làm bằng chứng cho UI mới.

Cập nhật tiếp 11/11/2026: người dùng xác nhận toàn bộ checklist S2 đều ổn (UI, click/trừ tiền, pause/hoàn tất, trận mới, thiếu tiền). **S2 DONE theo xác nhận thủ công**; chưa có ảnh/video, resolution cụ thể hoặc số liệu độc lập kèm báo cáo. Nội dung này bổ sung acceptance, không thay đổi kết quả Test Runner S1.

S3a ngày 11/11/2026: **50/50 DOTS tests pass**, scene API economy_tools vẫn trừ đúng 40 gỗ/15 vàng. [Chi tiết catalog](S3a-Catalog.md). S3 tổng thể PARTIAL, hiệu ứng và gate năm node còn lại chưa có bằng chứng.

S3b/S3 hoàn tất tiếp trong ngày 11/11/2026: **58/58 DOTS tests pass**, full six-node integration, locked command/debit, scene reset, fake panel và legacy SampleScene PASS. Đã chụp/kiểm UI ở 1920×1080. [S3 Evidence](S3-Evidence-2026-11-11.md) là trạng thái mới nhất; không dùng đoạn S3a lịch sử làm checkpoint hiện tại.

### Chạy lại có artifact ngày 05/11/2026

Unity EditMode `RTS.DOTS.Tests` đạt **34/34**; `RTS.ProceduralGeneration.Tests` đạt **3/3**. Build Windows Player mới từ code hiện tại thành công theo Editor log; gameplay smoke trên build đó đạt **18/18 checks**, `errors=[]`. Chi tiết môi trường, giới hạn và SHA-256 build nằm tại [Evidence/baseline-rerun-2026-11-05.md](Evidence/baseline-rerun-2026-11-05.md); [JSON smoke](Evidence/gameplay-smoke-current-2026-11-05.json) được lưu cùng tài liệu. Test Runner chưa xuất XML.

### Xác nhận của người dùng

Người dùng đã tự kiểm tra trực tiếp trong Unity và xác nhận các mục được yêu cầu đều đạt: khai thác/nộp cây, vàng, cừu; xây và huấn luyện; Pause/Resume; wave đầu tiên có quái tấn công. Đây là **báo cáo kiểm tra thủ công của người dùng**, chưa có ảnh, video, Console log, seed hay số liệu chi tiết kèm theo. Không gộp báo cáo này vào kết quả Test Runner hoặc gọi là kiểm tra tự động. Kết quả đủ để chốt baseline chức năng cho S0; S9 cần tái chạy kịch bản có artifact khi nghiệm thu khóa luận.

## Đã chạy

### Bổ sung Play Mode trong phiên tiếp theo ngày 05/11/2026

- Mở MainMenu rồi Play; gọi `GameFlowMenu.Play()` chuyển sang ProceduralGame. Scene sinh 120 Tree, 18 SheepUnit, có 3 WorkerUnit; map Ready và ví vẫn 100/100/100. Đây là gọi API của nút Play, chưa phải click chuột thật.
- Gọi `WorkerUnit.TryAssignResourceNode` cho cây gần một worker: trả `true`, worker chuyển `Moving`. Sau các lần quan sát tiếp theo, Wood vẫn 100 và worker vẫn Moving; chưa xác nhận được nộp gỗ. `Time.time` trả 0 trong phiên chạy qua tool dù Editor báo `isPlaying=true`, `isPaused=false`, nên không dùng khoảng chờ ngoài tool làm bằng chứng simulation đã tiến thời gian.
- Gọi `GameFlowMenu.Pause()` và `Resume()`: `Time.timeScale` lần lượt là 0 và 1. Đã thoát Play Mode. Chưa xác nhận timer ECS/ngày đêm dừng và tiếp tục đúng.
- Không thay scene asset trong Play Mode.

- Unity Editor kết nối với project RTSgame2026; scene mở `ProceduralGame.unity` ở trạng thái không dirty trước test.
- EditMode Test Runner, chọn `GameplaySimulationTests` và `ProceduralMapGeneratorTests`: **24/24 Passed, 0 Failed, 0 Skipped**, thời gian tool báo **6,895 giây**. Đây là kết quả baseline của hai fixture, không phải toàn bộ test suite. Chưa lưu NUnit XML.
- Vào Play Mode trên `ProceduralGame`: `ProceduralMapAuthoring.IsReady = true`; GameManager có `SwarmGameBridge`; `GameManager.Gold/Wood/Meat = 100/100/100`; `EnemyWaveManager.CurrentWaveIndex = 0`, `StartedWaveCount = 0` tại snapshot. Đã thoát Play Mode. Không đo thời gian scene→Ready.
- Kiểm tra code xác nhận seed mặc định 1, nguồn ví ECS, logic claims tài nguyên, cách queue training và điểm spawn theo vòng TownHall. Chi tiết trong [Architecture](Architecture.md).

## Giới hạn còn lại

- Báo cáo người dùng không nêu chi tiết cừu sau chết được khai thác bao nhiêu lần hoặc số dư từng giao dịch. Chưa có artifact để kết luận cân bằng hoặc số lượng thịt hữu hạn; S1/S8 phải test đúng phần này khi cần.
- Quét code `Assets/Game/**/*.cs` chưa tìm thấy flow thắng/thua hoặc EndGame rõ ràng; `Unit.Die()` đổi state Dead và unregister. Vì thế mốc sống 5 ngày vẫn là mục tiêu playtest đề xuất, điều kiện kết thúc phải triển khai ở S8. Không ghi đây là tính năng đã có.
- Build Player, toàn bộ Test Runner, performance, số Entity đăng ký và độ chính xác của ví sau thao tác.

S0 kết thúc với baseline Unity tự động 24/24, Play Mode khởi động do tool quan sát và kiểm tra chức năng thủ công do người dùng xác nhận. Thiếu artifact và các quan sát chi tiết được chuyển thành điều kiện kiểm chứng của S8/S9, không bị trình bày thành kết quả đã đo.
