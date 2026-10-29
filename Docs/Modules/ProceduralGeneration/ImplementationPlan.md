# Kế hoạch triển khai Procedural Generation

Ngày lập: **27/10/2026**. Cập nhật **29/10/2026**. Trạng thái: **đã triển khai lát cắt đầu tiên; còn kiểm chứng gameplay trong Unity**.

Căn cứ: [nghiên cứu thuật toán](Research.md), [Technical Overview](../../TechnicalOverview.md), [DOTS Development Guide](../DOTS/DevelopmentGuide.md), [Gameplay Architecture](../DOTSGameplay/Architecture.md) và [Swarm Architecture](../DOTS/Architecture.md). Trạng thái thực tế và phần còn thiếu được ghi trong [Architecture](Architecture.md) và [Validation](Validation.md).

## 1. Kết quả cần bàn giao

Một scene `ProceduralGame` dùng prefab và gameplay hiện có. Người phát triển chọn seed/cấu hình trong Inspector, chạy Play để tạo map cỏ/nước, rừng, cụm vàng và đàn cừu; dân khai thác, xây dựng, huấn luyện và chống wave trên map đó. Có preview trong Editor, báo cáo validation và bộ seed tái lập lỗi.

Phạm vi phiên bản đầu:

- Map hữu hạn, preset `MainlandWithLakes`, ưu tiên PvE chống wave theo gameplay đang có.
- Sinh một lần trước trận; thử seed khác bằng preview hoặc bắt đầu lại scene.
- Dùng Simplex noise nhiều octave, BFS/clearance, sửa kết nối, đặt tài nguyên theo cụm có quota và validator sau placement.
- Dùng đủ catalog sáu prefab vàng hiện có; cùng loại tài nguyên Gold, chưa thay đổi kinh tế thành sáu cấp mỏ.
- Giữ Unity, URP 2D, prefab và nền DOTS hiện tại; `SampleScene` là scene kiểm tra hồi quy.
- Tài nguyên hữu hạn, sinh lại toàn map giữa trận, streaming, save/load và PvP thuộc các đợt sau.

## 2. Quyết định kỹ thuật

| Hạng mục | Quyết định cho phiên bản đầu |
|---|---|
| Dữ liệu gốc | Mảng địa hình, vùng giữ chỗ và placements; không đọc ngược hình Tilemap để quyết định toàn bộ bố trí |
| Seed | RNG riêng cho từng phase; lưu seed, attempt, generator version và cấu hình/catalog hash |
| Địa hình | Simplex nhiều octave, phân loại cỏ/nước, giữ khu xuất phát và sửa đường nối cần thiết |
| Navigation validation | BFS 4 hướng trên mask có xét kích thước actor; kiểm tra vùng tương tác và lối ra công trình |
| Tài nguyên | Đặt quota khởi đầu trước; chọn tâm cụm bằng Poisson disk, đặt từng node bằng footprint và occupancy |
| Cây | Cụm rừng có khe khai thác; mọi cây được tính là tài nguyên phải có vị trí tiếp cận, kể cả khi chưa cây nào được claim |
| Vàng | Chọn prefab trước khi kiểm tra chỗ đặt; trọng số mặc định bằng nhau, lưu variant ID trong placement |
| Cừu | Spawn theo đàn trên bãi mở; sau spawn dùng actor ECS hiện có |
| Render | Tilemap/RuleTile và GameObject presentation hiện có; không tạo Entity cho từng ô |
| Bắt đầu trận | Coordinator sở hữu trạng thái chuẩn bị; chỉ mở simulation, ngày/đêm, wave và command sau khi grid cuối sẵn sàng |
| Thất bại | Retry hữu hạn với seed con xác định; có lỗi và seed tái lập, không đưa map thiếu quota bắt buộc vào trận |

Cấu hình phát triển ban đầu, **chưa phải thông số cân bằng hoặc hiệu năng đã đạt**:

