# Kiến trúc Save/Load

Ngày đối chiếu source: **24/11/2026**. Thực hiện theo [BuildPlan SL0–SL9](BuildPlan.md). Trạng thái thực tế nằm ở [Progress](Progress.md); schema và storage không đồng nghĩa đã có Continue toàn trận.

## Mục tiêu và phạm vi

Lưu toàn trận ProceduralGame, tiếp tục sau đóng ứng dụng, giữ pause sau load. Không áp dụng hợp đồng này cho SampleScene/SwarmSimulation. Không thay balance, pipeline hoặc phiên bản Editor. Không có offline income, cloud hoặc migration schema ở bản đầu.

## Các lớp và dependency

- `Assets/Game/SaveLoad/Core`, assembly `RTS.SaveLoad`: DTO, catalog thuần dữ liệu, validator, JSON codec, slot storage. Assembly không tham chiếu UnityEngine, ECS hoặc scene; dùng Newtonsoft.Json 3.2.1 đã có trong packages-lock, không thêm package.
- `Assets/Game/DOTS/Runtime/SaveIdentity.cs`: component `SaveIdentity` và allocator dùng chung ở SwarmSimulationSystem. Gameplay resource, actor, proxy, quái và đạn nhận identity từ cùng allocator. DOTS không tham chiếu codec hoặc đường dẫn file.
- Root inject `MatchCaptureService` với bridge, generator, game, clock, wave, fog, reports, camera và catalog. `BridgeSaveCapture`/`BridgeSaveRestore` là partial Integration của bridge; `SimulationSnapshot`/`SimulationImport` là API native không biết JSON/UI. SL4 restore first-day/idle-work đã nối, phần active state còn SL5/SL6. Không đặt Find/Instance trong consumer mới.
- Root ứng dụng/menu sở hữu operation xuyên scene; root gameplay chỉ sở hữu adapter theo scene. Global mutation fence và owner xuyên scene thuộc SL3/SL4/SL7, không suy ra từ khóa I/O của storage.

**Cập nhật SL4:** `MatchLoadBootstrap` giữ detached request qua MainMenu→ProceduralGame, generator nhận LoadGame trước Awake root; `SimulationImport` và `BridgeSaveRestore` dựng map/object/relationships hai lượt. Chỉ first-day/idle-work được nhận, các state active thuộc SL5/SL6 bị từ chối trước teardown. Load từ trận đang chạy vẫn bị từ chối đến khi có rollback SL6. [Phạm vi/API/evidence SL4](SL4-Report.md).

Assembly riêng Core cho phép Test Runner kiểm DTO/storage mà không phụ thuộc Assembly-CSharp, đồng thời giữ JSON/I/O ngoài RTS.DOTS. Không thêm abstraction storage hoặc DI container.

## Dữ liệu và thuật toán

`MatchSaveData` schema 1 dùng field tường minh, số 64 bit và mask dạng byte[] mã hóa base64. Tham chiếu instance là ID dương; 0 chỉ null. Một object có nhiều capability, ví dụ cừu có Actor + Resource, không tách hai object. `NextSaveId` lớn hơn mọi ID còn sống; allocator không reset khi ResetSimulation vì proxy/gameplay có thể còn tồn tại. Tăng ID đến gần long.MaxValue sẽ báo lỗi thay vì wrap.

`SaveCatalog` là whitelist ID → capability cùng action/technology IDs. Fingerprint SHA-256 gồm manifest được sort ordinal và chuỗi cấu hình caller cung cấp. SL3 có `ProceduralSaveCatalog.asset` trong Resources với21prefab/8action/6tech. Prefab có `SaveDefinitionTag` root, clone giữ definition ID và nhận SaveIdentity instance riêng. Rebuild giữ tag ID hiện có khi rename prefab; action ID dùng GUID asset authoring ổn định, không dùng Guid được new lúc runtime. Native visual0/1→Skull/Gnoll,2/3→Arrow/Gnoll_Bone theo whitelist. Unknown/duplicate/capability mismatch bị từ chối.

Editor `SaveContentCatalogEditor` bakes hash trước Play và build: bytes scene ProceduralGame + dependencies, prefab/action/research assets, mọi asset Resources và dependencies (bao gồm TacticalNightPlan), scripts/shaders/asmdefs trong Assets/Game, manifest/lock, các ProjectSettings liên quan và .meta. Bao cả thay đổi chưa commit; không dùng git HEAD. Manifest catalog tham gia hash, chỉ loại baked hash để tránh self-reference. Kiểm biến đổi tactical resource làm hash đổi đã PASS, restore bytes làm hash trở về cũ. Policy cố ý bảo thủ: đổi presentation hoặc serialization asset cũng có thể làm save incompatible. Chưa có migration. Bản schema1 vẫn là schema phát triển.

