# D4/S9 — phạm vi còn lại và chuẩn bị ngày17/11/2026

**Đã được thay thế:** D4a/b và audit hiện đã triển khai/kiểm, xem [D4 Validation](D4-Validation.md). Nội dung draft bên dưới là lịch sử chuẩn bị trong khi user test S8; không copy Temp draft vào code mới. S9 đang làm, xem Progress.

**Trạng thái: audit và draft, chưa áp dụng D4 vào Assets, chưa chạy kiểm thử D4.** Người dùng đang test S8; giữ Editor/code game ổn định tới khi test xong. Không dùng tài liệu này để đánh dấu D4/S9 DONE.

## Kết nối và điểm dừng

Cloud MCP thấy RTSgame2026, instance `3ccdabb5-ecdd-4b49-b5e1-108238b58493`. Chat chỉ expose instance tools; CLI đã cài/cache phiên bản1.11.3 gọi được rql-query/script-execute qua cloud. Không cài package hoặc khởi động Editor thứ hai. `--no-start` luôn dùng cho lệnh Editor.

Read-back đầu lượt: ProceduralGame sạch, isPlaying/isPaused/isCompiling=false, không Exception. Đã chạy đối chứng S8 seed1 rồi Exit Play để người dùng chơi thủ công. Trong thời gian user test **không copy draft vào Assets, refresh, reload scene, test runner hoặc build trên Editor của họ**.

## D4a — UI và cursor

Đã đọc callers/root/bridge và chuẩn bị10file draft tại `Temp/D4-Draft/`; manifest ghi source/draft. Đây là file tạm ngoài Assets, **chưa compile hoặc áp dụng**; nếu folder không còn, dựng lại từ bảng dưới và source hiện tại. Đọc diff trước khi copy để giữ thay đổi phát sinh trong lúc chờ.

| Consumer | Dependency được truyền | Việc phải làm |
|---|---|---|
| DayCounterUI / DayNightClockUI | DayNightCycleManager | Bỏ Find ở Start, Initialize/repeat guards; subscribe một lần, unsubscribe khi destroy |
| WaveProgressUI | EnemyWaveManager | Inject event source, giữ lịch/icon; không fallback Find |
| SelectedUnitsInfoPanelUI | GameManager | Inject selection source; giữ binding item và teardown |
| ResourceRequirementsDisplay | GameManager | Bỏ Get ở Awake, chặn Show trước Initialize; giữ local layout/asset lookup |
| IdleWorkerPanelUI | GameManager, CameraController | Bỏ Get/Find và fallback khi click; refresh lần đầu ở Start sau bridge bind; chỉ thêm/xóa listener của chính panel |
| MinimapFogUI | Fog, map, CameraController, Camera, bridge | Bỏ Find/Get/Instance/Camera.main trong consumer; giữ landmark init S7; destroy texture khi replace/destroy |
| CursorManager | GameManager, bridge, Camera | Bỏ Get/Instance/Camera.main trong nghiệp vụ hover; singleton base còn phải xử lý ở D4b |

Composition dự kiến: method chung `GameSceneCompositionRoot.ComposePresentation` nhận refs đầy đủ rồi duyệt scene-owned hierarchy một lần để Initialize đúng consumer. Root ProceduralGame gọi sau các dependency đã lắp; bootstrap SampleScene gọi cùng helper. Không thêm container, runtime registry/service locator hoặc biến static cache cho UI. Discovery chỉ nằm ở composition boundary. Chưa loại bootstrap legacy trước khi chuyển GameManager/fog.

Kiểm cần chạy khi áp dụng: null/different deps bị từ chối, repeat không tăng listener; UI inactive lúc init vẫn nhận dependency; disable/enable HUD; ngày/đồng hồ/wave; selection/requirements màu; idle focus; minimap pointer/drag/fog/enemy marker; cursor attack/build; menu→game→menu→game trên ProceduralGame và SampleScene; texture teardown. Dùng harness assert nhỏ và kiểm Unity thật, không chỉ rg.

## D4b — fog và bootstrap còn lại

- FogOfWarManager: bỏ GameManager.Get/TilemapManager.Get ở Initialize; root/legacy adapter cấp refs; giữ map-ready guard và refresh event. Registry ban đầu được composition/factory cấp, không Find trong consumer.
- FogOfWarAffectable: bỏ static cached manager và Find ở OnEnable; nhận owner fog từ scene composition hoặc bridge/factory cho view mới; register/unregister theo enable/disable và rebind khi pool. Phải kiểm resource views, sheep, spawned unit, pooled unit và view chưa Awake; giữ màu gốc khi rebind.
- GameManager: chuyển legacy dependency assembly/locators về composition adapter; loại fallback trong consumer sau khi SampleScene được nối rõ. Không xóa đường legacy trước khi caller đã chuyển.
- SingletonManager/Get: quét mọi caller trước khi bỏ base hoặc API; tooling/smoke có thể giữ discovery trực tiếp tại test boundary. Cursor/Tilemap/GameManager không được tự quét duplicate managers nếu đã bỏ nghiệp vụ singleton mà vẫn gọi base Awake.
- CameraController: lấy component Camera cùng GameObject là dependency nội bộ; Camera.main fallback phải chuyển ra composition hoặc serialized reference. Cursor/GameUtils/input cần đối chiếu camera và EventSystem như engine boundary, không chỉ đếm chữ Get.

## D4c — audit đóng phạm vi

Quét lại toàn `Assets/Game`, phân loại World/system access tại root/demo, scene actor/resource capture tại bridge, smoke/editor tools, Resources.Load asset visual, pool owner Scene và ProceduralMapRuntime. Mỗi ngoại lệ có owner/lifetime/lý do. Không coi149D0 là số còn lại hoặc tuyên bố cả project đạt DI chỉ vì8consumer mới hết Find. DOTS runtime tiếp tục độc lập Assembly-CSharp.

## S9 — đo và bàn giao sau D4

- Regression phù hợp, MainMenu/ProceduralGame/SampleScene và Player Windows nếu build support có sẵn. Không nâng Editor/package/pipeline để build.
- Reuse SwarmBenchmarkRecorder cho frame/GC/rawCSV, nhưng `ecs_tick_ms` hiện chỉ là Swarm; không gọi đó là toàn bộ thời gian gameplay ECS. Ghi explicit seed/worker/quân/quái, render thật, Editor/Player/resolution/máy/warmup/duration, median/p95 và giới hạn.
- Startup: đo request scene→Ready bằng callback/coroutine trong Editor/player, không lấy khoảng giữa hai MCP request làm latency. Tách Core, Tilemap, prefab, physics/navigation và binding bằng timestamps tại phase thực.
- ResearchPanel.Update đang GetDefinition/IsUnlocked theo frame; display key phụ thuộc elapsed cho cả5panel không active, làm dựng lại nhãn busy10Hz. Cache immutable definition/effect và chỉ dùng elapsed cho panel active; đo/kiểm steady polling không allocate, busy text không render lại vô ích. Thay đổi label/progress có allocation phải ghi rõ, không công bố0GC toàn game.
- Review existing gameplay smoke assumptions trước Player run: baseline là SampleScene; không tự áp kết quả cũ vào ProceduralGame/S8 hoặc ghi benchmark quái thành hiệu năng worker.
- Docs Architecture/Usage/Validation/Evidence/README/Overview/Progress cập nhật cùng code; DONE chỉ cho phần có bằng chứng. Không dùng manual S7 để đóng manual S8/D4.
