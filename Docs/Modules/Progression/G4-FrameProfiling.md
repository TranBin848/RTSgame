# G4 — Profile frame và GC trên Development Player

**21/11/2026 — G4 DONE trong phạm vi spike navigation.** G4 dùng cấu hình chơi thử của G3 với TownHall 1200 HP. Nghiệm thu chơi thực tế của G3 vẫn đang chờ. [Kế hoạch](NextPhasePlan.md) · [Cấu hình G3](G3-EconomyExpansion.md)

## Mục tiêu và workload

Đo phân phối thời gian frame và managed allocation trong Windows Development Player. Dùng trace ngắn để đối chiếu spike với công việc trên main thread, render thread và job, trước khi thay đổi runtime. Bản đo dùng `MainMenu` → `ProceduralGame`, seed map 1, generator v3, và khởi tạo `UnityEngine.Random` bằng 8001 trước các lệnh thử. RNG dùng chung với các hệ thống Unity khác nên bằng chứng chỉ áp dụng workload đã ghi. Harness dùng lệnh gameplay để giao ba dân khai thác, mở Tools và huấn luyện một dân. Nhịp ngày đêm chạy ở time scale 1.

## Cách chạy

Build bằng Unity `BuildPipeline.BuildPlayer`, target Windows x64, `BuildOptions.Development` và danh sách scene enabled hiện có. Dùng quy trình gọi từ Unity MCP trong `Tools/BuildS9.cs` với thư mục output riêng cho G4. Chạy Player với `--progression-smoke --g4-measure --swarm-output "<thư-mục-kết-quả>"`. Recorder lấy mẫu sau 2 giây và kéo dài 113 giây, gồm huấn luyện và đêm đầu. Game chạy với camera render sang RenderTexture 1920×1080 và ghi ảnh sau capture.

Dùng `--g4-trace` thay `--g4-measure` cho lượt chẩn đoán. Player ghi raw nhị phân trong hai cửa sổ, quanh lúc giao việc/huấn luyện và quanh wave đầu. `RTS.NightTransition`, `RTS.WaveStarted`, `RTS.TrainingComplete`, `RTS.DepositPopup`, `RTS.ActionQueueComplete` và `RTS.RefreshNavigation` đánh dấu các đường tích hợp. Các trace này bật allocation call stack. Load raw qua Unity Profiler hoặc chạy `Tools/ReadG4Trace.cs` bằng Unity MCP, class `G4Trace`, method `Run`, tham số `directory` là thư mục raw. Công cụ xuất sample theo frame, thread, inclusive time, self time, managed bytes và call stack khi có.

Dùng `node Tools/SummarizeG4.mjs <thư-mục-kết-quả> <summary.json>` cho lượt `--g4-measure`. Công cụ đối chiếu cấu hình, kết quả thực thi, số dân, thời lượng, enemy xuất hiện và dữ liệu của từng frame. Percentile dùng nearest-rank. `GC Allocated In Frame` là managed bytes, còn số Unity allocated bytes lấy mẫu 1 Hz gồm bộ nhớ do Unity theo dõi. Hai phép đo phản ánh hai đại lượng khác nhau.

## Nguyên nhân và thay đổi

Ở bản trước sửa, trace của đêm 1 ghi `RTS.NightTransition` ở frame 548 và `RTS.WaveStarted` ở frame 549. Trong frame 549, `RTS.RefreshNavigation` chiếm 157,14 ms của 168,00 ms và cấp phát 1.016.860 B managed. Call stack đi qua `SwarmGameBridge.RefreshNavigation` → `TilemapManager.CamWalkAtTile` → `isBlockByBuilding`. Có 627.312 B ở lệnh lấy layer mask cho 11.202 ô và 371.464 B ở mảng kết quả `Physics2D.OverlapBoxAll` cùng marker. Khi đổi pha, worker đổi claim và collider tài nguyên, làm `NavigationVersion` tăng. Bridge làm mới grid ở frame kế tiếp. `RTS.WaveStarted` cùng frame chỉ chiếm khoảng 0,21 ms. Lượt đo riêng với Profiler ghi trace tắt ghi 1.020.416 B ở tick 5400. Hai lượt là hai lần chạy khác nhau.

