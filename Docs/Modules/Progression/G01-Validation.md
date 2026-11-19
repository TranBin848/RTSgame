# G0–G1 — baseline và kiểm chứng policy phòng thủ

Thí nghiệm Unity ngày **18/11/2026**; đối chiếu source và chốt tài liệu ngày **19/11/2026**. **G0 và G1 hoàn tất phạm vi đo/chẩn đoán**, không phải nghiệm thu cân bằng. G2–G7 chưa thực hiện. [Hợp đồng đo và cách chạy](G01-MeasurementContract.md), [kế hoạch](NextPhasePlan.md), [raw và summary](Evidence/G01-2026-11-18/summary.json).

Đây là checkpoint **trước G2** với Hall10000. Trạng thái tiếp theo: [G2 đã chọn shortlistHall1600](G2-PressurePilot.md); không dùng mô tả trạng thái tại checkpointG01 bên dưới làm trạng thái hiện hành của scene.

## 1. Mục tiêu, phạm vi và thay đổi

Mục tiêu là xác định quân được mua có thực sự tham chiến và chuẩn hóa đối chứng trước khi chỉnh HP hoặc wave. Giữ nguồn authoritative ECS, prefab và bridge hiện có. Không đổi HP, wave, phase60/30/45, giá/duration research hoặc huấn luyện, Editor, package hay render pipeline.

Thêm `Tools/PlaytestG01.cs` ngoài Assets để chạy qua Roslyn/MCP; không thêm diagnostic vào tick production. Harness dùng command/factory/placement thật, ghi ledger và actor snapshots. Sửa control peak, nhãn not attempted, phân công worker ổn định và rally một lần cho quân mới. `Tools/SummarizeG01.mjs` đối chiếu raw bằng assert, không cần npm dependency. Raw S8 và pilot được giữ nguyên.

Một lỗi presentation được xác nhận và sửa tại `Unit.ApplyDotsSnapshot`: không gửi trigger `Attack` cho `GameplayKind.Worker`, vì worker đã được `ApplyDotsWorkerPresentation` điều khiển bằng bool `isAttacking`. Caller chung là bridge `SynchronizeGameplay`; không đổi command, AI, damage hoặc animator controller. `Tools/ValidateG01Presentation.cs` là self-check trước/sau trong Unity.

## 2. Baseline và cấu hình resolve

Unity **6000.3.11f1**, scene `Assets/Scenes/ProceduralGame.unity`, layout strategic version3, map seeds0/1/1543. Wave seed8001 tách khỏi map seed; dt0.05 giây, goal5đêm. Mỗi lượt thắng đầu ngày6 tại **675.75 giây mô phỏng**; sai khác với675giây danh nghĩa do bước rời rạc float/phase transition, không phải FPS hoặc thời gian wall-clock.

Hall scene có override `m_Health=10000`; prefab `Assets/Game/Prefabs/Units/Buildings/TownHall.prefab` là100. Mỗi run assert `MaxHealth=CurrentHealth=CombatBody.Health=10000` trước tick. SampleScene cũng có override10000 theo đối chiếu source YAML chỉ đọc; không mở SampleScene/Entity để kiểm lại trong đợt này. Không reset scene về prefab100.

Ví ban đầu100W/100G/100M. Quân/công trình player ban đầu:3worker,1archer,1TownHall,1Barrack,1ArcheryRange. Cừu/resource theo map có snapshot trong từng raw. Có2enemy author sẵn được ghi `initial=True`, ở trạng thái Retreating lúc bắt đầu; số76spawn sau đây là delta wave, không cộng2enemy đó. Counter arrival/retreat có thể bao gồm chúng; không ép các counter này thành phép phân hoạch của76.

| Tech | W/G/M | Duration (s) | Modifier/gate |
|---|---|---:|---|
| economy_tools | 40/15/0 | 20 | WoodMultiplier1.25 |
| economy_cargo | 80/30/0 | 30 | CargoMultiplier1.5; cần Tools |
| military_archery | 50/30/10 | 25 | mở archery |
| military_training | 80/60/20 | 35 | TrainingMultiplier0.8; cần Archery |
| expansion_tower | 60/25/0 | 25 | mở tower |
| expansion_fortify | 100/60/0 | 40 | TowerMultiplier1.25; cần Outposts |

Worker cost0W/0G/10M; warrior/archer0W/20G/30M; cả ba TrainDuration resolve8s trước modifier. Kho StoragePit100W/100G, ConstructionTime10s. Asset serialized snapshot ở `authoring.json` và `extra-authoring.json`; riêng worker train duration dùng field initializer8 vì asset không serialize override. ResearchDefinition resolve thực tế cũng lưu trong từng raw.