Codec xuất JSON UTF-8 với envelope `format/schemaVersion/payloadBytes/checksum/payload`. Payload là chuỗi JSON; checksum tính trên đúng bytes UTF-8 của chuỗi này, không serialize lại để kiểm checksum. Vì có escaping nên file lớn hơn payload. Header trận (capture UTC, snapshot UUID, generation, content fingerprint) nằm trong payload được checksum bảo vệ. Checksum phát hiện hỏng, không chống sửa save.

Decode chặn 64 MiB trước cấp buffer đọc, depth 24, duplicate JSON properties, trailing content, JSON metadata/type không thuộc DTO, field thiếu/thừa, scalar sai kiểu, số không hữu hạn, số nguyên ngoài miền. Kiểm shape trước ToObject; không làm tròn ID dạng 2.4 hoặc đổi chuỗi số thành ID. `TypeNameHandling.None`, không resolve type/asset từ file. Sau đó validator kiểm schema/config, identity, capability, quan hệ, mask/map, wallet/cargo, queue/request, research và state cơ bản. Giới hạn: 50.000 objects/placements, queue 256/producer, map 32..1024 mỗi chiều, selection 50.000, mỗi float hữu hạn và |value| ≤ 1e9. Đây là trần an toàn, không phải chứng nhận workload hoặc tốc độ tương ứng.

Validation theo gameplay config chi tiết (duration clock, quota wave, prerequisite tech, mapping action↔prefab, bounds camera) còn phải bổ sung tại preflight hydrate SL4–SL6. Capture hiện lấy những giá trị này từ owners thực và đối soát claims, queue/request context, catalog capabilities và mọi ID. Không sử dụng validator Core hiện tại như bằng chứng rằng một file bất kỳ đã đủ điều kiện hydrate.

## Storage và xử lý lỗi

Service sẽ truyền `Path.Combine(Application.persistentDataPath, "Saves")` từ composition. Core nhận directory tin cậy qua constructor; payload không chứa path. Chỉ hai enum slot `Manual` và `Dawn`, file tương ứng `manual.json`/`dawn.json`, `.bak` và `.tmp`. Đọc không sửa file bị lỗi. Nếu primary lỗi chỉ thử đúng một backup, trả `UsedBackup`, timestamp và `PrimaryError` để UI giải thích rõ.

Write: lock instance bằng Interlocked, lock directory bằng FileStream `slots.lock`/FileShare.None → kiểm thế hệ cũ → validate/encode → tạo temp cùng directory → write → Flush(true)/close → decode temp lại → File.Replace giữ primary cũ thành backup hoặc File.Move khi chưa có primary. Không có nhánh delete-primary trước commit. Nếu primary hỏng nhưng backup đọc được, write mới giữ backup tốt, không xoay file hỏng đè lên backup. Nếu cả hai hỏng/incompatible, write bị từ chối, không tự xóa chúng. Generation phải tăng so với thế hệ tốt đã kiểm. NewGame không được tự xóa slot.

Lỗi trước commit giữ nguyên primary/backup; temp dở bị bỏ qua khi đọc, lần ghi sau có thể thay temp. Fault sau commit có thể trả lỗi dù thế hệ mới đã commit; trạng thái đĩa mới vẫn đọc được, caller phải đọc lại generation trước retry. Không hứa exactly-once acknowledgement khi ứng dụng chết ở ranh giới commit. API hiện đồng bộ; chưa đưa I/O sang Task hoặc thêm background owner. Mất nguồn phần cứng/directory durability chưa được đo.

## Fence, hydrate và rollback đã chốt cho bước tiếp

Đọc [Inventory](Inventory.md) trước sửa adapter. SL3 `RequestCapture(generation)` nhận một yêu cầu, lần click thứ hai bị từ chối. `LateUpdate` order32000 xử lý sau callback gameplay thông thường. Guard GameManager/clock/wave/camera bật trên main thread; `PrepareSaveCapture` Complete hai system, commit damage/shots đã chờ, gọi SynchronizeGameplay để áp train/build/research events đúng1lần. Không advance clock/tick/timer để lấy snapshot, không Clear queue. Loop có giới hạn4pass, nếu writer callback tiếp tục enqueue hoặc external damage chưa có consumer thì từ chối capture rõ ràng.

