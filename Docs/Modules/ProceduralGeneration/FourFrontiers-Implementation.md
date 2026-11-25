# FourFrontiers v4 — triển khai FR0–FR7

Ngày bắt đầu: **24/11/2026**. Đây là nhật ký triển khai và bằng chứng; bảng trạng thái dưới đây được cập nhật sau mỗi kiểm chứng, không dùng kế hoạch làm kết quả.

## Hợp đồng FR0

Giữ `LegacyMainland=0/v1`, `StrategicRegions=1/v3`; thêm `FourFrontiers=2/v4`. ID vùng không phải role: `0 Start`, `1 Economy A`, `2 GoldFrontier A`, `3 Economy B`, `4 GoldFrontier B`. Bốn connection có `From=0`, `To=1..4` duy nhất. Hai gateway sở hữu region ID tường minh: bản đầu chọn vùng 1 và 2; xoay theo seed nên hướng thế giới thay đổi. Một landmark thuộc vùng 2. Không đổi ngày/đêm, tech, wave plan, actor hoặc economy simulation.

Tâm ngoài dùng bốn hướng vuông góc, rotation và jitter ±2 từ RNG layout riêng. Carve đĩa vùng và bốn tuyến L rộng 5 ô, bảo vệ pad 5×5 ở cả bốn tâm; chỉ hai tuyến được nối ra biên. Terrain ngoài phần bắt buộc tiếp tục dùng noise. Border hai ô là nước ngoài cửa đã khai báo. V3 giữ nguyên RNG và thứ tự placement.

Budget ngoài khởi đầu tính theo role rồi chia phần dư cho vùng có ID nhỏ hơn: mặc định `34/0/5`, `11/5/2`, `34/0/4`, `11/4/1` theo thứ tự Tree/Gold/Sheep. Ba vàng landmark nằm trong budget vùng 2; vàng tối thiểu ngoài Start là bốn, chia 3/1. Starting vẫn 30/3/6 theo BFS, không theo nhãn vùng. V4 đặt starting sheep trước để bảo vệ miền ứng viên nhỏ nhất giữa bốn tuyến Reserved; các preset cũ giữ thứ tự cũ. Tài nguyên ngoài chừa một vòng tương tác không bị footprint khác chiếm; vòng này được đi qua Reserved, còn footprint tài nguyên không được đè Reserved.

## Inventory consumers

| Consumer | Thay đổi/đối chiếu |
|---|---|
| Core settings / generator / StrategicLayout | Dispatch riêng v4, arrays 5/4/2, gateway theo ownership thay `To-1`, snapshot clone |
| StrategicResources | Budget theo ID/role; validator kiểm từng vùng, tâm, pad, footprint, BFS 3×3 và cửa biên |
| Preview | Màu theo role, nhãn ID/role và số tài nguyên ngoài Start từng vùng |
| Presentation + Daytime | Tint/decor/glyph theo role; minimap duyệt toàn bộ vùng; giữ budget chi tiết hiện có |
| Authoring / root / wave | Asset riêng; giữ Tilemap→Physics→navigation→bind→Ready và hai gateway; không đổi ECS/wave timing |
| Expansion / fog / camera / reports | Expansion đọc native grid và vị trí; không hardcode số vùng; fog vẫn kiểm explored; reports không có index vùng |
| Save capture / restore / preflight | Arrays 5/4/2; role/endpoints/ownership/budget/preset-version được kiểm trước chuyển scene; restore final map, không regenerate |
| Fallback / menu | Final MapData v4 bake thành Resources JSON, so settings và validate trước apply; error hold cùng retry/menu/quit |
| Tooling/tests | Seed runner nhận `four`; EditMode và explicit CLI Player smoke, không tự kích hoạt ở trận thường |

Baseline Unity v1/v3 của seed 0/1/1543 nằm tại [baseline-before](Evidence/FourFrontiers/baseline-before.txt), so với [baseline-after](Evidence/FourFrontiers/baseline-after.txt). Hash SHA256 gồm kích thước/seed/attempt/version, terrain/reserved, đầy đủ topology và mọi field placement theo thứ tự. Không dùng hash .NET làm baseline Unity: floating-point noise có thể khác giữa runtime. Sáu baseline Unity đã khớp.

## Save và fallback

