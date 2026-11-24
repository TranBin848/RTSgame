using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using RTS.DOTS;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

// Bounded normal PlayerLoop run, timeScale1. Commands obey existing costs/queues; no clock injection or grants.
public static class GameExperienceE7Natural
{
    const string Dir = "Docs/Modules/GameExperience/Evidence/E7/";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static object Field(object obj, string name) => obj.GetType().GetField(name, Private).GetValue(obj);
    public static string Start()
    {
        var root = Find<GameSceneCompositionRoot>(); var b = Find<SwarmGameBridge>(); var gp = b.Gameplay; var em = gp.EntityManager;
        var game = Find<GameManager>(); var clock = Find<DayNightCycleManager>(); var fog = Find<FogOfWarManager>(); var hall = Find<TownHall>();
        if (!EditorApplication.isPlaying || !EditorApplication.isPaused || !root.IsReady || root.Reports.CompletedNight != 0
            || clock.CurrentDay != 1 || clock.CurrentPhase != DayNightPhase.Day || gp.Wallet.Wood != 100 || fog.IsDisabled || Time.timeScale != 1)
            throw new Exception("E7 natural requires fresh Ready/paused Day1, original wallet and fog.");
        var query = em.CreateEntityQuery(typeof(GameplayResource));
        var log = new StringBuilder("E7 natural Editor match, seed1, unmodified balance/60-30-45 clock, Unity PlayerLoop/timeScale1.\nPolicy: gather nearest available W/G/M, up to8 workers; existing Barrack/Archery queues if present, army defensive ring/auto-acquire; research tools,cargo,archery,training,outposts,fortify. No grants/teleport/phase injection or forced targets.\n");
        var roles = new Dictionary<Entity, int>(); var rallied = new HashSet<Entity>();
        var progress = new Dictionary<Entity, (Vector2 pos, int cargo, float time)>();
        var rejected = new Dictionary<Entity, HashSet<Entity>>();
        var workerAction = AssetDatabase.LoadAssetAtPath<SpawnVillagerActionSO>("Assets/Game/ScriptableObjects/Units/SpawnVillagerAction.asset");
        var soldierAction = AssetDatabase.LoadAssetAtPath<SpawnWarriorActionSO>("Assets/Game/ScriptableObjects/Units/SpawnWarriorAction.asset");
        var archerAction = AssetDatabase.LoadAssetAtPath<SpawnArcherActionSO>("Assets/Game/ScriptableObjects/Units/SpawnArcherAction.asset");
        string[] order = { "economy_tools", "economy_cargo", "military_archery", "military_training", "expansion_tower", "expansion_fortify" };
        bool oldBackground = Application.runInBackground; Application.runInBackground = true;
        float started = Time.time, nextPolicy = 0, nextLog = 0, closeReportAt = float.PositiveInfinity;
        double wall = EditorApplication.timeSinceStartup;
        int reported = 0, endingFrames = 0, pendingDawn = 0, dawnFrame = -1; var photos = new HashSet<string>();
        var camera = Camera.main; camera.transform.position = new Vector3(hall.transform.position.x, hall.transform.position.y, camera.transform.position.z); camera.orthographicSize = 6;
        var controller = Find<CameraController>(); bool previousCameraLock = controller.LockCamera; controller.LockCamera = true;
        var capturePosition = camera.transform.position;
        void Balance()
        {
            var s = gp.Statistics; var w = gp.Wallet;
            if (w.Wood != s.Granted.Wood + s.Deposited.Wood - s.Spent.Wood || w.Gold != s.Granted.Gold + s.Deposited.Gold - s.Spent.Gold
                || w.Meat != s.Granted.Meat + s.Deposited.Meat - s.Spent.Meat) throw new Exception("Natural ledger mismatch");
            if (s.Granted.Wood != 100 || s.Granted.Gold != 100 || s.Granted.Meat != 100) throw new Exception("Unexpected external grant");
            if (Vector3.Distance(camera.transform.position, capturePosition) > .01f || Mathf.Abs(camera.orthographicSize - 6) > .001f) throw new Exception("Capture camera moved");
        }
        void Policy(float elapsed)
        {
            var workers = game.GetFriendlyUnits(true).OfType<WorkerUnit>().Where(u => u != null && u.CurrentHealth > 0 && u.IsDotsControlled).ToArray();
            if (gp.Research.Active == 0)
            {
                string id = order.FirstOrDefault(t => !root.Research.IsUnlocked(t));
                if (id != null) root.Research.Start(id, hall);
            }
            if (workers.Length < 8 && em.Exists(hall.DotsEntity) && em.GetBuffer<TrainingRequest>(hall.DotsEntity).Length == 0 && gp.Wallet.Meat >= workerAction.MeatCost)
                if (hall.EnqueueVillagerSpawn(workerAction)) game.AddResources(0, 0, -workerAction.MeatCost);
            var barrack = Find<Barrack>(); var archery = Find<ArcheryRange>();
            var army = game.GetFriendlyUnits(true).Where(u => (u is WarriorUnit || u is RangerUnit) && u.CurrentHealth > 0 && u.IsDotsControlled).ToArray();
            bool Affordable(IResourceCostAction cost) => gp.Wallet.Wood >= cost.WoodCost && gp.Wallet.Gold >= cost.GoldCost && gp.Wallet.Meat >= cost.MeatCost;
            if (barrack != null && barrack.IsDotsControlled && army.Count(u => u is WarriorUnit) < 8
                && em.GetBuffer<TrainingRequest>(barrack.DotsEntity).Length == 0 && Affordable(soldierAction) && barrack.EnqueueWarriorSpawn(soldierAction))
                game.AddResources(-soldierAction.GoldCost, -soldierAction.WoodCost, -soldierAction.MeatCost);
            if (archery != null && archery.IsDotsControlled && gp.CanUseArchery && army.Count(u => u is RangerUnit) < 6
                && em.GetBuffer<TrainingRequest>(archery.DotsEntity).Length == 0 && Affordable(archerAction) && archery.EnqueueArcherSpawn(archerAction))
                game.AddResources(-archerAction.GoldCost, -archerAction.WoodCost, -archerAction.MeatCost);
            foreach (var unit in army)
            {
                if (rallied.Add(unit.DotsEntity))
                {
                    float angle = rallied.Count * 2.39996f;
                    gp.SetStance(unit.DotsEntity, true);
                    b.MoveActor(unit, hall.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 3, true);
                }
            }
            if (clock.CurrentPhase != DayNightPhase.Day) return;
            using var nodes = query.ToEntityArray(Allocator.Temp);
            foreach (var unit in workers)
            {
                var entity = unit.DotsEntity; if (!roles.ContainsKey(entity)) roles[entity] = roles.Count;
                gp.TryGetWorker(entity, out var data); gp.TryGetActor(entity, out var actor, out var body);
                var pos = (Vector2)body.Position;
                if (!progress.TryGetValue(entity, out var last) || Vector2.Distance(pos, last.pos) > .2f || data.Carried != last.cargo)
                    progress[entity] = last = (pos, data.Carried, elapsed);
                bool stalled = data.Phase == WorkPhase.Gathering && data.Carried == 0 && elapsed - last.time > 15;
                if (data.Phase != WorkPhase.None && em.Exists(data.Node) && !stalled) continue;
                if (stalled)
                {
                    if (!rejected.TryGetValue(entity, out var set)) rejected[entity] = set = new HashSet<Entity>();
                    set.Add(data.Node); gp.CancelWork(entity); progress[entity] = (pos, data.Carried, elapsed);
                }
                int role = roles[entity] % 4; int type = role < 2 ? 0 : role == 2 ? 1 : 2;
                var node = nodes.ToArray().Where(e => { var r = em.GetComponentData<GameplayResource>(e); return r.Type == type && r.Claims < r.Capacity && (!rejected.TryGetValue(entity, out var set) || !set.Contains(e)); })
                    .OrderBy(e => math.distancesq(body.Position, em.GetComponentData<GameplayResource>(e).Position)).FirstOrDefault();
                if (node != Entity.Null) gp.AssignResource(entity, node);
            }
        }
        Directory.CreateDirectory(Dir);
        EditorApplication.CallbackFunction update = null;
        void Finish(string error)
        {
            EditorApplication.update -= update; query.Dispose(); Application.runInBackground = oldBackground;
            controller.LockCamera = previousCameraLock;
            log.AppendLine($"finished wall={EditorApplication.timeSinceStartup-wall:F3};gameSeconds={Time.time-started:F3};outcome={gp.Outcome};completedNights={root.Reports.CompletedNight};signals={root.DawnReport.DawnSignalCount};error={error}");
            File.WriteAllText(Dir + "natural-five-nights.txt", log.ToString());
            EditorApplication.isPaused = true;
        }
        update = () =>
        {
            try
            {
                if (!EditorApplication.isPlaying) { Finish("Play stopped early"); return; }
                if (EditorApplication.isPaused) return;
                float elapsed = Time.time - started;
                if (EditorApplication.timeSinceStartup - wall > 1100) { Finish("wall watchdog"); return; }
                Balance();
                if (!gp.IsMatchOver && elapsed >= nextPolicy) { nextPolicy = elapsed + 5; Policy(elapsed); }
                if (elapsed >= nextLog)
                {
                    nextLog = elapsed + 15;
                    float hp = em.Exists(hall.DotsEntity) ? em.GetComponentData<CombatBody>(hall.DotsEntity).Health : 0;
                    log.AppendLine($"t={elapsed:F3};day={clock.CurrentDay};phase={clock.CurrentPhase};HP={hp};wallet={gp.Wallet.Wood}/{gp.Wallet.Gold}/{gp.Wallet.Meat};stats=" + Newtonsoft.Json.JsonConvert.SerializeObject(root.Reports.Total));
                    File.WriteAllText(Dir + "natural-five-nights.txt", log.ToString());
                }
                string phase = clock.CurrentPhase.ToString();
                if (clock.CurrentDay == 1 && clock.CurrentPhaseProgress >= .5f && photos.Add(phase))
                    GameExperienceE7Validation.Capture("natural-" + phase + ".png");
                if (root.Reports.CompletedNight > reported)
                {
                    reported = root.Reports.CompletedNight;
                    log.AppendLine("dawn=" + reported + ";snapshot=" + Newtonsoft.Json.JsonConvert.SerializeObject(root.Reports.LatestNight));
                    root.DawnReport.OpenLatest();
                    closeReportAt = elapsed + 2;
                    pendingDawn = reported; dawnFrame = Time.frameCount;
                }
                if (pendingDawn > 0 && !gp.IsMatchOver && Time.frameCount > dawnFrame + 1)
                {
                    if (pendingDawn == 1 || pendingDawn == 4)
                    {
                        GameExperienceE7Validation.Capture("natural-dawn-" + pendingDawn + ".png");
                        if (pendingDawn == 4)
                        {
                            GameExperienceE7Validation.Capture("natural-dawn.png");
                            var audio = (AudioSource)Field(root.DawnReport, "m_Audio");
                            File.WriteAllText(Dir + "natural-dawn.txt", $"Natural dawn4;signals={root.DawnReport.DawnSignalCount};timeScale={Time.timeScale};reportOpen={root.DawnReport.IsOpen};sourcePlaying={audio.isPlaying};camera={camera.transform.position};orthographicSize={camera.orthographicSize};cameraLocked={controller.LockCamera}. Software playback, not physical audibility confirmation.\n");
                        }
                    }
                    pendingDawn = 0;
                }
                if (elapsed >= closeReportAt) { root.DawnReport.Close(); closeReportAt = float.PositiveInfinity; }
                // Give the Game View a rendered frame before saving terminal UI.
                if (gp.IsMatchOver && ++endingFrames >= 4)
                {
                    Balance();
                    File.WriteAllText(Dir + "natural-result.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
                    { outcome = gp.Outcome.ToString(), completedNights = root.Reports.CompletedNight, signals = root.DawnReport.DawnSignalCount,
                        total = root.Reports.Total, wallet = gp.Wallet, resultText = Find<MatchSession>().ResultStatisticsText, duration = elapsed,
                        wallSeconds = EditorApplication.timeSinceStartup - wall, unity = Application.unityVersion, editor = true, timeScaleBeforeTerminal = 1,
                        durations = new[] { clock.Definition.DayDuration, clock.Definition.DuskDuration, clock.Definition.NightDuration }, fogEnabled = !fog.IsDisabled,
                        cameraPosition = new[] { camera.transform.position.x, camera.transform.position.y, camera.transform.position.z }, cameraLocked = controller.LockCamera, orthographicSize = camera.orthographicSize, resolution = new[] { Screen.width, Screen.height } }, Newtonsoft.Json.Formatting.Indented));
                    GameExperienceE7Validation.Capture("natural-result.png"); Finish("");
                }
            }
            catch (Exception error) { Finish(error.ToString()); }
        };
        EditorApplication.update += update;
        EditorApplication.isPaused = false;
        return "Armed natural five-night Editor run (675 game seconds, watchdog1100wall). No manual simulation/clock ticks; writing progress every15game seconds.";
    }
}
