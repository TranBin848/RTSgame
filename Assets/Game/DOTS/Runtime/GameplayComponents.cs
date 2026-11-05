using Unity.Entities;
using Unity.Mathematics;

namespace RTS.DOTS
{
    public enum GameplayKind : byte { Worker, Soldier, Archer, Sheep, Building, Tower }
    // Values deliberately match the presentation enums UnitState/UnitTask, without referencing Assembly-CSharp.
    public enum GameplayState : byte { Idle, Moving, Attacking, Chopping, Mining, Building, Dead, Collecting }
    public enum GameplayTask : byte { None, Build, Chop, Mine, Attack, ReturnResource, Collect }
    public enum WorkPhase : byte { None, Gathering, Depositing, Constructing, ReturningToShelter, Sheltered }

    public struct GameplayActor : IComponentData
    {
        public GameplayKind Kind;
        public GameplayState State;
        public GameplayTask Task;
        public Entity Target, LastDamager;
        public float2 Destination, Facing, ProjectileOffset;
        public float Speed, Range, Detection, Interval, Delay, Cooldown, Windup, ThinkTimer, RepathTimer;
        public float MinIdle, MaxIdle, MinGraze, MaxGraze, GrazeTimer, FleeDistance, FleeJitter;
        public int Damage, AttackSequence, DamageSequence;
        public byte Moving, PathDirty, Offensive, SuppressAcquire, Grazing;
        public ProjectileParameters Projectile;
    }

    [InternalBufferCapacity(8)]
    public struct GameplayWaypoint : IBufferElementData { public float2 Position; }

    public struct GameplayWorker : IComponentData
    {
        public Entity Node, Depot, Structure, Shelter;
        public WorkPhase Phase;
        public int ResourceType, Carried;
        public float Timer;
    }

    [InternalBufferCapacity(3)]
    public struct GatheringProfileData : IBufferElementData
    {
        public int ResourceType, Amount, Capacity;
        public float TickTime;
        public GameplayState State;
        public GameplayTask Task;
    }

    public struct GameplayResource : IComponentData
    {
        public float2 Position;
        public float Radius;
        public int Type, Claims, Capacity, ExternalId;
        public byte RequiresKill;
    }

    public struct GameplayDepot : IComponentData { public int ResourceMask; public byte TownHall; }

    public struct GameplayConstruction : IComponentData
    {
        public Entity Worker;
        public float Duration, Elapsed;
        public int RequestId;
        public byte Active, Middle, Finished;
    }

    public struct GameplayProducer : IComponentData { public float Elapsed; public int CurrentRequest; }

    [InternalBufferCapacity(4)]
    public struct TrainingRequest : IBufferElementData { public int RequestId; public float Duration; }

    public struct PlayerWallet : IComponentData
    {
        public int Wood, Gold, Meat;
        public int Get(int type) => type == 0 ? Wood : type == 1 ? Gold : Meat;
        public void Add(int type, int amount)
        {
            if (type == 0) Wood += amount;
            else if (type == 1) Gold += amount;
            else Meat += amount;
        }
    }

    public enum GameplayEventKind : byte { Deposit, TrainComplete, ConstructionMiddle, ConstructionComplete }
    public struct GameplayEvent
    {
        public GameplayEventKind Kind;
        public Entity Entity;
        public int RequestId, Amount, ResourceType;
    }
}