DTO nâng schema từ 1 lên **2** vì bổ sung hai scalar bắt buộc `RequestedSeed`/`UsedFallback` trong MapSaveData và codec kiểm đủ field. Đây là thay đổi hợp đồng file để lưu provenance fallback, không phải nâng schema chỉ vì số vùng. Save schema1 bị từ chối trước chuyển scene; không sửa/xóa file cũ. Content fingerprint thay khi source/config thay; không migration ngầm và không cam kết tải save của content cũ. `Seed` luôn là seed thực tế. Capture lưu final terrain, topology và placements, kể cả tombstone; load dựng lại arrays detached và hydrate qua pipeline SL4 có sẵn. Chỉ phạm vi first-day/idle-work; SL5–SL9 giữ nguyên trạng thái chưa hoàn tất.

Fallback chứa settings và final arrays/placements của seed 1 cùng preset. Không chạy thêm vòng generation. Chỉ dùng sau exhausted attempts với cấu hình hợp lệ và tương thích chính xác ngoài seed; lỗi catalog/authoring không được che bằng fallback. Validate kích thước, enum, bounds, role/endpoints/gateway, quota toàn map/từng vùng, footprint, pad và đường đi trước apply. Nếu không thể dùng, simulation giữ blocked/timeScale0; UI cho thử lại/về menu/thoát. Nội dung thông báo không đưa lỗi kỹ thuật vào luồng người chơi. Provenance có log và save metadata.

## Trạng thái và evidence

| Mốc | Trạng thái kiểm chứng |
|---|---|
| FR0 | DONE: inventory và sáu baseline Unity v1/v3 khớp |
| FR1–FR2 | DONE kỹ thuật: preset v4 với năm vùng, bốn tuyến, hai gateway, budget từng vùng/starting và topology validator; 165/165 hồi quy RTS |
| FR3 | DONE: .NET và Unity đều 1.000/1.000 seed, histogram 782/175/28/10/4/1/0/0; seed âm và int.MinValue có tests; người dùng đồng ý preview seed 1 trong chat |
| FR4 | Đã kiểm Editor và Player ba seed: native khai thác/nộp từ bốn vùng, ví, xây kho landmark/train, reload giữ 150 resource Entity / 25 actor; ảnh fog và role. Wave 76 spawn trên seed 1/1543, edge retry/dawn trên seed 0 |
| FR5 | Đã kiểm SL4 v4 cold-process 0/1/1543 canonical, terrain khác seed và tombstone; 11 ca preflight giữ MainMenu; fallback fresh-scene canonical+provenance1543→1 PASS |
| FR6 | DONE phạm vi implementation/kiểm kỹ thuật: Player fallback/failure→retry, Resume không bypass blocked; 5 fixture fallback hỏng reject; failure→menu Editor reset sạch, panel/notice có ảnh. Các hành động lỗi trong Player được automation gọi cùng handlers; chưa chứng nhận click fault UI riêng bởi người chơi |
| FR7 | DONE phạm vi build/lifecycle/đo đã nêu và nghiệm thu FourFrontiers: Windows không lỗi/cảnh báo, 14/14 lượt Player cuối và 18/18 SampleScene; người dùng trả lời “Đã thử và đồng ý nghiệm thu” với source gameplay đã thử. Không suy thành cân bằng mọi seed hoặc đóng gate khác |

Các lượt thử chưa đạt được giữ nguyên: [initial](Evidence/FourFrontiers/sweep-initial.txt), [ring](Evidence/FourFrontiers/sweep-ring.txt), [outer-ring](Evidence/FourFrontiers/sweep-outer-ring.txt). Lượt cuối [scarcity-first](Evidence/FourFrontiers/sweep-scarcity-first.txt) không tăng MaxAttempts hoặc hạ quota. Thời gian .NET chỉ Core, không phải scene→Ready hay FPS. Không xóa seed lỗi khỏi thống kê.

## Cách chạy và giới hạn

Preview: `RTS/Procedural Generation/Preview Regions`, chọn `FourFrontiers`, nhập seed và sinh. Gameplay: `MainMenu → Chơi → ProceduralGame` sau khi asset v4 đã được áp. Preset v3 được giữ để so sánh; SampleScene không chuyển preset.

Quét độc lập: `dotnet run --project Tools/ProceduralGenerationSeedCheck -- 1000 four`. Helper `Tools/ValidateFourFrontiers.cs` chạy bằng script-execute trong Unity: `Baseline`, `Sweep`, `BakeAndActivate`, `FallbackChecks`. Player CLI có `--four-frontiers-smoke gameplay|fallback|failure <output-folder> <seed>` và smoke SL4 cũ để capture/load qua hai process riêng. Các smoke là fixture kỹ thuật, không chứng minh cân bằng hoặc trải nghiệm thao tác tự nhiên.

Đo phải tách Core, terrain, spawn resources, physics/nav, root initialize/bind và LoadScene→Ready. Ghi build/máy/config/seed/mẫu, median/p95/max; không suy thời gian Core thành FPS hoặc budget sản phẩm. Chưa có kết luận bốn hướng cân bằng logistics hoặc áp lực wave.

