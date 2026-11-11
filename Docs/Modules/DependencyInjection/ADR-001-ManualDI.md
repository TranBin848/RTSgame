# ADR-001 — Manual dependency injection cho gameplay scene

Ngày quyết định: **09/11/2026**. Trạng thái: **D1 đã triển khai ngày 10/11/2026** cho root/bridge/GameManager/locators; consumer còn lại tiếp tục ở D2–D4.

## Bối cảnh và mục tiêu

Các consumer gameplay đang lấy `GameManager.Get()`, `TilemapManager.Get()`, `SwarmGameBridge.Instance` hoặc tìm component trong scene. `GameManager.Awake` tự tạo hai resource locator. Bridge tự gắn qua `SceneManager.sceneLoaded`, tìm nhiều manager trong `Awake`, rồi bind Unit/resource trong `Start`. Những đường này làm dependency ẩn và khiến việc chứng minh thứ tự khởi động, thay thế dependency khi test và reload scene khó kiểm soát.

Mục tiêu D1 là một đường khởi tạo rõ cho `ProceduralGame`, đồng thời giữ `SampleScene` và demo chạy trong giai đoạn chuyển. Bảng caller và ngoại lệ nằm ở [Inventory](Inventory.md); snapshot vị trí code nằm ở [LookupScan](LookupScan-2026-11-09.txt).

## Quyết định

Sử dụng **manual DI**. Một `GameSceneCompositionRoot` tại scene gameplay lắp các reference scene, tạo service C# thuần, lấy World/system ECS tại ranh giới tích hợp, inject bằng hàm `Initialize` có tham số tường minh, rồi báo Ready. Các class C# thuần nhận dependency qua constructor. `MonoBehaviour` giữ serialized reference cho asset và component nội bộ; dependency nghiệp vụ của scene được root cấp.

Root sở hữu các service nó tạo và subscription của chính nó. Unity sở hữu GameObject/scene; ECS World hiện tại do Unity Entities sở hữu. Root không dispose World mặc định. `PlayerWallet` trong ECS tiếp tục là nguồn tiền duy nhất. Không inject managed interface vào Burst job.

Không dùng container hay service locator trong đợt này. Không có container khai báo trực tiếp trong manifest để tái sử dụng cho gameplay; các DLL `Microsoft.Extensions.*` được kéo theo package khác không chứng minh dự án đang dùng chúng làm composition root. Manual DI đủ cho một gameplay scene, một menu scene và các service hiện có.

## Hợp đồng khởi động D1

1. Root tồn tại trong `ProceduralGame` với reference bắt buộc được gán hoặc validate rõ. Root là điểm duy nhất dựng bridge ở scene đó. Auto-installer cũ nhận biết scene đã có root và không gắn bridge thứ hai. `SampleScene`/demo chưa migrate vẫn dùng bootstrap cũ.
2. `ProceduralMapAuthoring` hiện chạy ở `Awake` với execution order `-300`; bridge chạy `-200`. D1 phải kiểm tra map Ready và grid hợp lệ trước khi bind. Không chỉ đổi số execution order để che dependency. Các consumer không tick khi chưa Initialize.
3. Unit/resource có `Awake`/`Start` và prefab active có thể chạy trước lệnh inject. D1 chỉ chuyển các consumer thuộc phạm vi root/bridge, giữ adapter legacy cho Unit đến D3/D4. Khi chuyển factory/pool sau này, bind/rebind tường minh trước behavior, dọn ở return/unload.
4. `Initialize` từ chối thiếu dependency, không subscribe hai lần và không tạo hai Entity cho cùng view. Teardown hủy event và reference theo scene; không dọn World của Unity.
5. Kiểm thử: scene không root, scene có root, thiếu reference, Play/Exit/Play với domain reload bật/tắt; `MainMenu → ProceduralGame → MainMenu`; đếm bridge, bound Unit và ví. Giữ baseline smoke đã lưu.

## Phương án cân nhắc

| Phương án | Quyết định | Lý do |
|---|---|---|
| Giữ singleton/Find trong mọi consumer | Không chọn | Dependency và thứ tự khởi động vẫn ẩn; khó fake/test độc lập |
| Cài DI container | Chưa chọn | Thêm dependency và cấu hình cho graph nhỏ; chưa giải quyết tự động lifecycle Unity/ECS hoặc auto-bootstrap hiện tại |
| Manual DI + root scene | Chọn | Ít mã mới, dependency rõ, kiểm thử được, đi từng nhóm mà giữ scene cũ |

## Hệ quả và giới hạn

D1 sẽ sửa bootstrap bridge, scene config, caller root và tests; D2/D3/D4 tiếp tục chuyển consumer còn dùng global access. Việc có root ở `ProceduralGame` chưa đủ để tuyên bố cả project tuân thủ DI. Lookup trong Editor tooling và smoke test được phép như ngoại lệ có chủ sở hữu; `Resources.Load` cho asset hình ảnh là lookup asset, không phải service nghiệp vụ, nhưng D4 vẫn có thể chuyển sang serialized asset nếu cần kiểm chứng cấu hình. `ProceduralMapRuntime` là static mutable scene state; D1 cần kiểm reset/Ready và D4 cân nhắc đưa vào scene-owned context sau khi mọi caller đã được kiểm kê.

## Bằng chứng và nguồn

- [Inventory D0](Inventory.md), [snapshot 149 vị trí](LookupScan-2026-11-09.txt), [kiến trúc DOTS](../DOTS/Architecture.md), [hướng dẫn phát triển DOTS](../DOTS/DevelopmentGuide.md).
- Scene YAML: `ProceduralGame`/`SampleScene` có GameManager, TilemapManager, EnemyWaveManager, DayNightCycleManager; `ProceduralGame` có ProceduralMapAuthoring. `MainMenu` có GameFlowMenu.
- [Baseline S0](../Progression/Evidence/baseline-rerun-2026-11-05.md): 34/34 DOTS EditMode, 3/3 procedural EditMode, 18/18 Player smoke. Các kết quả này đo trước khi triển khai D1.
