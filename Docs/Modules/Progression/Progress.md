# Tiến độ và bàn giao — Progression + Procedural mở rộng

Cập nhật: **11/11/2026**. Kế hoạch: [BuildPlan.md](BuildPlan.md).

## Đọc ngay khi tiếp tục

**S0–S4 và D0–D2 DONE theo phạm vi đã ghi. S4 có Core/preview chiến thuật; gameplay preset mới chờ S5/S6.**

**Việc tiếp theo: S5 — budget tài nguyên theo vùng và kiểm topology sau placement.** Đọc [S4 Regions](../ProceduralGeneration/S4-Regions.md), BuildPlan mục S5 và Core generator trước khi sửa.

Không cần hỏi lại người dùng về tên/giá thử trong plan khi được yêu cầu tiếp tục implementation; đó là mặc định thử nghiệm có thể điều chỉnh và ghi lý do. Quyết định thay đổi phạm vi như prestige/save/load phải được tách rõ.

## Bảng trạng thái

Yêu cầu DI đã bổ sung vào [Architecture DI](../DependencyInjection/Architecture.md). Chưa migrate runtime. Thứ tự: S0 → D0 → D1 → S1/S2 + D2 → S3 → S4/S5 → S6 + D3 → S7/S8 → D4 → S9.

| Lát cắt | Trạng thái | Kết quả/bằng chứng | Bước tiếp |
|---|---|---|---|
| S0 Baseline | DONE | Rerun 34/34 DOTS, 3/3 procedural, 18/18 Player smoke; người dùng xác nhận thao tác; xem Validation/Evidence | S8/S9 kiểm UI/ngày đêm có artifact |
| D0 Inventory/ADR DI | DONE | 149 call site ứng viên/31 file, bảng owner/lifetime, ADR-001; xem DI Inventory | D1 root và lifecycle |
| D1 Composition root/lifecycle | DONE | Inject manager/locators; Ready/failure gate; scene/domain reload và regression tests PASS; xem DI Evidence | S1 + D2 |
| D2 Progression qua DI | DONE | Root→ResearchService→IResearchService panel; fake/lifecycle/transaction PASS | Giữ nguyên ranh giới khi mở thêm tech |
| D3 Gameplay integration qua DI | TODO | Chưa triển khai | Sau S3, hoàn tất cùng S6 |
| D4 Hoàn tất phạm vi DI | TODO | Chưa triển khai | Xử lý hết inventory trước S9 |
| S1 Một tech trong ECS | DONE | economy_tools, wallet/state/tick/modifier; 43/43 DOTS tests và scene API checks | S2 manual/visual acceptance |
| S2 UI nghiên cứu | DONE | Người dùng xác nhận toàn bộ checklist thủ công đạt; không có artifact kèm theo | S3 |
| S3 Sáu tech | DONE | 58/58 DOTS tests, six-node integration, gates/debit, legacy và ảnh UI 1920×1080 | S4 vùng/preview map |
| S4 Vùng và preview | DONE | StrategicRegions v2 + preview; 10/10 tests, 128 strategic và128 legacy seed không lỗi; ảnh đã kiểm | S5 budget/topology cuối |
| S5 Budget và topology | TODO | — | Sau S4 |
| S6 Map/wave/navigation | TODO | — | Sau S3 + S5 |
| S7 Hình ảnh/khám phá | TODO | — | Sau S6 |
| S8 Trận và cân bằng | TODO | — | Sau S3 + S6, hoàn thiện cùng S7 |
| S9 Nghiệm thu | TODO | — | Sau S7 + S8 |

## Checkpoint hiện tại

