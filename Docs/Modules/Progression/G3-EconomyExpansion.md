# G3 — Kinh tế, mở rộng và áp lực wave

**24/11 — cập nhật E8, manual gate vẫn OPEN:** người dùng xác nhận “Chưa thử, giữ nghiệm thu G3 mở”. [E8 report](../GameExperience/E8-ExpansionCommands.md) thêm preview/commands và đo lại controlledseed0/wave8001:Hall4chuyến/80G/19.9s,kho12chuyến/240G/.7s,cost100W100G→+60Gnet đổi100W; emptydepot0income/range14.13,cả3thuađêm4.89/89regression+movement/lifecyclePASS; không dùng dữ liệu này để thay nghiệm thu người chơi hoặc tuyên bố balance/mọiseedPASS. Các kết quả G3 cũ dưới đây giữ nguyên.

Ngày 21/11/2026. Đã chốt cấu hình chơi thử G3 với Hall 1200 HP và bán kính wave procedural lấy từ căn cứ đầu trận. Nghiệm thu của người chơi còn chờ. Tên run dùng UTC. [Kế hoạch](NextPhasePlan.md) · [shortlist G2](G2-PressurePilot.md) · [raw và fingerprint](Evidence/G3-2026-11-21/)

## Thiết kế đo

Unity MCP đã xác nhận đúng project RTSgame2026 và scene ProceduralGame sạch trước Play. Hall 1600 HP là điểm xuất phát. Control seed 0 thắng với 134 HP nên mức này trượt điều kiện control trên ba map. Candidate 1200 HP đã có trong shortlist G2 được thử trong bộ C/E/M/X với map seed 0, 1, 1543 và wave seed 8001. Giữ lịch ngày đêm, mục tiêu năm đêm, ví đầu trận và định nghĩa tech của G2 trong phép đo.

- C bỏ mặc với quân ban đầu và AI mặc định.
- E ưu tiên nâng kinh tế và thêm worker. M ưu tiên mở cung thủ, mua quân và giữ một nhóm worker nhỏ hơn.
- X giữ tiền cho kho landmark và duy trì phòng thủ. Đọc riêng thời điểm đặt móng, hoàn thành, lượng tài nguyên nộp vào kho và thời gian vận chuyển.
- EmptyDepot dùng lệnh xây và trả chi phí thật. Kho thiếu người vận chuyển và lực lượng tăng viện. Đọc bán kính và số quái tiếp cận để đối chiếu với C.

Harness tái sử dụng vòng tick native của G2, đổi HP trong phiên paused Play trước lúc bridge bind. Mỗi lượt kiểm MaxHealth, CurrentHealth và HP Entity khớp, đối chiếu tiền nộp và tiền chi W/G/M, rồi ghi outcome, attack sequence và thời điểm mở tech. `node Tools/SummarizeG3.mjs --compact` kiểm ledger từ raw.

## Kết quả trước khi sửa bán kính wave

Ở seed 0, X đặt móng kho lúc 30,05 giây. Bán kính TownHall dùng cho wave tăng từ 14,13 lên 54,80 world units. Kho chưa hoàn thành và nhận 0 tài nguyên, nhưng Hall giữ 1200/1200 HP trong khi C cùng mức HP thua. Số quái spawn vẫn đủ 76 ở X. Công trình được thêm vào danh sách của GameManager ngay lúc spawn view và bị tính vào bán kính ngay cả khi construction còn active.

Policy X đang có thể đổi builder sang khai thác khi worker có phase Constructing và node rỗng. Phép đo kho hoàn thành cần bảo vệ nhiệm vụ xây và trả builder về công trình sau trú đêm khi cần. Sau khi tách ảnh hưởng điểm spawn, dùng cùng mỏ vàng ở landmark để so thời gian chở về Hall và kho, lượng nộp và chi phí khóa 100 gỗ/100 vàng. Kết quả giữa E/M chỉ so hai gói chiến lược vì quân và worker khác nhau.

