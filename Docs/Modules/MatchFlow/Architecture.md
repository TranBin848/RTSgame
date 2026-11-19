# S8 — Luồng kết thúc trận và cân bằng bước đầu

**17/11 — S8 DONE ở phạm vi build plan:** người dùng test rồi trả lời “oke mọi thứ ổn” cho checklist thắng/thua/replay/menu/lựa chọn. Chỉ ghi xác nhận chung, không gán thêm seed/ngày thắng/độ khó cụ thể. Báo cáo giữ nguyên bằng chứng6policy và đối chứng không thao tác: baseline còn yếu về áp lực sinh tồn; đây là giới hạn đã biết, không tuyên bố cân bằng cuối hoặc độ vui. D4 đã đóng, S9 đang chạy; các trạng thái chờ16/11 dưới đây là lịch sử.

Ngày triển khai: **16/11/2026**. Theo [BuildPlan S8](../Progression/BuildPlan.md). Các mục đo dưới đây chỉ là kiểm tra Editor và kịch bản tự động; chưa thay cho playtest của người chơi, Player build hoặc benchmark.

## 1. Mục tiêu và phạm vi

ProceduralGame có một trận hữu hạn, mục tiêu nhìn thấy ngay trên màn hình, kết quả thắng/thua, chặn lệnh sau kết thúc, nút Play again và Main menu. S0 chưa có luồng kết thúc; mốc đề xuất 5 ngày được cụ thể hóa thành **sống qua 5 đêm**, thắng ở đầu ngày 6. Mất toàn bộ TownHall phe người chơi còn sống thì thua. Nếu TownHall cuối chết đúng biên bình minh, **thua ưu tiên trước thắng**.

Không có tiếp tục trận sau game-over, save/load, offline, prestige, thêm tech hoặc dependency mới. Các giá trị 5 đêm/giá/thời lượng ban đầu là cấu hình thử nghiệm, không được coi là cân bằng đã nghiệm thu.

## 2. Dữ liệu và ranh giới DI

| Thành phần | Trách nhiệm và lifetime |
|---|---|
| `GameplayMatch.cs` trong `RTS.DOTS` | `MatchOutcome` Running/Victory/Defeat; `SurvivalNights`; evaluate TownHall từ ECS, kết quả chỉ chuyển một lần |
| `GameplaySimulationSystem.ResetGameplay` | Dọn outcome/objective cùng wallet/research khi teardown; scene legacy không configure nên vẫn chạy như cũ |
| `MatchSession` trong Integration | Nhận gameplay/clock/game từ root; refs menu/panel/text/button được serialize trong scene; chuyển ngày MB sang EvaluateMatch, hiển thị kết quả |
| `GameSceneCompositionRoot` | Yêu cầu MatchSession, configure mục tiêu trước bind; MatchSession inject native state cho clock và menu |
| `GameFlowMenu`, `DayNightCycleManager` | Dependency match được truyền qua `InitializeMatch`, không Find/static service; chặn resume/options/pause hoặc tick sau kết thúc |
| `MatchSessionEditorTools` | Dựng/wire canvas trong Editor, được gọi cả khi Create or Refresh Gameplay Scene; lookup chỉ ở authoring, không trong runtime mới |

Runtime assembly `RTS.DOTS` không tham chiếu GameManager/Unity UI/MonoBehaviour gameplay. Swarm đọc native gameplay system hiện hữu trong cùng World để giữ terminal gate ngay cả khi `Paused` bị caller khác đổi; đây là cơ chế ECS, không đưa managed service vào Burst job. State thuộc World của bridge scene hiện tại, reset theo lifecycle đã có. UI canvas riêng sortingOrder 2000 để không bị nút ẩn HUD làm mất kết quả.

## 3. Luồng xử lý và thuật toán

