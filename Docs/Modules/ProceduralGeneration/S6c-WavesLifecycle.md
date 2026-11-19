# S6c — Wave, áp lực tiếp cận và lifecycle trận

Ngày **13/11/2026**, Unity **6000.3.11f1**. Tiếp [S6b](S6b-EconomyResourceDI.md). Wave/lifecycle API checks đã đạt theo phạm vi dưới đây; **S6/D3 tổng thể vẫn PARTIAL**, chưa chuyển S7 khi D3 và phần nghiệm thu hình ảnh/input còn dở.

## Mục tiêu và kết quả thay đổi

Kiểm lịch đêm45giây với3wave (3/4/5quái), spawn theo hai hướng strategic, retreat và tải trận mới. Giữ nguyên quota, enemy prefab/speed, interval2giây, delay10giây và thời lượng đêm. Không tăng quái theo diện tích đất hoặc đổi balance asset.

Đã sửa ba điểm từ bằng chứng Unity:

1. Timer spawn cuối wave không được xóa trước khoảng nghỉ, khiến wave2/3 trễ hơn mốc UI khoảng2/4giây. `EnemyWaveManager` đặt `m_SpawnTimer=0` tại lúc hoàn tất wave. Mốc trước sửa0,02/16,10/34,20giây; sau sửa0,02/14,10/30,20giây, khớp lịch UI0/14/30 trong sai số frame<0,25giây ở phép thử.
2. Spawn sát biên strategic làm thời gian đi quá dài so legacy. Seed1 cùng lịch đã sửa: chỉ6/12quái tới phạm vi10 quanh TownHall trước sáng, so legacy11/12; quái đầu tới22,08giây so5,36giây. Điểm spawn wave nay nằm trên **hành lang tiếp cận của từng gateway**, ngoài cùng bán kính spawn tính từ căn cứ như legacy. Không buộc sinh ở cửa biên nữa. Đây là quyết định tích hợp để đáp ứng mục tiêu giữ áp lực tương đương của BuildPlan, không đổi `MapData`/generator version3.
3. Khi ReturnToMenu, bridge/Entity/ví đã dọn nhưng `ProceduralMapRuntime.IsActive/IsReady` vẫn true. `EndComposition` nay xóa Active/Ready/Failed/LastError cùng composition gate khi root bị hủy, tránh giữ trạng thái map của trận cũ.

## Dữ liệu, thuật toán và điểm tích hợp

`TryGetGatewaySpawnPosition` cũ vẫn trả điểm cửa biên lùi2cell để kiểm topology/đường đi S6a. Wave dùng API mới `TryGetGatewayApproachPosition(index, townHallPosition, radius, out position)`; không đổi ý nghĩa API cũ.

API mới kiểm index, radius âm/NaN/Infinity. Nó đi theo các tâm tuyến L đã được `StrategicLayout` bảo vệ: start→region tương ứng→gateway, X trước rồi Y; chỉ chấp nhận cell có Routes mask. Với mỗi tâm cell world, lấy khoảng cách XY đến TownHall và chọn tâm gần nhất **ở ngoài** radius. Không có tâm phù hợp thì trả false; wave thử cửa còn lại và lịch retry hiện có, không fallback sang hướng ngẫu nhiên trên strategic. Radius là `CalculateTownHallRange + SpawnExtraRange`, được tính lại ngay tại `SpawnEnemy`, không phụ thuộc đã vẽ vòng range hay chưa. Bridge tiếp tục kiểm/resolver theo bán kính prefab thật trước tạo Entity.

Trong ba seed đã đo, radius≈20,13world units; hai tâm spawn tách biệt và nằm trong radius+1,5. Quái melee/ranged trong definition đều fit tại tâm mà không phải dịch vị trí. Khi căn cứ mở rộng, radius tăng như legacy; nếu đã vượt hết đường bảo vệ/map thì có thể không còn điểm spawn, cần kiểm ở S8/S9 thay vì lén sinh vào trong công trình. Thuật toán phụ thuộc hợp đồng tuyến L hiện tại: nếu đổi layout routes phải cập nhật adapter/test này cùng lúc. Không làm BFS mới mỗi frame; duyệt centerline chỉ khi yêu cầu spawn. Chưa đo chi phí CPU/GC mới.

