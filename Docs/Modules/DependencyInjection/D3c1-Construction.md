# D3c1 — Dependency của BuildingProcess

Ngày **13/11/2026**, Unity **6000.3.11f1**. Lát cắt `BuildingProcess` và caller đã triển khai/kiểm chứng; **D3 tổng thể vẫn PARTIAL**. Không thay BuildPlan hoặc chuyển sang S7.

## Mục tiêu và phạm vi

Loại bỏ `GameManager.Get()` và `SwarmGameBridge.Instance` trong process xây dựng, dùng dependency đã được scene composition cấp. Giữ ECS là nguồn tiến độ/công việc, prefab/sprite/thời gian xây/chi phí hiện có. Không thêm container, service locator hoặc interface chuyển tiếp chỉ cho một caller.

Các điểm đã sửa cùng nhau:

- `BuildingProcess.cs`: constructor nhận `(BuildActionSo, Vector3, WorkerUnit, GameManager, SwarmGameBridge)`; không giữ overload3tham số tự lookup.
- `GameManager.TryPlaceCurrentBuild`: truyền `this/m_Bridge`, kiểm worker tồn tại/còn sống/còn ECS binding trước finalize placement và debit.
- `GameplaySmokeAutomation.cs`: harness truyền manager/bridge đã chọn trong test; discovery trong harness vẫn là ngoại lệ tooling, không phải consumer production mới.
- `Tools/ValidateS6Economy.cs`: cập nhật caller constructor để các kiểm chứng S6 cũ tiếp tục chạy.
- `Tools/ValidateD3Construction.cs`: assert dependency, placement guard, UI completion, removed-view và SampleScene.

Không sửa `RTS.DOTS`, root/scene/prefab, asset giá, research hoặc thuật toán navigation. `ProductionBuilding`, actions train, spawner/procedural, pool và Unit/Structure adapters chưa được migrate trong slice này.

## Dữ liệu và luồng xử lý

Constructor nhận config action/prefab, vị trí và worker; manager cung cấp capability/UI, bridge cung cấp factory và command ECS. Hai reference scene chỉ sống cùng process của trận đó, không ghi vào ScriptableObject hoặc static field.

Trước spawn/UI, constructor từ chối action/prefab thiếu, worker/manager thiếu, worker khác scene hoặc đã chết. Worker đã có Entity phải có bridge. Bridge phải bound, thuộc cùng GameObject với GameManager theo composition hiện tại, và nhận diện worker trong Gameplay system của nó. Research gate được kiểm trước tạo view. Gọi constructor không tự trừ tiền; debit vẫn thuộc GameManager như trước.

Khi có bridge: gọi `SpawnGameplay`, kiểm view trả về, rồi enqueue UI, đăng ký foundation/process, `BeginConstruction` và `Gameplay.AssignConstruction` qua reference được truyền. Không gọi ngược `WorkerUnit.SendToBuild` trong nhánh ECS để lấy lại global bridge. Progress được đọc từ bridge đã nhận, stage/events vẫn chạy qua đường bridge hiện có. Hoàn tất cập nhật sprite và xóa queue entry.

Nhánh không có bridge vẫn tồn tại bằng dependency `null` tường minh với worker chưa bind: instantiate prefab và gửi lệnh legacy. Không dùng lookup để âm thầm đổi nhánh. **Chưa kiểm riêng một scene hoàn toàn không ECS**; SampleScene được kiểm là scene legacy-bootstrap nhưng vẫn chạy ECS.

`Update` bỏ qua view đã hủy/dead/hoàn tất; `UpdateDotsPresentation` kiểm view/bridge/World/Entity còn tồn tại trước khi đọc ECS. Không tái bind process cũ vào trận mới. Đây không phải triển khai hủy xây/hoàn tiền hoặc toàn bộ cleanup của queue/process registry khi một nhà đang xây bị phá.

## Kiểm chứng Unity và cách chạy lại

Mở `ProceduralGame` strategic seed1, Play rồi pause. Chạy `Tools/ValidateD3Construction.cs` qua MCP `script-execute`, class `D3ConstructionValidation`:

