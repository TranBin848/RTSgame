# S7 — nhận diện vùng, landmark và khám phá

Ngày **16/11/2026 — DONE trong phạm vi build plan**. Code, Unity checks, ảnh cùng seed/camera ngày–đêm và xác nhận người dùng đã có. [Progress](../Progression/Progress.md) là checkpoint tiếp S8; D4/S9 còn lại, không coi toàn project đã hoàn tất.

## Mục tiêu và phạm vi

Làm strategic v3 đọc được bằng hình ảnh: Economy có sắc xanh/cụm bụi, GoldFrontier có sắc ấm/cụm đá; tuyến tiếp cận có dấu đường mảnh, pad landmark có viền nền và tháp cũ đứng cạnh. Minimap có glyph cây cho Economy, glyph tháp vàng cho GoldFrontier/landmark khi đã khám phá. Không thêm tài nguyên, collider, Unit, gameplay reward hay công nghệ mới.

Màu ban đêm của DayNightCycleDefinition dùng chung được chỉnh để đọc được terrain/quân/tài nguyên; thời lượng60/30/45giây giữ nguyên. Không đổi Editor/render pipeline hoặc thuật toán generator/navigation/combat.

## Dữ liệu và DI

`ProceduralMapPresentation` có serialized refs tới3Texture2D hiện có và material sprite-lit hiện có. Root cấp MapData, TilemapManager, OriginCell và FogOfWarManager qua Initialize; kiểm dependency/cùng scene, repeat cùng context không nhân object/listener. Minimap nhận presentation qua InitializeLandmarks, không tự tìm presentation bằng global lookup.

Root ProceduralGame có hai refs mới bắt buộc `m_MapPresentation`, `m_Minimap`. `ProceduralMapEditorTools.ConfigureMapPresentation` được gọi từ scene generator và đã được dùng qua MCP để wire scene hiện tại; không hand-edit YAML. SampleScene không có root này nên không bắt buộc có presentation. Các dependency cũ của minimap/fog vẫn thuộc D4, không báo toàn UI đã đạt DI.

## Luồng và thuật toán

1. Authoring áp map như S6, expose OriginCell read-only. Root Initialize dựng presentation sau khi đã có MapData/map/fog. Presentation không tự chạy generation trong Awake/Start.
2. Mỗi grass cell được tint theo RegionIds và khoảng cách tới tâm vùng, fade trong8cell ngoài radius. Routes có spine hẹp dựa bốn láng giềng cách2cell; mép đường nhạt hơn. Pad5×5 dùng màu viền/nền. Chỉ đổi Tilemap color/LockColor, không đổi TileBase, Terrain/Reserved/Resources hoặc dữ liệu navigation.
3. Mỗi vùng ngoài Start thử16điểm quanh biên; landmark thử12đá quanh ring4. Điểm phải là grass, không Reserved/Routes và cách resource ít nhất3cell. Watchpost tìm vị trí hợp lệ theo16góc tại radius4→10 cạnh pad. Toàn bộ lựa chọn hữu hạn, xác định từ layout, không tiêu thụ UnityEngine.Random. Budget tối đa45decor, số thực tế ít hơn do clearance.
4. Mỗi decor chỉ có Transform/SpriteRenderer, sortingOrder10 thấp hơn quân/cây/mỏ25. Runtime Sprite được tạo từ texture có sẵn, material dùng chung; không collider/Unit/Animator, không chiếm ô build pad. Sprite runtime được Destroy khi owner teardown.
5. Subscribe một OnFogUpdated. Trước fog initialized hoặc chưa explored thì renderer tắt. Visible alpha1, explored ngoài vision alpha0,5. Site icon chỉ vẽ khi tâm region explored; nhớ lại sau khi worker rời tầm nhìn. Layout hiện tại đặt landmark tại tâm GoldFrontier nên dùng chung glyph tháp vàng, không thêm site giả.
6. Minimap vẽ glyph5×5 sau terrain, trước dấu enemy để không giấu địch đang nhìn thấy. Không tạo UI raycast target mới. Fog debug toggle nay phát OnFogUpdated ngay để world decor/minimap đồng bộ, thay vì chờ timer refresh.

