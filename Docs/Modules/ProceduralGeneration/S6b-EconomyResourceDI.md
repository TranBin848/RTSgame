# S6b / D3b — Kinh tế vùng, công trình và resource binding

Ngày **13/11/2026**, Unity **6000.3.11f1**. Tiếp [S6a Integration](S6-Integration.md), không thay BuildPlan. S6/D3 tổng thể vẫn **PARTIAL**: wave ban đêm/retreat/ùn quái/áp lực so legacy và các nhóm DI còn lại chưa nghiệm thu.

## Mục tiêu và phạm vi

Kiểm worker đi từ căn cứ tới Economy/GoldFrontier, khai thác và nộp qua ECS trên ba seed; kiểm công trình tại pad landmark cùng fog/minimap/camera bằng API. Chuyển cây/mỏ `ResourceNodeBase` từ service lookup sang dependency được bridge cấp, giữ prefab/collider/profile và hai simulation system hiện có.

Không sửa thuật toán sinh map, quota, giá/tech, tốc độ worker, thời gian xây, package hoặc render pipeline. Đây không phải thay đổi toàn bộ factory/pool hoặc toàn bộ UI sang DI.

## Dữ liệu và luồng DI

- `ResourceNodeBase.Initialize(SwarmGameBridge, TilemapManager)` giữ hai dependency theo scene; thiếu bridge/map hoặc bridge chưa khởi tạo bị từ chối. Init cùng references được gọi lại an toàn; thay reference khi còn bound bị từ chối. `IsInitialized` phản ánh reference bridge còn sống, không tự chứng minh Entity đã đăng ký.
- `SwarmGameBridge.RegisterResource` là điểm sở hữu registration: inject trước, tái dùng Entity đã có hoặc tạo một `GameplayResource`. Không gọi mỗi `node.Initialize` rồi mặc định có Entity.
- Scene binding quét resource ban đầu như trước, bao gồm các node do procedural sinh trước root. `SpawnGameplay` đăng ký resource mới trước khi trả view; đường regenerate legacy của ResourceSpawner cũng đã dùng API này khi có bridge. Không còn self-registration qua `ResourceNodeBase.Start` hoặc đọc bridge static ở consumer.
- Claim/query/release đi qua bridge được inject. Khi claims đổi, node đồng bộ collider và cập nhật navigation qua map đã nhận; authoritative claims vẫn ở ECS. Không tạo bộ đếm claims thứ hai cho node đã bind.
- `UnregisterResource` xóa Entity rồi unbind node, xóa reference scene, đưa local claim counter về0 và bật lại physical collider. Bridge teardown unbind các resource view còn sống trước reset system; `OnDestroy` của node gọi unregister qua dependency đang giữ. Local claim behavior khi không có bridge vẫn tồn tại, nhưng không tự tìm map hoặc World; runtime node trong scene DOTS phải qua bridge/factory để được điều khiển đúng.

`RTS.DOTS` không nhận managed interface và không thay dependency hướng về Assembly-CSharp. Thay đổi nằm ở `ResourceNodeBase.cs`, `GameplayGameBridge.cs` và teardown `SwarmGameBridge.cs`; không sửa prefab. Resource pooling/disable-enable không nằm trong slice này: cây/mỏ hiện không triển khai `IPooledRuntimeObject`. D3 tiếp phải kiểm factory/pool thật thay vì coi unregister/rebind test là đã nghiệm thu pool.

## Phương pháp kiểm chứng tái chạy

`Tools/ValidateS6Economy.cs`, chạy bằng Unity MCP `script-execute`, class `S6EconomyValidation`, trong session Play đã pause; thoát Play giữa các seed để dọn dữ liệu thử.

1. `CheckResourceBinding`: assert mọi cây/mỏ ban đầu đã inject. Tạo một cây qua `SpawnGameplay` ở vị trí thử ngoài map, repeat Init/Register giữ cùng Entity/count; thiếu bridge bị từ chối. Claim đúng capacity rồi từ chối claim thừa, physical collider tắt/navigation version tăng, Release hết bật collider. Unregister xóa Entity/reference, Register lại tạo handle mới, hủy view thử xóa Entity lần nữa. Đây là kiểm lifecycle registration, không đo placement hợp lệ ở vị trí thử ngoài map.
2. `Run`: chọn hai worker ban đầu và resource gần tâm của Economy (gỗ), GoldFrontier (vàng), lọc bằng `RegionIds` từ map thực. Giao resource bằng bridge; không teleport, không tăng speed hoặc sửa gathering profile. Tick Gameplay system0,05giây tối đa600giây mô phỏng; mỗi10tick đồng bộ view/claims, physics và refresh navigation khi version đổi. Assert event Deposit từng worker và số dư authoritative cùng tăng. Bảo vệ máu worker, không tick Swarm/day-night: đây là phép thử kinh tế riêng, không phải trận có quái gây gián đoạn. Time và máu được phục hồi; công việc/tiền còn lại chỉ tồn tại trong session thử.
3. `RunConstruction`: dùng StoragePit asset thật (2×2,10giây xây,100gỗ/100vàng), `PlacementProcess` kiểm footprint tại pad landmark. Gọi transaction trừ tài nguyên hiện có rồi `BuildingProcess`; worker thứ ba đi xây từ căn cứ, không teleport hoặc rút ngắn construction duration. Tick Gameplay và sync presentation; assert Active/Finished/sprite hoàn tất và actor count25→26. Đây là API integration, không mô phỏng click chuột/UI hay bảo đảm mọi kiểu công trình có footprint hợp lệ.
4. `CheckFogCamera`: chạy sau một `EditorApplication.Step()` để view mới có cơ hội chạy `Start`/đăng ký GameManager. Assert công trình có trong player registry, fog đã khám phá landmark và bounds khớp map, minimap texture đúng128×128, camera focus đúng XY và giữ Z; phục hồi camera. Đây là kiểm dữ liệu/API, không nghiệm thu hình ảnh, kéo minimap hoặc vùng nhìn ở mọi độ phân giải.

