# SL4 — Bootstrap LoadGame và hydrate hai lượt

Ngày **24/11/2026**. **DONE phạm vi lát cắt kỹ thuật SL4** theo [BuildPlan](BuildPlan.md): map logic cuối, instance IDs, actor/building/resource/native enemy trong scene/process mới; không cộng tiền đầu trận hoặc nhân resource. [Progress](Progress.md) giữ SL5–SL9 mở.

## Phạm vi đã triển khai

SL4 là lát cắt kỹ thuật **từ MainMenu**. `MatchLoadBootstrap.LoadFromMenu(bytes, catalog)` decode/validate/catalog preflight trước đổi scene, chỉ giữ detached DTO xuyên scene. Generator Awake nhận nhánh LoadGame trước root: áp terrain/reserved/topology/origin/cell size từ snapshot, không gọi generator hoặc SpawnResources, vô hiệu Unit authoring và legacy spawners. Layout factory clone arrays, không tái sinh topology từ seed.

Root cấu hình cùng default World, giữ native paused; bridge tạo shells/views theo thứ tự resource/building/actor/enemy, rồi remap toàn bộ quan hệ từ SaveId ở lượt hai. Import API nằm trong RTS.DOTS, không biết JSON/path/GameObject. Collider được dựng từ radius/box native snapshot; actor path cache yêu cầu rebuild. View khôi phục HP/MaxHealth/state/attack sequence im lặng, không Die/TakeDamage/popup/reward lịch sử. Map placements gắn lại đúng instance; tombstone0 vẫn mất, không sinh lại vì seed còn đó.

Ví/research unlock đã hoàn tất/statistics/counters được hydrate bằng setter chuyên biệt, không gọi grant/purchase; `BeginGameplay(false)` chỉ lắp subscription/UI, không cộng100. `nextSaveId` được đối soát/đặt lại trước gameplay sinh object tiếp; obstacle proxies là cache nên từ SL4 không cấp instance SaveIdentity và không đẩy allocator lên do nav rebuild. ID của actor và resource cừu vẫn chung một Entity.

Clock/wave đầu ngày, explored/camera/selection được restore tối thiểu để không công bố Ready với màn hình mới trái checkpoint. Native render cache rebuild ngay khi pause để quái hiển thị không cần advance tick. Trận giữ Time.timeScale0 **và** Simulation.Paused=true, camera bị khóa đến khi bấm Resume; Resume nhả cả hai. Clock/wave đọc cờ paused chung để không ăn thời gian đã được Unity tính trước frame. Map/import lỗi giữ blocked/paused và LastError; không resume world nửa hydrate.

## Biên hỗ trợ và preflight

Chỉ nhận first Day/day1, active work/cargo/shelter/suspended work/FIFO/research/projectile không có; dead actor presentation bị từ chối. Save có các state này vẫn capture được ở SL3 nhưng loader SL4 báo cần SL5/SL6 **trước rời menu**. Clock duration/settings/seed/dimensions, native visual-definition, action-prefab đã xây và wallet integer range được kiểm thêm sau Core validator.

Chỉ load từ MainMenu, không nhận load đè trận đang chơi vì rollback toàn trận chưa được triển khai (SL6). Không UI Continue/load slot/autosave (SL7), không migration compatibility. Đây không phải Save/Load hoàn chỉnh để nghiệm thu KLTN.

## API và file chính

- `MatchLoadBootstrap.cs`: owner detached request, preflight, success/error và chuyển scene.
- `MapSnapshotRestore.cs`/ProceduralMapAuthoring/StrategicLayout: terrain snapshot và topology clone trước Awake root.
- `SimulationImport.cs`: paused shell/apply object/player/finish identity/counters/render cache.
- `BridgeSaveRestore.cs`: resolver/view registry, hai lượt remap, authoritative values và body/view offsets.
- Root/GameManager/DayNight/EnemyWave/Fog/Camera/Unit/GameFlowMenu: bootstrap mode, không starting grant, hydrate im lặng, loaded pause/resume.
- `Tools/ValidateSaveLoadSL4.cs`: Editor fixture và canonical recapture; `SL4PlayerSmoke.cs` + `Tools/BuildSaveLoadSL4.cs`/`RunSaveLoadSL4.ps1`: Windows capture process→exit→fresh load process, explicit CLI flag, không chạy trong gameplay bình thường.

## Cách kiểm và bằng chứng

Editor helper chạy trên disposable ProceduralGame: `Prepare` → một frame → `SaveAndMenu` → `Load` → frame → `RequestRecapture` → frame → `Verify`. Fixture đổi ví137W/89G/123M, mất1resource, thêm1kho, native enemies/target và selection. So mọi serialized canonical field sau hydrate, chỉ loại snapshot UUID/capture UTC/generation. Không loại float/state đang sai để pass. Output lớn giữ tại `Outputs/SaveLoad/SL4/`, summary/raw tests ở `Evidence/SL4-2026-11-24/`.

Ca Editor seed1 đã PASS160objects/149resource/3enemy, tombstone1, ví giữ nguyên, không grant100, quan hệ/IDs/selection khớp và next spawn dùng chính nextSaveId162. Cùng default World, không dispose/recreate World của Unity. Số World toàn Editor còn có các world do Editor/package quản lý, không coi tổng số World bằng1 là hợp đồng.

