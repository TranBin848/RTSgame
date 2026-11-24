# E4 — ban ngày và bản sắc vùng

**24/11 — E5 đã triển khai:** [depth/occlusion report](E5-DepthOcclusion.md). E5 thêm depth cho Old Watchpost và bodies mà không đổi placement/ground E4; S7/E4 kiểm lại và hash giữ nguyên. Checkpoint “tiếp E5” ngày23/11 bên dưới là lịch sử.

Ngày **23/11/2026 — DONE phạm vi implementation và kiểm Editor trên seed0/1/1543**. [E3 đã được xác nhận nghe chuông](Evidence/E3/AudioAcceptance.md). Tiếp E5 theo [build plan](BuildPlan.md); G3/G5 vẫn mở, chưa đo Player E4.

## Mục tiêu và phạm vi

Economy có đồng cỏ, hoa và cụm bụi; GoldFrontier có mảng đất khô, sỏi/đá; Start giữ ít chi tiết để đọc căn cứ và lối ra. Mặt nước có vài gợn nhẹ, các điểm hoa tại Economy có bướm. Giữ cây/mỏ/cừu gameplay, tuyến strategic, quota150placement, generator v3 và clock60/30/45. Không thêm tài nguyên có thể khai thác, Unit, Entity, collider, công nghệ, âm thanh hoặc đường AI.

E4 mở rộng `ProceduralMapPresentation` hiện có bằng partial file `ProceduralMapPresentation.Daytime.cs`; không thêm manager. Root cấp `DayNightCycleManager` qua `InitializeDaytimeClock` trước Initialize map/fog. Clock chỉ được đọc để ẩn bướm lúc Night; generation không điều khiển clock. Consumer không tìm manager/service toàn cục.

Sửa sprite bụi S7: `Bushe1.png` là dải8frame1024×128, nay chỉ lấy một frame128×128. Tránh vẽ cả hàng tám bụi như một billboard. Các sprite cũ, landmark/minimap và prefab gameplay vẫn được giữ.

## Dữ liệu và cấu hình

| Nguồn | Sử dụng |
|---|---|
| `MapData.Seed`, `Layout.Regions/RegionIds` | Lựa chọn chi tiết theo vùng Start/Economy/GoldFrontier của strategic v3 |
| `Terrain`, `Reserved`, `Routes`, `Resources` | Chỉ đọc để chặn ô nước/bờ, pad, tuyến đi và vùng tương tác tài nguyên |
| `m_PresentationSeed=314159` | Seed riêng trên component; trộn với seed map và phase `0x45344445` bằng `DeterministicRandom` |
| `m_EnableDaytimeDetail=true` | Cấu hình authoring trước Play; tắt để so sánh nền S7, không có hệ thống regenerate khi toggle lúc đang chơi |
| `DaytimeDetailBudget=220` | Trần số renderer mới, bao gồm ambient; tối đa45decor S7 riêng |
| `AmbientBudget=12` | Tối đa4điểm bướm; slot còn lại cho gợn nước |
| Fog và clock được cấp từ root | Quyết định hình ảnh/animation, không thay simulation |

Không sửa `MapData` hoặc TileBase. Biến thể nền gồm màu theo mảng nội suy cộng các decal pixel cỏ/hoa/đất/sỏi. `PatchNoise` nội suy SmoothStep giữa bốn mẫu hash trên lưới5cell; Start đổi nhẹ tối đa8%, vùng ngoài tối đa24%. Không đổi tint tại Reserved/Routes nên tuyến và pad hiện có vẫn đọc được.

Art tái sử dụng: bush/rock/old tower và `StandardSprite` material của repository. Thêm một atlas RGBA32 **64×64**, filterPoint, clamp, không mip, tạo một lần bằng code: cỏ, hoa vàng/hồng, đất khô, sỏi, gợn nước và hai frame bướm. Atlas có7sprite dùng chung, không tạo texture/material mỗi frame. Sprite asset Tiny Swords gốc giữ license của bộ art; motif pixel mới được tạo bằng code, không tải/import tài sản ngoài.

