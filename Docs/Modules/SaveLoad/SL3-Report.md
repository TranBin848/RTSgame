# SL3 — Catalog thực và capture toàn trận

Ngày **24/11/2026**. **DONE phạm vi implementation/Editor SL1 và SL3**. [Progress](Progress.md), [BuildPlan](BuildPlan.md). Chưa có hydrate/restore hoặc Continue; SL4–SL9 vẫn mở.

## Mục tiêu, dữ liệu và thuật toán

Hoàn tất catalog whitelist từ prefab/action/config thực, capture authoritative ECS cùng state managed ở cuối frame, giữ pause/input và không mất completion. Không thêm I/O vào native Runtime hoặc đổi balance/render pipeline/package.

Implementation: `SaveContentCatalog`/`SaveDefinitionTag`/Editor bake; native `SimulationSnapshot` + shared fence/command guards; partial `BridgeSaveCapture` giữ request/action mapping; root inject `MatchCaptureService`; các managed owners cấp snapshot getter tường minh. `Tools/ValidateSaveCapture.cs` là harness disposable fixture. [Architecture](Architecture.md) mô tả luồng/cấu trúc/thuật toán/giới hạn; [Inventory](Inventory.md) mapping từng field; [Usage](Usage.md) cách chạy.

Catalog21prefab/8action/6tech; prefab tag là definition identity, SaveIdentity vẫn là instance64bit. Tag thêm qua PrefabUtility, Unity đồng thời chuẩn hóa serialization/default fields ở một số prefab; không chủ động đổi HP/giá/duration. Hall prefab100/scene1200 được giữ và capture MaxHealth1200 đã kiểm. Native renderer visual mapping0/1/2/3 được giải bằng whitelist; không lấy asset/type/path từ payload.

Fingerprint bakes bytes content/config/scripts thực, gồm file chưa commit, dependencies scene và Resources-loaded tactical plan. Duplicate prefab/unknown resolver bị từ chối. Hash lặp ổn định; sửa bytes TacticalNightPlan làm hash thay và restore bytes trở về hash cũ, bằng chứng [CatalogChecks](Evidence/SL3-2026-11-24/CatalogChecks.json). Hash trong evidence phản ánh source lúc kiểm; manifest source cuối ghi riêng, không dùng git HEAD làm compatibility.

Capture flush đã commit nhưng chưa publish trước khi acquire lease, không advance simulation để tạo snapshot. Native query gồm logical actors/resources/enemies/projectiles, loại cache obstacles; shallow component copies và deep buffer/array copies. Collider pointer/ExternalId không ra DTO; footprint box lấy từ collider native. Bridge nối action/prefab/FIFO/construction/offset/suspended work; generator giữ placement-instance reference theo index. Không normalize sai claims thành đúng: mismatch phải fail. Lease/finally khôi phục toàn guard sau thành công hoặc lỗi.

## Kiểm chứng integration

- [Fixture seed1](Evidence/SL3-2026-11-24/FixtureChecks.json):160logical objects, cargo7, worker sheltered còn ResumeResource, construction elapsed1.25, FIFO2, research economy_tools elapsed2.5, native enemy/đạn giữa quỹ đạo. Pending damage7 làm HP100→93 đúng1lần. Capture không đổi ví, tick counters, projectile Age.25 hoặc elapsed. Capture→encode→decode so toàn field PASS; đây chưa phải hydrate.
- [Capture lặp](Evidence/SL3-2026-11-24/RepeatedCapture.json): canonical state khớp, chỉ bỏ UUID/capture UTC/generation là metadata có chủ ý. Không mất hàng/queue và không trừ damage lại.
- [Failure/recovery](Evidence/SL3-2026-11-24/FailureRecovery.json): definition worker bị đổi sang ID lạ, capture bị từ chối; native/game/clock/wave/camera guards đều nhả, timeScale0 giữ nguyên; sửa fixture rồi request lại thành công.
- [Completion flush](Evidence/SL3-2026-11-24/Completions.json): chủ động đặt builder đã đến công trình, producer/research sát completion và tick native.02 để tạo queue chưa bridge apply. Capture flush tạo đúng1worker (tổng4), construction Finished, queue2→1, researchCompleted1. [Capture lặp sau completion](Evidence/SL3-2026-11-24/FinalRepeat.json) khớp state, không replay.
- [Running capture](Evidence/SL3-2026-11-24/RunningCapture.json): timeScale1/Simulation.Pausedfalse được giữ; snapshot detached tick350 vẫn nguyên khi live đã tick1005; game/clock/wave/native fence false sau capture.
- Seed[0](Evidence/SL3-2026-11-24/Seed0Capture.json)/[1543](Evidence/SL3-2026-11-24/Seed1543Capture.json): snapshot/encode/decode/mapping150placement/IDunique PASS. Reload cùng default World không sinh simulation cạnh tranh; override chỉ trong Play, asset seed1 giữ nguyên.
- Sau phát hiện getter SaveSettings lấy seed asset thay override, đã đổi lấy seed thực GeneratedMap; rerun seed0/1543 kiểm cả SettingsJson.Seed=Map.Seed PASS. Không sửa thuật toán generation.
- [SampleScene legacy](Evidence/SL3-2026-11-24/LegacySampleScene.json) bind/25actors PASS, không có capture service; exit Play trả scene ProceduralGame sạch.