Config ID matrix: `B4F61496CA56A0412C05A630F4857CC7B3935B17E7E03D0E424A8B6325CFAC44`. `fingerprint-v3.json` chứa269 SHA256 source/asset/prefab/scene/packages/harness. Kiểm sau matrix không đổi269file. Sau fix presentation, chỉ `Unit.cs` khác fingerprint, đúng hash trong `post-fix-fingerprint.json`; đối chiếu19/11 vẫn khớp. Matrix là **trước fix presentation**, self-check sau fix lưu riêng; không giả định đã chạy lại matrix trên source sau sửa.

## 3. Luồng và thuật toán đo

Chi tiết policy/role/rally/stall-recovery, tick order, resource argument order và restore nằm trong [MeasurementContract](G01-MeasurementContract.md). C là bỏ mặc có AI mặc định; E kinh tế trước; M quân sự trước; X dành tiền đặt kho landmark. Mỗi Entity quân chỉ nhận một move CodeTriggered, không spam move5s. Role worker giữ theo đời Entity; khi không tiến triển15s, policy ghi snapshot và thử node khác. Có biết tọa độ ngoài fog, nên không coi đây là hành vi khám phá của người chơi.

Ledger kiểm từng W/G/M: `initial + deposit - researchSpend - trainSpend - buildSpend = final`. Mỗi khoản chi được đo delta ví quanh command; deposit là delta ví trong tick không chi. Gather là delta cargo dương cùng loại, không phải tổng event gather tuyệt đối. Snapshot cargo/node/depot/path/state mỗi30s phục vụ chẩn đoán; không tuyên bố ledger chứng minh mọi cargo bị mất/giữ cuối trận đã được accounting độc lập.

AttackSequence đo khởi đầu attack native; không bằng số hit hoặc damage. Hall HP đo ròng. Arrival dùng vòng6unit quanh hall; hallAttackers yêu cầu State=Attacking và Target=Hall; retreatBeforeLine nghĩa retreat trước vòng6, vẫn có thể đã giao chiến công trình khác. Damage attribution theo nguồn **chưa đo**.

## 4. Matrix12run schemaG01-v3

Tất cả run Victory đầu ngày6, ledgerPASS, HP resolvePASS, spawn=eligible76. Cùng map/wave/config; một mẫu mỗi policy/seed, không phải ước lượng tỷ lệ thắng người chơi.

| Seed | Policy | Hall HP cuối | Attack sequence quân | Deposit W/G/M | Quân cuối | Stall được policy xử lý |
|---:|---|---:|---:|---|---:|---:|
| 0 | C | 8534 | 0 | 0/0/0 | 1 | 0 |
| 0 | E | 9936 | 395 | 900/250/222 | 9 | 0 |
| 0 | M | 9936 | 379 | 320/240/309 | 10 | 0 |
| 0 | X | 10000 | 165 | 480/240/165 | 9 | 1 |
| 1 | C | 8190 | 0 | 0/0/0 | 1 | 0 |
| 1 | E | 9890 | 211 | 1230/260/176 | 8 | 2 |
| 1 | M | 9898 | 240 | 680/200/300 | 13 | 0 |
| 1 | X | 9780 | 122 | 680/200/225 | 9 | 0 |
| 1543 | C | 8274 | 14 | 0/0/0 | 0 | 0 |
| 1543 | E | 9212 | 59 | 1260/290/222 | 9 | 2 |
| 1543 | M | 9140 | 56 | 660/260/250 | 11 | 0 |
| 1543 | X | 9802 | 42 | 640/240/225 | 11 | 0 |

C không ra lệnh nhưng archer vẫn có thể tự đánh; seed1543 có14attack và chết trước kết thúc. Không được gọi C là không có phòng thủ. Peak của C ghi đúng3worker/1quân, thay cho default0 trong raw S8.

E/M có engagement thực trên cả3seed; Hall được bảo vệ hơn C theo gói strategy: seed0 +1402HP, seed1 E+1700/M+1708HP, seed1543 E+938/M+866HP. Không quy riêng chênh lệch này cho multiplier tech vì số quân, công nhân, thứ tự chi và vị trí khác nhau. M seed1 nộp300thịt, sửa phương pháp đã loại case0thịt của S8; không giảm meatcost để đạt kết quả.

Army atHallLine quan sát thời gian từ rally: M0 3.70–4.75s, M1 4.60–7.10s, M1543 2.15–6.10s; đây là các Entity có ghi nhận vào vòng4, không khẳng định mọi quân đều tới đó. First army attack E/M: seed0 96.35s, seed1 102.75/101.25s, seed1543 228.35/228.20s. Seed1543 giao chiến muộn hơn rõ dù có rally; không kết luận thiếu command hoặc bug detection chỉ từ một timestamp. Snapshot quân không có SuppressAcquire=1.

