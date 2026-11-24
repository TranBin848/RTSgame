using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using RTS.ProceduralGeneration;
using UnityEngine;
using UnityEditor;

// script-execute in a fresh Ready, Editor-paused match. Probe objects/fog changes are discarded on Exit Play.
public static class GameExperienceE4Validation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Dir = "Docs/Modules/GameExperience/Evidence/E4/";
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static void Call(object target, string name, params object[] values) => target.GetType().GetMethod(name, Private).Invoke(target, values);
    static void Check(bool value, string text) { if (!value) throw new Exception("E4: " + text); }
    static string Sha(byte[] data) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant(); }
    static string Sha(string text) => Sha(Encoding.UTF8.GetBytes(text));

    public static string MapHash(MapData map)
    {
        using var stream = new MemoryStream(); using var w = new BinaryWriter(stream);
        foreach (int n in new[] { map.Width, map.Height, map.Seed, map.Attempt, map.StartX, map.StartY, map.GeneratorVersion }) w.Write(n);
        w.Write(map.Terrain); w.Write(map.Reserved); w.Write(map.Layout.RegionIds); w.Write(map.Layout.Routes);
        w.Write(map.Resources.Count);
        foreach (var r in map.Resources) { w.Write((int)r.Kind); w.Write(r.X); w.Write(r.Y); w.Write(r.VariantId); w.Write(r.ClusterId); w.Write(r.IsStartingResource); }
        foreach (var r in map.Layout.Regions) { w.Write(r.X); w.Write(r.Y); w.Write(r.Radius); w.Write((int)r.Role); }
        foreach (var c in map.Layout.Connections) { w.Write(c.From); w.Write(c.To); }
        foreach (var g in map.Layout.Gateways) { w.Write(g.X); w.Write(g.Y); w.Write(g.Region); }
        var l = map.Layout.Landmark; w.Write(l.X); w.Write(l.Y); w.Write(l.Radius); w.Write(l.Region); w.Flush();
        return Sha(stream.ToArray());
    }

    static string VisualHash(ProceduralMapPresentation p)
    {
        var ambient = new Dictionary<SpriteRenderer, object>();
        foreach (var a in (IEnumerable)Field(p, "m_Ambient")) ambient.Add((SpriteRenderer)a.GetType().GetField("Renderer").GetValue(a), a);
        var text = new StringBuilder();
        foreach (var r in p.Decorations)
        {
            text.Append(r.name).Append('|').Append(r.sprite.texture.name).Append('|').Append(r.sortingOrder).Append('|');
            if (ambient.TryGetValue(r, out var a)) text.Append(a.GetType().GetField("Home").GetValue(a)).Append('|').Append(a.GetType().GetField("Offset").GetValue(a));
            else text.Append(r.transform.position.ToString("R")).Append('|').Append(r.transform.localScale.ToString("R")).Append('|').Append(r.flipX).Append('|').Append(r.sprite.rect);
            text.AppendLine();
        }
        return Sha(text.ToString());
    }

    static string GroundHash(ProceduralMapAuthoring map, TilemapManager tiles)
    {
        var text = new StringBuilder();
        for (int y = 0; y < map.GeneratedMap.Height; y++) for (int x = 0; x < map.GeneratedMap.Width; x++)
            if (map.GeneratedMap.IsGrass(x, y)) text.Append(tiles.WalkableTilemap.GetColor(map.OriginCell + new Vector3Int(x, y, 0)).ToString("R"));
        return Sha(text.ToString());
    }

    static ProceduralMapPresentation Clone(ProceduralMapPresentation source, int seed, bool enabled)
    {
        var copy = new GameObject("E4 validation only").AddComponent<ProceduralMapPresentation>();
        foreach (var name in new[] { "m_BushTexture", "m_RockTexture", "m_WatchpostTexture", "m_WorldMaterial" }) Set(copy, name, Field(source, name));
        Set(copy, "m_PresentationSeed", seed); Set(copy, "m_EnableDaytimeDetail", enabled);
        var a = Find<ProceduralMapAuthoring>(); copy.InitializeDaytimeClock(Find<DayNightCycleManager>());
        copy.Initialize(a.GeneratedMap, Find<TilemapManager>(), a.OriginCell, Find<FogOfWarManager>()); return copy;
    }

    public static string Run()
    {
        var root = Find<GameSceneCompositionRoot>(); var p = Find<ProceduralMapPresentation>();
        Check(Application.isPlaying && EditorApplication.isPaused && root.IsReady, "fresh Ready paused match required");
        var a = Find<ProceduralMapAuthoring>(); var map = a.GeneratedMap; var tiles = Find<TilemapManager>(); var fog = Find<FogOfWarManager>();
        var bridge = Find<SwarmGameBridge>(); var game = Find<GameManager>(); var clock = Find<DayNightCycleManager>();
        string dataHash = MapHash(map), visual = VisualHash(p), ground = GroundHash(a, tiles), rng = JsonUtility.ToJson(UnityEngine.Random.state);
        int nav = tiles.NavigationVersion, actors = bridge.Gameplay.ActorCount; var tick = bridge.Simulation.TickCount; var wallet = (game.Wood, game.Gold, game.Meat);
        var settings = ((ProceduralMapSettings)Field(a, "m_Settings")).ToData(); settings.Seed = map.Seed;
        Check(new ProceduralMapGenerator().TryGenerate(settings, out var raw, out var error) && MapHash(raw) == dataHash, "presentation differs from fresh generator: " + error);
        Check(map.GeneratorVersion == 3 && map.Resources.Count == 150 && actors == 25, "version/quota/actors changed");
        Check(p.DaytimeDetailCount > 40 && p.DaytimeDetailCount <= ProceduralMapPresentation.DaytimeDetailBudget
            && p.AmbientDetailCount > 0 && p.AmbientDetailCount <= ProceduralMapPresentation.AmbientBudget
            && p.Decorations.Count <= 45 + ProceduralMapPresentation.DaytimeDetailBudget, "detail budget");
        var details = p.Decorations.Where(r => r.name.StartsWith("E4 ")).ToArray();
        int[] byRegion = new int[3];
        foreach (var r in details)
        {
            Check(r.GetComponents<Component>().All(c => c is Transform || c is SpriteRenderer), "decor has gameplay component");
            var cell = tiles.WalkableTilemap.WorldToCell(r.transform.position) - a.OriginCell;
            bool ripple = r.name.Contains("Water ripple");
            Check(r.sortingOrder > (ripple ? tiles.WaterTilemap : tiles.WalkableTilemap).GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder
                && r.sortingOrder < 25, "detail is behind terrain or covers units");
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                int i = map.Index(cell.x + dx, cell.y + dy);
                Check(map.Reserved[i] == 0 && map.Layout.Routes[i] == 0 && map.Terrain[i] == (byte)(ripple ? TerrainCell.Water : TerrainCell.Grass), "detail footprint hits protected/shore cell");
            }
            Check(ripple || map.Resources.All(n => (n.X - cell.x) * (n.X - cell.x) + (n.Y - cell.y) * (n.Y - cell.y) >= 9), "resource interaction clearance");
            if (!ripple) byRegion[map.Layout.RegionIds[map.Index(cell.x, cell.y)]]++;
        }
        Check(byRegion[0] < byRegion[1] && byRegion[0] < byRegion[2], "Start not sparse");
        Check(p.Decorations.Where(r => r.name == "Grove shrubs").All(r => r.sprite.rect.width == r.sprite.texture.height), "bush strip not cropped");
        p.InitializeDaytimeClock(clock); p.Initialize(map, tiles, a.OriginCell, fog);
        Check(VisualHash(p) == visual && fog.OnFogUpdated.GetInvocationList().Count(d => d.Target == p) == 1, "repeat init/listener");
        bool invalid = false; try { p.InitializeDaytimeClock(null); } catch (ArgumentNullException) { invalid = true; } Check(invalid, "missing clock accepted");
        string changedVisual, changedGround, baselineGround;
        var copies = new List<ProceduralMapPresentation>();
        try
        {
            var same = Clone(p, p.PresentationSeed, true); copies.Add(same);
            Check(VisualHash(same) == visual && GroundHash(a, tiles) == ground, "same seed not reproducible");
            var other = Clone(p, p.PresentationSeed + 1, true); copies.Add(other); changedVisual = VisualHash(other); changedGround = GroundHash(a, tiles);
            Check(changedVisual != visual && changedGround != ground, "presentation seed has no effect");
            var baseline = Clone(p, p.PresentationSeed, false); copies.Add(baseline); baselineGround = GroundHash(a, tiles);
            Check(baseline.DaytimeDetailCount == 0 && baseline.AmbientDetailCount == 0 && baselineGround != ground, "baseline switch");
            Check(MapHash(map) == dataHash, "presentation config changed gameplay map");
        }
        finally { foreach (var copy in copies) UnityEngine.Object.DestroyImmediate(copy.gameObject); Call(p, "PaintGround"); Call(p, "RefreshVisibility"); }

        Check(!p.IsSiteDiscovered(1) && !p.IsSiteDiscovered(2), "unknown site leaked");
        foreach (var r in details.Where(r => r.enabled)) Check(fog.IsWorldPositionExplored(r.transform.position), "unknown detail visible");
        var flower = details.First(r => r.name.Contains("flowers") && !fog.IsWorldPositionExplored(r.transform.position));
        var fc = tiles.WalkableTilemap.WorldToCell(flower.transform.position);
        var explored = (bool[,])Field(fog, "m_Explored"); var oldExplored = (bool[,])explored.Clone();
        try
        {
            explored[fc.x - fog.Bounds.xMin, fc.y - fog.Bounds.yMin] = true; Call(p, "RefreshVisibility");
            Check(!flower.enabled, "partial footprint reveals unknown neighbor");
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) explored[fc.x + x - fog.Bounds.xMin, fc.y + y - fog.Bounds.yMin] = true;
            Call(p, "RefreshVisibility"); Call(p, "LateUpdate");
            Check(flower.enabled && Mathf.Approximately(flower.color.a, .5f), "explored static memory");
            Check(details.Where(r => r.name.Contains("butterfly") || r.name.Contains("ripple")).All(r => !r.enabled || fog.TryGetCellState(tiles.WalkableTilemap.WorldToCell(r.transform.position), out var s, out _) && s == FogCellState.Visible), "animated detail in explored-only fog");
        }
        finally { Set(fog, "m_Explored", oldExplored); Call(p, "RefreshVisibility"); }
        Call(fog, "ToggleFogOfWar");
        try
        {
            Call(p, "RefreshVisibility"); Call(p, "UpdateAmbient", 0f);
            var motion0 = details.Select(r => (r.transform.position, r.transform.localScale, r.sprite)).ToArray();
            Call(p, "UpdateAmbient", 1f); var motion1 = details.Select(r => (r.transform.position, r.transform.localScale, r.sprite)).ToArray();
            Check(!motion0.SequenceEqual(motion1), "ambient does not animate");
            Call(p, "UpdateAmbient", 1f); Check(motion1.SequenceEqual(details.Select(r => (r.transform.position, r.transform.localScale, r.sprite))), "same time does not freeze motion");
            var phase = clock.CurrentPhase; typeof(DayNightCycleManager).GetProperty("CurrentPhase").SetValue(clock, DayNightPhase.Night);
            Call(p, "LateUpdate"); Check(details.Where(r => r.name.Contains("butterfly")).All(r => !r.enabled), "butterflies remain active at Night");
            typeof(DayNightCycleManager).GetProperty("CurrentPhase").SetValue(clock, phase);
        }
        finally { Call(fog, "ToggleFogOfWar"); Call(p, "RefreshVisibility"); Call(p, "LateUpdate"); }
        Set(fog, "m_IsInitialized", false); Call(p, "RefreshVisibility"); Call(p, "LateUpdate");
        Check(details.All(r => !r.enabled), "uninitialized fog reveals decor");
        Set(fog, "m_IsInitialized", true); Call(p, "RefreshVisibility"); Call(p, "LateUpdate");
        Check(dataHash == MapHash(map) && nav == tiles.NavigationVersion && actors == bridge.Gameplay.ActorCount && tick == bridge.Simulation.TickCount
            && wallet == (game.Wood, game.Gold, game.Meat) && rng == JsonUtility.ToJson(UnityEngine.Random.state), "presentation mutated gameplay or RNG");
        string result = $"PASS seed={map.Seed}; generatorVersion={map.GeneratorVersion}; resources={map.Resources.Count}; totalDecor={p.Decorations.Count}; E4={p.DaytimeDetailCount}; ambient={p.AmbientDetailCount}; regionCounts={string.Join(",", byRegion)}\nmapSHA256={dataHash}\nvisualSHA256={visual}\ngroundSHA256={ground}\nchangedSeedVisualSHA256={changedVisual}\nchangedSeedGroundSHA256={changedGround}\nbaselineGroundSHA256={baselineGround}\nPASS same seed reproduces; presentation seed/enable changes visuals, not full map hash; fresh raw generator agrees; footprint/resource/pad/routes safe; sparse Start; cropped bush; one listener/repeat-init; fog unknown/partial footprint/explored memory/visible-only motion/uninitialized; animation changes with time/freezes at same time/Night butterfly off; gameplay nav/wallet/actor/tick/RNG unchanged.\n";
        Directory.CreateDirectory(Dir); File.WriteAllText(Dir + $"seed-{map.Seed}-integration.txt", result); return result;
    }

    public static string SetSeed0() => SetSeed(0);
    public static string SetSeed1() => SetSeed(1);
    public static string SetSeed1543() => SetSeed(1543);
    static string SetSeed(int seed)
    {
        Check(!EditorApplication.isPlaying, "change fixture seed outside Play");
        var settings = (ProceduralMapSettings)Field(Find<ProceduralMapAuthoring>(), "m_Settings");
        if (!SessionState.GetBool("E4.SeedSaved", false)) { SessionState.SetInt("E4.OriginalSeed", settings.Seed); SessionState.SetBool("E4.SeedSaved", true); }
        settings.Seed = seed; EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets(); return "E4 test seed=" + seed;
    }
    public static string RestoreSeed() { int seed = SessionState.GetInt("E4.OriginalSeed", 1); SetSeed(seed); SessionState.SetBool("E4.SeedSaved", false); return "Restored original seed=" + seed; }

    static int[] OwnedIds(ProceduralMapPresentation p)
    {
        var ids = new List<int> { p.GetInstanceID(), ((Texture2D)Field(p, "m_DetailAtlas")).GetInstanceID() };
        ids.AddRange(((IEnumerable<Sprite>)Field(p, "m_Sprites")).Select(s => s.GetInstanceID()));
        ids.AddRange(p.Decorations.Select(r => r.gameObject.GetInstanceID())); return ids.ToArray();
    }
    static bool AllGone(string key) => SessionState.GetString(key, "").Split(',').Select(int.Parse).All(id => EditorUtility.InstanceIDToObject(id) == null);
    static string Lifecycle(string result) { Directory.CreateDirectory(Dir); File.AppendAllText(Dir + "lifecycle.txt", result + "\n"); return result; }

    public static string CreateDestroyProbe()
    {
        var p = Find<ProceduralMapPresentation>(); var fog = Find<FogOfWarManager>();
        var copy = Clone(p, p.PresentationSeed, true);
        SessionState.SetString("E4.DestroyIds", string.Join(",", OwnedIds(copy)));
        SessionState.SetInt("E4.DestroyContainer", copy.gameObject.GetInstanceID());
        SessionState.SetString("E4.SharedIds", string.Join(",", new[] { "m_BushTexture", "m_RockTexture", "m_WatchpostTexture", "m_WorldMaterial" }.Select(name => ((UnityEngine.Object)Field(p, name)).GetInstanceID())));
        UnityEngine.Object.DestroyImmediate(copy);
        Check(fog.OnFogUpdated.GetInvocationList().All(d => !ReferenceEquals(d.Target, copy)), "destroy retains fog listener");
        Call(p, "PaintGround"); Call(p, "RefreshVisibility");
        Directory.CreateDirectory(Dir); File.WriteAllText(Dir + "lifecycle.txt", "PASS component-only destroy immediately unsubscribes fog; step once to inspect deferred owned resources.\n");
        return "Destroyed owned test component, retained its container; step then CheckDestroyProbe";
    }

    public static string CheckDestroyProbe()
    {
        var container = (GameObject)EditorUtility.InstanceIDToObject(SessionState.GetInt("E4.DestroyContainer", 0));
        Check(AllGone("E4.DestroyIds") && container != null && container.transform.childCount == 0, "component-only destroy leaves owned views/sprites/atlas");
        Check(SessionState.GetString("E4.SharedIds", "").Split(',').Select(int.Parse).All(id => EditorUtility.InstanceIDToObject(id) != null), "shared art/material destroyed");
        UnityEngine.Object.DestroyImmediate(container);
        return Lifecycle("PASS component-only teardown: atlas/runtime sprites/decor views destroyed; shared art/material alive.");
    }

    public static string ReturnToMenu()
    {
        var p = Find<ProceduralMapPresentation>();
        SessionState.SetString("E4.SceneIds", string.Join(",", OwnedIds(p)));
        SessionState.SetString("E4.ExpectedVisual", VisualHash(p));
        Find<GameFlowMenu>().ReturnToMenu(); return "Loading menu with E4 owned IDs recorded";
    }

    public static string MenuAndReload()
    {
        Check(AllGone("E4.SceneIds") && Find<ProceduralMapPresentation>() == null && !ProceduralMapRuntime.IsActive, "scene owned resources survive menu");
        Find<GameFlowMenu>().Play(); return Lifecycle("PASS menu: all recorded controller/atlas/sprite/view IDs gone, old map context cleared; loading new match.");
    }

    public static string CheckNewMatch()
    {
        var p = Find<ProceduralMapPresentation>(); var root = Find<GameSceneCompositionRoot>(); var clock = Find<DayNightCycleManager>();
        Check(root.IsReady && Find<ProceduralMapAuthoring>().GeneratedMap.Seed == 1 && clock.CurrentPhase == DayNightPhase.Day
            && UnityEngine.Object.FindObjectsByType<ProceduralMapPresentation>(FindObjectsSortMode.None).Length == 1
            && Find<FogOfWarManager>().OnFogUpdated.GetInvocationList().Count(d => d.Target == p) == 1
            && VisualHash(p) == SessionState.GetString("E4.ExpectedVisual", "") && !p.IsSiteDiscovered(1) && !p.IsSiteDiscovered(2), "new match presentation/context not reset");
        return Lifecycle("PASS new seed1 match: Ready, Day, one presentation/one fog listener, same visual hash, unknown sites reset.");
    }

    public static string CheckLegacy()
    {
        Check(Find<ProceduralMapPresentation>() == null && Find<DayNightCycleManager>().Presentation == null, "E4 leaked into SampleScene");
        return Lifecycle("PASS SampleScene legacy: no procedural presentation/E4 and legacy palette retained.");
    }
}
