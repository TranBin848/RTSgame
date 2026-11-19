# S6a / D3a — Gateway và dependency của wave

Tiếp nối ngày **13/11/2026:** đã kiểm thêm kinh tế hai vùng/công trình/fog-minimap-camera API và migrate resource nodes, xem [S6b/D3b](S6b-EconomyResourceDI.md). Các mục chưa kiểm bên dưới là checkpoint ngày12/11; bước tiếp hiện tại là S6c/D3c trong Progress.

Ngày **12/11/2026**, Unity **6000.3.11f1**. S6/D3 tổng thể **PARTIAL**. Lát cắt này nối dữ liệu strategic vào scene và wave; chưa nghiệm thu toàn bộ kinh tế, xây tuyến hoặc cân bằng wave theo BuildPlan.

## Mục tiêu và phạm vi

`ProceduralGame` tham chiếu `StrategicProceduralMapSettings.asset`, preset `StrategicRegions` version3, seed mặc định1. Asset `DefaultProceduralMapSettings.asset` legacy được giữ để đối chiếu. Editor scene generator tạo/gán asset strategic và reference wave cho root khi refresh scene. Chưa thêm hình landmark; pad/vàng/region đã áp vào terrain/resources, phần nhận diện hình ảnh thuộc S7.

## Dữ liệu, thuật toán và luồng tích hợp

- `ProceduralMapAuthoring` lưu origin thực sự dùng khi áp Tilemap. Resource và gateway dùng cùng origin; `GatewayCount` bằng0 với legacy hoặc chưa có map hợp lệ. Index âm/vượt mảng trả false.
- `TryGetGatewaySpawnPosition` chuyển gateway biên vào trong2 cell rồi dùng `Tilemap.GetCellCenterWorld`. Đây là vị trí yêu cầu; bridge tiếp tục giải quyết vị trí theo bán kính từ prefab qua `TryResolveSpawnPosition`. Không bỏ qua navigation hoặc tạo một simulation mới.
- Wave chọn ngẫu nhiên gateway đầu tiên, thử từng gateway tối đa một lần nếu spawn thất bại; không fallback sang vòng tròn khi strategic có gateway. Nếu mọi cửa thất bại, timer hiện có thử lại sau0,5giây. Số wave/quái/interval không đổi theo diện tích đất.
- Legacy không có provider dùng vị trí trên vòng spawn hiện có. Vòng đỏ spawn được ẩn trên strategic vì nó không diễn tả vị trí sinh quái mới; vòng phòng thủ vẫn giữ.
- Map được sinh/áp một lần trong Awake. Gọi `TryGenerateAndApply` lần nữa bị từ chối trước khi instantiate thêm resource; đổi seed cần tải lại scene. Đây không phải API regenerate giữa trận.
- Pipeline cũ vẫn áp terrain/resources → `Physics2D.SyncTransforms` → rebuild Tilemap navigation → root `BindScene` cấu hình cả Gameplay/Swarm → bind resource/Unit → mở composition gate. Không thay ranh giới `RTS.DOTS`.

## Dependency injection và lifetime

`GameSceneCompositionRoot` yêu cầu serialized `m_Waves` và day/night; truyền GameManager, TilemapManager, DayNightCycleManager, bridge và generator vào `EnemyWaveManager.Initialize`. Thiếu dependency bị từ chối; gọi lại cùng references không thêm listener, đổi references sau Init bị từ chối. Consumer wave không còn Find manager/map/day-night hoặc đọc `SwarmGameBridge.Instance`.

`Initialize` subscribe `OnPhaseChanged` một lần; `OnDestroy` unsubscribe. Update/Spawn kiểm initialized, composition gate và bridge bound. Lookup của SampleScene nằm trong bootstrap legacy `SwarmGameBridge.OnSceneLoaded`, được giữ tách khỏi consumer wave. Bridge là dependency concrete hiện có, không tạo thêm container/interface chỉ để chuyển tiếp một lời gọi.

D3 còn: resource/procedural lookup, build/train actions, factories/pool và các caller trong [Inventory DI](../DependencyInjection/Inventory.md). Không coi một wave consumer đã migrate là toàn project đạt DI.

## Cách chạy và kiểm chứng