| Tham số | Giá trị thử |
|---|---|
| Kích thước / cell size | 128 × 128 / 1 world unit; grid vuông, không xoay, scale đồng nhất |
| Tỷ lệ nước mục tiêu | 20%; khoảng thử 15–25%, đo lại sau repair |
| Noise | 4 octave, persistence 0.5, lacunarity 2; scale được lưu trong settings |
| Khu xây ban đầu | Footprint TownHall cộng vùng trống 4–6 ô, lối dân ra và vị trí công trình khởi đầu nếu có |
| Quota khởi đầu | 30 cây, 3 node vàng, 6 cừu |
| Tổng budget thử | 120 cây, 12 node vàng, 18 cừu, **đã gồm** quota khởi đầu |
| Terrain attempts | Tối đa 8; placement/repair cũng có budget hữu hạn riêng |
| Khoảng đường đi thử | Cừu 6–12, mép rừng 8–16, vùng tương tác vàng 12–22 bước từ lối ra căn cứ |

Quota được lưu trong settings để chỉnh khi playtest. Cấu hình không đủ diện tích/footprint phải bị từ chối rõ ràng. Không tự giảm tài nguyên khởi đầu để một seed được báo thành công. Khoảng cách cần tính theo đường đi; quy đổi collider/offset sang world units trước khi rasterize về grid.

## 3. Cấu trúc mã dự kiến

```text
Assets/Game/ProceduralGeneration/
  Core/                 RTS.ProceduralGeneration.Core.asmdef
                        settings dạng dữ liệu, MapData, RNG, generator, validator
  Integration/          settings/catalog ScriptableObject, coordinator, Tilemap adapter
  Editor/               công cụ tạo scene, preview, gizmos và quét seed
  Tests/Editor/         tests core với assembly riêng
  Settings/             preset map và catalog prefab/tile
Assets/Scenes/ProceduralGame.unity
Docs/Modules/ProceduralGeneration/
  Research.md
  ImplementationPlan.md
  Architecture.md       bổ sung cùng implementation
  Usage.md              bổ sung khi có cách chạy thực tế
  Validation.md         ghi rõ chưa chạy / đã chạy và bằng chứng
  Evidence/             XML, CSV/JSON, seed lỗi, ảnh và điều kiện benchmark
```

`Core` nhận struct/mảng thuần dữ liệu, có thể tham chiếu Mathematics nhưng không tham chiếu `Unit`, `GameManager`, prefab hoặc scene. `Integration` nằm ngoài asmdef Core, trong Assembly-CSharp để nối với gameplay hiện tại. `RTS.DOTS` không tham chiếu ngược module Integration. Editor code cần asmdef phù hợp hoặc ở assembly Editor mặc định để truy cập adapter; không tạo phụ thuộc vòng vào Assembly-CSharp.

Các dữ liệu cần có: `MapGenerationSettingsData`, `MapData`, `ResourcePlacement`, `StartingPlacement`, `ResourceVariantData`, `MapValidationReport`. Catalog authoring ánh xạ stable variant ID sang prefab, chép collider footprint/offset và interaction radius sang dữ liệu kiểm tra. Các thuật toán dùng một quy ước tọa độ/origin duy nhất.

## 4. Sáu mốc triển khai

### Mốc 1 — Nền dữ liệu, catalog và hợp đồng khởi động

**Công việc**

- Tạo cấu trúc module, Core assembly, settings, catalog và RNG theo seed/phase; validation đầu vào, quy tắc hash dữ liệu và log lỗi.
- Đọc tile/RuleTile, sáu prefab vàng, cây/cừu, collider TownHall/dân/quái; xác định footprint, click trigger, sprite anchor và scale. Kiểm tra đúng hình bờ nước trong Editor.
- Xác định composition khởi đầu từ scene/prefab hiện có: TownHall, dân, công trình/quân nếu có và tài nguyên ví ban đầu. Chuyển thành cấu hình rõ ràng của scene mới, tránh spawn cả đối tượng mẫu lẫn đối tượng sinh.
- Thiết kế trạng thái `Idle → Generating → Applying → Binding → Ready`, nhánh `Failed`; quy định ai sở hữu generated roots và ai chờ readiness.
- Tạo `Architecture.md` với trạng thái implementation thực tế.

**Hoàn thành khi:** cùng dữ liệu đầu vào tạo cùng seed con/hash; seed 0, cấu hình lỗi, variant ID trùng và catalog thiếu được xử lý xác định. Hợp đồng readiness nêu đủ bridge, simulation, spawner, input, ngày/đêm và fog. Scene cũ vẫn compile và giữ đường khởi động tự động.

### Mốc 2 — Sinh địa hình và kiểm tra khả năng di chuyển

