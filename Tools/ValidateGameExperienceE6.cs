using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Text;
using RTS.DOTS;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class GameExperienceE6Validation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Dir = "Docs/Modules/GameExperience/Evidence/E6/";
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool value, string message) { if (!value) throw new Exception("E6: " + message); }
    static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Save(string name, string text) { Directory.CreateDirectory(Dir); File.WriteAllText(Dir + name, text); }
    static void Sync(SwarmGameBridge b) { Call(b, "SynchronizeGameplay"); Physics2D.SyncTransforms(); }
    static void Tick(SwarmGameBridge b, int count, bool combat = false)
    {
        var world = b.Gameplay.World; var old = world.Time; bool paused = b.Simulation.Paused;
        try
        {
            b.Simulation.Paused = false;
            for (int i = 1; i <= count; i++)
            {
                world.SetTime(new TimeData(old.ElapsedTime + .1 * i, .1f));
                if (combat) b.Simulation.Update(); else b.Gameplay.Update();
                Sync(b);
            }
        }
        finally { world.SetTime(old); b.Simulation.Paused = paused; }
    }
    static void Capture(string name)
    {
        var camera = Camera.main; var rt = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = rt });
            var previous = RenderTexture.active; RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply(); RenderTexture.active = previous;
            Directory.CreateDirectory(Dir); File.WriteAllBytes(Dir + name + ".png", image.EncodeToPNG());
        }
        finally { RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
    }

    public static string Run()
    {
        var root = Find<GameSceneCompositionRoot>(); var view = root.ActionFeedback; var b = Find<SwarmGameBridge>();
        var game = Find<GameManager>(); var fog = Find<FogOfWarManager>(); var camera = Camera.main;
        Check(EditorApplication.isPlaying && EditorApplication.isPaused && root.IsReady && view.IsInitialized, "fresh Ready paused match required");
        var log = new StringBuilder("E6 actual Editor/ProceduralGame fixture. ECS timers accelerated explicitly; not natural clock/performance.\n");
        b.Simulation.Paused = true;
        view.Initialize(b, game, fog, camera, root.Research, Find<DayNightCycleManager>().Presentation,
            Find<DayNightClockUI>().GetComponentInParent<Canvas>(), Find<SwarmInstancedRenderer>());
        var listeners = (Delegate)typeof(SwarmGameBridge).GetField("PresentationEvent", Private).GetValue(b);
        Check(listeners.GetInvocationList().Count(d => d.Target == view) == 1, "duplicate event subscription");
        var rng = JsonUtility.ToJson(UnityEngine.Random.state); var wallet = b.Gameplay.Wallet; var tick = b.Gameplay.TickCount;
        int materials = Resources.FindObjectsOfTypeAll<Material>().Length;
        for (int i = 0; i < 20; i++) { Sync(b); view.RefreshPresentation(0); }
        Check(view.ResearchCount == 0 && view.GatherCount == 0 && view.ImpactCount == 0, "snapshot replay");
        Check(materials == Resources.FindObjectsOfTypeAll<Material>().Length && rng == JsonUtility.ToJson(UnityEngine.Random.state)
            && wallet.Equals(b.Gameplay.Wallet) && tick == b.Gameplay.TickCount, "presentation changed state/material allocation");
        log.AppendLine("PASS repeated Initialize/listener=1; 20 snapshots no event replay/material growth/RNG/wallet/tick changes.");

        var worker = game.GetFriendlyUnits(true).OfType<WorkerUnit>().First(); var hall = Find<TownHall>();
        b.Gameplay.TryGetActor(worker.DotsEntity, out var actor, out var body);
        var position = body.Position;
        camera.transform.position = new Vector3(position.x, position.y, camera.transform.position.z); camera.orthographicSize = 4;
        if (!fog.IsDisabled) Call(fog, "ToggleFogOfWar");
        var node = b.Gameplay.CreateResource(new GameplayResource { Position = position, Radius = .3f, Type = 0, Capacity = 4 });
        Check(b.Gameplay.AssignResource(worker.DotsEntity, node), "assign fixture resource");
        int before = view.GatherCount;
        for (int i = 0; i < 200 && view.GatherCount == before; i++) Tick(b, 1);
        Check(view.GatherCount == before + 1, "gather event missing/duplicated");
        view.RefreshPresentation(0); Check(view.BadgeCount == 1 && view.ActivePulseCount > 0, "cargo/dust not shown");
        Capture("gather-cargo");
        var workerState = b.Gameplay.EntityManager.GetComponentData<GameplayWorker>(worker.DotsEntity);
        int carried = workerState.Carried; b.Gameplay.TryGetActor(hall.DotsEntity, out _, out var depot);
        body = b.Gameplay.EntityManager.GetComponentData<CombatBody>(worker.DotsEntity);
        body.Position = body.PreviousPosition = depot.Position;
        b.Gameplay.EntityManager.SetComponentData(worker.DotsEntity, body);
        workerState.Phase = WorkPhase.Depositing; workerState.Depot = hall.DotsEntity;
        b.Gameplay.EntityManager.SetComponentData(worker.DotsEntity, workerState);
        wallet = b.Gameplay.Wallet; int deposits = view.DepositCount;
        Tick(b, 1); view.RefreshPresentation(0);
        Check(view.DepositCount == deposits + 1 && b.Gameplay.Wallet.Wood == wallet.Wood + carried && view.BadgeCount == 0, "deposit/debit/badge mismatch");
        b.Gameplay.CancelWork(worker.DotsEntity);
        log.AppendLine($"PASS authoritative gather once + carried badge, deposit once amount={carried}, wallet +{carried}, badge cleared.");

        camera.transform.position = new Vector3(depot.Position.x, depot.Position.y, camera.transform.position.z);
        b.Simulation.QueueDamage(hall.DotsEntity, 7); int impacts = view.ImpactCount;
        Tick(b, 1, true); view.RefreshPresentation(0);
        Check(view.ImpactCount == impacts + 1 && b.Gameplay.EntityManager.GetComponentData<CombatBody>(hall.DotsEntity).Health == depot.Health - 7, "impact duplicated damage");
        Capture("deposit-hit");
        var pulses = (IEnumerable)Field(view, "m_Pulses");
        string Ages() => string.Join(",", pulses.Cast<object>().Select(p => p.GetType().GetField("Age").GetValue(p).ToString()));
        string ages = Ages(); view.RefreshPresentation(10); Sync(b); view.RefreshPresentation(10);
        Check(ages == Ages() && view.ImpactCount == impacts + 1, "pause changed age/replayed impact");
        log.AppendLine("PASS QueueDamage7 -> HP -7, one impact; repeated snapshots and paused delta10 preserve ages/count.");

        b.Gameplay.AddResources(10000, 10000, 10000);
        var tree = Find<ResearchTreePanel>();
        var profiles = b.Gameplay.EntityManager.GetBuffer<GatheringProfileData>(worker.DotsEntity).ToNativeArray(Unity.Collections.Allocator.Temp);
        var producer = b.Gameplay.EntityManager;
        // A synthetic projectile remains in flight while only research/economy advances.
        var projectile = b.Simulation.SpawnProjectile(new ProjectileRequest { Owner = hall.DotsEntity, Position = depot.Position,
            Aim = depot.Position + new float2(10, 0), Damage = 17, Faction = 0, Speed = .1f, VisualScale = 1 });
        Check(projectile != Entity.Null, "fixture projectile");
        b.Gameplay.EnqueueTraining(hall.DotsEntity, 900001, 500);
        float queued = producer.GetBuffer<TrainingRequest>(hall.DotsEntity)[0].Duration;
        string[] ids = { "economy_tools", "economy_cargo", "military_archery", "military_training", "expansion_tower", "expansion_fortify" };
        for (int i = 0; i < ids.Length; i++)
        {
            if (i % 2 == 0) tree.Open(); else tree.Close();
            var definition = root.Research.GetDefinition(ids[i]); wallet = b.Gameplay.Wallet;
            b.Simulation.Paused = false;
            Check(root.Research.Start(ids[i], hall) == ResearchResult.Started, "start " + ids[i]);
            b.Simulation.Paused = true;
            Check(b.Gameplay.Wallet.Wood == wallet.Wood - definition.Wood && b.Gameplay.Wallet.Gold == wallet.Gold - definition.Gold
                && b.Gameplay.Wallet.Meat == wallet.Meat - definition.Meat, "research debit " + ids[i]);
            var state = root.Research.State; state.Elapsed = definition.Duration - .05f;
            producer.SetComponentData(b.Gameplay.WalletEntity, state);
            Tick(b, 1); view.RefreshPresentation(0);
            Check(root.Research.IsUnlocked(ids[i]) && view.ResearchCount == i + 1, "completion " + ids[i]);
            string text = view.Notification;
            for (int repeat = 0; repeat < 4; repeat++) { Sync(b); view.RefreshPresentation(0); }
            Check(view.ResearchCount == i + 1 && view.Notification == text, "replay " + ids[i]);
            log.AppendLine($"PASS completion {ids[i]} treeOpen={tree.IsOpen} text={text}");
        }
        tree.Close();
        Check(producer.GetBuffer<TrainingRequest>(hall.DotsEntity)[0].Duration == queued, "existing queue duration changed");
        Check(producer.GetComponentData<SwarmProjectile>(projectile).Damage == 17, "in-flight damage changed");
        var now = producer.GetBuffer<GatheringProfileData>(worker.DotsEntity);
        for (int i = 0; i < profiles.Length; i++) Check(profiles[i].Equals(now[i]), "base gather profile mutated");
        profiles.Dispose();
        log.AppendLine("PASS base gather profiles unchanged; queued500 duration unchanged; in-flight damage17 unchanged.");
        var tower = b.SpawnGameplay(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Buildings/Tower.prefab"),
            hall.transform.position + new Vector3(3, -2), Quaternion.identity).GetComponent<TowerUnit>();
        body = producer.GetComponentData<CombatBody>(worker.DotsEntity);
        body.Position = body.PreviousPosition = depot.Position + new float2(2, -2); producer.SetComponentData(worker.DotsEntity, body);
        node = b.Gameplay.CreateResource(new GameplayResource { Position = body.Position, Radius = .3f, Type = 0, Capacity = 4 });
        b.Gameplay.AssignResource(worker.DotsEntity, node); before = view.GatherCount;
        for (int i = 0; i < 200 && view.GatherCount == before; i++) Tick(b, 1);
        view.RefreshPresentation(0);
        Check(view.BadgeCount == 2, "upgraded Cargo / Fortify badges missing");
        var badges = (SpriteRenderer[])Field(view, "m_Badges");
        Check(badges.Take(view.BadgeCount).Any(v => v.sprite == (Sprite)Field(view, "m_Crate") && Mathf.Approximately(v.transform.localScale.x, .32f)), "Cargo upgraded size");
        Check(badges.Take(view.BadgeCount).Any(v => v.sprite == (Sprite)Field(view, "m_Fortify")), "Fortify glyph");
        Find<GameSceneCompositionRoot>().DepthPresentation.RefreshPresentation();
        Capture("cargo-fortified-tower");
        body = producer.GetComponentData<CombatBody>(tower.DotsEntity); body.Health = 0; producer.SetComponentData(tower.DotsEntity, body);
        view.RefreshPresentation(0); Check(view.BadgeCount == 1, "dead building kept fortification glyph");
        b.Gameplay.CancelWork(worker.DotsEntity);
        log.AppendLine("PASS real bound Tower after Fortify + Cargo .32 badge; dead Tower immediately removes its badge.");
        // Burst of authoritative hits: visual capacity may drop, combat must still apply every hit.
        view.RefreshPresentation(0); int budget = ((Array)Field(view, "m_Pulses")).Length;
        var target = b.Simulation.SpawnEnemy(depot.Position + new float2(2, 0), EnemySpawnSettings.Default);
        body = producer.GetComponentData<CombatBody>(target); body.Health = 10000; producer.SetComponentData(target, body);
        impacts = view.ImpactCount;
        for (int i = 0; i < 300; i++) b.Simulation.QueueDamage(target, 1);
        Tick(b, 1, true); view.RefreshPresentation(0);
        Check(view.ImpactCount - impacts == 256 && view.ActivePulseCount <= budget && view.DroppedCount > 0, "bounded impact/pulse buffers");
        Check(producer.GetComponentData<CombatBody>(target).Health == 9700, "visual cap dropped damage");
        log.AppendLine($"PASS 300 hits -> HP -300, bridge impact cap256, active pulses {view.ActivePulseCount}/{budget}, visual drops {view.DroppedCount}.");
        Capture("research-impact-budget");
        b.Simulation.Remove(target); b.Simulation.Remove(projectile);
        Save("integration.txt", log.ToString());
        return log.ToString();
    }

    static string Pose()
    {
        var view = Find<GameSceneCompositionRoot>().ActionFeedback;
        return string.Join(";", ((IEnumerable)Field(view, "m_Pulses")).Cast<object>().Select(p =>
            p.GetType().GetField("Age").GetValue(p) + "," + ((SpriteRenderer)p.GetType().GetField("View").GetValue(p)).color))
            + "|" + Field(view, "m_ToastRemaining");
    }
    static string Log(string text)
    { Directory.CreateDirectory(Dir); File.AppendAllText(Dir + "lifecycle.txt", text + "\n"); return text; }

    public static string Pause()
    {
        Save("lifecycle.txt", ""); Find<GameFlowMenu>().Pause();
        SessionState.SetString("E6.PausePose", Pose());
        SessionState.SetFloat("E6.PauseProgress", Find<DayNightCycleManager>().CurrentPhaseProgress);
        return "Actual menu paused; step once then CheckPause.";
    }
    public static string CheckPause()
    {
        Check(Time.timeScale == 0 && Pose() == SessionState.GetString("E6.PausePose", "")
            && Find<DayNightCycleManager>().CurrentPhaseProgress == SessionState.GetFloat("E6.PauseProgress", -1), "menu pause moved lifetime/clock");
        Find<GameFlowMenu>().Resume(); return Log("PASS menu pause + Editor step freezes VFX color/age and toast/clock; resumed.");
    }
    static int[] Owned()
    {
        var view = Find<GameSceneCompositionRoot>().ActionFeedback;
        var ids = view.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Action pulse" || t.name == "Cargo or fortification")
            .Select(t => t.gameObject.GetInstanceID()).ToList();
        foreach (string name in new[] { "m_Material", "m_Texture", "m_Impact", "m_Dust", "m_Ring", "m_Crate", "m_Fortify", "m_Toast" })
        {
            var obj = (UnityEngine.Object)Field(view, name); ids.Add(obj.GetInstanceID());
            if (obj is Component component) ids.Add(component.gameObject.GetInstanceID());
        }
        return ids.ToArray();
    }
    static void Remember(string key) => SessionState.SetString(key, string.Join(",", Owned()));
    static void Gone(string key) => Check(SessionState.GetString(key, "").Split(',').Select(int.Parse).All(id => EditorUtility.InstanceIDToObject(id) == null), "owned objects leaked " + key);

    public static string DestroyComponent()
    {
        Remember("E6.Owned"); var root = Find<GameSceneCompositionRoot>(); var b = Find<SwarmGameBridge>(); var view = root.ActionFeedback;
        UnityEngine.Object.DestroyImmediate(view);
        var listeners = (Delegate)typeof(SwarmGameBridge).GetField("PresentationEvent", Private).GetValue(b);
        Check(listeners == null || listeners.GetInvocationList().All(d => d.Target != (object)view), "event listener retained");
        Check(!b.Simulation.CapturePresentationImpacts && !b.Simulation.TryTakePresentationImpact(out _), "capture/buffer retained");
        return "Destroyed only E6 consumer; step then CheckDestroyAndMenu.";
    }
    public static string CheckDestroyAndMenu()
    {
        Gone("E6.Owned"); Check(Resources.Load<Shader>("E6Feedback") != null, "shared shader destroyed");
        Log("PASS component-only destroy unsubscribes, stops/clears impact capture and destroys all owned glyphs/material/texture/160views/HUD; shared shader remains.");
        Find<GameFlowMenu>().ReturnToMenu(); return "Returned to menu; step then NewMatch.";
    }
    public static string NewMatch()
    {
        Check(Find<GameSceneCompositionRoot>() == null, "menu retained root"); Find<GameFlowMenu>().Play();
        return "Requested fresh ProceduralGame; step then CheckNewMatch.";
    }
    public static string CheckNewMatch()
    {
        var root = Find<GameSceneCompositionRoot>(); Check(root.IsReady && root.ActionFeedback.IsInitialized, "reload not Ready");
        var v = root.ActionFeedback; v.RefreshPresentation(0);
        Check(v.ResearchCount == 0 && v.ImpactCount == 0 && v.GatherCount == 0 && v.DepositCount == 0 && v.BadgeCount == 0 && v.ActivePulseCount == 0 && v.Notification == "", "stale match feedback");
        Check(root.Research.State.UnlockMask == 0 && Find<SwarmGameBridge>().Simulation.CapturePresentationImpacts, "reload ownership");
        return Log("PASS new match Ready with all feedback counters/badges/toast/pulses/unlocks reset and capture re-enabled.");
    }
    public static string ActiveUnload()
    {
        Remember("E6.ActiveOwned"); var simulation = Find<SwarmGameBridge>().Simulation;
        SessionState.SetString("E6.World", simulation.World.Name);
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene"); return "Loaded legacy scene with active E6; step then CheckLegacy.";
    }
    public static string FogAndPool()
    {
        var b = Find<SwarmGameBridge>(); var root = Find<GameSceneCompositionRoot>(); var v = root.ActionFeedback;
        var fog = Find<FogOfWarManager>(); var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Worker.prefab");
        var position = Find<TownHall>().transform.position + new Vector3(-3, -2);
        var go = RuntimeObjectPool.Spawn(prefab, position, Quaternion.identity, b.gameObject.scene); b.Register(go.GetComponent<Unit>());
        int id = go.GetInstanceID(); var worker = go.GetComponent<WorkerUnit>();
        var state = b.Gameplay.EntityManager.GetComponentData<GameplayWorker>(worker.DotsEntity); state.Carried = 3;
        b.Gameplay.EntityManager.SetComponentData(worker.DotsEntity, state);
        Camera.main.transform.position = new Vector3(position.x, position.y, Camera.main.transform.position.z);
        Sync(b); v.RefreshPresentation(0); Check(v.BadgeCount == 1, "fixture carried badge missing");
        var counts = (int[,])Field(fog, "m_VisibilityCounts");
        try
        {
            typeof(FogOfWarManager).GetField("m_VisibilityCounts", Private).SetValue(fog, new int[counts.GetLength(0), counts.GetLength(1)]);
            fog.OnFogUpdated.Invoke(); v.RefreshPresentation(0); Check(v.BadgeCount == 0 && v.ActivePulseCount == 0, "hidden badge reveals unit");
        }
        finally { typeof(FogOfWarManager).GetField("m_VisibilityCounts", Private).SetValue(fog, counts); fog.OnFogUpdated.Invoke(); }
        v.RefreshPresentation(0); Check(v.BadgeCount == 1, "visible cargo not restored");
        worker.DespawnToPool(); v.RefreshPresentation(0); Check(v.BadgeCount == 0, "pool retained cargo badge");
        go = RuntimeObjectPool.Spawn(prefab, position, Quaternion.identity, b.gameObject.scene); b.Register(go.GetComponent<Unit>());
        Check(go.GetInstanceID() == id, "pool instance not reused");
        v.RefreshPresentation(0); Check(v.BadgeCount == 0, "reused worker inherited cargo badge");
        go.GetComponent<Unit>().DespawnToPool();
        return Log("PASS explicit carried-state fixture: zero current fog hides badge, restore reveals; actual pool return/reuse same instance clears cargo without stale glyph.");
    }
    public static string CheckLegacy()
    {
        Gone("E6.ActiveOwned"); Check(Find<ActionFeedbackPresentation>() == null, "legacy gained E6");
        var simulation = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SwarmSimulationSystem>();
        Check(!simulation.CapturePresentationImpacts && !simulation.TryTakePresentationImpact(out _), "scene unload retained capture/impacts");
        return Log("PASS direct unload with active consumer destroys owned objects and capture; SampleScene has no E6.");
    }

    public static string GpuProbe()
    {
        var go = new GameObject("E6 GPU fixture"); var camera = go.AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 2; camera.transform.position = new Vector3(0, 0, -10);
        camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var image = new Texture2D(128, 128, TextureFormat.RGB24, false);
        var white = new Texture2D(4, 4); white.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray()); white.Apply();
        var sprite = Sprite.Create(white, new Rect(0, 0, 4, 4), Vector2.one * .5f, 2, 0, SpriteMeshType.FullRect);
        var glyph = new GameObject("Tinted E6 glyph"); glyph.layer = 31; var sr = glyph.AddComponent<SpriteRenderer>(); sr.sprite = sprite;
        var material = new Material(Resources.Load<Shader>("E6Feedback")); sr.sharedMaterial = material;
        var fog = new Texture2D(2, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        fog.SetPixels(new[] { Color.black, Color.white }); fog.Apply();
        var rt = RenderTexture.GetTemporary(128, 128, 24, RenderTextureFormat.ARGB32);
        void Render(string name)
        {
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = rt });
            var old = RenderTexture.active; RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply(); RenderTexture.active = old;
            Directory.CreateDirectory(Dir); File.WriteAllBytes(Dir + name + ".png", image.EncodeToPNG());
        }
        try
        {
            sr.color = new Color(.45f, .85f, 1); Render("gpu-friendly"); var blue = image.GetPixel(64, 64);
            sr.color = new Color(1, .4f, .22f); Render("gpu-hostile"); var red = image.GetPixel(64, 64);
            Check(blue.b > blue.r + .2f && red.r > red.b + .2f, "SpriteRenderer faction tint missing");
            sr.color = new Color(1, .4f, .22f, .2f); Render("gpu-fade"); var fade = image.GetPixel(64, 64);
            Check(fade.r < red.r - .15f && fade.r > .1f, "SpriteRenderer alpha/lifetime missing");
            sr.color = Color.white; material.SetTexture("_FeedbackFog", fog); material.SetVector("_FeedbackFogBounds", new Vector4(-2, -2, 4, 4));
            material.SetFloat("_FeedbackFogEnabled", 1); Render("gpu-fog");
            Check(image.GetPixel(48, 64).maxColorComponent < .05f && image.GetPixel(80, 64).r > .9f, "per-pixel visibility mask");
            var result = $"PASS actual E6 shader/Renderer2D128x128: friendly={blue}, hostile={red}, fade={fade}; left fog invisible/right visible.\n";
            Save("gpu-probe.txt", result); return result;
        }
        finally
        {
            RenderTexture.ReleaseTemporary(rt);
            foreach (var obj in new UnityEngine.Object[] { go, glyph, image, material, sprite, white, fog }) UnityEngine.Object.DestroyImmediate(obj);
        }
    }
    public static string FortifySetup()
    {
        var b = Find<SwarmGameBridge>(); b.Simulation.Paused = true;
        var state = b.Gameplay.Research; state.UnlockMask = 63; b.Gameplay.EntityManager.SetComponentData(b.Gameplay.WalletEntity, state);
        var tower = b.SpawnGameplay(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Buildings/Tower.prefab"),
            Find<TownHall>().transform.position + new Vector3(3, -2), Quaternion.identity).GetComponent<TowerUnit>();
        SessionState.SetInt("E6.Tower", tower.GetInstanceID());
        var fog = Find<FogOfWarManager>(); if (!fog.IsDisabled) Call(fog, "ToggleFogOfWar");
        Camera.main.transform.position = new Vector3(tower.transform.position.x, tower.transform.position.y + .3f, Camera.main.transform.position.z);
        Camera.main.orthographicSize = 2;
        Find<GameSceneCompositionRoot>().ActionFeedback.RefreshPresentation(0);
        return "Explicit unlocked-state visual probe; real Tower spawned. Step then FortifyCapture.";
    }
    public static string FortifyCapture()
    {
        var root = Find<GameSceneCompositionRoot>(); var view = root.ActionFeedback; view.RefreshPresentation(0);
        var badge = ((SpriteRenderer[])Field(view, "m_Badges")).First(v => v.enabled);
        Check(badge.sprite == (Sprite)Field(view, "m_Fortify"), "Fortify sprite missing");
        Capture("fortify-close");
        var result = $"PASS explicit unlocked-state/real Tower visual probe after Editor frame: badgeCount={view.BadgeCount}, badgePosition={badge.transform.position}, color={badge.color}, visible={badge.isVisible}.\n";
        Save("fortify-visual.txt", result); return result;
    }
}