Windows smoke build Mono/x64/URP hiện hành, **hai process riêng mỗi seed0/1/1543 đều PASS**. Process capture ghi fixture rồi thoát; process load mới bắt đầu MainMenu, đọc cùng file, hydrate, recapture và so canonical. Fixture cuối sửa1terrain cell khác kết quả seed để phát hiện loader chỉ regenerate. Mode chạy có graphics device và cửa sổ ẩn, không dùng nographics vì presentation hiện có đòi shader.

|Seed|Capture process|Load process|Kết quả|
|---|---|---|---|
|0|[PID3128](Evidence/SL4-2026-11-24/Player-0-capture.json)|[PID25796](Evidence/SL4-2026-11-24/Player-0-load.json)|160objects,149resources,tombstone1,ví137W/89G/123M,canonicalPASS|
|1|[PID17700](Evidence/SL4-2026-11-24/Player-1-capture.json)|[PID24448](Evidence/SL4-2026-11-24/Player-1-load.json)|cùng invariants/canonicalPASS|
|1543|[PID2976](Evidence/SL4-2026-11-24/Player-1543-capture.json)|[PID5480](Evidence/SL4-2026-11-24/Player-1543-load.json)|cùng invariants/canonicalPASS|

[Build cuối](Evidence/SL4-2026-11-24/Build.json):Windows x64/Mono2x/Unity6000.3.11f1/BuildOptions.None, **Succeeded,0errors/0warnings**,51.4537174s. Đây là build smoke chức năng; không đóng SL9/manual/performance.

[Editor seed1](Evidence/SL4-2026-11-24/EditorSeed1.json) canonicalPASS. [Preflight10ca](Evidence/SL4-2026-11-24/PreflightMatrix.json) từ chối cargo/work/FIFO/research/projectile/night/day2/wallet overflow/settings mismatch/native visual không khớp, giữ MainMenu nguyên vẹn. [Apply geometry fault](Evidence/SL4-2026-11-24/ApplyFailure.json) sau preflight giữ root notReady/compositionblocked/nativepaused/timeScale0. Trở về menu rồi load hợp lệ và [Resume](Evidence/SL4-2026-11-24/Resume.json) nhả camera/native pause, tick2→717 xác nhận gameplay tiếp tục; không phải benchmark thời gian.

Các lỗi phát hiện qua thực nghiệm và đã sửa: native/clock frame backlog vẫn tiến sau timeScale0 → giữ shared paused flag; camera edge-pan trượt1frame trong Player nền → giữ camera lock tới Resume. Probe đầu thiếugraphicsdevice không dựng được E6 shader, không sửa pipeline để vượt kiểm; chạy lại với graphics bình thường. Raw lỗi/mismatch cũ giữ ở Outputs để chẩn đoán, không dùng làm evidence PASS.

## Kiểm thử tự động

**148/148 EditMode PASS**,0skip:

|Assembly|Kết quả|Duration|Raw|
|---|---|---|---|
|RTS.DOTS.Tests|83/83|2.5767759s|[JSON](Evidence/SL4-2026-11-24/DotsTests.json),[XML](Evidence/SL4-2026-11-24/DotsTests.xml)|
|RTS.SaveLoad.Tests|47/47|57.2908159s|[JSON](Evidence/SL4-2026-11-24/CoreTests.json),[XML](Evidence/SL4-2026-11-24/CoreTests.xml)|
|RTS.ProceduralGeneration.Tests|18/18|8.3517436s|[JSON](Evidence/SL4-2026-11-24/ProceduralTests.json),[XML](Evidence/SL4-2026-11-24/ProceduralTests.xml)|

DOTS thêm paused import/refs/values/allocator/render và duplicate IDs/cache proxies; procedural thêm layout clone/invalid args. Test Runner duration không là load latency.145/145 SL3 là lịch sử. Build/functional runs sau cùng kiểm thêm menu/camera/failure-hold thay đổi cuối; không dùng unit tests làm bằng chứng cho thao tác UI.

## Giới hạn, hiệu năng và hướng tiếp

Chưa có Release save/load latency median/p95, GC/native memory10lần, gameplay active queue sau load, clock/wave đêm/reports/rollback hoặc manual UI nghiệm thu. Windows build/process smoke chỉ kiểm phạm vi SL4, không đóng SL9. Giữ toàn bộ gates SL5–SL9 mở. Bước tiếp **SL5 worker/cargo → construction/FIFO → research → shelter/resume → combat/projectile**, sau đó SL6 clock/wave/fog/report/caches/rollback đầy đủ.

Nguồn nội bộ: [Architecture](Architecture.md), [Inventory](Inventory.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md), [DI](../DependencyInjection/Architecture.md). API và validation căn cứ source packages đã khóa trong project; không nâng Editor/package/render pipeline.

Artifacts cuối: [source hashes](Evidence/SL4-2026-11-24/SourceFingerprints.json), [Player hash](Evidence/SL4-2026-11-24/PlayerFingerprint.json), [Editor state](Evidence/SL4-2026-11-24/FinalEditorState.json). Editor ProceduralGame sạch ngoài Play/pause/compile,0compileerrors, không pending load, seed override null,timeScale1/runInBackgroundfalse. Workspace chưa commit và giữ các thay đổi sẵn có.