- **S4 DONE ngày 11/11:** MapLayoutPreset.LegacyMainland (default/version1) và StrategicRegions (version2). MapData.Layout gồm Regions, Connections, Gateways, Landmark, RegionIds, Routes. RNG phase riêng; hai nhánh vuông góc theo seed, carve terrain/reserved trước BFS/placement.
- **Bằng chứng:** [S4 Regions](../ProceduralGeneration/S4-Regions.md), ba ảnh preview cùng thư mục Progression. Unity procedural tests 10/10; seedcheck 128 strategic và 128 legacy không lỗi, so sánh tái lập cả dữ liệu mới. Runtime asset DefaultProceduralMapSettings vẫn legacy, scene không dirty.
- **Bước tiếp S5:** phân bổ resource budget theo role/path distance; landmark cụm vàng có footprint/chỗ xây; validator sau placement về resource access, clearance, start area và gateway topology thật. Quét 1000 seed theo plan S5. Đọc MapGenerationData.cs, StrategicLayout.cs, ProceduralMapGenerator.cs và S4-Regions.md.
- **Giới hạn bắt buộc giữ:** gateway là cell đề xuất, chưa là spawn point hợp collider; noise vẫn có thể tạo đường phụ. Không gọi đây là đúng hai luồng quái, không nói vùng Economy đã giàu tài nguyên hơn. S6 mới áp scene/wave/cả hai navigation systems.
- **Editor cuối:** preview Regions mở và sinh strategic seed1543; scene ProceduralGame giữ nguyên, không Play. Preview lỗi đã kiểm xóa ảnh cũ. Không Player build/benchmark mới. Runtime đúng phiên bản phải dùng ToData preset0 trước S6.
- **Lịch sử:** S3 sáu tech đã DONE, 58/58 tests; D3/D4 migration legacy vẫn theo plan. Không làm lại tech hoặc ghi gameplay S4 đã kiểm chứng. Thay đổi plugin/font/package/ProjectSettings có sẵn vẫn giữ nguyên.

## Mẫu cập nhật bắt buộc khi kết thúc hoặc dừng giữa phiên

Thay nội dung Checkpoint hiện tại bằng trạng thái mới nhất, cập nhật bảng ở trên và thêm một mục lịch sử ngắn phía dưới. Không chỉ ghi trong chat.

```markdown
### YYYY-MM-DD — Sx — DONE/PARTIAL/IMPLEMENTED_UNVERIFIED/BLOCKED
- Mục tiêu phiên:
- Đã triển khai: hành vi cụ thể có thể quan sát.
- File/asset/config thay đổi: đường dẫn và lý do.
- Quyết định đã chốt: quy tắc, mặc định, khác biệt với BuildPlan.
- DI: consumer/dependency đã migrate, lookup/static còn lại, ngoại lệ và mục inventory tiếp theo.
- Kiểm tra đã chạy: lệnh/thao tác, kết quả thật, artifact, seed/config.
- Chưa kiểm tra: ghi rõ phần nào và vì sao.
- Lỗi còn lại/điều kiện gỡ chặn:
- Trạng thái môi trường: Play Mode, process/job còn chạy, scene mở và dữ liệu tạm cần giữ.
- Bước tiếp theo: 1–3 việc cụ thể, file/API nên đọc đầu tiên.
- Điểm dừng an toàn: compile/test gần nhất; thay đổi nào chưa hoàn chỉnh.
- Commit nếu có: hash; không ghi hash khi chưa commit.
```

DONE yêu cầu implementation + kiểm chứng bắt buộc + tài liệu. Không có Unity thì ghi IMPLEMENTED_UNVERIFIED, lưu test cần chạy; không biến giả định thành kết quả.

## Nhật ký

### 11/11/2026 — S4 — DONE (Core + preview)

- MapData/settings có preset/version, 3 region roles, graph2edges, 2 gateways, landmark và raster masks. Strategic dùng RNG stream riêng, carve vùng/đường bảo vệ rồi BFS/placement/validator hiện có. Legacy giữ branch cũ/version1.
- Preview Regions EditorWindow sinh/hiện màu/seed/attempt/errors không sửa scene. PNG strategic seed0/1543 và legacy seed1543 đã xuất/kiểm.
- Unity procedural EditMode10/10 pass; Core seedcheck128strategic +128legacy đều0lỗi và so sánh dữ liệu tái sinh. CLI bổ sung preset argument và include file Core mới; bin/obj bị ignore theo project tool.
- Chưa có budget tài nguyên vùng hoặc strategic gameplay integration. Default scene asset vẫn legacy; không spawn Entity mới từ preview. Đây là giới hạn S4 theo plan, S5/S6 xử lý tiếp.
- Điểm tiếp S5 theo S4-Regions.md. Cuối phiên không Play/compile; scene giữ nguyên, preview mở strategic seed1543.

