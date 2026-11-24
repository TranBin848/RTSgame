# Kế hoạch triển khai hệ thống lưu và tải trận đấu

Ngày lập **24/11/2026**. Trạng thái **IN PROGRESS**. [Checkpoint implementation](Progress.md): SL0–SL4 đã triển khai trong phạm vi từng gate; SL4 map/entity hydrate/remap và Windows cold-process0/1/1543 PASS cho first-day/idle-work. SL5–SL9 chưa đạt, chưa có restore toàn trận hoặc Continue UI. Các mục bên dưới là kế hoạch/gate, không phải kết quả đã hoàn thành.

## 1. Mục tiêu và phạm vi phiên bản đầu

Cho phép người chơi lưu trận `ProceduralGame`, thoát ứng dụng và tiếp tục từ trạng thái đã lưu. Bao gồm map procedural, kinh tế, công nghệ, đơn vị/công trình, giao tranh đang diễn ra, clock/wave và thông tin khám phá. Đây là nhiệm vụ Lưu/Tải của đề cương KLTN; xuất CSV/JSON profiling, tạo lại map theo seed và tải scene mới không đáp ứng nhiệm vụ này.

Mặc định triển khai:

- JSON UTF-8 có `schemaVersion`; DTO thuần dữ liệu, arrays/lists và field tường minh, tận dụng serializer sẵn có phù hợp thay vì thêm package trước khi cần. Không serialize nguyên World/GameObject hoặc dùng BinaryFormatter.
- Một slot lưu thủ công và một slot autosave bình minh; mỗi slot có thế hệ backup. Không có trình quản lý hàng chục slot trong bản đầu.
- Lưu ở thời điểm tùy ý khi trận đang chạy hoặc pause, kể cả đang nghiên cứu, xây, train, khai thác hoặc giao tranh. Yêu cầu được thực hiện tại một ranh giới simulation an toàn; không cắt giữa job hoặc giữa damage/completion đang áp dụng.
- Tiếp tục từ MainMenu, lưu/tải từ pause menu. Phạm vi đầu tiên là một gameplay scene hoạt động; không áp Save/Load vào SampleScene/SwarmSimulation chưa có hợp đồng khôi phục.
- Chỉ nhận save tương thích schema và content/config của bản hiện hành. Save khác phiên bản bị từ chối rõ ràng; migration thực sự là hạng mục sau, không tự đoán hoặc thay config để đọc được.
- Sau tải thành công giữ trận **pause**, hiển thị trạng thái đã tải và chờ Tiếp tục. Thời gian ngoài ứng dụng không làm tăng research, wave hoặc kinh tế.

Không thêm cloud save, chống sửa save, mã hóa, prestige, offline income, networking hoặc thay đổi balance. File có checksum dùng phát hiện hỏng, không được quảng bá như chống gian lận. Không sửa báo cáo giữa kỳ thành “đã hoàn thành Save/Load” trước khi gate cuối đạt.

## 2. Căn cứ source và các điểm dễ mất dữ liệu

| Hiện trạng | Rủi ro khi chỉ serialize component | Hướng xử lý |
|---|---|---|
| Wallet/research/actor/worker/construction/train ở ECS | Đọc view có thể nhận snapshot cũ; Entity không bền qua phiên | Capture từ ECS sau Complete; remap bằng SaveId |
| Quái và đạn native không có GameObject tương ứng | Duyệt Hierarchy bỏ sót toàn bộ nhóm này | Capture query/native state của Swarm, tạo lại theo catalog |
| Bridge có `m_TrainingViews`, `m_ConstructionViews`, `m_NextRequestId` | Buffer train chỉ có RequestId/Duration, không đủ biết prefab/action cần sinh | Lưu định danh producer/action/prefab và dựng lại registry/request context |
| Clock và wave có timer/counter managed | Load scene mặc định có thể reset ngày/wave hoặc spawn lại phần đã sinh | Thêm capture/restore tường minh cho clock/wave, không dùng reflection runtime |
| `BeginGameplay` cấp 100 mỗi tài nguyên | Load có thể cộng tiền đầu trận lần hai | Nhánh NewGame/LoadGame riêng trong bootstrap |
| Resource claims/nav được cập nhật qua bridge | Khôi phục sai thứ tự gây worker kẹt hoặc node vượt capacity | Dựng resource/structures trước, nối worker sau; đối soát claims |
| E7 có báo cáo đêm và ledger; E8 có recall/resume work | Chỉ lưu worker task hiện tại làm mất công việc bị tạm dừng hoặc báo cáo đã chốt | Inventory các state bổ sung và khôi phục cùng snapshot |
| Presentation có fog, audio, VFX, cache và event listeners | Restore có thể phát lại completion/reward hoặc lộ địch ngoài fog | Hydrate state im lặng, tái tạo cache; không replay historical events |

