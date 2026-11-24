# Tiến độ và bàn giao — Progression + Procedural mở rộng

**24/11 — Save/Load PLANNED:** [SL0–SL9](../SaveLoad/BuildPlan.md). Đã đối chiếu source và lập kế hoạch, chưa implementation/test mới. Giữ tiến độ E10 theo bằng chứng hiện có; module Save/Load bắt đầu ở SL0, không gộp log đo lường vào trạng thái hoàn thành lưu/tải.

**24/11/2026 — E10 Release/kiểm kỹ thuật và budget baseline PASS; nghiệm thu cuối PARTIAL:** [report/artifact/config/raw](../GameExperience/E10-ReleaseHandoff.md). Mono Windows x64/Ultra1080p/vSync1,235,732s build0error/0warning;89/89EditMode,18/18legacy gameplay smoke,15/15procedural lifecycle.4/4G5 p95≈13,888ms; thêm stress8đèn/32pulse/94quái p95=13,8952/p99=20,8337/max=27,7907ms.0invalidfocusframes, GC Release unavailable. G5budget đạt riêng candidate/máy/batch E10; G3manual theo yêu cầu người dùng và E9holdoutEconomythua vẫn mở, chưa nghiệm thu mọi seed/thẩm mỹ/audio/máy thứ hai.

**24/11/2026 — E9 candidate76-v1 implementation/Editor đã xong; balancePARTIAL:** [report/shortlist/matrix/realtime](../GameExperience/E9-TacticalNights.md). Plan riêngrootinject,quái/prefab/HP/giá/clockgiữ;5role,76quota,deterministicroster/gateway,Duskcùngdata,dawnstop/retreat.89/89regression9.1491996s,schedule/edge/legacyPASS.16matrix:training0/1/1543controlthua/3activewinmỗiseed;holdout137/901Economythua,khôngtunedữliệuholdout. RealtimeEditorseed1Victory5đêm675s/Hall1200,45mẫuví/5dawn/76spawnAuditPASS,ảnhNight5/briefing/result. G3manual/G5/Playercònmở,khôngsuythắngharnessrahumans. E10kếtiếpnếuđượcyêucầu.

**24/11/2026 — E8 implementation/Editor xong, G3manual OPEN:** [preview/commands/evidence](../GameExperience/E8-ExpansionCommands.md). BFS cùng grid, recall/rally counts và dawnresume.89/89EditMode11.3236323s, scene actualmovement/shelter/pause/dead/unknown/lifecyclePASS. A/B0/8001:4chuyếnHall19.9s/80G so12chuyếnkho.7s/240G,100W100Gcost→+60Gnet đổi100W; empty0/range14.13,cả3thuađêm4. Người dùng chưa thửG3 giữ mở. Chưa Player/performance/naturalE8; không tune balance, seed1/scene sạch khôi phục. TiếpE9 theo yêu cầu.

**24/11/2026 — E7 DONE implementation/Editor:** [report/ảnh/đối soát](../GameExperience/E7-DawnReports.md). Ledger native nguồn giao dịch/damage, service dawn snapshot và result cùng formatter.86/86EditMode5.1817389s, controlled ledger/reopen/pause/death/heal/overkill/lifecycle/legacyPASS; camera-fixed natural675s Victory5đêm/Hall986/1200,45mẫu ví và tổng5dawn khớp. UIABBA1280×720Editor/ảnh3pha/dawn/result đã lưu; lỗi exportVector3 được recover từ terminalfrozen và ghi rõ. Không đổi balance, chưa PlayerAB/video/manual đợt A, G3/G5 mở. Tiếp E8 theo yêu cầu.

**24/11/2026 — E6 DONE phạm vi implementation/Editor:** [report/ảnh và kiểm chứng](../GameExperience/E6-ActionFeedback.md). Consumer qua DI đọc gather/deposit/research events và bounded native impact; snapshot điều khiển Cargo/Fortify.84/84 EditMode6.6024524s, GPU tint/alpha/fog, transaction/completion một lần, pause/death/pool/destroy/reload/legacy PASS. Queue đã có, projectile đang bay và base profile giữ nguyên. Chưa Player/performance/manual thẩm mỹ E6. **Tiếp E7**; G3/G5 mở. Các checkpoint dưới là lịch sử.

**24/11/2026 — E5 DONE phạm vi implementation/Editor:** [report/ảnh/probes](../GameExperience/E5-DepthOcclusion.md). FootDepth + ContactShadow material references được giữ trong palette; body sorting/depth đồng nhất native/Mono, ring tại chân, upper-only stipple, shadow Dusk1.8giữa pha/Day-Night1, fog pixel masks và point/anchor snap. Baseline158bodies, route12ảnh named fade1sau/0trước,96native fixture→98rendered/contact, pool same-instance/pause/component-only/direct-unload/reload/legacy PASS. S7/E4 compatibility giữ full map SHA26102ba0...d71f5e và150placements;81/81 EditMode10.2861536s. E2 warm mask/tint + facade pixel probe PASS. Không sửa AI/path/giá/tech/clock/collider/pivot, chưa Player/performance E5 hoặc manual thẩm mỹ. Unity6000.3.11f1, ProceduralGame sạch seed1 ngoài Play/pause/compile. **Tiếp E6**; G3/G5 mở. ProjectSettings có define MCP_DEPS do plugin tự resolve, không nâng Editor/UPM/pipeline.

**23/11/2026 — E4 DONE phạm vi implementation/Editor:** [report/ảnh A/B/hashes](../GameExperience/E4-DaytimeRegions.md). S7 thêm nền biến thiên theo mảng, cụm hoa/cỏ/bụi và đất khô/sỏi theo vùng, gợn nước và bướm; sửa lấy một frame bush thay cả strip8frame. Seed0/1/1543 lần lượt179/168/176chi tiết E4 (12ambient nằm trong mỗi tổng), trần220. Cùng presentation seed tái tạo đúng; đổi config không đổi full MapData SHA256, Core v3/quota150/clock60-30-45 giữ nguyên. Fog footprint3×3, RNG, clearance, khai thác/nộp20wood+20gold, xây kho tại landmark, component/menu/reload/legacy PASS;81/81 regression6.4467015s. Source seed1 khôi phục, Editor sạch ngoài Play/pause/compile,0Exception; chưa Player E4/manual thẩm mỹ. E3 được người dùng xác nhận nghe chuông. **Tiếp E5**, không đóng G3/G5; checkpoint bên dưới là lịch sử.

**23/11/2026 — E3 DONE phạm vi implementation/Editor:** [báo cáo và ảnh](../GameExperience/E3-DuskWarning.md). Một chuông/Dusk, countdown trực tiếp từ clock gameplay; marker chỉ ô gateway Explored/Visible, không dự báo hướng wave chưa chốt. Integration/listener/fog/RNG/lifecycle PASS; natural Day60→Dusk30→Night có pause menu2s, counter vẫn1;81/81 EditMode6.5659028s. Timing60/30/45, gameplay và SampleScene giữ nguyên. Chưa Player/performance E3 hoặc nghe nghiệm thu. **Tiếp E4**, G3/G5 tiếp tục mở; các trạng thái “tiếp E3” bên dưới là lịch sử.

**23/11/2026 — E2 DONE phạm vi chức năng/ảnh/Player A/B seed1:** [implementation và bằng chứng](../GameExperience/E2-NightLighting.md). Đèn Hall/Tower/depot, warm glow mask từng pixel từ visibility texture, budget8/camera; không sửa gameplay, prefab hoặc timing60/30/45. Fixturephase/pause/culling/fog/pool/death/destroy/GPUtint vàmenu/unload/reload/SampleScene PASS;81/81 EditMode19.8600791s. Build ReleaseMono228.6971s,0errors/0warnings. Hai cặp onscreen1080p/Ultra/vSync1, warm30/capture90, invalid0: p95candidate13.8881/13.8879ms, tăng0.00216%/0%, đạt gate đề xuất10%. Player chỉ đo1Light2D/1glowgroup;8đèn chỉ kiểm Editor, G3/G5 vẫn mở. Hai lượt invalid920/30frames giữ riêng, không dùng so sánh. E0 đã được người dùng nghiệm thu. **Tiếp E3**; các trạng thái tiếp E2/manualE0 bên dưới là lịch sử.

**23/11/2026 — người dùng nghiệm thu E0:** “E0 tôi đã test và thấy oke nhé”. [Acceptance/giới hạn artifact](../GameExperience/Evidence/E0/Acceptance.md). Tiếp E2 theo build plan; các dòng E0/manual chờ bên dưới là checkpoint lịch sử, không còn là trạng thái hiện tại.

