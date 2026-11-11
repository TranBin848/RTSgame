# Progression — hợp đồng kiến trúc ban đầu

Ngày 11/11/2026. **S1–S3 và D2 hoàn tất phạm vi kỹ thuật; S2 được người dùng nghiệm thu.** Kế hoạch chi tiết: [BuildPlan](BuildPlan.md); điểm tiếp tục: [Progress](Progress.md).

## Trạng thái mới nhất S3

Catalog gồm sáu node và prerequisite; UnlockMask lưu trên WalletEntity. Cargo/training/tower modifiers có trường riêng và đọc theo unlock. Gate được đặt tại build/train action, manager, bridge và command ECS có capability data; SampleScene không configure research giữ nội dung sẵn có. UI sáu node cùng nhận IResearchService, query definition/unlock theo ID. Xem [S3 Evidence](S3-Evidence-2026-11-11.md) cho dữ liệu, thuật toán, vị trí tích hợp, cách chạy, 58/58 tests và giới hạn. Các mô tả S0/S1 phía dưới là lịch sử.

## Trạng thái mới nhất S1/D2

Root tạo ResearchService và inject IResearchService vào ResearchPanel. GameplayResearch.cs giữ command/state/tick trên WalletEntity; EconomyResearchDefinition chỉ là cấu hình, EconomyTools.asset không bị ghi unlock lúc chơi. Economy job đọc modifier runtime, giữ profile gốc cho cả worker hiện có/sinh sau. Thiếu tiền/ID sai/hall không hợp lệ/đang bận/đã unlock không debit. Hall mất hủy không refund, pause dừng timer, ResetGameplay xóa tiến trình. Chỉ một tech gốc; catalog graph nhiều node thuộc S3. Chi tiết [S1-D2 Evidence](S1-D2-Evidence-2026-11-11.md) gồm data, rounding và kết quả 43/43 tests.

Các mục mô tả S0 phía dưới là lịch sử đối chiếu, không thay thế trạng thái mới này.

## Mục tiêu và phạm vi

Thêm nghiên cứu cho vòng RTS khai thác → nâng cấp → chống wave, dùng PlayerWallet ECS làm nguồn tiền. S0 chỉ đối chiếu hiện trạng và chốt đường tích hợp; class/command research chưa được tạo.

## Dữ liệu và luồng hiện tại đã đối chiếu

- `GameplaySimulationSystem.WalletEntity` chứa `PlayerWallet`; `GameManager.Gold/Wood/Meat` đọc từ ECS khi bridge sẵn sàng. `GameManager.Start` cộng 100 mỗi loại làm tiền đầu trận thử nghiệm. Đây là giá trị code, đã thấy 100/100/100 trong Play Mode của ProceduralGame.
- `GameplayEconomyJob` tăng hàng mang theo `GatheringProfileData.Amount`, giới hạn bởi `Capacity`, rồi nộp vào wallet. `ResourceNodeBase` và `Tree`/`GoldStone` không có trữ lượng; claims là số worker tối đa. Chưa thấy trừ trữ lượng node trong đường ECS. Cừu đăng ký resource `RequiresKill=1`, phải chết trước khi khai thác thịt. Cần kiểm chứng lifecycle cừu qua Play Mode trước khi kết luận thịt có giới hạn thực tế.
- `ProductionBuilding.EnqueueProduction` chuyển sang `SwarmGameBridge.EnqueueTraining` nếu DOTS đang điều khiển; legacy giữ Queue riêng. `GameplaySimulationSystem.EnqueueTraining` thêm `TrainingRequest` với duration đã chốt ở thời điểm enqueue.
- `EnemyWaveManager` chọn điểm trên vòng quanh TownHall, thử điểm walkable trên Tilemap, rồi gọi bridge SpawnEnemy hoặc pool legacy. Wave được đếm theo ngày từ `DayNightCycleManager`; hiện chưa có gateway/region spawn do generator cung cấp.
- `ProceduralMapAuthoring.Awake` sinh và áp map, sync Physics2D rồi `TilemapManager.ReinitializePathfinding`; `ProceduralMapRuntime` chặn tick ngày đêm/wave đến Ready. Bridge vẫn có đường refresh navigation riêng. Thứ tự đầy đủ khi bắt đầu/đổi scene cần kiểm tra ở D1.
- `GameManager.TryFindClosestTownHall` loại TownHall chết. Quét `Assets/Game/**/*.cs` chưa thấy flow thắng/thua/EndGame rõ; S8 cần thiết kế điều kiện kết thúc. `Unit.Die()` unregister Unit nhưng chưa đồng nghĩa trận thua.

## Hợp đồng dự kiến cho S1

Research bắt đầu bằng command ở bridge/System, xác minh TownHall, prerequisite, slot và số dư; kiểm tra và trừ ví trong cùng đường xử lý. Dùng trạng thái ECS cho research và modifier; UI/MonoBehaviour chỉ gửi lệnh và hiển thị snapshot. `RTS.DOTS` không tham chiếu các lớp presentation. D0/D1 dependency injection hoàn tất trước S1 theo [thiết kế DI](../DependencyInjection/Architecture.md).

Mốc playtest 5 ngày và giá sáu tech trong BuildPlan vẫn là giả thuyết. Vì tiền đầu trận hiện 100 mỗi loại, một số tech giá thử có thể mua ngay; cần chỉnh sau khi test vòng chơi, không gọi đó là cân bằng đã đạt.

## Kiểm thử, đo hiệu năng và giới hạn

S0 có kết quả EditMode và Play Mode trạng thái khởi động trong [Validation](Validation.md). Chưa có benchmark progression. S1 phải test giao dịch ví và modifier; S8 mới đo tốc độ tích lũy theo ngày. Giới hạn hiện tại: chưa có save/load, điều kiện kết thúc nghiên cứu, UI và xác minh tương tác thực trong Play Mode.

Nguồn nội bộ: [TechnicalOverview](../../TechnicalOverview.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md), [DOTSGameplay Architecture](../DOTSGameplay/Architecture.md), `GameplaySimulationSystem.cs`, `GameplayEconomyJob.cs`, `GameplayComponents.cs`, `GameplayGameBridge.cs`, `GameManager.cs`, `ResourceNodeBase.cs`, `SheepUnit.cs`, `EnemyWaveManager.cs`, `ProceduralMapAuthoring.cs`.