| Seed | C với 1200 HP | E, HP còn khi thắng | M, HP còn khi thắng | X, HP còn khi thắng |
|---:|---|---:|---:|---:|
| 0 | Thua đêm 4, 536,50 s | 1136 | 1136 | 1200 |
| 1 | Thua đêm 4, 516,90 s | 960 | 1098 | 970 |
| 1543 | Thua đêm 5, 647,90 s | 420 | 340 | 1002 |

Cả ba lượt X ở bảng này đều dừng ở móng và nhận 0 tài nguyên. Lượt riêng chỉ đặt kho ở seed 0 thắng với 386 HP, tổng nộp bằng 0, số lệnh rally bằng 0, chi 100 gỗ và 100 vàng. Cùng seed, C thua. Kết quả này xác nhận thay đổi điểm spawn có thể đủ để đảo kết quả trận.

E mở `economy_tools` ở 20,05 giây và `economy_cargo` trong 55,05–60,05 giây. M mở `military_archery` ở 25,05 giây, so với 85,05–90,05 giây của E. Ở seed 1543, E nộp 1260 gỗ và kết thúc với 9 quân, M nộp 660 gỗ và kết thúc với 11 quân. Hai hướng có lợi ích khác nhau trong bộ thử nên chưa có căn cứ chỉnh giá hoặc duration một nhánh.

## Giới hạn bán kính wave cho map procedural

`GameSceneCompositionRoot` gọi `CaptureProceduralStartingRange` sau khi bridge đăng ký các công trình đầu trận. `EnemyWaveManager` dùng giá trị đó cho các lần sinh, rút quân và hiển thị vòng ở map có gateway. Công trình xây sau và công trình ban đầu bị phá đều không dời vòng đã chốt. Cách tính cũ vẫn áp dụng ở scene không có gateway. G3-v3 khóa builder trong công việc xây, nối lại sau trú đêm khi cần và assert bán kính không đổi ngay sau khi đặt móng. Phần thu/nộp và lái quân vẫn dùng command hiện có.

Regression seed 0 với kho bỏ trống sau sửa giữ bán kính 14,13 và thua đêm 4 tại 537,15 giây sau 57 spawn. Hall gặp 26 quái đánh trực tiếp và kho nhận 0 tài nguyên. Trước sửa, chính policy này thắng đủ năm đêm và Hall còn 386 HP.

## Vận chuyển cùng mỏ, seed 0

Cặp G3-v4 dùng mỏ tại `(-15.52962, -59.20689)`, cùng slot worker, 1200 HP và wave seed 8001. Hai lượt chỉ ra lệnh khai thác cho worker đó. Lượt có kho thêm một builder và trả 100 gỗ/100 vàng. Không có research, huấn luyện hoặc rally. So trong cửa sổ chung đến 535 giây, trước thời điểm sớm nhất Hall bị phá. Mỗi chuyến ghi từ lúc đầy hàng và chuyển sang Depositing tới lúc ECS nộp hết cargo.

| Đích nộp | Số chuyến | Trung vị chở về đích | Vàng nộp | Thay đổi W/G sau chi phí xây |
|---|---:|---:|---:|---:|
| TownHall | 4 | 19,90 s | 80 | 0 / +80 |
| Kho landmark | 12 | 0,70 s | 240 | −100 / +140 |

Kho tăng 160 vàng nộp và tăng 60 vàng ròng khi so với Hall sau khi trừ giá vàng của kho. Chi phí 100 gỗ vẫn là đánh đổi. Cả hai lượt thua đêm 4, cho thấy vận chuyển cần đi cùng phòng thủ. Lượt logistics G3-v3 với Hall bị bỏ khỏi phép so vì harness cũng chi cho research và huấn luyện. File raw đó vẫn được giữ để truy vết.

## Mở rộng có phòng thủ sau sửa

| Seed | Kho hoàn thành | Hall cuối | Vàng nhận tại kho |
|---:|---:|---:|---:|
| 0 | 62,25 s | 1136 HP, thắng | 0 |
| 1 | 56,25 s | 1080 HP, thắng | 40 |
| 1543 | 62,55 s | 420 HP, thắng | 0 |

