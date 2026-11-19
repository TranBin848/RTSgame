# Kế hoạch sau S9 — cân bằng, hiệu năng và bản nộp khóa luận

Ngày lập: **18/11/2026**. Chốt **19/11: G0–G2 DONE theo phạm vi từng gate**. [G01](G01-Validation.md) có baseline/policy12run; [G2](G2-PressurePilot.md) có9native+1realtime, shortlistHall1600 đã author riêng ProceduralGame. G3–G7 chưa thực hiện; chưa nghiệm thu cân bằng mọi seed hoặc bản Release.

Đây là đợt mới sau S0–S9/D0–D4 đã hoàn tất, không sửa lịch sử nghiệm thu cũ. Lượt lập kế hoạch chỉ đọc tài liệu/cấu hình/source và viết tài liệu; chưa đổi gameplay, chạy thử nghiệm, build hoặc thao tác Editor. Nguồn trạng thái hiện tại: [Progress](Progress.md), [S9 Acceptance](S9-Acceptance.md), [MatchFlow](../MatchFlow/Architecture.md), [D4 Validation](../DependencyInjection/D4-Validation.md).

## 1. Kết quả cần đạt và mặc định đề xuất

Ưu tiên một bản RTS sinh tồn ổn định, có lựa chọn đầu tư đáng giải thích và có bằng chứng phục vụ khóa luận. Giữ một trận **5 đêm, thắng ở đầu ngày 6**; thời lượng danh nghĩa 675 giây, chưa tính pause/thao tác menu. Độ khó đề xuất là Normal: người mới có thời gian chuẩn bị, nhưng bỏ mặc nền kinh tế và phòng thủ không nên tự thắng.

Thứ tự: **G0 chuẩn hóa baseline → G1 kiểm hiệu quả chiến đấu/policy → G2 áp lực sinh tồn → G3 giá trị tech/mở rộng → G4 profile spike → G5 Release/onscreen → G6 nghiệm thu bản nộp → G7 tài liệu/demo**. Không bắt đầu tune giá trước khi biết quân có thực sự tham chiến; không đo cải thiện hiệu năng bằng hai workload khác nhau.

Phạm vi mặc định chưa gồm prestige, offline income, save/load, tree mới, loại quân/tài nguyên mới, map streaming, thay render pipeline, đổi scripting backend hoặc nâng Editor/package. Không thêm chế độ khó, BalanceManager hoặc DI container chỉ để thử vài thông số.

## 2. Căn cứ đã có và điều chưa biết

| Bằng chứng hiện tại | Ý nghĩa cho kế hoạch |
|---|---|
| 81/81 EditMode và 18/18 Player smoke PASS | Giữ làm baseline hồi quy; đây không là chứng minh cân bằng tốt |
| Control seed 1 không thao tác vẫn thắng, hall còn 8190/10000 HP | Cần thay đổi áp lực hoặc hiệu quả phòng thủ; chưa đủ kết luận chỉ do HP |
| Sáu policy cũ đều thắng, hai nhánh trên cùng seed chỉ chênh HP hall 0–6 | Cần kiểm vị trí/engagement của lính và cách policy ra lệnh trước khi so giá tech |
| Policy cũ không đặt được kho vì không dành đồng thời 100W/100G | Chưa đo được lợi ích mở rộng; không coi là lỗi placement hoặc proof mở rộng vô dụng |
| Seed 1/military cũ không nộp thịt; control có field peak/expansion là default chưa đo | Chuẩn hóa phân công và telemetry; không giảm meat cost dựa trên số liệu thiếu |
| Scene ProceduralGame và SampleScene có override `m_Health=10000`, TownHall prefab gốc đọc được `m_Health=100` | Xác minh giá trị resolve trong Editor/Entity và nguồn override; không reset về 100 hoặc sửa prefab dùng chung một cách máy móc |
| S9: frame median 3.7341ms, p95 8.5298ms, max 53.9553ms; GC max 1020544B | Có spike cần attribution; median đẹp không thay cho việc tìm frame khựng |
| S9 là Development/offscreen/seed 1, một mẫu startup | Chưa biết Release/onscreen, overlay UI GPU, nhiều run và máy khác |

Các nhận định về nguyên nhân là **giả thuyết**, không phải bug đã xác nhận. Đặc biệt, lính mới có spawn/exit destination và acquisition theo detection range; policy mua quân rồi để ở nơi sinh không đại diện đầy đủ cho phòng thủ chủ động.