Nguồn đối chiếu: `GameplayComponents.cs`, `SwarmComponents.cs`, `GameplayResearch.cs`, `MatchStatistics.cs`, `GameplayGameBridge.cs`, `GameSceneCompositionRoot.cs`, `DayNightCycleManager.cs`, `EnemyWaveManager.cs`, fog và các module E7/E8. Inventory tại SL0 phải kiểm cả partial files và buffers thực tế, không dùng bảng trên như danh sách trường đã đầy đủ.

## 3. Dữ liệu của một snapshot

| Nhóm | Nội dung phải khôi phục | Dữ liệu tái tạo hoặc không đưa vào file |
|---|---|---|
| Header | Schema, save ID, thời điểm UTC, content/config fingerprint, payload size/checksum, slot generation | Đường dẫn máy phát triển, tên user hệ điều hành |
| Catalog/config | ID prefab/action/visual/technology; fingerprint catalog, map settings, research, clock và tactical plan | Asset instance, GUID runtime ngẫu nhiên, đường dẫn tùy ý do file chỉ định |
| Map | Kích thước/origin/cell size, seed, generator version, settings, terrain cuối, Reserved, regions/routes/gateways/landmark; identity tài nguyên | Tilemap renderer, decor/ambient có thể tái tạo |
| Người chơi | Ví, UnlockMask theo mapping catalog đã xác minh, active tech ID, elapsed, Hall nghiên cứu | Ví/timer thứ hai trên UI |
| Actor và công trình | SaveId, loại, faction, vị trí body/offset cần thiết, máu, targetable, state/task, cooldown/windup, mục tiêu, đích; công trình đã xây thêm hoặc đã mất | Animator instance, collider blob pointer, GetInstanceID |
| Worker/resource | Node/depot/structure/shelter, cargo đúng loại/lượng, phase/timer, trạng thái recall/suspended work; resource sống/chết, cừu/xác có thể lấy thịt | Không tự thêm trữ lượng hữu hạn cho cây/mỏ |
| Xây và train | Elapsed/duration/stage, builder, ordered FIFO, loại action/prefab, giá đã thanh toán, request context và next ID | Gọi lại lệnh mua để tạo queue, debit thêm lần nữa |
| Quái và đạn | Loại/visual, HP, state, target/cooldown; đạn owner/target khi có, vị trí trước/hiện tại, hướng/quỹ đạo, thời gian sống, damage snapshot, các trường cần tiếp tục va chạm | Chuyển native enemy sang MonoBehaviour để dễ lưu |
| Clock/wave | Ngày/pha/elapsed, wave index, started/spawned counts, spawn/delay timer, approach/gateway và bán kính căn cứ đầu trận, random state/counter có liên quan | Reset lại lịch tại đầu đêm hoặc dùng thời gian hệ thống để bù |
| Fog và thống kê | Explored mask, ledger tích lũy, baseline của chu kỳ, báo cáo đêm đã chốt, cờ report/signal đã tiêu thụ; mục tiêu trận | Visible mask tính lại từ đơn vị; không tự reveal toàn map |
| Trình bày tiện ích | Camera center/zoom; danh sách selection bằng ID nếu còn hợp lệ | Tree pan/tooltip, VFX đang nổ, tiếng chuông đang phát, pool trống, texture runtime |

**Map:** phiên bản đầu lưu dữ liệu map logic cuối cùng, không chỉ seed. Seed/settings/version vẫn được giữ để tái lập và đối chiếu, nhưng không phụ thuộc thuật toán mới tạo lại map cũ giống hoàn toàn. Terrain snapshot chỉ trở thành nguồn scene khi validation thành công. Bảo toàn identity của resource đã mất/đã đổi, tránh generator tự sinh lại sau load.

**Đường đi và RNG:** flow field, spatial hash, collider cache và A* scratch là dữ liệu dẫn xuất, được rebuild. Destination/work/target phải giữ; đường đi có thể tính lại theo grid khôi phục, vì vậy không cam kết replay từng frame giống bit. SL0 phải liệt kê RNG/timers ảnh hưởng tương lai, kể cả actor/cừu/native Swarm và wave; capture state/counter hoặc công bố phần không thể tái lập trước khi nghiệm thu. Không lưu/ghi đè UnityEngine.Random toàn cục chỉ vì thuận tiện.