1. Root khởi tạo bridge/catalog; MatchSession kiểm đủ dependency và configure 5 đêm, đăng ký hai nút một lần. Root bind scene rồi mới mở composition gate.
2. Trong LateUpdate, khi map ready và composition không bị chặn, gọi `EvaluateMatch(CurrentDay)`. Native hoàn tất dependency rồi duyệt depot có `CombatBody`: Hall != 0, Faction == 0, Health > 0. Chi phí O(số depot), không quét Hierarchy.
3. Không còn hall → Defeat; còn hall và day > SurvivalNights → Victory; còn lại Running. Kết quả terminal bất biến cho tới ResetGameplay; đánh giá lặp không hoàn tiền hoặc mở khóa lại.
4. Native dừng Swarm, ngừng tick economy/research/combat, hủy research đang chạy **không hoàn tiền**, giữ các tech đã mở. Queue đang chờ được đóng băng tới teardown; không hoàn phí khi game-over.
5. Session hủy placement đang xem, đóng pause/options, bật panel kết quả và đặt Time.timeScale = 0. Native terminal guards vẫn chặn khi caller sửa timeScale/Paused. Các lệnh move/attack/stance/gather/depot/shelter/build/train/research/wallet, spawn quái/đạn và queue damage đều có gate. Cancel/release/remove/reset là API cleanup, vẫn được phép phục vụ teardown.
6. Play again gọi menu Play để load ProceduralGame mới; Main menu load MainMenu. Teardown dọn ECS/producer/UI cũ theo bridge; trận mới cấp 100 mỗi tài nguyên, research 0, Running và goal 5. Mục tiêu chưa được áp vào SampleScene hoặc demo ECS không configure.

Không đổi tốc độ ngày/đêm: Day60 + Dusk30 + Night45 = 135 giây danh nghĩa, 5 chu kỳ = 675 giây. Với tick float .05 trong harness, biên pha rơi ở 675.75 giây; đây là lượng tử hóa/float của harness, không phải cam kết đồng hồ chính xác tuyệt đối.

## 4. Lịch wave: quyết định S8

Giữ luật **bình minh kết thúc tiếp viện** và quái hiện hữu retreat theo cơ chế S6. Quota cấu hình là trần trong một đêm, không phải cam kết mọi lượt phải xuất hiện. Không dồn 6 lượt trễ của đêm 3 vào bình minh hoặc kéo dài đêm chỉ để khớp quota. Ba đêm đầu vẫn là 12/15/15 = 42 lượt thực tế trên lịch hiện tại, so 48 cấu hình. UI hiện hiển thị điểm bắt đầu wave, không hiển thị tổng enemy quota; điểm bắt đầu nằm trước bình minh vẫn hợp lệ dù wave bị cắt.

Quyết định này giữ timing/áp lực nền S6 để có baseline so sánh. Chỉ tăng quota/giảm interval sau khi có đánh giá áp lực chiến đấu; không coi 48 vs 42 là lỗi đếm cần sửa bằng đổi số kiểm thử.

## 5. Cách chạy và kiểm thử

- Mở MainMenu → Play hoặc ProceduralGame → Play. Objective ở đáy màn hình. Bảo vệ ít nhất một TownHall qua 5 đêm; kết quả có Play again/Main menu. Escape hoặc Resume không được mở lại trận terminal.
- `MatchSession.m_SurvivalNights` là cấu hình authoring (mặc định5); runtime từ chối goal < 1 hoặc đổi goal giữa trận. Không dùng đổi clock để playtest cân bằng.
- `Tools/ValidateS8.cs`: harness qua MCP, Play disposable/paused. Victory dùng clock ngày6 giả lập; Defeat đặt hallHP0; đây là fixture kiểm đường kết thúc, không phải số liệu balance. Kiểm UI/time/native gate, research/wallet/production/clock/wave, nút thật và fresh state.
- `Tools/PlaytestS8.cs`: hai policy Economy/Military, ba seed0/1/1543. Tick .05 giây với clock/wave/ECS thật, HP gốc, ví100×3, giá/thời lượng gốc. Native commands và bridge production có kiểm giá, debit khi enqueue thành công; số frame Editor không đại diện simulated frame nên dùng bridge admission, không bypass thời gian train. Seed RNG cho lựa chọn wave được khóa seed+8000, khôi phục World.Time/RNG sau run.
- Policy mỗi5 giây thử research theo thứ tự nhánh; kinh tế nhắm6 worker, quân sự4; tối đa8 soldier và6 archer; phân worker theo index2wood/1gold/1meat, chọn node gần nhất. Không có tactical micro, có tri thức vị trí tài nguyên, không đo hành vi khám phá của con người. Mở rộng: thử kho tại landmark sau180giây nếu đủ100W/100G, có worker và placement hợp lệ.
- Số gather là tăng cargo, deposit là tăng wallet trong tick simulation, spend là giảm wallet trong command policy; không gộp tiền ban đầu thành income. Tốc độ deposit = tổng nộp / thời gian trận, không gọi là tốc độ khai thác lý thuyết.
- [Raw runs](../Progression/S8-ScriptedRuns-2026-11-16.txt) ghi unlock, từng bình minh, quân/worker, HP hall, outcome, gather/deposit/spend và quyết định mở rộng. Đọc các giới hạn policy trước khi suy ra balance.

## 6. Kết quả đã kiểm và phần chờ