## 3. Tiêu chí đích đề xuất để người dùng duyệt

Các ngưỡng dưới đây là mục tiêu thiết kế ban đầu, được chốt lại ở G0/G1 trước khi dùng làm tiêu chí pass; chưa là kết quả đã đạt.

| Mục tiêu | Tiêu chí đề xuất |
|---|---|
| Không tự thắng | Control không ra lệnh không thắng trên cả 3 seed chuẩn ở wave seed chuẩn; kiểm lại với wave seed bổ sung sau khi chọn cấu hình |
| Có đường chơi hợp lý | Mỗi seed chuẩn có ít nhất một strategy chủ động thắng; mỗi hướng kinh tế/quân sự/mở rộng có ít nhất một run thắng hoặc lợi ích cụ thể đo được, không ép mọi policy thắng mọi seed |
| Mở đầu có thể học | Policy phòng thủ cơ bản không bị diệt trước khi có thời gian sản xuất/đặt quân; đêm 1 không là bẫy bất ngờ cho người mới |
| Đầu tư tạo khác biệt | So với đối chứng cùng điều kiện, có khác biệt về HP được bảo vệ, lượng quân hữu dụng, thời điểm unlock hoặc thu/nộp; không chỉ thay số tài nguyên cuối trận |
| Không có một thứ tự luôn tối ưu | Không chỉ ra được bằng bộ run nhỏ rằng một nhánh luôn hơn mọi nhánh; nếu một hướng thắng mọi tiêu chí ở cả 3 seed thì kiểm lại trước khi chốt |
| Mở rộng có đánh đổi | Kho/tháp hoặc tiếp cận vùng mới cải thiện chỉ số hữu ích, nhưng công trình bỏ trống không được biến thành mẹo làm quái không thể tới căn cứ |
| Bản nộp ổn định | Các regression liên quan, lifecycle/reset và Player smoke PASS; không có lỗi gameplay/runtime đã biết làm đứt vòng trận |
| Hiệu năng | Đề xuất nhắm trải nghiệm 60 FPS ở 1920×1080 trên máy hiện tại; 16.67ms là budget tham khảo, chưa phải cam kết. Báo đủ p95/p99/max và điều kiện vSync/cap, không chỉ FPS trung bình |

Không lấy tỷ lệ thắng của vài run script làm tỷ lệ thắng của người chơi. Nếu số đo và cảm nhận mâu thuẫn, giữ cả hai và điều tra thay vì tự bỏ mẫu xấu.

## 4. Quy tắc thí nghiệm và ranh giới kỹ thuật

- Mỗi candidate chỉ đổi **một nhóm nguyên nhân**: lỗi/command; HP; lịch/interval; giá/duration; hoặc bố trí tài nguyên. Luôn có đối chứng A/B trên cùng map seed, wave RNG seed, policy và phiên bản code.
- Khóa map seed `0, 1, 1543`. Tách wave RNG seed khỏi map seed trong harness; seed RNG chỉ phục vụ tái lập, không biến game bình thường thành replay cứng. Sau shortlist dùng thêm hai RNG seed và hai map holdout (đề xuất 137, 901), không tune tiếp trên holdout rồi gọi là kiểm độc lập.
- Không grant thêm tiền, đặt HP bất tử, rút timer hoặc spawn bù quái khi so balance. Native manual tick được phép tăng tốc kiểm luật nhưng không dùng tính FPS; candidate thắng/thua phải được đối chiếu ít nhất một trận realtime.
- Công trình/quân/research dùng command/factory thật. Raw spawn phải được register; cùng luật khả năng chi trả, training, tech gate, fog/path và death/reset. Nếu policy được biết tọa độ map ngoài fog để đơn giản hóa harness, ghi rõ, không coi đó là hành vi người chơi.
- Xác minh HP đồng bộ giữa authoring, Unit.MaxHealth/CurrentHealth và CombatBody. Không chỉ thay Health trên Entity rồi để UI/max HP hoặc producer dùng dữ liệu khác.
- Ưu tiên Definition/scene override sẵn có và snapshot giá trị trước thử. Nếu asset dùng chung với SampleScene, dùng candidate tạm hoặc bản sao qua Unity API để cô lập. Chỉ author cấu hình thắng khi được chốt; không hand-edit Unity YAML, reset cả scene hoặc git để khôi phục vài trường.
- Trước mỗi phiên thao tác: đọc TechnicalOverview/checkpoint, kiểm đúng Unity MCP instance, scene dirty/Play/compile. CLI dùng `--no-start` khi Editor người dùng đang mở. Không mở Editor thứ hai, kill Editor hoặc retry mutation chưa biết hiệu ứng.
- Runtime `RTS.DOTS` giữ độc lập với gameplay MonoBehaviour; adapter nhận dependency tường minh. Nếu cần instrumentation, dùng read-only counters hoặc diagnostic opt-in có giới hạn; không đưa LINQ/reflection/log mỗi tick vào đường gameplay chỉ để đo.

