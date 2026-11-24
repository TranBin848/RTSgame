// script-execute in a fresh, Editor-paused ProceduralGame. No UI input or gameplay ticks.
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class GameExperienceE1Validation
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool value, string message) { if (!value) throw new Exception("E1: " + message); }
    static void Apply(DayNightCycleManager day, DayNightPhase phase) => typeof(DayNightCycleManager)
        .GetMethod("ApplyInstantEnvironmentEffects", Private).Invoke(day, new object[] { phase });

    public static string Run()
    {
        Check(Application.isPlaying && UnityEditor.EditorApplication.isPaused, "run in paused Play Mode");
        var day = Find<DayNightCycleManager>(); var root = Find<GameSceneCompositionRoot>();
        var game = Find<GameManager>(); var map = Find<ProceduralMapAuthoring>(); var nav = Find<TilemapManager>();
        var bridge = Find<SwarmGameBridge>(); var fog = Find<FogOfWarManager>();
        Check(root.IsReady && day.Presentation != null, "candidate not composed");
        var config = day.Presentation;
        var terrain = map.GeneratedMap.Terrain.ToArray(); var reserved = map.GeneratedMap.Reserved.ToArray();
        var resources = map.GeneratedMap.Resources.ToArray();
        var wallet = (game.Wood, game.Gold, game.Meat); int version = nav.NavigationVersion;
        var phase = day.CurrentPhase; var progress = day.CurrentPhaseProgress; int number = day.CurrentDay;
        var random = JsonUtility.ToJson(UnityEngine.Random.state);
        long ticks = bridge.Simulation.TickCount;
        int actors = bridge.Gameplay.ActorCount;
        var volume = (Volume)typeof(DayNightCycleManager).GetField("m_PostProcessingVolume", Private).GetValue(day);
        string sharedBefore = UnityEditor.EditorJsonUtility.ToJson(volume.sharedProfile);
        string[] componentsBefore = volume.sharedProfile.components.Select(c => UnityEditor.EditorJsonUtility.ToJson(c)).ToArray();
        Check(volume.profile != volume.sharedProfile && volume.profile.components.Zip(volume.sharedProfile.components,
            (a, b) => a != b).All(v => v), "volume overrides must be cloned independently");
        day.InitializePresentation(config, Camera.main); day.InitializePresentation(config, Camera.main);
        foreach (DayNightPhase p in Enum.GetValues(typeof(DayNightPhase)))
        {
            var next = p == DayNightPhase.Day ? DayNightPhase.Dusk : p == DayNightPhase.Dusk ? DayNightPhase.Night : DayNightPhase.Day;
            var a = config.Evaluate(p, 1); var b = config.Evaluate(next, 0);
            Check(Vector4.Distance(a.Filter, b.Filter) < .0001f && Mathf.Abs(a.Intensity - b.Intensity) < .0001f
                && Mathf.Abs(a.Vignette - b.Vignette) < .0001f, "discontinuity at phase boundary");
            var start = config.Evaluate(p, 0); var hold = config.Evaluate(p, .5f);
            Check(start.Filter == hold.Filter && start.Intensity == hold.Intensity, "palette faded before transition window");
            foreach (var t in new[] { float.NaN, float.PositiveInfinity, -1, 0, .9f, 1, 2 })
                Check(float.IsFinite(config.Evaluate(p, t).Intensity), "non-finite sample");
            Apply(day, p);
            volume.profile.TryGet<ColorAdjustments>(out var color);
            volume.profile.TryGet<Vignette>(out var vignette);
            Check(color.colorFilter.overrideState && color.postExposure.overrideState && vignette.intensity.overrideState,
                "grading overrides inactive");
            Check(Mathf.Abs(Mathf.Pow(2, color.postExposure.value) - start.Intensity) < .0001f, "intensity/exposure mismatch");
            Check(day.EnvironmentTint == Color.white, "instances would receive a second environment tint");
        }
        var invalid = UnityEngine.Object.Instantiate(config);
        try
        {
            invalid.Night.Intensity = float.NaN;
            bool rejected = false; try { invalid.Validate(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "invalid asset accepted");
            rejected = false; try { day.InitializePresentation(invalid, Camera.main); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "different palette silently rebound");
        }
        finally { UnityEngine.Object.DestroyImmediate(invalid); }
        Apply(day, phase);
        Check(sharedBefore == UnityEditor.EditorJsonUtility.ToJson(volume.sharedProfile)
            && componentsBefore.SequenceEqual(volume.sharedProfile.components.Select(c => UnityEditor.EditorJsonUtility.ToJson(c))), "shared profile mutated");
        Check(terrain.SequenceEqual(map.GeneratedMap.Terrain) && reserved.SequenceEqual(map.GeneratedMap.Reserved)
            && resources.SequenceEqual(map.GeneratedMap.Resources) && version == nav.NavigationVersion, "map/navigation changed");
        Check(wallet == (game.Wood, game.Gold, game.Meat) && actors == bridge.Gameplay.ActorCount
            && ticks == bridge.Simulation.TickCount, "presentation changed simulation/wallet");
        Check(day.CurrentPhase == phase && day.CurrentPhaseProgress == progress && day.CurrentDay == number
            && day.Definition.DayDuration == 60 && day.Definition.DuskDuration == 30 && day.Definition.NightDuration == 45, "clock/timing changed");
        Check(random == JsonUtility.ToJson(UnityEngine.Random.state), "presentation consumed gameplay RNG");
        Check(!fog.IsDisabled && !Find<ProceduralMapPresentation>().IsSiteDiscovered(1)
            && !Find<ProceduralMapPresentation>().IsSiteDiscovered(2), "unexplored sites leaked");
        return "PASS E1: composition/repeat init; independent volume components/shared profile unchanged; three phase boundaries continuous; hold/NaN/clamp; override/exposure; one grading path for instances; invalid/rebind rejected; map/resources/navigation/wallet/actors/tick/RNG/clock unchanged; timing60/30/45; unknown sites remain hidden.";
    }

    public static string Capture()
    {
        Check(Application.isPlaying && UnityEditor.EditorApplication.isPaused, "pause before capture");
        var camera = Camera.main; var day = Find<DayNightCycleManager>(); var fog = Find<FogOfWarManager>();
        var config = day.Presentation; var phase = day.CurrentPhase;
        var configField = typeof(DayNightCycleManager).GetField("m_Presentation", Private);
        var position = camera.transform.position; float size = camera.orthographicSize; float aspect = camera.aspect;
        var hall = Find<TownHall>().transform.position;
        var renderer = Find<RTS.DOTS.SwarmInstancedRenderer>();
        bool fogWasDisabled = fog.IsDisabled;
        var toggle = typeof(FogOfWarManager).GetMethod("ToggleFogOfWar", Private);
        const string directory = "Docs/Modules/GameExperience/Evidence/E1/";
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            camera.transform.position = new Vector3(hall.x + 1.5f, hall.y + 1, -10);
            camera.orthographicSize = 8; camera.aspect = 16f / 9;
            if (!fogWasDisabled) toggle.Invoke(fog, null);
            foreach (bool candidate in new[] { false, true })
            {
                configField.SetValue(day, candidate ? config : null);
                foreach (DayNightPhase p in Enum.GetValues(typeof(DayNightPhase)))
                {
                    Apply(day, p); renderer.EnvironmentColor = day.EnvironmentTint;
                    renderer.SendMessage("LateUpdate");
                    Save(camera, directory + (candidate ? "candidate-" : "baseline-") + p + "-fog-off.png");
                }
            }
            if (!fogWasDisabled) toggle.Invoke(fog, null);
            Save(camera, directory + "candidate-Night-fog-on.png");
        }
        finally
        {
            configField.SetValue(day, config); Apply(day, phase); renderer.EnvironmentColor = day.EnvironmentTint;
            if (fog.IsDisabled != fogWasDisabled) toggle.Invoke(fog, null);
            camera.transform.position = position; camera.orthographicSize = size; camera.aspect = aspect;
        }
        return "Captured raw camera PNGs1920x1080: baseline/candidate Day,Dusk,Night same frozen seed/camera/state with fog disabled for color comparison; candidate Night with original fog. Presentation probes apply colors only, no clock phase event or gameplay tick. No HUD in camera captures.";
    }

    static void Save(Camera camera, string path)
    {
        var target = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            // StandardRequest updates the volume stack; SingleCameraRequest reuses the previous stack.
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
    }
}