### 11/11/2026 — S3b/S3 — DONE

- Catalog asset đủ sáu node theo BuildPlan; root configure toàn graph, service/UI query theo ID. UI ba nhánh hai cấp, giữ DI/lifecycle.
- Cargo effective capacity, duration snapshot queue mới, tower shot damage; giữ base profiles/stats và queue cũ. Thêm capability guards trước debit và tại bridge/runtime construction/training. Scene legacy không research giữ mở nội dung.
- Unity DOTS EditMode 58/58 pass; Tools/ValidateS3.cs PASS mask63/all modifiers bằng tick tăng tốc, fake-panel PASS, locked archer action không debit, scene reload reset, SampleScene mở capabilities/hệ số1. Ảnh Game View đã kiểm ở1920×1080.
- Cuối phiên đã Exit Play, pause Editor tắt; scene ProceduralGame. Không build Player hoặc benchmark. Giá/thời gian chưa playtest dài; không nghiệm thu màn hình nhỏ/click thật cả6node. [Evidence](S3-Evidence-2026-11-11.md).
- Tiếp S4 regions/preview, giữ các behavior/prefab hiện có; D3/D4 migration phần legacy vẫn theo plan.

### 11/11/2026 — S2 acceptance DONE; S3a DONE, S3 PARTIAL

- Người dùng báo checklist UI/trừ tiền/pause-hoàn tất/trận mới/thiếu tiền đều đạt; ghi nhận là manual report, chưa có artifact kèm theo.
- S3a mở runtime catalog 1–6 node, validate duplicate/missing/cycle/unknown ID, copy config, giữ mask unlock khi slot bị hủy; giữ API single-tech cũ.
- Unity DOTS EditMode 50/50 pass; scene API economy_tools tương thích. Không đổi UI/asset cho sáu node, chưa triển khai các effect/gate còn lại.
- Tiếp S3b theo S3a-Catalog.md; scene cuối ProceduralGame, Play đã tắt.

### 11/11/2026 — S1/D2 — DONE; S2 IMPLEMENTED_UNVERIFIED

- ECS research command giao dịch ví một lần, check hall/slot/ID/unlock/giá; pause dừng, mất hall hủy không refund; reset theo trận. Modifier tính từ profile gốc cho worker cũ/mới, chỉ gỗ hưởng bonus.
- Root inject service/panel; panel config/giá/tiến độ/unlock, listener lifecycle đã test bằng fake. Scene và công cụ tạo scene cập nhật cùng asset.
- Unity 43/43 EditMode, scene API và fake asserts PASS; hoàn thành research bằng 405 tick tăng tốc. Chưa kiểm click chuột thật/hình ảnh/resolution/thời gian tự nhiên; không benchmark.
- Tiếp S2 manual/visual acceptance, rồi S3 catalog nhiều node. Evidence và Usage đã cập nhật.

### 11/11/2026 — Chốt D1 — DONE

- Hoàn tất root/bridge/GameManager/locators, BindScene trước Ready, chặn gameplay khi cấu hình lỗi; giữ adapter legacy cho SampleScene.
- Kiểm chứng Unity ngày 10/11: missing camera, repeat init, duplicate owner, game-menu-game, domain reload bật/tắt, SampleScene đều PASS. Regression EditMode DOTS 34/34 và procedural 3/3. Có mã assert Tools/ValidateD1.cs và D1 Evidence; không có XML hoặc Player build mới.
- Unity từng native crash trước restart; sau mở lại hoàn tất các kiểm tra, chưa xác định nguyên nhân crash. Không tuyên bố toàn project đã DI.
- Điểm tiếp: S1 economy_tools theo BuildPlan, cùng tiêu chí DI D2; không còn mục bắt buộc D1 đang chờ.