## 5. Các lát cắt triển khai

### G0 — Chốt baseline và hợp đồng đo

**Mục tiêu:** có thể lặp lại và khôi phục chính xác trước khi đổi số.

Việc làm sau khi được duyệt:

1. Kiểm MCP, đọc diff hiện tại và giữ thay đổi có sẵn. Ghi code/build fingerprint, seed, scene, version và cấu hình resolve thực tế.
2. Đối chiếu TownHall scene override/prefab/Entity; ghi tất cả công trình/quân ban đầu, giá/duration/multiplier 6 tech, unit cost/train time, wave và phase durations.
3. Audit harness cũ: sửa nhãn control “not attempted”/peak thực, xác định deposit/gather/cargo và gold/wood argument order, phân biệt phí research/train/build. Giữ raw cũ bất biến.
4. Chốt telemetry schema, strategy definitions, ngân sách candidate và ngưỡng đích trước khi thử; lưu snapshot cấu hình có cách restore từng trường.

**Phạm vi:** `Tools/PlaytestS8.cs` và harness phụ nếu cần; config snapshot/Docs. Chưa đổi gameplay mặc định.

**PASS:** có run ID/config ID/map+wave seed, baseline resolve nhất quán, ledger kiểm được `initial + deposit - spend = final` khi không có nguồn thu/chi khác; có recipe restore nếu phiên dừng/crash. Nếu không tái lập được baseline, dừng tune và sửa phương pháp đo trước.

### G1 — Kiểm phòng thủ thật và làm sạch so sánh strategy

**Mục tiêu:** xác định lính/tech có được dùng trong thí nghiệm hay chỉ được mua.

1. Theo dõi vị trí quân, mục tiêu, attack sequence/engagement, đường đi/detection và thời gian tới tuyến phòng thủ. Kiểm hiện tượng lính đứng ở spawn/exit, không thấy địch hoặc liên tục bị lệnh move ngắt combat.
2. Policy chủ động dùng chung một cách rally/phòng thủ cơ bản bằng command hiện hữu; ra lệnh cho quân mới hoặc lúc cần bố trí, không spam move mỗi 5 giây khiến suppress acquisition kéo dài. Control vẫn không ra lệnh.
3. Theo dõi lịch: configured quota → slot hợp lệ trước bình minh → spawn thành công → tới tuyến → đánh hall → retreat trước tiếp xúc. Không coi tăng quota sau mốc bình minh là tăng áp lực thực tế.
4. Kiểm worker phân công/carry/depot/path, nhất là case không có thịt ở seed 1. Ghi trường hợp không tìm thấy node/không tới nơi, không gọi đó là thiếu tài nguyên map khi chưa biết lý do.
5. Nếu có bug thật, sửa ở command/system/caller chung và thêm test nhỏ; không thay kinh tế để che bug. Nếu chỉ do harness, sửa harness và ghi rõ baseline phương pháp mới.

**Dữ liệu:** ưu tiên snapshot/counter hiện có; damage attribution chỉ thêm tại điểm áp damage nếu không thể kết luận bằng health delta/attack sequence. Ghi riêng “không đo được”, không bịa damage theo nguồn.

**PASS:** quân chủ động thực sự tham chiến trên 3 seed, policy/counters không tự phá lệnh; hai nhóm số liệu cũ/mới được phân biệt. Nếu quân chủ động vẫn không khác control, chưa được dùng HP để ép mọi bên cùng thua.

### G2 — Tạo áp lực sinh tồn, chọn shortlist cấu hình

**Mục tiêu:** chuẩn bị/phòng thủ có giá trị, giữ nhịp 5 đêm.

