# Build plan — Tech tree, tăng trưởng incremental và bản đồ chiến thuật

Ngày lập: **05/11/2026**. Trạng thái: **kế hoạch đề xuất, chưa triển khai gameplay trong đợt này**.

Điểm tiếp tục duy nhất: [Progress.md](Progress.md). Đọc file đó trước khi chọn công việc; không suy ra trạng thái hoàn thành từ mô tả trong kế hoạch này.

## 1. Mục tiêu và phạm vi

Phát triển game RTS phòng thủ theo ngày với vòng lặp: khai thác → đầu tư nghiên cứu → tăng năng lực kinh tế/quân sự → mở rộng địa bàn → chống wave → đầu tư tiếp. Tech tree và bản đồ phải khiến người chơi thay đổi thứ tự đầu tư giữa các seed.

Phiên bản đầu là một trận có tiến trình hữu hạn, kết thúc bằng mục tiêu sinh tồn được cấu hình và kiểm chứng trong lát cắt S0. Chưa coi đây là idle game chạy offline hoặc game có tăng trưởng vô hạn. Mốc đích đề xuất cho playtest là sống qua 5 ngày; chỉ là giả thuyết cân bằng, không phải thời lượng hay độ khó đã đạt.

Kết quả cần bàn giao:

- Tech tree 6 node, một nghiên cứu đang chạy cho mỗi người chơi, có điều kiện, chi phí, thời gian và phản hồi UI.
- Nâng cấp ảnh hưởng đúng simulation ECS; quân đang có và quân sinh sau nhận cùng quy tắc.
- Map có ba vai trò vùng: khu xuất phát, vùng mở rộng kinh tế, vùng mục tiêu có rủi ro; có hai hướng tiếp cận chiến thuật và một địa điểm đáng tranh giữ.
- Một vòng chơi hoàn chỉnh trong ProceduralGame, các scene tham chiếu vẫn hoạt động.
- Tài liệu trạng thái, kiểm thử, bằng chứng và điểm bàn giao sau từng lát cắt.

Ngoài phạm vi đợt đầu: prestige, offline income, save/load toàn bộ trận, vô hạn cấp nâng cấp, thêm loại tiền, streaming map, đổi render pipeline, viết framework hiệu ứng tổng quát. Nếu người dùng bổ sung các yêu cầu này, ghi quyết định mới và tách lát cắt; không âm thầm mở rộng công việc.

## 2. Căn cứ hiện tại và việc cần xác minh

Đã đọc TechnicalOverview, DOTS DevelopmentGuide, kiến trúc Procedural Generation và đối chiếu một số đường runtime ngày 05/11/2026.

| Hiện trạng | Hệ quả thiết kế |
|---|---|
| PlayerWallet và logic khai thác/xây/huấn luyện nằm trong ECS | Research phải dùng cùng nguồn ví, không giữ một ví riêng trên UI |
| GameObject là authoring/presentation của thực thể đã bind | Không thêm behavior nâng cấp vào Update legacy rồi kỳ vọng ECS sử dụng |
| Map sinh từ noise, giữ vùng tâm/hành lang phải, đặt quota và validate | Giữ RNG/BFS/validator hiện có; bổ sung cấu trúc vùng trước noise |
| Core chỉ có Grass/Water và Tree/Gold/Sheep | Ba vai trò vùng ban đầu dùng lại địa hình/tài nguyên, chưa cần biome vật lý mới |
| MapData chưa có region, gateway, landmark | Bổ sung dữ liệu rõ ràng, không suy luận gameplay từ sprite |
| Chưa có module save/load Entity theo TechnicalOverview | Tiến trình giữa các phiên phát triển nằm trong docs; tiến trình trận chưa được lưu qua lần mở game |

**Đính chính giả định tài nguyên:** quota trong generator là số placement. Đoạn Gathering trong GameplayEconomyJob tăng Carried nhưng chưa thấy trừ trữ lượng node. Không gọi tài nguyên hiện tại là hữu hạn chỉ từ quota. S0 phải kiểm tra Tree, GoldStone, SheepUnit, resource components, lifecycle và đường legacy để chốt nguồn nào cạn/tái sinh. Nếu cây/vàng khai thác liên tục, giữ hành vi đó trong phiên bản đầu; không tự thêm trữ lượng hoặc tái sinh.

