# D3c4b — Unit DI và vòng đời đăng ký/pool

Ngày **16/11/2026 — D3c4b và D3 DONE theo phạm vi build plan**. Binding/commands/pool cùng scene, scene tham chiếu và worker pool qua unload/reload PASS qua Unity MCP. D4 UI/fog/input/camera/renderer và audit ngoại lệ toàn project vẫn TODO; không coi toàn project đã đạt DI. Điểm tiếp: [Progress](../Progression/Progress.md).

## Mục tiêu và phạm vi

Truyền dependency cho Unit/Worker/Sheep/Structure/AIPawn từ bridge, bỏ việc các consumer này tự Get/Find/Instance. Giữ prefab, component authoring, command ECS và presentation hiện có. `RTS.DOTS` vẫn độc lập với các lớp MonoBehaviour gameplay; toàn bộ wiring thuộc Integration/Assembly-CSharp. Không thêm DI container/interface/service.

## Dữ liệu và ownership

| Consumer | Dependency được nhận | Owner/lifetime |
|---|---|---|
| Unit | GameManager, TilemapManager, SwarmGameBridge qua InitializeScene | Bridge của gameplay scene; clear khi unregister/return/teardown |
| WorkerUnit, SheepUnit | Bridge được kế thừa từ Unit; worker vẫn nhận node/depot locators và wallet qua Inject | Scene; worker return clear carry/work/shelter và locators |
| StructureUnit | Map đã truyền từ Unit | Bind/return/destroy cập nhật walkability/navigation version |
| AIPawn | GameManager, TilemapManager qua Initialize | Unit lắp ghép; Unbind xóa path/destination/dependency |
| ProductionBuilding | InitializeProduction sau Unit.BindDots như D3c3 | Dọn training presentation và dependencies khi unregister |

`SceneBridge` phục vụ adapter AIPawn; không phải service locator. ScriptableObject/prefab không giữ service runtime. `Resources.Load` cho outline/placeholder vẫn là asset lookup đã phân loại, không được báo như đã xóa mọi global trong project.

## Luồng, thuật toán và lifecycle

1. Unit.Awake chỉ cache component/presentation và config nội bộ; không tìm manager. Unit.Start không tự discovery/đăng ký. Humanoid/Structure không tick behavior trước IsSceneInitialized.
2. Bridge.Register gọi InitializeScene, kiểm cùng scene/game/bridge, rồi tạo/bind Entity. Cùng dependency lặp lại được, đổi dependency phải unbind trước; thiếu dependency bị từ chối.
3. Bridge gọi RegisterWithGame; manager có guard danh sách trùng. Worker locators/wallet được cấp tại manager registration. Sheep giữ override không tham gia danh sách quân người chơi, nhưng vẫn được ECS bind.
4. BindDots yêu cầu context đã inject và Entity tồn tại; tắt AIPawn legacy. Unit/Worker/Sheep gửi command/query qua bridge đã nhận, không qua static Instance. AIPawn command gọi actor.SceneBridge khi actor đã DOTS.
5. Return pool dọn công việc/carry/shelter, manager registry, Entity và refs; AIPawn clear path. Sheep reset claim/graze. Acquire chỉ reset local state; **caller bắt buộc bridge.Register sau raw RuntimeObjectPool.Spawn**, kể cả object tái dùng. Không tự lấy service từ scene trong callback pool.
6. Bridge.Unregister dọn producer UI/maps, remove Entity/proxy, gọi UnregisterFromGame rồi UnbindScene. UnregisterUnit đánh dấu chưa đăng ký trước callback manager để re-entry kết thúc hữu hạn; direct bridge unregister cũng dọn manager list.
7. Teardown scene chỉ unbind refs của các view; không gọi manager registry cleanup trong vòng foreach dictionary. Structure destroy cập nhật walkability trước base teardown; bind/return cũng invalidate navigation, tránh chỉ dựa vào Start không chạy lại khi reuse.

Không thay thuật toán A*, FIFO, khai thác, combat hoặc giá/duration. ECS tiếp tục là nguồn gameplay; không bật thêm một timer MonoBehaviour cho actor đã bind.

## Cách kiểm tra lại

Sau Start trong paused Play session, dùng Unity MCP script-execute với [ValidateD3Units.cs](../../../Tools/ValidateD3Units.cs), class D3UnitValidation:

