# D4 — DI presentation, fog và kết thúc inventory

Ngày **17/11/2026**. Trạng thái **D4 DONE trong phạm vi inventory**, với các ngoại lệ rõ dưới đây; S9 đang tiếp tục. Không đồng nhất DI với việc cấm mọi static API của Unity.

## Phạm vi, dữ liệu và luồng

Giữ manual DI/ADR-001, không container/package mới. GameSceneCompositionRoot nhận scene refs, lắp gameplay rồi ComposePresentation; helper dùng scene-owned hierarchy tại composition boundary để inject9view/8loại. Bootstrap SampleScene lắp game/map/camera/fog/day/wave/locators rồi gọi cùng helper. Consumer không Find/Get/Instance để lấy service. MonoBehaviour giữ config visual/local component, ECS runtime không tham chiếu Assembly-CSharp.

Các consumer DayCounterUI, DayNightClockUI, WaveProgressUI, SelectedUnitsInfoPanelUI, ResourceRequirementsDisplay, IdleWorkerPanelUI, MinimapFogUI, CursorManager nhận refs bằng Initialize, từ chối null/đổi context và giữ listener khi init lặp. Idle refresh đầu tiên ở Start sau bridge bind; panel chỉ remove listener của mình. Minimap texture được destroy khi thay/khi teardown.

Fog nhận game/map, khởi tạo grid một lần. Composition cấp fog cho affectable có sẵn; Unit/resource factory cấp fog cho view mới. Affectable không cache static hoặc tự Find, register/unregister theo enable/disable, Unbind xóa owner và trả alpha gốc. Unit/resource unbind gọi affectable.Unbind; pool qua scene mới phải bind lại. Không áp alpha ngay trong registration trước Fog.Start, tránh can thiệp màu trước Awake của các view ban đầu.

GameManager/TilemapManager/CursorManager bỏ SingletonManager inheritance. Legacy dependency discovery và tạo locator chuyển về bootstrap; GameManager không tự lắp service trong Awake/Start. CameraController nhận Camera; GameManager/minimap/cursor dùng camera đã cấp; PlacementProcess.Update nhận camera, GameUtils chuyển tọa độ bằng tham số. Trạng thái short-click thuộc GameManager, không còn static input state chung.

## Kiểm thử và bằng chứng

- Qua CLI Unity MCP đúng Editor, dùng --no-start, không mở Editor thứ hai. D4a/b compile và integration trên ProceduralGame và SampleScene.
- `Tools/ValidateD4Presentation.cs`: mỗi scene9views/8types, kể cả inactive;16null/different rejects; init lặp giữ listener/texture. Procedural→menu→game chạy lại PASS. Menu dọn actors/wallet/goal về0.
- `Tools/ValidateD4Fog.cs`:153active affectables ở ProceduralGame,71ở SampleScene đều nhận owner tường minh; registry không trùng;6null/different rejects; enable/disable/unbind và alpha gốc PASS.
- `Tools/ValidateD3Units.cs` được bổ sung assert fog: Worker/Ranger/Sheep/Barrack acquire/register/release/reuse sạch actor/context/fog. Worker pool sống qua menu→game mới nhận đúng fog mới và unbind sạch PASS.
- `Tools/ValidateS6Acceptance.cs.CheckInput`: selection/ground command tới ECS, minimap pointer/drag25%/75% và camera z PASS sau D4b. Không dùng xác nhận S8 trước refactor để thay kết quả này.
- Regression cuối sau D4 và cache ResearchPanel đầu S9: **81/81 PASS**,0fail/skip,10.9443911giây (64DOTS+17procedural). Thời gian Test Runner không phải benchmark gameplay.
- Play entry có một request bị plugin_reconnected sau assembly reload; đã kiểm hiệu ứng thực tế (Ready=true, fog=true) rồi tiếp kiểm, không gửi lại EnterPlay để nhân thao tác. Không coi đó là native crash đã sửa.

## Audit và ngoại lệ được giữ

| Nhóm còn lại | Lý do/owner/lifetime |
|---|---|
| Root/bridge scene bootstrap, scene actor/resource capture | Composition/integration boundary, không consumer gameplay; scene đơn hiện tại. Bridge reset khi unload; default World do Unity sở hữu, không dispose ở root |
| SwarmGameBridge.Instance | Giữ compatibility handle cho smoke/tooling cũ, không còn consumer nghiệp vụ dùng; reset subsystem/unload. Không thêm fallback từ DI về Instance |
| SingletonManager.cs | Source legacy không còn lớp production kế thừa hoặc caller Get của manager. Giữ file để tránh xóa asset ngoài scope; không nằm trong luồng gameplay |
| Editor tools, smoke/benchmark/test harness | Discovery ở authoring/test boundary; không gọi như service locator trong gameplay. GameplaySmokeAutomation bỏ GameManager.Get, dùng bridge scene của fixture |
| World access ở SwarmDemoController | Demo composition boundary, system do World quản lý; không đưa interface managed vào Burst job |
| Resources.Load/Shader.Find visual assets | Unit outline, placeholder/icon, renderer library/material/shader; asset config, không tìm manager. Không đổi hệ asset chỉ để xóa từ khóa Load |
| ProceduralMapRuntime | Gate/static state cho **một gameplay scene active**, reset SubsystemRegistration/root teardown; không chứa service reference. Additive nhiều gameplay scene cần scene-owned context trước khi hỗ trợ |
| RuntimeObjectPool | Cache scene-independent, view inactive unbound; acquire nhận owner Scene, caller Register; pool/reload đã kiểm |
| Unity Input/Time/Screen/EventSystem.current | Engine/platform boundary, không service nghiệp vụ của game; helper world-coordinate nhận camera tường minh |

## Cách tái lập và giới hạn

Mở ProceduralGame hoặc SampleScene, Play đủ Start rồi pause; chạy các harness qua script-execute. CheckPool/PrepareReloadPool/CheckReloadPool dùng session disposable; không save runtime fixtures. Run regression bằng tests-run với hai namespace. Không coi search text là kiểm chứng DI độc lập; phải đối chiếu inventory và lifetime như bảng trên.

Chưa đo FPS/GC toàn scene/Player của code này tại thời điểm đóng D4; chuyển S9. Chưa test mọi thao tác cảm ứng hoặc nhiều gameplay scene additive. Không nâng Editor/pipeline, không reset thay đổi có sẵn của người dùng. Tài liệu: [ADR-001](ADR-001-ManualDI.md), [Inventory](Inventory.md), [Architecture](Architecture.md), [Progress](../Progression/Progress.md).
