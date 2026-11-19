# D3c2 — Procedural và resource spawner qua DI

Cập nhật acceptance **15/11/2026:** người dùng xác nhận “tất cả ổn” cho hai mục phím P/click/giao lại cây/nộp ở checklist dưới. Manual PASS theo báo cáo người dùng, không artifact kèm theo; không suy ra các mục khác đã test.

Ngày **15/11/2026**, Unity **6000.3.11f1**. Implementation và các API integration checks dưới đây đã đạt; **D3/S6 tổng thể vẫn PARTIAL**. Tiếp [D3c1](D3c1-Construction.md), không thêm container hoặc đổi BuildPlan.

## Mục tiêu và phạm vi

Đưa việc chọn spawner/dependency ra cấu hình scene và bootstrap. `ProceduralMapAuthoring` không tự Find mọi spawner trong scene; `ResourceSpawner` không tự lấy TilemapManager/GameManager/bridge global trong Awake hoặc khi regenerate. Giữ quota/prefab/thuật toán sinh map và nguồn dữ liệu ECS hiện có.

Đã đối chiếu caller và asset GUID: ResourceSpawner nằm trong `SampleScene` và `ProceduralGame`, không có prefab/scene khác dùng component này trong kết quả quét. Không sửa SampleScene hoặc resource prefab. ProceduralGame được gán một reference ResourceManager qua Unity serialization; Editor scene generator cũng gán danh sách này. Các thay đổi package/plugin/skills có sẵn trong workspace không thuộc slice này.

## Dữ liệu và ownership

- `ProceduralMapAuthoring.m_LegacySpawners` là mảng reference Inspector, cung cấp `IReadOnlyList` cho root. Mảng không được null, phần tử phải không-null, cùng scene và không trùng. Danh sách rỗng có thể dùng cho scene không có legacy spawner; không có fallback runtime tự tìm để bù cấu hình thiếu. Scene hiện tại được wire1spawner, không để rỗng.
- `ResourceSpawner.Initialize(GameManager, TilemapManager, SwarmGameBridge)` nhận reference theo scene; bridge phải initialized và thuộc GameObject của manager. Map phải có walkable Tilemap, dependencies cùng scene. Init lại cùng refs không xóa/sinh thêm resource; đổi refs bị từ chối.
- Config count phải không âm, attempts/area size phải dương; nhóm có count>0 phải có prefab/container. Validate trước khi clear runtime resources. `IsInitialized`/`IsSuppressed` mô tả trạng thái spawner, không thay readiness của root/bridge.

Không đưa managed reference vào `RTS.DOTS`, không tạo thêm ví hoặc bản sao claim state. Resource nodes tiếp tục nhận bridge/map qua registration của D3b.

## Luồng xử lý

### ProceduralGame

1. Authoring Validate config và sinh/validate Core MapData trước khi dọn legacy resources; lỗi cấu hình không tự đi tìm hoặc dọn spawner khác.
2. Với map hợp lệ, authoring suppress đúng các spawner đã cấu hình và dọn các container legacy. Nó áp terrain/resources, sync physics và rebuild Tilemap navigation như pipeline cũ.
3. Root nhận danh sách đã cấu hình từ authoring, inject game/map/bridge vào từng spawner sau khi GameManager đã nhận dependencies. Spawner suppressed không sinh tài nguyên.
4. Root BindScene cấu hình hai ECS grids và bind các view/resources ban đầu đúng một lần, rồi mở Ready. `TryRegenerateResources` trên spawner suppressed trả false; component Update cũng không xử lý phím P.

Awake của spawner chỉ lấy BoxCollider2D nội bộ, đặt trigger/layer Ignore Raycast và tắt collider, không lookup hoặc sinh tài nguyên. Thứ tự root/generator/bridge hiện tại giữ nguyên.

### SampleScene và bootstrap legacy

`SwarmGameBridge.OnSceneLoaded` là adapter composition legacy: chọn manager/map/bridge và inject các ResourceSpawner của scene vừa tải trước bridge Start/BindScene. Lookup ở adapter này được giữ có phạm vi; hai consumer migrated không Find/Get/Instance nữa.

