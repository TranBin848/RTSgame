# D0 — inventory dependency ban đầu

Ngày 09/11/2026. Trạng thái **D0 DONE, inventory làm baseline cho D1–D4**. Quét `Assets/Game/**/*.cs` cho `World.DefaultGameObjectInjectionWorld`, `Find*`, `GameObject.Find`, `.Instance`, `.Get()`, `Resources.Load` và hai locator tạo trong GameManager. Có **149 vị trí ứng viên trong 31 file**; [snapshot đầy đủ theo file và dòng](LookupScan-2026-11-09.txt). Một vị trí ứng viên không tự động là lỗi DI. Quyết định kiến trúc: [ADR-001](ADR-001-ManualDI.md).

## Nhóm consumer cần phân loại và chuyển

| Nhóm | File ứng viên | Dependency/điểm cần đọc trước |
|---|---|---|
| Composition/bridge | `SwarmGameBridge.cs`, `GameplaySmokeAutomation.cs`, `SwarmSmokeAutomation.cs` | Bootstrap tĩnh, GameManager/Tilemap/Fog/DayNight, World và smoke tool |
| Manager | `GameManager.cs`, `EnemyWaveManager.cs`, `CursorManager.cs`, `FogOfWarManager.cs`, `FogOfWarAffectable.cs`, `SingletonManager.cs` | Manager references, static Get, spawn/retreat, lifecycle scene |
| Units/actions/build | `Unit.cs`, `WorkerUnit.cs`, `SheepUnit.cs`, `StructureUnit.cs`, `ProductionBuilding.cs`, `BuildingProcess.cs`, `PlacementProcess.cs`, `Projectile.cs`, `AIPawn.cs` | Bridge, GameManager, Tilemap, runtime factory/pool và fallback legacy |
| Resource/procedural | `ResourceSpawner.cs`, `ResourceNodeBase.cs`, `ProceduralMapAuthoring.cs`, `ProceduralMapEditorTools.cs` | Scene lookup, generated roots, Editor-only vs runtime |
| UI | `DayCounterUI.cs`, `DayNightClockUI.cs`, `IdleWorkerPanelUI.cs`, `MinimapFogUI.cs`, `ResourceRequirementsDisplay.cs`, `SelectedUnitsInfoPanelUI.cs`, `WaveProgressUI.cs` | Các manager/sự kiện đang tự tìm hoặc lấy static |
| Renderer | `SwarmInstancedRenderer.cs` | Shader/asset lookup có thể là dependency nội bộ hoặc config, cần phân loại |

`TilemapManager`, `CursorManager`, `GameManager` kế thừa `SingletonManager`; `SwarmGameBridge` có Instance riêng. `SingletonManager.Get()` được dùng trong resource node. File không khớp mẫu tìm vẫn có thể tự tạo service, lưu serialized ref hoặc truy cập World.DefaultGameObjectInjectionWorld; phải quét bổ sung.

## Phân loại ban đầu theo caller đã đọc

