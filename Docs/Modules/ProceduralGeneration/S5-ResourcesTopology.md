# S5 — budget tài nguyên và topology sau placement

Ngày 12/11/2026. **S5 DONE trong phạm vi Core/preview**. StrategicRegions nâng generator version **3**; LegacyMainland vẫn version1 và vẫn là preset gameplay hiện tại. Gameplay/wave/navigation thực tế chờ S6.

## Mục tiêu và dữ liệu

Map mới cần tạo quyết định mở rộng: vùng Economy ưu tiên gỗ/thịt, vùng GoldFrontier giữ vàng; landmark có cụm vàng và khoảng trống xây. Giữ nguyên tổng quota, prefab catalog và quota khởi đầu. Không coi số placement là trữ lượng tài nguyên hữu hạn.

StrategicResources.cs là phần partial của ProceduralMapGenerator để tái sử dụng footprint, occupancy, BFS và PlaceQuota hiện có. Không thêm dependency từ Core sang UnityEngine/GameManager/ECS. PlaceQuota thêm filter region tùy chọn; nhánh legacy không dùng filter nên giữ thứ tự RNG và placement cũ. Tools seedcheck được thêm file Core mới vào csproj.

Với preset mặc định 120 cây/12 vàng/18 cừu và quota khởi đầu30/3/6:

| Phần | Cây | Vàng | Cừu |
|---|---:|---:|---:|
| Quota khởi đầu theo khoảng BFS cũ | 30 | 3 | 6 |
| Economy (region1) | 68 | 0 | 9 |
| GoldFrontier (region2) | 22 | 9 | 3 |

Quy tắc chung: 75% cây và cừu ngoài khởi đầu vào Economy, làm tròn lên; phần còn lại vào GoldFrontier. Tất cả vàng ngoài khởi đầu vào GoldFrontier. Quota khởi đầu vẫn được chọn bằng đường đi gần căn cứ trước, không bị buộc vào một region label; số liệu bảng không khẳng định mỗi node khởi đầu nằm trong region0.

Landmark có pad **5×5** được giữ chỗ, ba mỏ vàng footprint **3×3** chọn ba vị trí hợp lệ trong bốn góc lệch±4 cell, ClusterId400; không đè hành lang hoặc pad. Những node này nằm trong tổng quota, variant vàng vẫn phân bổ tuần tự modulo6. Validator từ chối strategic không có ít nhất ba node vàng ngoài khởi đầu hoặc RegionRadius<8. Không hạ quota khi không đủ chỗ; giữ retry hữu hạn.

## Luồng sinh và kiểm tra cuối

1. Noise, layout phase riêng, carve start/vùng/đường như S4; thêm dải nước biên dày2 cell trước carve gateway. Chỉ các dải gateway mới mở ra biên, bỏ các lối vào biên ngẫu nhiên không đăng ký.
2. BFS và đặt quota khởi đầu như cũ. Đặt ba node landmark trước; cây/vàng/cừu ngoài đặt bằng quota và region filter, vẫn dùng khoảng cách tối thiểu và occupancy.
3. Validator quota/resource access cũ chạy sau placement. Validator strategic chạy tiếp trên terrain + footprint tài nguyên cuối, không chỉ dựa vào Routes metadata.
4. Footprint từng resource phải nằm trên đất, không đè Reserved hoặc resource khác. Tree block1cell, Gold block3×3, Sheep không block đường nhưng vẫn kiểm overlap placement.
5. BFS4hướng tìm đường tới từng ô tương tác resource. Báo cáo thêm MaxResourcePathDistance là khoảng BFS dài nhất tới ô tương tác gần nhất; đây là cell-distance từ start, chưa là thời gian mang/nộp thực tế từ depot được xây giữa trận.
6. Erode walkable bằng footprint **3×3** (tâm actor với clearance1cell), rồi BFS từ start tới cả hai gateway và landmark. Gateway ở biên cho phép phần footprint ngoài map, còn mọi cell trong map phải trống; S6 cần dịch sang world/spawn center hợp collider thật.
7. Kiểm start pad và landmark pad vẫn trống/Reserved; bỏ footprint công trình đề xuất5×5 và yêu cầu vòng thoát ở radius3 còn có đường. Đây là footprint thử nghiệm, S6 đối chiếu prefab TownHall/depot thực tế.
8. Mọi ô biên còn tới được phải thuộc bề rộng gateway đã khai báo. Report.ReachableGateways phải là2 cho preset được xuất. Không cho tài nguyên bịt đường bảo vệ rồi vẫn báo map hợp lệ.

