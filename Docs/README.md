# Tài liệu kỹ thuật RTSgame2026

**24/11 — kế hoạch Save/Load:** [Build plan SL0–SL9](Modules/SaveLoad/BuildPlan.md), PLANNED. Lưu toàn trạng thái trận, ID bền vững, capture fence, remap hai lượt, JSON có version và temp/backup; chưa triển khai hoặc nghiệm thu. Bước tiếp là SL0 inventory/schema.

**24/11 — E10 Release/budget đã kiểm, nghiệm thu cuối PARTIAL:** [Bản Windows, hướng dẫn chạy, hash và raw evidence](Modules/GameExperience/E10-ReleaseHandoff.md). Build0lỗi/0cảnh báo;89/89 regression,18/18 gameplay smoke,15/15 lifecycle.4/4G5 p95≈13,888ms và stress8đèn/32VFX/94quái p95=13,8952ms đạt trên baseline1080p/Ultra, foreground hợp lệ. G3manual/E9balance và manual thẩm mỹ vẫn mở; không coi đây là nghiệm thu toàn bộ E0–E10.

**24/11 — E9 đã triển khai/kiểm Editor:** [Năm đêm chiến thuật, shortlist/matrix/realtime](Modules/GameExperience/E9-TacticalNights.md).89/89regression,16policyledger/trace,normalPlayerLoop5đêmVictory vàPNG. Holdout137/901Economyđềuthua, nghiệmthu cânbằngPARTIAL; không suy tỷlệthắngharness thànhngườichơi hoặc đóngG3/G5/Player. TiếpE10 nếu được yêu cầu.

**24/11 — E8 đã triển khai/kiểm Editor:** [Preview logistics, gọi dân và tập kết quân](Modules/GameExperience/E8-ExpansionCommands.md).89/89regression, movement/resume/lifecycle và A/Bkho gần cùng mỏPASS; khoảng cách lưới/cost, không ước tính giây UI. G3manual vẫn mở theo người dùng; chưa Player/perf/naturalE8, không đóng gateE8đầyđủ/G5. Checkpoint dưới là lịch sử.

**24/11 — E7 DONE implementation/Editor:** [Bình minh, tổng kết đêm/trận, ledger và evidence](Modules/GameExperience/E7-DawnReports.md).86/86EditMode, fixture/lifecycle/reopen/pause và một lượt5đêm clock thậtPASS;45mẫu ví/5bình minh khớp cumulative, ảnh3pha/dawn/result và UIABBA Editor. Chưa Player/video/manual đợt A; không đóng G3/G5. Tiếp E8; checkpoint dưới là lịch sử.

**24/11 — E6 DONE phạm vi implementation/Editor:** [Phản hồi hit/khai thác/nộp kho, Cargo/Fortify và research](Modules/GameExperience/E6-ActionFeedback.md). Event authoritative, pool/buffer có trần, fog từng pixel;84/84 EditMode, GPU, pause/death/pool/destroy/reload/legacy PASS. Chưa Player/performance/nghiệm thu thẩm mỹ E6; tiếp E7, G3/G5 mở. Các checkpoint dưới là lịch sử.

**24/11 — E5 DONE phạm vi implementation/Editor:** [Chiều sâu, contact shadows, mái/tán và evidence](Modules/GameExperience/E5-DepthOcclusion.md). Unified foot depth cho SpriteRenderer/native, mesh batch đúng sortingOrder của Renderer2D, ring ở chân và fade chỉ phần trên. GPU depth/fog/zoom/snap, route ba pha, nhóm96native, pool/pause/component/unload/reload/legacy và S7/E4 compatibility PASS;81/81 regression10.2861536s. Map hash v3/quota/clock giữ nguyên; chưa Player E5 hoặc nghiệm thu thẩm mỹ. Tiếp E6, G3/G5 vẫn mở; các checkpoint dưới là lịch sử.

**23/11 — E4 DONE phạm vi implementation/Editor:** [Ban ngày và bản sắc vùng, ảnh A/B và bằng chứng](Modules/GameExperience/E4-DaytimeRegions.md). Cụm cỏ/hoa/bụi/đất khô/sỏi, gợn nước/bướm; seed presentation riêng, fog footprint guard. Seed0/1/1543 tái tạo/hash gameplay, khai thác/nộp/xây/đi đường và lifecycle/legacy PASS;81/81 regression. E3 đã được xác nhận nghe chuông. Tiếp E5; chưa Player E4, G3/G5 giữ mở. Các checkpoint bên dưới là lịch sử.

