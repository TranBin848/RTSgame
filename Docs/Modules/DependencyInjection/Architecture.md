# Dependency Injection — thiết kế và lộ trình chuyển đổi

**17/11 — D0–D4 DONE trong phạm vi inventory và ngoại lệ:** UI/cursor/fog/camera/input/bootstrap đã chuyển, pool nhận fog mới, scene tham chiếu giữ hoạt động. [D4 Validation](D4-Validation.md) ghi luồng,81tests, integration, scan và các giới hạn single-active-scene/engine/asset/tooling. Các checkpoint cũ dưới đây là lịch sử, không dùng làm trạng thái hiện tại.

Cập nhật **15/11/2026 — D3c2:** ProceduralMapAuthoring giữ explicit spawner refs; root và legacy bootstrap inject game/map/bridge vào ResourceSpawner, Awake không tìm service hoặc sinh resource. Suppression/initial-regenerate/Entity cleanup/worker deposit/menu reload PASS; [D3c2 Evidence](D3c2-ProceduralSpawners.md). Manual P/click đang chờ người dùng; D3 tổng thể PARTIAL, tiếp production/train/factory/pool.

Tiếp nối cuối **13/11/2026 — D3c1:** BuildingProcess dùng constructor injection game/bridge; GameManager sở hữu truyền deps, process không Get/Instance và nhánh ECS không gọi Worker.SendToBuild để lấy lại bridge global. Các caller/tools đã cập nhật, validation/construction/queue/removed-view và SampleScene checks PASS; [D3c1 Evidence](D3c1-Construction.md). D3 tổng thể vẫn PARTIAL, tiếp procedural/spawner/production/train/factory/pool.

Cuối **13/11/2026 — lifecycle S6c:** EndComposition xóa Active/Ready/error flags thay vì giữ state procedural ở MainMenu. Menu→game→menu→game đã kiểm registry/Entity/ví/listener reset; [Evidence](../ProceduralGeneration/S6c-WavesLifecycle.md). D3c chưa migrate procedural/spawner/build/train/factory/pool; không đổi trạng thái D3 thành DONE chỉ vì lifecycle đạt.

Cập nhật mới nhất **13/11/2026 — D3b resource nodes:** bridge RegisterResource inject bridge/map vào cây/mỏ; unregister và scene teardown unbind. Node không lookup service nữa. Lifecycle/claims/nav và kinh tế hai vùng trên3seed PASS; [S6b Evidence](../ProceduralGeneration/S6b-EconomyResourceDI.md). D3 tổng thể vẫn PARTIAL; procedural/spawner/build/train/factory/pool chưa hoàn tất.

Ngày: **12/11/2026**. Trạng thái: **D0–D2 hoàn thành; D3 PARTIAL (wave D3a đã kiểm); D4 chưa triển khai**. [ADR-001](ADR-001-ManualDI.md) là quyết định nền.

D3a: root inject game/map/day-night/bridge/generator vào EnemyWaveManager; lookup legacy ở bridge bootstrap. Init/listener/readiness, ba seed strategic spawn/path và SampleScene legacy PASS; [S6 Integration](../ProceduralGeneration/S6-Integration.md). Resource/procedural/factory/build/train/pool vẫn theo inventory, chưa coi toàn project đạt DI.

D2: root tạo ResearchService bằng constructor với bridge, inject IResearchService/GameManager vào panel. State và ví ở ECS; panel chỉ query/command, không lookup. Fake-service và listener close/reopen checks PASS; [Evidence](../Progression/S1-D2-Evidence-2026-11-11.md). S2 vẫn cần kiểm chứng click/hình ảnh UI trước khi phát triển thêm node.

Cập nhật 10/11/2026: root scene cấp bridge/map/camera/fog/day-night và hai resource locator cho GameManager. Root Start gọi BindScene, chỉ mở composition gate khi navigation và binding hoàn tất; thiếu cấu hình giữ gameplay blocked. Lookup còn lại trong GameManager chỉ ở adapter scene legacy. Xem [Usage](Usage.md), [Validation](Validation.md) và [D1 Evidence](D1-Evidence-2026-11-10.md).

Theo dõi tiến độ tại [Progression/Progress](../Progression/Progress.md), kế hoạch tổng tại [BuildPlan](../Progression/BuildPlan.md). Tài liệu này là hợp đồng kiến trúc cho các lát cắt tiếp theo.

## 1. Mục tiêu và phạm vi

Dependency của một đối tượng được truyền từ bên ngoài, thể hiện rõ ở constructor hoặc hàm khởi tạo, thay vì đối tượng tự tìm manager/service toàn cục. Mục tiêu là kiểm thử nghiệp vụ bằng dependency thay thế, kiểm soát vòng đời scene và giải thích được đồ thị phụ thuộc trong khóa luận.