Tài liệu procedural hiện có cả ImplementationPlan cũ và Architecture phản ánh lát cắt đầu tiên. Kế hoạch này là đợt mở rộng, không đánh dấu lại các mục cũ thành hoàn thành khi chưa có bằng chứng.

## 3. Quy tắc thực hiện qua nhiều phiên

**Yêu cầu bổ sung: Dependency Injection cho mã do dự án sở hữu.** Thực hiện theo [hợp đồng DI và lộ trình D0–D4](../DependencyInjection/Architecture.md). Thứ tự cập nhật: S0 → D0 → D1 → S1/S2 kèm D2 → S3 → S4/S5 → S6 kèm D3 → S7/S8 → D4 → S9. Các công việc S chưa thay đổi giữ nguyên; D là điều kiện bổ sung, không được bỏ qua để báo hoàn tất toàn project. D0/D1 tạo nền trước khi viết module progression mới.

Mỗi checkpoint phải ghi dependency đã chuyển, lookup/static access còn lại và trạng thái inventory D0. Kiểm tra DI trở thành một phần tiêu chí DONE của từng lát cắt có code mới. Runtime mới nhận dependency tường minh; không tự tìm manager/service. Thiết kế mặc định là manual DI, không tự thêm thư viện container.

1. Đọc AGENTS.md, TechnicalOverview và Progress.md; sau đó chỉ đọc kiến trúc/file liên quan lát cắt đang làm.
2. Kiểm tra git status. Ghi nhận thay đổi có sẵn; không reset hoặc đưa thay đổi không liên quan vào công việc.
3. Chọn **một lát cắt nhỏ nhất đang sẵn sàng**. Nếu chưa xong lát cắt cũ, tiếp tục nó. Không cần đọc lại toàn repo hoặc làm nhiều lát cắt để được coi là một phiên hữu ích.
4. Đọc các callers, tests, fixture, asset/config và bridge bị ảnh hưởng trước khi sửa. Liệt kê phạm vi thật trong nhật ký phiên.
5. Triển khai, chạy kiểm tra phù hợp, cập nhật Architecture/Usage/Validation tương ứng và Progress.md trong cùng phiên.
6. Khi không đủ thời gian/credits hoặc Unity không sẵn sàng, ghi rõ checkpoint, phần chưa chạy và bước tiếp theo. Chỉ đánh dấu PARTIAL hoặc IMPLEMENTED_UNVERIFIED; không dùng DONE.
7. Mỗi lần dừng phải để lại file bàn giao đã lưu trên đĩa. Ưu tiên checkpoint sớm sau một thay đổi đã compile thay vì đợi hết phiên; việc hết credits đột ngột vẫn có thể cắt mất cập nhật cuối.

Trạng thái hợp lệ: TODO → IN_PROGRESS → PARTIAL / IMPLEMENTED_UNVERIFIED → DONE. BLOCKED phải ghi điều kiện gỡ chặn cụ thể. Chỉ DONE khi implementation, kiểm tra bắt buộc và docs cùng hoàn tất. Có thể tách hậu kiểm Unity thành tác vụ riêng nhưng lát cắt gốc vẫn chưa DONE.

Không cam kết số credits/phút mỗi lát cắt. Khi scope quá lớn, tạo Sx.a/Sx.b trong Progress với hợp đồng đầu vào/đầu ra rõ ràng. Không tự tạo chat mới hoặc agent để thực hiện kế hoạch.

## 4. Thiết kế gameplay mặc định

### 4.1 Tech tree phiên bản đầu

Các tên ID và số dưới đây là cấu hình thử nghiệm, chưa phải kết quả cân bằng. Giá ghi theo Gỗ/Vàng/Thịt; dùng đúng thứ tự tham số của API thực tế khi tích hợp.