Phép thử construction đầu tiên của seed1 đã hoàn tất xây nhưng kiểm fog ngay trong cùng lời gọi không đạt: Unity chưa chạy `Start` của view mới. Đã tách bước lifecycle trên; sau Editor Step, registry/fog/minimap/camera PASS. Không sửa FogOfWarManager để che precondition của test. Hai seed0/1543 có kết quả fog/minimap/camera PASS trong phép thử kết hợp trước khi tách; chỉ seed1 được assert thêm registry sau Step trong phiên này.

## Kết quả Unity MCP ngày13/11

| Seed | Economy: gỗ nộp / có hàng đầu / nộp đầu (giây mô phỏng) | GoldFrontier: vàng nộp / có hàng đầu / nộp đầu | StoragePit: cell / hoàn tất (giây mô phỏng) |
|---|---|---|---|
| 0 | 20 /18,45 /54,25 | 20 /21,50 /59,70 | (-12,-56) /27,00 |
| 1 | 20 /17,15 /52,20 | 20 /20,45 /59,35 | (39,-2) /30,00 |
| 1543 | 20 /22,50 /60,80 | 20 /21,95 /58,10 | (-65,-3) /29,50 |

- Mỗi seed kinh tế kết thúc với ví120/120/100 từ baseline100/100/100. Giới hạn dừng là cả hai worker đã nộp ít nhất một chuyến, không phải năng suất dài hạn.
- Cả ba seed:132cây/mỏ đã inject; repeat/missing/claims/collider/navigation/unregister/rebind/destroy PASS. Construction giữ đúng chi phí100/100 và thêm đúng một actor, fog khám phá pad, minimap128×128/camera focus PASS theo phạm vi bước trên.
- SampleScene legacy:50cây/mỏ đã inject, toàn bộ resource lifecycle checks PASS; bridge bound/25actors/wave spawn PASS. Không đổi bố cục scene legacy.
- Regression EditMode: `RTS.DOTS.Tests` **58/58 PASS**,0fail/skip,1,9003524giây; `RTS.ProceduralGeneration.Tests` **17/17 PASS**,0fail/skip,5,2850361giây. Không Player build hoặc benchmark mới; các khoảng thời gian trong bảng là thời gian mô phỏng, không phải CPU/frame time.

## Cách chạy và giới hạn vận hành

Đổi Seed của `StrategicProceduralMapSettings.asset` trước khi Play; không áp map lại giữa trận. Factory tạo resource động phải dùng `bridge.SpawnGameplay` hoặc gọi `bridge.RegisterResource` ngay sau tạo view, trước behavior cần resource; `Instantiate` đơn lẻ sau scene binding không tự đăng ký qua Start nữa. Các caller hiện có đã đối chiếu: ProceduralMapAuthoring tạo trước BindScene, ResourceSpawner có bridge dùng SpawnGameplay, initial legacy nodes được BindScene quét.

Seed cuối đã trả về1. Editor quay lại ProceduralGame, ngoài Play/compile/pause; dữ liệu thử/probe được dọn bằng Exit Play. Không commit, không sửa scene YAML/prefab trong slice này. Native crash cũ chưa xác định nguyên nhân; không có kết luận đã sửa crash.

## Bước tiếp theo theo BuildPlan

- **S6c:** chạy lịch đêm thật/retreat trên ba seed, so legacy về số wave/quái và thời gian tiếp cận/áp lực; kiểm ùn quái, các prefab enemy, camera/minimap click/hình ảnh và vòng MainMenu→game→menu→game. S6 chưa DONE chỉ từ phép thử kinh tế/xây/API fog.
- **D3c:** ProceduralMapAuthoring/ResourceSpawner còn lookup, BuildingProcess/actions train/build, factories/pool và callers trong Inventory. Worker/Unit còn static bridge thuộc D3/D4. Giữ ranh giới ECS và nghiệm thu spawn/register/remove/reuse từng nhóm.

Nguồn đối chiếu: [BuildPlan](../Progression/BuildPlan.md), [Inventory DI](../DependencyInjection/Inventory.md), [DI Architecture](../DependencyInjection/Architecture.md), [DOTS Gameplay](../DOTSGameplay/Architecture.md), [DevelopmentGuide](../DOTS/DevelopmentGuide.md) và code/test nêu trên. Chưa dùng hoặc bổ sung thuật toán/thư viện bên ngoài.
