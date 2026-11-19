# S9 — nghiệm thu, đo và bàn giao

Ngày17/11/2026. **S9 DONE trong phạm vi build plan**, có build/functional smoke/đo Player/raw artifacts và giới hạn bên dưới. S0–S9 và D0–D4 đã đóng; kết quả này không có nghĩa cân bằng cuối hoặc mọi nền tảng đã tối ưu.

## Phạm vi và cách chạy

Giữ Unity6000.3.11f1, URP và package của project; không nâng phiên bản/pipeline. S9 chạy regression, MainMenu/ProceduralGame/SampleScene, Windows Development Player, startup timings, frame/GC với world render thật và cập nhật docs. Không suy benchmark quái thành toàn bộ gameplay hoặc số liệu Editor thành Player.

Build bằng `Tools/BuildS9.cs` qua Unity MCP script-execute: `S9Build.Run`, ngoài Play/compile, scene saved. Dùng scene enabled hiện hữu, yêu cầu MainMenu đầu tiên và cả hai scene gameplay. Output `.utmp/s9-build-2026-11-17/RTSgame2026.exe` cùng thư mục Data; không đưa binary/cache vào Docs/git. `S9-Build-2026-11-17.json` là build cuối, bản first được giữ riêng.

Player `--gameplay-smoke --gameplay-output <folder>` chạy chức năng trên SampleScene theo harness hiện hữu. Player `--progression-smoke --swarm-output <folder>` load ProceduralGame từ MainMenu, giao việc3worker tới cây/vàng gần, nghiên cứu Tools và train1villager theo command/giá thật; không grant tài nguyên hoặc bảo vệ HP. Capture warmup30s +90s realtime, TimeScale1, qua ngày/dusk/đêm tự nhiên; không dùng manual tick để tính FPS.

## Instrumentation và thuật toán

- Authoring ghi `Stopwatch` riêng cho Core TryGenerate, ApplyTerrain, SpawnResources, Physics2D.SyncTransforms + ReinitializePathfinding. Chỉ tạo timer lúc load, không thêm timer/object vào tick gameplay.
- Root ghi Initialize (gồm renderer/cache/fog/UI composition) và BindScene (gồm navigation capture, collider/native actor/resource binding). Hai stage không đồng nhất với Core. ReadyRealtime là timestamp đúng tại root hoàn tất Ready/composition.
- Automation ghi thời điểm ngay trước SceneManager.LoadScene; scene→Ready dùng hai timestamp cùng process, không lấy khoảng giữa hai request MCP. Generator Awake→Ready là khoảng hẹp hơn; không cộng các stage rồi gọi là toàn bộ load. Một lần startup không đủ để tính median/p95 startup.
- Reuse SwarmBenchmarkRecorder, cap36000frame; frame time = unscaledDeltaTime, GC Allocated In Frame qua ProfilerRecorder nếu counter hợp lệ; memory sample1Hz. `ecs_tick_ms` là tick **Swarm**, không phải toàn gameplay ECS/CPU frame; tick ID có thể lặp giữa nhiều render frame.
- Camera target RenderTexture1920×1080 và SubmitRenderRequest theo smoke renderer hiện hữu để cửa sổ hidden vẫn chạy GPU world pipeline. Đây là offscreen capture có chi phí request riêng, không chứng minh overlay UI GPU cost hoặc FPS onscreen độc lập. UI logic vẫn chạy. Không dùng -nographics, không tắt renderer để lấy số tốt.
- `Tools/SummarizeS9.mjs` kiểm dữ liệu, seed1/v3, timing dương, combat xuất hiện và mẫu hữu hạn; nearest-rank median/p95, GC nếu khả dụng, range enemy/projectile, sampled duration/cap. Không đổi giá trị -1 (counter unavailable) thành0.

## Kết quả đã có

- Regression sau D4/cache ResearchPanel:81/81PASS,0fail/skip,10.9443911s.
- `Tools/ValidateS9ResearchAllocations.cs`:1000idle+1000busy-other-panel Updates,0/0B managed thread allocation, immutable definition đọc1lần. Không gồm progress text thay đổi, Unity layout/render hay GC toàn frame. ResearchPanel cache definition/effect và chỉ dùng elapsed làm display key khi panel đó đang nghiên cứu; các nhãn busy khác không dựng lại10Hz.
- Build đầu succeeded374.8836195s,226644058bytes,0errors/warnings. MCP request fetch-failed trước response; Editor log và JSON xác nhận Build Successful, không build lại mù hoặc kill Editor. Postprocess/copy là phần lớn thời gian, không gọi đó là runtime performance.
- Smoke đầu15/18,errors=[]: fixture cũ đặt tower ở góc map làm legacy ring vượt map; raw Instantiate không còn tự register theo hợp đồng D3. Sửa **harness**: chọn placement hợp lệ gần hall, caller explicit bridge.Register enemy. Giữ JSON fail đầu trong thư mục output, không sửa balance hoặc phục hồi singleton để chiều test.
- Build cuối succeeded47.6252491s,226645394bytes,0errors/warnings; MainMenu/ProceduralGame/SampleScene/SwarmSimulation đều enabled. Player cuối **18/18checks PASS**, errors=[],29actors/5enemies/208ticks khi snapshot kết thúc. Đây là functional smoke có native timers rút ngắn trong fixture, không là playtest cân bằng hoặc benchmark.
- Progression perf Player chạy riêng sau smoke kết thúc;19.191frame/89.998giây được ghi, không chạm cap36000;errors=[]. Đã inspect PNG có terrain/structures/quái/đạn thật,11render snapshots và4draw calls ở snapshot cuối. Caption: ảnh world offscreen, không chứa HUD ScreenSpaceOverlay.