- CheckBindings:25Units/context, manager registry đủ/không trùng, repeat Initialize/Register,3invalid context, pawn map/disabled state.
- CheckWithoutGlobalBridge: tạm đặt Instance=null trong try/finally, kiểm move/target, AIPawn stop, worker assign/cancel/query và sheep query. Không advance MonoBehaviour trong khoảng bỏ global.
- CheckPool: Worker/Ranger/Sheep/Barrack acquire chưa init → register → return sạch → reuse mới; công trình phải tăng navigation version khi bind/return.
- CheckDirectUnregister: factory worker → direct unregister → rebind → destroy, actor/manager count về baseline.
- **Đã PASS lượt đóng16/11:** PrepareReloadPool → menu/CheckMenu → game mới/Editor Step/CheckReady → CheckReloadPool. Fixture inactive D3ReloadWorker giữ qua DDOL, đúng cùng view nhận game/bridge của scene mới, nhận resource work rồi return sạch; actor về baseline và refs cleared. Lượt bị chặn trước đó được lưu làm lịch sử, không phải trạng thái hiện tại.

`ValidateD3Production.cs` đã bổ sung explicit re-register sau reuse; `ValidateD3Projectiles.cs` cấp InitializeScene legacy cho fixtures. Không giữ overload tự lookup để chiều test cũ.

## Kết quả đã kiểm và giới hạn

| Kiểm | Kết quả |
|---|---|
| ProceduralGame | Binding25/repeat/3rejects, không global bridge, pool4loại + nav invalidation PASS |
| SampleScene, sau code cuối | Binding25/repeat/3rejects, commands global-null, pool4loại + nav và direct unregister/rebind/destroy PASS |
| Producer regression SampleScene | Return/reuse/new Entity, pending request/UI cleanup/destroy;20giây mô phỏng không ghost spawn PASS |
| Projectile regression SampleScene |10rejects không allocation/implicit binding; native1projectile/0legacy views, damage7 đúng một lần; legacy7+9/pool/scene ownership và3caller PASS |
| S6 ba seed |0/1/1543 kinh tế/xây/fog-camera/input adapter/3ngày-đêm PASS; [bằng chứng](../ProceduralGeneration/S6-FinalValidation.md) |
| DOTS cuối code |59/59 PASS,0fail/skip,1,0734240s; đây là Test Runner duration, không benchmark game |
| Procedural regression sau D3c4b |17/17 PASS lượt đóng16/11,0fail/skip,8,1212562s |
| Worker pool qua scene reload |Prepare→menu cleanup→game Ready idempotent→same view/new scene dependencies/resource command→return PASS |

Lỗi công cụ đã sửa: prefab-link của sheep sinh runtime có thể null; CheckPool nay đọc prefab bằng asset path tường minh. CheckBindings bổ sung precondition Play/bridge để không NullReference khi gọi trước scene composition. Các lỗi này không chứng minh nguyên nhân native crash trước đó. Không tuyên bố đã sửa native crash.

Chưa đo allocation/FPS/composition time, Player build hoặc legacy không ECS toàn scene. Cross-scene test đã kiểm cùng ProceduralGame unload/reload qua menu với scene instance/services mới; chưa kiểm mọi cặp map hoặc scene additive đồng thời. Ngoại lệ/công việc D4: UI/input/fog/minimap/camera, bootstrap legacy/default World, renderer/config assets và static bridge còn callers. Không xóa singleton chung trước khi migrate hết caller.

## Điểm dừng và nguồn

Lịch sử: PrepareReloadPool/menu-game từng bị automatic approval review từ chối trước thực thi do usage limit, không phải phán quyết unsafe. Lượt đóng16/11 tool MCP trực tiếp đã hoạt động lại;17procedural và pool reload đã PASS, không bypass review qua CLI. Môi trường cuối: ProceduralGame sạch/Seed1/ngoài Play-pause-compile,0Exception mới sequence>4123, chỉ log plugin path spaces; không fixture/job/CLI pending. Checklist manual16/11 do người dùng báo “oke chạy được”, không ảnh/log; pool nội bộ kiểm riêng bằng harness.

Nguồn nội bộ: [DI Architecture](Architecture.md), [Inventory](Inventory.md), [D3c4a](D3c4a-ProjectileFactory.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md), [Gameplay Architecture](../DOTSGameplay/Architecture.md). Code chính: Scripts/Units/Unit.cs, WorkerUnit.cs, Neutral/SheepUnit.cs, StructureUnit.cs, HumanoidUnit.cs; Scripts/AI/AIPawn.cs; Integration/SwarmGameBridge.cs; Managers/GameManager.cs. Thư mục runtime ECS và package/scene/prefab/preset không đổi trong slice này.