**23/11/2026 — E1 DONE phạm vi palette/ownership và Editor:** [Art direction/implementation/ảnh A/B/evidence](../GameExperience/E1-ArtDirection.md). Day ấm nhẹ, Dusk hổ phách, Night lạnh; root inject candidate riêng cho clock, post grading đồng nhất các renderer, profile runtime riêng và cleanup. Integration/map-wallet-RNG-clock invariants, SampleScene legacy và menu→game→menu→game PASS; regression mới81/81 PASS6.4597257s. Timing60/30/45 không đổi, chưa Player/perf. E0 PARTIAL: đã sửa fast-drag và debug overlay, fixtures cây/sáu tech81/81 ở lượt trước; người dùng hẹn kiểm drag sau, Computer Use đã dừng. Tiếp E2; G3/G5 vẫn mở. Các checkpoint PLANNED bên dưới là lịch sử.

**23/11/2026 — kế hoạch mới:** [Game Experience E0–E10](../GameExperience/BuildPlan.md), trạng thái PLANNED. Thứ tự: nghiệm thu cây → palette → đêm → hoàng hôn → ngày/vùng → chiều sâu → phản hồi → tổng kết → mở rộng → wave → Release. Giữ sáu tech, G3 nghiệm thu và G5 hiệu năng vẫn mở. Bước triển khai kế tiếp là E0, chưa làm trong phiên lập plan.

**Bàn giao cây công nghệ 23/11:** code/UI graph đã có, fixture API và 81/81 EditMode PASS theo kết quả tool trong chat trước; thao tác trực tiếp bằng Computer Use bị dừng khi người dùng nhấn Esc. Chưa chốt ảnh cuối, kiểm đa độ phân giải, phím/chuột đầy đủ hoặc full-six integration trên UI mới. E0 phải xác minh trạng thái Editor/Play trước tiếp tục; không coi đây là nghiệm thu hoàn chỉnh.

Cập nhật **21/11/2026**. [Kế hoạch S0–S9 đã nghiệm thu](BuildPlan.md) · [Kế hoạch G0–G7](NextPhasePlan.md)

## Đọc ngay khi tiếp tục

**21/11 — G5 đã hoàn tất phép đo, budget còn mở.** Windows Release smoke 18/18. Ba lượt seed 1 và một lượt military seed 1543 ở 1080p/Ultra đạt p95 13,90 / 13,90 / 20,83 / 13,90 ms. Startup năm mẫu có median 875 ms. Lượt Development cùng workload xác nhận fog góp phần ở một số frame và chưa tái lập đỉnh Release 166,66 ms. [Báo cáo G5](G5-ReleaseOnscreen.md)

**21/11 — G4 đã kiểm chứng nguồn spike navigation và bản sửa.** `RTS.RefreshNavigation` giảm từ 1.016.860 xuống 18.084 B managed trong trace wave đầu. Lượt đo khi tắt ghi trace xác nhận giảm 97,9% GC ở tick 5400. Kiểm collider 22.404 phép so khớp, EditMode 81/81 và research polling steady 0 B. [Báo cáo G4](G4-FrameProfiling.md)

**21/11 — G3 chờ người chơi nghiệm thu.** Đã lưu Hall 1200 HP vào `ProceduralGame` và sửa lỗi móng kho làm lệch vòng wave. Ba control thua, sáu lượt kinh tế/quân sự thắng, và ba lượt mở rộng hợp lệ sau sửa thắng. Trong phép đối chứng vận chuyển seed 0, kho gần mỏ mang lại thêm 60 vàng sau chi phí vàng xây kho, với chi phí 100 gỗ. MCP xác nhận seed 1 và scene sạch. EditMode đạt 81/81. [Báo cáo G3](G3-EconomyExpansion.md)

**19/11 — G2 DONE phạm vi shortlist:** [PressurePilot/report](G2-PressurePilot.md), [10run summary](Evidence/G2-2026-11-19/summary.json). Baseline10000+C/E/M và hai candidate1600/1200 đủ9native; C1600 thuađêm4/539.95s, E/M thắng với1348/1522HP; C1200 cũng thuađêm4, E/M thắng. Chọn1600 theo ruleHPcao hơn; realtimeE đủ5đêm/675.08s Victory/Hall1338, ledgerPASS. Đã author chỉ m_Health Hall trongProceduralGame10000→1600 quaUnityAPI, UI/tilefloat ngoài phạm vi đã phục hồi, byte-diff scene đúng1giá trị;269snapshot chỉscene này đổihash, prefab100/SampleScene10000/tech/wave/phase/package giữnguyên. Cloudchậpchờn nên dùngCLI --no-start vàrunnerEditor tạm; đã gỡ3script+meta, giữraw/source/failedprobe vàConsolelịch sử. Bước tiếp **G3**, kiểm sớm controlseed0 có thể còn thắng, matrix3seed/logistics/empty-building trước tune1nhánh. Chưa Release/onscreen/perf/manual hoặc robustness mới; không gọi shortlist thành Normal đã cân mọi seed. Xem closure-state.json cho trạng tháiMCP/compiler saucleanup.

**19/11 — G0–G1 DONE phạm vi đo/chẩn đoán:** [G01 report](G01-Validation.md), [contract](G01-MeasurementContract.md), raw12runv3/3seed×4policy,269hashbaseline,12ledgerPASS; quân chủ động tham chiến, control vẫn thắng3/3 (hall8534/8190/8274). Wavequota114 nhưng76slot/spawn; X đặt kho30.05s, chưa chứng minh logistics. Fix một điều kiện Unit.ApplyDotsSnapshot không triggerAttack cho worker; self-check Unity trước1warning/sau0, boolworker và triggerarcher đúng/nativeunchanged. Matrix trước presentationfix, không chạy lại sau guard; giữ pilot/fail và S8raw. Không đổi HP/wave/giá/tech hoặc làm G2–G7. MCP19/11 thấy và select đúng RTSgame2026, nhưng chưa expose scene/script tools để đọc live scene; trạng thái Editor cuối được xác nhận18/11: ProceduralGame sạch/seed1/ngoàiPlay-pause-compile. Console lịch sử còn lỗi kết nối/plugin và warnings, không claim0Exception. Chưa Player/perf/manual mới. Bước tiếp G2 tối đa2candidate/nhóm nguyên nhân theo plan; chưa thử/chốt giá trị mới. S0–S9/D0–D4 bên dưới giữ DONE.

**17/11 — BUILD PLAN HOÀN TẤT trong phạm vi đã chốt: S0–S9 và D0–D4 DONE.** [S9 nghiệm thu/bàn giao](S9-Acceptance.md):81/81EditMode,18/18Player smoke/errors=[], build Windows cuối47.625s/0error-warning; CSV19191frames/89.998s/seed1/v3,world render đã inspect, median3.7341ms/p958.5298ms, request→Ready836.3275ms. XML/JSON/CSV/PNG/416fileSHA được lưu. FinalMCP ProceduralGame sạch/seed1/ngoàiPlay-pause-compile,0Exception; không job/Player test/build pending, không commit. **Hướng tiếp ngoài plan:** profile frame/GCspike và tải lớn, benchmark Release/onscreen/nhiều seed; cải thiện áp lực sống sót vì control không thao tác vẫn thắng. Không tự mở prestige/save/offline hoặc tuyên bố đã tối ưu mọi nền tảng. Các checkpoint đang-làm bên dưới là lịch sử.

**17/11 — cập nhật mới nhất:** người dùng test xong S8 và trả lời “oke mọi thứ ổn” cho checklist thắng/thua/replay/menu/lựa chọn. Đóng S8 ở phạm vi trận hoàn chỉnh và báo cáo cân bằng bước đầu; không suy thêm seed/ngày/độ khó cụ thể từ xác nhận chung. Đối chứng seed1 không thao tác vẫn thắng (hall8190/10000) giữ như giới hạn balance đã biết. **D4 đã triển khai/kiểm xong:** UI/cursor/fog/camera/input/bootstrap, pool4loại và worker qua menu/newfog, SampleScene/ProceduralGame;81/81regression PASS. [D4 evidence/exceptions](../DependencyInjection/D4-Validation.md). S9 đang tiếp tục: UI polling idle/busy1000lần mỗi loại đạt0B thread allocation, immutable definition đọc1lần; chưa frame benchmark/Player/startup phases. Không dùng draft cũ Temp/D4 để ghi đè source hiện tại.

**17/11 — đang tiếp tục:** MCP cloud/CLI đã xác nhận đúng Editor; đối chứng seed1 không thao tác cũng thắng ngày6, hall8190/10000, ví100×3 và0research/train/deposit. Raw `S8-ControlRuns-2026-11-17.txt`; không suy thành cân bằng đạt. Người dùng nhận test S8 khi Editor rảnh; đã Exit Play/bàn giao. Trong lúc chờ chỉ audit và draft ngoài Assets, chưa compile/applyD4. [Phạm vi/draft D4 và chuẩn bịS9](../DependencyInjection/D4-RemainingWork.md). Không tự chiếm Editor/reload/compile trong lúc user đang chơi.