**Công việc**

- Tạo noise, ngưỡng nước, vùng xuất phát và mask bảo vệ lối ra/chỗ xây.
- BFS gán component, tạo clearance theo nhóm kích thước actor; loại đảo vô dụng hoặc nối vùng bắt buộc bằng hành lang cỏ đủ rộng.
- Tạo validator và repair có giới hạn. Chạy validator sau mọi sửa đổi ảnh hưởng kết nối; bảo vệ vùng bắt buộc khi làm sạch ô lẻ.
- Bổ sung preview dữ liệu: cỏ/nước, component, vùng giữ chỗ, đường đi và lỗi. Preview chưa tạo thực thể gameplay/ECS.

**Hoàn thành khi:** preview nhiều seed cho map khác nhau nhưng tái lập được; không mất vùng xuất phát; lối chỉ chạm góc hoặc không vừa collider bị phát hiện. Seed không khả thi kết thúc bằng báo cáo trong budget. Có fixtures cho nước chia cắt map và map không đủ chỗ xây.

### Mốc 3 — Phân bố cây, vàng và cừu

**Công việc**

- Tạo occupancy từ địa hình, công trình ban đầu, lối đi và vùng spawn cần bảo vệ.
- Đặt quota khởi đầu trước. Dùng distance field từ vùng đi ra căn cứ/depot để chọn vị trí theo đường vận chuyển.
- Chọn tâm cụm bổ sung bằng Poisson disk; rừng phát triển theo vùng có giới hạn, vàng thành cụm, cừu thành đàn. Lấy vị trí trên grid và kiểm tra lại khoảng cách sau snap.
- Chọn variant vàng rồi xét footprint/offset; tách collider vật lý và click trigger. Dùng catalog để thay đổi trọng số mà không sửa thuật toán.
- Kiểm tra lại đường về depot và vùng đứng tương tác sau khi đặt toàn bộ tài nguyên. Nếu thiếu quota, thử vị trí khác/sửa cục bộ rồi mới đổi terrain attempt.

**Hoàn thành khi:** đạt đúng budget yêu cầu hoặc trả lỗi rõ ràng; không overlap vật lý/nằm trên nước; không node nào được tính vào budget bị cô lập bởi cây/mỏ khác. Có fixtures kiểm tra cả sáu prefab vàng, rừng kín và mỏ sát bờ. Tất cả tài nguyên bắt buộc có đường khai thác hợp lệ ở trạng thái chưa claim.

### Mốc 4 — Tạo scene chạy được và tích hợp DOTS

**Công việc**

- Tạo scene `ProceduralGame` từ cấu hình gameplay hiện tại, thêm coordinator và các generated roots; cấu hình scene mới dùng generator thay cho `ResourceSpawner` cũ.
- Áp dụng batch Tilemap, xác nhận cỏ/nước logic khớp `CamWalkAtTile`. Nền nước trang trí dưới cỏ không được nhầm với nước gameplay. Preview Editor dùng Undo và chỉ sửa vùng do generator sở hữu.
- Refactor bridge để hỗ trợ khởi tạo có điều phối; thêm API đồng bộ navigation có kết quả thành công/version rõ ràng. Không dựa vào việc chờ ngẫu nhiên 0.25 giây.
- Thêm cơ chế giữ simulation trong lúc binding nếu cần đi qua nhiều frame; flag dữ liệu ở runtime do bridge điều khiển, không tạo phụ thuộc runtime vào MonoBehaviour. Scene thủ công và demo vẫn dùng hành vi mặc định.
- Đăng ký tài nguyên, TownHall, depot, dân và cừu đúng một lần qua bridge; chụp lại grid sau khi collider xuất hiện. Kiểm tra `IsDotsControlled` và entity/resource mapping.
- Khởi tạo fog/minimap/camera theo bounds mới, phát readiness và mở trận. Đường lỗi giữ trạng thái Failed, dọn đúng những đối tượng/entity do lần apply đó tạo.

**Thứ tự bắt buộc:**

```text
Giữ simulation/command/ngày đêm/wave
→ tạo và validate MapData hoàn chỉnh
→ ghi terrain, cấu hình grid cơ sở cho cả hai system
→ spawn và bind nhà/tài nguyên/actor
→ đồng bộ Physics2D, rebuild navigation cuối
→ xác nhận actor, depot, TownHall goal và đường tiếp cận
→ khởi tạo fog/minimap/camera
→ Ready, bắt đầu trận
```