**23/11 — E3 đã triển khai và kiểm Editor:** [Hoàng hôn/cảnh báo, dữ liệu, ảnh tự nhiên và giới hạn](Modules/GameExperience/E3-DuskWarning.md). Chuông một lần/Dusk, countdown từ clock, marker chỉ gateway đã biết; pause/reload/RNG/fog PASS,81/81 regression. Timing60/30/45 giữ nguyên. Chưa nghe nghiệm thu hoặc đo Player E3; tiếp E4. Các dòng “tiếp E3” bên dưới là lịch sử.

**23/11 — E2 đã kiểm chức năng và Player A/B:** [Ánh sáng đêm](Modules/GameExperience/E2-NightLighting.md),81/81 regression, GPU mask/tint, pool/death/unload/menu/legacy PASS; Release0lỗi/0cảnh báo. Hai cặp seed1 đạt gate p95+≤10%, candidate khoảng13.89ms với1đèn; không chứng nhận8đèn Player hoặc đóng G5. [E0 đã được nghiệm thu](Modules/GameExperience/Evidence/E0/Acceptance.md); tiếp E3. Các dòng E0/manual chờ bên dưới là lịch sử.

**23/11 — E1 đã triển khai:** [palette Day/Dusk/Night, ownership và ảnh A/B](Modules/GameExperience/E1-ArtDirection.md). Candidate riêng ProceduralGame, clock giữ60/30/45; không double-tint quái/đạn. Integration, SampleScene legacy, reload và **81/81 EditMode PASS**. E0/manual drag vẫn mở; tiếp E2, chưa đo Player E1. Dòng kế hoạch dưới là checkpoint trước triển khai.

**23/11 — kế hoạch trải nghiệm ngày/hoàng hôn/đêm:** [Game Experience Build Plan E0–E10](Modules/GameExperience/BuildPlan.md). Đợt A hoàn thiện UI/ánh sáng/môi trường/phản hồi; đợt B làm sâu mở rộng/wave và nghiệm thu Release. Đây là kế hoạch, chưa phải chức năng đã triển khai; giữ sáu tech hiện tại.

**23/11 — giao diện cây công nghệ:** panel toàn màn hình, node gốc và ba nhánh, kéo/zoom, đọc state và gửi command qua service ECS hiện có. Giữ sáu tech. Xem [thiết kế, cách chạy và kiểm chứng](Modules/Progression/ResearchTreeUI.md).

**21/11 — G5 đã đo Windows Release ở 1080p.** Smoke đạt 18/18. Ba trong bốn lượt đạt ngưỡng p95 16,67 ms. Kết quả frame, năm mẫu startup, chẩn đoán và giới hạn nằm trong [báo cáo G5](Modules/Progression/G5-ReleaseOnscreen.md).

**21/11 — G4 đã xử lý đỉnh managed allocation khi làm mới navigation.** Development Player ghi 1.020.416 B trước và 21.648 B sau sửa ở tick bắt đầu đêm 1. Xem phạm vi phép đo, trace, kiểm thử và các spike còn lại trong [báo cáo G4](Modules/Progression/G4-FrameProfiling.md).

**21/11 — G3 chờ người chơi nghiệm thu.** Cấu hình chơi thử dùng Hall 1200 HP trong `ProceduralGame` và khóa bán kính wave theo căn cứ đầu trận trên map procedural có gateway. Xem kết quả mô phỏng, đối chứng vận chuyển và cách chơi thử trong [báo cáo G3](Modules/Progression/G3-EconomyExpansion.md).

**Chốt G2 ngày19/11:** [báo cáo pilot và realtime](Modules/Progression/G2-PressurePilot.md):9native+1realtime đạt gate, chọn **Hall1600HP riêngProceduralGame** làm shortlistG3. Byte-diff scene đúng1giá trị; prefab/SampleScene/tech/wave/clock giữnguyên, runner tạm đãgỡ. Chưa nghiệm thu cân mọiseed; G3 cần kiểm controlseed0 và lợiích/exploit mởrộng.