| ID | Tên | Điều kiện | Giá thử | Thời gian thử | Tác dụng |
|---|---|---|---|---|---|
| economy_tools | Dụng cụ khai thác | Không | 40/15/0 | 20 giây | Lượng gỗ mỗi nhịp ×1,25 |
| economy_cargo | Vận chuyển cải tiến | economy_tools | 80/30/0 | 30 giây | Sức chứa dân ×1,5 cho các profile hỗ trợ |
| military_archery | Kỹ thuật cung | Không | 50/30/10 | 25 giây | Mở xây ArcheryRange và huấn luyện cung thủ |
| military_training | Huấn luyện có tổ chức | military_archery | 80/60/20 | 35 giây | Thời gian các lượt huấn luyện mới ×0,8 |
| expansion_tower | Phòng tuyến tiền tiêu | Không | 60/25/0 | 25 giây | Mở xây TowerUnit |
| expansion_fortify | Gia cố tháp | expansion_tower | 100/60/0 | 40 giây | Sát thương tháp ×1,25 |

S1/S2 làm duy nhất economy_tools trước, sau khi command–wallet–job–UI chạy đúng mới mở đủ catalog. Tech tree dùng quan hệ prerequisite không chu kỳ; không cần editor vẽ graph riêng. UI ba cột, mỗi cột hai node đủ cho bản đầu.

Quy tắc:

- Một research slot ở cấp người chơi; bắt đầu qua TownHall đã xây xong và còn sống. Prototype không có queue hay hủy nghiên cứu.
- Trừ tiền đúng một lần khi bắt đầu thành công. Thiếu tiền, đang bận, ID sai, đã mở hoặc thiếu prerequisite đều bị từ chối mà không thay đổi ví.
- Hoàn tất đúng một lần. Pause dừng timer theo simulation. TownHall nghiên cứu bị phá: hủy nghiên cứu, không hoàn tiền; UI phải báo trước quy tắc này. Trận kết thúc dừng nghiên cứu.
- Nâng cấp khai thác/carry/damage áp dụng cho thực thể hiện có và sinh sau. Không thay đổi lượng hàng đang mang; sức chứa hiệu dụng dùng cho các lần khai thác kế tiếp.
- Training multiplier được chốt khi enqueue; các lượt đã xếp hàng giữ duration cũ, tránh sửa tiến độ giữa chừng.
- Giá trị nguyên làm tròn theo một helper thống nhất, tối thiểu 1 với lượng khai thác/sát thương dương. Không nhân lại lên số đã nâng cấp mỗi frame hoặc khi load/rebind.
- Node bị khóa vẫn hiện lý do; UI hiển thị giá, hiệu ứng, thời gian, prerequisite và tiến độ. Kiểm tra điều kiện ở command, không chỉ disable nút.
- Khóa nội dung chỉ bật với preset progression của ProceduralGame; SampleScene/demo không có cấu hình progression giữ chế độ nội dung sẵn có. Không sửa prefab dùng chung để xóa actions.

### 4.2 Vai trò bản đồ

Khu xuất phát bảo đảm đủ tài nguyên/chỗ xây và không bị landmark hoặc trang trí chiếm lối. Hai vùng ngoài có lợi ích khác nhau: vùng giàu gỗ/cừu và vùng giàu vàng. Tổng quota vẫn có budget để tránh seed vô tình nhiều tài nguyên hơn quá mức.

Mỗi map có một landmark kinh tế ở xa: cụm mỏ nổi bật với chỗ đặt depot/phòng tuyến và đường đi dài hơn. Bản đầu dùng tài nguyên hiện có, không cần hệ thống chiếm điểm hay tiền thưởng mới. Landmark phải có tên/biểu tượng/khung cảnh nhận biết và lợi ích đọc được; không chỉ là sprite trang trí.

Hai cửa tiếp cận từ vùng spawn ngoài vào mạng đường chính tạo lựa chọn giữ tuyến. Không mặc định hai đoạn hành lang đồng nghĩa quái chỉ đi qua đó: cần kiểm tra toàn bộ walkable topology và nối EnemyWaveManager với spawn locations hợp lệ. Không thêm cửa thứ ba ngoài ý muốn do dải đất biên cũ.