Các check có thể phát hiện invalid map và loại attempt, không repair bằng giảm tài nguyên. Region placement sử dụng số lần thử cũ; mỗi generator attempt bị giới hạn. Graph có hai cửa biên thực sự trong grid sau placement, nhưng không khẳng định có đúng hai hành lang độc quyền bên trong: noise có thể tạo nhánh nối khác. S6 mới quyết định spawn/wave routes.

## Cách chạy và kiểm chứng

Unity menu **RTS/Procedural Generation/Preview Regions**, chọn StrategicRegions, seed0 hoặc1543, Sinh preview. Panel báo version3, số cửa tới được và max resource path. Runtime settings asset hiện vẫn legacy, preview không ghi scene hoặc Instantiate resource.

- [Ảnh preview S5 seed0](../Progression/S5-preview-seed0.png), xuất512×512 nearest-neighbor từ128×128; đã mở và kiểm: cụm vàng quanh landmark, hai cửa đỏ, vùng Economy nhiều cây/cừu hơn.
- Seed0 thực tế: attempt0, version3, ReachableGateways2, MaxResourcePathDistance118, GoldCount12.
- Unity MCP `RTS.ProceduralGeneration.Tests` code cuối: **17/17 pass**, 0 failed/skipped, Test Runner báo **8,007 giây**. Chưa xuất NUnit XML.
- Test fixtures mới: budget region/mỏ landmark; tree-wall cắt lối, mỏ bị nước chạm footprint, chỉ nối chéo, landmark bị vòng nước cô lập, cửa biên ngoài đăng ký, quá ít vàng; case quota quá lớn/attempt hữu hạn từ S4 vẫn chạy. Fixtures cố tình sửa dữ liệu rồi public ValidateStrategicMap từ chối.
- [Báo cáo seedcheck](S5-SeedCheck-2026-11-12.txt): **1.000 strategic và 1.000 legacy**, seed0..999; mỗi seed sinh lại so sánh mọi field layout/placements; 0 lỗi. Strategic attempt0/1/2/3/4 lần lượt822/146/27/4/1; legacy901/90/8/1.

```powershell
dotnet run --project Tools/ProceduralGenerationSeedCheck/ProceduralGenerationSeedCheck.csproj -- 1000 strategic
dotnet run --project Tools/ProceduralGenerationSeedCheck/ProceduralGenerationSeedCheck.csproj -- 1000 legacy
```

Tool có in toàn bộ seed lỗi nếu có, histogram accepted attempts và rerun final validator. Trong đợt này không có seed lỗi để lưu. Sandbox chặn ghi obj ở lần chạy CLI đầu, đã chạy lại cùng lệnh với quyền ghi build output; một đợt Unity script quét250seed quá60s không hoàn tất và không được dùng làm kết quả. Test/preview ở trên vẫn chạy trực tiếp trong Editor qua MCP.

## Hiệu năng và giới hạn

Core tool báo strategic trung bình25,25ms, chậm nhất142ms (seed204), legacy14,55ms/61ms(seed0). Những số này là diagnostics .NET gồm ảnh hưởng JIT/process; không có phép đo kiểm soát phần cứng, median/p95, Tilemap/prefab/Physics2D, bridge, rendering hoặc Player. Không dùng làm cam kết hiệu năng RTS.

Topology hiện dùng footprint cell quy ước; không đọc collider hoặc offset prefab trong Core. Range khởi đầu vẫn chọn theo BFS terrain trước placement; sau placement kiểm tồn tại đường và ghi khoảng vận chuyển thực tế, chưa buộc max distance sau mọi vật cản phải bằng interval ban đầu. Không coi quota/richness là cân bằng đã đạt.

S6 phải áp strategic scene, nối hai gateway với EnemyWaveManager, kiểm cả grid Gameplay và Swarm, fog/minimap/camera, Unit/resource bind và prefab footprints. Chưa có ảnh gameplay/wave/player smoke trên strategic map, chưa thay default scene sang strategic. DI của wave/factory/Unit vẫn theo D3/D4.

Nguồn nội bộ: [BuildPlan S5/S6](../Progression/BuildPlan.md), [S4](S4-Regions.md), [Architecture](Architecture.md); code StrategicResources.cs/StrategicLayout.cs/ProceduralMapGenerator.cs/MapGenerationData.cs và Tests/Editor/ProceduralMapGeneratorTests.cs. Không thêm thư viện hoặc thuật toán học máy.
