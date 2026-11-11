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

        void ConfigureResearch(float duration = 0.05f)
            => m_Gameplay.ConfigureResearch(new ResearchDefinition { Wood = 40, Gold = 15, Duration = duration, WoodMultiplier = 1.25f });

        ResearchDefinition[] ResearchCatalog() => new[] {
            new ResearchDefinition { Id="economy_cargo",Prerequisite="economy_tools",Wood=80,Gold=30,Duration=0.05f,WoodMultiplier=1 },
            new ResearchDefinition { Id="economy_tools",Wood=40,Gold=15,Duration=0.05f,WoodMultiplier=1.25f }
        };
        [Test] public void ResearchCatalogPrerequisitesRejectWithoutDebitAndUnlockInAnyCatalogOrder()
        {
            var catalog=ResearchCatalog(); m_Gameplay.ConfigureResearchCatalog(catalog);
            catalog[0].Wood=9999; // Configuration is copied at the trust boundary.
            var hall=Building(new float2(15));m_Gameplay.AddResources(200,200,0);
            Assert.That(m_Gameplay.StartResearch("economy_cargo",hall),Is.EqualTo(ResearchResult.MissingPrerequisite));
            Assert.That(m_Gameplay.Wallet.Wood,Is.EqualTo(200));
            m_Gameplay.StartResearch("economy_tools",hall);Tick(10);
            Assert.That(m_Gameplay.IsTechnologyUnlocked("economy_tools"),Is.True);
            Assert.That(m_Gameplay.StartResearch("economy_cargo",hall),Is.EqualTo(ResearchResult.Started));
            Assert.That(m_Gameplay.Wallet.Wood,Is.EqualTo(80));Tick(10);
            Assert.That(m_Gameplay.IsTechnologyUnlocked("economy_cargo"),Is.True);
            Assert.That(m_Gameplay.Research.Unlocked,Is.EqualTo(1));
        }
        [Test] public void CancelledChildResearchPreservesPreviouslyUnlockedParent()
        {
            m_Gameplay.ConfigureResearchCatalog(ResearchCatalog());var hall=Building(new float2(15));m_Gameplay.AddResources(200,200,0);
            m_Gameplay.StartResearch("economy_tools",hall);Tick(10);m_Gameplay.StartResearch("economy_cargo",hall);
            m_Gameplay.Remove(hall);Tick();
            Assert.That(m_Gameplay.IsTechnologyUnlocked("economy_tools"),Is.True);
            Assert.That(m_Gameplay.IsTechnologyUnlocked("economy_cargo"),Is.False);
            Assert.That(m_Gameplay.Research.Active,Is.Zero);
        }
        [TestCase("duplicate")] [TestCase("missing")] [TestCase("cycle")] [TestCase("unsupported")]
        public void InvalidResearchGraphIsRejectedBeforeConfiguration(string kind)
        {
            var catalog=ResearchCatalog();
            if(kind=="duplicate") catalog[0].Id="economy_tools";
            if(kind=="missing") catalog[0].Prerequisite="missing-id";
            if(kind=="cycle") catalog[1].Prerequisite="economy_cargo";
            if(kind=="unsupported") catalog[0].Id="unsupported-effect";
            Assert.Throws<System.ArgumentException>(()=>m_Gameplay.ConfigureResearchCatalog(catalog));
            ConfigureResearch(); // Failed validation must leave the system unconfigured.
        }
        [Test] public void AllSixPlannedTechnologyIdsCanBeValidatedAndResearched()
        {
            var ids=new[]{"economy_tools","economy_cargo","military_archery","military_training","expansion_tower","expansion_fortify"};
            var catalog=new ResearchDefinition[6];
            for(int i=0;i<6;i++)catalog[i]=new ResearchDefinition {Id=ids[i],Prerequisite=i%2==1?ids[i-1]:null,Duration=0.01f,WoodMultiplier=1};
            m_Gameplay.ConfigureResearchCatalog(catalog);var hall=Building(new float2(15));
            foreach(var id in ids){Assert.That(m_Gameplay.StartResearch(id,hall),Is.EqualTo(ResearchResult.Started));Tick(2);}
            Assert.That(m_Gameplay.Research.UnlockMask,Is.EqualTo(63u));
        }
        Entity ConfigureAllEffects()
        {
            var ids=new[]{"economy_tools","economy_cargo","military_archery","military_training","expansion_tower","expansion_fortify"};
            var definitions=new ResearchDefinition[6];
            for(int i=0;i<6;i++)definitions[i]=new ResearchDefinition {Id=ids[i],Prerequisite=i%2==1?ids[i-1]:null,Duration=0.01f,WoodMultiplier=1};
            definitions[1].CargoMultiplier=1.5f;definitions[3].TrainingMultiplier=0.8f;definitions[5].TowerMultiplier=1.25f;
            m_Gameplay.ConfigureResearchCatalog(definitions);return Building(new float2(20));
        }
        void Unlock(string id,Entity hall)
        {Assert.That(m_Gameplay.StartResearch(id,hall),Is.EqualTo(ResearchResult.Started));Tick(2);}
        [Test] public void CargoUpgradeUsesEffectiveCapacityForExistingAndNewWorkers()
        {
            var hall=ConfigureAllEffects();var old=Actor(GameplayKind.Worker,new float2(4),0);
            Unlock("economy_tools",hall);Unlock("economy_cargo",hall);var added=Actor(GameplayKind.Worker,new float2(8),0);
            foreach(var worker in new[]{old,added})m_Gameplay.AssignResource(worker,Resource(EM.GetComponentData<CombatBody>(worker).Position));
            Tick(12);
            foreach(var worker in new[]{old,added})
            {Assert.That(EM.GetComponentData<GameplayWorker>(worker).Carried,Is.EqualTo(6));Assert.That(EM.GetBuffer<GatheringProfileData>(worker)[0].Capacity,Is.EqualTo(4));}
        }
        [Test] public void TrainingUpgradeSnapshotsNewRequestsWithoutChangingOldQueue()
        {
            var hall=ConfigureAllEffects();var producer=Building(new float2(15),true);
            m_Gameplay.EnqueueTraining(producer,1,10);Unlock("military_archery",hall);Unlock("military_training",hall);
            m_Gameplay.EnqueueTraining(producer,2,10);var queue=EM.GetBuffer<TrainingRequest>(producer);
            Assert.That(queue[0].Duration,Is.EqualTo(10));Assert.That(queue[1].Duration,Is.EqualTo(8));
        }
        [Test] public void FortifyAffectsOldAndNewTowerShotsWithoutMutatingBaseDamage()
        {
            var hall=ConfigureAllEffects();var old=Actor(GameplayKind.Tower,new float2(4),0);
            Unlock("expansion_tower",hall);Unlock("expansion_fortify",hall);var added=Actor(GameplayKind.Tower,new float2(8),0);
            var a=Enemy(new float2(4.8f,4));var b=Enemy(new float2(8.8f,8));m_Gameplay.Attack(old,a);m_Gameplay.Attack(added,b);Tick(12);
            Assert.That(EM.GetComponentData<CombatBody>(a).Health,Is.EqualTo(87));Assert.That(EM.GetComponentData<CombatBody>(b).Health,Is.EqualTo(87));
            Assert.That(EM.GetComponentData<GameplayActor>(old).Damage,Is.EqualTo(10));
        }
        [Test] public void CapabilityChecksProtectDirectConstructionAndTrainingCommands()
        {
            var hall=ConfigureAllEffects();var range=Actor(GameplayKind.Building,new float2(12),0);
            m_Gameplay.AttachBuilding(range,7,false,true,1);
            Assert.That(m_Gameplay.EnqueueTraining(range,1,10),Is.False);Assert.That(m_Gameplay.BeginConstruction(range,10,1),Is.False);
            Assert.That(EM.GetBuffer<TrainingRequest>(range).Length,Is.Zero);
            Unlock("military_archery",hall);
            Assert.That(m_Gameplay.EnqueueTraining(range,1,10),Is.True);Assert.That(m_Gameplay.BeginConstruction(range,10,1),Is.True);
        }
        [Test] public void LegacyWithoutResearchKeepsCapabilitiesAvailable()
        {Assert.That(m_Gameplay.CanUseArchery,Is.True);Assert.That(m_Gameplay.CanUseTowers,Is.True);Assert.That(m_Gameplay.TrainingMultiplier,Is.EqualTo(1));}
        [TestCase(-0.1f)] [TestCase(1.1f)] [TestCase(float.NaN)]
        public void InvalidTrainingModifiersAreRejected(float value)
            => Assert.Throws<System.ArgumentException>(()=>new ResearchDefinition {Duration=1,WoodMultiplier=1,TrainingMultiplier=value}.Validate());

        [Test] public void ResearchDebitsOnceCompletesOnceAndResetClearsUnlock()
        {
            ConfigureResearch(); var hall = Building(new float2(15)); m_Gameplay.AddResources(100, 100, 100);
            Assert.That(m_Gameplay.StartResearch("bad-id", hall), Is.EqualTo(ResearchResult.InvalidTechnology));
            Assert.That(m_Gameplay.Wallet.Wood, Is.EqualTo(100));
            Assert.That(m_Gameplay.StartResearch(GameplaySimulationSystem.EconomyToolsId, hall), Is.EqualTo(ResearchResult.Started));
            Assert.That(m_Gameplay.StartResearch(GameplaySimulationSystem.EconomyToolsId, hall), Is.EqualTo(ResearchResult.Busy));
            Assert.That(m_Gameplay.Wallet.Wood, Is.EqualTo(60)); Assert.That(m_Gameplay.Wallet.Gold, Is.EqualTo(85));
            Tick(10); Assert.That(m_Gameplay.Research.Unlocked, Is.EqualTo(1));
            Assert.That(m_Gameplay.StartResearch(GameplaySimulationSystem.EconomyToolsId, hall), Is.EqualTo(ResearchResult.AlreadyUnlocked));
            Tick(10); Assert.That(m_Gameplay.Wallet.Wood, Is.EqualTo(60));
            m_Gameplay.ResetGameplay(); Assert.That(m_Gameplay.Research.Unlocked, Is.Zero);
        }
        [Test] public void ResearchRejectsPoorWalletAndUnfinishedOrEnemyHall()
        {
            ConfigureResearch(); var hall = Building(new float2(15));
            Assert.That(m_Gameplay.StartResearch(GameplaySimulationSystem.EconomyToolsId, hall), Is.EqualTo(ResearchResult.InsufficientResources));
            m_Gameplay.AddResources(100, 100, 0); m_Gameplay.BeginConstruction(hall, 10, 1);
            Assert.That(m_Gameplay.StartResearch(GameplaySimulationSystem.EconomyToolsId, hall), Is.EqualTo(ResearchResult.InvalidTownHall));
            EM.SetComponentData(hall, new GameplayConstruction()); var body=EM.GetComponentData<CombatBody>(hall); body.Faction=1; EM.SetComponentData(hall,body);
            Assert.That(m_Gameplay.StartResearch(GameplaySimulationSystem.EconomyToolsId, hall), Is.EqualTo(ResearchResult.InvalidTownHall));
            Assert.That(m_Gameplay.Wallet.Wood, Is.EqualTo(100));
        }
        [Test] public void ResearchPauseFreezesAndHallRemovalCancelsWithoutRefund()
        {
            ConfigureResearch(1); var hall=Building(new float2(15)); m_Gameplay.AddResources(100,100,0);
            m_Gameplay.StartResearch(GameplaySimulationSystem.EconomyToolsId,hall); m_Combat.Paused=true;
            Tick(10); Assert.That(m_Gameplay.Research.Elapsed,Is.Zero);
            m_Combat.Paused=false; Tick(); Assert.That(m_Gameplay.Research.Elapsed,Is.GreaterThan(0));
            m_Gameplay.Remove(hall); Tick(); Assert.That(m_Gameplay.Research.Active,Is.Zero); Assert.That(m_Gameplay.Research.Unlocked,Is.Zero);
            Assert.That(m_Gameplay.Wallet.Wood,Is.EqualTo(60));
        }
        [TestCase(-1, 1)] [TestCase(1, float.NaN)] [TestCase(0, 0)]
        public void ResearchRejectsInvalidDefinition(int wood,float duration)
            => Assert.Throws<System.ArgumentException>(()=>m_Gameplay.ConfigureResearch(new ResearchDefinition {Wood=wood,Duration=duration,WoodMultiplier=1.25f}));

        [TestCase(0, 5)] [TestCase(1, 4)] [TestCase(2, 4)]
        public void ResearchMultiplierAffectsExistingAndNewWorkersWithoutChangingProfiles(int type, int expected)
        {
            ConfigureResearch(); var hall=Building(new float2(20)); m_Gameplay.AddResources(100,100,0);
            var existing=Actor(GameplayKind.Worker,new float2(4),0);
            m_Gameplay.StartResearch(GameplaySimulationSystem.EconomyToolsId,hall); Tick(10);
            var added=Actor(GameplayKind.Worker,new float2(8),0);
            foreach(var worker in new[]{existing,added})
            {
                var p=EM.GetBuffer<GatheringProfileData>(worker); var profile=p[type]; profile.Amount=4;profile.Capacity=100;p[type]=profile;
                var body=EM.GetComponentData<CombatBody>(worker); var node=Resource(body.Position,type);
                Assert.That(m_Gameplay.AssignResource(worker,node),Is.True);
            }
            Tick(4);
            foreach(var worker in new[]{existing,added})
            {
                Assert.That(EM.GetComponentData<GameplayWorker>(worker).Carried,Is.EqualTo(expected));
                Assert.That(EM.GetBuffer<GatheringProfileData>(worker)[type].Amount,Is.EqualTo(4));
            }
            Assert.That(GameplaySimulationSystem.EffectiveGatherAmount(2,1.25f),Is.EqualTo(3));
            Assert.That(GameplaySimulationSystem.EffectiveGatherAmount(4,1),Is.EqualTo(4));
        }

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
        [TestCase(0)] [TestCase(1)]
        public void WorkersApproachResourceInteractionPerimeterAfterClaimOpensTile(int type)
        {
            var map = new byte[32 * 32]; System.Array.Fill(map, (byte)1);
            for (int y = 3; y <= 4; y++)
                for (int x = 3; x <= 4; x++) map[y * 32 + x] = 0;
            Configure(map);
            Entity worker = Actor(GameplayKind.Worker, new float2(8.5f, 4.5f));
            Entity node = Resource(new float2(4, 4), type);

            Assert.That(m_Gameplay.AssignResource(worker, node), Is.True);
            System.Array.Fill(map, (byte)1);
            Configure(map); // The bridge refreshes navigation after the claim disables the physical collider.
            Tick();
            var destination = EM.GetComponentData<GameplayActor>(worker).Destination;
            Assert.That(math.distance(destination, new float2(4, 4)), Is.EqualTo(0.3f).Within(0.001f));
            Tick(180);

            Assert.That(EM.GetComponentData<GameplayWorker>(worker).Carried, Is.GreaterThan(0));
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