`DayNightCycleManager.Update` và `EnemyWaveManager.Update` chuyển `Time.deltaTime` vào hàm `Tick(float)` nội bộ. Timer dùng tham số này;0/âm/NaN/Infinity không advance hoặc spawn. Đây là ranh giới thời gian để kiểm orchestration; không thêm clock service/container hoặc managed dependency vào ECS. Runtime bình thường vẫn dùng thời gian Unity. Tick0 trước đây có thể cho wave đầu spawn ngay khi timer bằng0; nay pause không làm phát sinh wave từ Update.

## Cách kiểm tái lập

`Tools/ValidateS6Waves.cs`, class `S6WaveValidation`, method `Run` qua Unity MCP `script-execute`, trong Play session mới/day1 đã pause. Exit Play sau mỗi lần. Đổi Seed/Preset ở asset trước khi Play, sau cùng trả `StrategicRegions`, seed1. Không thay SampleScene hoặc lưu terrain sinh ra vào scene.

- Giữ dt0,02 cho cả day/night tick, wave tick, Gameplay/Swarm World time. Gọi Night bằng phase API nội bộ, rồi chạy đủ đêm đến Day2; không bỏ qua delay/interval, không rút ngắn đêm. Đặt `UnityEngine.Random.InitState(2026)` riêng cho lịch wave và phục hồi state trong finally.
- Theo dõi Entity mới từ lịch wave, loại hai enemy đặt sẵn khỏi số đếm tiếp cận. Hai enemy gốc vẫn có trong mô phỏng/peak/retreat. Theo dõi lúc body còn sống đi vào phạm vi10world units quanh body TownHall; không đồng nhất điều này với đã đánh trúng, tới đúng mép collider hoặc gây cùng damage.
- Tick hai simulation, sync bridge/goal/presentation và refresh navigation khi version đổi. Không chạy mọi MonoBehaviour/UI Update như một Player frame; do đó gọi đây là **orchestration integration**, không phải playtest tự nhiên hoặc FPS benchmark.
- Assert đủ12quái/3wave, thời điểm start so lịch UI sai số<0,25giây, invalid/paused tick không spawn, index/radius sai bị từ chối, hai hướng approach tách biệt và fit tất cả prefab. Sang ngày, mọi quái còn sống phải Retreating/không targetable, và hết agents sau tối đa15giây cleanup.
- So legacy bằng cùng scene/settings/seed nhưng tạm chọn `LegacyMainland` để chạy generator v1 và vòng spawn cũ; same wave RNG. Layout khác nên không phải thí nghiệm cô lập mọi yếu tố terrain; không dùng SampleScene làm baseline có cùng tài nguyên.

## Kết quả sau sửa

Tất cả6lần strategic/legacy: đêm45giây,12/12quái,3wave bắt đầu0,02/14,10/30,20giây. [Log lời trả Unity](S6c-WaveChecks-2026-11-13.txt) giữ cả trước/sau để không thay kết quả lịch sử.

| Seed | Strategic: tới phạm vi10 / đầu tiên (giây) | Legacy: tới phạm vi10 / đầu tiên | Strategic: peak agents / sống lúc retreat | Cleanup sau ngày (giây), cả hai preset |
|---|---|---|---|---|
| 0 | 12/12 /5,42 | 10/12 /7,36 | 12 /12 | 11,98 |
| 1 | 12/12 /5,50 | 11/12 /5,36 | 12 /12 | 11,98 |
| 1543 | 9/12 /5,82 | 8/12 /5,56 | 11 /11 | 11,98 |