## Luồng và thuật toán

1. Initialize kiểm map, assets, scene/fog và clock đã được cấp. Lặp cùng context giữ một listener, không nhân decor; clock khác hoặc thiếu clock bị từ chối.
2. PaintGround giữ tint vùng/path/pad S7 rồi thêm biến thể mảng lên các ô hợp lệ. Cả map hash, TileBase, navigation data và gameplay RNG giữ nguyên.
3. Start thử tối đa5cụm, Economy/GoldFrontier mỗi vùng tối đa16cụm. Tâm cụm cách nhau ít nhất5cell; mỗi cụm chọn3–6chi tiết gần tâm với số lượt thử hữu hạn. Chọn theo vùng, không rải lên mọi ô. Điểm mới không trùng decor cũ/điểm mới; bướm được dùng cùng ô hoa đã có.
4. Chi tiết đất phải là grass và có lân cận3×3 không Reserved/Routes/nước. Tâm cách resource ít nhất3cell theo quy tắc clearance S7. Mảng đất/hoa chỉ rộng khoảng một cell, có khoảng đệm quanh pad, tuyến và tài nguyên. Không thêm collision nên không sửa ô build hoặc pathfinding.
5. Bướm chọn các điểm hoa Economy cách nhau ít nhất6cell. Gợn chọn ô water có cả3×3 là water, không Reserved/Routes, cách các điểm ambient khác ít nhất5cell và gần bờ cách3cell; không đè foam mép bờ.
6. Chi tiết mới chỉ hiện khi toàn bộ footprint3×3 đã explored. Static decor nhớ trong fog với alpha0.5; chưa biết một phần footprint thì ẩn. Ambient chỉ hiện khi toàn bộ footprint đang Visible, ẩn trong explored-only fog; debug fog-off có thể hiện để kiểm hình ảnh. Trước fog initialized luôn ẩn.
7. LateUpdate chạy tối đa12ambient: bướm dao động nhỏ quanh Home và đổi hai frame, gợn đổi scale/alpha. Dùng `Time.time`, không RNG hay timer pha thứ hai; cùng sample time cho cùng pose. Night ẩn bướm, gợn vẫn phụ thuộc visibility. Không cấp thêm tầm nhìn hoặc discovery reward.
8. Sorting mới: ground details11, bụi/đá12, bướm13, gợn6; trên grassTilemap10/waterTilemap5 và dưới quân/tài nguyên25. Fixture xác minh quan hệ này sau khi ảnh đầu phát hiện các chi tiết nhỏ bị terrain che.
9. Teardown bỏ listener và Destroy các view, sprite runtime, atlas do controller sở hữu; giữ texture/material asset dùng chung. Cleanup hoạt động cả khi chỉ gỡ component, và khi scene unload.

## Cách chạy và kiểm lại

- Mở ProceduralGame, Play với seed1 gốc. Start thoáng; khám phá Economy để thấy hoa/bụi/bướm, GoldFrontier để thấy đất khô/đá. Chưa khám phá site thì không hiện chi tiết/glyph của nó. Đi dọc bờ nước đã thấy có một số gợn nhẹ.
- Pause bằng menu giữ animation theo Time.time. Night không có bướm; static decor theo explored memory, gợn chỉ trong vision. Đây là presentation, không thể click khai thác hoặc chặn quân.
- Trong EditMode có thể đổi `PresentationSeed` trước khi Play; cùng map seed và presentation seed sẽ tái tạo cùng decor. Giá trị mặc định314159 và enabletrue nằm trên component, scene generator mới cũng dùng các default này.
- `Tools/ValidateGameExperienceE4.cs`, class `GameExperienceE4Validation.Run`: chạy qua Unity MCP script-execute trong **trận mới Ready và Editor-paused**. Fixtures clone presentation dùng cùng map để so seed/config và restore màu/fog; không dùng kết quả này như manual playthrough.
- Đổi test seed bằng SetSeed0/SetSeed1/SetSeed1543 ngoài Play, vào Play/step/Run. Test tạm thay asset seed; **RestoreSeed ngoài Play** khôi phục seed gốc1. Không nâng generator version hoặc thay quota/settings khác.
- Trên từng seed, chạy `Tools/ValidateS6Economy.cs` Run và RunConstruction sau integration để kiểm dân đi tới hai vùng khai thác/nộp và xây kho tại landmark. Fixture tick ECS bằng delta0.05, không là thời gian chạy thật/benchmark.
- `Tools/CaptureGameExperienceE4.cs` Run cần compile cùng helper `ValidateGameExperienceE4.cs`: gom các using lên đầu một lần khi ghép hai class cho script-execute. Capture baseline/candidate khi Editor-paused, giữ map/camera/clock; baseline chỉ tắt E4, **vẫn giữ sửa crop bụi**.
- Lifecycle: CreateDestroyProbe → step → CheckDestroyProbe; ReturnToMenu → step → MenuAndReload → step → CheckNewMatch. CheckLegacy ở SampleScene. Các object probe do fixture tạo được dọn sau kiểm.
- Thoát Play, restore seed1 rồi chạy `tests-run` EditMode với scene đã lưu.