## 4. Định danh bền vững và registry

- Mỗi thực thể logic cần lưu có `SaveId` số nguyên 64 bit dương, duy nhất trong trận. Snapshot giữ `nextSaveId`; không tái sử dụng ID của object đã chết. Validate overflow/duplicate trước nhận save.
- Cùng một cừu vừa là actor vừa là nguồn thịt dùng một identity với tập capability tương ứng; không hydrate hai đối tượng chồng nhau. Công trình đang xây là một object, không lưu thành hai bản authoring và construction.
- Entity handle, `CombatBody.ExternalId`, GetInstanceID, request/UI handle và BlobAssetReference không mang thẳng qua phiên. Restore dựng mapping `SaveId → Entity/view` và cấp lại handle runtime cần thiết.
- Tách **identity instance** khỏi **definition ID**. Catalog authoring ánh xạ các ID được cho phép sang prefab/action/visual. File save không được yêu cầu tải asset/type hoặc thực thi mã bất kỳ.
- Prerequisite/unlock nhận diện theo tech ID, sau đó chuyển sang bit index của catalog đã xác minh. Không tin một UnlockMask khi catalog đã đổi thứ tự.

## 5. Ranh giới kiến trúc dự kiến

Core SaveLoad giữ DTO, validation, version, codec và storage; không tham chiếu scene gameplay. Adapter Integration điều phối capture/restore Unity/ECS/managed state. `RTS.DOTS` cung cấp API snapshot/import thuần dữ liệu tại ranh giới system; không tham chiếu GameObject, JSON file path hoặc UI. Chọn vị trí assembly cụ thể ở SL0 theo dependency thực tế, không tạo package/assembly mới nếu chưa có lý do.

Một service Save/Load được composition root/bootstrap cấp cho menu, nhận các dependency cụ thể. UI không Find bridge, không tự serialize World. Storage thao tác dưới `Application.persistentDataPath/Saves/`; path được service xác định, không nối đường dẫn tùy ý từ payload.

Trạng thái operation: `Idle → Capturing/Reading → Writing/Restoring → Idle` hoặc `Failed`. Chỉ một mutation Save/Load/NewGame tại một thời điểm. Hai lần click không tạo writer/restore song song. Service báo phase/progress/error cho UI; đời sống operation khi chuyển scene/quit phải có owner rõ ràng, không để Task chạy sau khi dependency bị hủy.

## 6. Luồng lưu

1. Nhận yêu cầu và xác minh trận Ready, không đang load/unload, không terminal hoặc unsupported scene. Có thể lưu khi pause; giữ trạng thái pause trước thao tác.
2. Đặt một simulation/input fence chung: chặn command mới, clock, wave và cả hai simulation system, không chỉ đặt `Time.timeScale = 0` rồi giả định mọi writer dừng.
3. Hoàn tất job dependencies của Gameplay và Swarm tại cuối ranh giới tick. Áp hết damage/completion đã commit; xử lý event queue theo hợp đồng: event ảnh hưởng gameplay phải được áp đúng một lần trước snapshot hoặc được lưu để tiếp tục đúng một lần. Không xóa queue để che sự không nhất quán.
4. Capture bản sao DTO bất biến trên main thread từ authoritative state và managed owners. Đối soát ví/IDs/claims/queue/clock; không cầm NativeArray/EntityManager sang thread I/O.
5. Giải phóng fence, khôi phục pause/input trước đó sau khi capture xong. Encode/write chỉ dùng bản sao DTO. Nếu serializer được chọn chưa thread-safe, giữ encode ở thread phù hợp và đo stall; worker chỉ dùng thư viện đã xác minh.
6. Ghi file tạm cùng filesystem, flush/close, kiểm lại format/checksum rồi commit bằng primitive replace/rename đã kiểm trên Windows target. Giữ thế hệ tốt trước làm backup. Không xóa file chính trước khi file mới có thể thay an toàn.
7. Chỉ báo “Đã lưu” sau commit thành công. File tạm hỏng/hết dung lượng/không có quyền không được phá thế hệ cũ. Lưu timestamp là thời điểm capture, không phải thời điểm I/O kết thúc.

## 7. Luồng tải và khôi phục khi lỗi

