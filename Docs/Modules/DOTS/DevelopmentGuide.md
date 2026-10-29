# Hướng dẫn phát triển tiếp trên nền DOTS hiện có

Đối chiếu code ngày **26/10/2026**. Đọc [Tech Overview](../../TechnicalOverview.md) trước, sau đó dùng trang này để chọn đúng điểm tích hợp. Các bước bên dưới là quy trình phát triển, không phải tuyên bố rằng những tính năng tương lai đã được triển khai.

## 1. Chọn chỗ sửa theo loại công việc

| Yêu cầu | Điểm sửa chính | Phần phải đối chiếu cùng |
|---|---|---|
| Thay AI quái hoặc retreat | `SwarmJobs.cs`, `SwarmSimulationSystem.cs` | `SwarmComponents.cs`, spawn settings và flow field |
| Thay đường đi/di chuyển dân, quân, cừu | `GameplayActorJobs.cs` | `GameplaySimulationSystem.PathBudget`, waypoint, grid và body offset |
| Thay chiến đấu quân/tháp hoặc chạy trốn của cừu | `GameplayActorJob` trong `GameplayActorJobs.cs` | `GameplayActor`, cấu hình bridge, attack/damage sequence |
| Thay khai thác, nộp kho, trú nhà | `GameplayEconomyJob.cs` | `GameplayWorker`, resource/depot/wallet và adapter WorkerUnit |
| Thay xây hoặc huấn luyện | `GameplayEconomyJob.cs`, component construction/production | `BuildingProcess`, `ProductionBuilding`, event/request mapping trong bridge |
| Thêm lệnh từ UI | Unit/ActionSO/manager và `GameplayGameBridge.cs` | Command của `GameplaySimulationSystem`; lifecycle khi hủy/chết |
| Thêm hình quái/đạn | `SwarmVisualLibrary`, `SwarmEditorTools`, renderer và mapping VisualId | Spawn settings và animation clip/prefab gốc |
| Thêm/sửa bản đồ, vật cản | `TilemapManager` và `SwarmGameBridge.RefreshNavigation` | Cấu hình grid cho cả hai system, obstacle bodies và người đang có đường đi |

Đường dẫn runtime tính từ `Assets/Game/DOTS/Runtime`; bridge từ `Assets/Game/DOTS/Integration`. `GameplayGameBridge.cs` mở rộng **cùng class** `SwarmGameBridge`, không phải một MonoBehaviour thứ hai cần gắn vào scene.

## 2. Sinh thực thể và gửi command

### Đường sinh được tích hợp

Trong scene đã cấu hình, lấy bridge và kiểm tra trạng thái trước khi sử dụng:

```csharp
var bridge = SwarmGameBridge.Instance;
if (bridge == null || bridge.Simulation == null || !bridge.Simulation.IsConfigured)
    return; // Scene chưa sẵn sàng cho lệnh này.

bool spawned = bridge.SpawnEnemy(enemyPrefab, position);
```

`SpawnEnemy` đọc cấu hình prefab, giải quyết vị trí đủ chỗ cho collider và tạo Entity. Giá trị trả về là `bool`, không phải GameObject/Entity. Muốn giữ handle của Entity vừa tạo bằng API mức thấp, dùng `SwarmSimulationSystem.SpawnEnemy(float2, EnemySpawnSettings)`; API này trả `Entity` và yêu cầu vị trí hợp lệ. Có `TryResolveSpawnPosition` để giải quyết vị trí trước khi gọi mức thấp.

```csharp
GameObject view = bridge.SpawnGameplay(unitPrefab, position, Quaternion.identity);
```

Với Unit/resource thông thường, API tạo view và đăng ký dữ liệu ECS tương ứng. Với prefab enemy, nó chuyển sang `SpawnEnemy` và trả `null` dù spawn có thể đã thành công; không dùng `view != null` để kết luận việc sinh quái. Dùng API `SpawnEnemy` khi cần biết thành công/thất bại của đường enemy.

Các đường hiện có: `EnemyWaveManager.SpawnEnemy` gọi bridge; `ProductionBuilding`, `BuildingProcess`, `ResourceSpawner` dùng `SpawnGameplay`. Direct `Instantiate` của enemy có đường chuyển đổi tại lifecycle khi bridge đã sẵn sàng, nhưng API trực tiếp tránh tạo view quái tạm. Các loại prefab hoàn toàn mới vẫn cần mapping riêng.

### Command thay cho việc ghi view

```csharp
unit.MoveTo(destination, DestinationSource.PlayerClick);
unit.SetDotsTarget(enemyEntity);
worker.TryAssignResourceNode(resourceNode);
worker.SendToBuild(structure);
```