**S8 PARTIAL —16/11/2026:** S8a hoàn thành luồng trận (sống5đêm→thắng ngày6; mất hall cuối→thua; terminal gates; result UI/replay/menu qua DI). Final64/64DOTS+17/17procedural PASS; fresh/repeat/6DI rejects/terminal/replay/menu PASS. S8b đã đo6policy trên0/1/1543, tất cả thắng với HP hall81–94%, chưa đặt kho landmark; giữ config baseline và ghi giới hạn. Người dùng báo “Chưa test được, để mục này chờ”; manual/cân bằng chưa nghiệm thu, không đánh dấu toànS8DONE. [Báo cáo đầy đủ](../MatchFlow/Architecture.md), raw `S8-ScriptedRuns-2026-11-16.txt`. Tiếp theo: nhận playtest thủ công, kiểm control/điều chỉnh một biến nếu xác nhận quá dễ, so lại6policy; sau khiS8DONE mớiD4/S9.

Các checkpoint S7 bên dưới là trạng thái trước S8.

**16/11 — S7 DONE trong phạm vi build plan.** Đã có region tint/path spine, cụm decor không collider, watchpost cạnh pad và glyph minimap sau khám phá; night palette dễ đọc hơn, timing không đổi. [S7 Evidence/ảnh](../ProceduralGeneration/S7-Presentation.md) ghi ảnh cùng seed/camera,3seed checks và xác nhận người dùng “Ổn, dễ nhận ra”. Regression59/59DOTS+17/17procedural PASS; tiếp S8, D4 toàn project vẫn TODO.

**S0–S7 và D0–D3 DONE.** S7 giữ kết quả S6, không đổi Core/gen/quota/nav; decor mới nhận deps tường minh. Không đồng nhất kết quả này với toàn project đã đạt DI: UI/fog/input/camera/renderer và ngoại lệ còn theo D4.

Bước tiếp **S8 — trận hoàn chỉnh/cân bằng bước đầu**: đọc S0 điều kiện thắng/thua và BuildPlan S8, kiểm game-over chặn command/restart, playtest3seed theo nhánh kinh tế/quân sự. Đo unlock/thu-nộp/quân/ngày thắng-thua rồi mới chỉnh config; xử lý48configured vs42spawn trong lịch đêm theo evidence S6. D4 toàn project trước S9; không lặp matrix S6/S7 nếu không có thay đổi/lỗi liên quan.

Không cần hỏi lại người dùng về tên/giá thử trong plan khi được yêu cầu tiếp tục implementation; đó là mặc định thử nghiệm có thể điều chỉnh và ghi lý do. Quyết định thay đổi phạm vi như prestige/save/load phải được tách rõ.

## Bảng trạng thái

Yêu cầu DI theo [Architecture DI](../DependencyInjection/Architecture.md); đã migrate D1/D2, wave/nodes/build/procedural/spawner/production/projectile và Unit/Worker/Sheep/Structure/AIPawn tới D3c4b, nghiệm thu worker pool qua reload PASS. UI/input/fog/camera/renderer và ngoại lệ còn lại theo Inventory/D4. Thứ tự: S0 → D0 → D1 → S1/S2 + D2 → S3 → S4/S5 → S6 + D3 → S7/S8 → D4 → S9.

| Lát cắt | Trạng thái | Kết quả/bằng chứng | Bước tiếp |
|---|---|---|---|
| S0 Baseline | DONE | Rerun 34/34 DOTS, 3/3 procedural, 18/18 Player smoke; người dùng xác nhận thao tác; xem Validation/Evidence | S8/S9 kiểm UI/ngày đêm có artifact |
| D0 Inventory/ADR DI | DONE | 149 call site ứng viên/31 file, bảng owner/lifetime, ADR-001; xem DI Inventory | D1 root và lifecycle |
| D1 Composition root/lifecycle | DONE | Inject manager/locators; Ready/failure gate; scene/domain reload và regression tests PASS; xem DI Evidence | S1 + D2 |
| D2 Progression qua DI | DONE | Root→ResearchService→IResearchService panel; fake/lifecycle/transaction PASS | Giữ nguyên ranh giới khi mở thêm tech |
| D3 Gameplay integration qua DI | DONE | D3a–D3c4b, binding/global-null/pool4loại/nav/SampleScene/direct unregister; worker pool qua menu→game PASS;59DOTS+17procedural | Giữ lifecycle; D4 audit UI/fog/input/camera/renderer và ngoại lệ toàn project |
| D4 Hoàn tất phạm vi DI | DONE | UI/cursor/fog/camera/input/bootstrap;9views/8types,16DI rejects;fog153/71,6rejects,pool/reload PASS;81regression; audit ngoại lệ | S9; xem D4-Validation |
| S1 Một tech trong ECS | DONE | economy_tools, wallet/state/tick/modifier; 43/43 DOTS tests và scene API checks | S2 manual/visual acceptance |
| S2 UI nghiên cứu | DONE | Người dùng xác nhận toàn bộ checklist thủ công đạt; không có artifact kèm theo | S3 |
| S3 Sáu tech | DONE | 58/58 DOTS tests, six-node integration, gates/debit, legacy và ảnh UI 1920×1080 | S4 vùng/preview map |
| S4 Vùng và preview | DONE | StrategicRegions v2 + preview; 10/10 tests, 128 strategic và128 legacy seed không lỗi; ảnh đã kiểm | S5 budget/topology cuối |
| S5 Budget và topology | DONE | Strategic v3, budget/landmark/final validator;17/17 tests,1000seed mỗi preset0lỗi | S6/D3 scene integration |
| S6 Map/wave/navigation | DONE | Matrix0/1/1543 economy/build/fog-camera/input +3chu kỳ/expanded base PASS; manual xác nhận; final pool reload+17procedural PASS, scene sạch seed1 | S7 nhận diện vùng/khám phá; quota ngoài đêm chuyển S8 |
| S7 Hình ảnh/khám phá | DONE | Region/path/pad tint +decor/watchpost không collider, glyph sau khám phá;3seed checks/ảnh cùng seed ngày-đêm, user xác nhận;59+17 PASS | S8 trận/cân bằng, giữ artifact S7 |
| S8 Trận và cân bằng | DONE | Luồng trận+6policy/report;user17/11 xác nhận checklist ổn;đối chứng không thao tác thắng là giới hạn balance đã biết | Không tuyên bố độ khó tối ưu |
| S9 Nghiệm thu | DONE | 81tests XML;18Player smoke;Windows build;startup phases;19191frames/GC/PNG/CSV/hash;S9-Acceptance có giới hạn | Ngoài plan:balance sâu/Release/onscreen/tải lớn |

## Checkpoint hiện tại

- **Đóng plan17/11:** dùng S9-Acceptance và D4-Validation làm nguồn mới nhất. Code source hiện tại đã gồm D4 và timing/perf automation; không copy Temp/D4-Draft cũ đè source. Player chạy từ `.utmp/s9-build-2026-11-17/RTSgame2026.exe` với thư mục Data đi kèm; manifest416files đối chiếu binary đã test. CI smoke cũ đã sửa fixture explicit register/near-base placement, không đổi gameplay để chiều test.
- **Known limits:** survival baseline5đêm còn dễ; single-active-gameplay-scene; offscreen Development/seed1, một startup sample; maxframe53.9553ms,GCspike1020544B chưa attribution. Không dùng S0benchmark quái hoặc testtime làm performance của worker; không coi fake-service polling0B là0GC toàn scene.
- **Khi tiếp tục:** phát triển ngoài plan phải chốt scope mới; nếu user báo bug, đọc đúng module/caller trước sửa và kiểm Unity. Giữ prefab/package/pipeline/Editor và các thay đổi có sẵn; không cần lặp toàn bộ6policy/matrix khi không có thay đổi liên quan.

