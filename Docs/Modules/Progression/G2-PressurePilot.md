# G2 — pilot áp lực sinh tồn

Ngày **19/11/2026 — G2 DONE ở phạm vi pilot và chọn shortlist**. Đã chạy 9 lượt native và một trận realtime; lưu Hall **1600 HP** vào riêng `ProceduralGame`. Đây là cấu hình để kiểm tiếp G3, chưa phải nghiệm thu cân bằng mọi seed. Tiếp sau [G01](G01-Validation.md), theo [NextPhasePlan/G2](NextPhasePlan.md).

## Giả thuyết và phạm vi chốt trước khi thử

G1 đã có quân tham chiến nhưng C thắng 3/3; seed 1 C mất 1810 HP, E mất 110 HP, M mất 102 HP trên Hall 10000. Chọn nhóm **HP Hall** để kiểm liệu ngân sách chịu đòn thấp hơn có tạo giá trị cho phòng thủ, không tăng wave hoặc chỉnh giá tech trong cùng vòng. Đăng ký trước **1600 HP và 1200 HP**, baseline 10000 trên cùng source/harness mới; không đổi prefab gốc 100. Mức 1600 gần nhưng dưới thiệt hại C1 đã đo, 1200 thấp hơn để kiểm biên an toàn. Không giả định kết quả tuyến tính vì Hall chết có thể đổi outcome/targets. Các giá trị là giả thuyết trước thử; việc giữ 1600 được quyết định sau các gate bên dưới.

Seed1, wave seed8001, C/E/M, tối đa2candidate (6run), thêm3baseline sau worker presentation guard để tránh trộn code. E/M/rally/role/stall-recovery giữ nguyên G01. Giữ clock60/30/45, goal5đêm, quota/interval/retreat, giá/duration/multiplier, layout và wallet100W/G/M. Không grant tiền/spawn bù/teleport.

PASS shortlist: C Defeat, ít nhất E hoặc M Victory, không ép phòng thủ cơ bản chết đêm1. Candidate khiến mọi policy thua loại. Nếu cả hai thất bại, phục hồi baseline và quay giả thuyết G1, không thêm candidate3. Chọn candidate ít thay đổi nhất (HP cao hơn) nếu cả hai đạt; ghi số đo đủ, không lấy kết quả mong muốn thay bằng chứng. Shortlist tăng tốc phải đối chiếu ít nhất một trận realtime trước khi ghi G2DONE.

## Implementation và dữ liệu

`Tools/PrepareG2.mjs` derive harness từ G01 bằng replacement có assert, lưu full source đo và269hash/configID theo HP trong Evidence/G2-2026-11-19. Chỉ thay metadata, guard HP, vị trí/Entity Hall cache để telemetry xử lý view unbind lúc chết; không viết gameplay production. RunIDUTC cóhp/policy/seed, schemaG2-v1. Native tick/ledger/attack/arrival giữ nguyên giới hạn [G01 contract](G01-MeasurementContract.md). Quân vẫn dùng command/factory thật. Không dùng harness để đo performance.

Raw native giữ nhãn text `normalHP` kế thừa G01; trong G2 **không được đọc nhãn này thành Hall10000**. HP thực lấy từ `hallMax` trên header và `resolved` authoring/view/Entity; candidate1600/1200 được áp trước bind chứ không chỉnh HP giữa trận. Giữ raw/fingerprint bất biến thay vì sửa nhãn trong dữ liệu đã chạy.

Lượt CLI ban đầu (nguồn lưu `Evidence/G2-2026-11-19/RunG2.ps1.txt`) gọi Unity MCP CLI với **--no-start**, mỗi run vào Play mới. Set m_Health trên instance scene **trong bộ nhớ trước Awake** để MaxHealth/CurrentHealth/Entity resolve cùng HP; không sửa riêng CombatBody. Không SaveScene/SaveAssets. Snapshot pending-restore được ghi trước mutation; finally thoát Play và khôi phục instance10000/seed1/ngoài pause. Không mở Editor thứ hai hoặc thay SampleScene/prefab.