Ground tint là presentation: không ảnh hưởng CamWalkAtTile, placement validation hoặc native grids. World tháp là dấu landmark trung lập, không phải công trình huấn luyện/chiến đấu có thể chọn.

## Tài sản và cấu hình hình ảnh

Tái sử dụng trong repository, không tải/import/generate art bên ngoài:

- `Assets/Game/Images/Tiny Swords/Terrain/Decorations/Bushes/Bushe1.png`.
- `.../Terrain/Decorations/Rocks/Rock1.png`.
- `.../Buildings/Black Buildings/Tower.png` và shared material của prefab Tree.

Các tài sản thuộc bộ Tiny Swords đã có trong project; slice này không cấp license mới, không khẳng định CC0 hay chuyển quyền tài sản. Giữ điều kiện sử dụng gốc của bộ art; không có tài sản bên ngoài mới cần nguồn/license riêng.

Night light color từ(0,2;0,2;0,4)→(0,6;0,68;0,86), intensity0,1→0,35; vignette0,5→0,28; color filter(0,6;0,7;1)→(0,8;0,86;1). Day/dusk và timing không đổi. Asset này dùng chung nên SampleScene cũng nhận palette đêm mới; chưa làm playtest hình ảnh toàn trận SampleScene.

## Cách chạy và kiểm tra lại

Mở ProceduralGame, Play/để Start chạy rồi pause. MCP script-execute nội dung [ValidateS7.cs](../../../Tools/ValidateS7.cs), class S7Validation:

- CheckPresentation trên fresh scene: Ready,25actors/150resource placements, budget decor/1watchpost, pad trống, không Collider/Unit, repeat init/nav/core terrain ổn định,1listener, chưa explored không lộ site, dependency thiếu bị từ chối.
- CheckDiscovery: test-only reveal quanh landmark qua fog API, kiểm world/glyph hiện; RefreshFog bỏ visibility tại vùng đó nhưng explored giữ glyph và silhouette alpha0,5. Không suy ra phần thưởng khám phá hoặc fog mới.
- SetupCapture khóa camera tại LandmarkPosition, ortho9 và bật debug fog-off chỉ để so ảnh. BaselineVisuals tạm tắt tint/decor/glyph và unsubscribe callback test; RestoreVisuals trả màu/decor/glyph/listener. NightBefore/NightAfter dùng giá trị cũ/mới **trong bộ nhớ**, không SaveAssets; luôn gọi NightAfter để khôi phục giá trị authored trước Exit Play.
- Dùng Editor Step rồi screenshot-game-view. Không thay fog-off của ảnh test vào mặc định game. Exit Play bỏ toàn bộ probes; preset Seed phải về1.

Cả scene generator helper và S7 code đã compile; helper wire hiện tại chạy qua MCP. Chưa chạy lại toàn menu Create/Refresh gồm các UI khác vì không cần rebuild scene chỉ để đổi refs.

## Kết quả đã kiểm

| Kiểm | Kết quả thực tế |
|---|---|
| Seed1/0/1543 presentation |16/15/14decor, mỗi seed1watchpost; pad clear, no Collider/Unit;25actors/150placements, repeat init/nav/terrain không đổi,1listener, missing-dependency reject |
| Discovery cả3seed |Unknown sites/decor hidden → reveal hiện silhouette/glyph → leave vision giữ marker và silhouette mờ, PASS |
| Seed1 input |Selection/ground-click gửi ECS command; minimap pointer/drag focus đúng tọa độ và giữ z, PASS qua S6AcceptanceValidation.CheckInput |
| Seed1 economy |Nộp20gỗ sau50,50s và20vàng sau61,20s mô phỏng; wallet120/120/100, PASS sau decor |
| Seed1 build landmark |StoragePit hoàn tất27,00s mô phỏng, debit100wood/100gold, actor25→26, PASS; tháp decor không chặn placement/path |
| Regression |59/59DOTS,0fail/skip,2,1626465s;17/17procedural,0fail/skip,5,3338967s sau code cuối |
| Visual acceptance |Đã xem ảnh trước/sau; người dùng trả lời **“Ổn, dễ nhận ra”** cho landmark/hướng mở rộng/độ sáng đêm ngày16/11 |

