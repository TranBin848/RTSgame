using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace RTS.DOTS.Tests
{
    public class SaveIdentityTests
    {
        [Test] public void AllocatorPreservesLongRangeAndRejectsOverflow()
        {
            var ids = new SaveIdAllocator(9007199254740993L);
            Assert.That(ids.Allocate(), Is.EqualTo(9007199254740993L));
            Assert.That(ids.Next, Is.EqualTo(9007199254740994L));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SaveIdAllocator(0));
            Assert.Throws<System.InvalidOperationException>(() => new SaveIdAllocator(long.MaxValue - 1).Allocate());
        }
        [Test] public void AllFactoriesShareIdentityAndNeverReuseAfterReset()
        {
            using var world = new World("Save identity tests");
            var swarm = world.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            var gameplay = world.GetOrCreateSystemManaged<GameplaySimulationSystem>();
            var grid = new byte[32 * 32]; System.Array.Fill(grid, (byte)1);
            swarm.ConfigureGrid(0, new int2(32), 1, grid); gameplay.ConfigureGrid(0, new int2(32), 1, grid);
            var body = swarm.CreateActorBody(7, new float2(5), .2f, 100, 0);
            long Id(Entity e) => world.EntityManager.GetComponentData<SaveIdentity>(e).Value;
            long first = Id(body);
            gameplay.AttachActor(body, new GameplayActor { Kind = GameplayKind.Sheep });
            world.EntityManager.AddComponentData(body, new GameplayResource { Type = 2, RequiresKill = 1, Capacity = 1 });
            Assert.That(Id(body), Is.EqualTo(first), "Sheep actor/resource must share one identity.");
            Assert.That(world.EntityManager.GetComponentData<GameplayActor>(body).RandomSeed, Is.EqualTo(body.Index));
            var resource = gameplay.CreateResource(new GameplayResource { Capacity = 1 });
            var enemy = swarm.SpawnEnemy(new float2(7), EnemySpawnSettings.Default);
            Assert.That(world.EntityManager.GetComponentData<SwarmAgent>(enemy).RandomSeed, Is.EqualTo(enemy.Index));
            var projectile = swarm.SpawnProjectile(new ProjectileRequest { Position = new float2(6), Aim = new float2(7), Speed = 10 });
            var proxy = swarm.CreateProxy(9, new float2(8), new float2(.5f), 100);
            Assert.That(new[] { first, Id(resource), Id(enemy), Id(projectile), Id(proxy) }, Is.Unique);
            long next = swarm.NextSaveId;
            swarm.ResetSimulation(); gameplay.ResetGameplay();
            Assert.That(Id(swarm.CreateActorBody(7, new float2(5), .2f, 100, 0)), Is.GreaterThanOrEqualTo(next));
        }
        [Test] public void SheepRandomScheduleUsesSavedSeedAfterEntityRemap()
        {
            using var world = new World("Saved sheep seed test");
            var swarm = world.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            var gameplay = world.GetOrCreateSystemManaged<GameplaySimulationSystem>();
            var grid = new byte[32 * 32]; System.Array.Fill(grid, (byte)1);
            swarm.ConfigureGrid(0, new int2(32), 1, grid); gameplay.ConfigureGrid(0, new int2(32), 1, grid);
            var first = swarm.CreateActorBody(1, new float2(5), .2f, 100, 2);
            var remapped = swarm.CreateActorBody(2, new float2(8), .2f, 100, 2);
            foreach (var entity in new[] { first, remapped })
            {
                gameplay.AttachActor(entity, new GameplayActor { Kind = GameplayKind.Sheep, MinIdle = .2f, MaxIdle = .8f, MinGraze = 1, MaxGraze = 3 });
                var actor = world.EntityManager.GetComponentData<GameplayActor>(entity);
                actor.RandomSeed = 123; actor.AttackSequence = 7;
                world.EntityManager.SetComponentData(entity, actor);
            }
            world.SetTime(new Unity.Core.TimeData(1.0 / 60, 1f / 60)); gameplay.Update();
            var a = world.EntityManager.GetComponentData<GameplayActor>(first);
            var b = world.EntityManager.GetComponentData<GameplayActor>(remapped);
            Assert.That(a.Grazing, Is.EqualTo(1)); Assert.That(a.GrazeTimer, Is.EqualTo(b.GrazeTimer));
            Assert.That(a.AttackSequence, Is.EqualTo(b.AttackSequence));
        }
    }
}
