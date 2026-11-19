# S6 — tổng hợp nghiệm thu map/wave/navigation

Ngày **16/11/2026 — S6 DONE theo phạm vi build plan**. Matrix ba seed PASS qua API, người dùng xác nhận manual menu/input/ngày đêm/cung-tháp; hai kiểm cuối worker pool qua reload và17procedural đã PASS. [Progress](../Progression/Progress.md), [BuildPlan](../Progression/BuildPlan.md) ghi bước tiếp S7. Không coi đây là hoàn tất balance/Player/performance hoặc toàn project đạt DI.

## Mục tiêu và phạm vi

Tích hợp strategic v3 vào ProceduralGame: resource/landmark, gateway approach, hai navigation system, kinh tế/xây dựng, fog/minimap/camera, input readiness và lifecycle. Giữ áp lực theo lịch wave, không tăng số quái theo diện tích đất. DI bổ sung đi cùng S6 theo D3; UI/fog/camera audit toàn project thuộc D4.

## Dữ liệu và luồng

Authoring sinh terrain/resources từ settings/seed → root cấp dependencies → bridge capture grid và bind views/Entity → mở Ready gate → GameManager.BeginGameplay cấp tài nguyên/listener một lần. Unit mới được inject ở factory/bind; return/unload không giữ service scene cũ. Wave nhận clock/map/game/bridge/generator tường minh, chọn approach theo radius căn cứ hiện tại. Native economy/combat/path vẫn chạy trong RTS.DOTS; view/UI cập nhật từ bridge.

Không thay generator Core, quotas, giá, duration, preset/seed mặc định, package, Editor hoặc pipeline trong lần nghiệm thu này. Quét Core1000seed/S5 và baseline wave/arrival của S6a/b/c là bằng chứng lịch sử có phạm vi, không chạy lại rồi giả làm số đo mới.

## Cách chạy lại và raw evidence

[Raw log16/11](S6-FinalChecks-2026-11-16.txt) được driver ghi từ stdout MCP thật; không điền kết quả suy đoán. Tooling ngoài Assets, không đưa vào runtime build:

1. Mở ProceduralGame, Play, để Start chạy rồi pause Editor.
2. Từ project root chạy `./Tools/RunS6SeedChecks.ps1 -Seed 0`, rồi1543 và1. Driver dùng CLI Unity MCP; mỗi check thất bại sẽ dừng seed hiện tại. Cần kết nối/account/approval hoạt động; không chạy nếu công cụ đang bị chặn.
3. Driver đổi **Seed property trong bộ nhớ** rồi LoadScene, Editor Step, CheckBindings → economy Run → RunConstruction → CheckFogCamera → CheckInput → RunDays. Không SaveAssets; sau cùng restore Seed1, Exit Play và xác nhận asset trên disk vẫn1.
4. RunDays reset clock về ngày1, tick DayNight/EnemyWave và hai ECS system cùng dt0,05, synchronize bridge/navigation. **Chỉ nâng máu TownHall lên1.000.000 trong test rồi phục hồi** để kiểm lifecycle/routing qua ba đêm; không phải trận chơi cân bằng. Tối đa24.000ticks, restore World.Time/RNG trong finally.

Code: [ValidateS6Acceptance.cs](../../../Tools/ValidateS6Acceptance.cs), [driver](../../../Tools/RunS6SeedChecks.ps1), [economy/build/fog](../../../Tools/ValidateS6Economy.cs). Entry points và precondition của tests cũ ở [S6a](S6-Integration.md), [S6b](S6b-EconomyResourceDI.md), [S6c](S6c-WavesLifecycle.md).

## Kết quả ba bố trí khác nhau

Tất cả số giây dưới đây là **thời gian mô phỏng**, không phải frame time/FPS hoặc thời gian chạy Player.

| Seed | Nộp20gỗ /20vàng | Hoàn tất kho landmark | Radius wave sau mở rộng | Ba chu kỳ | Actor sau xây → cuối |
|---|---|---|---|---|---|
|0|54,25s /58,95s|30,00s|60,80|417,40s,42spawn,cleanup0agents|26→26|
|1|52,20s /58,10s|27,00s|59,15|417,40s,42spawn,cleanup0agents|26→26|
|1543|60,80s /55,55s|32,50s|60,86|417,40s,42spawn,cleanup0agents|26→25|

Mỗi seed bắt đầu25actors; hai worker được giao node trong Economy/GoldFrontier, wallet sau nộp120/120/100. Placement kho landmark được validate, debit100gỗ/100vàng một lần, actor25→26 và construction hoàn tất đúng sprite/native state. Fog reveal vị trí công trình, minimap128×128 và camera focus PASS.

Input check dùng selection/ground-click adapter của GameManager, kiểm command ECS; gọi OnPointerDown/OnDrag của minimap với PointerEventData/GraphicRaycaster trong EventSystem thật tại25%/75% hai chiều, camera về tọa độ đúng và giữ z. Đây là **synthetic event/callback checks**, không khẳng định đã thao tác chuột vật lý hoặc kiểm mọi hit-test/UI overlay.