1. Bắt đầu với HP hall **hoặc** áp lực wave, chọn theo kết quả G1. Giá/multiplier tech, layout, clock60/30/45 và luật bình minh kết thúc tiếp viện giữ cố định trong vòng thử đó.
2. Không chốt trước giá trị HP mới từ chênh lệch 10000→8190. Giảm HP có thể làm cả econ/military cùng chết; tự đổi về prefab100 càng không có căn cứ. Ghi 2 candidate số cụ thể sau khi có damage/engagement baseline và lý do chọn.
3. Nếu thử wave, ưu tiên số địch thực sự tới giao tranh, khoảng nghỉ và interval trong 45 giây; tính lại slot hợp lệ/UI schedule. Không đẩy toàn bộ vào đêm1 hoặc lén kéo dài đêm.
4. Pilot trên seed1, cùng wave RNG, cả control/econ/military. Giữ candidate thắng hoặc quay về baseline; tối đa2candidate trong một vòng. Nếu đều hỏng, quay G1/giả thuyết, không thêm các knob mới liên tiếp.

**PASS shortlist:** control không còn tự thắng, ít nhất một cách phòng thủ chủ động vẫn thắng và không có death quá sớm vô lý. Candidate chỉ làm mọi policy thua hoặc khiến đêm1 áp đảo bị loại. Chưa chỉnh giá research trong cùng vòng.

### G3 — Cân kinh tế/tech/mở rộng và người chơi xác nhận

**Mục tiêu:** lựa chọn đầu tư có lợi ích, không bắt buộc “mua mọi node”.

1. Dùng cấu hình áp lực shortlist; so 3 strategy chủ động và control theo matrix bên dưới.
2. Strategy mở rộng phải **dành ngân sách** cho kho/tháp, có vị trí đặt hợp lệ và bảo vệ cơ bản; không dùng policy chi sạch vào lính rồi kết luận không đủ tiền mở rộng.
3. So cùng vùng khai thác gần/xa depot, thời gian đi-nộp và thu ròng sau chi phí xây. Không thêm cạn tài nguyên để ép mở rộng trong scope này.
4. Kiểm công trình ngoài căn cứ có làm spawn radius/arrival quá xa, không spawn được hoặc hết đêm trước khi tới. So mở rộng có bảo vệ và công trình bỏ trống. Nếu có exploit, sửa/ràng buộc riêng trước khi gọi đó là “mở rộng tốt”.
5. Chỉnh giá hoặc duration của **tối đa một nhánh mỗi vòng**, dựa vào thời điểm tích lũy, unlock, hàng chờ train và cơ hội bị bỏ lỡ. Chỉ đổi multiplier nếu giá/duration chưa giải quyết được vấn đề đã đo.
6. Ablation nếu cần: cùng phân công/quân/lệnh, bật-tắt một upgrade để xác định hiệu ứng của nó. Không suy “tech gây ra X” từ so hai strategy có số worker/quân khác nhau.
7. Freeze một config để user chơi; trong lúc user test không compile/reload/build trên Editor của họ. Nhận lỗi/cảm nhận, sửa có giới hạn rồi kiểm lại case liên quan.

**PASS:** đạt các tiêu chí mục3 đã chốt; có lý do chọn ít nhất hai hướng đầu tư và một lợi ích mở rộng đo được, không cần mọi tech bắt buộc. Có manual feedback cụ thể hoặc ghi mục chờ; không ghi chung “mọi thứ ổn” thành số ngày/tỷ lệ thắng chưa được báo.

### G4 — Profile frame/GC spike và tối ưu có bằng chứng

**Mục tiêu:** tìm nguồn spike, không chỉ làm benchmark bớt tải.

1. Dùng Development Player/Profiler cho attribution, giữ cùng config/seed/workload và thời điểm sự kiện. Phân biệt managed GC, native allocation, CPU main thread/jobs, GPU, shader/asset warmup, UI/layout, fog/nav và instantiate.
2. Đánh dấu timeline vào đêm/wave mới, train complete, deposit popup, UI đổi trạng thái và nav refresh; nối timestamp với frame spike. Capture ngắn có allocation call stacks khi cần; không dùng Deep Profile toàn trận để công bố FPS bình thường.
3. Tái lập tối đa3lần cho event nghi vấn. Nếu không lặp được, ghi “spike chưa attribution”, không chọn một subsystem làm thủ phạm theo suy đoán.
4. Chỉ sửa hotspot có bằng chứng. Tái sử dụng cache/pool hiện có nếu đúng lifetime; không tắt fog, UI hoặc renderer để làm số đẹp. Mỗi fix có kiểm output/lifecycle và trước-sau cùng workload.