Trong scene procedural, kiểm soát cả các nhánh auto-initialize tại `Awake`/`Start`, không chỉ tắt `Update` của wave. Đăng ký lại từ `Unit.Start`/`ResourceNodeBase.Start` phải không tạo Entity trùng. Cần kiểm tra việc cấp tài nguyên ví ban đầu và subscribe event chỉ xảy ra một lần.

**Hoàn thành khi:** seed từ Inspector tạo được trận chơi; dân khai thác đủ ba loại và nộp đúng kho, xây nhà, huấn luyện và điều khiển quân; fog/minimap/camera đúng bounds. Không có tick gameplay trước Ready, Entity trùng hoặc đường đi giữ từ grid trước. `SampleScene` và Swarm demo vẫn chạy bằng đường khởi động cũ.

### Mốc 5 — Tương thích wave và hiệu chỉnh gameplay

**Công việc**

- Kiểm tra vòng spawn quanh TownHall theo cơ chế hiện tại, gồm radius của quái và vị trí đã resolve khi spawn; loại ứng viên không có đường tới vùng tiếp cận căn cứ.
- Cập nhật cache kết nối theo `NavigationVersion`, không BFS toàn map cho mỗi quái. Kiểm tra lại sau xây công trình và thay đổi collider tài nguyên.
- Giữ vòng spawn là lựa chọn đầu tiên. Thiết kế fallback có giới hạn trong vùng hợp lệ khi vòng nằm ngoài đất/map: tìm vị trí có đường tới căn cứ, đủ xa khu công trình theo cấu hình an toàn. Nếu không tồn tại, trả thất bại có chẩn đoán, không spawn trong nước hoặc ngay trong căn cứ. Ghi rõ quy tắc này trong Architecture vì đây là thay đổi hành vi spawn.
- Playtest khoảng đường tới tài nguyên, số chỗ khai thác, diện tích xây, lối tiến quân và nguy cơ kẹt khi claim/release.
- Hiệu chỉnh preset; ghi thời gian chuẩn bị, thời gian quái tới, hàng thực nộp về kho. Tài nguyên xa được đánh giá theo vận chuyển/năng lực khai thác vì hiện chưa có depletion.

**Hoàn thành khi:** wave xuất hiện và tới được căn cứ trên tập seed thử; có bài kiểm tra mở rộng căn cứ làm vòng spawn thay đổi, vòng vượt bounds và không có ứng viên an toàn. Dân/cừu/quái sử dụng collider thực trong kiểm thử Unity; không chỉ dựa vào ảnh preview.

### Mốc 6 — Kiểm thử nhiều seed, đo và bàn giao

**Công việc**

- Quét seed `0..999` cho preset mặc định; lưu seed gốc/attempt, kết quả quota, lỗi, số lần repair, hash map và thời gian từng stage. Thêm cấu hình 64 × 64, 256 × 256 và quota cực trị để kiểm tra giới hạn; chưa hứa hỗ trợ mọi tổ hợp.
- Điều kiện phát hành preset mặc định: toàn bộ 1.000 seed đạt yêu cầu trong retry budget; nếu chưa đạt, sửa/tune rồi chạy lại toàn bộ và lưu cả kết quả lỗi. Đây là mục tiêu kiểm thử, không phải bảo đảm mọi seed có thể tồn tại.
- Chạy tests core, tests DOTS khi sửa simulation, Player smoke và bài tích hợp generator. Chọn seed đại diện và seed từng lỗi để kiểm tra thủ công UI/ảnh/khai thác/wave.
- Đo riêng core generation, ghi Tilemap, instantiate, physics sync, grid capture, tổng thời gian tới Ready, allocation/bộ nhớ và frame time lúc chơi. Báo median/p95, cấu hình máy/build/rendering và số thực thể thực tế.
- So sánh baseline placement ngẫu nhiên và pipeline mới với cùng budget/config; báo cả tỷ lệ sinh lỗi. Chỉ tối ưu Jobs/Burst hoặc batching thêm khi profiling chỉ ra điểm nghẽn.
- Hoàn thiện `Architecture.md`, `Usage.md`, `Validation.md`, Evidence và các liên kết từ Docs/README/TechnicalOverview.