### 10/11/2026 — D1b — DONE trong phạm vi root/bridge; D1 tổng thể PARTIAL

- Root scene đã nhận refs, đợi generator Ready/live World rồi initialize bridge; serialized scene và editor scene generator được cập nhật cùng code.
- Unity assert checks PASS: root init lặp, missing config, ProceduralGame không duplicate bridge/actor, SampleScene legacy bootstrap.
- Chưa làm manager/locator injection, failure readiness gate và domain reload matrix. Tiếp D1c; không coi toàn project đã đạt DI.

### 10/11/2026 — D1a — DONE; D1 tổng thể PARTIAL

- Kết nối: Unity MCP đúng RTSgame2026; tool query/script hoạt động trở lại. Editor ban đầu MainMenu sạch, không Play/compile.
- Implementation: bridge Initialize nhận dependency tường minh, validate World/game/map; cùng dependencies gọi lại an toàn; khác dependency hoặc bridge owner khác bị từ chối; tái dùng renderer. Awake legacy còn lookup trong thời gian chuyển.
- Kiểm chứng Unity: compile; ProceduralGame Play có 25 actors và 1 renderer; assert checks repeat init, thiếu World/game/map, thay fog đều PASS. Thoát Play Mode. Chi tiết [Validation](../DependencyInjection/Validation.md).
- Chưa kiểm: root/scene wiring, duplicate owner bằng object thứ hai, teardown/reload/domain reload và regression toàn suite. Không có performance claim.
- Bước tiếp: D1b composition root/map/manager; D1 vẫn PARTIAL, chưa có tech tree.

### 09/11/2026 — D0 — DONE

- Đã làm: đối chiếu 149 vị trí lookup trong 31 file, static scene state, service creation, scene YAML và thứ tự Awake/Start; ghi inventory, snapshot và ADR manual DI.
- Unity MCP: 1.8.0 có lỗi compile nên rollback riêng package về 1.7.0; instance `RTSgame2026` kết nối lại. Chưa chạy test hoặc Play Mode sau rollback vì D0 chỉ đổi docs/package; D1 cần kiểm compile/scene khi tool truy vấn khả dụng.
- Quyết định: root scene cho ProceduralGame, adapter bootstrap cho scene cũ, World ECS do Unity sở hữu. D1 phải ngăn duplicate bridge/Entity và validate dependency trước Ready.
- Bước tiếp: D1, đọc `SwarmGameBridge.cs`, `ProceduralMapAuthoring.cs`, `GameManager.cs`, `Unit.cs`, scene YAML và ADR-001; cập nhật docs/test cùng code.

### 05/11/2026 — Chạy lại baseline — DONE (kiểm chứng S0)

- Unity Test Runner: `RTS.DOTS.Tests` 34/34; `RTS.ProceduralGeneration.Tests` 3/3, không fail/skip.
- Build Windows Player mới từ code hiện tại: Editor.log ghi Success; lời gọi tool vượt timeout vì build kéo dài, nhưng EXE mới chạy được. Smoke trên EXE mới đạt 18/18, errors rỗng. JSON và checksum ở [Evidence](Evidence/baseline-rerun-2026-11-05.md).
- Giới hạn: smoke ở SampleScene với timer rút ngắn; không thay thế click UI, wave tự nhiên hoặc benchmark ProceduralGame. Chưa xuất NUnit XML.
- Không sửa gameplay; D0 vẫn PARTIAL, bước tiếp vẫn hoàn tất inventory/ADR DI.

### 05/11/2026 — S0 DONE theo xác nhận thủ công; D0 tiếp tục — PARTIAL

