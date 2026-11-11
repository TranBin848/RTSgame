# Chạy và mở rộng DI sau D1

Cập nhật 10/11/2026. D1 hoàn thành cho cấu hình ProceduralGame hiện có; D2–D4 chưa triển khai.

## Khởi động

Mở MainMenu và Play để vào ProceduralGame, hoặc mở ProceduralGame trực tiếp. Trên GameManager có `GameSceneCompositionRoot` với references tới GameManager cùng object, TilemapManager, ProceduralMapAuthoring và CameraController; fog/day-night là tham số scene hiện đã gán. Root Awake kiểm cấu hình/map/World, tạo hai locator và inject manager/bridge. Root Start bind navigation + thực thể, sau đó mới mở composition gate và báo IsReady.

`IsInitialized` chỉ cho biết đã cấp dependency; `IsReady` của root và `IsSceneBound` của bridge mới xác nhận đã qua bước bind. Map generation Ready một mình chưa đủ để bật gameplay.

Thiếu reference/map không sẵn sàng: Console ghi `Scene composition failed`, root.LastError chứa lý do. Input manager, AI legacy, ngày/đêm và wave bị gate; không tự chuyển sang gameplay độc lập. Sửa reference trong Edit Mode rồi bắt đầu lại scene; không hỗ trợ hot recovery của một phiên root đã thất bại.

## Tạo lại scene

Menu tạo procedural scene trong `ProceduralMapEditorTools` gắn root và gán đủ các references, gồm camera. Không chỉ sao chép GameManager rồi bỏ component root: SampleScene hiện là cấu hình legacy riêng. Khi thêm dependency mới, cập nhật đồng thời root, hàm Initialize của consumer, công cụ tạo scene, scene đã lưu và check tương ứng.

## Kiểm chứng

Xem [D1 Evidence](D1-Evidence-2026-11-10.md) để chạy `Tools/ValidateD1.cs` qua script-execute và lặp scene/domain reload. EditMode dùng hai assembly RTS.DOTS.Tests và RTS.ProceduralGeneration.Tests. Nếu refresh hoặc enter Play làm MCP disconnected, đợi compile/reload xong và kiểm lại list_engine_instances trước khi suy ra crash.

## Quyền sở hữu

Root sở hữu hai resource locator theo trận. GameManager dùng references được inject, không tự tạo locator trong scene có root. Scene cũ giữ adapter legacy rõ trong Awake/InitializeLegacyDependencies. Bridge sở hữu Entity/view mapping và teardown simulation data theo đường hiện có; root không dispose ECS World do Unity sở hữu. Event manager/bridge được unsubscribe ở OnDestroy. Toàn project chưa đạt DI vì Unit/UI/factory còn các dependency global đang được chuyển ở D2–D4.