Áp dụng cho mã do dự án sở hữu trong Assets/Game: gameplay MonoBehaviour, manager, UI, resource services, progression và các adapter procedural/DOTS. Không sửa package bên thứ ba chỉ để áp quy ước. ECS giữ cơ chế World/system/component/job riêng; DI áp dụng tại cầu nối vào World, không truyền managed interface vào Burst job.

## 2. Baseline D0 lịch sử — đã được D1–D4 thay thế

- GameManager đọc ví và đăng ký Unit qua SwarmGameBridge.Instance; Start còn tìm camera/fog/canvas.
- EnemyWaveManager tìm manager/map/day-night rồi gọi bridge static để spawn/retreat.
- SwarmGameBridge có auto-bootstrap, Instance, tìm manager/map/fog/day-night trong lifecycle.
- ProceduralMapAuthoring bắt đầu công việc trong Awake; phải phối hợp với bootstrap mới.
- Đã có IPlayerResourceWallet, IResourceNodeLocator, IResourceDepotLocator. IPlayerResourceWallet hiện chỉ có AddResource, chưa đủ hợp đồng đọc số dư và chi tiêu research.
- Packages/manifest.json chưa khai báo DI container; chưa kiểm kê toàn bộ Assets để kết luận không có thư viện embedded.

Đây là các điểm đã thấy, không phải danh sách đầy đủ. D0 phải lập inventory trước khi tuyên bố project tuân thủ DI.

## 3. Quyết định kiến trúc

Mặc định dùng **manual DI**: constructor injection cho C# thuần, Init/Construct với tham số tường minh cho MonoBehaviour. Composition root lắp ghép bằng code và tham chiếu Inspector. Chưa thêm container; nếu D0 phát hiện container hiện có thì đánh giá tái sử dụng và ghi quyết định trước khi chọn.

Không tạo interface cho mọi class. Dùng interface ở ranh giới cần thay thế khi test hoặc có nhiều implementation, ví dụ query/command nghiên cứu, spawn và resource locator. Dependency concrete ổn định vẫn có thể được inject trực tiếp. Interface mới phải có caller thực và trách nhiệm nhỏ.

Quy ước bắt buộc:

1. Nghiệp vụ mới không gọi Manager.Instance, FindObject*, GameObject.Find hoặc container.Resolve/GetService để lấy dependency. Truyền một service locator vào mọi object cũng không đạt yêu cầu.
2. Composition root là nơi được tạo/chọn service và nối references. Lookup cần cho scene cũ chỉ tồn tại trong adapter bootstrap được ghi rõ; không đặt fallback lookup trong consumer đã migrate.
3. MonoBehaviour.Awake chỉ xử lý dữ liệu nội bộ; không gọi dependency chưa inject. OnEnable trước Init không subscribe/tick; Init phải thiết lập subscription đúng một lần và hoạt động được nếu object đã enabled.
4. Init thiếu dependency phải báo lỗi cấu hình rõ và giữ module chưa Ready. Không tự tạo service mặc định khiến hai ví/hai simulation chạy song song.
5. Không lưu service theo scene trong ScriptableObject dùng chung hoặc static context. ActionSO giữ config, nhận context/dependency theo lần thực thi; tái sử dụng tham số manager hiện có nếu phù hợp rồi thu hẹp khi migrate.
6. Runtime factory/spawner inject trước khi object bắt đầu behavior. Với prefab active, Awake có thể đã chạy trước khi inject; phải có guard hoặc tạo inactive rồi Init và activate. Pool phải unbind khi return, rebind khi acquire; không giữ reference của scene trước.
7. GameObject tự lấy component cùng object để phục vụ presentation được phép; ghi rõ đây là cấu trúc nội bộ, không dùng để tìm service của scene.

## 4. Composition root, dữ liệu và vòng đời

Root dự kiến: GameSceneCompositionRoot trong Integration/composition, tên cuối chốt D1. MainMenu có root nhỏ riêng nếu cần. Root không phải manager nghiệp vụ mới: chỉ dựng, nối, khởi động và dọn dependency.

| Phạm vi | Sở hữu | Quy tắc |
|---|---|---|
| Config asset | ScriptableObject/prefab | Dữ liệu authoring; không giữ trạng thái trận |
| Scene/trận | Root và adapter | Reference services/UI/map, subscriptions và trạng thái khởi tạo |
| ECS World | World/system | Wallet, research, actor data và NativeContainer; root không dispose World mặc định nếu không sở hữu |
| Unit spawn/pool | Factory + lifecycle | Bind view/entity và dependency; unbind đúng một lần |
| MainMenu | Root menu nếu cần | Không giữ bridge hoặc Entity của gameplay scene đã unload |