Các số này cho thấy đợt đầu không còn bị trì hoãn khoảng22giây chỉ vì sinh sát biên; không chứng minh đã cân bằng damage/độ khó. Seed1543 còn3quái không vào phạm vi10 trước sáng, có một quái chết trong mô phỏng và các quái khác có thể bị terrain/ùn/hành vi target ảnh hưởng; chưa phân loại từng trường hợp. Retreat có timeout12giây trong runtime, nên cleanup đạt không chứng minh mọi quái đi bộ thoát được về vòng/cửa. Chưa nghiệm thu đông quái hoặc nhiều ngày tại đây.

## Lifecycle và regression

`Tools/ValidateS6Lifecycle.cs` có `CheckReady` và `CheckMenu`; dùng `GameFlowMenu.Play/ReturnToMenu` thực. Sau tải gameplay cần Editor Step cho Awake/Start hoàn tất trước assert. Một phép thử ban đầu pause ngay sau LoadScene đã báo chưa Ready; không tính là pass, không sửa root để chạy sớm.

Sau sửa cờ static, vòng MainMenu→ProceduralGame→MainMenu→ProceduralGame PASS: gameplay một bridge/25actors/132resource node đã inject/một listener wave/ví100mỗi loại; menu không bridge, actors/resources/agents/projectiles bằng0, ví0, cờ map/gate/error sạch. Repeated root Initialize/Bind không thêm actor. `Tools/ValidateS6.cs.Run` cũng PASS lại sau lần Play thứ hai, gồm gateway fit/resource counts/DI/listener/readiness/reapply và wave API.

Regression cuối: `RTS.DOTS.Tests` **58/58 PASS**,0fail/skip,1,8726907giây; `RTS.ProceduralGeneration.Tests` **17/17 PASS**,0fail/skip,5,1120699giây. Đây là chạy sau các sửa Tick/timer/approach/cleanup, không tái sử dụng kết quả S6b. Runtime `RTS.DOTS` không sửa. Không Player build/benchmark mới.

## Môi trường và giới hạn

MCP từng mất kết nối trước khi gọi Play từ menu; log báo server đóng kết nối sau handshake/timeout. Lời gọi báo chưa gửi không được tính đã chạy. MCP tự nối lại, sau đó lifecycle/regression thực hiện được; không thấy bằng chứng native crash mới trong đoạn kiểm đó, không tuyên bố sửa crash cũ.

Cuối phiên asset strategic được trả seed1/preset1, Editor ProceduralGame sạch, ngoài Play/compile/pause; không job hoặc object thử đang chạy. Không commit mới. Các file thay đổi chính: hai manager Tick/timer, `ProceduralMapAuthoring` approach API, `ProceduralMapRuntime` cleanup, hai Tools kiểm chứng và tài liệu.

Chưa kiểm click/drag minimap, hình ảnh trên nhiều độ phân giải, một trận nhiều ngày/damage balance, expansion radius lớn, domain-reload matrix mới hoặc Player. S6b đã kiểm worker/nộp/xây/fog-camera API trên3seed; S6c không thay các kết quả đó thành nghiệm thu thao tác người dùng.

## Điểm tiếp theo

**D3c** theo Inventory: procedural/spawner lookup, BuildingProcess/build/train actions và factories/pool; giữ lifecycle resource node đã migrate ở D3b. Kiểm runtime spawn/register/remove/return/reuse trước đánh dấu D3 DONE. Phần S6 còn lại là xác nhận UI/input/hình ảnh và xử lý nếu các kiểm expansion/wave thực phát hiện vấn đề; chưa chuyển S7 trong lúc D3 đang dở.

Nguồn đối chiếu: [BuildPlan S6](../Progression/BuildPlan.md), [S6a](S6-Integration.md), [S6b](S6b-EconomyResourceDI.md), [Inventory DI](../DependencyInjection/Inventory.md), [DOTS Architecture](../DOTS/Architecture.md), [DevelopmentGuide](../DOTS/DevelopmentGuide.md), `StrategicLayout.cs`, `EnemyWaveManager.cs`, `DayNightCycleManager.cs`, `ProceduralMapRuntime.cs` và các Tools nêu trên. Không bổ sung nguồn thuật toán/thư viện bên ngoài.
