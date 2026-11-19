# D3c4a — factory đạn và bắt đầu gameplay tường minh

Ngày **15/11/2026**. Implementation và kiểm API trong Unity Editor PASS; D3/S6 tổng thể vẫn PARTIAL. [Progress](../Progression/Progress.md) là checkpoint, [BuildPlan](../Progression/BuildPlan.md) là kế hoạch tổng. D3c4 được chia nhỏ: slice này xử lý đạn; **D3c4b tiếp Unit/Worker/Structure/Sheep/AIPawn và pool lifecycle**.

## Mục tiêu, phạm vi và quyết định

Loại dependency ẩn `SwarmGameBridge.Instance` khỏi Projectile. Không cần inject bridge vào đạn khi có thể bỏ hẳn trách nhiệm chuyển ECS khỏi view legacy. Tái dùng `GameManager.m_Bridge` đã được root cấp, không thêm container/interface/service. Ba caller TowerUnit, RangerUnit, EnemyRangerUnit gửi prefab/owner/target/damage/position qua `GameManager.TrySpawnProjectile`.

Trong vòng kiểm scene reload, quan sát root Ready nhưng ví0 và GameManager không có listener ngày/đêm, vẫn vậy sau frame kế tiếp. `GameManager.Start` cũ có early return khi root chưa Ready, không retry. Slice này sửa bằng bước `BeginGameplay` do root gọi sau BindScene; scene legacy gọi từ Start. Đây là lỗi lifecycle gặp trong phiên, không suy ra nó do thay đổi factory hoặc do native crash cũ.

## Dữ liệu, xử lý và tích hợp

`GameManager.TrySpawnProjectile` nhận prefab và context mỗi lần bắn; không lưu scene service vào ScriptableObject/prefab. Kiểm prefab/owner/target, damage dương, active/targetable, cùng scene, composition gate và Time.timeScale. Bridge hiện hữu quyết định nhánh ECS; thiếu bridge nhưng actor đã DOTS thì từ chối, tránh damage writer legacy thứ hai.

Nhánh ECS gọi `SwarmGameBridge.TrySpawnProjectile` (thay `ConvertLegacyProjectile`). Bridge kiểm scene bound/pause, owner/target cùng scene, còn sống cả view và CombatBody, có proxy đã đăng ký. Không gọi ProxyFor để ngầm tạo actor trong lúc bắn. Native SpawnProjectile trả Entity; chỉ báo true khi Entity khác Null. Mapping faction/visual/arc/speed/scale/damage giữ nguyên từ prefab. Đường gameplay ECS tự bắn qua jobs/Fire hiện có không chuyển sang MonoBehaviour.

Nhánh legacy dùng RuntimeObjectPool với owner Scene rồi Initialize owner/target/damage/position. Projectile chỉ quản lý chuyển động tuyến tính/parabola, va chạm, sát thương một lần và return/reset; không tra bridge hoặc manager. Initialize từ chối owner/target DOTS hoặc khác scene. Không tạo rồi release một GameObject đạn tạm trong nhánh ECS như trước.

Pool static và dependency của Unit mẹ chưa migrate. Ba ranged callers vẫn dùng `m_GameManager` hiện có; cách cấp field này trong Unit.Awake còn global và thuộc D3c4b. Không gọi đây là toàn graph đạt DI. Kiểm tra pool qua scene trong slice này chỉ với Projectile không giữ bridge; không chứng nhận Unit reuse qua nhiều gameplay scene.

`BeginGameplay` là bước idempotent: root gọi sau BindScene, trước IsReady/CompleteComposition; guard ngăn root-scene bắt đầu trước khi initialized/bound. Lần đầu clear action UI, cấp100 mỗi tài nguyên và subscribe ngày/đêm; lần sau không debit/credit/subscribe lại. GameManager.Start chỉ còn khởi động scene legacy; không phụ thuộc thứ tự Start với root. Startup không thay đổi preset/seed/chi phí hoặc luật ngày đêm.

## Cách chạy và kiểm thử

Mở ProceduralGame hoặc SampleScene, vào Play, chờ Start rồi pause Editor. Qua Unity MCP `script-execute`, dùng nội dung [ValidateD3Projectiles.cs](../../../Tools/ValidateD3Projectiles.cs), class `D3ProjectileValidation`:

