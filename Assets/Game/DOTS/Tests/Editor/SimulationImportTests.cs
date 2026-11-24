using System;
using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace RTS.DOTS.Tests
{
    public class SimulationImportTests
    {
        [Test] public void ImportPreservesValuesReferencesAndAllocatorWithoutSpawnClamping()
        {
            using var world = new World("Import state tests");
            var combat = world.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            var gameplay = world.GetOrCreateSystemManaged<GameplaySimulationSystem>();
            var grid = new byte[1024]; Array.Fill(grid, (byte)1);
            combat.ConfigureGrid(0, new int2(32), 1, grid); gameplay.ConfigureGrid(0, new int2(32), 1, grid);
            Assert.Throws<InvalidOperationException>(() => combat.CreateImportShell());
            combat.Paused = true;
            var enemy = combat.CreateImportShell(); var actor = combat.CreateImportShell();
            combat.ApplyImportedObject(enemy, new SimulationObjectSnapshot { Id = 77,
                Body = new CombatBody { Position = new float2(8), PreviousPosition = new float2(7.9f), Radius = .2f, Health = 73, Faction = 1, Targetable = 1 },
                Enemy = new SwarmAgent { Target = actor, RandomSeed = 902, Windup = .17f, Cooldown = .6f, VisualScale = 1 } }, 0);
            combat.ApplyImportedObject(actor, new SimulationObjectSnapshot { Id = 8, BoxHalfSize = new float2(1, .5f),
                Body = new CombatBody { Position = new float2(5), Radius = 1.1f, Health = 1200, Targetable = 1 },
                Actor = new GameplayActor { Kind = GameplayKind.Building, Target = enemy, RandomSeed = 155, Windup = .21f } }, 99);
            gameplay.ApplyImportedPlayer(new PlayerWallet { Wood = 137, Gold = 89, Meat = 123 }, new PlayerResearch(),
                new MatchStatistics { Granted = new ResourceTotals { Wood = 137 } }, 35);
            combat.FinishSnapshotImport(100, 40, 3, 1);
            using (combat.AcquireSnapshotFence())
            {
                var snapshot = combat.CaptureSnapshot(); var saved = snapshot.Objects.Single(x => x.Id == 77);
                Assert.That(saved.Enemy.Value.Target, Is.EqualTo(actor)); Assert.That(saved.Enemy.Value.Windup, Is.EqualTo(.17f));
                Assert.That(saved.Body.Value.Health, Is.EqualTo(73)); Assert.That(saved.Body.Value.PreviousPosition, Is.EqualTo(new float2(7.9f)));
                Assert.That(snapshot.Objects.Single(x => x.Id == 8).Actor.Value.Target, Is.EqualTo(enemy));
                Assert.That(snapshot.Wallet.Wood, Is.EqualTo(137)); Assert.That(snapshot.GameplayTicks, Is.EqualTo(35)); Assert.That(snapshot.SwarmTicks, Is.EqualTo(40));
            }
            var spawned = combat.CreateActorBody(2, new float2(12), .2f, 100, 0);
            Assert.That(world.EntityManager.GetComponentData<SaveIdentity>(spawned).Value, Is.EqualTo(100));
            Assert.That(combat.RenderData.Length, Is.EqualTo(1));
        }
        [Test] public void FinishRejectsDuplicateIdentityAndObstacleCacheDoesNotConsumeIds()
        {
            using var world = new World("Import identity tests");
            var combat = world.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            var grid = new byte[1024]; Array.Fill(grid, (byte)1); combat.ConfigureGrid(0, new int2(32), 1, grid);
            long next = combat.NextSaveId;
            var obstacle = combat.CreateProxy(0, new float2(10), new float2(1), float.MaxValue, -1);
            Assert.That(world.EntityManager.HasComponent<SaveIdentity>(obstacle), Is.False); Assert.That(combat.NextSaveId, Is.EqualTo(next));
            combat.Paused = true;
            foreach (int i in new[] { 1, 2 }) combat.ApplyImportedObject(combat.CreateImportShell(), new SimulationObjectSnapshot { Id = 8,
                Resource = new GameplayResource { Position = new float2(i), Capacity = 1 } }, 0);
            Assert.Throws<ArgumentException>(() => combat.FinishSnapshotImport(100, 0, 0, 0));
        }
    }
}