- **S8 kỹ thuật đã kiểm:** `GameplayMatch.cs`, `MatchSession.cs`, `MatchSessionEditorTools.cs`, root/scene/generator wiring; gates ở gameplay/research/swarm/bridge/GameManager/clock/wave/menu. Native reset kết quả/goal về0 khi teardown; legacy opt-in không bị goal5 áp tự động.6DI rejects/repeat-init PASS; không thêm runtime global lookup ở consumer mới.
- **Bằng chứng cuối:**64/64DOTS2.8412262s +17/17procedural7.0509617s,0fail/skip;6run675.75s mô phỏng mỗi run,76spawn (12/15/15/15/19),không shield/hoàn máu/cấp tiền. Ảnh `Evidence/S8-defeat.png` và `Evidence/S8-victory-final.png`; ảnh `S8-victory.png` là render cũ lúc paused, không dùng làm final.
- **Cấu hình/giới hạn:** goal5đêm mới; timing60/30/45,giá6tech/quota/HP/prefab giữ nguyên. Quota là trần tiếp viện tới bình minh; UIwave là mốc bắt đầu. HP81–94% vàkhông expansion cho thấy balance chưa thuyết phục trong policy này; chưa kết luận độ vui/hai nhánh cân bằng. User yêu cầu để manual chờ.
- **Điểm bàn giao:** ProceduralGame sạch/seed1/ngoàiPlay-pause-compile;0Exception sequence>4171;3filemới cómeta. Không commit/job/CLI đang chờ; không sửa pipeline/package/prefab. Chưa buildPlayer/FPS/GC hoặc tìm nguyên nhân crash Editor cũ. Giữ toàn bộ thay đổi tồn tại trước phiên.

### Checkpoint S7 đã đóng (lịch sử)

- **S7 đóng16/11:** [Presentation evidence](../ProceduralGeneration/S7-Presentation.md), `Tools/ValidateS7.cs`, ảnh4file `Evidence/S7-final-{before/after}-seed1-{day/night}.png`. Ghi nhận người dùng xác nhận “Ổn, dễ nhận ra” cho landmark/hướng mở rộng và độ sáng đêm; không tự coi manual S6 cũ là S7 mới.
- **Implementation:** ProceduralMapPresentation nhận MapData/map/origin/fog từ root,3texture+material có sẵn qua refs. Tint region/path spine/pad;16điểm decor/vùng, ring đá và watchpost search4→10cell, không RNG toàn cục/collider/Unit, pad trống, sorting10 dưới actor/resource25. Minimap nhận presentation tường minh, vẽ tree/watchpost glyph chỉ sau explored, giữ khi ngoài vision; world silhouette dim0,5. Fog toggle phát OnFogUpdated ngay. Root scene+editor generator wire refs cùng code.
- **Night presentation:** shared DayNightCycleDefinition đổi màu/intensity0,1→0,35/vignette0,5→0,28/filter; timing60/30/45 không đổi, ảnh finalđêm đọc được hơn. Không import art/package/pipeline mới; sourceTinySwords trong project, texture/material gốc không sửa.
- **Kiểm S7:** seed1/0/1543 có16/15/14decor, mỗi seed1watchpost; actors25/resources150, no collider/Unit/pad occupied, nav/terrain/repeat init stable,1listener, missing-dependency reject. Unknown sites hidden → reveal glyph/world → explored memory/dim PASS cả3seed. Seed1 selection/ground/minimap pointer-drag PASS; nộp20wood50,50s và20gold61,20s; StoragePit landmark27,00s debit100/100,actor25→26 PASS.
- **Regression cuối S7:**59/59DOTS,0fail/skip,2,1626465s;17/17procedural,0fail/skip,5,3338967s. Đã inspect ảnh1920×1080 trước/sau với camera lock/ortho9 cùng seed, debug fog-off chỉ dùng chụp. Baseline controlled tắt S7 visuals và palette đêm cũ trong bộ nhớ; restore authored values trước Exit Play. Không dùng các draft Evidence làm ảnh final.
- **Môi trường cuối S7:** ProceduralGame sạch/seed1, isPlaying/isPaused/isCompiling=false; NightIntensity0,35 và NightDuration45,0Exception sequence>4134. Test-only reveal/camera/fog bỏ bằng Exit Play; không job/probe/commit mới. Chỉ scene refs và shared night visual asset thay đổi cùng implementation; giữ mọi thay đổi cũ của user.
- **Tiếp S8:** hoàn chỉnh điều kiện kết thúc theo S0, chặn command khi game-over và restart;3seed ×ưu tiên kinh tế/quân sự, ghi số liệu thật trước balance. Còn D4 và S9 build/perf/thesis. Chưa đo allocation/FPS hoặc Player, chưa mọi seed/preset; watchpost search hữu hạn có thể không tìm được chỗ ngoài3seed đã kiểm, ghi limit trong evidence.

### Checkpoint đóng S6/D3 — lịch sử, giữ bằng chứng nền

- **Đóng S6/D3 ngày16/11:** MCP tools đã xuất hiện lại và hoạt động, không dùng CLI để vượt approval. ProceduralGame ban đầu sạch/ngoài Play/compile; chỉ có cảnh báo plugin về path spaces.17/17procedural PASS,0fail/skip,8,1212562s. Không sửa gameplay từ code đã có59/59DOTS PASS1,0734240s; không chạy lại DOTS/matrix khi không cần.
- **Pool Unit qua unload/reload PASS:** CheckReady → PrepareReloadPool giữ worker inactive/unbound → GameFlowMenu.ReturnToMenu → CheckMenu (bridge/entities/projectiles/ví0/cờ sạch) → GameFlowMenu.Play/Editor Step → CheckReady (1bridge/25actors/132nodes/1wave listener/ví100x3, repeat BeginGameplay không nhân) → CheckReloadPool. Đúng cùng worker view vào scene mới, nhận bridge/game mới, nhận resource work, return không rò actor/dependency. Fixture tự tạo đã dọn.
- **Bằng chứng nền S6 giữ nguyên:** raw matrix0/1/1543, kho landmark, fog/minimap/camera/input và3chu kỳ/seed đã PASS; manual16/11 do người dùng báo.42/42lượt đến hạn theo lịch, configured48/6lượt ngoài đêm chuyển S8. Harness bảo vệ máu TownHall, không coi là cân bằng trận/benchmark/Player.
- **Môi trường đóng:** ProceduralGame sạch, seed1; isPlaying/isPaused/isCompiling=false. Console Exception sequence>4123=0; có log4128 plugin cảnh báo đường dẫn có spaces, không đổi tên/move project để né cảnh báo. Không job/probe/CLI còn chờ; không commit, không sửa scene/prefab/package/Editor/render pipeline hoặc thay đổi có sẵn của người dùng.
- **Tiếp S7:** đọc BuildPlan S7 và Procedural Architecture/Usage; chụp baseline cùng seed ngày/đêm trước chỉnh hình ảnh; dùng tile/prefab có sẵn tạo nhận diện region/landmark/minimap sau khám phá; kiểm selection/gather/nav sau decor. S8 phải xử lý quota ngoài đêm và balance/endgame bằng playtest, D4 audit toàn project, S9 build/performance/bàn giao. Không báo toàn game/khóa luận hoàn tất.

### Checkpoint trước khi đóng S6 — lịch sử, đã thay thế

