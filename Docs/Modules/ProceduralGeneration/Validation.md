# Kiểm chứng Procedural Generation

**17/11 — S9 Player/bàn giao:** [S9](../Progression/S9-Acceptance.md) ghi build mới,81regression/18Player smoke và capture seed1/v3/world render;Core102.3715ms,Terrain112.0096ms,Prefab24.0047ms,Physics/pathfinding25.1017ms,load→Ready836.3275ms trong một lần Player. Không gọi đó là median/p95startup hoặc áp mọi seed. S7/S8 user đã xác nhận checklist, D4 runtime/legacy integration đã kiểm. Các trạng thái chưa Player/perf bên dưới là lịch sử.

Cập nhật **16/11 — S7 DONE:** [Evidence/ảnh](S7-Presentation.md): seed1/0/1543 có16/15/14decor/1watchpost, unknown→reveal→explored glyph/silhouette PASS; collider/Unit0/pad clear/actor25/resources150/repeat init/nav stable. Seed1 input/gather/deposit/build PASS;4ảnh final cùng camera/seed ngày-đêm inspect, user báo “Ổn, dễ nhận ra”. Regression59/59DOTS2,1626465s và17/17procedural5,3338967s,0fail/skip. Chưa Player/perf/fullbalance, không biến thời gian mô phỏng thành FPS.

Cập nhật đóng **16/11/2026 — S6/D3 DONE:** [S6 final](S6-FinalValidation.md) và [raw log](S6-FinalChecks-2026-11-16.txt) ghi matrix0/1/1543 economy/build/fog-camera/input +3chu kỳ đêm PASS. Manual được người dùng xác nhận; không ảnh kèm. Worker pool qua menu→game mới PASS;17/17procedural mới8,1212562s,59/59DOTS sau code cuối giữ nguyên. Editor sạch/seed1/ngoài Play-pause-compile,0Exception mới. Tiếp S7; chưa Player/balance/performance.

## D3c2 — 15/11/2026

Procedural seed1 suppression/repeat/missing config/dependency replacement PASS, count120/12/18và25actors/150resourceEntities. SampleScene regenerate nhiều lượt giữ30/20/18và25actors/68resourceEntities; view/Entity cũ dọn, worker cancel, node mới inject/nav/collider PASS; giao cây mới nộp20gỗ. Menu reload PASS; regression sau code cuối58/58 DOTS +17/17 procedural,0fail/skip. [Evidence/checklist manual](../DependencyInjection/D3c2-ProceduralSpawners.md) ghi rõ API tests, thời gian mô phỏng và phím P/click chưa xác nhận; chưa Player/benchmark mới.

## S6c — cuối ngày13/11/2026

Ba seed0/1/1543, strategic và legacy cùng RNG wave2026:12spawn/3wave/45giây, mốc0,02/14,10/30,20giây, retreat/untargetable và hếtagents sau11,98giây. Strategic approach arrival12/12/9 so legacy10/11/8; không dùng arrival để khẳng định damage cân bằng. Menu→game→menu→game và S6 smoke PASS; cờ map cũ khi về menu đã được sửa. Regression sau code mới58/58 DOTS,17/17 procedural,0fail/skip. [S6c Evidence](S6c-WavesLifecycle.md) và [raw log](S6c-WaveChecks-2026-11-13.txt) ghi phương pháp/những phần chưa kiểm; S6/D3 tổng thể PARTIAL.

## S6b/D3b — 13/11/2026

Ba seed0/1/1543: worker tới Economy lấy gỗ/GoldFrontier lấy vàng và nộp20mỗi loại, ví120/120/100; StoragePit tại pad landmark trừ100/100,25→26actors, hoàn tất xây; fog/minimap/camera API PASS.132resource node/seed và50nodes SampleScene injection/claims/nav/rebind/destroy PASS. DOTS58/58, procedural17/17,0fail/skip. [S6b Evidence](S6b-EconomyResourceDI.md) ghi timing, phương pháp tick riêng, precondition view Start và phần wave/DI chưa hoàn tất; chưa Player build hoặc benchmark.

## S6a/D3a — 12/11/2026

Unity MCP Play checks seed0/1/1543: gateway/radius spawn, resource counts,25actors/một bridge, readiness/reapply và repeat-init/listener đều PASS. Hai probe/seed tới phạm vi10 quanh TownHall trong khoảng25,65–26,90giây mô phỏng. SampleScene legacy bootstrap/spawn PASS. Regression EditMode: DOTS58/58 (2,278giây), procedural17/17 (7,363giây), không fail/skip. [S6 Integration](S6-Integration.md) ghi phương pháp, phép thử chưa đạt khi chạy sau smoke, crash cũ và giới hạn; S6/D3 tổng thể vẫn PARTIAL, chưa nghiệm thu worker/nộp/xây/fog/wave thật hoặc hiệu năng.

Ngày kiểm chứng: **29/10/2026**.

## Kiểm tra bổ sung ngày 05/11/2026

Trên scene `ProceduralGame`, Unity MCP xác nhận trước khi sửa rằng BoxCollider2D trigger của `ResourceManager` phủ gần toàn bản đồ (59,83 × 49,09 đơn vị) và được `Physics2D.Raycast` chọn trước collider của cả ba Worker. Ở tâm collider nhận click, 18/39 cây cũng bị raycast chọn nhầm `ResourceManager` hoặc cây chồng lấn khác. Khi click rơi vào `ResourceManager`, `GameManager` xử lý như click nền và biểu tượng bị đặt tại ô đi được gần đó.

