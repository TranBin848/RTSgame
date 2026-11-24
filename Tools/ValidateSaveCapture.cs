using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RTS.DOTS;
using RTS.SaveLoad;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

// Run static stages through script-execute, with one real frame between Request and Check.
public static class ValidateSaveCapture
{
    const string Folder = "Outputs/SaveLoad/SL3";
    static GameSceneCompositionRoot Root => UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
    static MatchSaveData Snapshot
    {
        get { var c = Root.SaveCapture; if (c.IsPending || c.LastSnapshot == null || c.LastError != null) throw new Exception("Capture: " + c.LastError); return c.LastSnapshot; }
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Save(string file, object report)
    { Directory.CreateDirectory(Folder); File.WriteAllText(Path.Combine(Folder, file), JsonConvert.SerializeObject(report, Formatting.Indented)); }
    public static string Setup()
    {
        Check(Root != null && Root.IsReady, "ProceduralGame must be Ready.");
        Application.runInBackground = true; Time.timeScale = 0;
        var bridge = Root.GetComponent<SwarmGameBridge>(); var game = Root.GetComponent<GameManager>();
        var content = Resources.Load<SaveContentCatalog>("ProceduralSaveCatalog");
        var em = bridge.Gameplay.EntityManager;
        var workers = UnityEngine.Object.FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None).OrderBy(x => x.GetInstanceID()).ToArray();
        Check(workers.Length >= 3, "Three initial workers required.");
        var hall = UnityEngine.Object.FindFirstObjectByType<TownHall>();
        var node = UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None).First(x => x.MaxWorkers >= 2);
        game.AddResources(500, 500, 500);
        Check(bridge.AssignResource(workers[0], node), "Cargo assignment failed.");
        var worker = em.GetComponentData<GameplayWorker>(workers[0].DotsEntity);
        worker.Carried = 7; worker.Timer = .75f; worker.Phase = WorkPhase.Depositing; worker.Depot = hall.DotsEntity;
        em.SetComponentData(workers[0].DotsEntity, worker);
        Check(bridge.AssignResource(workers[2], node), "Resume assignment failed.");
        Check(workers[2].TryRecallToTownHall(hall) == GroupCommandResult.Accepted, "Recall command failed.");
        var sheltered = em.GetComponentData<GameplayWorker>(workers[2].DotsEntity);
        sheltered.Phase = WorkPhase.Sheltered; em.SetComponentData(workers[2].DotsEntity, sheltered);
        var shelterBody = em.GetComponentData<CombatBody>(workers[2].DotsEntity);
        shelterBody.Position = shelterBody.PreviousPosition = em.GetComponentData<CombatBody>(hall.DotsEntity).Position;
        shelterBody.Targetable = 0; em.SetComponentData(workers[2].DotsEntity, shelterBody);
        var buildAction = content.Actions.Select(x => x.Action).OfType<BuildActionSo>().First(x => content.DefinitionFor(x.StructureUnitPrefab.gameObject) == "storagepit");
        var hallBody = em.GetComponentData<CombatBody>(hall.DotsEntity);
        Check(bridge.Simulation.TryResolveSpawnPosition(hallBody.Position + new float2(9, 3), .5f, out var buildPosition), "Build fixture position failed.");
        new BuildingProcess(buildAction, new Vector3(buildPosition.x, buildPosition.y), workers[1], game, bridge);
        var buildWorker = em.GetComponentData<GameplayWorker>(workers[1].DotsEntity);
        var construction = em.GetComponentData<GameplayConstruction>(buildWorker.Structure);
        construction.Elapsed = 1.25f; em.SetComponentData(buildWorker.Structure, construction);
        var builder = em.GetComponentData<GameplayActor>(workers[1].DotsEntity); builder.State = GameplayState.Building;
        em.SetComponentData(workers[1].DotsEntity, builder);
        var train = content.Actions.Select(x => x.Action).OfType<SpawnVillagerActionSO>().Single();
        Check(bridge.EnqueueTraining(hall, train, content.ResolvePrefab("worker"), train.TrainDuration, "fixture"), "Train fixture failed.");
        Check(bridge.EnqueueTraining(hall, train, content.ResolvePrefab("worker"), train.TrainDuration, "fixture second"), "Second FIFO fixture failed.");
        var producer = em.GetComponentData<GameplayProducer>(hall.DotsEntity);
        producer.CurrentRequest = em.GetBuffer<TrainingRequest>(hall.DotsEntity)[0].RequestId; producer.Elapsed = 1.5f;
        em.SetComponentData(hall.DotsEntity, producer);
        Check(bridge.Gameplay.StartResearch("economy_tools", hall.DotsEntity) == ResearchResult.Started, "Research fixture failed.");
        var research = bridge.Gameplay.Research; research.Elapsed = 2.5f; em.SetComponentData(bridge.Gameplay.WalletEntity, research);
        Check(bridge.Simulation.TryResolveSpawnPosition(hallBody.Position + new float2(-7, 0), .25f, out var enemyPosition), "Enemy fixture position failed.");
        var enemy = bridge.Simulation.SpawnEnemy(enemyPosition, EnemySpawnSettings.Default);
        var shot = bridge.Simulation.SpawnProjectile(new ProjectileRequest { Owner = workers[0].DotsEntity, Target = enemy,
            Position = hallBody.Position + new float2(-2, 0), Aim = enemyPosition, Speed = 2, Damage = 9, VisualId = 2, VisualScale = 1 });
        var projectile = em.GetComponentData<SwarmProjectile>(shot); projectile.Age = .25f;
        projectile.Position = (projectile.Start + projectile.Aim) * .5f; em.SetComponentData(shot, projectile);
        bridge.Simulation.QueueDamage(enemy, 7, workers[0].DotsEntity);
        Save("Expected.json", new { seed = UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>().GeneratedMap.Seed,
            wood = bridge.Gameplay.Wallet.Wood, gold = bridge.Gameplay.Wallet.Gold, meat = bridge.Gameplay.Wallet.Meat,
            gameplayTicks = bridge.Gameplay.TickCount, swarmTicks = bridge.Simulation.TickCount, researchElapsed = 2.5f,
            expectedEnemyHealth = 93, cargo = 7, queue = 2 });
        Check(Root.SaveCapture.RequestCapture(10), "Capture request refused.");
        Check(!Root.SaveCapture.RequestCapture(11), "Duplicate pending request accepted.");
        return "Controlled cargo/shelter/construction/FIFO/research/native combat fixture requested.";
    }
    public static string CheckFixture()
    {
        var state = Snapshot;
        var expected = JObject.Parse(File.ReadAllText(Path.Combine(Folder, "Expected.json")));
        Check(state.Player.Wallet.Wood == (long)expected["wood"] && state.Player.Wallet.Gold == (long)expected["gold"] && state.Player.Wallet.Meat == (long)expected["meat"], "Capture changed wallet.");
        Check(state.GameplayTicks == (long)expected["gameplayTicks"] && state.SwarmTicks == (long)expected["swarmTicks"], "Capture advanced simulation ticks.");
        Check(state.Objects.Single(x => x.Enemy != null).Body.Health == 93, "Pending damage was not committed once.");
        Check(state.Objects.Single(x => x.Projectile != null).Projectile.Age == .25f, "Projectile age advanced during capture.");
        Check(state.Objects.Count(x => x.Worker?.Carried == 7) == 1, "Cargo missing.");
        Check(state.Objects.Single(x => x.Worker?.Phase == (int)WorkPhase.Sheltered).Worker.ResumeResource != 0, "Suspended work lost.");
        Check(state.Objects.Single(x => x.Construction?.Active == true).Construction.Elapsed == 1.25f, "Construction progress changed.");
        Check(state.Objects.Where(x => x.Producer != null).Sum(x => x.Producer.Queue.Length) == 2, "FIFO lost.");
        Check(state.Player.ActiveTechnologyId == "economy_tools" && state.Player.ResearchElapsed == 2.5f, "Research changed.");
        Check(state.Objects.Single(x => x.Depot?.TownHall == true).MaxHealth == 1200, "Hall override lost.");
        Check(state.Map.Placements.All(x => x.ObjectId > 0), "Generated placement identity missing.");
        var bytes = new SaveCodec(Root.SaveCapture.Catalog).Encode(state);
        var decoded = new SaveCodec(Root.SaveCapture.Catalog).Decode(bytes);
        Check(JToken.DeepEquals(JObject.FromObject(state), JObject.FromObject(decoded)), "Real snapshot DTO round-trip mismatch.");
        File.WriteAllBytes(Path.Combine(Folder, "FixtureCapture.json"), bytes);
        Save("FixtureChecks.json", new { passed = true, seed = state.Map.Seed, objects = state.Objects.Length, bytes = bytes.Length,
            captureMs = Root.SaveCapture.CaptureMilliseconds, paused = Time.timeScale == 0, cargo = 7, fifo = 2,
            pendingDamageOnce = true, research = state.Player.ActiveTechnologyId, construction = true, shelterResume = true, realFrameBoundary = true });
        Check(Root.SaveCapture.RequestCapture(11), "Repeat capture refused.");
        return "Fixture fields and real DTO round-trip PASS; repeat requested.";
    }
    public static string CheckRepeatAndInjectFailure()
    {
        var previous = JObject.FromObject(new SaveCodec(Root.SaveCapture.Catalog).Decode(File.ReadAllBytes(Path.Combine(Folder, "FixtureCapture.json"))));
        var current = JObject.FromObject(Snapshot);
        foreach (string field in new[] { "SaveId", "CapturedUtc", "Generation" }) { previous.Remove(field); current.Remove(field); }
        Check(JToken.DeepEquals(previous, current), "Repeated paused capture changed canonical gameplay state.");
        Check(!Root.GetComponent<SwarmGameBridge>().Simulation.SnapshotFenced, "Fence leaked after success.");
        UnityEngine.Object.FindFirstObjectByType<WorkerUnit>().GetComponent<SaveDefinitionTag>().SetAuthoringId("invalid_capture_fixture");
        Check(Root.SaveCapture.RequestCapture(12), "Failure fixture request refused.");
        Save("RepeatedCapture.json", new { passed = true, canonicalFieldsEqual = true, damageAppliedOnce = true });
        return "Repeated capture PASS; unknown-definition failure requested.";
    }
    public static string CheckFailureAndRequestCompletion()
    {
        var c = Root.SaveCapture; var bridge = Root.GetComponent<SwarmGameBridge>();
        Check(!c.IsPending && c.LastSnapshot == null && c.LastError != null, "Unknown definition was not rejected.");
        Check(!bridge.Simulation.SnapshotFenced && !Root.GetComponent<GameManager>().SnapshotFenced
            && !UnityEngine.Object.FindFirstObjectByType<DayNightCycleManager>().SnapshotFenced
            && !UnityEngine.Object.FindFirstObjectByType<EnemyWaveManager>().SnapshotFenced
            && !UnityEngine.Object.FindFirstObjectByType<CameraController>().LockCamera && Time.timeScale == 0, "Capture failure leaked a fence/pause state.");
        Save("FailureRecovery.json", new { passed = true, error = c.LastError, allFencesReleased = true, pausePreserved = true });
        foreach (var worker in UnityEngine.Object.FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None)) worker.GetComponent<SaveDefinitionTag>().SetAuthoringId("worker");
        var em = bridge.Gameplay.EntityManager;
        using var buildings = em.CreateEntityQuery(typeof(GameplayConstruction));
        using var entities = buildings.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            var build = em.GetComponentData<GameplayConstruction>(entity);
            if (build.Active != 0)
            {
                build.Worker = UnityEngine.Object.FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None)
                    .First(x => em.GetComponentData<GameplayWorker>(x.DotsEntity).Structure == entity).DotsEntity;
                build.Elapsed = build.Duration - .001f; em.SetComponentData(entity, build);
                var body = em.GetComponentData<CombatBody>(build.Worker);
                body.Position = body.PreviousPosition = em.GetComponentData<CombatBody>(entity).Position;
                em.SetComponentData(build.Worker, body);
                var actor = em.GetComponentData<GameplayActor>(build.Worker); actor.State = GameplayState.Building; actor.Moving = 0;
                em.SetComponentData(build.Worker, actor);
            }
            if (em.HasComponent<GameplayProducer>(entity) && em.GetBuffer<TrainingRequest>(entity).Length > 0)
            { var p = em.GetComponentData<GameplayProducer>(entity); p.Elapsed = em.GetBuffer<TrainingRequest>(entity)[0].Duration - .001f; em.SetComponentData(entity, p); }
        }
        var research = bridge.Gameplay.Research; research.Elapsed = bridge.Gameplay.ResearchDuration - .001f;
        em.SetComponentData(bridge.Gameplay.WalletEntity, research);
        var world = em.World; world.SetTime(new TimeData(world.Time.ElapsedTime + .02, .02f)); bridge.Gameplay.Update();
        Check(bridge.Gameplay.PendingSnapshotEvents >= 3, "Pending completion fixture did not generate events.");
        Check(c.RequestCapture(13), "Capture after failure refused.");
        return "Failure release PASS; queued native completions requested for capture.";
    }
    public static string CheckCompletions()
    {
        var snapshot = Snapshot;
        Check(snapshot.Objects.Count(x => x.Construction?.Finished == true) == 1, "Construction completion missing.");
        Check(snapshot.Objects.Count(x => x.Worker != null) == 4, "Training completion was not applied exactly once.");
        Check(snapshot.Objects.Where(x => x.Producer != null).Sum(x => x.Producer.Queue.Length) == 1, "FIFO did not advance one request.");
        Check(snapshot.Player.UnlockedTechnologyIds.Contains("economy_tools") && snapshot.Player.Statistics.ResearchCompleted == 1, "Research completion missing/duplicated.");
        Check(Root.GetComponent<SwarmGameBridge>().Gameplay.PendingSnapshotEvents == 0, "Committed completion queue was not consumed.");
        new SaveCodec(Root.SaveCapture.Catalog).Decode(new SaveCodec(Root.SaveCapture.Catalog).Encode(snapshot));
        Save("Completions.json", new { passed = true, constructionFinished = true, workers = 4, remainingQueue = 1,
            researchCompleted = snapshot.Player.Statistics.ResearchCompleted, noHistoricalEventReplay = true });
        File.WriteAllBytes(Path.Combine(Folder, "CompletionCapture.json"), new SaveCodec(Root.SaveCapture.Catalog).Encode(snapshot));
        Check(Root.SaveCapture.RequestCapture(14), "Post-completion repeat refused.");
        return "Native train/build/research completion flush PASS; repeat requested.";
    }
    public static string CheckFinalRepeat()
    {
        var previous = JObject.FromObject(new SaveCodec(Root.SaveCapture.Catalog).Decode(File.ReadAllBytes(Path.Combine(Folder, "CompletionCapture.json"))));
        var current = JObject.FromObject(Snapshot);
        foreach (string field in new[] { "SaveId", "CapturedUtc", "Generation" }) { previous.Remove(field); current.Remove(field); }
        Check(JToken.DeepEquals(previous, current), "Completion replayed during repeat capture.");
        Save("FinalRepeat.json", new { passed = true, repeatedCompletionAppliedOnce = true });
        return "Completion exactly-once across repeated capture PASS.";
    }
}