### Wave quota và tiếp xúc

| Đêm | Quota | Slot hợp lệ giả sử spawn thành công | Spawn thực mỗi run |
|---:|---:|---:|---:|
| 1 | 12 | 12 | 12 |
| 2 | 15 | 15 | 15 |
| 3 | 21 | 15 | 15 |
| 4 | 24 | 15 | 15 |
| 5 | 42 | 19 | 19 |
| Tổng | 114 | 76 | 76 |

Delay10s, interval2/1.98/1.96/1.94/1.92s và bình minh kết thúc tiếp viện giải thích vì sao quota114 không biến thành114enemy. Không xác nhận lỗi spawn:76thực khớp76slot ở tất cả run. Tăng quota ngoài cửa sổ45s có thể không tăng áp lực.

| Seed | Policy | Vào vòng6 quanh hall | Hall attackers | Retreat trước vòng6 |
|---:|---|---:|---:|---:|
| 0 | C | 42 | 33 | 36 |
| 0 | E | 12 | 4 | 64 |
| 0 | M | 10 | 3 | 65 |
| 0 | X | 9 | 1 | 68 |
| 1 | C | 54 | 36 | 24 |
| 1 | E | 20 | 7 | 56 |
| 1 | M | 13 | 4 | 63 |
| 1 | X | 21 | 13 | 57 |
| 1543 | C | 38 | 32 | 40 |
| 1543 | E | 15 | 15 | 59 |
| 1543 | M | 17 | 15 | 57 |
| 1543 | X | 14 | 12 | 60 |

Các counter cumulative và có thể chồng lấp; đối chiếu summary/raw là nguồn chính. Kho X được **đặt lệnh** ở30.05s trên3seed, chi100W/100G thật. Chưa có counter completion/deposit theo depot để chứng minh lợi ích logistics. `CalculateTownHallRange` tính cả công trình ngoài căn cứ, nên kho có thể làm điểm spawn xa và giảm arrival; HP10000 ở X0 không là proof kinh tế vượt trội. Điều tra exploit/đối chứng công trình bỏ trống thuộc G3 trước khi nghiệm thu mở rộng.

### Worker và pilot

V3 ghi5stall: X0 một, E1 hai, E1543 hai; noNode=0. Node được tìm thấy nhưng worker không tiến triển, không đồng nghĩa map thiếu tài nguyên. Policy đổi node, vẫn có deposit. Không sửa pathfinding từ giả thuyết; cần case tái lập cô lập nếu muốn chốt lỗi runtime. Diagnostic fallback cây/mỏ targetHP0 là unavailable theo hợp đồng, không phải HP đã đo.

Hai pilotv1 tái lập C1 hall8190 và thử rallyM1 hall9126/124attack/0thịt. V2 thử thêm instrumentation và phản ứng stall: M1 có240attack/167thịt deposit; C1 giữ8190. Một E1v2 FAILED vì diagnostic đọc CombatBody trên resource chỉ có GameplayResource. V3 sửa guard, cố định role và chạy lại toàn12run. Không trộn pilot vào số liệu cuối, không xóa fail.

## 5. Kiểm thử, lỗi và trạng thái MCP

`node Tools/SummarizeG01.mjs --complete --compact` chạy lại19/11: **PASS12run**, đủ C/E/M/X trên0/1/1543, một configID; ledger từng loại, tổng chi theo category, HP resolve, wave seed, eligible/spawn và engagement chủ động đều đạt. `node --check Tools/SummarizeG01.mjs` kiểm cú pháp. Raw có runID/timestampUTC, file summary chứa mapping chính xác tới từng run.

Presentation self-check Unity18/11: trước sửa phát1warning `Parameter 'Attack' does not exist.`; sau sửa0warning, worker bool true, archer trigger accepted, attack sequence ECS unchanged. Artifact `presentation-before.json`/`presentation-after.json`. Matrix chưa chạy lại sau guard vì chỉ bỏ trigger sai trên worker; không đổi native data. Không chạy lại81EditMode/18Player smoke, không build Player trong G0–G1; các kết quả17/11 chỉ là lịch sử.

Console trước fix có61warning trigger, warning InputManager deprecated/Outline SRP Batcher, cùng Error/Exception đường truyền HubConnection/Socket và plugin báo path chứa space. Không ghi Console sạch hoặc0Exception. Raw `console-final.txt` là trước fix và giữ nguyên. Self-check sau fix chứng minh case worker, chưa phải sweep mọi cảnh báo project.

