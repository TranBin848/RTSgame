# E10 — Windows Release, hiệu năng và bàn giao

Trạng thái 24/11/2026: **build, regression, Release smoke và gate p95 đã PASS; nghiệm thu cuối còn PARTIAL**. Candidate gameplay là E9 `candidate76-v1`; E10 giữ TownHall 1200 HP, wave seed 8001, sáu công nghệ, clock 60/30/45 và budgets presentation hiện có. Không nâng Unity, backend hoặc pipeline. [Build plan](BuildPlan.md) · [E9/balance](E9-TacticalNights.md) · [G5 lịch sử](../Progression/G5-ReleaseOnscreen.md).

## Mục tiêu và phạm vi

Tạo artifact Windows x64 Release mới từ code E0–E9, kiểm hồi quy/lifecycle UI và đo lại G5 onscreen. Gate hiệu năng là **p95 ≤1000/60 ms ở từng lượt chuẩn**, không lấy trung bình các lượt để che lượt chậm. G3 manual vẫn mở theo yêu cầu người dùng; E9 holdout Economy thua trên 137/901 vẫn là giới hạn cân bằng.

## Dữ liệu, thuật toán và tích hợp

`Tools/BuildE10.cs` kiểm ngoài Play/compile và mọi scene sạch; dùng scenes enabled với MainMenu đầu tiên, `BuildOptions.None`, StandaloneWindows64. BuildReport ghi lên đĩa trước khi phản hồi MCP. `Tools/FingerprintE10.mjs` tạo SHA-256 từng source/artifact và hash manifest đã sắp thứ tự; không dùng HEAD thay cho source có thay đổi chưa commit.

`E10ReleaseValidation` chỉ được cài qua cờ `--e10-smoke`. Harness đi qua MainMenu → ProceduralGame, root Ready, fog/feedback/lighting, tree, pause/resume, native last-Hall defeat, result/replay, reset ledger/research/report, năm dawn được inject, victory và result/menu → game mới. Result buttons được invoke qua listener thật; đây là integration API, không thay nghiệm thu click/chuột vật lý. Fixture terminal không là một trận chiến tự nhiên.

Trong phép kiểm pause, chờ qua frame chuyển trạng thái rồi so clock, wallet và vị trí/HP ECS. `SwarmSimulationSystem.TickCount` vẫn có thể tăng ở delta bằng 0, vì vậy không dùng số lần cập nhật system làm số giây gameplay đã chạy. Lượt thử harness ban đầu dùng điều kiện tick không tăng đã bị loại; không kết luận gameplay tự chạy từ điều kiện đó.

`SwarmSmokeAutomation` giữ workload G5 cũ, sửa nhãn onscreen cho đúng thực tế và ghi thêm peak actor/VFX. Cờ `--e10-night-stress` gọi coroutine fixture: giữ pha Night, ngừng wave tự nhiên, clone bảy Hall quanh Hall thật, nâng HP riêng fixture để duy trì tám đèn, bổ sung native enemy lên 96 mỗi 0,2 giây. Gửi 40 Gather presentation event mỗi 0,2 giây vào consumer để chạm trần 32 pulse; không grant/debit tài nguyên. Fog vẫn bật, camera cố định, UI vẫn render. Đây là **tải tổng hợp**, không có giá trị đánh giá đường xây nhà, cân bằng, hoặc số quái tối đa của mọi trận.

Reflection/discovery chỉ ở harness opt-in, không là API mới cho gameplay consumer. Fixture không lưu scene/asset hoặc thay ECS job layout. Có watchdog, log lỗi, assert trong smoke và summarizer. Cấu hình chạy thường không có các cờ này và không cài harness.

**Mục tiêu quái của fixture và tải đã đo khác nhau:** vòng lặp cố bổ sung tới 96 nhưng `SpawnEnemy` có thể trả `Entity.Null` khi ô không fit. CSV trong toàn cửa sổ đo ghi **94 quái sống**, peak projectiles 0; kết quả không chứng minh tải 96 sống hoặc stress projectile. Chưa ghi từng lần từ chối nên không khẳng định nguyên nhân chính xác của hai quái thiếu. Assert ban đầu đòi peak96 đã fail; sửa sanity check theo giới hạn thực của API, xuất `enemyTarget=96`, `enemyTargetReached=false`, `minEnemies=peakEnemies=94`. Giữ nguyên toàn bộ dữ liệu đo và gate 8 đèn/32 pulse/p95/focus. Nhãn `scenario` của binary ghi mục tiêu fixture, không thay cho cột live enemies trong CSV.