Do domainreload cloud có503/UNREACHABLE và progressdialog Initializing callbacks, batch đầu hoàn tất C/Ebaseline rồi dừng trước khi chạy M. Giữ transcript, xác nhận clean Edit và restore10000, không gọi lỗi kết nối là gameplay fail. Lượt warmCLI (nguồn lưu `Evidence/G2-2026-11-19/RunG2Warm.ps1.txt`) resume các case chưa có raw bằng reload scene trong một phiên pausedPlay (giống G01). One-shot sceneLoaded callback kiểm root chưaReady/Unit chưaDotsControlled rồi set cả m_Health và m_CurrentHealth **sau Awake nhưng trước Start/BindScene**; Entity sau đó được tạo tự nhiên từ CurrentHealth. Guard HPresolve trong harness xác nhận cả ba nguồn đồng nhất. Không set HP trên Entity đã chạy, không sửa scene trên disk. Callback tự unsubscribe ngay khi scene load; finally thoátPlay, kiểm sceneEdit vẫn10000/seed1/sạch. Hai baselineC/E dùng freshPlay, các case resume dùng freshScene/resetWorld; EntityIndex/cừu không bảo đảm bitwise giữa hai lifecycle như giới hạnG01, nên không suy chênh vàiHP là tác dụng candidate.

Chạy chuẩn bị từ project root: `node Tools/PrepareG2.mjs` và `node Tools/PrepareG2Realtime.mjs`. Đây là recipe của đợt đã đóng băng: fingerprint và derived source phải khớp, không overwrite evidence nếu source/scene đã đổi. Lượt thực thi cuối dùng runnerEditor bên dưới; các wrapperCLI được lưu dạng `.ps1.txt` cùng evidence để đọc lịch sử, không giữ nhiều runner trùng trách nhiệm trong Tools. Chỉ resume case chưa có raw, không chạy lại mutation khi request có thể đã thực hiện. Nếu dừng/crash, đọc pending-restore/status/raw, kiểm Play/dirty trước; thoát Play rồi dùng Unity API khôi phục đúng trường m_Health10000 và Seed1 của baseline thí nghiệm, không reset scene/git hoặc overwrite asset từ snapshot. Khi game mặc định đã chuyển sang shortlist, ghi lại giá trị hiện hành trước tái lập baseline và khôi phục đúng giá trị đó sau đo; không SaveScene trong bước tái lập.

MCP19/11: CLI1.12.0 đã probe đúng E:/Unity project/RTSgame2026,6000.3.11f1,ProceduralGame sạch/ngoàiPlay-pause-compile/HP10000. Connector cloud quản lý instance vẫn nối đúng RTSgame2026; dùng CLI cho scene/script đang thiếu trong connector. Không đổi plugin/package.

### Fallback khi cloud không giữ được request

Sau3baseline, cloud liên tụcUNREACHABLE cả lệnh đọc; C1600 request không có raw nên không coi đã hoàn tất hoặc tự retry mutation. Chuẩn bị runnerEditor tạm từ `Tools/G2EditorRunner.cs` và hai harness frozen, chỉ đổi tên class thành G2EditorPlaytest/G2EditorRealtime để tránh trùng Roslyn. Các file tạm ở `Assets/Game/DOTS/Editor/G2Editor*.cs`, không thuộc runtime/Player. Request opt-in `.utmp/G2-editor-request.json` modepilot, SessionState giữ phase qua domainreload. Runner kiểm trạng thái, dừng phiên bị ngắt rồi vào pausedPlay, skip case đã có raw, reload/prebindHP, gọi đúng harness, ghi status; không grant tiền hoặc rút timer.

Sau9native, runner chỉ chọn HP cao nhất trong hai candidate nếu CDefeat và E/MVictory, rồi đối chiếu realtime. Sau realtime tự thoátPlay và phục hồiEditbaseline10000/seed1, lưu restore-local.json; **không tự author candidate** trước review. Modeauthor là bước riêng sau evidence đạt, chỉ SerializedObject m_Health/PrefabInstance override trên ProceduralGame và SaveScene qua UnityAPI. Khi hoàn tất phải gỡ3scriptEditor tạm và meta chúng tạo; giữ bản nguồn/evidence trong Tools/Docs. Không gỡ plugin, killEditor hoặc mởEditorthứhai.

ComputerUse kiểm/kích hoạtUnity bị automatic approval review từ chối (`Computer Use was not approved to use unity`); không thực hiện tiếp inputUI và không dùng lệnh hệ thống để điều khiển cửa sổ thay thế. Runner được Unity AssetDatabase compile bình thường, trạng thái/compile/trace đọc qua file và MCP khi khả dụng. Không coi việc cloud fail là lỗi gameplay, không claimConsole sạch: log còn warningplugin/pathspace và lỗiDNSUnityServices ngoài phạm vi.

