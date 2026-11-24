# SL0 — Ma trận coverage và ownership

Đối chiếu source **24/11/2026**, gồm các partial gameplay/research/match/statistics/logistics và Swarm/bridge. Bản inventory SL0 bên dưới là hợp đồng field coverage. SL3 nối capture qua SimulationSnapshot/BridgeSaveCapture/MatchCaptureService; **SL4 đã nối restore first-day/idle-work** qua SimulationImport/BridgeSaveRestore và pre-Awake bootstrap. Active state ngoài SL4 được preflight từ chối, không bỏ qua field. [SL3 report](SL3-Report.md), [SL4 report](SL4-Report.md).

## Runtime authoritative

| Owner / trường source | DTO / policy |
|---|---|
| CombatBody: Position, PreviousPosition, Radius, Health, Faction, Targetable | Object.Body, giữ giá trị ở boundary; Health có thể âm khi chết |
| CombatBody: Collider, ExternalId | Không serialize pointer/instanceID. Adapter giữ BoxHalfSize hoặc radius; rebuild collider bằng API system; cấp ExternalId của view mới |
| Unit.MaxHealth, bridge m_BodyOffsets | Object.MaxHealth/ViewOffset; đặc biệt TownHall scene1200 khác prefab100, không lấy prefab default thay override |
| GameplayActor: Kind/State/Task | Actor giữ enum integer có kiểm miền; không suy từ view |
| GameplayActor: Target/LastDamager | Actor IDs, ref đã chết bị remove phải canonical về0; ref sống phải có trong object table |
| GameplayActor: Destination/Facing/ProjectileOffset, Speed/Range/Detection/Interval/Delay | Actor cùng tên; giữ effective runtime, không áp nâng cấp lần hai |
| GameplayActor: Cooldown/Windup/ThinkTimer/RepathTimer, Damage/AttackSequence/DamageSequence | Actor cùng tên; giữ windup -1 và timer âm hợp lệ, seed/counter không reset |
| GameplayActor: MinIdle/MaxIdle/MinGraze/MaxGraze/GrazeTimer/FleeDistance/FleeJitter/Grazing | Actor cùng tên; cừu cả sống và chết có cùng resource identity |
| GameplayActor: Moving/Offensive/SuppressAcquire | Actor boolean; stance từ Offensive, không lấy bản sao legacy |
| GameplayActor: PathDirty, GameplayWaypoint buffer | Derived: moving restore đặt PathDirty và yêu cầu A*; không lưu waypoint. Không cam kết path/job order từng frame |
| GameplayActor.Projectile: Speed/ArcHeight/VisualScale/MinDuration/MaxDuration/DurationFactor/Arc/Spinning | Actor.Projectile giữ đủ snapshot thông số |
| GameplayWorker: Node/Depot/Structure/Shelter, Phase/ResourceType/Carried/Timer | Worker refs ID và scalar; cargo giữ đúng loại kể cả đổi node/nộp/trú đêm |
| GatheringProfileData buffer: ResourceType/Amount/Capacity/TickTime/State/Task | Worker.Profiles, giữ toàn bộ buffer, không đọc profile đã nhân modifier lại |
| GameplayResource: Position/Radius/Type/Claims/Capacity/RequiresKill | Resource; Claims đối soát worker.Node + claims ngoài command nếu có. Không tự thêm reserve hữu hạn |
| GameplayResource.ExternalId | Runtime view handle mới; cừu không tạo resource Entity thứ hai |
| GameplayDepot: ResourceMask/TownHall | Depot, dựng kho trước nối worker |
| GameplayConstruction: Worker/Duration/Elapsed/RequestId/Active/Middle/Finished/RequiredTechnology | Construction và ActionId bridge/process. Finished elapsed được vượt duration tối đa một tick .05s; không clamp mất số liệu |
| GameplayProducer: Elapsed/CurrentRequest/RequiresArchery, TrainingRequest buffer RequestId/Duration theo FIFO | Producer + Queue, gắn ActionId/PrefabId từ bridge; không enqueue/debit lại |
| PlayerWallet: Wood/Gold/Meat | Player.Wallet; kiểm int range khi apply vào ECS (DTO totals dùnglong) |
| PlayerResearch: TownHall/Elapsed/Active/Unlocked/UnlockMask/Technology | Hall ID, ResearchElapsed, ActiveTechnologyId và UnlockedTechnologyIds. Unlocked economy_tools và bitmask suy theo catalog đã kiểm. Inactive Technology là metadata lần trước, normalize theo ID/catalog khi hydrate; inactive Elapsed vẫn giữ |
| MatchStatistics: Deposited/Spent/Granted Wood/Gold/Meat, UnitsLost/ResearchCompleted/TownHallDamage | Player.Statistics, giữ long chính xác. Không chạy AddResources để hydrate ledger |
| Gameplay match: Outcome/SurvivalNights | Chỉ Running được save. Player.SurvivalNights giữ goal; terminal bị từ chối, không persist result/reward |
| SwarmAgent: Target, Velocity/RetreatGoal, Speed/AttackRange/DetectionRange/AttackInterval/AttackDelay | Enemy tương ứng; targetable0 vẫn cần lưu quái retreat |
| SwarmAgent: Cooldown/Windup/ThinkTimer/StateTimer/VisualScale, Damage/VisualId/ProjectileVisualId/Ranged/State/Projectile | Enemy tương ứng, gồm đủ ProjectileParameters |
| SwarmProjectile: Owner/Target, Position/Start/Aim/PreviousPosition, Speed/Radius/Lifetime/Age/Duration/ArcHeight/VisualScale, Damage/Faction/VisualId/Arc/Consumed/Spinning | Projectile giữ mọi trường, không tạo từ ProjectileRequest mới vì sẽ reset Age/Duration |
| Gameplay m_Events/m_Damage/m_Shots; Swarm m_Damage/m_Shots/m_ExternalDamage | Boundary SL3: Complete, commit damage/shots, apply gameplay completions qua bridge đúng một lần. Bắt queue chưa xử lý là lỗi capture; không Clear. PresentationImpacts/VFX được bỏ vì không authoritative |
| Gameplay TickCount, Swarm TickCount/TotalSpawned/TotalHits | Header giữ diagnostic counters; Generation Swarm là guard runtime lifecycle, cấp generation mới thay vì tái dùng handle |
| Gameplay m_PathCursor/PathBudget, grids/scratch/targets/spatial, Swarm m_FlowDirty/m_LastGoalCell/render data/collider caches | Cursor/cache rebuild; PathBudget là config fingerprint. Goal/PrimaryTarget tìm Hall hợp lệ sau remap theo same closest-Hall policy, không dùng Entity cũ |