**PASS:** đã tìm và giảm nguyên nhân có thể sửa trong scope, hoặc xác định giới hạn/ngoại sinh với bằng chứng và được chấp nhận. Báo median/p95/p99/max, các spike và allocation; không hứa0GC toàn game. Giữ kiểm0B steady research polling sau mọi thay đổi UI liên quan.

### G5 — Đo Release và onscreen đúng phạm vi

**Mục tiêu:** có số liệu gần bản nộp và biết giới hạn phần cứng.

1. Build Windows Release từ cấu hình đã freeze, không tự đổi backend/pipeline/Editor. Ghi build flags, source/config hash và machine spec.
   Nếu request MCP hết thời gian chờ, kiểm log/BuildReport/process trước; build đầu S9 từng mất hơn6phút dù request mất kết nối. Không gửi build thứ hai khi build đầu có thể còn chạy.
2. Chạy onscreen1920×1080, HUD/fog/camera/render thật; warmup30giây/capture90giây là điểm bắt đầu. Ghi vSync/cap; nếu cần bỏ cap để diagnostic phải tách lượt, không so lẫn với cap60 của chơi bình thường.
3. Đo ít nhất3run seed1 cho độ dao động, thêm seed/workload nặng nhất trong bộ balance; không tuyên bố FPS mọi seed từ một mẫu.
4. Đo startup request→Ready5lần cùng cách gọi, ghi lần cold riêng. Với5mẫu chỉ báo phân bố/median/min/max quan sát; không diễn giải p95startup như ước lượng quần thể đáng tin cậy.
5. Release thiếu Profiler counter thì ghi unavailable; attribution GC dùng Development cùng workload, không đổi -1 thành0. Data offscreen S9 giữ làm nhóm riêng, không gọi chênh lệch offscreen→onscreen là tối ưu code.
6. Nếu user có máy thứ hai, chạy smoke và một trận ngắn rồi ghi máy/resolution/settings. Nếu chưa có, ghi chưa kiểm; không tự hạ chuẩn test trên máy hiện tại.

**PASS:** raw data/điều kiện đo hợp lệ, smoke Release đạt và có kết luận so budget đã chọn. Nếu budget không đạt, quay G4 cho hotspot liên quan hoặc thương lượng setting/target, không tự hạ resolution/tắt hiệu ứng mà không ghi.

### G6 — Nghiệm thu bản nộp và đóng phiên bản

1. Regression phù hợp: giữ baseline81EditMode và18Player smoke, bổ sung case nhỏ cho code mới. Không chạy lặp toàn suite sau mỗi chỉnh số; chạy khi chốt candidate hoặc có code/bug liên quan.
2. Manual trên **đúng Release cuối**: menu/options/âm lượng; chọn/click/drag; idle focus; cursor; HUD toggle; minimap/fog; gather/deposit/build/train/research; ngày/đêm; victory/defeat; Escape sau kết thúc; replay/menu; cửa sổ/resolution dự kiến.
3. Chơi ít nhất một trận trọn vẹn trên Release; có thể dùng checklist nhiều phiên, không coi timers rút ngắn của smoke là full match.
4. Kiểm chạy không cần MCP/kết nối mạng cho gameplay, không phụ thuộc đường dẫn workspace; kiểm lỗi/credential hoặc dữ liệu phát triển trước khi chia sẻ log/package. Không sửa plugin bên thứ ba trừ khi thực tế có vấn đề được xác nhận.
5. Đóng gói đủ exe/Data/runtime đi kèm, kèm hướng dẫn chạy, mục tiêu5đêm và việc trận chưa có save. Lưu SHA-256 manifest và known issues, giữ bản S9 cũ. Chỉ commit/tag/push nếu được người dùng yêu cầu; không gom thay đổi ngoài scope.

**PASS:** build gắn với source/config/test cụ thể, không có lỗi chặn vòng trận, user nghiệm thu checklist cuối; artifact có thể chạy từ folder phân phối khác.

### G7 — Tài liệu và demo bảo vệ khóa luận