**Checkpoint G0–G1 trước G2:** [báo cáo baseline/policy12run](Modules/Progression/G01-Validation.md), [hợp đồng đo/chạy lại](Modules/Progression/G01-MeasurementContract.md), [NextPhasePlan](Modules/Progression/NextPhasePlan.md). Quân chủ động tham chiến3seed, ledgerPASS; control10000HP vẫn thắng3/3. Có fix trigger worker+self-check. S0–S9/D0–D4 giữ DONE theo phạm vi cũ.

**Bàn giao17/11:** build plan S0–S9 và D0–D4 DONE trong phạm vi đã chốt. Bắt đầu ở [Progress](Modules/Progression/Progress.md), [S9 nghiệm thu/artifacts/giới hạn](Modules/Progression/S9-Acceptance.md) và [D4 DI](Modules/DependencyInjection/D4-Validation.md); các trạng thái trước đó dưới đây là lịch sử.

Các tài liệu này phục vụ phát triển tiếp dự án, bàn giao và giải thích kiến trúc trong khóa luận tốt nghiệp. **Logic thực thể đã có DOTS; các lớp Unit MonoBehaviour hiện có cả vai trò adapter/presentation.** Đọc trạng thái tích hợp trước khi thêm logic vào script cũ.

## Bắt đầu từ đâu?

| Nhu cầu | Tài liệu nên đọc |
|---|---|
| Quay lại dự án hoặc bàn giao cho người khác | [Tech Overview](TechnicalOverview.md) — bản đồ code, nguồn dữ liệu, luồng chạy, giới hạn |
| Tiếp tục tech tree và procedural mở rộng | [Tiến độ/bàn giao](Modules/Progression/Progress.md) — đọc trước; [Build plan S0–S9](Modules/Progression/BuildPlan.md) — phạm vi, hợp đồng, nghiệm thu từng lát cắt |
| Thêm quân, sửa AI/spawner hoặc làm module nối với DOTS | [DOTS Development Guide](Modules/DOTS/DevelopmentGuide.md) — API, điểm mở rộng, lifecycle và chẩn đoán |
| Chạy game/demo, kiểm tra Entity hoặc build | Usage của module tương ứng trong bảng bên dưới |
| Trích kết quả cho khóa luận | Validation và artifact Evidence, kèm ngày và điều kiện thực thi |

**S8:** [Luồng trận, DI và báo cáo kiểm thử](Modules/MatchFlow/Architecture.md) — mục tiêu5đêm, game-over/restart, số liệu scripted/manual tách riêng.

## Tài liệu module

[D4 Validation và ngoại lệ DI17/11](Modules/DependencyInjection/D4-Validation.md) là trạng thái mới nhất; S8 đã được người dùng xác nhận checklist, S9 đang build/đo. Giữ các ghi chú draft bên dưới như lịch sử.

[D4 còn lại và chuẩn bị S9 ngày17/11](Modules/DependencyInjection/D4-RemainingWork.md): audit/draft, chưa áp dụng trong lúc người dùng testS8.

**Progression + Procedural mở rộng:** [Build plan](Modules/Progression/BuildPlan.md) và [sổ tiến độ](Modules/Progression/Progress.md). Đã hoàn tất S0–S9/D0–D4 ngày17/11/2026; [S9 nghiệm thu](Modules/Progression/S9-Acceptance.md) liên kết dữ liệu và giới hạn.

S0 có [hợp đồng kiến trúc](Modules/Progression/Architecture.md), [cách chạy](Modules/Progression/Usage.md) và [kiểm chứng](Modules/Progression/Validation.md); baseline đã chốt, D0 đang triển khai.