Các giây economy/build là thời gian mô phỏng, Test Runner duration không phải FPS. Không benchmark startup/alloc/frame mới. Quan sát đồ họa chỉ kết luận trong góc/seed và ảnh được ghi.

## Ảnh và điều kiện so sánh

Ảnh final1920×1080, cùng seed1, camera tại landmark, orthographicSize9 và camera lock. Debug fog disabled để hình ảnh cùng vùng không bị che; discovery logic kiểm riêng với fog bật. Cùng map/assets/gameplay state paused; trước/sau có Editor Step để render nên HUD/animation có thể khác frame. Baseline có kiểm soát tắt S7 presentation và dùng palette đêm cũ trong cùng scene, không phải build lịch sử khác. Các ảnh before-seed1 ban đầu/draft khác trong Evidence là quá trình thử, không dùng thay cặp final.

| | Trước | Sau |
|---|---|---|
| Ngày |[Ảnh](Evidence/S7-final-before-seed1-day.png)|[Ảnh](Evidence/S7-final-after-seed1-day.png)|
| Đêm |[Ảnh](Evidence/S7-final-before-seed1-night.png)|[Ảnh](Evidence/S7-final-after-seed1-night.png)|

Sau: thấy spine tuyến đi, viền pad và tháp cũ trung lập; màu đêm còn giữ sắc xanh nhưng thấy nền/đá/cây rõ hơn. UI research hiện có vẫn phủ phần trên màn hình, không được refactor trong S7.

## Hiệu năng, giới hạn và hướng tiếp

Chỉ một lần paint theo số cell khi Initialize; fog update duyệt decor hữu hạn; glyph2site×25pixel/refresh. Đây là mô tả độ phức tạp, không phải kết quả đo tốc độ. Chưa Player build/FPS/allocation hoặc ma trận additive scenes. Chưa kiểm mọi seed; search watchpost hữu hạn có thể không tìm được chỗ ở layout ngoài3seed nếu clearance kín, cần mở kiểm chứng khi mở thêm preset.

Không thêm discovery reward/save-state; icon được nhớ bằng explored state trong trận như fog hiện tại, reset khi game mới. Gameplay economy/combat và D4 migration còn theo module hiện có. Tiếp **S8**: hoàn chỉnh kết thúc trận/playtest kinh tế-quân sự/cân bằng, gồm quota48 vs42mốc trong đêm từ S6; không thêm tree để che vấn đề vòng chơi.

Môi trường cuối: ProceduralGame sạch, seed1, ngoài Play/pause/compile; NightLightIntensity0,35/NightDuration45; Exception sequence>4134=0. Không commit, không package/Editor/pipeline upgrade. Không tuyên bố sửa native crash cũ.

## Nguồn nội bộ/file thay đổi

[BuildPlan S7](../Progression/BuildPlan.md), [S6 evidence](S6-FinalValidation.md), [DI Architecture](../DependencyInjection/Architecture.md), Core StrategicLayout/MapData hiện có. Implementation: Integration/ProceduralMapPresentation.cs và meta mới; Authoring.OriginCell; GameSceneCompositionRoot refs/Initialize; MinimapFogUI.InitializeLandmarks/glyph call; FogOfWarManager toggle notification; ProceduralMapEditorTools.ConfigureMapPresentation; ProceduralGame serialized refs và DayNightCycleDefinition palette. Không dùng nguồn web hoặc thêm dependency cho tính năng này.