## Cách build, chạy và kiểm chứng

1. Chạy EditMode bằng Unity MCP `tests-run`; chỉ build khi compile sạch và scene đã lưu.
2. Gọi `E10Build.Run` qua `script-execute` full-code từ `Tools/BuildE10.cs`, timeout 290. Nếu CLI báo Editor busy giữa BuildPipeline, chờ báo cáo trên đĩa; không cancel build hoặc dùng artifact đang ghi.
3. Mở `.utmp/e10-release-2026-11-24/RTSgame2026.exe` không tham số để chơi từ MainMenu; giữ toàn bộ thư mục Data/MonoBleedingEdge cạnh exe.
4. Phép đo từ root dự án:

```powershell
.\Tools\RunE10.ps1 -BuildDirectory '.utmp/e10-release-2026-11-24' -OutputDirectory '.utmp/e10-release-onscreen'
node Tools/SummarizeE10.mjs .utmp/e10-release-onscreen .utmp/e10-release-onscreen/summary.json
node Tools/FingerprintE10.mjs .utmp/e10-release-2026-11-24 Docs/Modules/GameExperience/Evidence/E10/fingerprints.json
```

Dùng output directory mới để giữ bằng chứng cũ. Player phải foreground liên tục, borderless 1920×1080, HUD/fog thật, camera không có target RenderTexture. Recorder kiểm focus/kích thước/camera trên từng frame capture; không tính lượt mất focus là kết quả hợp lệ. Người dùng đã xác nhận giữ Player foreground trong đợt này.

G5: warmup 30 giây, capture 90 giây, seed 1 ba lượt và military seed 1543 một lượt; thêm first/warm startup và SampleScene smoke. Night stress dùng cùng warmup/capture/settings, đứng riêng. CSV là dữ liệu thô, percentile nearest-rank từ `SummarizeG5/E10`, báo p95/p99/max và số frame vượt budget. GC Release không khả dụng phải ghi null/unavailable, không ghi 0.

## Kết quả thực đo

- Unity MCP kết nối đúng RTSgame2026, scene ProceduralGame sạch, ngoài Play/pause/compile trước đợt.
- EditMode **89/89 PASS**, 0 fail/skip, 10,6428327 giây: [raw result](Evidence/E10/editmode-result.json).
- Windows x64 Release **Succeeded**, Mono, `BuildOptions.None`, **0 lỗi/0 cảnh báo**, 235,732 giây, BuildReport totalSize 137.883.211 B: [build.json](Evidence/E10/build.json). CLI đã báo modal busy giữa compile; BuildReport trên đĩa và các process Player sau đó xác nhận thành công, không cancel/rebuild để bỏ lượt.
- **18/18 SampleScene smoke PASS**, **15/15 procedural lifecycle PASS**, không lỗi, `development=false`. Preflight trên cùng artifact cũng đạt15/15. [Raw smoke](Evidence/E10/Release/g5/gameplay-smoke/gameplay-result.json), [raw lifecycle](Evidence/E10/Release/lifecycle/lifecycle-result.json).
- Summarizer assert cấu hình, seed, đủ frame/90 giây, focus toàn capture, HUD/fog, ảnh1080p, gameplay smoke, lifecycle và budgets: [summary.json](Evidence/E10/Release/summary.json). Raw CSV/JSON/log/PNG nằm cùng thư mục từng lượt; không chỉ giữ số tổng hợp.

Máy baseline: Windows11 10.0.26300, Core i5-11300H, GTX1650, RAM16.179MB, D3D11, Ultra, fullscreen borderless1920×1080,144,003Hz, vSync1, cap−1. Tất cả5 capture dùng warmup30s/capture90s, foreground hợp lệ ở mọi frame, camera render onscreen, HUD/fog bật; không hạ budgets hoặc quality. GC Release unavailable (`-1` thô, `null` trong G5 summary); chưa có số GC Development mới.

