# S4 — dữ liệu vùng và preview chiến thuật

Ngày 11/11/2026. **S4 DONE cho Core + preview**. Chưa nghiệm thu gameplay trên map chiến thuật, chưa đổi preset của scene đang chơi.

## Mục tiêu và phạm vi

Tạo cấu trúc có vai trò trước lớp địa hình chi tiết: vùng xuất phát, vùng mở rộng kinh tế và vùng mỏ xa. Hai nhánh đường kết nối vùng ngoài với căn cứ và hai điểm tiếp cận ở biên; landmark được đặt ở vùng mỏ. Preview giúp đọc bố trí và tái lập seed lỗi mà không instantiate prefab, ghi Tilemap hoặc bind ECS.

`MapLayoutPreset.LegacyMainland` là giá trị mặc định 0, giữ preset/luồng RNG cũ với generator version 1. `StrategicRegions` sinh `StrategicLayout` và báo version 2. DefaultProceduralMapSettings của scene hiện vẫn LegacyMainland. Không tự áp map mới vào gameplay trong S4; S5 làm budget/topology cuối và S6 nối scene/wave/navigation.

## Dữ liệu và thuật toán

- Settings thêm Preset và RegionRadius; authoring ToData truyền hai trường vào Core. Validator từ chối preset không biết, radius/corridor không hợp lệ hoặc không đủ khoảng giữa start–vùng ngoài–biên. Cấu hình mặc định 128×128, start radius7, region radius8, corridor half-width2.
- MapData.Layout là null ở legacy, hoặc StrategicLayout ở strategic. Layout chứa Regions[3], Connections[2], Gateways[2], Landmark, RegionIds và Routes. Gateway/landmark dùng tọa độ cell của MapData, chưa phải world position/spawn point hợp collider.
- Layout dùng stream RNG riêng phase `0x5245474E`, seed và attempt. Chọn một trong bốn hướng xoay; hai vùng ngoài nằm trên hai hướng vuông góc, có jitter ngang nhỏ. Khoảng cách lấy theo chiều map, trừ radius và khoảng đệm biên.
- Gán mỗi cell vào tâm vùng gần nhất (khoảng cách bình phương; tie giữ region index thấp). Rasterize đĩa đất quanh các tâm. Hai graph edges start→economy và start→gold được rasterize thành hành lang chữ L có bề rộng `2*halfWidth+1`, nối tiếp từ tâm ngoài tới gateway ở biên.
- Vùng xuất phát vuông, hành lang và footprint landmark được giữ trong Reserved, tránh resource placement chiếm chỗ. Noise vẫn quyết định phần đất/nước ngoài mask. Strategic không ép dải đất chạy quanh toàn biên và không dùng hành lang cố định sang phải của legacy.
- BFS loại đất cô lập, rồi placement/validator cũ tiếp tục kiểm quota và lối tương tác sau placement. RegionIds/Routes không sinh Entity; Core vẫn chỉ phụ thuộc Mathematics.

Thời gian layout tỷ lệ số cell ×3 tâm và chiều dài đường × footprint corridor; vẫn có MaxAttempts/placement attempt hữu hạn của generator. Không thêm thư viện graph/Voronoi. Cấu trúc dữ liệu và cách repair hiện là blockout, không khẳng định tạo bản đồ cân bằng hoặc đúng hai tuyến quái độc quyền.

## Preview và cách chạy

Unity menu **RTS → Procedural Generation → Preview Regions**. Chọn asset settings tùy ý hoặc dùng defaults; Seed/Preset ở cửa sổ chỉ override dữ liệu bản sao cho preview, không ghi vào asset. Bấm **Sinh preview**. Thử seed 0 và 1543, chuyển LegacyMainland/StrategicRegions để so sánh. Panel hiển thị seed, attempt, version, resource count, repairs và lỗi. Cấu hình lỗi xóa preview cũ, không giữ ảnh thành công khiến người dùng hiểu sai.

Màu: vàng sẫm=Start, xanh=Economy, cam=GoldFrontier, xanh dương=water, trắng xám=route, đỏ=gateway, tím=landmark, cyan=tâm vùng; chấm xanh đậm/vàng/trắng=cây/vàng/cừu. Đây là resource placement cũ, chưa phản ánh budget riêng theo vai trò.