Initial resources được instantiate thành view **trước** capture grid, rồi BindScene đăng ký Entities. Vì bridge chưa bound lúc initial generation, spawner không gọi factory để bind Unit/resource quá sớm. Khi regenerate trong scene đã bound, spawner dùng `bridge.SpawnGameplay` để inject/register view mới ngay trong factory hiện có.

`TryRegenerateResources` kiểm initialized, suppression, dependency còn sống, bridge bound và composition gate. Thiếu game/map không được xóa tài nguyên đang có. Nó validate config, bật area collider để lấy bounds, dọn runtime views, spawn bằng quy tắc placement hiện tại, rồi luôn tắt collider/sync physics trong finally. Sau sinh, rebuild pathfinding/version và clear locator cache/cancel công việc cũ qua manager được inject. Không báo spawn thành công nếu factory trả null. Attempts vẫn hữu hạn và có thể thiếu quota khi vùng không đủ chỗ; không bổ sung thuật toán cân bằng/placement mới.

Khi generator thất bại, authoring chỉ restore lựa chọn legacy của chính các refs cùng scene; không tìm scene khác. Scene có root vẫn giữ fail-closed khi generator chưa Ready, không coi việc enabled lại spawner là trận đã phục hồi. Nhánh failure fallback không có root/chế độ hoàn toàn không ECS chưa được nghiệm thu lại.

## File và caller đã cập nhật

`ProceduralMapAuthoring.cs`, `ResourceSpawner.cs`, `GameSceneCompositionRoot.cs`, legacy bootstrap trong `SwarmGameBridge.cs`, `ProceduralMapEditorTools.cs`, reference array trong `ProceduralGame.unity`, `Tools/ValidateD3Spawners.cs` và docs. Scene được lưu qua Unity, không sửa YAML thủ công; serialization Unity có thể làm các float animation10↔10.000001 xuất hiện trong diff có sẵn.

Scene mới dùng ResourceSpawner phải có composition gọi Initialize; không còn tự chạy generation từ Awake chỉ vì thêm component. Prefab/scene hiện có ngoài hai scene đã quét không được suy ra là đã migrate. Unit/Worker/Structure adapters còn trong Inventory, factory/pool toàn project chưa hoàn tất.

## Kiểm chứng tự động qua Unity MCP

`Tools/ValidateD3Spawners.cs`, class `D3SpawnerValidation`, gọi qua `script-execute` trong disposable Play session đã pause; Exit Play giữa các scene.

| Check | Kết quả ngày15/11 |
|---|---|
| `CheckProcedural`, seed1 | Root/bridge Ready; một explicit spawner initialized/suppressed/disabled, area collider tắt;120cây/12vàng/18cừu,25actors,150GameplayResource (gồm18cừu). Init/Bind lặp không tăng count/nav version; TryRegenerate bị chặn; đổi map sau Init bị từ chối |
| `CheckValidation` |4case thiếu dependency/config bị từ chối; marker resource và Entity/actor không bị xóa, spawner chưa initialized. Regenerate trước Init trả false; authoring null/duplicate refs bị từ chối |
| `RegenerateLegacy`, SampleScene | Initial/repeat-init và regeneration PASS:30cây/20vàng/18cừu,25actors,68resource Entities; không tăng/giảm count; toàn bộ view/Entity cũ đã dọn, nodes mới được inject |
| Dependency mất sau Init | Tạm bỏ game/map riêng từng trường hợp trong fixture: regeneration trả false, không dọn resources/actors; refs được phục hồi trong finally |
| Worker giữ node cũ | Giao worker tới cây trước regenerate; sau regenerate worker.Phase=None/Node=Null, navigation version tăng, cả Gameplay/Swarm grids configured, area collider không chặn click |
| `GatherAfterRegeneration` | Giao cây mới và tick Gameplay, sync/nav: nộp20gỗ sau21,90giây mô phỏng, ví100→120. Bảo vệ máu worker và không tick day/night/Swarm để tách phép thử kinh tế; không đổi profile/speed hoặc teleport |

Legacy regeneration đã chạy nhiều lượt trong các session thử, gồm hai lượt liên tiếp ở cùng session trước lần kiểm cuối; không giữ resource Entity của lượt trước. Các object bị clear là runtime views của session Play thử, không phải asset/prefab hoặc dữ liệu scene lưu trên disk.