1. Read với giới hạn file size, độ sâu, số thực thể, dimensions và buffer length. Decode vào DTO trước, xác minh schema/checksum/catalog và mọi ID/tham chiếu/numeric range/NaN. Không thay trận đang chơi trong giai đoạn này.
2. Preflight asset resolver và cấu hình runtime. Nếu đang có trận, tạo checkpoint rollback trong bộ nhớ ở ranh giới an toàn trước khi tháo scene. Chốt nội dung rollback bao gồm map, clock, wave, reports và UI owner; không chỉ backup wallet.
3. Nhánh bootstrap `LoadGame` tắt NewGame seeding/spawners, tiền khởi đầu, auto phase callbacks và mọi completion/reward. World mặc định có thể tồn tại qua scene; chỉ dọn thực thể do trận sở hữu, không dispose World của Unity hoặc tạo simulation cạnh tranh.
4. Áp map snapshot, tạo resource và công trình trước, dựng collider/nav theo cùng origin/body coordinates. Tạo toàn bộ actor, native enemy, projectile và lập bảng SaveId.
5. Lượt thứ hai nối target/depot/node/builder/shelter/owner, khôi phục worker phase, construction, FIFO/request registry, research, clock/wave/RNG và ledger. Dùng API hydrate chuyên biệt đã validate; không replay command mua/xây/train để trả tiền lại.
6. Rebuild claims và derived caches theo policy đã chốt, so với dữ liệu kiểm chứng; rebuild navigation/flow fields/path requests. Khôi phục explored trước, tính visible từ actor; tái tạo UI queue/report/lighting theo state nhưng không phát lại sự kiện lịch sử.
7. Đối soát snapshot sau hydrate: số lượng theo loại, IDs, ví, cargo, queue, unlock, ngày/pha, wave, reports và quan hệ. Chỉ đặt Ready khi mọi invariant đạt; giữ pause chờ người chơi Tiếp tục.
8. Nếu lỗi apply sau khi teardown, dọn phần hydrate, khôi phục checkpoint cũ và trạng thái pause trước đó. Nếu rollback cũng thất bại, giữ simulation bị chặn, đưa về màn hình lỗi/menu có hành động rõ ràng; không resume một world khôi phục một nửa. Save file trên đĩa vẫn nguyên.

**Backup:** nếu file chính hỏng, chỉ thử một backup đã xác minh, thông báo dùng bản lưu cũ và thời điểm của nó. Không ghi đè bản hỏng hoặc backup trong quá trình đọc. Nếu cả hai hỏng, giữ nguyên file cho chẩn đoán và cho phép NewGame riêng.

## 8. Các mốc triển khai

| Mốc | Công việc | Gate nghiệm thu |
|---|---|---|
| SL0 | Inventory state/owner/partial buffers, dependencies và quy tắc lưu; chốt schema/ID/catalog/fence/rollback | Có field coverage matrix; mọi state quan trọng có owner và policy, không còn “chỉ lưu seed” |
| SL1 | SaveId, catalog definition và DTO/validator/codec | Round-trip DTO; duplicate/unknown ID, NaN, size/count vượt giới hạn, schema/config sai bị từ chối |
| SL2 | Slot storage, temp/backup/checksum, operation lock | Ghi/đọc file thật; fault injection từng pha commit; thế hệ đã lưu không mất khi write lỗi |
| SL3 | Capture fence nhất quán và snapshot toàn trận | Snapshot không thay gameplay state/tiền; job/clock/wave dừng cùng boundary; capture lặp không double events |
| SL4 | Bootstrap LoadGame, map/entity hydrate và remap hai lượt | Fresh scene/process khôi phục map/IDs/actors/native enemy; không cộng 100 tài nguyên hoặc sinh resource hai lần |
| SL5 | Worker/cargo/shelter, construction/FIFO, research và projectile | Tiếp tục đúng tiến độ, đúng prefab/action; không mất hàng, nhân quân, debit/reward trùng hoặc reset đạn |
| SL6 | Clock/wave/RNG, fog, reports, derived caches và rollback | Save giữa wave/dusk/trú đêm tiếp tục đúng; không spawn lại lượt cũ, lộ map hoặc cộng lại báo cáo; rollback PASS |
| SL7 | Save/Load/Continue UI, autosave dawn và quit/lifecycle | Click thật, pause, lỗi rõ ràng, không writer trùng; lần đầu không save thì Continue disabled; thoát/mở ứng dụng tiếp tục được |
| SL8 | Ma trận hồi quy, file lỗi/version/cấu hình, đo save/load | Field equivalence và fault matrix PASS, phạm vi không hỗ trợ ghi rõ; có số đo stall/size/time thực tế |
| SL9 | Windows Release, nghiệm thu người chơi và cập nhật báo cáo | Cold-process Continue, trận có research/build/train/combat, corruption recovery, artifacts/hash/docs/ảnh/video đầy đủ |