Không khóa công nghệ bắt buộc sau vùng mà chính công nghệ đó mới mở được. Đợt đầu không thêm cầu xây được, phá đá mở đường hoặc địa hình thay đổi giữa trận.

## 5. Kiến trúc, dữ liệu và luồng

Quy mô: small-game phục vụ khóa luận. Dùng bốn phần có trách nhiệm rõ:

| Phần | Sở hữu dữ liệu và trách nhiệm |
|---|---|
| Progression authoring | TechDefinition/catalog ScriptableObject: stable ID, tên/icon, prerequisite, giá, duration, hiệu ứng có kiểu rõ ràng |
| DOTS runtime | Trạng thái nghiên cứu/người chơi, tập tech đã mở và các modifier hiệu dụng; PlayerWallet là nguồn tiền duy nhất |
| Integration/UI | Resolve catalog sang dữ liệu runtime, command adapter, cập nhật UI qua snapshot/event; không ghi ngược state từ UI |
| Procedural Core + Integration hiện có | Region/gateway/landmark trong MapData; sinh và validate; áp dụng Tilemap/prefab/navigation/fog/minimap |

Tên class/file mới là dự kiến; quyết định theo pattern thực tế trong S0/S1. Đặt authoring/UI ở Assets/Game/Progression; dữ liệu/system ECS mới ở Assets/Game/DOTS/Runtime. RTS.DOTS không tham chiếu ScriptableObject gameplay hoặc Assembly-CSharp. Không tạo singleton MonoBehaviour thứ hai giữ cùng unlock state.

Luồng research: UI → bridge → command validate và giao dịch ví → ECS tick → hoàn tất/unlock → modifier hoặc capability gate → bridge → UI. Validate và debit phải cùng đường thực thi tuần tự, an toàn với jobs đang truy cập ví; không dùng giá trị HUD cache để quyết định đủ tiền.

Luồng khởi động: map sinh/validate → áp dụng terrain/prefab → đồng bộ physics/navigation → bind simulation và cấu hình progression → mở input/ngày đêm/wave. Tận dụng readiness hiện có, chỉ mở rộng các thiếu sót đo được; không dựa vào delay đoán trước.

Luồng generator đề xuất:

1. Settings + seed + generator version; các phase có RNG độc lập để thêm decor không đổi placement gameplay.
2. Chọn tâm vùng với khoảng cách tối thiểu, gán vai trò và budget; tạo graph nhỏ nối khu xuất phát với hai vùng ngoài và landmark.
3. Rasterize vùng/đường/cửa với bề rộng hợp collider; noise điều chỉnh bờ và chi tiết ngoài mask bảo vệ.
4. Đặt tài nguyên theo budget vùng; giữ quota xuất phát theo path distance, footprint và ô tương tác.
5. Validate đường dân, đường quái, vùng xây, landmark, gateway và mọi tài nguyên sau placement. Repair hữu hạn rồi validate lại; thất bại có seed/attempt/lý do.
6. Decor chạy cuối, không thay navigation; áp dụng scene theo pipeline hiện có.

Không bắt buộc Voronoi library hoặc WFC: vài tâm vùng, distance assignment, graph nhỏ và BFS hiện có đủ cho phạm vi đầu. Nếu cần thuật toán khác, ghi vấn đề cụ thể và so sánh trước khi thay.

## 6. Các lát cắt triển khai

### S0 — Xác lập baseline và hợp đồng gameplay