| Consumer/nhóm | Dependency ẩn hiện tại | Owner/lifetime dự kiến | Cách nhận sau chuyển đổi | Slice |
|---|---|---|---|---|
| `SwarmGameBridge` | `GameManager`, `TilemapManager`, fog, day/night qua Find; default ECS World trong Awake; static auto-bootstrap | Gameplay scene composition root cấp bridge; Unity/ECS sở hữu World | Root truyền manager/map/fog/day-night và system handles trước khi Ready; scene cũ dùng adapter legacy rõ | D1 |
| `GameManager` | Bridge.Instance, Find camera/fog/canvas, TilemapManager.Get; tự tạo resource locators | Gameplay scene root | Root truyền bridge, camera/fog/map, HUD và locators; Wallet vẫn ở ECS | D1/D3 |
| `EnemyWaveManager` | Find day-night/GameManager/map; bridge.Instance | Gameplay scene root | Root truyền các reference, spawn adapter; đăng ký event sau Init, hủy ở teardown | D3 |
| `Unit`, `WorkerUnit`, `SheepUnit`, `ResourceNodeBase`, `StructureUnit`, `ProductionBuilding` | Bridge.Instance, GameManager.Get, TilemapManager.Get | Gameplay scene factory/bridge và pool lifecycle | Inject tại bind/spawn; object active trước bind phải có guard; rebind khi lấy từ pool | D3/D4 |
| `FogOfWarManager`, `FogOfWarAffectable`, `CursorManager` | Manager.Get, Find, bridge.Instance | Gameplay scene root/registry | Root hoặc factory cấp refs; affectables động đăng ký qua registry có lifecycle | D3/D4 |
| UI: day/night/wave, idle worker, minimap, selected units, requirements | Find/Manager.Get/bridge.Instance | Gameplay scene root hoặc panel Init | Root truyền read model/event source; unsubscribe khi disable/destroy | D2/D4 |
| `ResourceSpawner`, `ProceduralMapAuthoring` | TilemapManager.Get/FindAll spawner, bridge.Instance | Scene root; map owns generated roots | Root truyền map/spawner references; preset procedural/legacy quyết định rõ | D1/D3 |
| `BuildingProcess`, `PlacementProcess`, `Projectile` | Manager.Get, bridge.Instance; placeholder via Resources.Load | Đối tượng tạo process/factory, asset config | Constructor/Init nhận manager, bridge và asset; projectile pool rebind | D3 |
| `GameplaySmokeAutomation`, `SwarmSmokeAutomation`, `ProceduralMapEditorTools` | Find/Instance trong tooling | Test harness/Editor tool | Cho phép discovery trong test/editor composition; không tính consumer gameplay production, vẫn ghi rõ ngoại lệ | D4 audit |
| `SwarmInstancedRenderer`, `Unit` highlight, `ResourceRequirementsDisplay`, `PlacementProcess` | Resources.Load cho shader/sprite/material | Visual config asset | Serialized asset hoặc root inject nếu cần test; chưa là service locator nghiệp vụ | D4 audit |
| `SwarmDemoController` | Default ECS World | Demo bootstrap | Demo root/explicit World access tại composition boundary; không inject managed service vào jobs | D4 audit |

`SingletonManager<T>.Get()` tìm theo tag và là đường fallback chung; xóa chỉ khi mọi caller đã chuyển. `FogOfWarAffectable` hiện dùng static cached manager; đây là state vượt lifetime scene cần dọn ở D4. `GameManager.Awake` đã `new SceneResourceNodeLocator` và `new SceneResourceDepotLocator`: đây là điểm tạo service cần chuyển về root, dù không khớp mẫu lookup. `ResourceSpawner` có đường legacy riêng phải giữ chạy được trong quá trình chuyển.

## Phạm vi từng caller và ngoại lệ

Snapshot lưu từng match với file/dòng; bảng trên gộp các call site cùng owner/lifetime để tránh lặp 149 hàng giống nhau. D1 phải đọc lại đúng dòng của nhóm bridge/root trước khi sửa; D3/D4 đọc lại nhóm tương ứng vì số dòng có thể đổi. `GameManager.Get`, `TilemapManager.Get` và `SwarmGameBridge.Instance` là các lối vào service nghiệp vụ cần chuyển. `FindObjectsByType` ở bootstrap dùng để bind actor; chuyển vào root/registry theo lifecycle. `FindObjectsByType` trong smoke và Editor tool là discovery phục vụ test/authoring, được giữ như ngoại lệ có phạm vi.

Static mutable state cần theo dõi riêng: `SwarmGameBridge.Instance`; `ProceduralMapRuntime.IsActive/IsReady/HasFailed/LastError/Ready`; `FogOfWarAffectable.s_FogOfWarManager`. `RuntimeObjectPool` có cache tĩnh; khi migrate factory/pool ở D3, kiểm việc reset giữa scene. Static helper thuần (ví dụ toán, validator, seed mixing) không là service và không cần inject. `Resources.Load` đang lấy material/sprite trực quan, phân loại asset lookup; D4 kiểm lại cấu hình, nhưng không coi đó là bằng chứng sử dụng service locator nghiệp vụ.

Đã đọc thứ tự chính: `ProceduralMapAuthoring.Awake` ở -300; `SwarmGameBridge.Awake` ở -200, tự gắn trong `sceneLoaded`; `SwarmGameBridge.Start` quét Unit/resource và bind; `Unit.Awake` tự lấy GameManager, `Unit.Start` đăng ký, `Unit.OnDestroy` unregister; `EnemyWaveManager.Start` subscribe day/night và `OnDestroy` unsubscribe. `ProceduralGame` và `SampleScene` có các manager chung; `ProceduralGame` có authoring, `MainMenu` có menu. D1 phải nghiệm thu prefab spawned/pool và domain reload bằng Unity, không suy ra từ YAML.

Giới hạn của D0: đây là audit code và scene YAML, chưa có test DI vì runtime chưa chuyển. Sau mỗi D1–D4 phải quét lại snapshot, phân loại điểm mới và ghi ngoại lệ còn lại trước khi tuyên bố toàn project đạt DI.
