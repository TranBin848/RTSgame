using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace RTS.DOTS.Tests
{
    public class GameplaySimulationTests
    {
        World m_World;
        SwarmSimulationSystem m_Combat;
        GameplaySimulationSystem m_Gameplay;
        EntityManager EM => m_World.EntityManager;
        double m_Time;
        [SetUp] public void Setup()
        {
            m_World = new World("Gameplay ECS tests");
            m_Combat = m_World.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            m_Gameplay = m_World.GetOrCreateSystemManaged<GameplaySimulationSystem>();
            var map = new byte[32 * 32]; System.Array.Fill(map, (byte)1);
            Configure(map); m_Time = 0;
        }
        void Configure(byte[] map)
        { m_Combat.ConfigureGrid(0, new int2(32), 1, map); m_Gameplay.ConfigureGrid(0, new int2(32), 1, map); }
        [TearDown] public void Teardown() { m_World.Dispose(); }
        void Tick(int count = 1)
        {
            for (int i = 0; i < count; i++)
            { m_Time += 1.0 / 60; m_World.SetTime(new TimeData(m_Time, 1f / 60)); m_Gameplay.Update(); m_Combat.Update(); }
        }
        Entity Actor(GameplayKind kind, float2 position, float speed = 4)
        {
            Entity entity = m_Combat.CreateActorBody(100 + kind.GetHashCode(), position, 0.2f, 100, kind == GameplayKind.Sheep ? 2 : 0);
            m_Gameplay.AttachActor(entity, new GameplayActor { Kind = kind, Speed = speed, Range = 1, Detection = 6,
                Interval = 0.25f, Delay = 0.05f, Damage = 10, Offensive = 1,
                MinIdle = 0.05f, MaxIdle = 0.1f, MinGraze = 0.05f, MaxGraze = 0.1f, GrazeTimer = 0.05f,
                FleeDistance = 2, Projectile = new ProjectileParameters { Speed = 20, VisualScale = 1 } });
            if (kind == GameplayKind.Worker)
                m_Gameplay.AttachWorker(entity, new[] {
                    new GatheringProfileData { ResourceType = 0, Amount = 2, Capacity = 4, TickTime = 0.05f, State = GameplayState.Chopping, Task = GameplayTask.Chop },
                    new GatheringProfileData { ResourceType = 1, Amount = 2, Capacity = 4, TickTime = 0.05f, State = GameplayState.Mining, Task = GameplayTask.Mine },
                    new GatheringProfileData { ResourceType = 2, Amount = 2, Capacity = 4, TickTime = 0.05f, State = GameplayState.Collecting, Task = GameplayTask.Collect }
                });
            return entity;
        }
        Entity Building(float2 position, bool production = false)
        { Entity e = Actor(GameplayKind.Building, position, 0); m_Gameplay.AttachBuilding(e, 7, true, production); return e; }
        Entity Resource(float2 position, int type = 0, int capacity = 4)
            => m_Gameplay.CreateResource(new GameplayResource { Position = position, Radius = 0.3f, Type = type, Capacity = capacity });
        Entity Enemy(float2 position)
        { var settings = EnemySpawnSettings.Default; settings.Speed = 0; settings.Damage = 0; return m_Combat.SpawnEnemy(position, settings); }

        [Test] public void AllRolesUseTheSameAuthoritativeHealthWithoutExternalDamageEcho()
        {
            foreach (GameplayKind kind in System.Enum.GetValues(typeof(GameplayKind)))
            {
                Entity e = Actor(kind, new float2(2 + (int)kind * 3, 2), 0);
                m_Combat.QueueDamage(e, 13);
            }
            Tick();
            using var query = EM.CreateEntityQuery(typeof(GameplayActor));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var e in entities) Assert.That(EM.GetComponentData<CombatBody>(e).Health, Is.EqualTo(87));
            Assert.That(m_Combat.TryTakeExternalDamage(out _), Is.False);
            Tick(); foreach (var e in entities) Assert.That(EM.GetComponentData<CombatBody>(e).Health, Is.EqualTo(87));
        }
        [Test] public void PlayerNavigationRoutesAroundWallAndStops()
        {
            var map = new byte[1024]; System.Array.Fill(map, (byte)1);
            for (int y = 0; y < 10; y++) map[y * 32 + 8] = 0;
            Configure(map); Entity soldier = Actor(GameplayKind.Soldier, new float2(4.5f, 4.5f), 8);
            m_Gameplay.SetDestination(soldier, new float2(12.5f, 4.5f), true);
            Tick(240);
            Assert.That(math.distance(EM.GetComponentData<CombatBody>(soldier).Position, new float2(12.5f, 4.5f)), Is.LessThan(0.2f));
            Assert.That(EM.GetComponentData<GameplayActor>(soldier).Moving, Is.Zero);
        }
        [Test] public void SoldiersAcquireAndAttackAnEntityEnemy()
        {
            Entity soldier = Actor(GameplayKind.Soldier, new float2(4, 4)); Entity enemy = Enemy(new float2(4.8f, 4));
            Tick(12); Assert.That(EM.GetComponentData<GameplayActor>(soldier).Target, Is.EqualTo(enemy));
            Assert.That(EM.GetComponentData<CombatBody>(enemy).Health, Is.LessThan(100));
        }
        [Test] public void DefensiveStanceDoesNotChaseOutOfRangeTarget()
        {
            Entity soldier = Actor(GameplayKind.Soldier, new float2(4, 4)); Entity enemy = Enemy(new float2(7, 4));
            m_Gameplay.SetStance(soldier, false); m_Gameplay.Attack(soldier, enemy); Tick(45);
            Assert.That(EM.GetComponentData<CombatBody>(soldier).Position.x, Is.EqualTo(4));
            Assert.That(EM.GetComponentData<CombatBody>(enemy).Health, Is.EqualTo(100));
        }
        [TestCase(GameplayKind.Archer)] [TestCase(GameplayKind.Tower)]
        public void RangedPlayerActorsLaunchEcsProjectiles(GameplayKind kind)
        {
            Entity actor = Actor(kind, new float2(4, 4), 0); Entity enemy = Enemy(new float2(5, 4));
            m_Gameplay.Attack(actor, enemy); Tick(12);
            Assert.That(EM.GetComponentData<GameplayActor>(actor).AttackSequence, Is.GreaterThan(0));
            Assert.That(EM.GetComponentData<CombatBody>(enemy).Health, Is.LessThan(100));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void WorkersGatherAndDepositToNativeWallet(int type)
        {
            Entity worker = Actor(GameplayKind.Worker, new float2(4, 4));
            Building(new float2(4.2f, 4)); Entity node = Resource(new float2(4, 4.1f), type);
            Assert.That(m_Gameplay.AssignResource(worker, node), Is.True); Tick(30);
            Assert.That(m_Gameplay.Wallet.Get(type), Is.GreaterThanOrEqualTo(4));
            Assert.That(m_Gameplay.Wallet.Get((type + 1) % 3), Is.Zero);
        }
        [Test] public void SimultaneousDepositsKeepAllTransactions()
        {
            Building(new float2(4, 4)); Entity node = Resource(new float2(4.1f, 4));
            for (int i = 0; i < 3; i++) m_Gameplay.AssignResource(Actor(GameplayKind.Worker, new float2(4, 4)), node);
            Tick(8); Assert.That(m_Gameplay.Wallet.Wood, Is.EqualTo(12));
        }
        [Test] public void ClaimCapacityCancellationAndWorkerDeathReleaseSlots()
        {
            Entity node = Resource(new float2(4), capacity: 1);
            Entity first = Actor(GameplayKind.Worker, new float2(4)); Entity second = Actor(GameplayKind.Worker, new float2(4));
            Assert.That(m_Gameplay.AssignResource(first, node), Is.True);
            Assert.That(m_Gameplay.AssignResource(second, node), Is.False);
            m_Gameplay.SetDestination(first, new float2(8), true);
            Assert.That(m_Gameplay.AssignResource(second, node), Is.True);
            m_Combat.QueueDamage(second, 1000); Tick(2);
            Assert.That(EM.GetComponentData<GameplayResource>(node).Claims, Is.Zero);
        }
        [Test] public void ConstructionPausesWithoutBuilderAndCompletesOnceAfterReassignment()
        {
            Entity building = Building(new float2(4)); m_Gameplay.BeginConstruction(building, 0.3f, 11);
            Entity worker = Actor(GameplayKind.Worker, new float2(4.5f, 4)); m_Gameplay.AssignConstruction(worker, building);
            Tick(5); float progress = EM.GetComponentData<GameplayConstruction>(building).Elapsed;
            m_Gameplay.CancelWork(worker); Tick(10);
            Assert.That(EM.GetComponentData<GameplayConstruction>(building).Elapsed, Is.EqualTo(progress));
            m_Gameplay.AssignConstruction(worker, building); Tick(30);
            Assert.That(EM.GetComponentData<GameplayConstruction>(building).Finished, Is.EqualTo(1));
            int completed = 0; while (m_Gameplay.TryTakeEvent(out var evt)) if (evt.Kind == GameplayEventKind.ConstructionComplete) completed++;
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(EM.GetComponentData<GameplayWorker>(worker).Phase, Is.EqualTo(WorkPhase.None));
        }
        [Test] public void TrainingQueueIsOrderedAndBlockedDuringConstruction()
        {
            Entity building = Building(new float2(4), true); m_Gameplay.BeginConstruction(building, 1, 7);
            m_Gameplay.EnqueueTraining(building, 21, 0.05f); m_Gameplay.EnqueueTraining(building, 22, 0.05f);
            Tick(10); Assert.That(EM.GetBuffer<TrainingRequest>(building).Length, Is.EqualTo(2));
            var build = EM.GetComponentData<GameplayConstruction>(building); build.Active = 0; EM.SetComponentData(building, build);
            Tick(8); var ids = new System.Collections.Generic.List<int>();
            while (m_Gameplay.TryTakeEvent(out var evt)) if (evt.Kind == GameplayEventKind.TrainComplete) ids.Add(evt.RequestId);
            CollectionAssert.AreEqual(new[] { 21, 22 }, ids);
        }
        [Test] public void SheepGrazesAndFleesOnNativeDamage()
        {
            Entity sheep = Actor(GameplayKind.Sheep, new float2(8, 8)); Entity hunter = Actor(GameplayKind.Worker, new float2(7, 8), 0);
            Tick(4); Assert.That(EM.GetComponentData<GameplayActor>(sheep).Grazing, Is.EqualTo(1));
            m_Combat.QueueDamage(sheep, 1, hunter); Tick(60);
            Assert.That(EM.GetComponentData<CombatBody>(sheep).Position.x, Is.GreaterThan(9));
        }
        [Test] public void HuntingSheepKeepsCorpseAsMeatResource()
        {
            Entity sheep = Actor(GameplayKind.Sheep, new float2(4), 0);
            EM.AddComponentData(sheep, new GameplayResource { Type = 2, Radius = 0.3f, Capacity = 1, RequiresKill = 1 });
            Entity worker = Actor(GameplayKind.Worker, new float2(4.5f, 4)); Building(new float2(4.2f, 4));
            m_Gameplay.AssignResource(worker, sheep); m_Combat.QueueDamage(sheep, 1000, worker); Tick(30);
            Assert.That(EM.Exists(sheep), Is.True); Assert.That(m_Gameplay.Wallet.Meat, Is.GreaterThan(0));
        }
        [Test] public void ShelterPreventsDamageAndDaybreakRestoresMovement()
        {
            Entity hall = Building(new float2(4)); Entity worker = Actor(GameplayKind.Worker, new float2(4.4f, 4));
            m_Gameplay.Shelter(worker, hall); Tick(2);
            Assert.That(EM.GetComponentData<GameplayWorker>(worker).Phase, Is.EqualTo(WorkPhase.Sheltered));
            m_Combat.QueueDamage(worker, 50); Tick(); Assert.That(EM.GetComponentData<CombatBody>(worker).Health, Is.EqualTo(100));
            m_Gameplay.LeaveShelter(worker, new float2(6.5f, 4.5f), new float2(8.5f, 4.5f)); Tick(60);
            Assert.That(EM.GetComponentData<CombatBody>(worker).Targetable, Is.EqualTo(1));
            Assert.That(EM.GetComponentData<CombatBody>(worker).Position.x, Is.GreaterThan(8));
        }
        [Test] public void RemovedResourceInvalidatesWorkerAssignmentWithoutStaleEntityAccess()
        {
            Entity node = Resource(new float2(4)); Entity worker = Actor(GameplayKind.Worker, new float2(4));
            m_Gameplay.AssignResource(worker, node); m_Gameplay.Remove(node); Tick(3);
            Assert.That(EM.GetComponentData<GameplayWorker>(worker).Node, Is.EqualTo(Entity.Null));
        }
        [Test] public void SwitchingResourceTypeDepositsOldCargoWithoutRelabellingIt()
        {
            Entity worker = Actor(GameplayKind.Worker, new float2(4));
            Entity wood = Resource(new float2(4), 0); Entity gold = Resource(new float2(4), 1);
            m_Gameplay.AssignResource(worker, wood); Tick(4);
            int carried = EM.GetComponentData<GameplayWorker>(worker).Carried;
            Assert.That(carried, Is.GreaterThan(0));
            m_Gameplay.AssignResource(worker, gold); Building(new float2(4.2f, 4)); Tick();
            Assert.That(m_Gameplay.Wallet.Wood, Is.EqualTo(carried));
            Assert.That(m_Gameplay.Wallet.Gold, Is.Zero);
            Assert.That(EM.GetComponentData<GameplayWorker>(worker).ResourceType, Is.EqualTo(1));
        }
        [Test] public void DestroyedShelterEjectsWorkerOntoWalkableGrid()
        {
            Entity hall = Building(new float2(4.5f)); Entity worker = Actor(GameplayKind.Worker, new float2(4.8f, 4.5f));
            m_Gameplay.Shelter(worker, hall); Tick(2);
            var map = new byte[1024]; System.Array.Fill(map, (byte)1); map[4 * 32 + 4] = 0;
            Configure(map); m_Gameplay.Remove(hall); Tick();
            var body = EM.GetComponentData<CombatBody>(worker);
            Assert.That(body.Targetable, Is.EqualTo(1));
            Assert.That(math.all((int2)math.floor(body.Position) == new int2(4)), Is.False);
            Assert.That(EM.GetComponentData<GameplayWorker>(worker).Phase, Is.EqualTo(WorkPhase.None));
        }
    }
}