Nguồn: [kế hoạch gốc](FourFrontiersBuildPlan.md), [Architecture](Architecture.md), [DOTS guide](../DOTS/DevelopmentGuide.md), [SL4](../SaveLoad/SL4-Report.md), source và raw evidence nêu trên. Không thêm thuật toán hoặc dependency bên ngoài mới.


## Bằng chứng kỹ thuật bổ sung

- [165 EditMode](Evidence/FourFrontiers/editmode-final.json), [Unity sweep1000](Evidence/FourFrontiers/unity-sweep-summary.json), [raw .NET](Evidence/FourFrontiers/dotnet-FourFrontiers.csv), [metrics vùng/BFS](Evidence/FourFrontiers/region-metrics-FourFrontiers.json).
- [Editor bốn vùng/khai thác/xây/train seed0](Evidence/FourFrontiers/editor-economy-seed0.json); Player từng seed có `Player-<seed>-gameplay-first.json` và reload report.
- [SL4 preflight11ca](Evidence/FourFrontiers/preflight.json), [fallback restore canonical](Evidence/FourFrontiers/fallback-save-restore.json).
- [Wave seed1](Evidence/FourFrontiers/Waves/live-schedule.json), [wave1543](Evidence/FourFrontiers/WavesSeed1543/live-schedule.json), [edge seed0](Evidence/FourFrontiers/WavesSeed0/edge-cases.txt). Seed1 có2quái sinh trước fixture, nên delta76/total78 được ghi riêng, không gọi total78 là quota76; seed1543 bắt đầu fixture trước wave.
- Ảnh `editor-region-1..4-applied.png` là fixture reveal+ApplyFogColors, không phải khám phá tự nhiên. Ảnh chưa ApplyFogColors giữ riêng và không dùng chứng nhận presentation. `editor-fog-before.json` có cả4false; after reveal cả4true. Fog unknown không được bật toàn map để làm ảnh nghiệm thu.
- Lượt Player đầu thất bại vùng vàng2 được giữ tại `BeforeSmokeFix`. Nguyên nhân helper tickECS thủ công thiếu flush nativegrid ngay sau claim thay collider. Refresh đúng pipeline trước tick thì native nộp20vàng; chỉ sửa helper, không đổi luật khai thác. Menu notice nhận generator tường minh từ root vì authoring nằm trên GameObject khác GameManager.
- [Thông báo fallback sau sửa](Evidence/FourFrontiers/fallback-notice-final.png) ở dưới giữa, tránh HUD góc trái; [panel lỗi](Evidence/FourFrontiers/failure-panel-editor.png). Panel lỗi có thể còn hiển thị các view authoring chưa bind phía sau; simulation vẫn blocked/timeScale0, không có trận nửa sẵn sàng đang tick.

## Nghiệm thu người chơi FR7

Người dùng đã trả lời **“Đã thử và đồng ý nghiệm thu”** cho bản gameplay `164713` sau đề nghị kiểm MainMenu → Chơi, khám phá bốn hướng, khai thác/nộp và xây kho. Bản đóng gói `165350` giữ cùng source gameplay, chỉ trả `PlayerSettings.runInBackground` về giá trị gốc 0 sau khi fixture Editor đã lưu 1. Bản này được chạy lại ma trận tự động; không gán cho người dùng một lượt chơi binary mà họ chưa xác nhận. [Xác nhận và phạm vi](Evidence/FourFrontiers/acceptance.json). FR3 đã được đồng ý bố cục trước đó. Không diễn giải xác nhận này thành thử hết mọi seed/máy, click fault UX hoặc cân bằng logistics mọi vùng. G3manual của đợt cân bằng cũ và SaveLoadSL5–SL9 không được đóng từ đợt này.


## Bản bàn giao và số đo cuối

Build Windows64 Release Mono: `.utmp/four-frontiers-20261124-165350/RTSgame2026.exe`, Unity6000.3.11f1/URP hiện có,0errors/0warnings, build 25.229 s. CPUi5-11300H/RAM16179MiB/GTX1650,Ultra/vSync1/1280×720. Player chạy batchmode, cửa sổ hidden; một số lượt có Editor cùng tồn tại, không là benchmark foregroundFPS. [Build](Evidence/FourFrontiers/Build.json), [config/hash và từng sample](Evidence/FourFrontiers/player-measurements.json).