1. `CheckValidation`:8case bị từ chối — thiếu action/prefab/worker/manager/bridge, manager khác owner, worker chưa bind, Tower còn khóa. Actor25, queue0, ví100/100 không đổi. Trong SampleScene không khóa Tower nên7case được chạy, không coi Tower đã bị khóa tại scene đó.
2. `CheckPlacementGuard`: gọi đường `TryPlaceCurrentBuild` với worker null/unbound/dead, assert placement chưa finalize, không debit hoặc thêm actor. Test tạm vô hiệu EventSystem để bảo đảm không bị return sớm do con trỏ trên UI, cung cấp screen point hợp lệ và fixture highlight rỗng; sau test phục hồi EventSystem, selection, state và placement. Đây là kiểm guard/command, không nghiệm thu thao tác click hoặc footprint mới.
3. `S6EconomyValidation.RunConstruction` trong `Tools/ValidateS6Economy.cs`: cùng StoragePit thật tại pad landmark, đúng debit100/100, actor25→26, worker tới xây và sprite complete. Hai lượt trong phiên hoàn tất sau30giây và27giây mô phỏng; đây không phải CPU time hoặc benchmark, không yêu cầu worker khởi đầu/các tick trước đó giống nhau giữa mọi lượt.
4. Cho một Editor Step để view chạy Start, rồi `S6EconomyValidation.CheckFogCamera`: registry/fog khám phá, minimap128×128/camera focus PASS. `CheckCompletedQueue` xác nhận không còn UI entry sau xây xong.
5. `CheckDestroyedView` tạo một construction view thử, hủy view đó và assert Entity bị xóa; gọi Update/UpdateDotsPresentation/ApplyDotsStage không dereference Entity đã xóa. Vị trí thử ngoài map, không dùng kết quả này làm chứng nhận placement hoặc refund/queue cancellation. Exit Play để dọn dữ liệu thử sau các check.

SampleScene, session mới: `CheckValidation`7case PASS; `RunSampleScene` tìm footprint StoragePit trống, debit100/100, inject manager/bridge, worker xây xong sau15giây mô phỏng, actor25→26, foundation→complete và queue được dọn. Không thay scene/prefab. Harness `GameplaySmokeAutomation` đã cập nhật caller và compile, **chưa chạy lại Player smoke harness** trong phiên này.

Regression sau thay đổi cuối: `RTS.DOTS.Tests` **58/58 PASS**,0fail/skip,1,9266349giây; `RTS.ProceduralGeneration.Tests` **17/17 PASS**,0fail/skip,5,5503457giây. Quét `BuildingProcess.cs` không còn `GameManager.Get`, `SwarmGameBridge.Instance` hoặc Find service; toàn bộ call sites đã đối chiếu bằng `rg`. Search chỉ chứng minh những mẫu trực tiếp đó không còn, không thay nghiệm thu toàn project DI.

## Hiệu năng và giới hạn

Không chạy benchmark hoặc Player build mới; không tuyên bố DI làm game nhanh hơn. Không thêm vòng lặp mỗi frame trong production. Validation diễn ra khi tạo process; tick xây và navigation ECS giữ nguyên.

Unit/Worker/Structure vẫn có các global adapters theo Inventory; process không trực tiếp gọi lookup nữa không đồng nghĩa toàn bộ đồ thị callee đã migrate. Chưa kiểm hủy xây/hoàn tiền, pool return/reuse, train actions, click UI thật, mọi loại nhà hoặc scene không ECS. Các kết quả3seed S6 trước đây là lịch sử riêng; slice này kiểm lại seed1 và SampleScene, không ghi thành regression3seed mới.

## Checkpoint và bước tiếp

Editor cuối ProceduralGame sạch, ngoài Play/compile/pause; strategic seed1. Các object/scene-data thử đã được dọn bằng Exit Play; không job đang chạy, không commit mới. MCP có lần mất kết nối tạm lúc domain reload, sau đó các check/regression trả kết quả; không có kết luận sửa native crash cũ.

**Tiếp D3c2:** đọc `ProceduralMapAuthoring`, `ResourceSpawner`, root/scene-generator và Inventory; chuyển lựa chọn spawner/map/factory ra composition phù hợp, kiểm spawn/register/remove. Sau đó tiếp ProductionBuilding/build-train actions và pool lifecycle. S6 vẫn còn nghiệm thu hình ảnh/input, D3 chưa DONE nên chưa chuyển S7.

Nguồn đối chiếu nội bộ: [DI Architecture](Architecture.md), [Inventory](Inventory.md), [ADR-001](ADR-001-ManualDI.md), [BuildPlan](../Progression/BuildPlan.md), [DOTS Gameplay](../DOTSGameplay/Architecture.md), [DevelopmentGuide](../DOTS/DevelopmentGuide.md), [S6b](../ProceduralGeneration/S6b-EconomyResourceDI.md) cùng các file code/tools nêu trên. Không thêm thuật toán hoặc dependency ngoài.
