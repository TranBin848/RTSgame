// script-execute in a fresh, Editor-paused ProceduralGame. Test prefabs are explicitly bound; exit Play afterwards.
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public static class GameExperienceE2Validation
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool value, string message) { if (!value) throw new Exception("E2: " + message); }
    static void Refresh()
    {
        var lighting = Find<StructureNightLighting>();
        typeof(StructureNightLighting).GetField("m_NextRegistryRefresh", Private).SetValue(lighting, -1f);
        lighting.RefreshPresentation();
    }
    static void Phase(DayNightPhase phase, float progress = 0)
    {
        var clock = Find<DayNightCycleManager>();
        // A presentation probe: no wave/reward/phase event is fired by the fixture.
        typeof(DayNightCycleManager).GetProperty("CurrentPhase").SetValue(clock, phase);
        typeof(DayNightCycleManager).GetProperty("CurrentPhaseProgress").SetValue(clock, progress);
        clock.SendMessage("UpdateEnvironmentEffects"); Refresh();
    }
    static StructureUnit Spawn(string name, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Buildings/" + name + ".prefab");
        var go = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        var structure = go.GetComponent<StructureUnit>(); Find<SwarmGameBridge>().Register(structure);
        // Direct fixture binding does not certify construction or research gates.
        return structure;
    }
    static void Fog(bool disabled)
    {
        var fog = Find<FogOfWarManager>();
        if (fog.IsDisabled != disabled) typeof(FogOfWarManager).GetMethod("ToggleFogOfWar", Private).Invoke(fog, null);
    }

    public static string Run()
    {
        Check(Application.isPlaying && EditorApplication.isPaused && Find<GameSceneCompositionRoot>().IsReady, "fresh Ready paused match required");
        var root = Find<GameSceneCompositionRoot>(); var lighting = root.NightLighting;
        var clock = Find<DayNightCycleManager>(); var fog = Find<FogOfWarManager>(); var game = Find<GameManager>();
        var camera = Camera.main; var map = Find<TilemapManager>(); var bridge = Find<SwarmGameBridge>();
        var presentation = Find<ProceduralMapPresentation>(); var definition = clock.Presentation;
        var originalPhase = clock.CurrentPhase; float originalProgress = clock.CurrentPhaseProgress;
        var originalCamera = camera.transform.position; float originalSize = camera.orthographicSize;
        var terrain = Find<ProceduralMapAuthoring>().GeneratedMap.Terrain.ToArray();
        int nav = map.NavigationVersion, actors = bridge.Gameplay.ActorCount;
        var wallet = (game.Wood, game.Gold, game.Meat); var random = JsonUtility.ToJson(UnityEngine.Random.state);
        var hall = Find<TownHall>();
        camera.transform.position = new Vector3(hall.transform.position.x, hall.transform.position.y, -10); camera.orthographicSize = 10;
        lighting.Initialize(game, clock, fog, map, camera, definition, bridge.GetComponent<RTS.DOTS.SwarmInstancedRenderer>());
        lighting.Initialize(game, clock, fog, map, camera, definition, bridge.GetComponent<RTS.DOTS.SwarmInstancedRenderer>());
        Phase(DayNightPhase.Day); Check(lighting.ActiveLocalLights == 0 && lighting.ActiveGlowGroups == 0, "day lights active");
        Phase(DayNightPhase.Dusk, .5f); Check(lighting.Strength > 0 && lighting.Strength < 1, "dusk did not ramp");
        Phase(DayNightPhase.Night); Check(lighting.ActiveLocalLights >= 1 && lighting.ActiveGlowGroups >= 1, "Hall night lighting missing");
        var group = hall.transform.Find("Night light presentation"); Check(group != null, "Hall light group missing");
        var glows = group.GetComponentsInChildren<SpriteRenderer>(); var colors = glows.Select(g => g.color).ToArray();
        lighting.RefreshPresentation(); Check(colors.SequenceEqual(glows.Select(g => g.color)), "paused flicker changed");
        Check(group.GetComponentsInChildren<Collider2D>().Length == 0 && group.GetComponentsInChildren<Unit>().Length == 0, "lighting added gameplay components");
        lighting.enabled = false; Check(!group.gameObject.activeSelf && lighting.ActiveLocalLights == 0, "disable left lights on");
        lighting.enabled = true; Refresh(); Check(group.gameObject.activeSelf, "re-enable failed");
        camera.transform.position += Vector3.right * 1000; Refresh(); Check(lighting.ActiveLocalLights == 0 && lighting.ActiveGlowGroups == 0, "off-camera lights active");
        camera.transform.position = new Vector3(hall.transform.position.x, hall.transform.position.y, -10);
        Phase(DayNightPhase.Night, 1); Check(lighting.Strength == 0 && lighting.ActiveGlowGroups == 0, "dawn did not fade");
        Phase(originalPhase, originalProgress); camera.transform.position = originalCamera; camera.orthographicSize = originalSize;
        Check(terrain.SequenceEqual(Find<ProceduralMapAuthoring>().GeneratedMap.Terrain) && nav == map.NavigationVersion
            && actors == bridge.Gameplay.ActorCount && wallet == (game.Wood, game.Gold, game.Meat)
            && random == JsonUtility.ToJson(UnityEngine.Random.state), "presentation mutated gameplay/RNG");

        // Capacity/lifecycle fixtures intentionally add temporary views/entities after the invariant check.
        var fixtures = new System.Collections.Generic.List<StructureUnit>();
        bool originalFog = fog.IsDisabled;
        try
        {
            Fog(true); camera.transform.position = new Vector3(hall.transform.position.x, hall.transform.position.y, -10); camera.orthographicSize = 18;
            for (int i = 0; i < 9; i++) fixtures.Add(Spawn(i == 0 ? "Tower" : "StoragePit", hall.transform.position + new Vector3((i % 3 - 1) * 4, 5 + i / 3 * 3, 0)));
            Phase(DayNightPhase.Night);
            Check(lighting.ActiveLocalLights == definition.LocalLightBudget && lighting.ActiveGlowGroups > lighting.ActiveLocalLights, "budget/fallback not enforced");
            var tower = fixtures[0]; var towerGroup = tower.transform.Find("Night light presentation");
            Check(towerGroup != null && towerGroup.GetComponentsInChildren<SpriteRenderer>().Any(g => g.name == "Tower torch core"), "tower torch missing");
            var depot = fixtures[1]; var depotGroup = depot.transform.Find("Night light presentation");
            Check(depotGroup != null && depotGroup.GetComponentsInChildren<SpriteRenderer>().Any(g => g.name == "Depot lantern core"), "depot lantern missing");
            depot.OnReturnedToPool(); depot.gameObject.SetActive(false); Refresh(); Check(!depotGroup.gameObject.activeInHierarchy, "pool return left glow");
            depot.gameObject.SetActive(true); depot.OnSpawnedFromPool(true); bridge.Register(depot); Refresh();
            Check(depot.GetComponentsInChildren<Light2D>(true).Count(l => l.gameObject.activeInHierarchy) == 1, "pool reuse duplicated light");
            var body = bridge.Gameplay.EntityManager.GetComponentData<RTS.DOTS.CombatBody>(tower.DotsEntity); body.Health = 0;
            bridge.Gameplay.EntityManager.SetComponentData(tower.DotsEntity, body); bridge.SendMessage("SynchronizeGameplay"); Refresh();
            Check(towerGroup == null || !towerGroup.gameObject.activeInHierarchy, "native death left torch");
            int count = lighting.RegisteredEmitters; UnityEngine.Object.DestroyImmediate(fixtures[2].gameObject); Refresh();
            Check(lighting.RegisteredEmitters == count - 1, "destroyed building retained emitter");
            var unknown = Spawn("StoragePit", presentation.LandmarkPosition); fixtures.Add(unknown);
            camera.transform.position = new Vector3(unknown.transform.position.x, unknown.transform.position.y, -10);
            Fog(false); Refresh(); var unknownGroup = unknown.transform.Find("Night light presentation");
            Check(unknownGroup != null && !unknownGroup.gameObject.activeSelf, "unexplored source leaked light");
            typeof(FogOfWarManager).GetMethod("RevealAroundWorldPosition", Private).Invoke(fog, new object[] { unknown.transform.position, 5 });
            fog.SendMessage("ApplyFogColors"); fog.OnFogUpdated.Invoke(); Refresh();
            Check(unknownGroup.gameObject.activeSelf, "visible source did not light");
            fog.SendMessage("RefreshFog");
            // Player structures normally reveal themselves; explicit array state exercises explored-only gating.
            var cell = fog.FogTilemap.WorldToCell(unknown.transform.position); int x = cell.x - fog.Bounds.xMin, y = cell.y - fog.Bounds.yMin;
            ((int[,])typeof(FogOfWarManager).GetField("m_VisibilityCounts", Private).GetValue(fog))[x, y] = 0;
            Refresh(); Check(!unknownGroup.gameObject.activeSelf, "explored-only source leaked light");
        }
        finally
        {
            foreach (var structure in fixtures) if (structure != null) UnityEngine.Object.DestroyImmediate(structure.gameObject);
            Fog(originalFog); camera.transform.position = originalCamera; camera.orthographicSize = originalSize;
            Phase(originalPhase, originalProgress);
        }
        MaskProbe();
        return "PASS E2: injected/repeat init; Day off, Dusk ramp, Night on/dawn fade; pause stable; disable/re-enable; camera culling; no collider/Unit or gameplay/RNG mutations; 8 local-light cap with glow fallback; Tower/Depot details; return/reuse/death/destroy; unexplored/visible/explored-only fog gates; GPU shader probe shows visible half and masks hidden half. Temporary direct-bind fixtures do not validate tech/construction/balance; exit Play afterwards.";
    }

    static void MaskProbe()
    {
        var cameraObject = new GameObject("E2 mask probe camera"); var spriteObject = new GameObject("E2 mask probe sprite");
        var material = new Material(Find<DayNightCycleManager>().Presentation.NightGlowMaterial);
        var mask = new Texture2D(2, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        var image = new Texture2D(64, 64, TextureFormat.RGB24, false);
        var rt = RenderTexture.GetTemporary(64, 64, 24, RenderTextureFormat.ARGB32); var previous = RenderTexture.active;
        try
        {
            cameraObject.layer = spriteObject.layer = 31;
            var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 1; camera.aspect = 1; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var renderer = spriteObject.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material;
            renderer.color = new Color(1, .25f, .05f, 1);
            spriteObject.transform.localScale = Vector3.one * 2;
            mask.SetPixels(new[] { Color.white, Color.black }); mask.Apply();
            material.SetTexture("_NightFog", mask); material.SetVector("_NightFogBounds", new Vector4(-1, -1, 2, 2)); material.SetFloat("_NightFogEnabled", 1);
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = rt });
            RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); image.Apply();
            Check(image.GetPixel(16, 32).grayscale > .1f && image.GetPixel(48, 32).grayscale < .01f, "GPU fog mask failed");
            Check(image.GetPixel(16, 32).r > image.GetPixel(16, 32).g * 1.8f, "SpriteRenderer warm tint was ignored");
        }
        finally
        {
            RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(spriteObject);
            UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(mask);
        }
    }

    public static string RunMaskProbe() { MaskProbe(); return "PASS GPU: visible half drawn, hidden half masked, SpriteRenderer warm tint applied."; }

    public static string SetupCapture()
    {
        var hall = Find<TownHall>(); Spawn("Tower", hall.transform.position + new Vector3(4, 1, 0));
        Spawn("StoragePit", hall.transform.position + new Vector3(-4, 1, 0));
        var camera = Camera.main; camera.transform.position = hall.transform.position + new Vector3(0, 1, -10); camera.orthographicSize = 8; camera.aspect = 16f / 9;
        Find<CameraController>().LockCamera = true; Fog(false); Find<FogOfWarManager>().SendMessage("RefreshFog"); Phase(DayNightPhase.Night);
        return "Frozen capture fixture: seed1, Hall plus direct-bound test Tower/StoragePit, ortho8/16:9, fog on, night presentation probe only. No gameplay phase event.";
    }
    public static string CaptureBefore() { Find<StructureNightLighting>().enabled = false; return Capture("before"); }
    public static string CaptureAfter() { Find<StructureNightLighting>().enabled = true; Refresh(); return Capture("after"); }
    static string Capture(string label)
    {
        const string directory = "Docs/Modules/GameExperience/Evidence/E2/"; System.IO.Directory.CreateDirectory(directory);
        var rt = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32); var previous = RenderTexture.active;
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = rt });
            RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
            System.IO.File.WriteAllBytes(directory + label + "-Night-fog-on.png", image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
        var lights = Find<StructureNightLighting>(); return $"Captured {label}, fog-on, localLights={lights.ActiveLocalLights}, glowGroups={lights.ActiveGlowGroups}";
    }
}