## Kiểm thử, kết quả và giới hạn

Tại thời điểm đăng ký chưa có kết quả candidate. Mỗi run phải assert HP resolve, ledgerW/G/M, terminal outcome và eligible/spawn; giữ failraw, riêng Defeat có thể dừng trước76spawn nên không ép spawn tổng76 cho Defeat. Actor/arrival counter chỉ là telemetry, không damage attribution. Native cập nhật tăng tốc không thay thế Unity realtime/Animator/UI callbacks.

### Kết quả9run native

[Native summary và mappingrunID](Evidence/G2-2026-11-19/native-summary.json). Tất cả9run đạt HPresolve, ledgerW/G/M, config/seed, terminal và engagement theo vai trò. Không có grant tiền; C không chi hoặc gửi rally. Victory đầu ngày6 tại675.75s, đủ76spawn; Defeat dừng trước bình minh nên tổngspawn thấp hơn hợp lệ.

| Hall ban đầu | Policy | Outcome | Thời điểm(s) | Ngày | Hall cuối | Attack sequence quân | Spawn |
|---:|---|---|---:|---:|---:|---:|---:|
| 10000 | C | Victory | 675.75 | 6 | 8190 | 0 | 76 |
| 10000 | E | Victory | 675.75 | 6 | 9928 | 213 | 76 |
| 10000 | M | Victory | 675.75 | 6 | 9898 | 240 | 76 |
| 1600 | C | Defeat | 539.95 | 4 | 0 | 0 | 57 |
| 1600 | E | Victory | 675.75 | 6 | 1348 | 223 | 76 |
| 1600 | M | Victory | 675.75 | 6 | 1522 | 346 | 76 |
| 1200 | C | Defeat | 516.90 | 4 | 0 | 0 | 49 |
| 1200 | E | Victory | 675.75 | 6 | 964 | 212 | 76 |
| 1200 | M | Victory | 675.75 | 6 | 1130 | 240 | 76 |

Cả1600 và1200 đạt gate native: C thuađêm4, E/M sống quađêm1 và thắngđủ5đêm. Chọn **1600 vào shortlist realtime** vì HPcao hơn và ít giảm ngân sách chịuđòn hơn theo quy tắc đãđăngký;1200 không bị gọi là bug/fail, chỉ không được ưu tiên author trong vòngnày. Không thêm candidate3, không đổi nhómwave/giá.

Rủi ro cần kiểm sớm ởG3: G01Cseed0 chỉ mất1466HP ở baseline10000, nhỏhơn1600. Vì vậy chưa có căn cứ rằng1600 làm Cthua ở mọiseed; seed0 có thể còn tựthắng. Không thay tiêu chí Normal hoặc tự đổi shortlist sang1200 chỉ dựa phép trừHP; G3matrix phải đo lại, có thể loại1600 và dùng candidate1200 đãđăngký nếu gate đủ. Kết quảseed1 và một trận realtime không thay thế matrix3seed/wave bổsung/holdout.

BaselineE9928 khácG01E9890, candidateE1600 chịu252damage khác72damage của baselineE; không suy điều kiệnHP làm quân đánh tốt/xấu hơn. EntityIndex/cừu/lifecycle/va chạm có thể làm trajectory khác như giới hạnG01. Bằng chứng quyếtđịnh là controlterminal và activeVictory, không lấy deltaHall nhỏ làm quan hệ nhânquả riêngcủaHP hoặc presentationfix. Chưa suy tỷlệthắng hoặc kếtquảseed0/1543 của candidate từ9runseed1.

Không chạy fullregression sau mỗi chỉnh số; khi chọn shortlist, kiểm tích hợp phù hợp và realtime trên config chọn. Chưa benchmark Release/onscreen, chưa manual feedback hoặc robustness/holdout; G3–G7 còn theo plan. Module lớn/thuật toán mới không được thêm trong pilot.

### Đối chiếu realtime đã hoàn tất

`Tools/PrepareG2Realtime.mjs` derive cùng policy E/M từ harness G2, đăng ký một EditorApplication.update callback để ra lệnh khi cần, để **Unity PlayerLoop tự chạy** clock/wave/native simulation/bridge/presentation. Không gọi World.SetTime/Gameplay.Update thủ công, không tăng timeScale. Dừng ở outcome/deadline, dispose query và bỏ callback, phục hồi runInBackground/Random state. File realtime-status.json cho phép theo dõi mà không giữ request MCP11phút hoặc khởi động automation.