## Managed owners, map và trình bày

| Owner / trường source | DTO / policy |
|---|---|
| ProceduralMapAuthoring.GeneratedMap: Width/Height/Seed/Attempt/StartX/StartY/Terrain/Reserved/Resources/GeneratorVersion | Map snapshot cuối + Placements/identity tombstone0. Tắt Awake generation trong LoadGame, không respawn bằng seed |
| StrategicLayout: Regions/Connections/Gateways/Landmark/RegionIds/Routes | Map tương ứng, giữ version3; Legacy version1 dùng arrays topology rỗng |
| ProceduralMapSettings/settings data, authoring OriginCell, WalkableTilemap.CellToWorld/cellSize/scale | Map.SettingsJson + OriginCell/WorldOrigin/CellSize. Adapter phải parse settings type whitelist, kiểm origin/nav và map consistency trước apply |
| MapValidationReport, timing build/core/terrain/resource/nav | Recompute report, không coi metrics cũ là số đo load mới |
| bridge m_Proxies/m_UnitsById/m_ResourceEntities/m_BodyOffsets | Remap SaveId→Entity/view, handles cũ không đưa file; resource/unit registry từ instance cùng ID |
| bridge m_TrainingViews: Building/Prefab/Action/UiId/Message; m_NextRequestId | Producer.Queue action/prefab ID, next request counter; SL3 giữ ActionSO trong registry. UI ID cấp mới khi hydrate; Message lấy action định nghĩa tương thích |
| bridge m_ConstructionViews → BuildingProcess m_BuildAction/m_Worker/m_Structure/m_QueueUiId/m_ProgressTimer/m_isMiddle/m_isFinished | Construction action ID/native state, rebuild process presentation bằng API hydrate im lặng. Constructor hiện tạo object/queue mới, không gọi khi load |
| ProductionBuilding m_SpawnSequence | Producer.SpawnSequence: golden-angle scatter của lần spawn tiếp; không reset. Legacy queue/current/timer chỉ dùng khi không DOTS, ngoài phạm vi save |
| ProductionBuilding m_LastQueuedFrame | Input debounce, reset khi resume; không lưu frame counter từ process cũ |
| WorkerUnit m_ResumeResource/m_ResumeConstruction/m_ShelterTownHall | Worker.ResumeResource/ResumeConstruction/Shelter; restore cả managed refs, nếu chỉ restore ECS thì daybreak không ra Hall đúng |
| WorkerUnit legacy carried dictionary/assigned node/depot/active profile/gather timer/isReturning/isSheltered | Không lấy làm nguồn khi IsDotsControlled. Presentation/collider trạng thái lấy từ ECS Phase; hydrate phải đồng bộ managed shelter owner |
| DayNightCycleManager CurrentDay/CurrentPhase/m_PhaseTimer/m_HasStarted | Clock.Day/Phase/Elapsed. Restore suppress OnDayChanged/OnPhaseChanged lịch sử, tính progress/effects từ duration và timer; subscription cấp lại theo scene |
| EnemyWaveManager m_CurrentTownHallRange/m_ProceduralStartingRange/m_SpawnTimer/m_WaveDelayTimer/m_CurrentWaveIndex/m_SpawnedInCurrentWave/m_StartedWaveCount/FailedSpawnAttempts/FallbackSpawnCount | Wave tương ứng; spawn/delay có thể âm sau trừ tick. Giữ starting range, không tính lại từ nhà đã mở rộng |
| TacticalNightPlan WaveSeed/Nights/Counts/SpawnInterval/Delay/RangedEvery/Approach, prefab slots | WaveSeed + content fingerprint. Đường procedural E9 dùng hash theo seed/day/wave/ordinal, không UnityEngine.Random |
| EnemyWaveManager nhánh legacy GetRandomEnemyPrefab/ring random | Unsupported trong Save/Load v1; preflight đòi tactical plan + procedural gateway. Không capture/ghi đè global Random.state |
| Fog m_Bounds/m_Explored/ExploredCellCount | Map explored theo origin/dim; count derive. m_VisibilityCounts/Strengths/NextRefreshTime/affectables/texture/tile cache rebuild sau attach actors, trước publish UI |
| MatchReportService m_PreviousPhase/m_PreviousDawn/LatestNight/CompletedNight | Reports tương ứng; giữ baseline và delta đã chốt. m_Final/m_Frozen không save vì terminal bị từ chối; m_Disposed lifecycle mới |
| DuskWarningPresentation m_LastPhase/DuskSignalCount + last rendered seconds/waves/known | Reports.WarningConsumedDay; init snapshot phase, không phát bell lịch sử. Counter presentation không tác động gameplay; HUD cache derive |
| Autosave dawn consumed cycle | Reports.LastAutosavedNight; service tương lai chốt sau retreat + worker daybreak + report. Current code chưa có autosave owner |
| MatchSession m_ResultShown, menu Time.timeScale/pause, build placement gesture | Không lưu result. Save trả pause cũ; load thành công luôn pause. Placement chưa commit và drag/tooltip hủy an toàn, không mất transaction đã trả tiền |
| CameraController camera transform XY/orthographicSize; GameManager selection | View.Camera/Zoom/Selection ID có bounds; camera Z/config lấy camera definition, reset smoothing velocity/drag state để không trượt sau load |
| Unit CurrentState/Task/Target/CurrentHealth/isTargeted, m_DotsAttackSequence, animator, flash/death events | Derive từ ECS và selection. Hydrate im lặng seed sequence/health, không gọi ApplyDotsSnapshot bình thường gây popup/trigger/death lần hai |
| E1–E6/E8 lighting/depth/VFX/badge/decor/rings/minimap/UI listeners/pools | Rebuild cùng config/fog/clock; không serialize materials/textures/AudioSource/VFX/empty pool hoặc replay completion signals |