- Đầu vào: tài liệu hiện tại và scene chạy được; chưa sửa gameplay để mở rộng.
- Kiểm tra tài nguyên cạn/tái sinh, nguồn TownHall/điều kiện thua, wave spawn, training queue, readiness và prefab actions. Ghi callers và API thật cần dùng.
- Chạy baseline compile/EditMode phù hợp; thử MainMenu → ProceduralGame, khai thác/nộp, xây/huấn luyện, pause, một wave. Ghi seed, settings và phiên bản.
- Chốt mục tiêu 5 ngày hoặc điều chỉnh có lý do; ghi cơ chế thắng/thua đã có hay cần bổ sung ở S8. Chốt số liệu khởi đầu để không khóa toàn bộ kinh tế.
- Đầu ra: Architecture/Usage/Validation sơ bộ của Progression; baseline vào Progress.
- DONE: không còn giả định chưa xác minh về ví, resource lifecycle, command research và spawn wave; bằng chứng Unity hoặc trạng thái hậu kiểm rõ ràng.

### S1 — Một công nghệ chạy trong simulation

- Phụ thuộc S0. Tạo catalog tối thiểu economy_tools; validate ID trùng, prerequisite không tồn tại/chu kỳ, giá/duration sai và effect không hỗ trợ.
- Tạo research state, command bắt đầu và tick hoàn thành; transaction dùng PlayerWallet; kiểm tra TownHall và lifecycle.
- Nối modifier khai thác gỗ vào GameplayEconomyJob; đọc modifier theo tick/job, không quét mọi entity mỗi frame từ MonoBehaviour.
- Test: thiếu tiền; bắt đầu hai lần; công nghệ hoàn tất một lần; unlock không nhân dồn; worker đang có/sinh sau; pause và TownHall bị phá.
- DONE: test runtime pass, compile, chạy được research qua API kiểm chứng; chưa cần UI đẹp. Ghi thay đổi chính xác ở runtime/bridge.

### S2 — Giao diện nghiên cứu dùng được

- Phụ thuộc S1. Panel từ TownHall, một node và slot tiến độ; giá/điều kiện/lý do từ chối; phản ánh ví authoritative.
- Kiểm tra đóng/mở panel, đổi selection, thiếu tiền, spam click, hoàn tất khi panel đóng và tải lại scene không giữ event subscription cũ.
- Kiểm tra thao tác chuột thật trong Unity, pause/resume và độ đọc ở độ phân giải mục tiêu. Lỗi không có asset icon dùng placeholder được ghi rõ.
- DONE: từ MainMenu vào trận, tự khai thác đủ tiền, bấm research và thấy tốc độ thu gỗ thay đổi đúng quy tắc; ghi seed và bằng chứng.
- Điểm dừng sử dụng được đầu tiên: một research hoàn chỉnh từ UI tới ECS.

### S3 — Hoàn tất cây sáu công nghệ

- Phụ thuộc S2. Thêm năm node còn lại, prerequisite, cargo/training/tower effect và capability gates ở build/train command.
- Rà tất cả callers của BuildActionSo, spawn actions, ProductionBuilding và bridge; mọi đường bypass UI vẫn phải tuân gate khi progression bật.
- Giữ bản config không progression để hồi quy SampleScene/demo. Không sửa shared SO runtime.
- Test: prerequisite; không thể xây/huấn luyện lách gate; queue cũ/mới; Tower hiện có/sinh sau; rounding; reopen scene reset state; config thiếu catalog báo lỗi rõ.
- DONE: sáu node hoạt động, docs mô tả đúng hiệu ứng, preset/scene gán đủ asset và hồi quy scene cũ pass.

### S4 — Dữ liệu vùng và preview map có vai trò

- Phụ thuộc S0; thực hiện sau S3 theo thứ tự mặc định để giữ một nhánh việc.
- Mở rộng MapData/settings với region, gateway, landmark và generator version. Giữ preset cũ để so sánh.
- Tạo graph vùng và rasterization; preview tô màu vai trò/đường/cửa/landmark với seed và lỗi, chưa cần art mới.
- Test determinism toàn bộ dữ liệu mới, seed 0/âm, settings không khả thi, retry hữu hạn, không overlap vùng xuất phát.
- DONE: Core + preview thể hiện rõ hai hướng và ba vai trò; thiếu tính kết nối cuối scene vẫn ghi là chưa được nghiệm thu gameplay.