**Hoàn thành khi:** có bằng chứng tái lập cho preset mặc định, quy trình nhập seed/chạy/đọc lỗi, kết quả Unity và báo cáo giới hạn. Ngưỡng thời gian tải/frame cụ thể sẽ được đặt sau baseline trên máy mục tiêu; không ghi số hiệu năng giả định thành kết quả.

## 5. Các file hiện tại dự kiến bị tác động

| File/khu vực | Thay đổi dự kiến và lý do |
|---|---|
| `DOTS/Integration/SwarmGameBridge.cs` | Điều phối bootstrap, đồng bộ grid rõ ràng, readiness và goal |
| `DOTS/Integration/GameplayGameBridge.cs` | Kiểm tra spawn/bind một lần, cleanup khi apply lỗi nếu cần |
| `DOTS/Runtime/*SimulationSystem.cs` | Chỉ thêm cờ giữ simulation hoặc API dữ liệu phục vụ đồng bộ nếu cần; giữ ownership/dependencies |
| `Scripts/Managers/TilemapManager.cs` | Áp dụng/công bố grid version, quy ước bounds và nước logic |
| `Scripts/Resources/Services/ResourceSpawner.cs` | Ngăn đường spawn tự động/phím P chạy đồng thời trong chế độ procedural; giữ chức năng scene cũ |
| `Scripts/Managers/GameManager.cs` | Readiness cho input/placement, composition và ví khởi đầu đúng một lần |
| `Scripts/Managers/DayNightCycleManager.cs` | Bắt đầu đồng hồ khi map Ready |
| `Scripts/Managers/EnemyWaveManager.cs` | Readiness, ứng viên spawn có kết nối và fallback hữu hạn |
| `Scripts/Managers/FogOfWarManager.cs` | Khởi tạo bounds/dữ liệu đúng thời điểm, quản lý texture/event lifecycle |
| `Scripts/UI/MinimapFogUI.cs`, `Scripts/Utils/CameraController.cs` | Đồng bộ bounds/điểm nhìn ban đầu sau tạo map |

Đường dẫn tính từ `Assets/Game/`. Chỉ sửa phần thực sự cần theo kết quả tích hợp; không refactor toàn bộ manager hoặc di chuyển logic AI ra khỏi ECS trong đợt này. Các thay đổi đang có trong working tree phải được giữ, không reset để bắt đầu module.

## 6. Thứ tự bàn giao và tiêu chí kết thúc

Thực hiện lần lượt **M1 → M2 → M3 → M4 → M5 → M6**. Có thể phát triển contract readiness từ M1, nhưng chỉ mở gameplay sau khi M4 đạt các kiểm tra bắt buộc. Mỗi mốc là một thay đổi có thể review riêng và có cập nhật tài liệu tương ứng.

Ba bản bàn giao dễ kiểm tra:

1. Sau M2: nhập seed và xem địa hình, vùng xuất phát, đường nối và lỗi validation.
2. Sau M3: xem đầy đủ bố trí tài nguyên và báo cáo đường khai thác/footprint.
3. Sau M6: chạy trận RTS trên map tự sinh, có bằng chứng kiểm thử và hiệu năng.

Kết thúc phiên bản đầu khi scene mới chơi được theo seed, mọi invariant bắt buộc được kiểm tra, preset mặc định đạt bộ seed đã định, gameplay cũ không hồi quy và tài liệu chỉ rõ chức năng/kết quả nào đã thực thi. Nghiên cứu, kế hoạch và test/benchmark thực tế được giữ thành các trạng thái riêng.

## 7. Tham khảo và trạng thái kiểm chứng kế hoạch

Thuật toán, nguồn gốc và lý do lựa chọn được dẫn tại [Research — Nguồn tham khảo](Research.md#10-nguồn-tham-khảo). Quy trình chạy tests hiện có nằm trong [Gameplay Usage](../DOTSGameplay/Usage.md) và [Swarm Usage](../DOTS/Usage.md).

Lần lập kế hoạch này đọc lại tài liệu và các điểm khởi động/navigation/spawn trong code; chỉ cập nhật Markdown. Chưa chạy Unity, chưa tạo scene/module runtime và chưa thực hiện bất kỳ mốc triển khai hoặc benchmark nào ở trên.