| Pha (ms, median / p95 / max, mỗi nhóm n=3) | v3 lượt đầu | v4 lượt đầu | v3 reload | v4 reload |
|---|---:|---:|---:|---:|
| Core | 87.52 / 98.56 / 98.56 | 167.84 / 178.15 / 178.15 | 73.83 / 77.22 / 77.22 | 142.04 / 142.39 / 142.39 |
| Tilemap terrain | 79.38 / 97.69 / 97.69 | 88.35 / 107.68 / 107.68 | 66.48 / 70.62 / 70.62 | 62.83 / 64.05 / 64.05 |
| Resource spawn | 12.57 / 13.23 / 13.23 | 14.63 / 19.32 / 19.32 | 6.37 / 7.22 / 7.22 | 6.15 / 6.47 / 6.47 |
| Physics/nav | 14.50 / 16.58 / 16.58 | 18.81 / 23.49 / 23.49 | 11.62 / 14.60 / 14.60 | 11.81 / 12.62 / 12.62 |
| Root initialize | 292.81 / 341.87 / 341.87 | 296.75 / 355.99 / 355.99 | 153.85 / 154.21 / 154.21 | 137.37 / 143.12 / 143.12 |
| Bind | 54.78 / 78.67 / 78.67 | 56.30 / 94.44 / 94.44 | 11.31 / 13.01 / 13.01 | 10.23 / 10.51 / 10.51 |
| LoadScene→Ready | 663.15 / 800.25 / 800.25 | 716.94 / 930.23 / 930.23 | 362.17 / 368.16 / 368.16 | 405.89 / 420.78 / 420.78 |

P95 dùng nearest-rank, với n3 bằng max; chưa đủ để đặt budget phổ quát. V4 lượt đầu median 716.94 ms so v3 663.15 ms, tăng khoảng 53.80 ms trong batch đóng gói cuối; reload 405.89 ms so 362.17 ms. Kết quả batch trước khôi phục cấu hình chạy nền nằm ở `BeforeBackgroundSettingRestore`, không trộn các sample giữa hai build. Chấp nhận bố cục không phải bằng chứng v4 nhanh hơn v3. Chưa đo allocation/memory/FPS mới.

Sweep Unity1000 seed chia3batch do giới hạn snippet: median103.97ms/p95265.32ms/max621.79ms; .NET cuối median20.148ms/p9554.705ms/max513ms. Khác runtime và instrumentation nên không so trực tiếp như tối ưu hiệu năng. Retry histogram0..7=782/175/28/10/4/1/0/0; không seed nào bị loại khỏi tập. [Metrics1000seed](Evidence/FourFrontiers/region-metrics-summary.json): bốn tâm ngoài BFS52–54ô (median53), grass mỗi vùng ngoài thấp nhất1547ô; density riêng từng role được ghi, không suy tương đương kinh tế.

Ma trận cuối: baselinev3 và gameplayv4 cùng seed0/1/1543 (6process), SL4capture/load mỗi seed qua process khác nhau (6process), fallback và failure→retry (2process): **14/14PASS**. Gameplay mỗi seed nộp20gỗ từ mỗi Economy và20vàng từ mỗi GoldFrontier, ví100/100/100→140/140/100; xây kho100W100G và train10M→40/40/90; không teleport worker hoặc rút ngắn duration. TickECS được helper tiến thủ công trong scopefixture, không phải trận thực theo đồng hồ/wave. Reload trở lại150resourceEntity/25actor, Worldcount6không tăng.

SL4cold-process giữ topology5/4/2, terrain[0] cố ý khác generator,1tombstone và149resource cùng ví137/89/123 sau restore; canonical bỏ đúng SaveId/CapturedUtc/Generation rồi so mọi field còn lại. Activework/cargo/FIFO/research/projectile hoặc ngoàiDay1 vẫn bị từ chối. Fallback fresh-scene thêm kiểm allocator/remap/rendererpause và provenance; không gọi đó là cold-processfallback.

[SampleScene smoke18/18](Evidence/FourFrontiers/legacy-player-smoke.json) PASS build cuối; scene/prefab legacy không chuyển preset. [Editor cuối](Evidence/FourFrontiers/editor-final-state.json): ProceduralGame sạch, ngoàiPlay/pause/compile,settingsseed1, mọi seed/preset/failure override null/false. Automatic TMP font atlas do fixture tạo đã trả về bytes ban đầu; không commit font cache ngẫu nhiên. Bản trước sửa notice giữ trong `BeforeNoticeLayoutFix`.


Unity lưu scene đã serialize các giá trị presentation mặc định có sẵn và chuẩn hóa vài giá trị Tilemap animation `10.000001 ↔ 10`; thay đổi cấu hình gameplay chủ đích của scene là tham chiếu asset FourFrontiers riêng. Project Settings, package và font cache cuối giữ giá trị gốc.
