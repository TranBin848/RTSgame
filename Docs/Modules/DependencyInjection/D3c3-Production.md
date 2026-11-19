# D3c3 — DI huấn luyện và vòng đời producer

Ngày **15/11/2026**. Implementation và kiểm thử API trong Editor đã đạt; **D3/S6 tổng thể vẫn PARTIAL**. Checkpoint tiếp tục tại [Progress](../Progression/Progress.md), kế hoạch tại [BuildPlan](../Progression/BuildPlan.md).

## Mục tiêu và phạm vi

`ProductionBuilding` nhận `GameManager` và `SwarmGameBridge` tường minh; không tự Get/Find/Instance. Giữ ECS làm nguồn FIFO/timer, GameObject làm presentation và UI. Áp dụng cho TownHall, Barrack, ArcheryRange cùng ba Spawn ActionSO và bridge binding/teardown. Không thêm container hoặc service mới.

Sửa hai vấn đề được quan sát: action trước đây trừ tiền dù enqueue bị từ chối; pooled producer khi tái dùng vẫn thuộc scene DontDestroyOnLoad sau khi tách parent. Bổ sung guard fog khi đăng ký affectable trước khi map/arrays runtime sẵn sàng. Đây chưa phải migration DI toàn bộ pool, Unit hoặc fog.

## Dữ liệu và luồng xử lý

1. Root/legacy bootstrap cấp dependency cho bridge như các slice trước. `RegisterGameplayUnit` tạo/bind Entity rồi gọi `InitializeProduction(game, bridge)` nếu view là producer.
2. Initialize kiểm manager cùng scene, bridge thuộc manager và Entity có `GameplayProducer`; cùng dependency gọi lại an toàn, đổi dependency bị từ chối. Nhánh legacy không ECS chỉ được bật bằng Initialize với bridge null và Entity.Null.
3. ActionSO kiểm owner, công nghệ và tài nguyên; gọi wrapper enqueue trả bool. Chỉ khi được chấp nhận mới debit và hiện popup thành công. Raw enqueue không tự thu phí: caller chịu trách nhiệm giao dịch, giống các harness trước.
4. Producer chặn chưa inject, dead view, composition blocked, Time.timeScale≤0, prefab/action thiếu, duration không dương/NaN/Infinity, bridge chưa bound/capability chưa mở và lệnh lặp cùng frame. Bridge còn kiểm Simulation.Paused và gửi Entity đã bind, không dùng ProxyFor để ngầm đăng ký producer mới.
5. Native `EnqueueTraining` kiểm duration và CombatBody còn sống rồi append `TrainingRequest`; giữ modifier duration hiện có. **Cho phép xếp hàng khi đang xây; timer đợi xây xong** theo contract/test hiện có.
6. Khi hoàn tất, bridge dùng factory hiện có sinh view, bind ECS và gửi `MoveActor` ra khỏi công trình; không gọi AIPawn bị tắt cho quân DOTS. Timer không được nhân đôi ở MonoBehaviour cho view đã bind.
7. Unregister/return/destroy producer xóa request presentation/UI và unbind dependency; Entity removal dọn FIFO native. Sync cũng dọn presentation khi producer mất hoặc chết. Không thêm chính sách hoàn tiền khi hủy công trình.

Thuật toán FIFO và Golden-angle scatter hiện có được giữ. Guard cùng-frame vẫn giữ: một producer chỉ nhận một lệnh mỗi frame, lệnh sau bị từ chối không mất tiền. Các frame tiếp theo được xếp FIFO bình thường. Chi phí/tốc độ huấn luyện/tech không đổi.

## Pool và fog

`RuntimeObjectPool.Spawn` hai overload nhận thêm tham số bắt buộc `Scene scene`. Validate scene đã load trước khi lấy object; sau tách parent chuyển object sang scene đích trước active/reuse callbacks. TowerUnit/RangerUnit/EnemyRangerUnit truyền `gameObject.scene`; mọi caller repo đã được rà và compile. Không có fallback GetActiveScene. Test invalid scene không đổi actor/queue; Barrack return/reuse cùng scene giữ đúng owner, Entity mới và dependency mới.

Pool vẫn là cache static với root DontDestroyOnLoad. Base Unit/Projectile và các adapter vẫn còn dependency global theo [Inventory](Inventory.md); chưa chứng minh reuse qua hai gameplay scene khác nhau. Thay đổi này bảo đảm scene ownership cho đường đã kiểm, không chứng nhận toàn bộ pool đạt DI.

Trong Play check đã gặp NullReference ở `ApplyVisibilityToAffectable` khi runtime map/arrays chưa sẵn sàng. Guard trả về sớm khi thiếu dữ liệu, rồi áp dụng bình thường sau khi dữ liệu tồn tại. Công cụ kiểm thử lần lượt bỏ map/visibility/explored rồi restore, PASS sau refresh/compile thật. Không kết luận đã sửa mọi lỗi fog hoặc native crash cũ. Console lịch sử được giữ; truy vấn Exception sequence>3507 sau các training/pool/FIFO checks cuối trả 0.

## Cách chạy lại

Mở ProceduralGame hoặc SampleScene, vào Play và pause sau Start. Dùng Unity MCP `script-execute` với nội dung [ValidateD3Production.cs](../../../Tools/ValidateD3Production.cs), class `D3ProductionValidation` (file ngoài Assets, không thêm runtime tooling vào build).