1. Cập nhật Architecture/Usage/Validation/Progress cùng implementation; giữ nguồn số liệu và trạng thái đã làm/đã đo/hướng phát triển tách biệt.
2. Chốt sơ đồ root→service/bridge→ECS→presentation; giải thích manual DI, World lifetime, pool/fog bind-unbind, research transaction và ngoại lệ còn lại.
3. Chuẩn bị bảng baseline/candidate và benchmark, thuật toán procedural/validation, lý do giữ/bỏ giả thuyết; nêu hạn chế dữ liệu nhỏ, policy tự động và offscreen cũ.
4. Kịch bản demo đề xuất8–10phút: mở menu/game, nhận diện vùng, worker/nộp, chọn một nhánh research, xây/train, phòng thủ một wave, cho xem kết thúc/replay bằng footage đã quay nếu không đủ thời gian chờ. Phân biệt gameplay tự nhiên với fixture tăng tốc, không dựng clip thành bằng chứng full match.
5. Checklist ngày bảo vệ: đúng build/hash, máy/resolution, âm thanh, điều khiển, bản dự phòng, dữ liệu/ảnh offline và known issues. Có thể chia demo thao tác ngắn và video trận đầy đủ riêng.

**PASS:** tài liệu dẫn được về code/evidence và người dùng có một kịch bản trình bày chạy thử được. Báo cáo/slides theo mẫu trường là phần cần user cung cấp mẫu nếu muốn tạo artifact riêng.

## 6. Bộ strategy và matrix kiểm thử

| ID | Định nghĩa | Giới hạn so sánh |
|---|---|---|
| C — control | Không research/train/build/move/gather theo lệnh; giữ đơn vị/công trình ban đầu và AI mặc định | Không gọi là “không có phòng thủ”: lính ban đầu vẫn có AI |
| E — economy | Tools→Cargo trước, chủ động tăng worker/phân công, sau đó quân sự; dùng phòng thủ cơ bản chung | So gói strategy; không cô lập hiệu ứng tech vì worker count khác |
| M — military | Archery/Training trước, ưu tiên quân, phân công đủ nguồn chi; phòng thủ cơ bản chung | Không được bỏ qua thiếu thịt hoặc để quân đứng xa rồi kết luận nhánh yếu |
| X — expansion | Dành ngân sách cho một kho hoặc vị trí tháp/vùng cần kiểm, giữ phòng thủ tối thiểu | Ghi riêng lợi ích logistics và lợi ích quân sự; không luôn gộp cả hai thay đổi |

Ngân sách đo được chia đợt để tiết kiệm credits; đây là số run dự kiến, không là cam kết thời gian hoặc mức tin cậy thống kê:

| Đợt | Matrix | Mục đích |
|---|---|---|
| Baseline G1 | 3map ×1wave RNG ×4policy =12run | Phân biệt bỏ mặc/phòng thủ/kinh tế/mở rộng; có thể bỏ run tương đương đã tái lập hợp lệ |
| Pilot G2 | Seed1 ×2candidate ×C/E/M =6run, đối chiếu baseline | Loại nhanh hướng sửa sai; tối đa2candidate/vòng |
| Shortlist G3 | 3map ×C/E/M/X =12run ở wave RNG chuẩn | Giữ/chọn một config; ghi candidate không làm được expansion |
| Xác nhận robustness | Config thắng,3map ×2wave RNG bổ sung ×3policy (C +2chủ động tốt nhất) =18run | Kiểm lệch theo hướng địch; tăng mẫu nếu kết quả sát ngưỡng/mâu thuẫn |
| Holdout | Map137/901 ×C +policy chủ động phù hợp =4run ban đầu | Bắt overfit3map; fail thì quay chẩn đoán, không giấu holdout |

Không chạy toàn matrix này cho mọi candidate. Dừng candidate khi có bug, metadata thiếu hoặc cả control/active đều sai tiêu chí; giữ raw fail. Số run có thể giảm nếu sửa chỉ là logging, tăng nếu lỗi ngẫu nhiên chưa đủ bằng chứng; phải ghi lý do.

Mỗi run lưu: run/config ID; source/build fingerprint; map/wave seed; strategy/order/rally rule; HP/config resolve; elapsed/day/phase/outcome; HP hall theo đêm; unit trained/alive và engagement; unlock start/finish/cancel; gather/deposit/cargo/spend theo nguồn; placement/budget/reason; quota/eligible/spawn/contact/retreat; lỗi và giới hạn telemetry. Không dùng field chưa đo hoặc giá trị mặc định như kết quả thực.

## 7. Khi nào cần người dùng test?

