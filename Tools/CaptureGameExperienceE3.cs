using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

// Starts a bounded Editor callback; clock/gameplay run normally at timeScale1, without phase injection.
public static class GameExperienceE3Capture
{
    const string Dir = "Docs/Modules/GameExperience/Evidence/E3/";
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();

    public static string SetupKnownFixture()
    {
        if (!EditorApplication.isPaused || !EditorApplication.isPlaying) throw new Exception("Pause required");
        var fog = Find<FogOfWarManager>(); var map = Find<ProceduralMapAuthoring>();
        var gate = map.GeneratedMap.Layout.Gateways[0]; var cell = map.OriginCell + new Vector3Int(gate.X, gate.Y, 0);
        var explored = (bool[,])typeof(FogOfWarManager).GetField("m_Explored", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fog);
        explored[cell.x - fog.Bounds.xMin, cell.y - fog.Bounds.yMin] = true;
        Find<DuskWarningPresentation>().RefreshPresentation();
        return "Fixture reveals gateway0 cell only; step once then CaptureKnownFixture. Not player exploration evidence.";
    }

    public static string CaptureKnownFixture()
    {
        var w = Find<DuskWarningPresentation>();
        if (w.KnownApproachCount != 1) throw new Exception("Exactly one known fixture marker required");
        SaveGameView(EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")), Dir + "fixture-one-known-gateway.png");
        File.WriteAllText(Dir + "known-marker-fixture.txt", "PASS one known gateway marker; other gateway remains hidden. Fog memory injected by fixture, not actual scouting.\n" + w.WarningText);
        return "Saved one-known-gateway UI fixture PNG; exit Play to discard fog injection.";
    }

    public static string StartNatural()
    {
        var root = Find<GameSceneCompositionRoot>(); var clock = Find<DayNightCycleManager>(); var warning = root.DuskWarning;
        if (!EditorApplication.isPlaying || !EditorApplication.isPaused || !root.IsReady
            || clock.CurrentPhase != DayNightPhase.Day || clock.CurrentPhaseProgress > 0.01f || warning.DuskSignalCount != 0)
            throw new Exception("Fresh paused Day match required for natural capture");
        Directory.CreateDirectory(Dir);
        var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")); gameView.Show();
        var log = new StringBuilder("Natural clock capture: no phase/progress/definition injection; Editor GameView, fog on.\n");
        bool oldBackground = Application.runInBackground; Application.runInBackground = true;
        double start = EditorApplication.timeSinceStartup, pauseAt = 0, pausedProgress = 0;
        int pausedSeconds = 0, pausedSignals = 0, stage = 0;
        EditorApplication.CallbackFunction update = null;
        Action<string> snapshot = name =>
        {
            warning.RefreshPresentation();
            SaveGameView(gameView, Dir + name + ".png");
            log.AppendLine($"{name}: gameTime={Time.timeAsDouble:F4}, phase={clock.CurrentPhase}, progress={clock.CurrentPhaseProgress:F6}, countdown={warning.SecondsUntilNight}, signals={warning.DuskSignalCount}, knownGateways={warning.KnownApproachCount}, lighting={root.NightLighting.Strength:F4}, timeScale={Time.timeScale}");
            File.WriteAllText(Dir + "natural-capture.txt", log.ToString());
        };
        update = () =>
        {
            try
            {
                if (!EditorApplication.isPlaying || clock == null || EditorApplication.timeSinceStartup - start > 125)
                    throw new Exception("Natural capture stopped or exceeded125s");
                if (stage == 0 && clock.CurrentPhase == DayNightPhase.Day && clock.CurrentPhaseProgress > 0.97f)
                { snapshot("natural-Day-before-Dusk"); stage++; }
                else if (stage == 1 && clock.CurrentPhase == DayNightPhase.Dusk && clock.CurrentPhaseProgress > 0.01f)
                { snapshot("natural-Dusk-entry"); stage++; }
                else if (stage == 2 && clock.CurrentPhase == DayNightPhase.Dusk && clock.CurrentPhaseProgress > 0.25f)
                {
                    Find<GameFlowMenu>().Pause(); pauseAt = EditorApplication.timeSinceStartup;
                    pausedProgress = clock.CurrentPhaseProgress; pausedSeconds = warning.SecondsUntilNight; pausedSignals = warning.DuskSignalCount; stage++;
                }
                else if (stage == 3 && EditorApplication.timeSinceStartup - pauseAt > 2)
                {
                    if (Time.timeScale != 0 || clock.CurrentPhaseProgress != pausedProgress || warning.SecondsUntilNight != pausedSeconds || warning.DuskSignalCount != pausedSignals)
                        throw new Exception("Real menu pause changed countdown/cue");
                    Find<GameFlowMenu>().Resume(); log.AppendLine("PASS real menu pause2s: progress/countdown/cue unchanged, resumed without a second signal."); stage++;
                }
                else if (stage == 4 && clock.CurrentPhase == DayNightPhase.Dusk && clock.CurrentPhaseProgress > 0.5f)
                { snapshot("natural-Dusk-mid"); stage++; }
                else if (stage == 5 && clock.CurrentPhase == DayNightPhase.Night && clock.CurrentPhaseProgress > 0.01f)
                {
                    snapshot("natural-Night-entry");
                    if (warning.DuskSignalCount != 1 || warning.SecondsUntilNight != 0 || Find<FogOfWarManager>().IsDisabled
                        || clock.Definition.DayDuration != 60 || clock.Definition.DuskDuration != 30 || clock.Definition.NightDuration != 45)
                        throw new Exception("Natural transition invariants failed");
                    log.AppendLine("PASS natural Day60→Dusk30→Night; one Dusk signal, countdown follows clock; timings unchanged. Capture ends at Night entry, not a full45s Night.");
                    File.WriteAllText(Dir + "natural-capture.txt", log.ToString());
                    EditorApplication.update -= update; Application.runInBackground = oldBackground; EditorApplication.isPaused = true;
                }
            }
            catch (Exception error)
            {
                log.AppendLine("FAIL: " + error); File.WriteAllText(Dir + "natural-capture.txt", log.ToString());
                EditorApplication.update -= update; Application.runInBackground = oldBackground; EditorApplication.isPaused = true;
                Debug.LogException(error);
            }
        };
        EditorApplication.update += update; EditorApplication.isPaused = false;
        return "Natural capture armed: Day60+Dusk30, one real menu pause2s, four GameView PNGs, stop paused at Night entry (<=125s).";
    }

    static void SaveGameView(EditorWindow view, string path)
    {
        view.Repaint();
        var render = (RenderTexture)view.GetType().GetField("m_RenderTexture", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        if (render == null) throw new Exception("GameView render texture unavailable");
        var previous = RenderTexture.active;
        var image = new Texture2D(render.width, render.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = render; image.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); image.Apply();
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                var pixels = image.GetPixels32();
                for (int y = 0; y < render.height / 2; y++)
                    for (int x = 0; x < render.width; x++)
                    { int a = y * render.width + x, b = (render.height - 1 - y) * render.width + x; var p = pixels[a]; pixels[a] = pixels[b]; pixels[b] = p; }
                image.SetPixels32(pixels); image.Apply();
            }
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
    }
}