Realtime dùng RNG bình thường của game: InitState8001 nhưng chung UnityRandom với TextPopup, khác stream wave được cô lập của run native. Vì vậy đây là **đối chiếu outcome trên cùng config**, không là paired A/B cùng chuỗi spawn hoặc phép đo FPS; metadata ghi rng=normal-shared-UnityRandom. Counter/ledger ghi riêng schemaG2-realtime-v1. Snapshot thời gian native vs realtime và callbacks có thể khác; không gộp các run để tính winrate.

| Điều kiện | Kết quả quan sát |
|---|---|
| Hall / map / policy | 1600 HP / seed 1 / E |
| Thời gian | 675.08 giây game, 675.37 giây wall-clock; timeScale1 |
| Outcome | Victory đầu ngày 6, đủ 5 đêm |
| Hall cuối | 1338/1600 HP |
| Quân | 236 attack sequence, 8 rally command, peak8 quân/6 worker |
| Wave | 76 spawn; arrival28, hallAttackers12, retreatBeforeLine47 |
| Deposit W/G/M | 1380/260/176 |
| Research spend W/G/M | 410/220/30 |
| Train spend W/G/M | 0/140/240 |
| Ví cuối W/G/M | 1070/0/6 |
| Ledger | PASS từng loại; không grant tài nguyên |

Run realtime: `20261119T100706242-hp1600-economy-1-realtime.txt`. [Summary chung10run](Evidence/G2-2026-11-19/summary.json) tách cờ realtime, không trộn với9run native để tính một tỷ lệ thắng. Native E1600 còn1348HP, realtime còn1338HP: cùng đạt outcome gate, không suy 10HP khác biệt là hiệu ứng của FPS/RNG riêng lẻ. Control1600 thua hiện được đo bằng native; **chưa kiểm control realtime** hoặc E/M trên seed0/1543 của candidate.

`node Tools/SummarizeG2.mjs --complete --compact` kiểm9run native và kiểm thêm realtime khi có file: wallet/chi theo category, configHP/map/wave, HPresolve, terminal, engagement. Victory yêu cầu ngày6 và native đủ76slot; Defeat được phép dừng trước76. Tool kiểm không sửa raw/config.

## Cấu hình được giữ và phạm vi diff

Sau review, author qua Unity `SerializedObject`, `PrefabUtility.RecordPrefabInstancePropertyModifications` và `EditorSceneManager.SaveScene`; không hand-edit YAML. Chỉ instance Hall trong `Assets/Scenes/ProceduralGame.unity` đổi `m_Health: 10000 → 1600`. `TownHall.prefab` vẫn100; `SampleScene` vẫn giữ override10000. Unit.Awake/bridge bind tạo CurrentHealth/CombatBody từ authoring; SelectedUnitInfoItemUI chia CurrentHealth/MaxHealth nên không cần thêm HP UI hoặc một BalanceManager.

SaveScene ban đầu làm serialize lại3RectTransform (Gold/Wood/MeatDisplay) và20float animation tile (10↔10.000001). Kiểm diff đã bắt các thay đổi ngoài phạm vi. Đọc bản scene trước lưu để lấy đúng objectID/cell/giá trị, khôi phục bằng **SerializedObject Unity API**, không viết YAML. Bản sao memory khớp bản đã lưu trước restore, không có edit khác cần ghi đè. Một probe Get/SetAnimationSpeed bị reject ngay khi compile vì API không tồn tại, không chạy mutation; nguồn probe thất bại giữ riêng. Schema `m_AnimatedTiles` đã được kiểm tra qua MCP rồi dùng serialized map `first/second.m_AnimationSpeed` trong runner đã compile.

[Author diff check](Evidence/G2-2026-11-19/author-diff-check.json) xác nhận **toàn bộ byte scene khớp bản trước, ngoại trừ đúng một giá trị HP10000→1600**. Trong269file baseline đã chụp, chỉ scene này khác hash; prefab, SampleScene, runtime source, tech, wave, phase, map settings và package manifest/lock giữ nguyên. Đây là phạm vi snapshot269file, không khẳng định toàn workspace chỉ có một diff vì dự án đã có thay đổi trước lượt này.