19/11 kiểm MCP: có instance `af69a69f-b6a4-4d04-9866-f02863390018`, Unity RTSgame2026 trên BJNB0; đã select đúng. Trong phiên chốt docs connector chỉ expose quản lý instance, chưa expose rql-query/script-execute: **chưa đọc lại live scene/Console ngày19/11**. Trạng thái Editor được xác nhận cuối18/11 ở `editor-final.json`: ProceduralGame sạch/seed1/ngoài Play-pause-compile. Không dùng kết nối account19/11 để thay cho scene probe. Không mở thêm Editor, restart hay kill Editor để khắc phục tool availability.

## 6. Cách chạy lại và phục hồi

Mở project bằng Editor hiện có; kết nối/chọn đúng UnityMCP instance. Khi có rql-query, kiểm project/scene/dirty/Play/compile trước khi mutation. Chạy full harness như mục Cách chạy của [hợp đồng đo](G01-MeasurementContract.md). Mỗi seed/policy một scene mới và Ready; không replay vào trận đã chi tiền. Không overwrite raw có sẵn; tên runID mới ghi riêng, cần tách thư mục/đợt nếu chạy lại toàn matrix để `--complete` không lẫn24run.

Kiểm raw offline từ project root:

```powershell
node --check Tools/SummarizeG01.mjs
node Tools/SummarizeG01.mjs --complete --compact
```

Presentation probe: paused Ready Play, đọc `Tools/ValidateG01Presentation.cs`, class `G01PresentationValidation`, method `Run`. Probe cần worker/archer bind, không dùng để chạy balance. Nó kiểm bool/trigger và native unchanged; sau đó reload fresh scene/thoát Play, seed1. Snapshot giả lập không đại diện một trận đang tiếp tục chơi.

Khôi phục chỉ những trường thử trong bộ nhớ: Seed=1, thoát Play/pause; không SaveAssets/SaveScene trong lượt đo. Không reset prefab/scene/git. Không rollback `Unit.cs` toàn file vì workspace có thay đổi trước đợt; nếu cần bỏ fix, chỉ phục hồi điều kiện trigger thêm Worker. Bản trước fix lưu làm evidence, không dùng ghi đè nguyên file hiện tại.

## 7. Kết luận, hiệu năng và bước tiếp

G0 đạt baseline/config/ledger và recipe phục hồi; G1 đạt quân chủ động thực sự tham chiến trên3seed, policy không tự suppress acquisition, worker/resource được chẩn đoán thay vì chỉnh giá che lỗi. **Chưa đạt mục tiêu Normal không tự thắng**: control thắng3/3. Đây là đầu vào G2, không là G0/G1 thất bại hoặc lý do hạ tiêu chí.

Không đo mới FPS/GC/CPU/GPU/startup; reflection/LINQ/snapshot của harness không thuộc workload hiệu năng. Không có claim cải thiện performance hay tỷ lệ thắng người chơi. Map+wave seed tái lập nhưng AI cừu phụ thuộc Entity.Index, không bảo đảm bitwise replay toàn trận; thêm lặp/đối chứng khi chọn candidate sát ngưỡng.

Đề xuất bước G2: chọn **một nhóm HP hall hoặc wave thực sự tới tuyến**, tối đa2candidate, pilot seed1×C/E/M; giữ tech/clock/layout cố định, loại candidate làm mọi policy thua. Chưa author hoặc thử candidate trong G0–G1. Candidate đạt cần ít nhất một đối chiếu realtime; G3 còn kiểm logistics/geometry công trình, G4–G7 profile/Release/manual/demo. Không tự mở rộng phạm vi từ việc có báo cáo.

## 8. Nguồn và artifact

- [MeasurementContract](G01-MeasurementContract.md), [NextPhasePlan](NextPhasePlan.md), [MatchFlow](../MatchFlow/Architecture.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md).
- [Summary12run](Evidence/G01-2026-11-18/summary.json); thư mục cùng cấp giữ17raw (12final +4pilot +1FAILED), fingerprints, snapshots, before/after presentation và trạng thái Editor.
- Source: `GameplayComponents.cs`, `GameplayActorJobs.cs`, `GameplayEconomyJob.cs`, `GameplaySimulationSystem.cs`, `EnemyWaveManager.cs`, `DayNightCycleManager.cs`, `ProductionBuilding.cs`, `Unit.cs`, `WorkerUnit.cs`, `GameplayGameBridge.cs`.
- Thuật toán/game API tái sử dụng source đã khóa trong project; không thêm dependency hoặc suy diễn từ phiên bản API ngoài project. Các nguồn chính thức về ECS/path/spatial đã dẫn trong Architecture liên quan; đợt này không bổ sung thuật toán bên ngoài.