`Tools/ValidateS6Lifecycle.cs` chạy lại luồng MainMenu→ProceduralGame→MainMenu→ProceduralGame qua GameFlowMenu và Editor Step: Ready một bridge/25actors/132resource nodes inject/một wave listener/ví100mỗi loại; menu không bridge/Entity/ví/cờ procedural cũ. Procedural suppression check PASS lại ở trận thứ hai.

Regression **sau thay đổi cuối**: `RTS.DOTS.Tests` **58/58 PASS**,0fail/skip,1,9211446giây; `RTS.ProceduralGeneration.Tests` **17/17 PASS**,0fail/skip,5,2637561giây. Một lượt trước đó cũng58/17pass nhưng không dùng thay kết quả cuối. Quét hai consumer không còn Find*/Instance/Get service trực tiếp. Editor scene-generator code đã compile và reference scene hiện có đã wire qua MCP; chưa chạy lại toàn bộ menu Create/Refresh vì menu đó còn tái cấu hình UI ngoài phạm vi slice.

## Cách chạy và checklist người dùng

Các kiểm chứng trên gọi API, chưa mô phỏng bàn phím/chuột thật. Người dùng kiểm giúp hai mục:

1. **ProceduralGame:** Play, chọn worker đi khai thác; nhấn **P** hai lần. Mong đợi map/tài nguyên không bị regenerate, không có tài nguyên trùng, worker vẫn thao tác được; không có exception gameplay mới.
2. **SampleScene:** Play, giao worker khai thác cây rồi nhấn **P**. Mong đợi tài nguyên cũ được thay bằng tài nguyên mới; click chọn quân không bị ResourceManager chặn. Giao lại worker tới cây mới và chờ nộp gỗ; không có NullReference/MissingReference. Regenerate chủ động hủy công việc cũ, không cam kết worker tự tìm lại node nếu chưa giao lại.

Không cần test các case cấu hình lỗi bằng cách xóa asset/prefab. Nếu báo lỗi, gửi scene, hành động vừa làm và exception/stack Console; không coi thiếu ảnh/report là đã pass manual. Checklist này cũng được liên kết trong Progress để phiên sau ghi nhận kết quả.

## Hiệu năng, giới hạn và bàn giao

Không đo CPU/GC/frame hoặc build Player mới; thời gian21,90giây ở trên là thời gian mô phỏng. Không hứa DI tăng FPS. Config validation/HashSet authoring chạy tại init/generation, không thêm scene lookup mỗi frame. Bridge navigation capture cũ vẫn còn các discovery theo Inventory; không coi toàn bộ graph đã đạt DI.

Chưa nghiệm thu keyboard P/click thực, regenerate khi worker đang mang hàng hoặc đang xây/huấn luyện, mọi cấu hình quota/area, failure fallback không root, scene-generator UI refresh hoặc domain-reload matrix mới. Không đổi production/pool architecture trong slice này. Những phần này không được ghi thành kết quả đã đạt.

Cuối phiên: ProceduralGame sạch, ngoài Play/compile/pause, preset strategic/seed1; runtime test objects được dọn bằng Exit Play; không job hoặc commit mới. MCP có lần reconnect khi vào Play/domain reload, kiểm lại state rồi mới tiếp tục; không suy ra native crash mới hoặc đã sửa crash cũ. Package/plugin/skills có thay đổi từ trước, giữ nguyên.

**Bước tiếp D3c3:** ProductionBuilding và actions huấn luyện, factory/pool/lifecycle còn lại theo Inventory; giữ các caller/constructor DI D3c1 và resource-node lifecycle D3b. S6 còn manual UI/input/hình ảnh; S7 chưa bắt đầu khi D3 chưa đủ điều kiện DONE.

Nguồn đối chiếu: [BuildPlan](../Progression/BuildPlan.md), [DI Architecture](Architecture.md), [Inventory](Inventory.md), [ADR-001](ADR-001-ManualDI.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md), [DOTS Gameplay](../DOTSGameplay/Architecture.md), code và Tools nêu trên. Không bổ sung thuật toán hoặc dependency ngoài.
