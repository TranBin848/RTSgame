# G5 — Windows Release và đo onscreen

G5 dùng TownHall 1200 HP và bản sửa navigation của G4. Phần nghiệm thu chơi thực tế G3 vẫn chờ. [Build plan G0–G7](NextPhasePlan.md) · [Đối chiếu G4](G4-FrameProfiling.md)

**Trạng thái 21/11/2026** — Build, đo và smoke đã xong. Cấu hình Ultra 1080p chưa đạt budget p95 16,67 ms ở mọi lượt nên G5 còn mở phần hiệu năng.

## Mục tiêu và cách chạy

Build Windows x64 qua `BuildPipeline.BuildPlayer` với `BuildOptions.None`, giữ backend, phiên bản Unity, URP và danh sách scene enabled của dự án. Chạy từ project root bằng PowerShell với thư mục dữ liệu mới.

```powershell
.\Tools\RunG5.ps1 -BuildDirectory '.utmp/g5-release-2026-11-21' -OutputDirectory '.utmp/g5-release-onscreen'
node Tools/SummarizeG5.mjs .utmp/g5-release-onscreen .utmp/g5-release-onscreen/summary.json
```

Script mở từng Player ở fullscreen borderless 1920×1080, đợi kết quả và mã thoát, rồi mới mở lượt tiếp. Cửa sổ foreground, HUD và fog đang hoạt động là điều kiện xác nhận. Trong từng frame đo, recorder kiểm focus, kích thước cửa sổ và camera render ra màn hình. Lượt lấy ảnh sau khi đo dùng `ScreenCapture.CaptureScreenshotAsTexture` ở cuối frame. Tool giữ cài đặt vSync/cap đang có và ghi thông số thực tế vào kết quả. [Unity Player arguments](https://docs.unity3d.com/6000.0/Documentation/Manual/PlayerCommandLineArguments.html)

## Workload và dữ liệu

- Ba lượt seed 1, warmup 30 giây rồi ghi 90 giây thời gian thật. Player tự giao ba dân khai thác, mở Tools và huấn luyện một dân qua command hoặc ActionSO hiện có. Thời gian tự nhiên đi qua đêm 1.
- Lượt military seed 1543 lấy từ nhánh có 11 lính ở đỉnh trong ma trận G3. Trong cửa sổ đêm đầu, harness mở Archery, đặt ba lệnh huấn luyện theo khả năng chi trả ở giây 10, 20, 30 và tập kết lính gần Hall. Đây là lát cắt onscreen cùng họ workload, với thời lượng và policy được ghi rõ. Override seed chỉ tồn tại từ khi harness yêu cầu load scene đến khi scene đã load, rồi được trả về cấu hình authored. [Kết quả G3](G3-EconomyExpansion.md)
- Startup đo từ ngay trước yêu cầu `SceneManager.LoadScene` đến lúc composition root đặt `ReadyRealtime` trong cùng process. Năm mẫu seed 1 gồm lần khởi chạy đầu của artifact, ba lượt đo frame và lượt startup riêng cuối. Mỗi mẫu dùng process mới. Không kiểm soát được cache hệ điều hành nên lần đầu được ghi nhãn first launch.
- `SwarmBenchmarkRecorder` ghi `unscaledDeltaTime`, managed GC qua ProfilerRecorder khi khả dụng, đếm quái/đạn và lấy mẫu tổng bộ nhớ Unity. Báo median/p95/p99/max theo nearest-rank. Dùng `1000/60 ms` làm budget tham khảo và xét p95 của từng lượt. Nếu counter Release không khả dụng, trường kết quả là `null` và giá trị thô `-1` được giữ lại.

Kịch bản đo chạy khi có cờ `--progression-smoke --g5-onscreen`. Lệnh chạy thường dùng MainMenu và các scene authored. Bản Release G5 là candidate đo, với nghiệm thu người chơi và đóng gói theo G6.

## Kết quả Release ngày 21/11/2026

Build Windows x64 thành công với Unity 6000.3.11f1, Mono, `BuildOptions.None`, 0 lỗi và 0 cảnh báo theo BuildReport. Thời gian build 254,97 giây. MCP ngắt kết nối khi chờ trả kết quả, sau đó báo cáo trên đĩa và các lượt Player xác nhận artifact đã được tạo thành công. Máy đo dùng Windows 11, Intel Core i5-11300H, GTX 1650 và 16.179 MB RAM. Player chạy D3D11, quality Ultra, fullscreen borderless 1920×1080, 144 Hz, vSync 1, target FPS −1. Mọi frame capture kiểm focus và camera đạt điều kiện. [BuildReport và SHA-256](Evidence/G5-2026-11-21-fingerprints.json)

| Lượt | Frame | Median ms | P95 ms | P99 ms | Max ms | Frame vượt 16,67 ms |
|---|---:|---:|---:|---:|---:|---:|
| Seed 1, lượt 1 | 11.210 | 6,94 | 13,90 | 20,84 | 69,46 | 320 (2,85%) |
| Seed 1, lượt 2 | 10.740 | 6,94 | 13,90 | 20,84 | 111,12 | 432 (4,02%) |
| Seed 1, lượt 3 | 9.533 | 6,94 | **20,83** | 27,78 | 166,66 | 729 (7,65%) |
| Seed 1543, military | 10.633 | 6,94 | 13,90 | 20,85 | 83,34 | 457 (4,30%) |

Ba trong bốn lượt đạt p95 16,67 ms. Lượt seed 1 thứ ba vượt budget, với nhiều frame chậm từ khoảng giây 40 đến 80 và đỉnh 166,66 ms quanh tick 3383 trước đêm 1. Vì vậy độ lặp ở budget 60 FPS chưa đạt trên cấu hình Ultra. Bốn lượt giữ giá trị GC Release `-1` vì counter không khả dụng. Kịch bản military có hai trong ba lệnh warrior được chấp nhận theo tiền thực, kết thúc với 28 actor và 53 hit.

Startup seed 1 theo thứ tự là 863,19 / 875,00 / 1.436,31 / 746,58 / 1.048,10 ms. First launch 863,19 ms. Min/median/max quan sát là 746,58 / 875,00 / 1.436,31 ms. Mốc đo scene request → Ready không gồm thời gian mở process. Release smoke tích hợp `SampleScene` qua 18/18 bài, gồm nộp tài nguyên, xây, huấn luyện, trú đêm và sinh quái. [Kết quả từng lượt và smoke](Evidence/G5-2026-11-21-results.json)

CSV, log và ảnh HUD/fog/quái giữ trong `.utmp/g5-release-onscreen`. Trường văn bản `scenario` trong JSON thô kế thừa mô tả offscreen của harness cũ. Điều kiện chạy thật được kiểm bằng `onscreen=true`, `offscreenRender=false`, `FullScreenWindow`, số frame hợp lệ và ảnh PNG 1920×1080. S9 và G4 dùng offscreen nên đứng riêng trong phân tích. Chưa kiểm trên máy thứ hai.

## Chẩn đoán theo G4

Chạy lại workload seed 1 với Development Player onscreen, cùng Ultra, 1920×1080 và vSync 1. Hai đoạn trace ngắn bật allocation call stack ở khoảng giây 53–62 và 87–99. Dùng `Tools/ReadG4Trace.cs` xuất `.tsv` từ 1.138 frame giữa ngày và 1.351 frame quanh wave đầu. Mã định thời trace chỉ có trong bản Development chẩn đoán. File harness trong workspace đã trở về SHA-256 của candidate Release. [Build, patch tái lập và timeline](Evidence/G5-2026-11-21-development.json)

- Các frame giữa ngày 25,19 / 20,25 / 19,16 ms có `FogOfWarManager.Update` lần lượt 15,26 / 11,66 / 10,81 ms trên main thread. Render thread chờ lệnh từ main thread trong các frame đó. Marker chờ phản ánh trạng thái CPU và không đo GPU time. [Unity Profiler markers](https://docs.unity3d.com/6000.0/Documentation/Manual/profiler-markers.html)
- Ở wave đầu, navigation refresh mất 12,28 ms và cấp phát 18.084 B trong frame 21,07 ms. Đỉnh managed allocation của toàn lượt Development là 36.920 B. Release thiếu counter nên những số này chỉ mô tả Development.
- Frame Release 166,66 ms và cụm chậm ở lượt seed 1 thứ ba chưa tái lập trong lần chẩn đoán. Trace cho thấy fog tốn CPU ở một số frame và cả worker jobs cùng present wait xuất hiện ở các frame chậm khác. Chưa có đủ bằng chứng để gán toàn bộ phần vượt budget cho một subsystem. Cấu hình và gameplay của candidate vẫn theo build đã đo.

Sau khi khôi phục file harness, Unity MCP xác nhận `ProceduralGame` sạch và ở Edit Mode, seed 1 và TownHall 1200 HP. EditMode đạt 81/81. [Trạng thái kết thúc](Evidence/G5-2026-11-21-closure.json)