`TilemapManager` giữ layer mask từ lần dùng đầu tiên và truyền một `List<Collider2D>` tái sử dụng vào overload `Physics2D.OverlapBox`. Danh sách có thể tăng khi số collider đồng thời vượt capacity đã có. Filter lấy `Physics2D.queriesHitTriggers` ở thời điểm gọi. Cách kiểm tag `Building` và `Resource`, việc bỏ trigger click của tài nguyên, tâm hộp và kích thước truy vấn đều theo luồng cũ. Cả hai ECS system vẫn nhận grid sau khi cập nhật collider, giữ nguyên vòng đời bộ nhớ và đường đi.

Trong cửa sổ đầu, `RTS.TrainingComplete` frame 1920 chiếm 3,83 ms và cấp phát 12.802 B, gồm 160 B trong `RTS.ActionQueueComplete`. Các frame lớn khác trong trace có chi phí `FogOfWarManager.Update` và đợi ECS jobs. Thay đổi G4 tập trung vào nguồn cấp phát đã truy vết qua navigation.

## Đối chiếu trace của wave đầu

| Chỉ số | Trước | Sau |
|---|---:|---:|
| Frame chứa sự kiện | 549 | 508 |
| Managed allocation trong `RTS.RefreshNavigation` | 1.016.860 B | 18.084 B |
| Managed allocation của truy vấn từng ô | 998.776 B | 0 B |
| Thời gian marker khi bật allocation call stack | 157,14 ms | 24,51 ms |
| Thời gian frame chứa marker, khi bật trace | 168,00 ms | 63,44 ms |

Managed allocation giảm 98,2% trong marker. Số ms của trace chịu chi phí thu call stack, đặc biệt khi bản trước sửa có hơn 22.000 mẫu cấp phát trong một frame. Lượt đo riêng dùng để so phân phối frame khi tắt ghi trace. Sau sửa, trace cùng frame vẫn có 18,41 ms self time ở fog và có dấu đợi job. Chi phí xây lại native grid, collider proxy, render và UI vẫn nằm trong workload. [Trace trước](Evidence/G4-2026-11-21/trace-before.json) · [Trace sau](Evidence/G4-2026-11-21/trace-after.json)

## Đo frame khi tắt ghi trace

Máy đo dùng Intel Core i5-11300H, 8 logical cores, GTX 1650 và 16.179 MB RAM. Hai build dùng Unity 6000.3.11f1, Windows Development Player, Direct3D 11, 1920×1080, cấu hình vSync 1 và target FPS −1. Cửa sổ Player được chạy ẩn, camera dùng render request riêng. Cả hai lượt có 26 actor, 4 dân, 9 quái đã sinh và 20 hit ở thời điểm kết thúc. Các file fingerprint chung khớp giữa hai build, trừ `TilemapManager.cs` và assembly biên dịch chứa thay đổi.

| Chỉ số | Trước sửa | Sau sửa |
|---|---:|---:|
| Frame hợp lệ trong khoảng 113 giây | 24.845 | 24.830 |
| Frame ms median / p95 / p99 / max | 3,48 / 9,62 / 18,38 / 76,70 | 3,53 / 9,43 / 17,67 / 116,49 |
| GC B/frame median / p95 / p99 / max | 104 / 144 / 232 / 1.020.416 | 104 / 144 / 232 / 34.390 |
| GC ở tick 5400, frame đầu có quái | 1.020.416 B | 21.648 B |
| Frame ở tick 5400 | 23,01 ms | 17,33 ms |
| Unity allocated bytes, mẫu đầu → cuối | 257.665.690 → 258.510.762 | 257.715.198 → 258.494.560 |