Ba chu kỳ có phase/day index1→4, approach tồn tại ở radius mở rộng, có vị trí navigation hợp lệ cho địch; không nhân gameplay actors, retreat dọn sạch sau sáng. Actor giảm ở seed1543 là tổn thất trong scenario, không được coi mọi quân đều bất tử. Lần chạy thử seed1 trước raw matrix có kết quả actor26→25; không trộn lần đó vào bảng chạy cuối26→26. Vị trí/timing chịu ảnh hưởng RNG/config và chỉ đại diện ba seed đã chạy.

## Quota và điểm chuyển cho S8

Kịch bản ban đầu so tổng configured quota48 với actual42 và fail. Đối chiếu lịch UI cho thấy **chỉ42lượt đến hạn trước khi hết45giây mỗi đêm**:12/15/15. Sáu lượt còn lại của wave cuối đêm3 nằm sau bình minh; không phải spawn được phép nhưng thất bại vì địa hình. Test cuối tính mốc GetWaveStartTime + interval từ config, chỉ đếm mốc nằm trong thời lượng đêm, rồi so actual/Entity seen; cả ba seed42/42 PASS.

Không nới assertion tùy tiện và không tăng thời lượng đêm/quota để chiều test. **S8 phải quyết định** giữ cắt wave lúc sáng hay điều chỉnh duration/interval/quota, dựa playtest. Lịch UI chuẩn hóa hiện có có thể dồn mốc ngoài đêm về cuối thanh; ghi đây là đầu mối cân bằng/UX cho S8. Chưa có kết luận trận vui hoặc áp lực quân sự đủ chỉ từ matrix này.

## DI và regression liên quan

[D3c4b Evidence](../DependencyInjection/D3c4b-UnitLifecycle.md) ghi Unit/Worker/Sheep/Structure/AIPawn injection, register/unregister, pool, navigation invalidation và scene tham chiếu. SampleScene sau code cuối PASS25Units, đủ registry không trùng,3rejects, commands khi Instance=null, pool4loại/nav, direct unregister/rebind/destroy. Producer/đạn SampleScene PASS lại sau migration. DOTS cuối59/59 PASS,0fail/skip,1,0734240s; code gameplay không đổi từ lượt đó. Procedural đã rerun17/17 PASS,0fail/skip,8,1212562s trong lượt đóng S6 ngày16/11; không dùng duration15/11 như số đo mới.

## Nghiệm thu cuối và trạng thái

1. **Pool Unit qua unload/reload PASS:** CheckReady → D3UnitValidation.PrepareReloadPool → GameFlowMenu.ReturnToMenu/CheckMenu → GameFlowMenu.Play/Editor Step/CheckReady → CheckReloadPool. Worker vẫn đúng cùng view, vào scene mới với bridge/GameManager mới, nhận resource work rồi return sạch. Menu không bridge/actors/resources/agents/projectiles/ví/cờ cũ. Game mới1bridge/25actors/132nodes/1wave listener/ví100x3, gọi BeginGameplay lặp không nhân ví/listener. Không có code train mới nên không lặp production tests đã đạt.
2. **Manual đã xác nhận16/11:** người dùng báo “oke chạy được” sau checklist menu→game→menu→game/tài nguyên/selection/minimap và ngày/đêm/cung-tháp. Ghi PASS theo báo cáo, không ảnh/log kèm. Pool nội bộ được kiểm riêng tại mục1, không suy từ manual. Chưa có ảnh Game View mới16/11; S7 phải chụp baseline cùng seed trước/sau thay đổi hình ảnh, không dùng ảnh S5 giả làm S6.
3. **Chốt môi trường PASS:**17procedural đạt, Seed property1; ProceduralGame sạch, ngoài Play/pause/compile. Console Exception sequence>4123=0; có4128 là cảnh báo plugin đường dẫn có spaces. Không move project/đổi Editor để né log. Fixture đã dọn, không job/CLI chờ. Không sửa gameplay nên không lặp Native59.

**Lịch sử công cụ:** lượt trước PrepareReloadPool/menu-game bị automatic approval review từ chối trước thực thi do usage limit, không phải phán quyết unsafe. Lượt đóng S6 các tool MCP trực tiếp đã xuất hiện và hoạt động lại; đã chạy các kiểm còn thiếu bằng quyền tool thông thường, không bypass review qua CLI. Trạng thái hiện tại đã read-back ở mục3; không còn block đó trong checkpoint đang hoạt động.

## Hiệu năng, giới hạn và nguồn

Chưa đo FPS/allocation/composition time hoặc build Player. Tick harness không advance mọi MonoBehaviour/coroutine/input/render như Player; bảo vệ TownHall và gọi callback qua reflection là setup kiểm integration, không thay cho playtest S8. Không xác định nguyên nhân/sửa native crash trước đó. Không nâng Editor/pipeline để né crash.

Tài liệu nguồn: [BuildPlan S6–S9](../Progression/BuildPlan.md), [DI Architecture](../DependencyInjection/Architecture.md), [DOTS gameplay architecture](../DOTSGameplay/Architecture.md), [S5 topology](S5-ResourcesTopology.md). Mục tiêu/thuật toán generator và ECS theo các tài liệu này; lần này chỉ kiểm và hoàn thiện integration/lifecycle, không thêm thuật toán bên ngoài.