## Kết quả đã kiểm

[Regression81/81 PASS](Evidence/E4/editmode-result.json),0fail/skip,6.4467015giây. Unity compile và fixtures đều chạy trên scene thật. Không nâng Editor/package/pipeline hoặc sửa DOTS runtime/Core.

| Seed | Tổng decor, gồm S7 | E4 mới | Ambient trong E4 | Start/Economy/GoldFrontier mới trên đất | Kết quả |
|---|---:|---:|---:|---|---|
|0|194|179|12|25/73/73|[Integration/hash/fog](Evidence/E4/seed-0-integration.txt), [gameplay](Evidence/E4/seed-0-gameplay.txt) PASS|
|1|184|168|12|17/70/73|[Integration/hash/fog](Evidence/E4/seed-1-integration.txt), [gameplay](Evidence/E4/seed-1-gameplay.txt) PASS|
|1543|190|176|12|24/84/60|[Integration/hash/fog](Evidence/E4/seed-1543-integration.txt), [gameplay](Evidence/E4/seed-1543-gameplay.txt) PASS|

Region counts bao gồm bướm trên ô hoa, không bao gồm ripple; ambient là tập con E4, không cộng lại vào E4 count.

Trên cả3seed: fresh Core generator cho cùng **SHA256 đầy đủ** với MapData sau presentation. Hash bao gồm dimensions/seed/attempt/start/version, Terrain/Reserved/Resources, RegionIds/Routes, Regions/Connections/Gateways/Landmark. Cùng presentation seed tái tạo cùng visual/ground hash; đổi seed presentation hoặc disable làm đổi hình ảnh, không đổi map hash. RNG UnityEngine, navigation version, ví, actor/tick giữ nguyên trong fixture presentation.

Kiểm thêm: footprint và resource clearance, sorting trên terrain/dưới units, một fog listener/repeat-init, thiếu clock bị từ chối, bush crop, unknown site/partial footprint không lộ, static explored memory, ambient visible-only, fog chưa initialize, animation thay đổi theo sample time và đứng khi sample time giữ nguyên, Night tắt bướm.

Khai thác/xây dựng native sau E4:

| Seed | Economy: cargo đầu / nộp20wood | GoldFrontier: cargo đầu / nộp20gold | Kho tại landmark hoàn tất |
|---|---|---|---|
|0|18.45s /54.25s|19.55s /58.45s|30.00s|
|1|17.15s /52.20s|22.05s /59.75s|27.00s|
|1543|22.50s /60.80s|19.10s /55.55s|32.50s|

Các số là **simulation time trong fixture Editor tăng tốc**, không là performance/FPS. Sau deposit ví120/120/100; xây kho debit100wood/100gold, actor25→26 và construction finished/sprite hoàn tất đã kiểm. Chứng minh các tuyến/lệnh được thử vẫn hoạt động, không chứng nhận mọi tuyến của mọi seed.

