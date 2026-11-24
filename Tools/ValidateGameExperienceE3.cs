using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

// Run via script-execute in a fresh Ready, Editor-paused match; exit Play after the probe.
public static class GameExperienceE3Validation
{
    const string Dir = "Docs/Modules/GameExperience/Evidence/E3/";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool value, string message) { if (!value) throw new Exception("E3: " + message); }
    static void Phase(DayNightPhase phase) => typeof(DayNightCycleManager).GetMethod("SetPhase", Private).Invoke(Find<DayNightCycleManager>(), new object[] { phase, false });
    static void Save(string file, string value) { System.IO.Directory.CreateDirectory(Dir); System.IO.File.WriteAllText(Dir + file, value); }

    public static string Run()
    {
        var root = Find<GameSceneCompositionRoot>(); var warning = root.DuskWarning;
        Check(Application.isPlaying && EditorApplication.isPaused && root.IsReady, "fresh Ready paused match required");
        var clock = Find<DayNightCycleManager>(); var waves = Find<EnemyWaveManager>();
        var fog = Find<FogOfWarManager>(); var map = Find<ProceduralMapAuthoring>(); var game = Find<GameManager>();
        var bridge = Find<SwarmGameBridge>(); var camera = Camera.main;
        var rng = JsonUtility.ToJson(UnityEngine.Random.state); var terrain = map.GeneratedMap.Terrain.ToArray();
        var wallet = (game.Wood, game.Gold, game.Meat); int actors = bridge.Gameplay.ActorCount;
        var ticks = bridge.Simulation.TickCount; int nav = Find<TilemapManager>().NavigationVersion;
        var canvas = warning.GetComponentInParent<Canvas>();
        int listeners = clock.OnPhaseChanged.GetInvocationList().Count(x => x.Target == warning);
        Check(listeners == 1, "one listener");
        root.Initialize(); warning.Initialize(clock, waves, map, fog, camera, canvas);
        Check(clock.OnPhaseChanged.GetInvocationList().Count(x => x.Target == warning) == listeners, "repeat init duplicates listener");
        Check(clock.Definition.DayDuration == 60 && clock.Definition.DuskDuration == 30 && clock.Definition.NightDuration == 45, "timing changed");
        float volume = AudioListener.volume;
        AudioListener.volume = 0;
        Phase(DayNightPhase.Dusk); warning.RefreshPresentation();
        Check(warning.DuskSignalCount == 1 && warning.SecondsUntilNight == 30, "Dusk once/countdown30");
        Check(warning.WarningText.Contains("DUSK") && warning.WarningText.Contains("unknown") && warning.KnownApproachCount == 0, "muted generic warning / hidden gateway");
        clock.OnPhaseChanged.Invoke(DayNightPhase.Dusk);
        warning.enabled = false; warning.enabled = true;
        warning.Initialize(clock, waves, map, fog, camera, canvas);
        Check(warning.DuskSignalCount == 1, "duplicate event or reopen rings twice");
        var bell = (AudioClip)typeof(DuskWarningPresentation).GetField("m_Bell", Private).GetValue(warning);
        float[] samples = new float[bell.samples]; bell.GetData(samples, 0);
        Check(samples.Any(x => Mathf.Abs(x) > 0.2f) && samples.All(x => !float.IsNaN(x) && Mathf.Abs(x) <= 1), "bell is silent/clipped/invalid");
        var menu = Find<GameFlowMenu>(); menu.Pause();
        for (int i = 0; i < 10; i++) typeof(DayNightCycleManager).GetMethod("Tick", Private).Invoke(clock, new object[] { 0f });
        warning.RefreshPresentation();
        Check(Time.timeScale == 0 && warning.SecondsUntilNight == 30 && warning.DuskSignalCount == 1, "pause changes clock/cue");
        menu.Resume();
        foreach (float elapsed in new[] { 0.01f, 7.5f, 15f, 29.9f })
        {
            typeof(DayNightCycleManager).GetField("m_PhaseTimer", Private).SetValue(clock, elapsed);
            typeof(DayNightCycleManager).GetProperty("CurrentPhaseProgress").SetValue(clock, elapsed / 30);
            warning.RefreshPresentation();
            Check(warning.SecondsUntilNight == Mathf.CeilToInt(30 - elapsed), "countdown differs from clock");
        }
        var explored = (bool[,])typeof(FogOfWarManager).GetField("m_Explored", Private).GetValue(fog);
        var gate = map.GeneratedMap.Layout.Gateways[0]; var cell = map.OriginCell + new Vector3Int(gate.X, gate.Y, 0);
        int x = cell.x - fog.Bounds.xMin, y = cell.y - fog.Bounds.yMin;
        explored[x, y] = true; warning.RefreshPresentation();
        Check(warning.KnownApproachCount == 1 && warning.WarningText.Contains("possible") && warning.WarningText.Contains("Routes chosen on spawn"), "discovered route overpromises");
        var markers = (RectTransform[])typeof(DuskWarningPresentation).GetField("m_Markers", Private).GetValue(warning);
        Check(markers.Count(m => m.gameObject.activeSelf) == 1, "unknown markers leaked");
        camera.transform.position += new Vector3(20, 10, 0); warning.RefreshPresentation();
        Check(markers[0].anchoredPosition.x >= -canvas.GetComponent<RectTransform>().rect.width / 2, "marker outside HUD");
        explored[x, y] = false; warning.RefreshPresentation(); Check(warning.KnownApproachCount == 0, "fog reset retains marker");
        Phase(DayNightPhase.Night); warning.RefreshPresentation(); Check(warning.SecondsUntilNight == 0 && warning.DuskSignalCount == 1, "Night repeats Dusk");
        Phase(DayNightPhase.Day); warning.RefreshPresentation(); Check(markers.All(m => !m.gameObject.activeSelf), "Day retains markers");
        Phase(DayNightPhase.Dusk); Check(warning.DuskSignalCount == 2, "next Dusk missing cue");
        AudioListener.volume = volume;
        Check(rng == JsonUtility.ToJson(UnityEngine.Random.state), "presentation consumes gameplay RNG");
        Check(terrain.SequenceEqual(map.GeneratedMap.Terrain) && nav == Find<TilemapManager>().NavigationVersion
            && wallet == (game.Wood, game.Gold, game.Meat) && actors == bridge.Gameplay.ActorCount && ticks == bridge.Simulation.TickCount, "presentation changed gameplay");
        string result = "PASS E3: one listener/repeat init; one signal per Dusk/duplicate event/disable-enable; muted text and nonzero finite bell; pause/resume; countdown30,30,23,15,1; unexplored hidden, one discovered gateway marker, fog reset; second cycle signal; timing60/30/45; gameplay RNG/terrain/navigation/wallet/actor/tick unchanged. Fixture sets phases/progress explicitly; not a natural capture or listening test.";
        Save("integration-result.txt", result); return result;
    }

    public static string ReturnToMenu()
    {
        var w = Find<DuskWarningPresentation>(); var clock = Find<DayNightCycleManager>();
        var ids = new System.Collections.Generic.List<int> { w.GetInstanceID(), w.GetComponent<AudioSource>().GetInstanceID() };
        foreach (string field in new[] { "m_Bell", "m_Banner" }) ids.Add(((UnityEngine.Object)typeof(DuskWarningPresentation).GetField(field, Private).GetValue(w)).GetInstanceID());
        foreach (var m in (RectTransform[])typeof(DuskWarningPresentation).GetField("m_Markers", Private).GetValue(w)) ids.Add(m.gameObject.GetInstanceID());
        SessionState.SetString("E3.OwnedIds", string.Join(",", ids));
        UnityEngine.Object.DestroyImmediate(w);
        Check(clock.OnPhaseChanged.GetInvocationList().All(x => x.Target != w), "destroy retains listener");
        Find<GameFlowMenu>().ReturnToMenu(); return "Unsubscribed controller and loading MainMenu";
    }

    public static string MenuAndReload()
    {
        Check(SessionState.GetString("E3.OwnedIds", "").Split(',').Select(int.Parse).All(id => EditorUtility.InstanceIDToObject(id) == null), "owned object survived unload");
        Check(Find<DuskWarningPresentation>() == null && !ProceduralMapRuntime.IsActive, "old context retained");
        Find<GameFlowMenu>().Play(); return "PASS old controller/listener/audio/clip/banner/markers cleanup; loading new match";
    }

    public static string CheckReload()
    {
        var r = Find<GameSceneCompositionRoot>(); var c = Find<DayNightCycleManager>(); var w = r.DuskWarning;
        Check(r.IsReady && w.DuskSignalCount == 0 && c.CurrentPhase == DayNightPhase.Day && w.KnownApproachCount == 0
            && FindObjectsCount() == 1 && c.OnPhaseChanged.GetInvocationList().Count(x => x.Target == w) == 1, "reload not fresh");
        Phase(DayNightPhase.Dusk); Check(w.DuskSignalCount == 1, "new match cue count");
        string result = "PASS E3 destroy unsubscribes; menu destroys all owned IDs; new match one controller/one listener/signal reset0/unknown gateways reset/Day start; entering Dusk signals once.";
        Save("lifecycle-result.txt", result); return result;
    }
    static int FindObjectsCount() => UnityEngine.Object.FindObjectsByType<DuskWarningPresentation>(FindObjectsSortMode.None).Length;

    public static string CheckLegacySample()
    {
        Check(Find<DuskWarningPresentation>() == null && Find<DayNightCycleManager>().Presentation == null, "E3 leaked into legacy SampleScene");
        const string result = "PASS SampleScene legacy: no E3 controller and no procedural palette; procedural-only warning does not replace legacy HUD.";
        Save("legacy-result.txt", result); return result;
    }
}