Đây là ví dụ các API hiện có, không phải một chuỗi lệnh phải thực hiện liên tiếp. `PlayerClick` có ý nghĩa hủy mục tiêu/công việc và tạm ngăn tự tìm địch; chỉ chọn `CodeTriggered` khi muốn đúng hành vi đó.

Bridge chuyển destination từ tọa độ view sang tọa độ `CombatBody` có offset collider. Nếu gọi `GameplaySimulationSystem.SetDestination` trực tiếp, đầu vào là tọa độ body; không bỏ qua offset khi chuyển từ pivot sprite. Teleport cần command cập nhật cả `Position` và `PreviousPosition`, xử lý đường đi/công việc và kiểm tra ô hợp lệ.

Tác động máu qua `SwarmSimulationSystem.QueueDamage`. Với actor đã bind, `Unit.TakeDamage` cũng chuyển yêu cầu sang ECS. Bridge chỉ phản chiếu số máu và hiệu ứng; không gọi thêm damage legacy sau khi ECS đã áp dụng.

## 3. Thêm một loại quân hoặc hành vi mới

1. Chốt dữ liệu và hành vi cần có trong `GameplayComponents.cs` hoặc `SwarmComponents.cs`. Phân biệt vai trò thật với việc chỉ đổi sprite/thông số của vai trò hiện hữu.
2. Ánh xạ prefab/class authoring trong `RegisterGameplayUnit` hoặc `Unit.GetDotsSettings`/`SwarmGameBridge.SpawnEnemy`. Mapping hiện tại dùng kiểm tra kiểu class; một subclass mới có thể rơi vào vai trò cũ nếu chưa được phân biệt.
3. Bổ sung nhánh xử lý trong đúng job; với dữ liệu nhiều phần tử, xem các buffer `GameplayWaypoint`, `GatheringProfileData`, `TrainingRequest` đang có.
4. Nối command và presentation qua Integration. Giữ `RTS.DOTS` độc lập với `Unit`, `GameManager`, Animator và UI.
5. Nếu là loại quái có hình mới, thêm visual library/mapping. Hiện bridge ánh xạ `EnemyRangerUnit` sang visual 1, enemy khác sang visual 0; chỉ thêm prefab không tự tạo visual/AI mới.
6. Đối chiếu các enum state/task với adapter trong `Unit`, nhất là các phép cast giữa `GameplayState`/`UnitState` và `GameplayTask`/`UnitTask`.
7. Kiểm tra spawn, command, đổi mục tiêu, chết, hủy, pool và tải lại scene; cập nhật docs cùng code.

Không đưa logic mới vào `HumanoidUnit.UpdateBehaviour` rồi cho rằng actor DOTS sẽ chạy nó: `HumanoidUnit.Update` đang bỏ qua đường này khi `IsDotsControlled`. Tương tự, `StructureUnit.Update` chỉ đồng bộ presentation xây dựng cho actor đã bind.

## 4. Thêm công việc hoặc module kinh tế

Khai thác, claims và ví có một writer trong `GameplayEconomyJob`. Thêm loại hàng hoặc thay chu trình cần cập nhật đồng bộ profile, worker state, resource/depot mask, wallet, command và UI mapping. Đặc biệt, không đổi nhãn hàng đang mang khi đổi node sang loại khác.

Xây/huấn luyện dùng ID yêu cầu để bridge nối event native với sprite/action queue/prefab. Component runtime chỉ giữ dữ liệu mô phỏng; chuỗi UI và tham chiếu prefab nằm ở Integration. Khi thêm hủy hàng đợi, hoàn tiền hoặc nguồn tài nguyên có hạn, thiết kế cả transaction và lifecycle; các chức năng mới đó không tự có chỉ vì đã dùng ECS.

## 5. Tích hợp procedural generation trong tương lai

**Chưa có module procedural generation hoàn chỉnh.** Ba layout Swarm là kịch bản mô phỏng dựng sẵn. Khi triển khai module riêng, dùng thứ tự tích hợp sau:

1. Tạo dữ liệu bản đồ và seed trong module mới; ghi rõ thuật toán và khả năng tái lập.
2. Áp dụng dữ liệu địa hình/vật cản vào Tilemap trên main thread, rồi cập nhật pathfinding/`NavigationVersion` theo API của `TilemapManager`.
3. Bảo đảm `RefreshNavigation` chụp lại bản đồ và cấu hình **cả** `GameplaySimulationSystem` và `SwarmSimulationSystem`. Hiện bridge kiểm tra version theo nhịp, không có API rebuild tức thì cho module ngoài; nếu cần sinh ngay trong frame, bổ sung một điểm đồng bộ rõ ràng.
4. Chỉ spawn Unit/resource sau khi grid mới đã sẵn sàng. Resource/view và obstacle data cần phản ánh cùng bố trí; việc chỉ đổi tile không tự dựng mọi Entity tài nguyên.
5. Khi thay toàn bản đồ trong lúc chơi, thiết kế rõ việc hủy Entity cũ, giải phóng claims, cập nhật đường đi, depot/shelter, fog/minimap và mục tiêu chính. Không gọi mỗi `ResetSimulation` rồi mặc định gameplay và view đã được reset đầy đủ.
6. Tạo `Docs/Modules/ProceduralGeneration/` với Architecture, Usage, Validation và liên kết từ `Docs/README.md` khi bắt đầu implementation.

Đây là yêu cầu thiết kế cho lần phát triển sau, không phải API generator đã có sẵn.

## 6. Bộ nhớ, thread và vòng đời

- Unity objects được đọc/cập nhật trên main thread qua bridge; job nhận dữ liệu unmanaged/snapshot.
- Giữ dependencies và các ranh giới `Complete()` đúng khi đổi stage. Việc bỏ một `Complete()` cần thiết kế lại quyền truy cập dữ liệu, không chỉ xóa lệnh để mong tăng tốc.
- System sở hữu các NativeContainer/collider blob được tạo trong runtime; giữ đúng lifecycle dispose và tránh thay grid khi job còn sử dụng.
- `Gameplay.Remove` dọn công việc của worker trước khi xóa Entity. Phân biệt actor gameplay với quái/đạn/obstacle khi chọn API remove/reset.
- `Entity` có index và version; kiểm tra handle tồn tại trước khi dùng. `ExternalId` hiện liên kết view bằng Unity instance ID, không phải ID bền vững cho save/load hoặc networking.
- Những tên như `m_Proxies`/`ProxyFor` vẫn tồn tại từ giai đoạn chuyển đổi. Với Unit gameplay đã bind, Entity đó mang dữ liệu mô phỏng thật, không chỉ là bản sao collider thụ động.

## 7. Chẩn đoán nhanh

| Hiện tượng | Điểm kiểm tra đầu tiên |
|---|---|
| Sửa `WorkerUnit.UpdateBehaviour` nhưng game không đổi | Unit đã bind; sửa FSM/job hoặc command tương ứng |
| Đã sửa Transform nhưng vật thể bật lại vị trí cũ | `CombatBody` vẫn là nguồn chính; bridge đang ghi snapshot |
| Quái không xuất hiện dưới dạng GameObject | Kiểm tra `SwarmAgent` và `SwarmInstancedRenderer`, không tìm Collider2D từng quái |
| Spawner enemy trả null | Kiểm tra đang gọi `SpawnGameplay` hay `SpawnEnemy`; hai API có kiểu trả về khác nhau |
| Unit mới vẫn chạy AIPawn hoặc đứng im | Bridge/grid đã sẵn sàng chưa, có mapping/bind Entity và đúng component chưa |
| Xây bản đồ mới nhưng quân đi theo đường cũ | `NavigationVersion`, grid của cả hai system và đường đi đang giữ |
| Máu giảm hai lần hoặc có hai viên đạn | Đường legacy/Animation Event có còn chạy cùng đường ECS không |
| Cừu chết nhưng Entity còn tồn tại | Kiểm tra vai trò resource: xác cừu được giữ để lấy thịt |
| Timing Swarm thấp nhưng frame RTS vẫn chậm | Đo thêm Gameplay system, bridge, Animator, UI và rendering |

## 8. Xác nhận sau thay đổi

Chọn kiểm thử theo phạm vi sửa: EditMode cho state/thuật toán, Player smoke cho bridge/spawn/prefab, thao tác thủ công cho input và UI. [Gameplay Usage](../DOTSGameplay/Usage.md) có lệnh chạy và thu bằng chứng; [Swarm Usage](Usage.md) có cách chạy demo/benchmark.

Kết quả 32 test và 18 check trong tài liệu là bằng chứng đã lưu của ngày 16/10/2026, không phải test tự động đã chạy lại trong lần viết docs này. Sau thay đổi behavior, tạo artifact mới và ghi đúng ngày/kịch bản. Không suy diễn tải hàng nghìn worker từ benchmark quái.

Nguồn đối chiếu implementation: [Gameplay Architecture](../DOTSGameplay/Architecture.md), [Swarm Architecture](Architecture.md), `Assets/Game/DOTS/Runtime`, `Assets/Game/DOTS/Integration`, `Unit.cs`, `AIPawn.cs`, `HumanoidUnit.cs`, `StructureUnit.cs`, `ProductionBuilding.cs` và `TilemapManager.cs`.