- Fresh session: `CheckAdmission` → `CompleteQueued` → `QueueArcherAfterUnlock` → `CompleteQueued` → `CheckPoolAndRemovedProducer`.
- `CheckFogBeforeRuntimeData` kiểm deferral/restore fog.
- Fresh session khác: `QueueOneVillager` → `EditorApplication.Step()` → `QueueOneVillager` → `CompleteQueued`. Assertion exit movement chạy ngay lúc từng view xuất hiện; quân đầu có thể đã đến nơi khi quân sau hoàn tất.
- `Tools/ValidateS6Lifecycle.cs`: kiểm menu cleanup và game Ready khi reload.
- Exit Play để bỏ wallet/research/fixtures tạm, không save runtime scene. Chạy EditMode assemblies `RTS.DOTS.Tests` và `RTS.ProceduralGeneration.Tests` khi scene sạch.

Tool dùng tick Gameplay + bridge sync thủ công với dt0,05; không thay cho toàn bộ MonoBehaviour/Swarm/day-night hoặc Player playtest. Pool fixture đặt ngoài map chỉ kiểm lifecycle, không kiểm placement hợp lệ.

## Kết quả đã kiểm

| Kiểm tra | Kết quả thực tế |
|---|---|
| ProceduralGame seed1 initial/repeat injection | TownHall/Barrack/ArcheryRange nhận đúng owner; repeat không mất queue |
| Admission ProceduralGame | 14 trường hợp dependency/gate/dead native/pause/duration/prefab/funds/locked tech bị từ chối, không đổi wallet/actor/UI/queue |
| Villager + warrior | Một debit/lệnh, duplicate cùng frame không debit; sau8,00giây mô phỏng actors25→27, WorkerUnit+WarriorUnit, ECS exit command, UI/maps sạch |
| Archer | Nghiên cứu military_archery thật trên procedural rồi enqueue; một debit, duplicate không debit;8,00giây sinh RangerUnit |
| FIFO hai dân | Hai frame khác nhau, queue1→2;16,00giây, actors25→27, đúng hai WorkerUnit, một debit mỗi lệnh, UI/maps sạch |
| SampleScene | Injection/admission12case ở bản tool trước khi bổ sung native-dead case; worker/warrior/archer và cleanup PASS. Native-dead đã kiểm lại trên procedural và NUnit |
| Pool/destroy | Invalid scene bị từ chối; Barrack return/reuse đúng object và scene, Entity cũ mất/Entity mới bind; destroy có queue dọn UI; tick20giây không ghost spawn. Procedural và SampleScene PASS |
| Menu reload | Sau training/pool, menu không bridge/entities/ví/cờ procedural cũ; vào game lại Ready một bridge,25actors,132nodes,ví100 mỗi loại |
| Regression cuối | DOTS **59/59**,0fail/skip,2,1416279s; procedural **17/17**,0fail/skip,7,1246614s |

Test mới `TrainingRejectsInvalidDurationAndDeadProducerWithoutAppending` xác nhận invalid duration/dead không append, alive/positive nhận lệnh. Regression giữ test queue-during-construction. Các thời gian8/16/20giây là thời gian mô phỏng của scenario, thời gian Test Runner là duration thực thi tests; **không phải benchmark hiệu năng game**.

## Nghiệm thu còn lại và bước tiếp

Người dùng đã xác nhận hai mục P/click D3c2 và checklist D3c3 bên dưới ngày15/11 (“oke tất cả ổn tiếp tục đi”). Ghi nhận manual PASS theo báo cáo, không ảnh/log kèm theo; không chuyển thành số đo Player/performance. Checklist đã gửi:

1. Chọn TownHall, bấm hai lần huấn luyện ở hai frame; thấy hai mục queue, đúng tiền và hai dân đi ra. Barrack và ArcheryRange sau mở tech cũng sinh đúng quân.
2. Pause/resume khi đang train: queue không tiến khi pause, không mất thêm tiền, resume hoàn tất bình thường.
3. Nếu phá công trình có queue: queue UI biến mất, không sinh quân từ công trình đã mất; chưa có chính sách refund mới.

Tiếp **D3c4**: đọc Unit/Structure/Worker/Projectile và mọi factory/pool caller, inventory dependency/scene lifecycle còn lại trước khi migrate. S6 vẫn còn input/camera/minimap/hình ảnh và nhiều ngày/expansion/wave tự nhiên trước S7. Chưa kiểm Player build, hiệu năng, legacy không ECS thuần, cross-scene pooled reuse; không nâng package/Editor/render pipeline.

## Nguồn và file đối chiếu

- [DI Architecture](Architecture.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md), [Gameplay Architecture](../DOTSGameplay/Architecture.md).
- `Assets/Game/Scripts/Units/ProductionBuilding.cs`, TownHall/Barrack/ArcheryRange và Actions/Spawn*ActionSO.
- `Assets/Game/DOTS/Integration/GameplayGameBridge.cs`, `SwarmGameBridge.cs`; runtime `GameplaySimulationSystem.cs`, test `GameplaySimulationTests.cs`.
- `Assets/Game/Scripts/Utils/RuntimeObjectPool.cs`, ba projectile callers; `Managers/FogOfWarManager.cs`.
- Không dùng nguồn web hoặc thay thuật toán bên ngoài trong slice này. Bằng chứng API trong phiên Editor, không có ảnh/video manual mới.