| Lượt | Frame | Median ms | P95 ms | P99 ms | Max ms | Vượt16,67ms |
|---|---:|---:|---:|---:|---:|---:|
| Seed1 lượt1 | 12.154 | 6,9445 | 13,8884 | 13,8965 | 27,7858 | 46 (0,378%) |
| Seed1 lượt2 | 12.169 | 6,9445 | 13,8882 | 13,8967 | 27,7798 | 65 (0,534%) |
| Seed1 lượt3 | 12.148 | 6,9445 | 13,8884 | 13,8969 | 34,7176 | 65 (0,535%) |
| Seed1543 military | 12.175 | 6,9446 | 13,8881 | 13,8963 | 27,7911 | 41 (0,337%) |
| Night stress tổng hợp | 11.186 | 6,9445 | 13,8952 | 20,8337 | 27,7907 | 189 (1,690%) |

**4/4 lượt chuẩn G5 đạt p95**, thêm1/1 lượt stress8đèn/32pulse đạt. Đây là bằng chứng đóng phần budget còn mở của G5 **cho candidate E10, máy và batch này**; không sửa kết quả3/4 của artifact21/11. Vẫn có frame vượt budget, max toàn batch34,718ms; không tuyên bố mọi frame đạt60FPS hoặc đã hết spike. Chưa profile Development mới vì không có lượt p95 thất bại cần chẩn đoán trong batch này; không suy ra cải thiện GC hoặc nguyên nhân tốt hơn từ số FPS.

| Lượt | Peak actor gameplay | Peak native enemy | Peak projectile | Peak local lights | Peak pulse |
|---|---:|---:|---:|---:|---:|
| Seed1 mỗi lượt | 26 | 9 | 0 | 1 | 3 |
| Seed1543 military | 28 | 8 | 0 | 1 | 2 |
| Stress | 32 | 94 | 0 | 8 | 32 |

Military chấp nhận2/3 lệnh train theo ví thực. VFX dùng sprite pool; fixture không tạo `ParticleSystem`, số component cuối lượt là0. Actor/pulse peak lấy trong capture; local light peak lấy toàn lượt. Enemies/projectiles lấy max CSV, actor gameplay không cộng lẫn quái native. Stress có6.399 hit và0 projectile; chưa coi đây là stress ranged/projectile. Ảnh [night-stress](Evidence/E10/Release/night-stress/screenshot.png) là fixture tám Hall chồng vùng nhìn, không phải bố trí công trình có thể xây như vậy trong trận hợp lệ.

Năm mẫu startup scene request→Ready:933,389 /763,772 /771,890 /750,512 /805,687ms; first933,389; median771,890; min750,512/max933,389. Process mới cho mỗi mẫu nhưng không kiểm soát cache hệ điều hành; không phải cold-start tuyệt đối. Không dùng số này làm startup của mọi máy.

Ảnh result [Victory fixture](Evidence/E10/Release/lifecycle/victory.png) và [Defeat fixture](Evidence/E10/Release/lifecycle/defeat.png) đã được chụp từ Player, không hậu kỳ; victory được inject năm dawn, không phải trận combat thắng tự nhiên.

## Một trận Player bình thường và thao tác UI

Chạy chính exe đã đo, **không cờ smoke/stress**, từ MainMenu. Computer-use gửi click Chơi, T mở cây, Escape đóng cây/mở pause, click Tiếp tục; sau đó control không phát lệnh kinh tế/di chuyển/nghiên cứu/huấn luyện. Clock60/30/45 tự chạy, không đổi HP, phase hoặc cấp tiền. Qua hai đêm, bảng đêm2 mở/đóng được và hiển thị590 damage trong chu kỳ. Trận kết thúc tự nhiên **Defeat ngày3**, tổng damage Hall1200, mất dân/quân0, deposit/spend0, research0, explored316, granted100/100/100. Đây là đối chứng bỏ mặc trên Player; không là bằng chứng thắng5đêm hoặc tỷ lệ thua của người chơi.

