using System;
using System.Linq;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace RTS.DOTS.Tests
{
    public class SimulationSnapshotTests
    {
        World m_World;
        GameplaySimulationSystem m_Gameplay;
        SwarmSimulationSystem m_Combat;
        Entity m_Actor;
        [SetUp] public void Setup()
        {
            m_World = new World("Snapshot fence tests");
            m_Combat = m_World.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            m_Gameplay = m_World.GetOrCreateSystemManaged<GameplaySimulationSystem>();
            var grid = new byte[1024]; Array.Fill(grid, (byte)1);
            m_Combat.ConfigureGrid(0, new int2(32), 1, grid); m_Gameplay.ConfigureGrid(0, new int2(32), 1, grid);
            m_Gameplay.ConfigureMatch(5);
            m_Actor = m_Combat.CreateActorBody(42, new float2(5), .2f, 100, 0);
            m_Gameplay.AttachActor(m_Actor, new GameplayActor { Kind = GameplayKind.Worker, Speed = 4, Interval = 1 });
            m_Gameplay.AttachWorker(m_Actor, new[] { new GatheringProfileData { Amount = 2, Capacity = 10, TickTime = 1 } });
            m_Gameplay.AddResources(100, 90, 80);
        }
        [TearDown] public void Teardown() { m_World.Dispose(); }
        [Test] public void FenceBlocksBothTicksAndCommandsAndReleasesAfterFailure()
        {
            Assert.Throws<InvalidOperationException>(() => m_Combat.CaptureSnapshot());
            try
            {
                using var lease = m_Combat.AcquireSnapshotFence();
                m_World.SetTime(new TimeData(1, .02f)); m_Gameplay.Update(); m_Combat.Update();
                Assert.That(m_Gameplay.TickCount, Is.Zero); Assert.That(m_Combat.TickCount, Is.Zero);
                Assert.Throws<InvalidOperationException>(() => m_Gameplay.AddResources(1, 0, 0));
                Assert.Throws<InvalidOperationException>(() => m_Gameplay.SetDestination(m_Actor, new float2(7)));
                Assert.Throws<InvalidOperationException>(() => m_Combat.SpawnEnemy(new float2(9), EnemySpawnSettings.Default));
                Assert.Throws<InvalidOperationException>(() => m_Combat.QueueDamage(m_Actor, 10));
                Assert.Throws<InvalidOperationException>(() => m_Gameplay.ResetGameplay());
                Assert.Throws<InvalidOperationException>(() => m_Combat.AcquireSnapshotFence());
                Assert.That(m_Combat.CaptureSnapshot().Wallet.Wood, Is.EqualTo(100));
                throw new ApplicationException("Injected conversion failure");
            }
            catch (ApplicationException) { }
            Assert.That(m_Combat.SnapshotFenced, Is.False);
            m_Gameplay.AddResources(1, 0, 0);
            Assert.That(m_Gameplay.Wallet.Wood, Is.EqualTo(101));
        }
        [Test] public void PendingDamageCommitsExactlyOnceWithoutAdvancingTimers()
        {
            m_Combat.QueueDamage(m_Actor, 13);
            Assert.Throws<InvalidOperationException>(() => m_Combat.AcquireSnapshotFence());
            m_Gameplay.PrepareSnapshotBoundary(); m_Combat.PrepareSnapshotBoundary();
            using (m_Combat.AcquireSnapshotFence())
            {
                var snapshot = m_Combat.CaptureSnapshot();
                Assert.That(snapshot.Objects.Single().Body.Value.Health, Is.EqualTo(87));
                Assert.That(snapshot.TotalHits, Is.EqualTo(1)); Assert.That(snapshot.GameplayTicks, Is.Zero); Assert.That(snapshot.SwarmTicks, Is.Zero);
            }
            m_Gameplay.PrepareSnapshotBoundary(); m_Combat.PrepareSnapshotBoundary();
            using (m_Combat.AcquireSnapshotFence())
            {
                var snapshot = m_Combat.CaptureSnapshot();
                Assert.That(snapshot.Objects.Single().Body.Value.Health, Is.EqualTo(87)); Assert.That(snapshot.TotalHits, Is.EqualTo(1));
            }
        }
        [Test] public void CompletionMustBeAppliedBeforeCaptureAndIsNotClearedByFence()
        {
            m_Gameplay.AttachBuilding(m_Actor, 7, true, true);
            Assert.That(m_Gameplay.EnqueueTraining(m_Actor, 10, .01f), Is.True);
            m_World.SetTime(new TimeData(1, .02f)); m_Gameplay.Update();
            Assert.That(m_Gameplay.PendingSnapshotEvents, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => m_Combat.AcquireSnapshotFence());
            Assert.That(m_Gameplay.TryTakeEvent(out var evt), Is.True);
            Assert.That(evt.Kind, Is.EqualTo(GameplayEventKind.TrainComplete));
            using (m_Combat.AcquireSnapshotFence()) Assert.That(m_Combat.CaptureSnapshot().Objects.Single().Training, Is.Empty);
            Assert.That(m_Gameplay.TryTakeEvent(out _), Is.False);
        }
        [Test] public void BuffersAndBodyCopiesAreDetachedAndDoNotExposeColliderPointers()
        {
            SimulationSnapshot snapshot;
            using (m_Combat.AcquireSnapshotFence()) snapshot = m_Combat.CaptureSnapshot();
            var actor = snapshot.Objects.Single();
            Assert.That(actor.Id, Is.GreaterThan(0)); Assert.That(actor.Body.Value.Collider.IsCreated, Is.False);
            Assert.That(actor.Body.Value.ExternalId, Is.Zero);
            actor.Profiles[0] = new GatheringProfileData { Amount = 999 };
            Assert.That(m_World.EntityManager.GetBuffer<GatheringProfileData>(m_Actor)[0].Amount, Is.EqualTo(2));
            m_Gameplay.AddResources(1, 0, 0);
            Assert.That(snapshot.Wallet.Wood, Is.EqualTo(100));
        }
        [Test] public void LogicalQueryIncludesNativeProjectilesEnemiesResourcesAndSheepOnce()
        {
            m_World.EntityManager.AddComponentData(m_Actor, new GameplayResource { Capacity = 1, RequiresKill = 1 });
            m_Gameplay.CreateResource(new GameplayResource { Capacity = 1 });
            var enemy = m_Combat.SpawnEnemy(new float2(10), EnemySpawnSettings.Default);
            m_Combat.SpawnProjectile(new ProjectileRequest { Position = new float2(5), Aim = new float2(10), Target = enemy, Speed = 3 });
            m_Combat.CreateProxy(0, new float2(20), 1, float.MaxValue, -1);
            using var lease = m_Combat.AcquireSnapshotFence();
            var objects = m_Combat.CaptureSnapshot().Objects;
            Assert.That(objects.Length, Is.EqualTo(4));
            Assert.That(objects.Count(x => x.Actor.HasValue && x.Resource.HasValue), Is.EqualTo(1));
            Assert.That(objects.Count(x => x.Enemy.HasValue), Is.EqualTo(1)); Assert.That(objects.Count(x => x.Projectile.HasValue), Is.EqualTo(1));
            Assert.That(objects.Select(x => x.Id), Is.Unique);
        }
        [Test] public void BuildingFootprintComesFromNativeColliderWithoutViewBounds()
        {
            var building = m_Combat.CreateProxy(99, new float2(12), new float2(1.5f, .75f), 100);
            m_Gameplay.AttachActor(building, new GameplayActor { Kind = GameplayKind.Building, Interval = 1 });
            using var lease = m_Combat.AcquireSnapshotFence();
            var saved = m_Combat.CaptureSnapshot().Objects.Single(x => x.Entity == building);
            Assert.That(saved.BoxHalfSize, Is.EqualTo(new float2(1.5f, .75f)));
            Assert.That(saved.Body.Value.Collider.IsCreated, Is.False);
        }
    }
}
