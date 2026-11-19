# Progression — cách chạy baseline S0

**Cấu hình sau G2 ngày19/11:** `MainMenu` → `ProceduralGame` dùng Hall1600HP, vẫn sống5đêm/thắngngày6; prefabTownHall và SampleScene không đổi. Đây là shortlist để kiểmG3, chưa xác nhận mọiseed đều không tựthắng. [G2 report/recipe/restore](G2-PressurePilot.md). Runner tạm đãgỡ, có thể Play bìnhthường; các lệnh harnessG01 bên dưới cần baseline10000 đãđóngbăng, không chạy thẳng vào trận1600 rồi trộn số liệu.

**Harness G0–G1, chốt19/11:** [MeasurementContract](G01-MeasurementContract.md) mô tả paused Ready Play, scene mới/seed, class G01Playtest và bốn method policy, khôi phục Seed=1/thoát Play. Kiểm raw offline từ project root: `node Tools/SummarizeG01.mjs --complete --compact`. [Báo cáo và giới hạn](G01-Validation.md) tách pilot/fail, matrix trước guard và self-check presentation sau guard; không chạy harness này để đo FPS hoặc tiếp tục trận người dùng đang chơi.

**Bàn giao17/11:** Play MainMenu→ProceduralGame, chọn TownHall/nghiên cứu/build/train như cũ; sống5đêm→Victory ngày6, mất hall cuối→Defeat, Play again/Main menu reset trận. DI UI/fog/input hiện được composition cấp; không gọi Get/Instance trong consumer mới. Windows build đã test ở `.utmp/s9-build-2026-11-17/RTSgame2026.exe`, cần giữ Data đi kèm. Cách tái lập smoke/perf và giới hạn xem [S9](S9-Acceptance.md); kiểm map/research config qua Inspector trước thay số. Không nâng pipeline để chạy. Các trạng thái manual-chờ phía dưới là lịch sử trước user xác nhận17/11.

**S8 —16/11:** ProceduralGame hiện có objective sống5đêm, thắng khi sang ngày6, thua khi mất hall cuối. Result chặn lệnh, có Play again/Main menu; trận mới reset ví/research. [MatchFlow/cách chạy và playtest](../MatchFlow/Architecture.md). Manual S8 đang chờ theo người dùng, chưa nghiệm thu balance.

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