- **Mới nhất16/11 — implementation/API nghiệm thu S6 và D3c4b đã chạy:** [D3c4b Evidence](../DependencyInjection/D3c4b-UnitLifecycle.md), [S6 final](../ProceduralGeneration/S6-FinalValidation.md), [raw log](../ProceduralGeneration/S6-FinalChecks-2026-11-16.txt). Người dùng mở lại Editor/cho phép tiếp; MCP đúnginstance RTSgame2026, trạng thái ban đầu sạch/ngoài Play/compile, chỉ cảnh báo path có spaces. Không tuyên bố sửa native crash.
- **Code đã chốt:** Unit.InitializeScene/SceneBridge/UnbindScene, AIPawn.Initialize/Unbind; không Get/Instance trong Unit/Worker/Sheep/Structure/AIPawn/Projectile. Bridge inject/bind/registry, raw pool acquire phải Register; worker clear carry/work/shelter, sheep reset claim/graze. Structure invalidate nav lúc bind/return/destroy. Direct bridge unregister dọn cả manager registry/Entity; teardown chỉ unbind, không callback xóa dictionary đang duyệt. Producer/projectile harness đã cập nhật callers.
- **Kiểm D3 thật:** Procedural binding25/repeat/3rejects/registry đủ, commands khi static bridge=null, pool Worker/Ranger/Sheep/Barrack +nav PASS. SampleScene sau code cuối cùng PASS cùng4nhóm trên và direct unregister/rebind/destroy. Producer pending UI cleanup/reuse/destroy/20giây không ghost, projectile native/legacy damage/pool/3caller PASS lại tại SampleScene.
- **Matrix S6 cuối:**0/1/1543 nộp20gỗ+20vàng; kho landmark hoàn tất30,00/27,00/32,50giây mô phỏng, đúng debit100/100, actor25→26; fog/minimap128×128/camera/selection-ground/minimap pointer-drag PASS. Ba chu kỳ/seed417,40giây mô phỏng, expanded radius60,80/59,15/60,86;42/42spawn đến hạn trong đêm,12/15/15,retreat0agents; actors26→26/26/25. Chỉ TownHall được bảo vệ máu trong harness, không phải balance/Player playtest.
- **Đầu mối S8:** configured quota48 cho ba đêm nhưng6lượt nằm sau bình minh45giây, nên actual42 theo lịch là đúng trong scope hiện tại. Không đổi duration/quota để chiều test. Test so48 ban đầu fail, đã đối chiếu lịch để tính42eligible; raw final và giải thích giữ trong S6 evidence.
- **Regression:** DOTS cuối59/59 PASS,0fail/skip,1,0734240s.17procedural chưa rerun sau D3c4b; kết quả17/17 ngày15/11 không được ghi thành mới. Raw logs/harness ngoài Assets đã lưu. Không benchmark hoặc Player build mới.
- **Manual16/11:** sau checklist hai nhóm menu→game→menu→game/tài nguyên/selection/minimap và ngày đêm/cung-tháp, người dùng báo “oke chạy được”. Ghi manual PASS theo báo cáo, không ảnh/log kèm; không suy ra pool nội bộ đã đạt. Không cần hỏi lại checklist đó khi tiếp.
- **Phần còn lại, thứ tự cụ thể:** (1) khi công cụ hoạt động, D3UnitValidation.PrepareReloadPool → MainMenu/CheckMenu → ProceduralGame/Start/CheckReady → CheckReloadPool; (2)17procedural, query Console, xác nhận Seed1/scene sạch/ngoài Play, cập nhật final evidence rồi mới đóng S6/D3. Không rerun matrix0/1/1543 nếu không có code/lỗi mới liên quan. Nếu muốn ảnh Game View làm artifact thêm thì capture/inspect, chưa có ảnh16/11 và không giả là đã chụp.
- **Block công cụ/môi trường:** lệnh chuẩn bị reload bị automatic approval review từ chối trước thực thi vì usage limit; không phải phán quyết unsafe. Không bypass/retry qua đường khác. Lệnh đó không tạo pending job; không còn lượt seed-check chạy. Lần quan sát MCP cuối là SampleScene đang Play/pause; sau đó người dùng tự test, trạng thái Editor hiện tại chưa xác minh. Asset preset trên disk vẫn Seed1, không SaveAssets/scene/prefab/package/render/Editor trong slice; không commit. Giữ thay đổi có sẵn của người dùng.

### Checkpoint trước khi restart16/11 — lịch sử, đã được thay thế bởi phần trên

- **16/11 — D3c4b IMPLEMENTED_UNVERIFIED, dừng do người dùng báo Editor crash:** MCP list/select đúng instance RTSgame2026; phiên chat chỉ có discovery tools, đã dùng Unity MCP CLI1.11.3 để đọc Editor sạch/ngoài Play và refresh/test. CLI truy cập server được khi chạy ngoài sandbox; npm trong sandbox lỗi DNS. Không thêm package vào project.
- **Code đang dở:** Unit.InitializeScene(game,map,bridge), SceneBridge, UnbindScene và BindDots guard; Awake bỏ Get, Start bỏ tự đăng ký. Bridge.Register inject rồi bind/đăng ký manager; Unregister/teardown unbind. Worker/Sheep dùng bridge đã truyền; Structure dùng map đã truyền, Humanoid/Structure guard trước Init; AIPawn Initialize/Unbind, bỏ Get/Instance. Worker return xóa carry/shelter/work refs; Sheep reset claim/graze. GameManager thêm guard danh sách trùng. Raw pool không tự re-register Unit, caller phải bridge.Register sau acquire; Tools/ValidateD3Production.cs đã cập nhật, projectile fixtures InitializeScene legacy rõ ràng.
- **Files cần đọc khi tiếp:** Assets/Game/Scripts/Units/Unit.cs, WorkerUnit.cs, Neutral/SheepUnit.cs, StructureUnit.cs, HumanoidUnit.cs; Scripts/AI/AIPawn.cs; DOTS/Integration/SwarmGameBridge.cs; Managers/GameManager.cs. Harness mới Tools/ValidateD3Units.cs: CheckBindings, CheckWithoutGlobalBridge, CheckPool; cập nhật ValidateD3Production.cs và ValidateD3Projectiles.cs. Chưa có evidence module D3c4b hoàn chỉnh; phải viết cùng kết quả kiểm thật.
- **Đã kiểm thật:** giữa slice DOTS59/59 PASS,0fail/skip,1,9146581s. Sau đó còn bổ sung Worker/Sheep reset và Structure teardown ordering; refresh cuối trả1, nhưng chưa có regression sau cùng. Console có lỗi compile giữa các lần sửa file khi caller chưa viết xong; không dùng log lịch sử đó làm kết luận compile hiện tại. Play entry trả accepted; hai CLI pause/check đang Connecting thì người dùng báo crash. Chưa có kết quả CheckBindings/CheckWithoutGlobalBridge/CheckPool, không suy đoán crash do code nào.
- **Lệnh chờ khi dừng:** đã xác định bốn node CLI và gửi cleanup có kiểm tra PID/command line; tới lúc cleanup chúng đã kết thúc. Read-back sessions: pause đã trả accepted, CheckBindings trả NullReferenceException tại snippet line19, chưa điều tra; hai check sau không chạy vì loop dừng khi lỗi. Kiểm tra process cuối không còn Unity MCP CLI chờ. Không đóng Editor của người dùng hoặc tự gửi thêm lệnh Editor sau yêu cầu dừng. Không commit. Không sửa scene/prefab/config/preset/package/render pipeline trong D3c4b. Giữ mọi thay đổi có sẵn.
- **Bước tiếp sau khi người dùng mở lại:** (1) kiểm MCP và lỗi compile hiện tại, không tự Enter Play ngay; xem log crash nếu có; (2) kiểm registration recursion/list duplicates, sheep không vào manager list, map teardown/return, explicit rebind, rồi chạy D3UnitValidation trên ProceduralGame và SampleScene cùng production/projectile/lifecycle regressions; (3) tiếp ba seed S6 kinh tế/xây/wave/input-camera-minimap/nhiều ngày, đủ bằng chứng mới đánh dấu S6/D3. Manual D3c4a chưa được xác nhận riêng. D4/S7/S8/S9 chưa thực hiện.

- **Điểm tiếp mới nhất15/11 — D3c4a:** [Projectile factory/startup evidence](../DependencyInjection/D3c4a-ProjectileFactory.md), `Tools/ValidateD3Projectiles.cs`. Projectile không lookup bridge; Tower/Ranger/EnemyRanger gọi GameManager factory dùng bridge đã cấp. ECS sinh Entity trực tiếp, legacy pool nhận owner Scene; native dùng proxies đã bound, reject thiếu/dead/gate/pause/cross-scene, không ngầm register hoặc fallback legacy cho DOTS actor.
- **Kiểm D3c4a:**10rejectcases/scene không tạo actor/đạn/view; native bắn1Entity/0legacy view, damage7 đúng một lần sau2giây mô phỏng tại ProceduralGame và SampleScene. Legacy fixture collision/reuse7+9damage, foreign-target reject và projectile pool về owner scene PASS; ba caller đúng context/pending callback lặp không duplicate. Đây chưa phải toànlegacy scene hoặc Unit cross-scene pool.
- **Startup sửa theo evidence:** menu→game trước sửa rootReady/wallet0/listener0; root nay gọi GameManager.BeginGameplay sau BindScene, Start legacy riêng. Menu→game→menu→game sau sửa PASS25actors/132nodes/ví100x3/1wave+1manager listener, repeat BeginGameplay không nhân ví/listener; menu entity/projectile/ví0/cờ sạch.
- **Regression cuối D3c4a:**59/59 DOTS (2,0068762s),17/17 procedural (6,9916392s),0fail/skip; không Exception mới sequence>3507 sau integration cuối. `ValidateS6Lifecycle.cs` thêm repeat-start assertions. Manual D3c3 train/pause/destroy được người dùng xác nhận “oke tất cả ổn tiếp tục đi”; không artifact kèm theo.
- **Bước tiếp D3c4b:** Unit.Initialize/bind/registration/unbind/return và field manager/bridge/map; Worker/Sheep globals, Structure map, AIPawn Get/Instance và mọi caller. Giữ RTS.DOTS độc lập. Manual D3c4a: menu reload ví ban đầu + cung/tháp bắn/pause. S6 hình ảnh/input/nhiều ngày và D4 inventory vẫn còn; không chuyển S7 hoặc đánh dấu toàn D3/S6 DONE.
- **Môi trường cuối D3c4a:** ProceduralGame sạch/ngoài Play/compile/pause; preset/seed không đổi, fixtures đã bỏ, không probe/job/commit mới. Không sửa scene/prefab/package/render/Editor. Các thay đổi plugin/package/skills có sẵn giữ nguyên. Chưa Player/benchmark/fullcombat/animation hoặc legacy không ECS trọn scene.