**S8a luồng kỹ thuật: hoàn thành. S8b đo/cân bằng: PARTIAL, chờ playtest thủ công theo yêu cầu người dùng16/11 “Chưa test được, để mục này chờ”.** Không chuyển sang D4/S9 trong lượt này.

- DOTS EditMode cuối: **64/64 PASS**, không fail/skip, 2.8412262 giây; gồm5 kiểm thử mới về biên ngày/thua ưu tiên/nhiều hall/reset/legacy/invalid config và terminal commands. Procedural **17/17 PASS**, không fail/skip, 7.0509617 giây. Đây là thời gian Test Runner, không phải benchmark runtime.
- Unity MCP đã kiểm đúng ProceduralGame. Fixture Victory và Defeat PASS; Resume/Pause/Back/Options, production, wallet, clock và wave không làm trận terminal chạy tiếp. Play again khởi tạo scene mới đúng state; Defeat → nút Main menu dọn actors/agents/wallet/goal về0 → Play lại được.
- Ảnh Defeat đã render/inspect ở [S8-defeat](../Progression/Evidence/S8-defeat.png); [Victory final](../Progression/Evidence/S8-victory-final.png) là kết quả run quân sự seed1543 qua đủ5đêm, UI hiển thị ngày6. Ảnh `S8-victory.png` đầu tiên chụp lúc Editor paused nên là render cũ, **không dùng làm bằng chứng hình ảnh thắng**.
- Editor mặc định không chạy frame khi mất focus (`runInBackground=0`); harness bật Application.runInBackground tạm trong Play để scene Start/render tiến được, không đổi ProjectSettings. Trạng thái chưa ready trong lần kiểm đầu do chưa có frame, không phải fix lifecycle mới.
- DI final: repeat Initialize giữ state;6trường hợp dependency null/khác bị từ chối (MatchSession,clock,menu). Fixture Victory/terminal gates và replay fresh state đã chạy lại sau code cuối. Xác nhận S7 trước đây không được tái dùng làm acceptance S8.

### Sáu kịch bản đã đo

E = kinh tế trước, M = quân sự trước. Tất cả **Victory ngày6 tại675.75giây**, spawned76 với từng đêm12/15/15/15/19. HP TownHall gốc10000, không được bảo vệ/hoàn máu trong run.

| Seed/policy | Tools/Cargo mở lúc (s) | Archery/Training mở lúc (s) | Outposts/Fortify mở lúc (s) | Worker/lính cuối | HP hall cuối | Deposit W/G/M |
|---|---|---|---|---|---|---|
| 0/E | 20.05/60.05 | 90.05/385.05 | 455.05/650.05 | 6/8 | 8592 | 990/260/222 |
| 0/M | 250.05/340.05 | 25.05/225.05 | 370.05/chưa | 4/10 | 8598 | 560/260/220 |
| 1/E | 20.05/55.05 | 85.05/chưa | chưa/chưa | 6/7 | 8110 | 660/110/268 |
| 1/M | 180.05/230.05 | 25.05/100.05 | 330.05/495.05 | 4/3 | 8114 | 680/230/0 |
| 1543/E | 20.05/55.05 | 85.05/375.05 | 405.05/630.05 | 6/8 | 9414 | 1110/290/245 |
| 1543/M | 325.05/370.05 | 25.05/220.05 | 460.05/chưa | 4/10 | 9414 | 650/260/250 |

| Seed/policy | Gather W/G/M | Deposit trung bình W/G/M mỗi phút |
|---|---|---|
| 0/E | 1081/275/236 | 87.90/23.09/19.71 |
| 0/M | 609/260/236 | 49.72/23.09/19.53 |
| 1/E | 720/140/277 | 58.60/9.77/23.80 |
| 1/M | 725/241/0 | 60.38/20.42/0.00 |
| 1543/E | 1118/290/245 | 98.56/25.75/21.75 |
| 1543/M | 673/267/272 | 57.71/23.09/22.20 |

Không run nào đặt được kho landmark: policy giữ quân/research trước nên chưa đồng thời dư100W/100G vào lần thử. Không được diễn giải thành placement bị lỗi hoặc người chơi không thể mở rộng; kiểm xây landmark S6/S7 trước đây đã PASS. Chỉ là một thứ tự chi tiêu tự động chưa chọn expansion. Seed1/M không nộp thịt: giới hạn phân công/node-path của policy cần quan sát riêng, không kết luận map không có thịt hay tự giảm meat cost. Gather có thể lớn hơn deposit vì cargo còn mang khi kết thúc.

