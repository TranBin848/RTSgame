using System;

namespace RTS.SaveLoad
{
    // No Entity, Unity object, asset path or collider pointer crosses the file boundary.
    [Serializable] public struct SaveVector { public float X, Y; }
    [Serializable] public struct ResourceAmounts { public long Wood, Gold, Meat; }
    [Serializable] public sealed class MatchSaveData
    {
        public int SchemaVersion;
        public string SaveId, CapturedUtc, ContentFingerprint;
        public long Generation, NextSaveId;
        public int NextRequestId;
        public MapSaveData Map;
        public PlayerSaveData Player;
        public ObjectSaveData[] Objects;
        public ClockSaveData Clock;
        public WaveSaveData Wave;
        public ReportSaveData Reports;
        public ViewSaveData View;
        public long GameplayTicks, SwarmTicks;
        public int TotalSpawned, TotalHits;
    }
    [Serializable] public sealed class MapSaveData
    {
        public int Width, Height, Seed, Attempt, GeneratorVersion, StartX, StartY, OriginCellX, OriginCellY;
        public SaveVector WorldOrigin;
        public float CellSize;
        public string SettingsJson;
        public byte[] Terrain, Reserved, RegionIds, Routes, Explored;
        public RegionSaveData[] Regions;
        public ConnectionSaveData[] Connections;
        public GatewaySaveData[] Gateways;
        public LandmarkSaveData Landmark;
        public PlacementSaveData[] Placements;
    }
    [Serializable] public struct RegionSaveData { public int X, Y, Radius, Role; }
    [Serializable] public struct ConnectionSaveData { public int From, To; }
    [Serializable] public struct GatewaySaveData { public int X, Y, Region; }
    [Serializable] public struct LandmarkSaveData { public int X, Y, Radius, Region; }
    [Serializable] public struct PlacementSaveData
    {
        public int Kind, VariantId, X, Y, ClusterId;
        public bool IsStartingResource;
        // Zero means this original placement no longer exists; never respawn it from the seed.
        public long ObjectId;
    }
    [Serializable] public sealed class PlayerSaveData
    {
        public ResourceAmounts Wallet;
        public string[] UnlockedTechnologyIds;
        public string ActiveTechnologyId;
        public long ResearchTownHall;
        public float ResearchElapsed;
        public int SurvivalNights;
        public StatisticsSaveData Statistics;
    }
    [Serializable] public sealed class ObjectSaveData
    {
        public long Id;
        public string DefinitionId;
        public int MaxHealth;
        public BodySaveData Body;
        public ActorSaveData Actor;
        public WorkerSaveData Worker;
        public ResourceSaveData Resource;
        public DepotSaveData Depot;
        public ConstructionSaveData Construction;
        public ProducerSaveData Producer;
        public EnemySaveData Enemy;
        public ProjectileSaveData Projectile;
        public SaveVector ViewOffset;
    }
    [Serializable] public sealed class BodySaveData
    {
        public SaveVector Position, PreviousPosition, BoxHalfSize;
        public float Radius, Health;
        public int Faction;
        public bool Targetable;
    }
    [Serializable] public sealed class ProjectileParametersData
    {
        public float Speed, ArcHeight, VisualScale, MinDuration, MaxDuration, DurationFactor;
        public bool Arc, Spinning;
    }
    [Serializable] public sealed class ActorSaveData
    {
        public int Kind, State, Task;
        public long Target, LastDamager;
        public SaveVector Destination, Facing, ProjectileOffset;
        public float Speed, Range, Detection, Interval, Delay, Cooldown, Windup, ThinkTimer, RepathTimer;
        public float MinIdle, MaxIdle, MinGraze, MaxGraze, GrazeTimer, FleeDistance, FleeJitter;
        public int Damage, AttackSequence, DamageSequence, RandomSeed;
        public bool Moving, Offensive, SuppressAcquire, Grazing;
        public ProjectileParametersData Projectile;
    }
    [Serializable] public struct GatheringSaveData
    {
        public int ResourceType, Amount, Capacity, State, Task;
        public float TickTime;
    }
    [Serializable] public sealed class WorkerSaveData
    {
        public long Node, Depot, Structure, Shelter, ResumeResource, ResumeConstruction;
        public int Phase, ResourceType, Carried;
        public float Timer;
        public GatheringSaveData[] Profiles;
    }
    [Serializable] public sealed class ResourceSaveData
    {
        public SaveVector Position;
        public float Radius;
        public int Type, Claims, Capacity;
        public bool RequiresKill;
    }
    [Serializable] public sealed class DepotSaveData { public int ResourceMask; public bool TownHall; }
    [Serializable] public sealed class ConstructionSaveData
    {
        public long Worker;
        public float Duration, Elapsed;
        public int RequestId, RequiredTechnology;
        public bool Active, Middle, Finished;
        public string ActionId;
    }
    [Serializable] public sealed class ProducerSaveData
    {
        public float Elapsed;
        public int CurrentRequest, SpawnSequence;
        public bool RequiresArchery;
        public TrainingSaveData[] Queue;
    }
    [Serializable] public sealed class TrainingSaveData
    {
        public int RequestId;
        public float Duration;
        public string ActionId, PrefabId;
    }
    [Serializable] public sealed class EnemySaveData
    {
        public long Target;
        public SaveVector Velocity, RetreatGoal;
        public float Speed, AttackRange, DetectionRange, AttackInterval, AttackDelay;
        public float Cooldown, Windup, ThinkTimer, StateTimer, VisualScale;
        public int Damage, VisualId, ProjectileVisualId, State, RandomSeed;
        public bool Ranged;
        public ProjectileParametersData Projectile;
    }
    [Serializable] public sealed class ProjectileSaveData
    {
        public long Owner, Target;
        public SaveVector Position, Start, Aim, PreviousPosition;
        public float Speed, Radius, Lifetime, Age, Duration, ArcHeight, VisualScale;
        public int Damage, Faction, VisualId;
        public bool Arc, Consumed, Spinning;
    }
    [Serializable] public sealed class ClockSaveData { public int Day, Phase; public float Elapsed; }
    [Serializable] public sealed class WaveSaveData
    {
        public float CurrentTownHallRange, ProceduralStartingRange, SpawnTimer, DelayTimer;
        public int WaveIndex, SpawnedInWave, StartedWaveCount, FailedSpawnAttempts, FallbackSpawnCount;
        public int WaveSeed;
    }
    [Serializable] public struct StatisticsSaveData
    {
        public ResourceAmounts Deposited, Spent, Granted;
        public int UnitsLost, ResearchCompleted;
        public float TownHallDamage;
    }
    [Serializable] public struct ReportSnapshotData { public StatisticsSaveData Statistics; public int ExploredCells; }
    [Serializable] public sealed class ReportSaveData
    {
        public int PreviousPhase, CompletedNight, LastAutosavedNight, WarningConsumedDay;
        public ReportSnapshotData PreviousDawn, LatestNight;
    }
    [Serializable] public sealed class ViewSaveData
    {
        public SaveVector Camera;
        public float Zoom;
        public long[] Selection;
    }
}