Harness dùng command authoring thật cùng đặt trực tiếp một số component để tái tạo edge cases; không coi là trận chơi tự nhiên hoặc full gameplay acceptance. Lượt thử đầu completion fixture chưa đặt builder tới công trình nên không tạo build event; đã sửa precondition, reset disposable match và chạy lại toàn chuỗi PASS. Không sửa gameplay để làm fixture qua.

## Hồi quy

Kết quả Test Runner sau source cuối: **145/145 PASS**,0skip:

| Assembly | Kết quả | Duration Test Runner | Raw |
|---|---|---|---|
|RTS.DOTS.Tests|81/81|3.1344471s|[JSON](Evidence/SL3-2026-11-24/DotsTests.json), [XML](Evidence/SL3-2026-11-24/DotsTests.xml)|
|RTS.SaveLoad.Tests|47/47|60.3852376s|[JSON](Evidence/SL3-2026-11-24/CoreTests.json), [XML](Evidence/SL3-2026-11-24/CoreTests.xml)|
|RTS.ProceduralGeneration.Tests|17/17|6.7023537s|[JSON](Evidence/SL3-2026-11-24/ProceduralTests.json), [XML](Evidence/SL3-2026-11-24/ProceduralTests.xml)|

Các count139/139 của SL0–SL2 là lịch sử, không thay lượt này. Native tests mới kiểm capture không fence bị từ chối, pending event không bị Clear, damage apply1lần, nested mutation/ticks bị khóa, dispose sau exception, detached buffer/collider và box geometry native.

## Đo sơ bộ và giới hạn

Fixture seed1 file217.077bytes, capture55.0752ms; baseline cuối seed0 file213.960bytes/capture43.64ms, seed1543 file216.037bytes/capture6.4113ms; running seed1 capture76.7351ms. Đây là các **mẫu đơn Editor**, khác số objects/frame/caches, chứa Complete/flush/conversion/validation và instrumentation; không phải median/p95/Release budget hoặc cam kết hitch. Encode/file IO không nằm trong CaptureMilliseconds. Snapshot đầy đủ giữ ở Outputs; evidence Docs chỉ lưu summary không đường dẫn user. SL8 cần đo5lượt và Release theo BuildPlan.

Chưa kiểm fresh-process hydrate, rollback, action pricing khi load, wave/fog/report sau restore,10chu kỳ load, UI/autosave/quit. Chưa có service tự ghi player slot. Unknown native visual/queue context/claims mismatch bị fail rõ; API hydrate phải có preflight semantic theo config. Scene owner hủy pending capture khi disable/unload; chưa có background Task hoặc mutation load/newgame để chạy đua. Capture đang chạy là main-thread synchronous không yield.

Nguồn: source local có fingerprint trong evidence; [Architecture và tham khảo API](Architecture.md). Bước kế tiếp **SL4 bootstrap LoadGame trước Awake + map/entity hydrate/remap**, sau đó SL5/SL6 khôi phục sâu và rollback trước UI.

Editor cuối: [FinalEditorState](Evidence/SL3-2026-11-24/FinalEditorState.json) xác nhận ProceduralGame sạch, ngoài Play/pause/compile,0compileerrors, override null/timeScale1/runInBackgroundfalse. [Source fingerprints](Evidence/SL3-2026-11-24/SourceFingerprints.json) ghi source/assets thực; chưa commit hoặc build Release. Font fallback bị Unity ghi lại khi chạy fixture đã trả về nội dung trước phiên; các thay đổi workspace sẵn có được giữ.
