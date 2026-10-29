# Kiểm chứng gameplay ECS

## Các mức bằng chứng

1. EditMode world độc lập: thuật toán và giao dịch dữ liệu ECS.
2. Windows Player/Burst AOT: compile toàn dự án và thực thi trong SampleScene với prefab/UI thật.
3. Kiểm tra thủ công: thao tác chuột, độ dễ nhìn, timing hoạt ảnh và chu kỳ ngày/đêm đầy đủ.

Không đánh đồng một mức với mức khác. Kết quả đo performance là mục riêng với các bài test chức năng.

## Bộ test tự động

`Assets/Game/DOTS/Tests/Editor/GameplaySimulationTests.cs` bao phủ:

- Máu authoritative và không phát external damage echo cho mọi vai trò.
- A* vượt vật cản, kết thúc đường đi, lệnh của người chơi và stance Defensive.
- Quân tự tìm/đánh Entity, cung thủ/tháp tạo đạn ECS.
- Khai thác và nộp gỗ/vàng/thịt, nhiều giao dịch cùng tick.
- Capacity của node, hủy command, chết và giải phóng claim.
- Xây dừng khi không có builder, nhận lại và hoàn tất một lần.
- FIFO huấn luyện, chờ công trình xây xong.
- Cừu ăn cỏ, nhận damage, chạy trốn, xác giữ nguồn thịt.
- Trú nhà không nhận damage, ra khỏi nhà phục hồi di chuyển.
- Node bị xóa không gây đọc Entity handle cũ.

Chạy kèm toàn bộ `SwarmSimulationTests` để tránh hồi quy va chạm và tải quái/đạn. Bộ Swarm bổ sung test tìm ô sinh quái hợp lệ gần vị trí authoring bị chặn.

## Kết quả đã thực thi ngày 16/10/2026

| Bài kiểm chứng | Kết quả |
|---|---|
| Unity 6000.3.11f1, EditMode, world độc lập | **32/32 test Passed**: 19 case gameplay, 13 case Swarm |
| Windows x64 Release Player | Build thành công; managed backend Mono và thư viện Burst AOT `lib_burst_generated.dll` |
| SampleScene / `--gameplay-smoke` | **18/18 check Passed**, danh sách `errors` rỗng |
| Registry trong bài Player | 25 view/actor ban đầu đều bind ECS, AIPawn đã tắt; JSON hiện lưu ghi cuối bài có 29 actor và 5 quái ECS |

Bằng chứng gốc: [NUnit XML](Evidence/editmode-tests.xml), [kết quả Player JSON](Evidence/gameplay-result.json), [điều kiện bài chạy](Evidence/README.md). `Tools/Collect-GameplayEvidence.ps1` chỉ chép bằng chứng khi XML và JSON đều Passed, đồng thời kiểm tra log Player sau khi thoát.

Các check Player xác nhận command di chuyển cập nhật Entity/view; claim cây, khai thác và nộp vào ví qua facade GameManager; xây tháp hoàn tất và đổi sprite; sinh Worker/Warrior/Archer từ ba nhà; ẩn/hiện worker khi trú/ra nhà; cừu nhận damage và chạy; wave manager, API spawn và direct Instantiate đều tạo quái ECS; không còn EnemyUnit GameObject active chạy legacy.

Đây là bài kiểm tra tích hợp có rút ngắn timer, teleport tới điểm tiếp cận và tắt lịch ngày/đêm/wave tự động. Nó gọi API thật nhưng không mô phỏng toàn bộ click chuột hoặc chạy hết một ngày đêm tự nhiên. Check nộp tài nguyên xác nhận giá trị ví qua GameManager; chưa tự đọc và so sánh từng text HUD. Thao tác chuột, text HUD và hình thức trình bày thuộc checklist thủ công dưới đây. Số actor cuối bài là trạng thái kiểm tra, không phải benchmark khả năng tải.

## Checklist thủ công

- Chọn đơn/chọn nhóm/khung kéo, click nền/minimap và stance đều điều khiển Entity đúng.
- Dân chặt cây, đào vàng, săn/lấy thịt và nộp đúng kho; khi thay lệnh không mất/đổi loại hàng.
- Đặt nhà bằng placement UI, đổi sprite theo tiến độ, hủy giao builder rồi nhận lại.
- Xếp hàng nhiều dân/lính/cung thủ, kiểm tra queue UI và lệnh đi ra cửa.
- Quân và tháp đánh quái, projectile không bị tạo hai lần bởi Animation Event.
- Dân đi vào nhà qua một chu kỳ đêm tự nhiên, ra ngoài ban ngày; phá nhà khi dân đang trú.
- Nhấn P tái sinh tài nguyên khi dân đang khai thác, kiểm tra claim/Entity cũ được dọn.
- Tải lại scene và vào/thoát Play nhiều lần, không có Entity hoặc registry tồn dư.

## Hiệu năng

Chưa có kết luận benchmark đại trà cho toàn bộ worker/quân/công trình hybrid. Bằng chứng Swarm trước đây chỉ áp dụng cho kịch bản quái/đạn đã mô tả trong module đó. Đo correctness và một lần smoke không chứng minh game chạy mượt với hàng nghìn Animator hoặc hàng nghìn đường A* độc lập.
