# Sử dụng Procedural Generation

Cập nhật **16/11 — S7:** ProceduralGame root cần `Map Presentation` và `Minimap`; component presentation cần3texture TinySwords có sẵn và shared material Tree. `ProceduralMapEditorTools.ConfigureMapPresentation` wire cấu hình này, menu refresh scene cũng gọi helper. World watchpost là decor trung lập cạnh pad, không phải Unit có thể chọn; tree/watchpost glyph chỉ hiện sau khám phá và giữ khi đã explored. [S7 evidence](S7-Presentation.md) có cách chạy ValidateS7/capture trước-sau, nightly values và nguồn asset. Mặc định fog bật/seed1; fog-off/camera lock trong ảnh chỉ là setup test, không save runtime scene.

Cập nhật đóng **16/11/2026 — S6/D3 DONE:** xem [S6 final](S6-FinalValidation.md) để chạy `Tools/RunS6SeedChecks.ps1 -Seed 0`, `-Seed 1`, `-Seed 1543` từng lần trong paused Play, restore Seed1 khi kết thúc; không SaveAssets cho Seed tạm. Unit/pool mới phải bridge.Register sau acquire, không tự tìm manager trong Awake/Start. Matrix/manual/pool qua reload và17procedural đã PASS; [Progress](../Progression/Progress.md) chuyển sang S7. Các mục chờ trong checkpoint cũ dưới đây mang tính lịch sử.

Cập nhật **15/11/2026:** ProceduralMapAuthoring phải có danh sách `Legacy Spawners` đúng scene (ProceduralGame hiện1ResourceManager); menu scene generator cũng gán refs này. ResourceSpawner không tự tìm manager hoặc sinh từ Awake, phải được root/bootstrap Initialize. API regeneration trong procedural bị suppress; [D3c2](../DependencyInjection/D3c2-ProceduralSpawners.md) có hướng dẫn/checklist P và click đang chờ người dùng, không coi đã pass manual.

Trạng thái cuối **13/11/2026:** gameplay dùng strategic seed1; wave sinh trên hành lang của hai gateway ở radius căn cứ tương đương legacy. `Tools/ValidateS6Waves.cs` kiểm nguyên lịch đêm và `Tools/ValidateS6Lifecycle.cs` kiểm menu/reload; [S6c](S6c-WavesLifecycle.md) có cách chạy/giới hạn. Không dùng mỗi vị trí cửa biên trong preview để suy ra vị trí spawn wave.

Cập nhật: **12/11/2026**. S6a dùng `StrategicProceduralMapSettings`, seed mặc định1; S6/D3 chưa nghiệm thu hết. [Cách chạy checks và phần còn lại](S6-Integration.md).

## Tạo scene lần đầu

1. Mở project bằng Unity **6000.3.11f1** và đợi import/compile hoàn tất.
2. Chọn menu `RTS > Procedural Generation > Create or Refresh Gameplay Scene`.
3. Công cụ tạo `Assets/Scenes/ProceduralGame.unity` từ `SampleScene`, tạo preset tại `Assets/Game/ProceduralGeneration/Settings/StrategicProceduralMapSettings.asset`, gán tile/prefab/root wave và thêm scene vào Build Settings.
4. Mở scene bằng menu `RTS > Procedural Generation > Open Gameplay Scene` nếu cần, rồi nhấn Play.

Khi thành công, Console có log `[ProceduralMap] Ready` cùng seed, số ô cỏ/nước, quota và số repair. Khi thất bại, Console ghi seed và lỗi; trận không chuyển sang `Ready`.

Nếu Console báo `Map settings asset is missing`, dừng Play và chạy lại menu `Create or Refresh Gameplay Scene`. Scene chuẩn tham chiếu `StrategicProceduralMapSettings` trong `ProceduralMapAuthoring`; root phải có Waves/day-night. Khi generator thất bại, root giữ composition blocked và báo lỗi; việc bật lại spawner legacy không tự mở gameplay gate của scene có root.

Menu refresh chỉ sao chép `SampleScene` khi `ProceduralGame` chưa tồn tại. Các lần sau nó cập nhật cấu hình của `ProceduralMapAuthoring` trong scene hiện có.

## Đổi seed và cấu hình

Chọn asset `StrategicProceduralMapSettings` trong thư mục `Assets/Game/ProceduralGeneration/Settings`; asset `DefaultProceduralMapSettings` legacy giữ để đối chiếu:

- `Seed`: đổi bố cục nhưng vẫn tái lập được.
- `Width`, `Height`: kích thước lưới, tối thiểu 32 và tối đa 1024.
- `Water Threshold`: tăng giá trị để có nhiều nước hơn; thay đổi này cần kiểm tra lại gameplay.
- `Start Clear Radius`, `Required Corridor Half Width`: vùng căn cứ và bề rộng lối bắt buộc.
- `Starting ... Count` và `Total ... Count`: quota gần căn cứ và toàn map; total đã bao gồm starting.
- `Starting ... Min/Max Distance`: khoảng đường đi BFS từ tâm, tính theo ô.

Sau khi đổi asset, dừng và Play lại scene. Phiên bản này chưa có regenerate an toàn giữa trận.

## Chạy kiểm tra

Trong Unity Test Runner, chạy EditMode assembly `RTS.ProceduralGeneration.Tests`.

Quét Core bằng dòng lệnh:

```powershell
dotnet run --project Tools/ProceduralGenerationSeedCheck/ProceduralGenerationSeedCheck.csproj -- 1000
```

Exit code `0` nghĩa là toàn bộ seed tạo thành công và lần sinh lặp cho cùng kết quả. Công cụ dùng DLL `Unity.Mathematics` đã được Unity import trong `Library/ScriptAssemblies`.

## Checklist playtest Unity

- Map có cả cỏ và nước; TownHall nằm trong vùng cỏ trống và có lối ra biên phải.
- Có đúng 120 cây, 12 vàng, 18 cừu với preset mặc định; sáu mẫu vàng đều xuất hiện.
- Dân tới/chặt cây, đào từng mẫu vàng, săn cừu và nộp tài nguyên đúng kho.
- Có thể đặt công trình, huấn luyện và điều khiển quân trên terrain mới.
- Wave chỉ bắt đầu sau log `Ready`; quái spawn trên đất và tới được căn cứ.
- Fog, minimap và camera phản ánh đúng bounds mới.
- `SampleScene` vẫn chạy theo đường cũ khi không có `ProceduralMapAuthoring`.

Kết quả nào đã thực sự chạy được ghi tại [Validation](Validation.md).
# Preview chiến thuật S4 (11/11/2026)

**Cập nhật S5 12/11/2026:** Preview StrategicRegions hiện generatorv3, resource budget vùng và landmark3gold; report có ReachableGateways/MaxResourcePathDistance. [S5](S5-ResourcesTopology.md) có cách chạy seedcheck1000 và giới hạn trước khi áp scene ở S6.

Menu **RTS/Procedural Generation/Preview Regions** mở cửa sổ sinh preview theo seed/preset; không ghi Tilemap hoặc sửa asset. Default scene vẫn LegacyMainland. [S4 Regions](S4-Regions.md) có legend, ảnh so sánh và lệnh quét `128 strategic`/`128 legacy`. Chưa chuyển scene sang StrategicRegions trước S5/S6.