| Module | Tài liệu | Nội dung |
|---|---|---|
| Dependency Injection | [Thiết kế và lộ trình](Modules/DependencyInjection/Architecture.md) | Manual DI, composition root, lifecycle Unity/ECS; D0–D4 hoàn tất, bằng chứng/ngoại lệ ở D4-Validation |
| Dependency Injection | [Inventory D0](Modules/DependencyInjection/Inventory.md) | Baseline 149 vị trí ứng viên/31 file, owner/lifetime, ngoại lệ và lộ trình D1–D4; D0 hoàn tất |
| Dependency Injection | [ADR-001](Modules/DependencyInjection/ADR-001-ManualDI.md) · [Lookup scan](Modules/DependencyInjection/LookupScan-2026-11-09.txt) | Quyết định manual DI và snapshot D0 ngày 09/11/2026 |
| Dependency Injection | [Usage](Modules/DependencyInjection/Usage.md) · [Kiểm chứng](Modules/DependencyInjection/Validation.md) · [D1 Evidence](Modules/DependencyInjection/D1-Evidence-2026-11-10.md) | D1 root/lifecycle và D2 research service đã có; D3–D4 còn lại |
| Progression / DI D2 | [S1-D2 Evidence](Modules/Progression/S1-D2-Evidence-2026-11-11.md) · [Usage](Modules/Progression/Usage.md) | economy_tools ECS, research service/panel qua DI; S2 đã được người dùng nghiệm thu |
| Progression S3 | [S3 Evidence](Modules/Progression/S3-Evidence-2026-11-11.md) · [Catalog S3a lịch sử](Modules/Progression/S3a-Catalog.md) | Sáu tech, modifiers, gates và UI ba nhánh; 58/58 tests và kiểm tích hợp/ảnh |
| DOTS / Swarm | [Kiến trúc](Modules/DOTS/Architecture.md) | ECS, Jobs, Burst, flow field, spatial hash, collision, rendering và cầu nối gameplay |
| DOTS / Swarm | [Hướng dẫn chạy](Modules/DOTS/Usage.md) | Scene mô phỏng, điều khiển, test và build |
| DOTS / Swarm | [Kiểm thử và đo hiệu năng](Modules/DOTS/Validation.md) | Tiêu chí, bằng chứng thực thi và phương pháp benchmark |
| DOTS / Swarm | [Dữ liệu kiểm chứng](Modules/DOTS/Evidence/README.md) | CSV, JSON, ảnh Player và NUnit XML |
| DOTS / Gameplay | [Kiến trúc](Modules/DOTSGameplay/Architecture.md) | ECS authoritative cho dân, quân, cừu, công trình, tài nguyên, xây và huấn luyện |
| DOTS / Gameplay | [Hướng dẫn chạy](Modules/DOTSGameplay/Usage.md) | Tích hợp command/spawner, kiểm tra Entity/view và Player smoke |
| DOTS / Gameplay | [Kiểm chứng](Modules/DOTSGameplay/Validation.md) | Test gameplay, tích hợp SampleScene, giới hạn bằng chứng và checklist |
| DOTS / Gameplay | [Bằng chứng](Modules/DOTSGameplay/Evidence/README.md) | Đợt 16/10/2026: 32 test EditMode và 18 check tích hợp Player |
| Procedural Generation | [Nghiên cứu và đề xuất](Modules/ProceduralGeneration/Research.md) | Lựa chọn thuật toán sinh cỏ/nước, phân bố cây/vàng/cừu và ràng buộc đường đi |
| Procedural Generation | [Kế hoạch triển khai](Modules/ProceduralGeneration/ImplementationPlan.md) | Sáu mốc và trạng thái lát cắt đầu tiên |
| Procedural Generation | [Kiến trúc](Modules/ProceduralGeneration/Architecture.md) · [Cách chạy](Modules/ProceduralGeneration/Usage.md) · [Kiểm chứng](Modules/ProceduralGeneration/Validation.md) | Core xác định theo seed, scene integration, quét 1.000 seed và phần còn phải kiểm chứng trong Unity |
| Procedural Generation / S4 | [Regions + preview](Modules/ProceduralGeneration/S4-Regions.md) | StrategicRegions v2, graph ba vùng/hai cửa/landmark, preview không sửa scene; 10/10 tests, 128 seed mỗi preset |
| Procedural Generation / S5 | [Resources/topology](Modules/ProceduralGeneration/S5-ResourcesTopology.md) · [Log seedcheck](Modules/ProceduralGeneration/S5-SeedCheck-2026-11-12.txt) | Strategic v3, budget vùng/3 vàng landmark/clearance; 17/17 tests,1000 seed mỗi preset không lỗi; gameplay chờ S6 |
| Game Flow | [Kiến trúc](Modules/GameFlow/Architecture.md) · [Cách chạy](Modules/GameFlow/Usage.md) · [Kiểm chứng](Modules/GameFlow/Validation.md) | MainMenu, vào ProceduralGame, tạm dừng, tùy chọn âm lượng và thoát |
| Procedural Generation / S6a + D3a | [Scene, gateway và wave DI](Modules/ProceduralGeneration/S6-Integration.md) | Strategic scene, ba seed spawn/path/radius checks và legacy bootstrap PASS; S6/D3 tổng thể PARTIAL |
| Procedural Generation / S6b + D3b | [Kinh tế vùng và resource binding](Modules/ProceduralGeneration/S6b-EconomyResourceDI.md) |3seed worker/nộp/StoragePit/fog-minimap-camera API; cây/mỏ qua DI và lifecycle checks PASS; còn wave/DI để hoàn tất S6/D3 |
| Procedural Generation / S6c | [Wave và lifecycle](Modules/ProceduralGeneration/S6c-WavesLifecycle.md) · [Raw checks](Modules/ProceduralGeneration/S6c-WaveChecks-2026-11-13.txt) | Sửa trễ wave, approach radius so legacy3seed, retreat và menu reload PASS; S6/D3 vẫn PARTIAL |
| Dependency Injection / D3c1 | [Construction process](Modules/DependencyInjection/D3c1-Construction.md) | BuildingProcess constructor DI, caller/placement guards và construction/UI checks PASS; D3 tổng thể PARTIAL |
| Dependency Injection / D3c2 | [Procedural/spawners](Modules/DependencyInjection/D3c2-ProceduralSpawners.md) | Explicit refs/Initialize, suppression và legacy regeneration/cleanup/deposit/menu checks PASS; manual P/click được người dùng xác nhận15/11 |
| Dependency Injection / D3c3 | [Production/lifecycle](Modules/DependencyInjection/D3c3-Production.md) | Producer DI, debit sau accepted, training/FIFO/pool lifecycle API PASS;59/59 DOTS+17/17 procedural, manual được người dùng xác nhận15/11 |
| Dependency Injection / D3c4a | [Projectile factory/startup](Modules/DependencyInjection/D3c4a-ProjectileFactory.md) | Đạn dùng factory được cấp bridge, native/legacy damage/pool/caller checks PASS; root khởi động gameplay idempotent sau bind; tiếp D3c4b Unit graph |
| Dependency Injection / D3c4b | [Unit DI/lifecycle](Modules/DependencyInjection/D3c4b-UnitLifecycle.md) | D3 DONE: binding/global-null/pool/nav/SampleScene/direct unregister và worker pool qua menu→game mới PASS; D4 toàn project còn lại |
| Procedural Generation / S6 final | [Nghiệm thu16/11](Modules/ProceduralGeneration/S6-FinalValidation.md) · [Raw log](Modules/ProceduralGeneration/S6-FinalChecks-2026-11-16.txt) | S6 DONE: matrix0/1/1543 +manual +pool reload +17procedural PASS;59DOTS sau code cuối giữ nguyên; tiếp S7 |
| Procedural Generation / S7 | [Presentation/khám phá/ảnh](Modules/ProceduralGeneration/S7-Presentation.md) | DONE: vùng/đường/landmark, decor không collider, glyph sau explored, night readability;3seed/API/ảnh/user acceptance,59+17 PASS; tiếp S8 |

