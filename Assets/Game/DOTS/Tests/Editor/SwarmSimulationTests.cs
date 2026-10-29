using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace RTS.DOTS.Tests
{
    public class SwarmSimulationTests
    {
        World m_World;
        SwarmSimulationSystem m_Simulation;
        double m_Time;
        [SetUp]
        public void SetUp()
        {
            m_World = new World("Swarm tests");
            m_Simulation = m_World.GetOrCreateSystemManaged<SwarmSimulationSystem>();
            byte[] grid = new byte[64 * 64]; System.Array.Fill(grid, (byte)1);
            m_Simulation.ConfigureGrid(new float2(-32), new int2(64), 1, grid);
            m_Time = 0;
        }
        [TearDown] public void TearDown() { m_World.Dispose(); }
        void Tick(int count = 1)
        {
            for (int i = 0; i < count; i++) { m_Time += 1.0 / 60; m_World.SetTime(new TimeData(m_Time, 1f / 60)); m_Simulation.Update(); }
        }
        Entity Enemy(float2 point, float speed = 0)
        { var settings = EnemySpawnSettings.Default; settings.Speed = speed; return m_Simulation.SpawnEnemy(point, settings); }

        [Test]
        public void SpawnResolverMovesBlockedAuthoringPositionToNearestValidCell()
        {
            byte[] map = new byte[64 * 64]; System.Array.Fill(map, (byte)1);
            map[32 * 64 + 32] = 0;
            m_Simulation.ConfigureGrid(new float2(-32), new int2(64), 1, map);
            Assert.That(m_Simulation.TryResolveSpawnPosition(new float2(0.5f), 0.25f, out var resolved), Is.True);
            Assert.That(math.all((int2)math.floor(resolved) == 0), Is.False);
            Assert.That(m_Simulation.SpawnEnemy(resolved, EnemySpawnSettings.Default), Is.Not.EqualTo(Entity.Null));
            System.Array.Fill(map, (byte)0);
            m_Simulation.ConfigureGrid(new float2(-32), new int2(64), 1, map);
            Assert.That(m_Simulation.TryResolveSpawnPosition(0, 0.25f, out _), Is.False);
        }
        [Test]
        public void FlowFieldFindsRouteAroundWallAndDoesNotCutCorner()
        {
            const int size = 24;
            byte[] map = new byte[size * size];
            System.Array.Fill(map, (byte)1);
            for (int y = 0; y < 20; y++) map[y * size + 10] = 0;
            using var walkable = new NativeArray<byte>(map, Allocator.TempJob);
            using var costs = new NativeArray<int>(size * size, Allocator.TempJob);
            using var queue = new NativeArray<int>(size * size, Allocator.TempJob);
            using var directions = new NativeArray<float2>(size * size, Allocator.TempJob);
            var job = new FlowFieldJob { Walkable = walkable, Costs = costs, Queue = queue, Directions = directions, Size = new int2(size), Goal = new int2(18, 4) };
            job.Execute();
            Assert.That(costs[4 * size + 4], Is.LessThan(int.MaxValue));
            float2 p = new float2(4.5f, 4.5f);
            for (int i = 0; i < 1000 && math.distance(p, new float2(18.5f, 4.5f)) > 1; i++)
            {
                int2 cell = (int2)math.floor(p);
                float2 steering = SwarmMath.FlowDirection(p, directions[cell.y * size + cell.x], 0, 1);
                p = SwarmMath.MoveOnGrid(p, steering * 0.1f, 0.2f, 0, 1, new int2(size), walkable);
                Assert.That(SwarmMath.Fits(p, 0.2f, 0, 1, new int2(size), walkable), Is.True);
            }
            Assert.That(math.distance(p, new float2(18.5f, 4.5f)), Is.LessThan(2));
        }
        [Test] public void DamageIsAggregatedAndAppliedExactlyOnce()
        {
            Entity enemy = Enemy(new float2(8, 8));
            m_Simulation.QueueDamage(enemy, 10); m_Simulation.QueueDamage(enemy, 20); Tick();
            Assert.That(m_Simulation.TryGetBody(enemy, out var body), Is.True); Assert.That(body.Health, Is.EqualTo(70));
            Tick(); m_Simulation.TryGetBody(enemy, out body); Assert.That(body.Health, Is.EqualTo(70));
        }
        [Test] public void FastProjectileSweepsThroughTargetWithoutTunnelling()
        {
            Entity enemy = Enemy(new float2(8, 0));
            m_Simulation.SpawnProjectile(new ProjectileRequest { Position = new float2(-8, 0), Aim = new float2(8, 0), Target = enemy,
                Faction = 0, Damage = 17, Speed = 200 });
            Tick(10); m_Simulation.TryGetBody(enemy, out var body);
            Assert.That(body.Health, Is.EqualTo(83)); Assert.That(m_Simulation.ProjectileCount, Is.Zero);
        }
        [Test] public void StaticObstacleConsumesProjectileBeforeEnemy()
        {
            Entity enemy = Enemy(new float2(8, 0));
            m_Simulation.CreateProxy(0, 0, new float2(0.5f, 3), float.MaxValue, -1);
            m_Simulation.SpawnProjectile(new ProjectileRequest { Position = new float2(-8, 0), Aim = new float2(8, 0), Target = enemy,
                Faction = 0, Damage = 17, Speed = 200 });
            Tick(10); m_Simulation.TryGetBody(enemy, out var body);
            Assert.That(body.Health, Is.EqualTo(100)); Assert.That(m_Simulation.ProjectileCount, Is.Zero);
        }
        [Test] public void FriendlyProjectileDoesNotDamageSameFaction()
        {
            Entity enemy = Enemy(new float2(8, 0));
            m_Simulation.SpawnProjectile(new ProjectileRequest { Position = new float2(-8, 0), Aim = new float2(8, 0), Target = enemy,
                Faction = 1, Damage = 17, Speed = 200 });
            Tick(10); m_Simulation.TryGetBody(enemy, out var body); Assert.That(body.Health, Is.EqualTo(100));
        }
        [Test] public void DestroyedTargetInvalidatesProjectileAndEntityHandle()
        {
            Entity enemy = Enemy(new float2(8, 0));
            m_Simulation.SpawnProjectile(new ProjectileRequest { Position = 0, Aim = new float2(8, 0), Target = enemy, Speed = 10 });
            m_Simulation.Remove(enemy); Tick(); Assert.That(m_Simulation.ProjectileCount, Is.Zero);
            Assert.That(m_Simulation.TryGetBody(enemy, out _), Is.False);
        }
        [Test] public void ExternalDamageCanBeDrainedOnce()
        {
            Entity proxy = m_Simulation.CreateProxy(77, 0, new float2(0.3f), 100);
            m_Simulation.QueueDamage(proxy, 25); Tick();
            Assert.That(m_Simulation.TryTakeExternalDamage(out var damage), Is.True);
            Assert.That(damage.ExternalId, Is.EqualTo(77)); Assert.That(damage.Amount, Is.EqualTo(25));
            Assert.That(m_Simulation.TryTakeExternalDamage(out _), Is.False);
        }
        [Test] public void RangedAttackUsesConfiguredProjectileInsteadOfHardcodedTrajectory()
        {
            Entity goal = m_Simulation.CreateProxy(0, new float2(3, 0), new float2(0.3f), 1000);
            m_Simulation.SetGoal(new float2(3, 0), goal);
            var settings = EnemySpawnSettings.Default;
            settings.Speed = 0; settings.Ranged = true; settings.AttackRange = 6; settings.AttackDelay = 0.01f;
            settings.Projectile = new ProjectileParameters { Speed = 2, Arc = 0, Spinning = 1, VisualScale = 0.75f };
            m_Simulation.SpawnEnemy(0, settings); Tick(2);
            using var query = m_World.EntityManager.CreateEntityQuery(typeof(SwarmProjectile));
            Assert.That(query.CalculateEntityCount(), Is.EqualTo(1));
            var projectile = query.GetSingleton<SwarmProjectile>();
            Assert.That(projectile.Speed, Is.EqualTo(2)); Assert.That(projectile.Arc, Is.Zero);
            Assert.That(projectile.Spinning, Is.EqualTo(1)); Assert.That(projectile.VisualScale, Is.EqualTo(0.75f));
        }
        [Test] public void RetreatingEnemiesCannotBeTargetedAndEventuallyDespawn()
        {
            Entity enemy = Enemy(new float2(4, 0), 4); m_Simulation.Retreat(0, 6);
            Assert.That(m_Simulation.TryGetBody(enemy, out _), Is.False);
            Tick(120); Assert.That(m_Simulation.AgentCount, Is.Zero);
        }
        [Test] public void EnemyAcquisitionDoesNotTreatNeutralSheepAsPlayerUnits()
        {
            Entity player = m_Simulation.CreateProxy(0, new float2(8, 0), new float2(0.3f), 1000);
            m_Simulation.CreateProxy(0, new float2(1, 0), new float2(0.3f), 1000, 2);
            m_Simulation.SetGoal(new float2(8, 0), player);
            Entity enemy = Enemy(0); Tick(30);
            Assert.That(m_World.EntityManager.GetComponentData<SwarmAgent>(enemy).Target, Is.EqualTo(player));
        }
        [Test] public void ResetRemovesAllAgentAndProjectileState()
        {
            Entity enemy = Enemy(new float2(8, 0)); Tick();
            m_Simulation.SpawnProjectile(new ProjectileRequest { Target = enemy, Aim = new float2(8, 0), Speed = 10 });
            m_Simulation.ResetSimulation(); Tick();
            Assert.That(m_Simulation.AgentCount, Is.Zero); Assert.That(m_Simulation.ProjectileCount, Is.Zero);
            Assert.That(m_Simulation.RenderData.Length, Is.Zero); Assert.That(m_Simulation.TryFindEnemy(new float2(8, 0), 10, out _), Is.False);
        }
        [Test] public void FiveThousandAgentsAndTwoThousandProjectilesStayFinite()
        {
            Entity goal = m_Simulation.CreateProxy(0, 0, new float2(1), 1000000000); m_Simulation.SetGoal(0, goal);
            Entity[] enemies = new Entity[5000];
            for (int i = 0; i < enemies.Length; i++)
                enemies[i] = Enemy(new float2(-25 + (i % 100) * 0.5f, -25 + (i / 100) * 0.5f), 2);
            for (int i = 0; i < 2000; i++) m_Simulation.SpawnProjectile(new ProjectileRequest
                { Owner = goal, Target = enemies[i], Position = 0, Aim = new float2(-25, -25), Damage = 1, Faction = 0, Speed = 10 });
            Tick(10);
            Assert.That(m_Simulation.AgentCount, Is.EqualTo(5000));
            foreach (var render in m_Simulation.RenderData) Assert.That(math.all(math.isfinite(render.Position)), Is.True);
        }
    }
}
