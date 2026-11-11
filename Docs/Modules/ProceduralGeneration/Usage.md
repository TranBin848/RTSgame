# Sử dụng Procedural Generation

Cập nhật: **29/10/2026**.

## Tạo scene lần đầu

1. Mở project bằng Unity **6000.3.11f1** và đợi import/compile hoàn tất.
2. Chọn menu `RTS > Procedural Generation > Create or Refresh Gameplay Scene`.
3. Công cụ tạo `Assets/Scenes/ProceduralGame.unity` từ `SampleScene`, tạo preset tại `Assets/Game/ProceduralGeneration/Settings/DefaultProceduralMapSettings.asset`, gán tile/prefab và thêm scene vào Build Settings.
4. Mở scene bằng menu `RTS > Procedural Generation > Open Gameplay Scene` nếu cần, rồi nhấn Play.

Khi thành công, Console có log `[ProceduralMap] Ready` cùng seed, số ô cỏ/nước, quota và số repair. Khi thất bại, Console ghi seed và lỗi; trận không chuyển sang `Ready`.

Nếu Console báo `Map settings asset is missing`, dừng Play và chạy lại menu `Create or Refresh Gameplay Scene`. Scene chuẩn phải tham chiếu asset `DefaultProceduralMapSettings` trong component `ProceduralMapAuthoring`. Nhánh lỗi sẽ trả scene về spawner cũ thay vì giữ ngày/đêm và wave ở trạng thái chờ vô hạn.

Menu refresh chỉ sao chép `SampleScene` khi `ProceduralGame` chưa tồn tại. Các lần sau nó cập nhật cấu hình của `ProceduralMapAuthoring` trong scene hiện có.

## Đổi seed và cấu hình

Chọn asset `DefaultProceduralMapSettings` trong thư mục `Assets/Game/ProceduralGeneration/Settings`:

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

Menu **RTS/Procedural Generation/Preview Regions** mở cửa sổ sinh preview theo seed/preset; không ghi Tilemap hoặc sửa asset. Default scene vẫn LegacyMainland. [S4 Regions](S4-Regions.md) có legend, ảnh so sánh và lệnh quét `128 strategic`/`128 legacy`. Chưa chuyển scene sang StrategicRegions trước S5/S6.