## Kết nối, lỗi công cụ và dọn dẹp

MCP CLI đã probe live đúng project/scene trước đo. Cloud bị ngắt ở domainreload và các request dài; không retry mutation chưa biết hiệu ứng. Khi cloud không ổn định, runner Editor chạy local và ghi checkpoint. ComputerUse bị automatic approval review từ chối Unity, không có inputUI tiếp theo; người dùng trả lời sẽ bấm Assets→Refresh. MCP sau đó đọc được Editor và serialized schema trở lại. Assembly chưa refresh từng khiến runner cũ xử lý nhầm request restore như pilot; guard baseline chặn và lỗi được lưu trong `author-refresh-pending-FAILED.txt`, không tạo thêm run balance. Runner mới dừng khi lỗi Edit Mode, dùng request deferred chỉ sau compile. Đây là lỗi điều phối cleanup, không phải kết quả thua của candidate.

Sau restore, Unity đã xóa ba `G2Editor*.cs` tạm và meta qua AssetDatabase. Nguồn frozen/runner pilot/closure và các wrapperCLI cũ giữ trong Evidence; Tools chỉ giữ generator, runner nguồn và summarizer cần đối chiếu, không để driver trong runtime/Player. `restore-local.json` chứng minh đã trả baseline10000 trước author; `editor-final.json` ghi ProceduralGame/seed1/HP1600/sạch/ngoàiPlay-pause trước cleanup compile. Cần đọc checkpoint kiểm cuối cho trạng thái sau compile; không dùng một bản ghi trước compile để tuyên bố Editor đã compile xong.

**MCP kiểm cuối sau cleanup:** [closure-state.json](Evidence/G2-2026-11-19/closure-state.json) xác nhận scene sạch, ngoài Play/pause/compile, seed1, Hall1600/prefab100, `EditorUtility.scriptCompilationFailed=false`, không còn file hoặc typeG2Editor trong Assembly-CSharp-Editor. Console vẫn giữ3CS1061 lịch sử từ bản runner restore trước khi API được sửa, cùng lỗi kết nối/plugin đã ghi; không xóa Console hoặc tuyên bố không từng có lỗi. Bản source đã sửa được Unity compile và chạy restore thành công; assembly sau cleanup cũng không lỗi compile hiện hành.

Nếu cần rollback shortlist, đổi **đúng m_Health của Hall scene** về10000 bằng Unity SerializedObject/Inspector và ghi prefab override/SaveScene; không reset cả scene/prefab/git. Không dùng bản backup để ghi đè toàn scene. Khi tái lập thí nghiệm từ cấu hình mới, giữ nguyên raw cũ, tạo đợt/configID mới và ghi giá trị đang dùng để restore sau đo.

## Hiệu năng, kiểm còn thiếu và bước tiếp

Không có thay đổi thuật toán/runtime, dependency hoặc pipeline trong G2. Các helper nằm ngoài runtime hoặc đã được gỡ; không có polling diagnostic mới trong bản game. **Không đo FPS/GC/CPU/GPU mới**; wall-clock của trận realtime chỉ kiểm timeScale và thời lượng thật, không là benchmark. Không lấy số S9 trước đây chứng minh hiệu năng trên config mới.

Không chạy lại81EditMode/18Player smoke trong đợt chỉ chỉnh scene HP; tích hợp được kiểm bằng9run có HPresolve/ledger/terminal và một full-match realtime. Manual người chơi, Release/onscreen, candidate0/1543, wave RNG bổ sung, holdout, logistics và exploit công trình xa còn thiếu. Không gọi G2 DONE thành toàn bộ Normal đã cân bằng.

**Bước tiếp G3:** kiểm ngay C trên seed0 với1600, chạy matrix C/E/M/X trên3seed, đo completion/depot logistics và so công trình được bảo vệ/bỏ trống trước khi coi mở rộng tốt. Nếu1600 không đạt gate toànmatrix, có thể quay lại candidate1200 đã đăng ký và kiểm lại; không lén đổi giá/HP/wave cùng vòng. Sau freeze cấu hình mới mới lấy manual feedback, không compile/reload trong lúc người dùng đang chơi.

Nguồn: G01 raw/schema/summary, Unit.Awake/MaxHealth, GameplayGameBridge.RegisterGameplayUnit, GameplayMatch.EvaluateMatch, EnemyWaveManager/DayNightCycleManager và build plan hiện có trong repository.