- **Điểm tiếp mới nhất15/11 — D3c3:** [Production evidence](../DependencyInjection/D3c3-Production.md), `Tools/ValidateD3Production.cs`. ProductionBuilding nhận game/bridge tại bind; không tự lookup. Actions chỉ debit sau enqueue accepted. Native reject invalid duration/dead; queue khi đang xây vẫn chờ hoàn thành như contract cũ. Unregister/return/destroy dọn producer refs/UI/maps.
- **Kiểm D3c3:** Procedural14rejectcases không side effect; villager+warrior8giây, archer sau unlock thật8giây, FIFO hai dân hai frame16giây; một debit/lệnh, duplicate không debit, bound views/AIPawn off/ECS exit command, UI/maps sạch. SampleScene training và producer pool lifecycle PASS. Pool Spawn nhận owner Scene bắt buộc, ba projectile callers cập nhật; sửa object reuse còn ở DDOL scene. Invalid scene/return/reuse/destroy/20giây không ghost spawn PASS. Fog thiếu runtime arrays/map defer/restore PASS; chưa migrate toàn fog/pool.
- **Regressions cuối D3c3:**59/59 DOTS (2,1416279s),17/17 procedural (7,1246614s),0fail/skip. Menu cleanup/game reload Ready PASS; không Exception mới sau sequence3507 trong checks cuối, giữ log lịch sử. Test FIFO ban đầu assertion moving cuối queue không phù hợp vì quân đầu đã đến nơi; đã sửa kiểm ngay lúc spawn và rerun PASS, không sửa gameplay để chiều test.
- **Bước kế tiếp sau D3c3 (đã tiến D3c4a ở trên):** D3c4b inventory/migrate Unit/Structure/Worker/Sheep/AIPawn và factory/pool refs còn lại. Manual D3c3 đã được xác nhận; S6 input/visual/nhiều ngày chưa nghiệm thu. Chưa Player/performance/Unit cross-scene pooled reuse hoặc legacy không ECS thuần. Không gọi D3/S6 DONE.
- **Môi trường cuối D3c3:** ProceduralGame sạch, ngoài Play/compile/pause, preset/seed không đổi; không job/commit mới. Fixtures bỏ bằng Exit Play. Không sửa scene/prefab/package/Editor/render pipeline trong D3c3; giữ các thay đổi có sẵn từ trước.

- **S6a/D3a ngày12/11:** ProceduralGame và scene generator dùng asset StrategicProceduralMapSettings mới, seed cuối1. Authoring dùng chung origin terrain/resource/gateway, spawn vào trong2cell và chặn reapply. Root bắt buộc Waves/day-night; EnemyWaveManager nhận dependency tường minh, không Find manager hoặc bridge.Instance, Init lặp không nhân listener. SampleScene được inject từ bootstrap legacy.
- **Bằng chứng:** [S6 Integration](../ProceduralGeneration/S6-Integration.md), `Tools/ValidateS6.cs`. Seed0/1/1543 đều25actors,120cây/12vàng/18cừu,5spawn kiểm tra, hai gateway fit radius thật; root/binding lặp không duplicate, gate blocked từ chối spawn. Hai probe/seed tới phạm vi10 quanh TownHall khoảng25,65–26,90giây mô phỏng. SampleScene: một bridge/25actors/một listener/spawn ring PASS.
- **S6b/D3b ngày13/11:** ResourceNodeBase nhận bridge/map từ RegisterResource, không còn Find/Get/Instance; query/claims/navigation/OnDestroy dùng deps này. Unregister và bridge teardown unbind view. Ba seed0/1/1543: hai worker đi khai thác Economy/GoldFrontier rồi nộp20gỗ+20vàng, ví120/120/100; StoragePit tại pad landmark thêm đúng1actor và trừ100/100. [Bằng chứng S6b](../ProceduralGeneration/S6b-EconomyResourceDI.md), `Tools/ValidateS6Economy.cs`.
- **Kiểm mới nhất:**132resource node/seed đã inject; repeat/missing/capacity/collider/nav/unregister/rebind/destroy PASS. SampleScene50nodes/25actors/wave spawn PASS. Construction hoàn tất27/30/29,5giây mô phỏng; fog/minimap128×128/camera focus API PASS. Seed1 fog check ban đầu chạy trước Start không đạt, tách Editor Step rồi assert registry/fog PASS; không sửa fog để che precondition. DOTS58/58, procedural17/17 regression PASS.
- **S6c ngày13/11:** sửa timer dư gây wave2/3 trễ hơn UI2/4giây; Tick nội bộ nhận deltaTime,0/invalid không spawn/advance. So seed1 thấy spawn biên chỉ6/12quái tới gần căn cứ, legacy11/12; wave nay sinh trên tuyến L của từng gateway tại radius legacy, không bắt buộc cửa biên. API cửa biên cũ giữ cho topology. SpawnEnemy tự tính lại radius, không phụ thuộc đã vẽ vòng range.
- **Bằng chứng S6c:** [Waves/lifecycle](../ProceduralGeneration/S6c-WavesLifecycle.md), [raw log](../ProceduralGeneration/S6c-WaveChecks-2026-11-13.txt), `Tools/ValidateS6Waves.cs` và `ValidateS6Lifecycle.cs`. Seed0/1/1543 strategic12/12/9quái tới phạm vi10, legacy10/11/8; đợt đầu strategic5,42/5,50/5,82giây. Mỗi đêm45giây12spawn/3wave, mốc0,02/14,10/30,20giây, đủ quota; alive retreat/targetable/cleanup11,98giây PASS. Đây là orchestration tick thật clock/ECS, chưa Player playtest/damage balance.
- **Lifecycle mới:** MainMenu còn Active/Ready cũ trước sửa dù entities/ví đã dọn. EndComposition nay xóa map/error flags; MainMenu→game→menu→game PASS một bridge/25actors/132nodes/một listener/ví100; menu entities/ví0/cờ sạch. S6 Run smoke PASS lại. Regression sau sửa58/58 DOTS (1,8726907s),17/17 procedural (5,1120699s).
- **D3c1 cuối ngày13/11:** BuildingProcess nhận action/position/worker/game/bridge qua constructor5tham số; không Get/Instance/Find. GameManager và hai harness/tool callers đã cập nhật; branch ECS gửi AssignConstruction qua bridge được truyền. Constructor validate trước spawn/UI; GameManager chặn worker null/dead/unbound trước finalize/debit.
- **Bằng chứng D3c1:** [Construction DI](../DependencyInjection/D3c1-Construction.md), `Tools/ValidateD3Construction.cs`, caller S6Economy đã cập nhật. ProceduralGame8rejectcases giữ actors25/queue0/ví100; placement3case không finalize/debit; StoragePit trừ100/100,25→26actor/sprite complete/queue xóa, fog-minimap-camera sauStart PASS; destroyed-view/Entity an toàn khi gọi Update. SampleScene7rejectcases (tech mở) và construction15giây mô phỏng PASS. Regression sau code cuối58/58 DOTS (1,9266349s),17/17 procedural (5,5503457s).
- **D3c2 ngày15/11:** Authoring có mảng LegacySpawners Inspector, không Find runtime; scene/editor-generator wire một spawner. ResourceSpawner Awake chỉ local collider setup, Initialize nhận game/map/bridge, root inject suppressed procedural, legacy bootstrap inject trước grid/bind. API TryRegenerate chặn uninitialized/suppressed/dependency mất/gate, validate config trước clear; spawn ban đầu view-only rồi BindScene, regenerate bound dùng factory bridge.
- **Bằng chứng mới nhất:** [D3c2 Evidence/checklist](../DependencyInjection/D3c2-ProceduralSpawners.md), `Tools/ValidateD3Spawners.cs`. Procedural seed1:120cây/12vàng/18cừu/25actors/150resourceEntities, suppressed/repeat/replacement validation PASS. SampleScene:30cây/20vàng/18cừu/25actors/68resourceEntities, initial/repeat/regenerate giữ count; view/Entity cũ dọn, worker hủy node cũ, nodes mới inject,nav version/cả hai grids và collider blocker PASS. Giao cây mới nộp20gỗ sau21,90giây mô phỏng. Invalid config/deps không clear resources.
- **Lifecycle/regression sau D3c2:** menu→game→menu→game/Ready/cleanup PASS;58/58 DOTS (1,9211446s),17/17 procedural (5,2637561s),0fail/skip sau code cuối. Editor generator đã compile nhưng chưa chạy lại toàn menu refresh UI; hai consumer không còn Find/Get/Instance trực tiếp, bootstrap legacy là ngoại lệ có phạm vi.
- **Manual D3c2 PASS theo báo cáo người dùng ngày15/11:** (1) ProceduralGame khai thác/P không regen hoặc nhân resource; (2) SampleScene khai thác→P→chọn quân/giao cây mới→nộp. Người dùng nói tất cả ổn; không ảnh/log đính kèm, không chuyển thành số đo hoặc nghiệm thu các mục khác.
- **Điểm tiếp sau D3c3/S6:** Unit/Worker/Structure/Projectile adapters và factory/pool dependencies còn theo Inventory. Còn camera/minimap click/hình ảnh, nhiều ngày/expansion radius và wave tự nhiên. Có thể tiếp implementation độc lập khi chờ manual report, chưa đánh dấu D3/S6 DONE hoặc chuyển S7.
- **Giới hạn:** S6b kiểm kinh tế riêng/StoragePit2×2/API fog-camera, không mọi prefab/click UI; resource pooling chưa nghiệm thu. S6c tiến clock ngày/đêm/wave và hai ECS cùng dt0,02, không gọi mọi MonoBehaviour/UI như Player. Seed1543 có3quái chưa tới phạm vi10 trước sáng; cleanup dựa timeout không chứng minh mọi quái đi ra cửa. Approach phụ thuộc tuyến L/Routes; căn cứ quá lớn có thể hết điểm spawn. Không Player build/benchmark mới.
- **Crash/restart:** phiên trước người dùng yêu cầu dừng sau native crash. Sau mở lại, MCP kết nối và các Play checks chạy được. Stack cũ ở SerializationCache/clone WaveProgressUI, chưa biết root cause/chưa tuyên bố sửa crash; MCP mất tạm khi domain reload rồi nối lại là trạng thái khác.
- **Điểm dừng ngày15/11 cuối D3c2:** code/docs/tools và reference array ProceduralGame đã lưu; Editor ProceduralGame sạch, không Play/compile/pause, preset strategic/seed1; không probe/job chạy. Không commit mới, không sửa SampleScene/prefab/package/render pipeline/Editor trong slice này. Những thay đổi plugin/package/skills có sẵn giữ nguyên; reconnect khi domain reload không được coi là native crash mới.

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