### S5 — Tài nguyên và kiểm chứng topology cuối

- Phụ thuộc S4. Chuyển quota ngoài căn cứ thành budget vùng; landmark là cụm vàng xa có footprint/chỗ xây/lối tương tác.
- Validate sau khi đặt vật cản: đường tới tài nguyên, khoảng đường vận chuyển, cửa đủ clearance, khu xuất phát và lối ra building. Xác định cửa trên topology cuối, không chỉ metadata.
- Fixtures: cổ chai bị cây bịt, mỏ sát nước, chỉ chạm góc, landmark cô lập, budget không đủ chỗ. Quét 1.000 seed và tái sinh so sánh, lưu cả seed lỗi.
- DONE: không có map invalid được báo thành công trong tập kiểm tra; lỗi settings được từ chối hữu hạn. Ghi pass rate/attempts thực đo, không ép báo 100% bằng hạ quota.

### S6 — Tích hợp map, wave và navigation

- Phụ thuộc S3 + S5. Áp dụng region/landmark trong ProceduralGame; nối spawn wave với vị trí hợp lệ dẫn qua các hướng tiếp cận.
- Đồng bộ navigation cho cả GameplaySimulationSystem và SwarmSimulationSystem sau terrain/resources. Kiểm tra fog/minimap/camera, input readiness và lifecycle khởi động lại scene.
- Giữ mức áp lực wave tương đương khi đổi vị trí spawn; không tăng số quái ngẫu nhiên theo lượng đất.
- Unity checks: worker tới hai vùng và nộp; xây tuyến; quái tới căn cứ qua các đường dự định; không spawn nước/kẹt collider; không duplicate entities.
- DONE: ít nhất ba seed có bố trí khác nhau hoàn thành các thao tác trên; ghi bằng chứng và giới hạn.

### S7 — Nhận diện vùng và khoảnh khắc khám phá

- Phụ thuộc S6. Tận dụng tile/prefab hiện có để tạo mép vùng, cụm trang trí, silhouette landmark và icon trên minimap sau khi khám phá.
- Chỉ thêm art còn thiếu thực sự; ghi nguồn/license của asset mới. Decoration không cản click, navigation hoặc che tài nguyên quan trọng.
- Đặt mức sáng/màu theo ngày đêm vẫn đọc được quân/quái; không đổi pipeline toàn dự án.
- DONE: ảnh cùng seed trước/sau, kiểm tra ngày/đêm và thao tác chọn/khai thác; người xem nhận ra landmark và hướng mở rộng bằng hình ảnh.

### S8 — Một trận hoàn chỉnh và cân bằng bước đầu

- Phụ thuộc S3 + S6; S7 cần xong trước nghiệm thu hình ảnh. Hoàn thiện điều kiện kết thúc theo S0, ngăn command sau kết thúc và thử bắt đầu trận mới.
- Playtest ít nhất ba seed cố định, mỗi seed thử ưu tiên kinh tế và ưu tiên quân sự; ghi thời gian unlock, tốc độ thu/nộp, lượng quân, ngày thua/thắng và lựa chọn mở rộng.
- Mục tiêu đánh giá: có lý do đổi thứ tự nghiên cứu/hướng mở rộng; không có node bắt buộc mua ngay trong mọi tình huống vì quá rẻ; không có thời gian chờ dài mà không có quyết định khả dụng. Ghi nhận quan sát, không khẳng định vui chỉ bằng test tự động.
- Chỉnh giá/duration/quota/wave dựa trên dữ liệu. Không mở rộng tree để che vòng chơi chưa hấp dẫn.
- DONE: có báo cáo playtest, config cuối và lỗi còn tồn tại; game có thể chơi từ đầu tới kết thúc theo điều kiện đã chốt.

### S9 — Hồi quy, đo hiệu năng và bàn giao khóa luận