Root inject vào bridge, bridge lấy/được cấp World theo một điểm rõ ràng và chuyển command xuống system. Runtime assembly RTS.DOTS không tham chiếu root, Unit, UI hoặc ScriptableObject gameplay. Không duy trì bản sao wallet authoritative trên service managed.

Luồng khởi động dự kiến:

1. Root xác minh serialized references/config và lựa chọn World; tạo các adapter chưa hoạt động.
2. Inject dependency cho map authoring, bridge, manager và UI; các consumer chưa tick gameplay.
3. Sinh/áp dụng map, đồng bộ collider/navigation; tránh auto-registration Unit chạy trước bước này bằng readiness gate hoặc danh sách đăng ký chờ rõ ràng.
4. Configure simulation, bind thực thể ban đầu đúng một lần; bật progression và UI snapshot.
5. Đặt Ready rồi mở input/ngày đêm/wave.

Khi unload: ngừng command/tick do scene điều phối, hủy đăng ký events, unbind/remove thực thể do scene sở hữu, giải phóng tài nguyên thuộc adapter. Root không được dispose container/World của chủ sở hữu khác. Thứ tự cụ thể phải kiểm chứng với bridge hiện tại ở D1.

Trong giai đoạn chuyển tiếp, scene có root phải vô hiệu bootstrap tự động cũ trước khi nó tạo bridge; scene chưa migrate dùng adapter legacy. Cơ chế phát hiện phải tường minh và thử được, không dựa vào Awake ngẫu nhiên. D4 loại bỏ đường legacy không còn caller; ngoại lệ còn lại phải có lý do và phạm vi.

## 5. Giao tiếp và hợp đồng

- Command/query trực tiếp qua dependency đã inject; event chỉ dùng thông báo thay đổi cho UI/presentation và phải có unsubscribe.
- Research request kiểm tra/trừ tiền ở ECS một lần. Không thiết kế service TrySpend riêng tách rời StartResearch làm mất tính nguyên tử của giao dịch.
- Tận dụng IResourceNodeLocator/IResourceDepotLocator sau khi đọc implementation/callers. Không nhân đôi interface chỉ vì thêm DI.
- Nếu cần mở rộng IPlayerResourceWallet, kiểm kê mọi implementation/caller/test; cân nhắc hợp đồng đọc tách khỏi ghi. Không dùng HUD snapshot làm nguồn đủ tiền.
- Pure generator nhận settings/seed như hiện có; không inject scene manager vào Core. RNG xác định là input/implementation nội bộ rõ ràng, không UnityEngine.Random toàn cục.
- Tests fake query/command ở UI boundary; tests nghiệp vụ phải chạy command/system thật để kiểm tra ví và hiệu ứng. Fake không thay cho integration test.

## 6. Lộ trình thực hiện

Cập nhật đóng16/11 — **D3 DONE**: bridge inject Unit(game,map,bridge) trước BindDots/RegisterWithGame; Worker/Sheep và AIPawn dùng refs đã cấp, Structure cập nhật nav lúc acquire/return. Raw pool chỉ reset local state; caller phải Register sau acquire. Unregister dọn registry/Entity; teardown chỉ unbind, không sửa dictionary khi duyệt. Worker pool qua menu→game mới/command/return và17procedural PASS. [Evidence](D3c4b-UnitLifecycle.md), [S6 final](../ProceduralGeneration/S6-FinalValidation.md) ghi phạm vi; D4 toàn project chưa triển khai.

Cập nhật15/11 — D3c4a: GameManager factory dùng dependency đã nhận, Projectile chỉ legacy không lookup; native tạo Entity trực tiếp, không view tạm/ProxyFor ngầm bind. Root gọi BeginGameplay idempotent sau BindScene thay vì trông thứ tự Start của manager. [Evidence](D3c4a-ProjectileFactory.md) ghi startup/reload và damage/pool/callers PASS. Unit mẹ còn cấp manager bằng Get, cùng Worker/Sheep/Structure/AIPawn là D3c4b; chưa hoàn tất toàn graph DI.

Cập nhật15/11/2026: D3c3 đã inject producer tại bind, action debit sau enqueue accepted và cleanup khi unregister/return/destroy. Pool Spawn nhận owner Scene; chưa migrate toàn graph Unit/Projectile/static pool. [D3c3 evidence](D3c3-Production.md) ghi training/FIFO/pool API PASS và regression59+17; D3 tổng thể vẫn PARTIAL, tiếp D3c4 theo Inventory, rồi D4 consumer còn lại.

