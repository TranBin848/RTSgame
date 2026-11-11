# Kiểm chứng Dependency Injection

Ngày 10/11/2026. **D1 DONE**. Các mục D1a/D1b bên dưới ghi lịch sử checkpoint; trạng thái mới nhất và kết quả nghiệm thu nằm ở [D1 Evidence](D1-Evidence-2026-11-10.md).

Ngày 11/11/2026: **D2 DONE về injection và lifecycle của research service/panel**. Fake-service, init lặp, disable/reopen, replacement guard và UI→ECS transaction đã chạy. [Evidence S1/D2](../Progression/S1-D2-Evidence-2026-11-11.md); UI visual/manual acceptance thuộc S2 còn chờ.

## D1c — nghiệm thu cuối

GameManager và hai locators đã inject; root IsReady chỉ mở sau BindScene; lỗi composition chặn input/AI legacy/ngày đêm/wave. Các kiểm tra repeat init, missing config, duplicate owner, MainMenu reload, domain reload bật/tắt và SampleScene legacy đạt. Regression EditMode đạt 34/34 DOTS và 3/3 procedural. Mã assert tái lập nằm ở `Tools/ValidateD1.cs`; [Usage](Usage.md) mô tả cách chạy. Không có kết quả Player build hoặc benchmark mới.

## D1a — Bridge nhận dependency tường minh

`SwarmGameBridge.Initialize(World, GameManager, TilemapManager, FogOfWarManager, DayNightCycleManager)` nhận dependency từ bên ngoài. World, GameManager và TilemapManager là bắt buộc; fog/day-night vẫn tùy chọn như trước. Hàm từ chối dependency thiếu hoặc khác với lần khởi tạo trước. Khởi tạo lặp với cùng dependency không tạo renderer/system mới. Nếu một bridge khác đang sở hữu simulation, hàm từ chối khởi tạo. `IsInitialized` phản ánh hai simulation system đã được cấp, **không thay cho map/gameplay Ready**.

`Awake` hiện vẫn là adapter legacy tìm dependency rồi gọi Initialize. Vì vậy D1a chưa loại bỏ lookup, chưa có scene composition root và chưa thể tuyên bố scene đã dùng DI đầy đủ.

## Kết quả thực chạy

- Unity MCP instance `unity:RTSgame2026` kết nối; truy vấn Editor thành công, trước sửa `isPlaying=false`, `isCompiling=false`; MainMenu không dirty.
- Sau chỉnh file ngoài Editor đã gọi RefreshAssets. Unity hoàn tất compile; không có lỗi C# mới trong truy vấn Console. Console còn cảnh báo plugin về project path có khoảng trắng, không đổi đường dẫn dự án.
- Mở ProceduralGame, Play. Assert-based check qua script-execute: bridge `IsInitialized=True`, 25 actors và 1 renderer. Gọi Initialize hai lần cùng World/manager/map/fog/day-night: actor count và renderer count không đổi.
- Assert-based check: thiếu World ném ArgumentException; thiếu GameManager hoặc TilemapManager ném ArgumentNullException; đổi fog dependency sau initialize ném InvalidOperationException. Cả hai snippet trả PASS.
- Thoát Play Mode sau kiểm tra. `git diff --check` cho bridge đạt. Không ghi số liệu này thành benchmark.

## Phần D1 còn lại

### D1b — root scene, đã triển khai ngày 10/11/2026

`GameSceneCompositionRoot` gắn trên GameManager của ProceduralGame, giữ serialized references tới manager/map/generator/fog/day-night. Awake ở -250, sau map authoring -300, kiểm generator Ready và live World trước khi tạo/initialize bridge. Bridge Awake và sceneLoaded installer nhường quyền cho root trên cùng GameObject. Công cụ `ProceduralMapEditorTools` cũng gán root khi tạo scene lại.

Assert checks trong Unity: ProceduralGame root initialized, một bridge, 25 actors, wallet 100/100/100. Gọi root.Initialize hai lần giữ nguyên actor count. Root thiếu config trên GameObject tạm inactive ném InvalidOperationException trước khi tạo bridge; object kiểm tra được dọn ngay. SampleScene không root vẫn bootstrap legacy một bridge/25 actors. Đã thoát Play Mode; scene mở cuối là SampleScene.

Giới hạn: root mới inject bridge; GameManager và locators vẫn tự khởi tạo theo đường cũ. Ready của root chỉ là hoàn tất composition, chưa bảo đảm Start của bridge/binding đã kết thúc. Map generation vẫn tự chạy trong Awake. D1 còn PARTIAL; cần chuyển manager/locator, gate readiness/input/ngày đêm khi root thất bại và kiểm scene reload/domain reload. Lưu scene bằng Unity tạo thêm một số thay đổi serialization float AnimationSpeed 10↔10.000001; chưa thay nội dung terrain bằng code.

1. Root scene/bridge đã có; bổ sung test reload và kiểm duplicate-owner đầy đủ.
2. Nối GameManager/resource locator và map readiness theo ADR; validate trước Ready, giữ scene legacy SampleScene/demo.
3. Kiểm tra missing config, duplicate bridge, scene reload và domain reload bật/tắt, MainMenu→game→menu, teardown và wallet/Entity count. Chạy regression tests phù hợp sau khi hoàn tất các thay đổi này.

Nguồn đối chiếu: [ADR-001](ADR-001-ManualDI.md), [Inventory](Inventory.md), `Assets/Game/DOTS/Integration/SwarmGameBridge.cs`. Baseline lịch sử nằm ở [S0 Evidence](../Progression/Evidence/baseline-rerun-2026-11-05.md).