| Mốc | Yêu cầu người dùng | Thời lượng dự kiến, không bắt buộc cùng một buổi |
|---|---|---|
| Sau G1/G2 pilot | Chơi tới đêm1–2; nhận xét có thời gian chuẩn bị, quân phòng thủ có dễ điều khiển, bị ép vô lý không | Khoảng5–8phút |
| Sau G3 shortlist | Hai trận econ/military và một lần thử mở rộng, trải trên các seed chuẩn; báo lựa chọn, điểm chờ, lý do thắng/thua | Khoảng3trận ×11.25phút game time, cộng pause/retry; chia nhiều phiên |
| Sau G5/G6 | Checklist UI/lifecycle và một full match trên đúng Release cuối; máy thứ hai nếu có | Checklist5–10phút +một trận; có thể dùng trận G3 nếu đúng bản cuối và chứng minh không có thay đổi liên quan |
| G7 | Duyệt narrative/sơ đồ và diễn tập demo | Theo thời lượng bảo vệ của trường |

Mỗi đợt gửi user config/build/seed rõ ràng và câu hỏi cụ thể, không chỉ “test thấy ổn không”. Khi user đang chơi, không chiếm Editor để compile/reload. Không đòi user quay đủ mọi trận; chỉ cần dữ liệu báo cáo phù hợp, footage là tùy chọn cho demo.

## 8. Bàn giao qua nhiều phiên và rollback

Mỗi lát cắt kết thúc bằng checkpoint, kể cả PARTIAL:

```text
Ngày / G-stage / trạng thái: TODO | IN PROGRESS | PARTIAL | DONE
Mục tiêu, giả thuyết và tiêu chí đang kiểm:
Source/config/build ID; các giá trị baseline → candidate:
File/caller/scene/assets đã thay; dependency/lifetime liên quan:
Run ID + map/wave seed/policy; đã chạy / chưa chạy:
Kết quả, raw artifact, test fail và giới hạn:
Phản hồi user nguyên văn và điều có thể suy ra:
Cấu hình được giữ / bị loại và lý do:
Editor hiện tại; giá trị tạm đã restore hay còn pending:
Bước tiếp cụ thể và cách resume/rollback từng trường:
```

Implementation mới có tài liệu module nếu thay kiến trúc lớn; liên kết Docs/README. Candidate không đạt được revert **đúng phần của đợt**, giữ thay đổi người dùng và raw fail. Không dùng git reset/restore toàn project hoặc xóa log để trở lại baseline. Không nâng Editor/package/pipeline để giải quyết số đo chưa được điều tra.

Trình tự thực hiện sau khi duyệt: chỉ mở **G0–G1** trước, tạo checkpoint và báo kết quả; G2 trở đi theo evidence/gates. Phần triển khai được duyệt không tự bao gồm tính năng ngoài scope; mọi thay đổi mục tiêu5đêm hoặc thêm cơ chế mới phải được tách quyết định rõ.

## 9. Bảng trạng thái triển khai

| Lát cắt | Trạng thái | Bằng chứng mới |
|---|---|---|
| G0 Baseline/đo | DONE | Snapshot269hash, HP10000resolve,12ledgerPASS; recipe restore; [G01](G01-Validation.md) |
| G1 Engagement/policy | DONE | 12runv3, quân chủ động tham chiến3seed, worker role/rally/stall diagnostic; worker trigger fix+self-check |
| G2 Áp lực sinh tồn | DONE — shortlist | [9native+1realtime](G2-PressurePilot.md); C1600 thuađêm4, E/M thắng; realtimeE thắng/Hall1338. Author1600 chỉProceduralGame; bước tiếpG3 |
| G3 Tech/mở rộng/manual | TODO | Chưa chạy |
| G4 Spike attribution | TODO | Chưa profile thêm |
| G5 Release/onscreen | TODO | Chưa build/đo |
| G6 Bản nộp | TODO | Chưa chốt release candidate |
| G7 Demo/tài liệu bảo vệ | TODO | Chưa tạo artifact theo mẫu trường |

Nguồn đối chiếu: báo cáo S9/MatchFlow/raw control và source hiện tại của EnemyWaveDefinition, EconomyTools.asset, scene overrides, ProductionBuilding/GameplayGameBridge/GameplayActorJobs, D4 lifetime contract. Các bảng mục2 giữ số liệu trước G0–G1; kết quả đợt mới được ghi riêng trong báo cáoG01, không ghi đè raw lịch sử.
