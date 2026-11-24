using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class GameExperienceE4Capture
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Dir = "Docs/Modules/GameExperience/Evidence/E4/";
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj, args);
    static void EnableDetails(ProceduralMapPresentation p, bool enabled)
    {
        typeof(ProceduralMapPresentation).GetField("m_EnableDaytimeDetail", Private).SetValue(p, enabled);
        Call(p, "PaintGround"); Call(p, "RefreshVisibility");
        if (enabled) Call(p, "UpdateAmbient", 0f);
        else foreach (var renderer in p.Decorations.Where(r => r.name.StartsWith("E4 "))) renderer.enabled = false;
    }

    public static string Run()
    {
        if (!EditorApplication.isPlaying || !EditorApplication.isPaused || !Find<GameSceneCompositionRoot>().IsReady) throw new Exception("Ready paused match required");
        Directory.CreateDirectory(Dir);
        var p = Find<ProceduralMapPresentation>(); var a = Find<ProceduralMapAuthoring>(); var fog = Find<FogOfWarManager>();
        var camera = Camera.main; var controls = Find<CameraController>();
        var position = camera.transform.position; float size = camera.orthographicSize, aspect = camera.aspect;
        bool locked = controls.LockCamera, originalFog = fog.IsDisabled, originalEnabled = p.enabled;
        var mapHash = GameExperienceE4Validation.MapHash(a.GeneratedMap);
        var log = new System.Text.StringBuilder("E4 camera PNG1920x1080: seed1, Day, frozen gameplay. A/B differs only E4 ground/detail visibility. Bush frame-crop fix retained in both. Fog-off world comparisons are explicitly debug views; not player discovery evidence.\n");
        try
        {
            controls.LockCamera = true; p.enabled = false; camera.aspect = 16f / 9;
            var unknown = a.GeneratedMap.Layout.Regions[2];
            camera.transform.position = World(unknown.X, unknown.Y); camera.orthographicSize = 14;
            if (fog.IsDisabled) Call(fog, "ToggleFogOfWar");
            Call(p, "RefreshVisibility"); Call(p, "UpdateAmbient", 0f);
            Save(camera, Dir + "candidate-GoldFrontier-unexplored-fog-on.png");
            if (!fog.IsDisabled) Call(fog, "ToggleFogOfWar");
            string[] names = { "Start", "Economy", "GoldFrontier" };
            for (int region = 0; region < 3; region++)
            {
                var area = a.GeneratedMap.Layout.Regions[region];
                camera.transform.position = World(area.X, area.Y); camera.orthographicSize = region == 0 ? 10 : 14;
                foreach (bool candidate in new[] { false, true })
                {
                    EnableDetails(p, candidate);
                    Save(camera, Dir + (candidate ? "candidate-" : "baseline-") + names[region] + "-Day-fog-off.png");
                }
                log.AppendLine($"{names[region]}: center=({area.X},{area.Y}), ortho={camera.orthographicSize}, both views same gameplay/camera/clock; fog disabled.");
            }
            EnableDetails(p, true);
            foreach (string type in new[] { "butterfly", "ripple" })
            {
                var target = p.Decorations.First(r => r.name.Contains(type));
                camera.transform.position = new Vector3(target.transform.position.x, target.transform.position.y, -10); camera.orthographicSize = 4;
                foreach (float t in new[] { 0f, .8f })
                {
                    Call(p, "UpdateAmbient", t); Save(camera, Dir + $"candidate-{type}-sample-{(t == 0 ? "A" : "B")}-fog-off.png");
                }
                log.AppendLine($"{type}: camera frozen ortho4, presentation animation sampled at0 and0.8; no gameplay ticks/clock injection.");
            }
            if (GameExperienceE4Validation.MapHash(a.GeneratedMap) != mapHash) throw new Exception("Capture changed map");
            log.AppendLine("PASS capture map hash unchanged: " + mapHash);
            File.WriteAllText(Dir + "capture-context.txt", log.ToString());
        }
        finally
        {
            EnableDetails(p, true);
            if (fog.IsDisabled != originalFog) Call(fog, "ToggleFogOfWar");
            Call(p, "RefreshVisibility"); Call(p, "LateUpdate"); p.enabled = originalEnabled;
            camera.transform.position = position; camera.orthographicSize = size; camera.aspect = aspect; controls.LockCamera = locked;
        }
        return log.ToString();
    }

    static Vector3 World(int x, int y)
    {
        var a = Find<ProceduralMapAuthoring>(); var p = Find<TilemapManager>().WalkableTilemap.GetCellCenterWorld(a.OriginCell + new Vector3Int(x, y, 0));
        return new Vector3(p.x, p.y, -10);
    }

    static void Save(Camera camera, string path)
    {
        var renderer = Find<RTS.DOTS.SwarmInstancedRenderer>(); renderer.SendMessage("LateUpdate");
        var target = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active; var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(image); }
    }
}