- Người dùng xác nhận toàn bộ checklist khai thác/nộp, xây/huấn luyện, Pause/Resume và wave đều đạt. Không có artifact đính kèm; ghi riêng trong Validation.
- Quét `Assets/Game/**/*.cs` không thấy EndGame/Victory/Defeat rõ ràng; điều kiện kết thúc để S8. Không suy ra cừu/tài nguyên hữu hạn từ quota placement.
- D0: đã nhận diện các nhóm singleton/lookup, World access và asset lookup; Inventory bổ sung phân loại sơ bộ. Chưa có ADR hoặc refactor runtime.
- Bước tiếp: hoàn tất bảng consumer→owner→injection và ADR D0; sau đó D1.

### 05/11/2026 — S0 tiếp tục, D0 inventory khởi đầu — PARTIAL

- Đã chạy: MainMenu→ProceduralGame bằng GameFlowMenu.Play; scene Ready với 3 worker, 120 cây, 18 cừu; giao cây trả true; Pause/Resume đổi timeScale 0→1; thoát Play Mode.
- Chưa xác nhận: giao cây nộp được, xây/huấn luyện/wave; Time.time trả 0 trong phiên tool. Cần Play Mode tương tác/Player smoke có thời gian mô phỏng thực.
- D0: quét sơ bộ điểm lookup/static trong Assets/Game, lưu [Inventory](../DependencyInjection/Inventory.md). Chưa phân loại hết callers, chưa hoàn thành ADR.
- Trạng thái: scene Editor đang MainMenu sau khi thoát Play Mode; không có job đang chạy. Không sửa gameplay.
- Bước tiếp: nghiệm thu S0 bằng thao tác thật, sau đó hoàn tất inventory D0 theo bảng consumer.

### 05/11/2026 — S0 — PARTIAL

- Mục tiêu phiên: kiểm chứng baseline trước research và DI.
- Đã triển khai: chỉ tài liệu Architecture, Usage, Validation; không sửa gameplay.
- Kiểm tra: Unity EditMode 24/24 pass; Play ProceduralGame Ready, có bridge, ví 100/100/100; thoát Play Mode. Không có XML artifact.
- Chưa kiểm tra: luồng MainMenu, thao tác resource/build/train, wave, cừu sau chết, thắng/thua.
- Quyết định: không coi quota resource là trữ lượng; research dùng PlayerWallet ECS. Giá/5 ngày là cấu hình thử.
- Bước tiếp: hoàn tất các kiểm tra trong Validation rồi D0; đọc `EnemyWaveManager.cs`, `SheepUnit.cs`, `GameManager.cs` và scene khi xác minh phần còn lại.

### 05/11/2026 — Bổ sung yêu cầu DI — DONE (chỉ tài liệu)

Thêm hợp đồng manual DI, composition root/lifetime, ranh giới ECS, lộ trình D0–D4 và tiêu chí bằng chứng khóa luận. Đọc các điểm lookup trong GameManager/EnemyWaveManager/SwarmGameBridge, contract wallet và manifest. Chưa refactor runtime, chưa cài container, chưa chạy kiểm thử DI. Bước tiếp vẫn S0 rồi D0/D1; không nhảy thẳng vào S1.

### 05/11/2026 — Lập kế hoạch — DONE (chỉ tài liệu)

Tạo BuildPlan S0–S9, quy tắc research/địa hình dự kiến và quy trình checkpoint. Các con số trong plan là thông số khởi điểm cho playtest. Không triển khai hoặc xác nhận tính năng gameplay mới.

## Câu lệnh giao việc gợi ý cho phiên sau

> Đọc AGENTS.md, Docs/TechnicalOverview.md và Docs/Modules/Progression/Progress.md. Tiếp tục lát cắt chưa hoàn thành đầu tiên theo BuildPlan, ưu tiên một lát cắt trong phiên. Kiểm tra đúng phạm vi và cập nhật Progress.md cùng docs/bằng chứng trước khi dừng. Nếu chưa nghiệm thu được trong Unity, ghi rõ phần chưa xác minh và bước tiếp theo; không đánh dấu DONE.