- [Seed0 strategic](../Progression/S4-preview-seed0.png)
- Cùng seed1543: [Legacy](../Progression/S4-preview-LegacyMainland-seed1543.png) và [Strategic](../Progression/S4-preview-StrategicRegions-seed1543.png)

Các ảnh xuất 512×512 bằng nearest-neighbor từ preview128×128; màu đúng dữ liệu Core, không phải ảnh gameplay. Trong phiên đã mở cửa sổ và Generate cả hai preset, kiểm lỗi rồi sinh lại thành công. Scene không dirty; runtime asset vẫn legacy.

## Kiểm thử và bằng chứng 11/11/2026

- Unity `RTS.ProceduralGeneration.Tests`: **10/10 pass**, 0 fail/skip, 7,396 giây theo Test Runner; chưa có XML mới.
- Tests mới: cùng seed 0/-1/int.MinValue/1543 tái lập terrain/reserved/resources, toàn bộ region/connection/gateway/landmark/masks; không đặt tài nguyên trong Reserved; landmark xa start; legacy version1/Layout=null; strategic không đủ chỗ bị từ chối trước attempt; lỗi quota kết thúc sau MaxAttempts=1.
- Công cụ .NET đã thêm file StrategicLayout vào csproj và so sánh toàn dữ liệu mới. Lệnh thực chạy:

```powershell
dotnet run --project Tools/ProceduralGenerationSeedCheck/ProceduralGenerationSeedCheck.csproj -- 128 strategic
dotnet run --project Tools/ProceduralGenerationSeedCheck/ProceduralGenerationSeedCheck.csproj -- 128 legacy
```

| Run Core | Số seed | Lỗi | Thời gian trung bình / chậm nhất do tool báo |
|---|---:|---:|---|
| StrategicRegions, seed0..127 | 128 | 0 | 13,66 ms / 304 ms seed0 |
| LegacyMainland, seed0..127 | 128 | 0 | 10,65 ms / 72 ms seed0 |

Mỗi seed được sinh lại để so sánh dữ liệu. Các thời gian là diagnostics Core .NET trong process có JIT; không gồm Tilemap/prefab/physics/bridge/rendering, không có kiểm soát phần cứng/warmup để coi là benchmark so sánh. Không dùng làm thành tích hiệu năng. Chưa thay đợt quét1000 seed ở S5.

## Giới hạn và điểm tiếp S5/S6

1. Vai trò vùng hiện là dữ liệu/preview; rừng/vàng/cừu vẫn theo quota toàn map. S5 chuyển budget vùng và biến landmark thành cụm vàng có lợi ích gameplay.
2. Gateway là điểm đề xuất, hành lang không chặn mọi đường phụ tự nhiên từ noise. S5 phải kiểm topology sau toàn bộ placements/clearance; S6 nối EnemyWaveManager và cả hai navigation system. Không khẳng định quái hiện chỉ đi qua hai cửa.
3. Corridor chữ L/đĩa đất còn hình học rõ; art/biome/địa hình động không thuộc S4. Có thể điều chỉnh hình dạng sau khi topology và gameplay được đo, giữ seed/version khi sửa thuật toán.
4. Chưa spawn quái/worker trên strategic map trong Unity, chưa kiểm click/fog/minimap trên preset mới, chưa Player build/scene→Ready benchmark.
5. MapData chưa có save/load và version chỉ mô tả thuật toán hiện tại; nếu thay layout/placement phải nâng version và chạy lại kiểm chứng.

Nguồn nội bộ: [Architecture](Architecture.md), [Research](Research.md), [BuildPlan S4/S5](../Progression/BuildPlan.md); code MapGenerationData.cs, StrategicLayout.cs, ProceduralMapGenerator.cs, ProceduralMapPreviewWindow.cs, ProceduralMapSettings.cs và Tools/ProceduralGenerationSeedCheck. Thiết kế dùng nearest-center partition, RNG stream và BFS hiện có; không tuyên bố triển khai đầy đủ Voronoi/Delaunay hoặc WFC.