Trạng thái từng mốc cập nhật ở [Progress](Progress.md), kiểm chứng ở [Validation](Validation.md). SL0–SL4 DONE phạm vi từng lát cắt, SL4 có kiểm Windows process mới; SL5–SL9 TODO. Các mốc chỉ DONE khi đạt gate; SL1–SL4 là lát cắt kỹ thuật, không phải bản Save/Load hoàn chỉnh để nghiệm thu đề cương.

### Thứ tự đi sâu ở SL5

Kiểm riêng lần lượt worker đang mang hàng/nộp kho → worker đang xây → queue train nhiều loại → research đang chạy → worker Returning/Sheltered/resume-work → combat windup → đạn đang bay. Các completion phải đi qua đúng đường apply hiện hành sau restore, đồng thời không replay phần đã hoàn tất trước capture. Không mở rộng toàn bộ cùng lúc rồi chỉ thử save lúc idle.

### Autosave và trạng thái đặc biệt

Autosave tại điểm bình minh sau khi wave retreat, worker daybreak và report cycle đã xử lý xong theo hợp đồng; một checkpoint mỗi chuyển đêm→ngày, không dựa vào listener vô tình chạy lại. Nếu save thủ công đang I/O, gộp/hoãn một yêu cầu autosave, không đẩy queue vô hạn. Bản đầu từ chối tạo checkpoint terminal và không ghi đè save sống cuối khi defeat; NewGame không tự xóa slot cũ. Settings âm lượng không gộp vào snapshot trận; camera/selection khôi phục có kiểm giới hạn và ID.

## 9. Ma trận kiểm thử tối thiểu

| Nhóm | Ca bắt buộc |
|---|---|
| Map | Seed 0/1/1543; công trình mới; cây/mỏ/cừu bị đổi/xóa; origin và geometry khớp; không nhân resource |
| Economy | Worker idle/gather/deposit, cargo từng loại, nhiều worker một node, depot bị mất, claims đầy, cừu chết còn thịt |
| Xây/train | Công trình từng stage, builder đi tới/đang xây/đã chết; FIFO nhiều loại, hàng đợi rỗng, gần completion; request ID không trùng |
| Research | Trước/sau unlock; Tools/Cargo/Training/Fortify; đang research; Hall mất; queue/đạn trước nâng cấp giữ snapshot cũ |
| Combat | Quái native/ranged/retreat, target mất, windup/cooldown, projectile giữa quỹ đạo và gần impact; damage không trùng |
| Clock/wave | Giữa Day/Dusk/Night, ngay sát đổi pha, giữa spawn interval và delay, đã spawn một phần; bán kính/gateway và wave counters không reset |
| Fog/reports | Unexplored/explored/visible, dân trong Hall, night snapshot đã chốt, report đang mở; không phát chuông hoặc reward lại |
| An toàn file | Không file, truncated/empty, checksum sai, backup hỏng, schema/config sai, unknown prefab, duplicate/dangling IDs, số âm/NaN/quá lớn, hết dung lượng/quyền ghi, lỗi commit |
| Lifecycle | Save/load lặp 10 chu kỳ trong disposable match, menu→Continue, load khi pause, double click, scene unload, quit giữa write; không nhân entity/listener/material/native allocations |
| Liên phiên | Đóng Player hoàn toàn, mở process mới và Continue; file chính hỏng dùng backup có thông báo; load lỗi không phá trận hiện hành |

Định nghĩa tương đương: so dữ liệu gameplay canonical trước lưu và sau hydrate ở cùng boundary, remap tham chiếu theo SaveId. Float dùng tolerance quy định cho từng đại lượng; giá trị rời rạc/ví/ID/queue phải khớp chính xác. Chỉ loại trừ cache/VFX/waypoints đã công bố là dẫn xuất, không loại trường đang sai để làm test PASS.

Sau resume, kiểm completion đúng một lần, income/cargo liên tục, time không nhảy và lệnh mới được xử lý. Không đòi hai trận combat tiếp tục bit-identical nếu path/job scheduling không có hợp đồng replay; dùng đối chứng kiểm invariants và scenario kiểm soát để xác nhận không mất tiến trình. Tách test tăng tốc khỏi lượt chơi tự nhiên và frame performance.