- `CheckFactory`: fresh scene không đạn; kiểm10rejectcases rồi bắn native thật. Tick Swarm40 lần với dt0,05, restore World.Time và health sau test.
- `CheckLegacyPool`: tạm bỏ bridge field của manager trong try/finally, dùng fixtures Unit chưa Start/chưa bind. Kiểm callbacks collision thật, reset/reuse và scene ownership; restore field/destroy đúng fixtures tự tạo. Không coi đây là scene legacy không ECS hoàn chỉnh.
- `CheckLegacyCallers`: fixtures Tower/Ranger/EnemyRanger, gọi callback launch qua reflection; kiểm owner/target và pending callback lặp không tạo đạn thứ hai.
- `Tools/ValidateS6Lifecycle.cs`: `CheckMenu`, `CheckReady` khi đổi scene; CheckReady nay gọi BeginGameplay hai lần và kiểm wallet/listener không nhân đôi. SampleScene không có GameFlowMenu, dùng SceneManager.LoadScene(MainMenu); từ MainMenu dùng GameFlowMenu.Play; từ ProceduralGame dùng ReturnToMenu. Cho scene chạy Start bằng Editor Step trước assert.
- Exit Play bỏ mọi thay đổi tạm; EditMode test assemblies `RTS.DOTS.Tests` và `RTS.ProceduralGeneration.Tests`.

## Bằng chứng đã chạy

| Trường hợp | Kết quả |
|---|---|
| ProceduralGame và SampleScene |10rejectcases không thêm actor/projectile/view: null prefab/owner/target, damage0, target chưa bind, TimeScale0, Simulation.Paused, composition blocked, native target health0, thiếu bridge với DOTS actor |
| ECS launch |Đúng1projectile Entity, không view legacy/actor mới; owner/target/damage đúng. Sau2giây mô phỏng health giảm đúng7, projectile đã dọn. Cả hai scene PASS; procedural chạy lại sau reload/startup fix PASS |
| Legacy impact/reuse |Collision callback lặp không damage lần hai; cùng object pool bắn7 rồi9, health tổng giảm16, reset target/owner/hit state; không thêm ECS projectile. Hai scene PASS, procedural kiểm lại sau startup fix |
| Scene ownership |Target khác scene bị từ chối. Projectile inactive được chuyển vào scene tạm, acquire lại trở về đúng owner scene trước dùng; PASS. Không kiểm projectile đang bay qua unload target scene |
| Callers |Tower/Ranger/EnemyRanger truyền đúng owner/target; pending launch lặp sinh một shot. PASS trên fixtures procedural, không phải animation clip playtest |
| Lifecycle trước/sau sửa |Trước sửa menu→game rootReady nhưng wallet0/listener0; sau sửa menu→game→menu→game:25actors/132nodes, ví100x3, đúng1listener của wave và GameManager; repeat BeginGameplay không nhân ví/listener. Menu không bridge/entities/projectiles, ví0/cờ sạch |
| Regression cuối |**59/59 DOTS**,0fail/skip,2,0068762s; **17/17 procedural**,0fail/skip,6,9916392s |

Console lịch sử không bị xóa; query Exception sequence>3507 sau checks cuối trả0. Hai lỗi công cụ trong phiên đã điều chỉnh: MoveGameObjectToScene đòi root nên fixture pool phải tách parent trước khi chuyển scene; SampleScene không có GameFlowMenu nên chuyển menu trực tiếp. Không sửa gameplay để che các lỗi setup này.

## Hiệu năng, giới hạn và nghiệm thu còn lại

Không benchmark FPS/allocation/startup mới. Quan sát nhánh native không tạo view là kiểm đếm object, không phải số đo tốc độ cải thiện. Thời gian2giây là mô phỏng, durations Test Runner là thời gian chạy tests.

Chưa Player build, full combat/animation/camera playtest hoặc toàn legacy scene không ECS. Cần người dùng thử: (1) vào game từ menu rồi quay menu/vào lại vẫn đủ tài nguyên đầu trận; (2) cung thủ/tháp bắn bình thường, không mất/multiplicate mũi tên hoặc sát thương thấy được; pause/resume bình thường. Không tuyên bố sửa native crash SerializationCache trước đây.

D3c3 manual train/pause/destroy đã được người dùng xác nhận “oke tất cả ổn tiếp tục đi” ngày15/11; không ảnh/log kèm theo. D3c4a manual mới còn chờ. Tiếp D3c4b rà Unit BindDots/registration/return, Worker/Sheep bridge globals, Structure map và AIPawn dependencies trước sửa. S6 input/visual/nhiều ngày còn lại trước S7.

## Nguồn và file đối chiếu

- [DI Architecture](Architecture.md), [Inventory](Inventory.md), [D3c3](D3c3-Production.md), [DOTS gameplay architecture](../DOTSGameplay/Architecture.md), [DevelopmentGuide](../DOTS/DevelopmentGuide.md).
- `Assets/Game/Scripts/Utils/Projectile.cs`, `Managers/GameManager.cs`, `Units/TowerUnit.cs`, `RangerUnit.cs`, `EnemyRangerUnit.cs`.
- `Assets/Game/DOTS/Integration/SwarmGameBridge.cs`, `GameSceneCompositionRoot.cs`; `Tools/ValidateD3Projectiles.cs`, `ValidateS6Lifecycle.cs`.
- Giữ thuật toán projectile/native hiện có; không thêm thư viện, không sửa package/Editor/render pipeline/scene asset trong slice này.
