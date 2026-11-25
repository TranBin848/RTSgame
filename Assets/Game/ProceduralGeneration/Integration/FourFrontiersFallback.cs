using System;
using System.Collections.Generic;
using RTS.ProceduralGeneration;
using UnityEngine;

// A baked final map, never a second unbounded generation loop.
[Serializable]
public sealed class FourFrontiersFallback
{
    public int Version = 4, Seed, Attempt;
    public MapGenerationSettingsData Settings;
    public byte[] Terrain, Reserved, RegionIds, Routes;
    public ResourcePlacement[] Resources;
    public MapRegion[] Regions;
    public RegionConnection[] Connections;
    public MapGateway[] Gateways;
    public MapLandmark Landmark;

    public static FourFrontiersFallback Capture(MapGenerationSettingsData settings, MapData map) => new()
    {
        Settings = settings.Clone(), Seed = map.Seed, Attempt = map.Attempt,
        Terrain = map.Terrain, Reserved = map.Reserved, RegionIds = map.Layout.RegionIds, Routes = map.Layout.Routes,
        Resources = map.Resources.ToArray(), Regions = map.Layout.Regions, Connections = map.Layout.Connections,
        Gateways = map.Layout.Gateways, Landmark = map.Layout.Landmark
    };

    public MapData Restore(MapGenerationSettingsData requested)
    {
        if (Version != 4 || Settings == null || Settings.Preset != MapLayoutPreset.FourFrontiers
            || RequestedContract(requested) != RequestedContract(Settings) || Seed != Settings.Seed || Attempt < 0)
            throw new InvalidOperationException("Fallback version/settings mismatch.");
        int cells = Settings.Width * Settings.Height;
        if (Terrain == null || Reserved == null || RegionIds == null || Routes == null
            || Terrain.Length != cells || Reserved.Length != cells || RegionIds.Length != cells || Routes.Length != cells
            || Regions == null || Regions.Length != 5 || Resources == null)
            throw new InvalidOperationException("Fallback arrays mismatch.");
        foreach (byte value in Terrain) if (value > 1) throw new InvalidOperationException("Invalid fallback terrain.");
        foreach (byte value in Reserved) if (value > 1) throw new InvalidOperationException("Invalid fallback reserved mask.");
        foreach (var r in Resources)
            if (r.X < 0 || r.Y < 0 || r.X >= Settings.Width || r.Y >= Settings.Height || (byte)r.Kind > 2
                || r.Kind == ProceduralResourceKind.Gold && (r.VariantId < 0 || r.VariantId >= 6))
                throw new InvalidOperationException("Invalid fallback resource.");
        foreach (var r in Regions) CheckCell(r.X, r.Y);
        foreach (var g in Gateways ?? Array.Empty<MapGateway>()) CheckCell(g.X, g.Y);
        CheckCell(Landmark.X, Landmark.Y);
        var layout = StrategicLayout.FromSnapshot(Regions, Connections, Gateways, Landmark, RegionIds, Routes);
        var map = new MapData(Settings.Width, Settings.Height, Seed, Attempt, Settings.Width / 2, Settings.Height / 2,
            (byte[])Terrain.Clone(), (byte[])Reserved.Clone(), new List<ResourcePlacement>(Resources), new MapValidationReport(), layout,
            requested.Seed, true);
        var report = ProceduralMapGenerator.ValidateSnapshot(Settings, map);
        if (!report.IsValid) throw new InvalidOperationException("Fallback validation: " + string.Join(" | ", report.Errors));
        return new MapData(map.Width, map.Height, map.Seed, map.Attempt, map.StartX, map.StartY,
            map.Terrain, map.Reserved, map.Resources, report, map.Layout, requested.Seed, true);

        void CheckCell(int x, int y)
        { if (x < 0 || y < 0 || x >= Settings.Width || y >= Settings.Height) throw new InvalidOperationException("Fallback cell out of bounds."); }
    }

    static string RequestedContract(MapGenerationSettingsData settings)
    {
        if (MapGenerationSettingsValidator.Validate(settings) != null) throw new InvalidOperationException("Invalid fallback settings.");
        var normalized = settings.Clone(); normalized.Seed = 0;
        return JsonUtility.ToJson(normalized);
    }
}