Click **Play again** ở result đã thấy ngày1/ví100/100/100 mới; Escape → Về Menu trở về MainMenu. Player được để mở ở MainMenu cho người dùng xem. Input UI qua computer-use, không invoke API trong lượt này. [Biên bản quan sát](Evidence/E10/normal-ui-checks.json), [Player log](Evidence/E10/normal-player.log) không có Error/Exception/Assert; ảnh UI được tool hiển thị trong chat, không có PNG local riêng cho lượt thường. Lượt control không có CSV/frame timing và không được ghép vào phép đo G5. Ngày thua khác harness Editor là lý do không coi manual-tick và PlayerLoop là cùng phép thử.

## Candidate, gói chạy và khôi phục môi trường

- Build folder: `.utmp/e10-release-2026-11-24/`; ZIP: `.utmp/RTSgame2026-E10-Windows-x64-2026-11-24.zip`,46.784.663B,264file. Giải nén cả thư mục, mở exe; không tách riêng exe khỏi Data/DLL/MonoBleedingEdge.
- SHA-256 ZIP: `b6eebf80a7cce354e08119e3c99a7216bb038ebb657a5fbaca82d214c0cea846`; [package metadata](Evidence/E10/package.json).
- Source manifest SHA-256: `38282c7ee821b2f2eabef0c1ee4cb4e3a01c7dbe9ce2697c1bc9060fb52d6dcd`; [từng file source/artifact](Evidence/E10/fingerprints.json). Bao gồm source chưa commit, không dùng git HEAD đại diện sai bản build.
- Unity source/settings manifest: `752cb95b8eb19ef512c9a4cfb9442978bb330cacaf7dd6de718aa79cde10f956`; measured binary manifest bỏ README: `cdcecfe50764223735606e81e66c6426a8bb1c918900d06d74e67c5273c0b5be`. [Đối chiếu checkpoint](Evidence/E10/unchanged-measured-binary.json): mọi Assets/ProjectSettings/Packages và binary đã đo giữ nguyên; sau checkpoint chỉ sửa hai tool runner/summarizer và README bàn giao. Không rebuild âm thầm sau phép đo.
- Editor cuối: ProceduralGame sạch, seed1, ngoài Play/pause/compile, root LastError=null: [state](Evidence/E10/final-editor-state.txt), [RQL](Evidence/E10/final-rql-state.txt). Không lưu fixture vào scene hoặc tự commit/stage thay đổi của người dùng.
- [Validation notes](Evidence/E10/validation-notes.txt) giữ việc sửa điều kiện kiểm pause, assert96 và phản hồi CLI busy. RawRelease/Preflight, metadata/hash, ảnh/log/CSV/JSON đi cùng report; build/cache nặng vẫn ở `.utmp`.

## Giới hạn và hạng mục còn mở

Manual G3 vẫn mở đúng yêu cầu người dùng; chưa có xác nhận mở rộng và thắng5đêm bằng người chơi. E9 holdout137/901 Economy thua, không tune ngược holdout trong E10; manual thẩm mỹ/audio toàn đợt và máy thứ hai chưa kiểm. Giới hạn clearance của Archer ở vị trí đầu trận được ghi trong [E8](E8-ExpansionCommands.md) chưa được sửa trong E10. Trận tự nhiên Editor675giây/5đêm của E9 có bằng chứng riêng trong [E9](E9-TacticalNights.md); không đổi nhãn thành Player E10. Chưa có lượt thắng5đêm tự nhiên trên Player cuối; lượt Player thường ở E10 là control thua ngày3, victory Player là fixture inject. Không suy kết quả máy baseline sang máy khác, không suy tải tổng hợp thành worst case tuyệt đối hoặc claim từng thay đổi E4/E6 giảm/tăng bao nhiêu% khi chưa có A/B tương ứng. Chưa cập nhật slide theo kết quả chưa nghiệm thu.

## Nguồn đối chiếu

[Unity BuildPipeline.BuildPlayer](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html), [Player command-line arguments](https://docs.unity3d.com/6000.0/Documentation/Manual/PlayerCommandLineArguments.html); hợp đồng repo: [BuildPlan §8](BuildPlan.md), [G5](../Progression/G5-ReleaseOnscreen.md), [DOTS Development Guide](../DOTS/DevelopmentGuide.md), source harness/build/summarizer đi kèm candidate.