### 16/11/2026 — S7 DONE: nhận diện vùng/landmark và khám phá

- Kiểm MCP đúngProceduralGame sạch, chụp baseline trước sửa. Tái dùng TinySwords bush/rock/black tower/material; thêm presentation DI, root/minimap/editor refs, region/path/pad tint, bounded decor không collider và explored markers. Fog toggle notify và shared night palette chỉnh readability, không đổi timing/Core/quota/combat/pipeline.
- S7 checks3seed, glyph/world discovery memory, input/gather/build PASS.59/59DOTS+17/17procedural PASS sau code cuối.4ảnh final cùng seed/camera ngày/đêm đã inspect, người dùng xác nhận dễ nhận ra. Tài liệu module/README/Architecture/Inventory/Overview cập nhật cùng code.
- Editor cuối ProceduralGame sạch/seed1/ngoài Play-pause-compile; restored night values, no newException/job/commit. Tiếp S8; chưa Player/performance hoặc fullSeed matrix mới.

### 16/11/2026 — S6 và D3 DONE theo build plan

- Công cụ MCP trực tiếp hoạt động lại. Chạy17/17procedural8,1212562s và bài worker pool qua menu/game mới PASS; Ready idempotent, menu cleanup và resource work sau rebind đều đạt. Không thay gameplay và không chạy lại59DOTS/matrix đã đạt vì không có thay đổi liên quan.
- Final read-back: ProceduralGame sạch/seed1/ngoài Play-pause-compile; không Exception mới sequence>4123, chỉ cảnh báo path spaces. Không commit/job/CLI pending. Cập nhật S6/D3 evidence, Progress/BuildPlan/Overview/Inventory/README. Tiếp S7; S8/D4/S9 vẫn chưa làm, chưa Player/perf/crash-root-cause claim.

### 16/11/2026 — D3c4b/API S6 PASS; chốt tài liệu, còn2kiểm cuối do tool review limit

- Tiếp sau user restart, kiểm MCP và59DOTS trước Play. Fix test precondition/prefab lookup, chạy binding/global-null/pool/nav, tiếp matrix ba seed và nhiều ngày với raw log. Chốt direct unregister cleanup ở bridge, unbind chỉ clear refs khi teardown; regression59/59 sau code cuối và SampleScene/unit/producer/projectile checks PASS.
- Người dùng xác nhận checklist menu/input/ngày đêm/cung-tháp chạy được. Đã viết D3c4b và S6 final evidence, cập nhật Inventory/Architecture/Overview/README cùng implementation. Không thêm package/scene/render changes, không Player/perf/crash-root-cause claim.
- Lệnh PrepareReloadPool/menu-game bị automatic approval review chặn do usage limit, không thực thi; không bypass. Còn Unit pool qua reload +17procedural và environment read-back; S6/D3 PARTIAL, chưa S7. Hướng tiếp đầy đủ ở checkpoint đầu trang, không cần làm lại matrix đã PASS.

### 16/11/2026 — D3c4b đang dở, người dùng yêu cầu dừng vì Editor crash

- Người dùng yêu cầu hoàn thành S6; đã kiểm đúng MCP instance, dùng CLI do tools Editor không xuất hiện trong chat. Tiếp Unit dependency/lifecycle theo inventory, thay đổi code/harness như checkpoint trên. DOTS giữa slice59/59 PASS; code cuối mới refresh, Play checks chưa có kết quả.
- Người dùng báo Editor vừa crash và sẽ mở lại. Đã dừng thao tác, hủy các CLI chờ; chưa xác định nguyên nhân crash. Không tiếp tục cho tới khi người dùng báo sẵn sàng; không gọi D3c4b hoặc S6 DONE. Tất cả thay đổi chưa commit còn trong workspace để review/test sau restart.

### 15/11/2026 — D3c4a projectile factory/startup API PASS; D3/S6 — PARTIAL

- Ghi nhận người dùng xác nhận toàn checklist D3c3; không artifact. MCP đúngproject/scene sạch trước làm. Rà Unit/Worker/Structure/Sheep/AIPawn và mọi projectile/pool caller; chọn slice nhỏ projectile factory, để Unit graph sang D3c4b.
- GameManager nhận context shot và dùng bridge đã inject; Projectile bỏ static bridge, chỉ legacy movement/damage/pool. Ba caller cập nhật; native method đổi sang TrySpawnProjectile, không ProxyFor ngầm bind. Native/legacy damage, invalid/pause/gate, reuse/scene ownership và callers checks PASS tại Procedural/Sample theo [D3c4a evidence](../DependencyInjection/D3c4a-ProjectileFactory.md).
- Phát hiện reload rootReady nhưng ví0/listener0; chuyển BeginGameplay idempotent về root sau BindScene. Rerun menu→game→menu→game PASS, ví/listener không nhân khi gọi lặp. Không Exception mới trong Console cuối; regression59/59+17/17 sau code cuối. Không sửa native crash cũ.
- Dừng an toàn ở ProceduralGame ngoài Play/compile/pause, scene sạch, không job/commit mới; preset/seed/scene/prefab/package không đổi trong slice. Tiếp D3c4b Unit/Worker/Structure/Sheep/AIPawn lifecycle; manual D3c4a mới còn chờ, chưa Player/perf/fullcombat.

### 15/11/2026 — D3c3 production API đã kiểm; D3/S6 — PARTIAL

- ProductionBuilding Initialize/unbind qua bridge, wrappers enqueue trả bool; ba Actions owner guard và debit sau accepted. Giữ FIFO/duration/cost/tech và queue-during-construction, reject invalid duration/dead ở native. Spawn view gửi movement ECS; teardown/return/destroy dọn refs/UI/maps.
- Pool Spawn nhận Scene tường minh, sửa producer reuse còn DDOL scene; cập nhật mọi caller. Fog guard map/arrays chưa ready được kiểm defer/restore. Đây chưa phải toàn pool/Unit/fog DI. Không sửa crash native cũ.
- Procedural14rejects, worker/warrior/archer, hai dân FIFO16giây và producer pool lifecycle PASS; SampleScene training/pool PASS; menu/game reload PASS. Regression59/59+17/17 sau code cuối. [D3c3 Evidence](../DependencyInjection/D3c3-Production.md) ghi cách chạy, giới hạn và manual còn chờ.
- Điểm dừng: Editor ProceduralGame sạch ngoài Play/compile/pause; không job/commit. Tiếp D3c4 inventory/factory/pool/adapters; chưa Player/perf hoặc manual D3c3. Thay đổi plugin/package/skills có sẵn giữ nguyên.