## Identity và RNG đã sửa trong runtime

`SaveIdentity` cấp một lần tại native factories/AttachActor/CreateResource, dùng allocator của Swarm. Cùng một cừu actor/resource giữ một component. Obstacle proxies cũng nhận ID nhưng là cache; adapter loại object chỉ có proxy dẫn xuất, ID gap được phép. IDs không tái dùng sau reset/death; allocator mới ở World mới, SL4 phải phục hồi NextSaveId trước spawn mới và gắn ID từ bảng đã validate.

GameplayActor.RandomSeed và SwarmAgent.RandomSeed được chốt từ Entity.Index tại lần spawn gốc. Thay cách đọc RNG/think cadence/separation tie-break sang trường seed này; trận mới giữ cùng giá trị công thức cũ, remap có thể giữ seed mà không phụ thuộc Entity.Index mới. Cừu còn dùng AttackSequence và DamageSequence để derive RNG; cả hai nằm trong DTO. Swarm không có RNG mutable riêng trong job. Pathfinding/spatial iteration vẫn không có hợp đồng bit-replay.

## Chốt integration và rollback

Theo [Architecture](Architecture.md): capture ở cuối frame đã apply native completion/damage; fence phải bao cả clock/wave/command, không chỉ Time.timeScale. Không mutate ECS để làm sạch mismatch claims/ref. Semantic canonicalization chỉ ref đã chết→0 theo rule rõ và cache dẫn xuất; lưu báo cáo mismatch để không che bug.

Rollback snapshot bao cùng map/identity/cargo/queue/research/clock/wave/explored/report/camera/selection và pause trước load. Không dùng wallet-only rollback. Chỉ tháo scene sau disk validation và asset preflight; rollback thất bại thì giữ blocked, không resume world nửa hydrate. Các policy đã chốt nhưng kiểm nghiệm còn SL3–SL6.