[Lifecycle](Evidence/E4/lifecycle.txt): component-only destroy dọn runtime atlas/sprites/views và bỏ listener, shared assets còn; menu dọn mọi ID được ghi; trận seed1 mới Ready/Day/unknown sites reset, một presentation/một listener và visual hash giống trận trước. SampleScene không có E4/palette procedural.

## Ảnh bằng chứng

[Ngữ cảnh capture](Evidence/E4/capture-context.txt):1920×1080, seed1, Day và gameplay đóng băng. Các ảnh so hình dùng **debug fog-off**, không là bằng chứng người chơi đã đi khám phá. Không có HUD trong ảnh camera. So sánh giữ sửa crop bụi ở cả hai phía.

| Vùng | Baseline S7 với crop đúng | Candidate E4 |
|---|---|---|
|Start, ortho10|[Trước](Evidence/E4/baseline-Start-Day-fog-off.png)|[Sau](Evidence/E4/candidate-Start-Day-fog-off.png)|
|Economy, ortho14|[Trước](Evidence/E4/baseline-Economy-Day-fog-off.png)|[Sau](Evidence/E4/candidate-Economy-Day-fog-off.png)|
|GoldFrontier, ortho14|[Trước](Evidence/E4/baseline-GoldFrontier-Day-fog-off.png)|[Sau](Evidence/E4/candidate-GoldFrontier-Day-fog-off.png)|

Ảnh gần bướm [sample0](Evidence/E4/candidate-butterfly-sample-A-fog-off.png)/[sample0.8](Evidence/E4/candidate-butterfly-sample-B-fog-off.png) và gợn nước [sample0](Evidence/E4/candidate-ripple-sample-A-fog-off.png)/[sample0.8](Evidence/E4/candidate-ripple-sample-B-fog-off.png) dùng cùng camera ortho4, chỉ lấy mẫu animation presentation. [GoldFrontier chưa khám phá, fog bật](Evidence/E4/candidate-GoldFrontier-unexplored-fog-on.png) là đối chứng fog. Ảnh đã kiểm trực tiếp: hoa/bụi và mảng đất/sỏi hiện trên terrain, Start không phủ kín chi tiết, pad/tuyến vẫn đọc được.

## Hiệu năng, giới hạn và hướng tiếp

Chưa build/đo Player E4 hoặc đóng G5. Trần220renderer mới/12ambient là budget implementation, không là thành tích FPS. Atlas64×64 RGBA32 tương đương16KiB dữ liệu pixel trước overhead/driver, dùng chung các motif; không đo VRAM thực tế. Khởi tạo hữu hạn theo số lượt thử/cụm; mỗi fog update kiểm tối đa9cell trên mỗi detail, mỗi frame chỉ animate tối đa12điểm. Ground variation chạy khi khởi tạo, không tô lại toàn map mỗi frame.

Chưa có manual playthrough E4 của người dùng hoặc QA mọi zoom/resolution. Butterfly/ripple chỉ hai frame/pulse nhẹ, không có đàn chim, weather, fauna AI hoặc animation art bổ sung. Cảnh không bị phủ chi tiết lên mọi ô; mật độ và màu motif có thể chỉnh sau nghiệm thu hình ảnh. Fog footprint3×3 cố ý bảo thủ nên một chi tiết có thể hiện muộn hơn ô tâm. Chi tiết là sprite phẳng; bóng/sorting theo chân/occlusion là E5 tiếp theo.

## Nguồn đối chiếu

- [E4 build plan](BuildPlan.md), [S7](../ProceduralGeneration/S7-Presentation.md), [procedural architecture](../ProceduralGeneration/Architecture.md).
- [Manual DI](../DependencyInjection/Architecture.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md), [E1 palette](E1-ArtDirection.md), [E2 lighting](E2-NightLighting.md).
- Source trong Assets/Game: ProceduralMapPresentation và partial Daytime; GameSceneCompositionRoot; DeterministicRandom; MapData/StrategicLayout; FogOfWarManager; DayNightCycleManager. Fixtures và capture trong Tools được ghi ở phần cách chạy.