Đỉnh GC toàn cửa sổ giảm 96,6%. Ở sự kiện kiểm riêng tại tick 5400, GC giảm 97,9%. Giá trị Unity allocated bytes là snapshot 1 Hz, gồm nhiều nhóm bộ nhớ và không cho biết chi phí churn của native grid trong một frame. [Đo trước](Evidence/G4-2026-11-21/measure-before.json) · [Đo sau](Evidence/G4-2026-11-21/measure-after.json)

Lượt lặp dùng cùng bản sau sửa, 26.626 frame trong 112,997 giây, vẫn có 26 actor, 4 dân, 9 quái đã sinh và 20 hit. Frame ms median/p95/p99/max là **3,30 / 8,71 / 16,06 / 72,30**. GC B/frame median/p95/p99/max là **104 / 144 / 232 / 34.390**. Đỉnh trong khoảng tick 5399–5401 lặp lại ở **21.648 B**. Spike **116,49 ms** trong lượt A/B sau sửa nằm ở tick 1712 với 144 B GC. Trong khoảng tick 1680–1740, lượt trước sửa dừng ở 20,71 ms và lượt lặp sau sửa ở 14,60 ms. Spike 116,49 ms chưa tái lập và chưa có attribution, nên vẫn được giữ trong bảng. [Lượt lặp sau sửa](Evidence/G4-2026-11-21/measure-after-repeat.json)

## Kiểm thử và vùng chưa đo

- Qua Unity MCP, `Tools/ValidateG4Navigation.cs` so từng ô đang đi được trên map seed 1 với phép gọi cũ ở cả hai trạng thái `queriesHitTriggers`. 22.404 phép so khớp. Fixture chồng 32 trigger tài nguyên kiểm trường hợp crowded, layer ngoài mask, trigger công trình và collider bị tắt. Quét collider đã warm-up ghi 0 B managed trên main thread.
- Toàn bộ 81/81 EditMode qua. Kiểm riêng `Tools/ValidateS9ResearchAllocations.cs` cho 1.000 lượt polling idle và 1.000 lượt lúc panel khác đang nghiên cứu, đều 0 B. Số đó chỉ áp dụng polling và không gồm label đổi theo tiến độ, layout hoặc render.
- Development Player chạy smoke tích hợp `SampleScene` đạt 18/18, gồm di chuyển, claim và nộp hàng, xây, huấn luyện, trú đêm và sinh quái. Unity MCP xác nhận `ProceduralGame` sạch, ở Edit Mode, seed 1 và TownHall 1200 HP sau kiểm thử. [Player smoke](Evidence/G4-2026-11-21/gameplay-smoke-after.json) · [Trạng thái Unity và EditMode](Evidence/G4-2026-11-21/closure-state.json)
- Các đoạn trace xuất qua Profiler giữ tối đa 2.000 frame cuối mỗi file trên Editor này. Marker `RTS.DepositPopup` đã có trong luồng ECS, nhưng không có mẫu trong phần trace đã xuất. Chưa có số GPU time hoặc call stack native allocator riêng. G4 dùng CPU markers, managed counters và tổng allocated để mô tả các chi phí đã quan sát. Release, HUD onscreen và nghiệm thu G3 là các phần riêng.

Các CSV gốc, log và trace nhị phân nằm trong `.utmp/g4-before-measure`, `.utmp/g4-after-measure`, `.utmp/g4-repeat-measure`, `.utmp/g4-before-trace` và `.utmp/g4-after-trace`. [Build và fingerprint](Evidence/G4-2026-11-21/)

Nguồn API Unity cho [ghi trace nhị phân](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Profiling.Profiler-enableBinaryLog.html), [allocation call stack](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Profiling.Profiler-enableAllocationCallstacks.html) và [đọc sample](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/profiling/rawframedataview).
