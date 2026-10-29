# Kiểm chứng Procedural Generation

Ngày kiểm chứng: **29/10/2026**.

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

Chưa tạo `ProceduralGame.unity` bằng menu và chưa chạy Play Mode. Vì vậy chưa xác nhận trực quan RuleTile/bờ nước, prefab anchor/collider, số Entity đăng ký, khai thác/xây/huấn luyện, wave, fog, minimap, camera hoặc tổng thời gian từ bắt đầu scene đến `Ready`.

Số 16,73 ms chỉ đo Core trong process .NET có cả ảnh hưởng khởi động/JIT; không gồm ghi Tilemap, instantiate 150 GameObject, `Physics2D.SyncTransforms`, rebuild navigation, DOTS registration hoặc rendering. Chưa đo median/p95, allocation hay Player build nên không dùng con số này làm cam kết hiệu năng gameplay.

## Cách hoàn tất nghiệm thu

Mở đúng Unity version, chạy menu tạo scene, chạy EditMode tests, rồi thực hiện checklist trong [Usage](Usage.md). Với lỗi gameplay, ghi lại seed, settings, log `[ProceduralMap]`, ảnh scene và XML Test Runner. Đo Player build nhiều lần trước khi chốt quota/cân bằng hoặc ngân sách thời gian.