## Số liệu Player đã đo

Máy: Intel Core i5-11300H3.10GHz/8logical cores, GTX1650, RAM16179MB. Unity6000.3.11f1, Windows Development Player,1920×1080, vSync1/targetFPS-1 theo config; cửa sổ hidden và explicit world render request làm số liệu không tương đương trình bày onscreen/vSync thông thường. Editor vẫn mở trên máy làm việc. Map seed1/v3; wave RNG không pin riêng, không phải deterministic combat replay.

| Chỉ số | Kết quả | Phạm vi |
|---|---|---|
| Scene request→Ready | 836.3275ms | Một lần MainMenu→ProceduralGame; không gồm toàn bộ process boot |
| Generator Awake→Ready | 533.7064ms | Đầu Awake authoring tới root Ready |
| Core TryGenerate | 102.3715ms | Generator và settings copy |
| ApplyTerrain | 112.0096ms | Tilemap/presentation terrain |
| SpawnResources | 24.0047ms | Prefab resources |
| Physics/pathfinding authoring | 25.1017ms | SyncTransforms + ReinitializePathfinding |
| Root Initialize | 133.4200ms | Dependency assembly/fog/UI/renderer cache |
| Bridge BindScene | 82.6452ms | Capture nav, collider, actor/resource binding |
| Frame ms median/p95/max | 3.7341 / 8.5298 / 53.9553 | 19.191frames, nearest-rank; mean4.6896ms |
| Sampled last Swarm tick median/p95 | 2.0597 / 3.0090ms | 5385unique sampled tick IDs; không là toàn gameplay ECS |
| GC bytes/frame median/p95/max | 144 / 184 / 1020544 | Counter khả dụng; mean221.94B/frame; chưa gán nguyên nhân spike |
| Unity allocated memory đầu/cuối | 257721252 / 258235626B | Sample1Hz; không suy chênh lệch thành leak hoặc0leak |

Trong cửa sổ frame có0–7quái và tối đa4đạn; snapshot sau capture/export là8quái/3đạn, tổng10spawn/31hits,26actors/4worker/ngày1. Hai thời điểm không đồng nhất nên không sửa max của CSV theo snapshot. Không đổi tốc độ đồng hồ/HP/giá để lấy số. Startup là một mẫu; không công bố startup median/p95. Frame max~54ms và GC spike~1.02MB là rủi ro cần profile khi tăng tải, không được bỏ khỏi báo cáo để chỉ trình bày median đẹp.

Artifact: [build cuối](S9-Build-2026-11-17.json), [build đầu](S9-Build-first-2026-11-17.json), [81tests XML](Evidence/S9-2026-11-17/editmode-tests.xml), [Player18checks](Evidence/S9-2026-11-17/gameplay-smoke-final.json), [fail đầu](Evidence/S9-2026-11-17/gameplay-smoke-first-failed.json), [result gốc](Evidence/S9-2026-11-17/progression-result.json), [summary](Evidence/S9-2026-11-17/progression-summary.json), [rawCSV](Evidence/S9-2026-11-17/progression-frames.csv), [PNG world](Evidence/S9-2026-11-17/progression-world.png), [SHA-256416files](Evidence/S9-2026-11-17/build-sha256.json). Log đầy đủ giữ trong .utmp; scan runtime lỗi ở cả smoke/perf:0matches cho Exception/errorCS/undisposed NativeCollection/Shader error. Không dùng memory-label summary lúc engine shutdown để khẳng định có/không leak.

## Dữ liệu, kiểm thử và giới hạn bàn giao

Evidence JSON/CSV/PNG/XML đã lưu tại `Evidence/S9-2026-11-17/`; raw build/smoke/perf lớn ở `.utmp`, không coi binary cũ là build của code hiện tại. Tên CPU/GPU/RAM, Unity/Editor flag/resolution/vSync/targetFPS có trong header CSV. Development Player và Editor còn mở trên máy làm việc không là môi trường lab cô lập; kết quả chỉ cho workload/seed/máy đã ghi. CSharp whitespace check0. Final MCP: ProceduralGame sạch/seed1/matchWired=true, isPlaying/isPaused/isCompiling=false,0Exception. Hai Player test đã tự thoát; Editor người dùng giữ mở, không job/build/CLI pending.

S8 có6policy/report và user xác nhận checklist; đối chứng seed1 không thao tác vẫn thắng là giới hạn cân bằng đã biết. D4 có runtime assertions/audit ngoại lệ, không có container. Chưa hỗ trợ nhiều gameplay scene additive, save/offline/prestige, mọi thiết bị cảm ứng hoặc mọi seed trong Player. Chưa đo Release, phân phối startup nhiều run, UI overlay GPU riêng hoặc tải quân/quái lớn hơn. Không tuyên bố0GC toàn game hoặc nguyên nhân native crash Editor cũ đã được sửa.

Nguồn: [BuildPlan](BuildPlan.md), [Progress](Progress.md), [D4 Validation](../DependencyInjection/D4-Validation.md), [MatchFlow/S8](../MatchFlow/Architecture.md), code timers tại ProceduralMapAuthoring/GameSceneCompositionRoot, recorder và automation tại `Assets/Game/DOTS/Runtime`/`Integration`, các Tools nêu trên.