Sau flush, `AcquireSnapshotFence` khóa cả hai ticks và native command APIs; nested fence/command mutation báo lỗi. Capture query Actor/Resource/Enemy/Projectile, không chứa obstacle proxy cache; sao chép component/buffers, bỏ pointer Collider/ExternalId, lấy BoxHalfSize từ collider native trước khi bỏ pointer. Integration remap SaveId, đối soát claims và queue action/prefab context, ghép map/explored/clock/wave/report/view. Mất definition hoặc live relation ngoài registry là lỗi; Entity đã destroy canonical vềnull. Lease/finally luôn nhả guard và trả camera/input; không sửa Time.timeScale hoặc Simulation.Paused trong capture. Snapshot giữ detached managed arrays, không truyền NativeArray/EntityManager sang I/O.

Map placements giữ reference gốc theo index từ generator, vì cừu có thể đã đi xa vị trí spawn. Object đã bị remove dùng tombstone0, không rematch bằng tọa độ hoặc spawn lại. SaveSettings ghi seed thực đã dùng kể cả override trong fixture. Bridge giữ ActionSO cho training và action của construction sau completion, để ID/action không mất khi registry UI được dọn.

Bootstrap LoadGame phải được chọn trước Awake generator/root/clock. Dựng map snapshot, không sinh lại resources từ seed; tạo resources/buildings trước actors/native enemies/projectiles, rồi nối ID ở lượt hai. Hydrate component trực tiếp, không gọi StartResearch/EnqueueTraining/BuildingProcess constructor mua mới. Dựng lại claims/nav/path/cache; hydrate report/clock/fog im lặng; đồng bộ health/attack sequences không popup hoặc reward lịch sử. Load thành công giữ pause. Trước teardown trận cũ, chụp rollback cùng coverage như save; nếu apply lỗi thì khôi phục checkpoint, rollback lỗi giữ simulation bị chặn và hiện đường về menu.

Fence/capture đã kiểm SL3; hydrate hai lượt/paused bootstrap first-day đã kiểm cả process Windows mới ở SL4. Hydrate active gameplay, later clock/wave/report và rollback vẫn là hợp đồng thiết kế SL5/SL6; chỉ đóng từng gate sau thực nghiệm theo BuildPlan.

## Cách chạy, kiểm thử và hiệu năng

Xem [Usage](Usage.md), [Validation](Validation.md). Test Core chạy DTO round-trip và filesystem thật trong directory tạm riêng từng test; DOTS test kiểm identity/factory/RNG và fence. SL3 có capture từ live ProceduralGame cùng controlled native component fixtures; sample4objects SL0–SL2 vẫn là dữ liệu nhân tạo riêng, không thay integration evidence mới.

Chưa đặt budget cứng và chưa đo save/load gameplay. SL8 phải đo riêng main-thread freeze, encode, write/flush/commit, read/validate, hydrate/remap/cache và Continue→Ready; ghi size/counts/seed/hardware/build/hash, median/p95/max. Thời gian Test Runner không thay phép đo này.

## Giới hạn và nguồn

Xem coverage chưa nối ở Inventory và gate mở trong Progress. Chưa có nút lưu/tải, autosave hoặc cold-process Continue. Replay bit-identical không nằm trong hợp đồng: paths và thứ tự spatial query được rebuild.

- [Unity JSON serialization](https://docs.unity.com/en-us/engine/6000.3/manual/programming-environment/code-reload-serialization/script-serialization/json-serialization): đối chiếu trước chọn serializer; cần kiểm duplicate/type/missing-field bằng Newtonsoft đã cài.
- [Unity persistentDataPath](https://docs.unity.cn/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html): vị trí dữ liệu persistent trên Windows.
- [Microsoft File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=netframework-4.8.1): thay file, backup và cùng volume.
- [Microsoft FileStream.Flush](https://learn.microsoft.com/dotnet/api/system.io.filestream.flush): Flush(true) yêu cầu flush buffer đĩa.
- Source/package local: `Packages/packages-lock.json`, `Library/PackageCache/com.unity.nuget.newtonsoft-json@4dfd81071c64/Runtime/`, `GameplayComponents.cs`, `SwarmComponents.cs`, các owner trong [Inventory](Inventory.md).