## 10. Đo hiệu năng và bằng chứng

Đo riêng: thời gian freeze/Complete/capture main thread, encode, write/flush/commit, read/decode/validate, scene/map hydrate, entity remap, cache rebuild và tổng Continue→Ready. Ghi file size, số actor/enemy/projectile/resource/queue, nguồn sáng, seed, build/hash và phần cứng. Thời gian I/O bất đồng bộ không được gọi là thời gian đứng hình.

SL0–SL3 lấy baseline trước khi đặt budget cứng; mục tiêu ban đầu là tránh hitch kéo dài hoặc tăng bộ nhớ không giới hạn, UI phản hồi ngay và phục hồi đúng khi lỗi. Đề xuất đo ít nhất 5 save/load cho một state nhỏ và một state đang giao tranh trên máy baseline, báo median/p95/max cùng số mẫu; không suy 5 mẫu thành bảo đảm latency tuyệt đối. Đo Release onscreen; GC không có counter phải ghi unavailable. Tải đạn cần fixture riêng vì stress E10 trước đó có peak projectile = 0.

Tại mỗi mốc ghi `SL<n>-Report.md` với mục tiêu, schema/API/file sửa, thuật toán, cách chạy, kết quả, giới hạn. Evidence đặt `Docs/Modules/SaveLoad/Evidence/SL<n>-<ngày>/`; giữ sample save nhân tạo không có dữ liệu cá nhân, field coverage/diff, fault logs, test XML/JSON, screenshot/video và fingerprints. Build/cache/file save lớn giữ ngoài Docs.

Chạy regression phù hợp module thay đổi; kết quả 89/89 E10 là baseline lịch sử, không thay kiểm thử sau import/export mới. Native memory, registry và scene-owned objects phải được kiểm sau load lặp, không kết luận hết leak chỉ từ một lần memory giảm.

## 11. Rủi ro và giới hạn cần ghi rõ

1. **Bỏ sót managed registry:** ưu tiên inventory queue/action/prefab và restore bridge trước khi UI hóa chức năng.
2. **Startup callback tác động kép:** có load mode trước Awake/Start của generator/root/clock, hydrate im lặng rồi publish trạng thái đúng một lần; kiểm gain tiền, wave và research completion.
3. **Dữ liệu không nhất quán:** chỉ capture ở fence thống nhất; checksum không thay thế semantic validation.
4. **Runtime load thất bại:** rollback được kiểm bằng fault injection; không chỉ dựa try/catch rồi tiếp tục trận hỏng.
5. **Phiên bản nội dung thay đổi:** bản đầu từ chối incompatible content rõ ràng; hash phải biểu diễn cấu hình liên quan, không dùng số version hiển thị duy nhất hoặc git HEAD của working tree chưa commit.
6. **Độ chính xác và điều hướng:** bảo toàn vị trí/cargo/task/target, rebuild cache; nếu actor phải resolve sang ô khác phải báo trong validation và có tolerance đã chốt, không âm thầm teleport để vượt test.

## 12. Tích hợp tài liệu và nguồn

Khi bắt đầu implementation, bổ sung `Architecture.md`, `Usage.md`, `Validation.md` trong module này và liên kết Docs README. Cuối SL9 cập nhật báo cáo giữa kỳ/khóa luận: dữ liệu đã lưu, thuật toán remap, xử lý file lỗi, kết quả round-trip và đo lường mới; phân biệt rõ Save/Load với telemetry và các hạng mục đề cương khác chưa hoàn tất.

Tài liệu nền: [Tech Overview](../../TechnicalOverview.md), [DOTS Development Guide](../DOTS/DevelopmentGuide.md), [Gameplay Architecture](../DOTSGameplay/Architecture.md), [DI](../DependencyInjection/Architecture.md), [Progression](../Progression/Architecture.md), [MatchFlow](../MatchFlow/Architecture.md), [E7](../GameExperience/E7-DawnReports.md), [E8](../GameExperience/E8-ExpansionCommands.md), [E9](../GameExperience/E9-TacticalNights.md), [E10](../GameExperience/E10-ReleaseHandoff.md).

Trước viết codec/storage thực tế phải kiểm API serializer, `persistentDataPath` và primitive ghi thay file trên Unity/Windows hiện hành bằng tài liệu chính thức. Lượt lập kế hoạch này chưa thực hiện phép thử tính nguyên tử hoặc durability trên filesystem, nên không ghi các thuộc tính đó như kết quả đã xác nhận.