### 15/11/2026 — Manual D3c2 được người dùng xác nhận

- Người dùng báo “oke tất cả ổn nhe” sau checklist hai mục P/click/giao lại resource. Ghi nhận manual PASS theo báo cáo, không artifact đính kèm. Tiếp D3c3 production/train/factory/pool; D3/S6 tổng thể vẫn PARTIAL.

### 15/11/2026 — D3c2 procedural/spawner API đã kiểm; D3/S6 — PARTIAL

- Kiểm MCP đúng RTSgame2026, scene ProceduralGame sạch/ngoài Play/compile trước làm. Authoring dùng explicit LegacySpawners; root/legacy bootstrap inject ResourceSpawner, Awake không lookup/spawn; editor generator và scene refs cập nhật cùng code. Giữ quota/core/prefab/ECS runtime.
- Procedural suppression/repeat/missing/replacement config checks PASS. SampleScene initial/regenerate nhiều lần giữ30/20/18resource views,25actors/68resourceEntities; dọn views/entities cũ, cancel work, bind nodes mới/nav/collider PASS; giao cây mới nộp20gỗ. Menu reload và regression58/58+17/17 PASS sau code cuối. [D3c2 Evidence](../DependencyInjection/D3c2-ProceduralSpawners.md) ghi method/timing/giới hạn.
- Chưa keyboard P/click thực; đã gửi checklist2mục, chưa có xác nhận người dùng. Chưa Player/benchmark, failure fallback không root, mọi quota/area hoặc menu scene-generator UI refresh. Tiếp D3c3 production/train/factory/pool, không coi toàn project đạt DI.
- Cuối phiên ProceduralGame sạch, ngoài Play/compile/pause/seed1; test views dọn bằng Exit Play, không job/commit mới. Scene chỉ cập nhật refs bằng Unity serialization; không chỉnh plugin/package/skills đang có thay đổi từ trước.

### 13/11/2026 — D3c1 BuildingProcess đã kiểm; D3 — PARTIAL

- MCP đúng RTSgame2026, ngoài Play/compile trước làm. Đọc overview/plan/DOTS/DI và mọi caller của BuildingProcess. Constructor nhận manager/bridge; bỏ service lookup, validate trước spawn/UI; GameManager guard worker trước finalize/debit. Cập nhật GameplaySmokeAutomation và S6Economy caller, không giữ overload lookup.
- Unity ProceduralGame:8dependency/gate rejects không side effect;3placement worker cases không debit; StoragePit hoàn tất,actor25→26,chi phí100/100,queue xóa; fog/minimap/camera sauStart PASS; destroyed-view không dereference Entity cũ. SampleScene7rejects (capabilities mở), construction15giây mô phỏng vàqueue PASS.
- Regression58/58 DOTS +17/17 procedural,0fail/skip, sau code cuối. Không Player smoke/build/benchmark hoặc scene không ECS mới. [D3c1 Evidence](../DependencyInjection/D3c1-Construction.md) có cách chạy và giới hạn. Còn procedural/spawner/production/train/factory/pool theo D3c2; không coi D3/S6 DONE.
- Editor cuối ProceduralGame sạch/ngoài Play/compile/pause/seed1; không job/commit mới. Các object test được dọn bằng Exit Play, asset/prefab/scene không sửa.

### 13/11/2026 — S6c wave/lifecycle đã kiểm; S6/D3 — PARTIAL

- Unity MCP đúng dự án; đọc checkpoint/BuildPlan/DOTS trước làm. Tách tick timer và ngăn delta0/invalid sinh wave; sửa spawnTimer dư giữa wave. Đo border strategic seed1 yếu hơn legacy; chuyển wave sang approach centerline bảo vệ tại cùng radius legacy, giữ API biên và quotas/interval/night.
- Ba seed0/1/1543, cả strategic/legacy:12spawn/3wave/45giây, mốcUI đúng trong0,2giây, retreat/untargetable/cleanup PASS. Arrival-count/delay cụ thể và các kết quả cũ trong [S6c Evidence](../ProceduralGeneration/S6c-WavesLifecycle.md) + raw log; chưa khẳng định cân bằng damage hoặc hết ùn quái.
- Sửa cờ procedural còn Active/Ready khi về menu. Luồng menu→game→menu→game và S6 smoke PASS sau Start/EditorStep; regression58/58 DOTS,17/17 procedural,0fail/skip sau sửa. Không Player build/benchmark.
- MCP có đợt handshake offline khi chuẩn bị lifecycle, chưa gửi lời gọi Play; tự nối lại rồi kiểm tiếp thành công. Cuối phiên ProceduralGame sạch, preset1/seed1, ngoài Play/compile/pause; không job/commit. Tiếp D3c và nghiệm thu UI/input S6; không đánh dấu S6 hoặc D3 DONE.

### 13/11/2026 — S6b/D3b đã kiểm; S6/D3 — PARTIAL

- Đã kiểm Unity MCP đúng RTSgame2026, Editor ngoài Play/compile. Đọc TechnicalOverview/BuildPlan/DOTS guide/DI trước tiếp; migrate ResourceNodeBase bridge/map injection tại RegisterResource và unbind khi unregister/teardown. Không sửa Runtime ECS/prefab/quota/giá.
- Ba seed0/1/1543: cây/mỏ132node/seed init, claims/collider/nav/rebind/destroy PASS; worker tới hai vùng/nộp20gỗ+20vàng; StoragePit100/100 ở landmark xây xong, actors25→26; fog/minimap/camera API checks PASS theo giới hạn bằng chứng. SampleScene50nodes/25actors/wave spawn PASS.
- Tests DOTS58/58 (1,9003524s), procedural17/17 (5,2850361s),0fail/skip. Không Player/benchmark mới. Tool/test [S6b Evidence](../ProceduralGeneration/S6b-EconomyResourceDI.md) ghi cách chạy, thông số, lần fog check trước view Start không đạt và cách kiểm lại đúng lifecycle.
- Cuối phiên ProceduralGame sạch, seed1, ngoài Play/compile/pause; Exit Play dọn objects thử; không job/commit mới. Tiếp S6c/D3c; wave đêm/áp lực và các nhóm DI còn lại chưa hoàn tất. Không tuyên bố sửa native crash cũ.

### 12/11/2026 — S6a/D3a đã kiểm; S6/D3 — PARTIAL

- Đã lưu gateway world positions/chặn reapply trong Authoring, root→wave Initialize và legacy bootstrap trong bridge; scene/editor generator nhận Waves và asset strategic riêng (seed cuối1). Không đổi RTS.DOTS runtime, package hoặc prefab.
- Unity MCP sau người dùng restart: ba seed0/1/1543 smoke spawn/resource/radius/DI/listener/readiness PASS; probe tới gần TownHall sau25,65–26,90giây; SampleScene legacy một bridge/25actors/spawn ring PASS. Có test chạy sau smoke seed1543 vượt120giây; đã ghi riêng, không tính pass hoặc kết luận sửa ùn quái.
- Regression DOTS58/58 và procedural17/17 PASS, không fail/skip. [S6 Integration](../ProceduralGeneration/S6-Integration.md) và `Tools/ValidateS6.cs` ghi checks tái chạy được. Không Player build/benchmark mới; root cause crash phiên trước chưa xác định.
- Editor cuối ProceduralGame sạch, không Play/compile/pause; không còn probe/job đang chạy. Chưa commit. Tiếp S6b/D3b theo checkpoint; không đánh dấu S6 hoặc toàn D3 DONE.

### 12/11/2026 — S5 — DONE (Core/preview)

- Unity MCP xác nhận đúng RTSgame2026, không Play/compile trước làm. Core strategic v3 có budget theo region, cluster3gold, pad/start exits, footprint checks,3×3clearance tới gateway/landmark và chặn cửa biên ngoài đăng ký. Legacy branch giữ nguyên.
- Unity17/17 pass gồm fixtures tree-wall/gold-water/diagonal/landmark-island/extra-entrance và cấu hình thiếu vàng. Quét1000strategic +1000legacy không lỗi/tái lập; histogram và diagnostics trong S5-SeedCheck. CLI cần quyền ghi obj; một scan MCP timeout không được tính kết quả.
- Preview seed0version3: gateways2,maxResourcePath118,gold12; ảnh đã kiểm. Default runtime asset vẫn LegacyMainland. Không test gameplay/collider/wave preset mới; phạm vi S6.
- Cập nhật docs/preview/tool csproj/tests cùng code; không sửa prefab/package/scene gameplay. Tiếp S6/D3 theo checkpoint.

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