Sau sửa, `Assembly-CSharp.csproj` biên dịch thành công với 0 lỗi/0 warning. Trong Play Mode, `ResourceManager` ở layer `Ignore Raycast`, collider vùng sinh đã tắt; raycast tại Worker và một cây procedural trả về đúng collider của chúng. Gọi `WorkerUnit.TryAssignResourceNode` trên một cây gần worker trả `true`; sau khi bỏ Pause của Editor, ECS ghi nhận worker ở state `Chopping`, đang mang 16 gỗ. Đã thoát Play Mode sau kiểm tra. Chưa tự động hóa thao tác click chuột thật hoặc chạy toàn bộ Unity Test Runner.

## Kết quả đã chạy

Môi trường dòng lệnh: Windows, .NET SDK 9.0.309. Preset Core: 128 × 128, 4 octave, 120 cây, 12 vàng, 18 cừu, tối đa 8 attempt.

Lệnh:

```powershell
dotnet run --project Tools/ProceduralGenerationSeedCheck/ProceduralGenerationSeedCheck.csproj -- 1000
```

Kết quả:

| Chỉ số | Giá trị đo |
|---|---:|
| Seed kiểm tra | 1.000 (`0..999`) |
| Seed lỗi | 0 |
| Thời gian Core trung bình | 16,73 ms/seed |
| Lượt Core chậm nhất | 321 ms, seed 0 |
| Kiểm tra xác định | Mỗi seed sinh lại lần hai và so sánh terrain, reserved mask, attempt và toàn bộ placements |

Mỗi lần sinh thành công đã đi qua validator: đủ quota, vùng bắt đầu/hành lang còn tới được và từng tài nguyên có ô tương tác tới được. Với tổng 12 vàng, thuật toán phân bổ hai placement cho mỗi `VariantId` từ 0 đến 5.

Artifact văn bản: [Evidence/seed-check-2026-10-29.txt](Evidence/seed-check-2026-10-29.txt).

Assembly runtime và Editor đã được kiểm tra biên dịch bằng project C# do Unity sinh, sau khi tạm đưa các file mới vào danh sách compile vì Unity chưa được mở để refresh project. Kết quả là 0 warning, 0 error; thay đổi tạm trong các file `.csproj` đã được hoàn tác. Đây là kiểm tra compile C#, không thay thế import/Play Mode của Unity.

## Kiểm thử đã viết nhưng chưa chạy trong Unity

`ProceduralMapGeneratorTests` có ba nhóm kiểm tra:

- cùng seed sinh cùng terrain, mask và placements;
- 32 seed preset mặc định đạt quota, kết nối và phân bổ cân bằng sáu biến thể vàng;
- cấu hình `StartingGoldCount > TotalGoldCount` bị từ chối trước khi sinh.

Unity Test Runner chưa được chạy trong lần này vì máy hiện tại không tìm thấy Unity Editor executable. Không có XML Test Runner mới để công bố.

## Phần chưa kiểm chứng

Tại thời điểm 29/10/2026, chưa tạo `ProceduralGame.unity` bằng menu và chưa chạy Play Mode. Vì vậy chưa xác nhận trực quan RuleTile/bờ nước, prefab anchor/collider, số Entity đăng ký, khai thác/xây/huấn luyện, wave, fog, minimap, camera hoặc tổng thời gian từ bắt đầu scene đến `Ready`. Phần kiểm tra bổ sung ngày 05/11 ở trên chỉ xác nhận các đường raycast và khai thác đã nêu.

Số 16,73 ms chỉ đo Core trong process .NET có cả ảnh hưởng khởi động/JIT; không gồm ghi Tilemap, instantiate 150 GameObject, `Physics2D.SyncTransforms`, rebuild navigation, DOTS registration hoặc rendering. Chưa đo median/p95, allocation hay Player build nên không dùng con số này làm cam kết hiệu năng gameplay.

## Cách hoàn tất nghiệm thu

Mở đúng Unity version, chạy menu tạo scene, chạy EditMode tests, rồi thực hiện checklist trong [Usage](Usage.md). Với lỗi gameplay, ghi lại seed, settings, log `[ProceduralMap]`, ảnh scene và XML Test Runner. Đo Player build nhiều lần trước khi chốt quota/cân bằng hoặc ngân sách thời gian.
# Kiểm chứng bổ sung S4 — 11/11/2026

**Cập nhật S5 — 12/11/2026:** Unity MCP procedural tests **17/17 pass**; Core seedcheck **1000strategic +1000legacy**,0lỗi/tái lập. [S5 evidence](S5-ResourcesTopology.md), [log](S5-SeedCheck-2026-11-12.txt). Strategic đã version3; scene gameplay vẫn legacy; không suy diễn đã kiểm collider/wave/fog thực tế của preset mới.

Unity procedural EditMode **10/10 pass**; Core tool quét và tái sinh **128 strategic +128 legacy**, mỗi preset 0 lỗi. Preview lỗi không giữ ảnh cũ; Generate hai preset và kiểm PNG, scene không dirty, default gameplay asset vẫn legacy. [Chi tiết S4](S4-Regions.md) ghi lệnh, thời gian Core, ảnh và các phần topology/gameplay chưa được kiểm chứng. Kết quả này không chứng nhận strategic gameplay scene hoặc budget theo vùng; chúng thuộc S5/S6.
