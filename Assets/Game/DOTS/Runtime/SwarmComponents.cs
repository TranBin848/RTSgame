using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace RTS.DOTS
{
    // Simulation coordinates are XY. Visual Z and ballistic height never enter collision queries.
    public struct CombatBody : IComponentData
    {
        public float2 Position, PreviousPosition;
        public float Radius, Health;
        public int Faction, ExternalId;
        public byte Targetable;
        public BlobAssetReference<Collider> Collider;
    }

    public enum SwarmState : byte { Moving, Attacking, Dead, Retreating }
    public struct ProjectileParameters
    {
        public float Speed, ArcHeight, VisualScale, MinDuration, MaxDuration, DurationFactor;
        public byte Arc, Spinning;
    }

    public struct SwarmAgent : IComponentData
    {
        public Entity Target;
        public float2 Velocity, RetreatGoal;
        public float Speed, AttackRange, DetectionRange, AttackInterval, AttackDelay;
        public float Cooldown, Windup, ThinkTimer, StateTimer, VisualScale;
        public int Damage, VisualId, ProjectileVisualId;
        public byte Ranged;
        public ProjectileParameters Projectile;
        public SwarmState State;
    }

    public struct SwarmProjectile : IComponentData
    {
        public Entity Owner, Target;
        public float2 Position, Start, Aim, PreviousPosition;
        public float Speed, Radius, Lifetime, Age, Duration, ArcHeight, VisualScale;
        public int Damage, Faction, VisualId;
        public byte Arc, Consumed, Spinning;
    }

    public struct TargetSnapshot
    {
        public Entity Entity;
        public CombatBody Body;
    }

    public struct DamageEvent
    {
        public Entity Target, Source;
        public int Amount;
    }

    public struct ProjectileRequest
    {
        public Entity Owner, Target;
        public float2 Position, Aim;
        public int Damage, Faction, VisualId;
        public float Speed, ArcHeight, VisualScale;
        public byte Arc;
        public byte Spinning;
        public float MinDuration, MaxDuration, DurationFactor;
    }

    public struct SwarmRenderData
    {
        public Entity Entity;
        public float2 Position, Direction;
        public float Height, Age, Health, VisualScale;
        public int VisualId, Faction;
        public SwarmState State;
        public byte Projectile;
        public byte Spinning;
    }

    public struct EnemySpawnSettings
    {
        public float Speed, Radius, Health, AttackRange, DetectionRange, AttackInterval, AttackDelay, VisualScale;
        public int Damage, VisualId, ProjectileVisualId;
        public bool Ranged;
        public ProjectileParameters Projectile;
        public static EnemySpawnSettings Default => new EnemySpawnSettings
        {
            Speed = 3f, Radius = 0.25f, Health = 100, AttackRange = 0.65f,
            DetectionRange = 5, AttackInterval = 1.5f, AttackDelay = 0.4f, Damage = 10, VisualScale = 1,
            Projectile = new ProjectileParameters { Speed = 10, ArcHeight = 0.8f, VisualScale = 1, Arc = 1 }
        };
    }
}
