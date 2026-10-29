# Chạy module DOTS / Swarm

## Môi trường

Mở bằng Unity 6000.3.11f1 và để Package Manager resolve manifest/lock. Sau khi Unity cập nhật Burst trong một Editor đang mở, khởi động lại Editor nếu có lỗi compiler service của phiên bản Burst đã tải trước đó.

## Mô phỏng độc lập

Mở `Assets/Scenes/SwarmSimulation.unity`, bấm Play. Nếu cần tái tạo scene/library, dùng `RTS > DOTS > Generate swarm assets and scene`. Công cụ sẽ yêu cầu lưu scene đang sửa trước khi mở scene khác.

Mặc định demo hướng tới 5.000 quái và 2.000 đạn. Spawn được phân bổ theo frame. Số đạn live là kết quả của tốc độ sinh, thời gian bay và va chạm nên có thể thấp hơn giá trị đặt; phải báo cáo số live đo được.

| Điều khiển | Chức năng |
|---|---|
| WASD/phím mũi tên/chuột giữa | Di chuyển camera |
| Cuộn chuột | Zoom |
| Chuột phải | Đổi vị trí mục tiêu chung |
| Space | Pause/resume |
| R | Reset và dùng lại seed |
| F1 | Bật/tắt HUD |
| B | Bắt đầu capture sau warmup |
| E | Xuất CSV các frame đã capture |
| Nút 100/1000/5000/10000 | Đặt tải quái và reset |
| Rendering on/off | Tách chi phí hiển thị khỏi simulation |
| Nút scenario | Bãi trống, lối hẹp hoặc vây căn cứ |

Inspector `SwarmDemoController` cho phép chỉnh population, projectile population, seed, AutoReplenish và scenario. `SwarmBenchmarkRecorder` có WarmupSeconds/CaptureSeconds. AutoReplenish bổ sung quái đã chết để duy trì tải.

## Gameplay RTS

Mở `SampleScene` và Play như trước. Bridge được tạo khi scene tải: wave dùng Entity; logic dân/công trình/quân người chơi/cừu cũng chạy ECS, với prefab MonoBehaviour giữ phần hiển thị và UI. Chọn Warrior/Archer rồi click quái ECS để attack; Tower và quân offensive có thể tự tìm quái. Xem [DOTS Gameplay](../DOTSGameplay/Usage.md) để kiểm tra công việc, huấn luyện và Entity/view.

Các phím debug cũ O (fog), P (tái tạo tài nguyên), F1 (HUD) vẫn do module cũ quản lý. Walkability mới được phản chiếu sang Swarm theo NavigationVersion.

## Kiểm thử và build

Trong `Window > General > Test Runner`, chạy EditMode fixture `RTS.DOTS.Tests.SwarmSimulationTests`. Các bài test sử dụng ECS World riêng và giải phóng World sau mỗi bài.

Dùng `RTS > DOTS > Build swarm demo (Windows)` để build demo riêng vào `.utmp/swarm-build/SwarmDemo.exe`. Menu giữ scene RTS riêng; không publish hoặc deploy lên dịch vụ ngoài.

Ví dụ CLI, thay đường dẫn Editor cho phù hợp máy:

```powershell
$unityEditor = 'F:\unityinstaller6000.2.14f1\6000.3.11f1\Editor\Unity.exe'
$projectPath = (Get-Location).Path
& $unityEditor -batchmode -nographics -projectPath $projectPath -runTests -testPlatform EditMode -testFilter RTS.DOTS.Tests -testResults "$projectPath/Logs/dots-tests.xml" -logFile "$projectPath/Logs/dots-tests.log"
& $unityEditor -batchmode -nographics -quit -projectPath $projectPath -executeMethod SwarmEditorTools.BuildDemo -logFile "$projectPath/Logs/dots-build.log"
```

Không chạy một Editor batch khác cùng project đang được Editor tương tác giữ khóa. Có thể dùng bản sao kiểm chứng độc lập; không kết luận benchmark headless là hiệu năng rendering của bản build.

## Vị trí kết quả

CSV trong Editor: `.utmp/benchmarks`. CSV trong Player: `Application.persistentDataPath/Benchmarks`. Mỗi CSV có cấu hình CPU/GPU/RAM, Unity, resolution, vSync, scenario, seed, trạng thái rendering, frame time, ECS tick time, số quái/đạn thực tế và GC.

CSV còn ghi `unity_allocated_bytes` (bộ nhớ Unity allocator lấy mẫu 1 Hz và giữ giá trị giữa các lần lấy mẫu, không phải toàn bộ RAM tiến trình/GPU) và `ecs_tick_id`. Khi render FPS khác nhịp fixed tick, cần loại tick ID lặp để thống kê thời gian từng tick. `gc_bytes = -1` nghĩa là counter không khả dụng trong cấu hình build đó; không được diễn giải là zero GC. Reset, pause hoặc đổi trạng thái rendering sẽ hủy bài capture đang chạy.

Để smoke test tự động bản Player, dùng `--swarm-smoke --swarm-output <thư mục>` hoặc `--rts-smoke --swarm-output <thư mục>`. Có thể thêm `--swarm-count 10000` hoặc `--swarm-no-render`. Runner khóa camera, gửi render request URP vào texture 1920×1080, chụp ảnh, xuất JSON/CSV rồi thoát. Đây là kiểm chứng offscreen, không thay thế benchmark cửa sổ game tương tác có present/vSync thực tế.

Các thư mục `.utmp` và `Logs` không đưa vào Git. Chọn CSV dùng trong khóa luận, kiểm tra dữ liệu rồi lưu bản kết quả cần trích dẫn cùng cấu hình thí nghiệm.