| Lát cắt | Phụ thuộc | Công việc và tiêu chí DONE |
|---|---|---|
| D0 — Inventory/ADR | S0 | Kiểm kê Instance, Find*, static mutable state, GetService/Resolve, new service và lifecycle trong Assets/Game. Lập bảng consumer → dependency → owner → cách inject → slice chuyển đổi. Phân loại helper thuần/static config hợp lệ. Chốt manual DI/container dựa trên code thật; ghi baseline và ngoại lệ. |
| D1 — Root và lifecycle | D0 | Root cho ProceduralGame, bridge/map/manager được inject, Ready gate rõ; adapter legacy cho scene chưa migrate. Không duplicate bridge/entity hoặc timer chạy sớm. Test thiếu dependency, Init lặp, enter/exit/reload scene và Play Mode khi domain reload bật/tắt. |
| D2 — Progression qua DI | D1 + S1/S2 | Được thực hiện ngay trong S1/S2: research adapter và UI nhận dependency tường minh; fake được ở test UI; runtime vẫn dùng ví ECS. Consumer mới không lookup global. Kiểm tra đóng/mở UI và unload không rò subscription. |
| D3 — Gameplay tích hợp qua DI | D2 + S3, hoàn tất cùng S6 | Migrate resource services, wave/day-night, build/train actions, factories/pool, procedural integration và callers liên quan. Kiểm tra spawn/register/remove và vòng đời pooled objects. Cập nhật mapping D0, không chỉ sửa class mới. |
| D4 — Hoàn tất phạm vi project | D3, trước S9 | Đi hết inventory còn lại: UI/input/camera/fog/minimap, menu, Unit adapters và scene tham chiếu/demo do dự án sở hữu. Xóa fallback/static access không còn cần; giữ ngoại lệ có giải thích. Quét lại và nghiệm thu chức năng. Không tuyên bố toàn project đạt DI khi vẫn còn consumer nghiệp vụ ngoài inventory. |

Mỗi D có thể chia a/b/c theo nhóm consumer để vừa một phiên; cập nhật Progress trước khi dừng. Không gom D4 thành một refactor không giới hạn: inventory D0 là danh sách hữu hạn và là nguồn chọn tác vụ tiếp.

## 7. Kiểm chứng và đo hiệu năng

- Structural audit: so sánh inventory trước/sau; mỗi lookup/global access còn lại phải được phân loại và liên kết tới ngoại lệ. Search là công cụ tìm ứng viên, không chứng minh tính đúng kiến trúc một mình.
- Unit tests: inject fake vào consumer và kiểm tra output/command thực tế; thiếu dependency phải fail rõ, Init không subscribe hai lần, Dispose/unbind không gọi dependency cũ.
- Integration: MainMenu → gameplay → menu → gameplay; SampleScene/SwarmSimulation; spawn/return/reuse pool; map Ready; resource deposit/build/train/research/wave.
- Performance: đo chi phí composition lúc khởi động và allocation/frame ở các flow vừa migrate nếu có thay đổi đường nóng; không Resolve/Find mỗi frame. Không hứa DI làm game nhanh hơn.
- Lưu Usage/Validation và Evidence cùng D1 trở đi; ghi phiên bản, scene, seed, test XML/log/ảnh. Chưa có số đo hoặc test DI đã chạy ở thời điểm viết tài liệu này.

## 8. Tài liệu khóa luận và giới hạn

ADR cần mô tả vấn đề phụ thuộc ẩn, lựa chọn manual DI, phương án container được cân nhắc, ownership/lifetime và tradeoff phải nối bằng code. Bổ sung sơ đồ dependency trước/sau của một flow research và một flow spawn, test thay dependency, bằng chứng scene reload và inventory ngoại lệ.

DI và Dependency Inversion Principle có liên quan nhưng không đồng nhất: truyền concrete dependency vẫn là DI; interface ở ranh giới nghiệp vụ giúp hướng phụ thuộc ổn định. Không dùng số lượng interface hay việc cài container làm tiêu chí hoàn thành khóa luận.

Giới hạn: ECS có mô hình truy cập dữ liệu riêng; Unity serialization không inject constructor cho MonoBehaviour; scene/prefab dùng chung và static bootstrap cũ có thể gây đăng ký trùng nếu chuyển đổi thiếu lifecycle. Vì vậy giữ baseline và migrate theo từng nhóm callers.

Nguồn nội bộ: [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md), [Gameplay Architecture](../DOTSGameplay/Architecture.md), [Procedural Architecture](../ProceduralGeneration/Architecture.md), các file nêu ở mục 2. Đây là quyết định thiết kế dự án, chưa phải kết quả kiểm chứng.
