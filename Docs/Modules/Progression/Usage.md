# Progression — cách chạy baseline S0

Ngày 11/11/2026. Đã có sáu công nghệ, UI ba nhánh hai cấp và capability gates.

S2 đã được người dùng xác nhận checklist thủ công đạt. S3 đã mở catalog/UI; xem [S3 Evidence](S3-Evidence-2026-11-11.md).

Chọn TownHall để nghiên cứu. Tools→Cargo tăng kinh tế; Archery→Training mở cung và tăng tốc lượt train mới; Outposts→Fortify mở tháp và tăng damage. Node con hiện prerequisite; một slot nghiên cứu chung, mất hall đang nghiên cứu hủy không refund. Muốn xây ArcheryRange/train Archer hoặc xây Tower cần nghiên cứu nhánh tương ứng trước; nội dung legacy trong SampleScene không bị khóa. Kiểm integration tăng tốc bằng Tools/ValidateS3.cs như mô tả trong Evidence; không coi đó là gameplay tự nhiên.

## Chạy nghiên cứu mới

Mở MainMenu→ProceduralGame hoặc Play ProceduralGame trực tiếp. Chọn TownHall đã hoàn thành và bấm panel Research tools ở phía trên HUD. Cấu hình thử mặc định: 40 gỗ, 15 vàng, 20 giây; bắt đầu thành công trừ tiền một lần, đang nghiên cứu hiện tiến độ, xong hiện wood multiplier. TownHall bị phá sẽ hủy, không refund. Pause dừng timer. Bắt đầu trận mới reset tiến trình.

Root giữ reference asset EconomyTools và panel. UI nhận service qua Initialize; không tự tìm bridge. Để gọi API kiểm chứng, dùng root.Research.Start(GameplaySimulationSystem.EconomyToolsId, townHall); lấy trạng thái root.Research.State. Việc tìm root qua tool chỉ dành kiểm chứng; consumer gameplay phải được inject.

Xem [Evidence S1/D2](S1-D2-Evidence-2026-11-11.md) và chạy Tools/ValidateResearchPanel.cs qua script-execute để tái lập fake/lifecycle checks. Điều kiện mở S3: nghiệm thu click, bố cục ở resolution mục tiêu và một lần research với thời gian tự nhiên.

## Baseline S0 lịch sử

1. Mở Unity theo phiên bản trong [TechnicalOverview](../../TechnicalOverview.md). Mở `Assets/Scenes/ProceduralGame.unity` hoặc vào từ `Assets/Scenes/MainMenu.unity`.
2. Chạy EditMode tests của `GameplaySimulationTests` và `ProceduralMapGeneratorTests`. Ghi số pass/fail, Unity version và artifact nếu xuất được.
3. Play `ProceduralGame`. Kiểm tra log `[ProceduralMap] Ready`, `ProceduralMapAuthoring.IsReady`, bridge trên GameManager và ví đầu trận.
4. Kiểm tra bằng thao tác thật: chọn worker → khai thác cây/vàng → nộp; xây và huấn luyện; pause/resume; đợi một wave ban đêm. Ghi seed, ngày, số tiền trước/sau và lỗi console. Không coi việc scene vào Play là bằng chứng cho các thao tác này.
5. Thoát Play Mode và xác nhận scene không dirty ngoài ý muốn. Cập nhật [Progress](Progress.md) ngay khi kết thúc.

Giá trị seed mặc định trong asset `DefaultProceduralMapSettings.asset` là 1 tại lúc đối chiếu. Nếu đổi seed/settings, ghi giá trị thật trong bằng chứng. Cách chạy gameplay chi tiết nằm trong [DOTSGameplay Usage](../DOTSGameplay/Usage.md) và [Procedural Usage](../ProceduralGeneration/Usage.md).