### Kết luận cân bằng bước đầu và cấu hình giữ lại

- Kinh tế trước mở Tools/Cargo sớm và thường tạo nhiều gỗ hơn, nhưng không bảo đảm đủ vàng mở mọi tech (seed1/E). Quân sự trước mở Archery sớm nhưng có thể trì hoãn Training tới220–225giây do chi tiêu song song. Các kết quả này phụ thuộc số worker/phân công và không phải so sánh cô lập hiệu ứng research.
- Trong3cặp, chênh lệch HP hall cuối chỉ6/4/0, trong khi hall còn81.10–94.14% máu. Hai policy đều thắng và chưa mở rộng. Đây là dấu hiệu cấu hình baseline **chưa tạo khác biệt rõ về sống sót hoặc buộc cân nhắc mở rộng** trong kịch bản không micro; chưa phải xác nhận cân bằng tốt.
- Giữ nguyên giá6tech, thời lượng research/train, HP/prefab, quota/interval và60/30/45 để không đồng thời đổi nhiều biến khi chưa có phản hồi người chơi. Cấu hình mới đã chốt về chức năng chỉ là goal5đêm. Không mở rộng tree để che kết quả này.
- Đợt S8b tiếp theo: người chơi thử kinh tế/quân sự và ghi đêm áp lực, thời gian không có quyết định khả dụng, lý do chọn landmark; thêm control không nghiên cứu/không mua quân nếu cần xác định HP hall tự gánh được bao nhiêu. Nếu nhận xét xác nhận quá dễ, thử một biến HP hoặc wave ở scene/config thử nghiệm, chạy lại cùng6policy và so loss-day/HP/unlock/expansion; không sửa cùng lúc giá/quota/duration. Sau đó mới chốt cân bằng và đánh dấu toàn S8 DONE.

## 7. Đo hiệu năng, giới hạn và hướng tiếp

Chưa đo FPS/GC/allocation/Player S8. Harness có reflection, LINQ, query copy và snapshot mỗi tick nên không dùng thời gian chạy harness để công bố hiệu năng game. EvaluateMatch duyệt depot main thread mỗi frame khi trận Running; nếu số công trình lớn gây chi phí đo được thì chuyển sang kiểm theo event/native count trong lần tối ưu có profile.

Không suy từ policy thắng rằng hai nhánh đã cân bằng hoặc trò chơi đủ hấp dẫn. Nếu thiếu vàng/thịt làm policy ngừng mở tech, trước hết kiểm phân công/đường đi/đặt kho và chiến thuật; không tự giảm mọi giá để làm test thắng. Nếu TownHall HP gốc làm cả hai nhánh đều thắng dễ, cần playtest xác nhận trước khi tune HP/wave. D4 toàn project và S9 build/performance/thesis vẫn chưa triển khai trong slice này.

## 8. Nguồn đối chiếu

Bổ sung17/11: [raw control seed1](../Progression/S8-ControlRuns-2026-11-17.txt) thắng đủ5đêm tại675.75s/76spawn, hall10000→8190, ví100×3,0nghiên cứu/huấn luyện/thu/nộp/chi. Giữ quân/công trình ban đầu và AI phòng thủ; không có lệnh người chơi từ policy. Baseline có thể thắng không tương tác trên seed1, chưa ngoại suy seed khác hoặc kết luận chỉ do HP. Field peak=0/expansion=not-affordable trong raw là default harness khi bỏ toàn policy, không phải phép đo peak/khả năng mua kho; snapshot cuối thực tế3worker/1lính. Người dùng đã nhận test S8 trong lượt17/11, Editor được trả ở ProceduralGame/seed1/ngoàiPlay; chưa áp draftD4 hoặc đoS9 trong lúc user đang chơi.

[BuildPlan](../Progression/BuildPlan.md), [S0 baseline](../Progression/Progress.md), [S6 wave evidence](../ProceduralGeneration/S6c-WavesLifecycle.md), [DI Architecture](../DependencyInjection/Architecture.md), [DOTS DevelopmentGuide](../DOTS/DevelopmentGuide.md). Source: `GameplayMatch.cs`, `GameplayResearch.cs`, `GameplaySimulationSystem.cs`, `SwarmSimulationSystem.cs`, `MatchSession.cs`, `GameSceneCompositionRoot.cs`, `GameFlowMenu.cs`, `EnemyWaveManager.cs`, `DayNightCycleManager.cs`; kiểm thử tại `GameplaySimulationTests.cs` và hai harness nêu trên. Chưa bổ sung thư viện/thuật toán từ nguồn ngoài.