`DOTS` và `DOTSGameplay` là hai phần tài liệu của cùng nền runtime tại `Assets/Game/DOTS`: phần đầu tập trung Swarm/quái/đạn, phần sau giải thích gameplay RTS. Không coi chúng là hai implementation tách rời cần tự lắp lại.

## Quy ước cho các module tiếp theo

Mỗi module lớn có thư mục riêng dưới `Docs/Modules`, ví dụ `ProceduralGeneration`. Giai đoạn nghiên cứu có thể bắt đầu bằng `Research.md`; khi triển khai, cấu trúc tối thiểu gồm `Architecture.md`, `Usage.md` và `Validation.md`.

Tài liệu cần trả lời: vấn đề gì được giải quyết; dữ liệu nào được lưu; thuật toán nào được dùng và tại sao; module giao tiếp với phần còn lại thế nào; cách tái lập bài chạy; bằng chứng kiểm thử; giới hạn và hướng phát triển.

Không ghi tính năng dự kiến thành tính năng đã có. Mỗi kết quả hiệu năng phải đi kèm số lượng thực thể, kịch bản, cấu hình máy, loại build, độ phân giải và điều kiện bật/tắt hiển thị.

Khi thêm module hoặc đổi ranh giới kiến trúc, cập nhật trang tổng quan và đường dẫn trong danh mục này cùng tài liệu module. Giữ artifact lịch sử rõ ngày; chỉ ghi test/benchmark mới khi đã thực thi và có bằng chứng.