- Phụ thuộc S7 + S8. Chạy test Progression/Procedural/DOTS phù hợp; MainMenu, ProceduralGame, SampleScene; kiểm tra build Player nếu môi trường cho phép.
- Đo tổng scene→Ready và tách Core, Tilemap/prefab, physics/navigation/binding; frame time trong gameplay có worker/quân/quái và render thật. Ghi máy, Editor/build, resolution, seed, số thực thể, warmup, thời lượng và median/p95 khi đủ mẫu.
- Research không phát sinh allocation mỗi tick do UI polling/quét catalog; generator có retry budget; không suy rộng benchmark quái thành hiệu năng dân.
- DONE: Architecture, Usage, Validation và Evidence mới liên kết đầy đủ, ghi giới hạn; Progress có hướng tiếp theo dựa trên lỗi/quan sát thật.

## 7. Chạy kiểm tra và lưu bằng chứng

- Dùng Unity Test Runner EditMode cho quy tắc research, modifier, command và generator. Khi có code mới phải thêm case nhỏ kiểm tra nhánh nghiệp vụ, không chỉ phản chiếu implementation.
- Runtime/bridge/UI/prefab cần Play Mode hoặc Player smoke; compile C# ngoài Unity không thay thế các bước này.
- Tool quét Core hiện có: `dotnet run --project Tools/ProceduralGenerationSeedCheck/ProceduralGenerationSeedCheck.csproj -- 1000`. Mở rộng so sánh dữ liệu mới khi S4/S5 thay MapData.
- Sử dụng hướng dẫn chạy hiện có của DOTSGameplay và ProceduralGeneration; không đoán lệnh CLI/test filter. Lưu lệnh thực đã dùng trong Validation.
- Evidence đặt tại `Docs/Modules/Progression/Evidence/` hoặc `Docs/Modules/ProceduralGeneration/Evidence/`; tên gồm ngày, slice và seed. Test XML/JSON/CSV và ảnh phải có chú thích điều kiện; không đưa build/cache lớn vào repo.
- Bằng chứng lịch sử 1.000 seed ngày 29/10/2026 chỉ áp dụng generator cũ. Sau sửa topology phải quét lại.

## 8. Rủi ro và giới hạn cần theo dõi

| Rủi ro | Cách xử lý trong kế hoạch |
|---|---|
| Nâng cấp chỉ tác động quân sinh mới hoặc bị áp dụng nhiều lần | Quy tắc base/effective và tests ở S1/S3 |
| Trừ tiền hai lần hoặc UI dùng ví cũ | Một command authoritative và test spam/insufficient funds |
| Ngày đêm quá gấp biến research thành chờ chết | S8 đo thời gian tích lũy/unlock so với wave |
| Cửa trên preview không đúng đường quái thật | S5 kiểm topology; S6 kiểm cả hai navigation system trong Unity |
| Decor làm giảm hiệu năng/cản click | S7 raycast/selection và S9 đo Player |
| Credit hết giữa chừng | Checkpoint sớm; PARTIAL có file, test, bước tiếp cụ thể |
| Chưa lưu được trận | UI/docs nói rõ tiến trình reset khi bắt đầu trận mới; save/load là đợt riêng |

## 9. Nguồn đối chiếu

- [TechnicalOverview](../../TechnicalOverview.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md), [Gameplay Architecture](../DOTSGameplay/Architecture.md).
- [Procedural Architecture](../ProceduralGeneration/Architecture.md), [Research](../ProceduralGeneration/Research.md), [Validation](../ProceduralGeneration/Validation.md).
- Code: GameplayComponents, GameplayEconomyJob, GameplaySimulationSystem, GameplayGameBridge, GameManager, BuildActionSo, SpawnArcherActionSO, ProceduralMapGenerator, MapGenerationData.
- [Experience-Driven Procedural Content Generation](https://yannakakis.net/wp-content/uploads/2015/11/PID3821875.pdf): nền tham khảo cho việc đánh giá nội dung theo trải nghiệm; kế hoạch này không triển khai mô hình học người chơi.
- [Red Blob Mapgen4](https://www.redblobgames.com/maps/mapgen4/): tham khảo tổ chức lớp địa hình/vùng; không sao chép toàn bộ thuật toán hoặc thêm dependency.