1. Mở `ProceduralGame`, kiểm root có Waves và generator tham chiếu asset strategic. Đổi Seed ở asset trước khi Play; seed cuối được trả về1.
2. Trong session Play mới, pause và chạy `Tools/ValidateS6.cs` bằng Unity MCP `script-execute`, class `S6Validation`, method `RunPaths` **trước** `Run`. Scene gốc có hai enemy; không chạy phép thử đường đi sau khi smoke đã thêm quái.
3. `RunPaths` tạo hai probe dùng speed/radius từ prefab đầu tiên ở hai gateway, bảo vệ TownHall/probe và đặt damage probe0. Tick thật hai ECS system ở0,05giây, tối đa180giây mô phỏng; đo lúc tới phạm vi10 world units quanh tâm body TownHall. Không advance day/night hoặc đồng bộ view mỗi tick, nên đây là kiểm tra tiếp cận, không phải trận wave tự nhiên hay đo FPS. Time/hall body được phục hồi trong finally; thoát Play để dọn probe.
4. `Run` assert DI/listener/invalid-index/readiness, hai grid configured, reapply bị từ chối, 120cây/12vàng/18cừu, một bridge/25actors giữ nguyên; spawn mỗi enemy prefab ở từng cửa và thêm một spawn qua wave API. Kiểm bán kính không cần dịch khỏi vị trí yêu cầu ở các seed đã chạy.

| Seed | Gate0/Gate1, tọa độ cell world | Smoke | Probe tới phạm vi10: gate0/gate1 (giây mô phỏng) |
|---|---|---|---|
| 0 | (-75,-6) / (-12,-66) | PASS;25actors;5spawn kiểm tra | 26,70 / 26,40 |
| 1 | (-14,-66) / (48,-2) | PASS;25actors;5spawn kiểm tra | 26,55 / 25,80 |
| 1543 | (-12,57) / (-75,-3) | PASS;25actors;5spawn kiểm tra | 25,65 / 26,90 |

Đây là ba bố trí gateway khác nhau, không phải xác suất thành công cho mọi seed. Probe không chứng minh quái đi qua tâm mỗi region hoặc chỉ dùng các cell Routes: noise cho phép đường tắt. Không gọi thời gian trên là benchmark hoặc cân bằng đã đạt.

Một lần thử seed1543 **sau** smoke thêm quái không đạt giới hạn120giây: khoảng gần nhất2,576668/10,76015. Tick tiếp10giây thấy probe chậm vẫn tiến tới căn cứ. Phép thử đã tách thứ tự và ghi rõ precondition; kết quả cũ giữ làm đầu mối kiểm ùn quái trong wave thật, không tính là PASS hoặc kết luận đã sửa kẹt.

## Crash và giới hạn chưa nghiệm thu

Regression cuối phiên: `RTS.DOTS.Tests` **58/58 PASS**,0fail/skip,2,277906giây; `RTS.ProceduralGeneration.Tests` **17/17 PASS**,0fail/skip,7,3625595giây. SampleScene legacy: không root, một bridge bound,25actors,một listener wave và spawn ring thành công. Editor cuối ở ProceduralGame sạch, không Play/compile/pause; seed strategic đã trả về1. Không còn probe hoặc job tool đang chạy.

Phiên trước Editor native crash; `Editor-prev.log` sau restart có `SerializationCache::BuildTransferSignatureForCache` / clone prefab tại `WaveProgressUI.RebuildWaveIcons`. Chưa xác định root cause, không sửa UI hoặc nâng Editor dựa trên stack này. Sau người dùng mở lại, Play và các kiểm tra ba seed chạy được; domain reload có lúc làm MCP mất kết nối tạm rồi tự nối lại. Không gọi mất kết nối tạm là crash mới.

Chưa nghiệm thu worker tới hai vùng và nộp, xây tuyến/collider công trình thực, fog/minimap/camera trên strategic, MainMenu→game→menu→game, domain-reload matrix cho thay đổi wave mới hoặc Player build. Config night hiện45giây; probe tới gần sau khoảng26giây không chứng minh đủ áp lực cho các wave xuất phát muộn. Cần playtest wave tự nhiên/ùn quái và so sánh legacy trước DONE S6. Footprint validator Core vẫn là mô hình cell của S5; spawn radius check ở đây không xác nhận toàn bộ footprint resource/building.

## Bước tiếp theo

S6b: dùng ba seed trên kiểm worker tới Economy/GoldFrontier và nộp, xây tuyến, fog/minimap/camera và wave ban đêm đầy đủ; ghi thời gian tiếp cận/retreat/ùn quái trước khi điều chỉnh cấu hình. D3b: migrate nhóm resource/procedural/factory theo inventory và kiểm spawn/register/remove/pool. S7 chỉ bắt đầu sau khi S6 đủ điều kiện DONE.

Nguồn đối chiếu nội bộ: [BuildPlan S6](../Progression/BuildPlan.md), [S5 topology](S5-ResourcesTopology.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md), [ADR manual DI](../DependencyInjection/ADR-001-ManualDI.md), `ProceduralMapAuthoring.cs`, `EnemyWaveManager.cs`, `GameSceneCompositionRoot.cs` và `Tools/ValidateS6.cs`. Chưa thu số đo hiệu năng mới.