Seed 0 dùng G3-v4 và giữ worker vàng ở mỏ trong lúc worker gỗ xây. Lượt G3-v3 cùng seed dùng worker vàng làm builder, hết vàng cho huấn luyện và thua. Seed 1 và 1543 dùng policy G3-v3. Full matrix C/E/M trước sửa cùng 1200 HP xác nhận ba C thua và sáu E/M thắng. Sau sửa bán kính, C seed 0 lặp lại đúng thời điểm thua 536,50 giây của đối chứng. Các lượt X sau sửa đều spawn 76 quái. Phép đo vận chuyển riêng là căn cứ cho lợi ích khi bố trí đúng mỏ. Hai lượt X nộp 0 vàng vào kho cho thấy lợi ích phụ thuộc vị trí khai thác.

## Bản chơi thử và nghiệm thu còn lại

Hall trong `ProceduralGame` được author 1600 → 1200 qua `SerializedObject` và lưu scene trong Unity. Khi Unity serialize lại ba RectTransform và 18 tốc độ animation tile trong lúc lưu, các trường đó được phục hồi qua Unity API. Phép so byte với bản trước lưu kiểm riêng đúng giá trị HP. Bản này giữ lựa chọn tech và nhịp wave từ G2. Candidate 1200 qua điều kiện seed chuẩn trong mô phỏng, còn chờ phản hồi người chơi và đo với wave seed bổ sung cùng holdout.

Chơi từ `MainMenu` với seed mặc định 1. Thử hai hướng nâng kinh tế trước và mở quân sớm ở các trận riêng, rồi thử đặt kho gần mỏ vàng tại landmark. Ghi seed, thời điểm thiếu vàng, đêm phải đổi cách phòng thủ và lý do chọn kho. Seed 1543 là ca nên ưu tiên khi có thể đổi seed bằng Inspector. G3 được đánh dấu chờ manual trong build plan. G4 sẽ có dữ liệu profiler riêng.

## Tái lập và giới hạn

Mở `ProceduralGame` trong Unity và kiểm scene sạch bằng MCP. Vào paused Play. Trước mỗi lượt, chỉnh seed của `StrategicProceduralMapSettings` trong bộ nhớ và đăng ký callback `SceneManager.sceneLoaded` một lần. Callback đặt cả `m_Health` và `m_CurrentHealth` trên TownHall mới sau Awake và trước khi ECS bind. Reload `ProceduralGame`, gọi Step qua MCP và xác nhận root Ready. Gửi toàn bộ `Tools/PlaytestG3.cs` qua script-execute với class `G3Playtest` và method `Control`, `Economy`, `Military`, `Expansion`, `EmptyDepot`, `LogisticsHall` hoặc `LogisticsDepot`. Giá trị thử là 1200 HP, map seed 0, 1 hoặc 1543 và wave seed 8001. Chụp SHA-256 scene, source, harness và assets của lượt mới vào manifest. Các manifest và raw đã lưu đi cùng phiên bản harness trong evidence.

Khôi phục seed authoring về 1, thoát Play và kiểm lại scene sạch qua MCP. Kiểm 24 lượt raw, gồm cả lượt chẩn đoán và lượt loại khỏi đối chứng, bằng `node Tools/SummarizeG3.mjs --compact`. Chạy toàn bộ EditMode qua Unity MCP được 81/81 bài, không lỗi hoặc bỏ qua. Engine chạy tick tăng tốc, vì vậy raw dùng cho cân bằng và luồng ECS. FPS, frame time, Player Release và phản hồi người chơi được kiểm theo các gate riêng. [Trạng thái Unity và kết quả EditMode](Evidence/G3-2026-11-21/closure-state.json)

Nguồn tích hợp gồm `EnemyWaveManager.CalculateTownHallRange`, `ProceduralMapAuthoring.TryGetGatewayApproachPosition`, `BuildingProcess`, `GameplayEconomyJob.FindDepot` và [kiến trúc DOTS gameplay](../DOTSGameplay/Architecture.md).
