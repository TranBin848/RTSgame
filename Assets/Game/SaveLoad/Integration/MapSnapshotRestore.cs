using System;
using System.Linq;
using RTS.ProceduralGeneration;
using RTS.SaveLoad;
using UnityEngine;

public sealed partial class ProceduralMapAuthoring
{
    void ApplySavedMap(MapSaveData saved)
    {
        string error = ValidateAuthoring();
        if (error != null) throw new InvalidOperationException(error);
        m_LoadedMap = saved;
        var layout = saved.GeneratorVersion == 3 ? StrategicLayout.FromSnapshot(
            saved.Regions.Select(x => new MapRegion { X = x.X, Y = x.Y, Radius = x.Radius, Role = (RegionRole)x.Role }).ToArray(),
            saved.Connections.Select(x => new RegionConnection { From = x.From, To = x.To }).ToArray(),
            saved.Gateways.Select(x => new MapGateway { X = x.X, Y = x.Y, Region = x.Region }).ToArray(),
            new MapLandmark { X = saved.Landmark.X, Y = saved.Landmark.Y, Radius = saved.Landmark.Radius, Region = saved.Landmark.Region }, saved.RegionIds, saved.Routes) : null;
        GeneratedMap = new MapData(saved.Width, saved.Height, saved.Seed, saved.Attempt, saved.StartX, saved.StartY,
            (byte[])saved.Terrain.Clone(), (byte[])saved.Reserved.Clone(), saved.Placements.Select(p => new ResourcePlacement
            { Kind = (ProceduralResourceKind)p.Kind, VariantId = p.VariantId, X = p.X, Y = p.Y, ClusterId = p.ClusterId, IsStartingResource = p.IsStartingResource }).ToList(), new MapValidationReport(), layout);
        DisableLegacyResourceSpawner();
        if (m_GeneratedResourceRoot == null)
        { var root = new GameObject("Procedural Resources"); root.transform.SetParent(transform, false); m_GeneratedResourceRoot = root.transform; }
        ApplyTerrain(GeneratedMap);
        Vector3 world = m_TilemapManager.WalkableTilemap.CellToWorld(m_Origin);
        float cell = m_TilemapManager.WalkableTilemap.cellSize.x * m_TilemapManager.WalkableTilemap.transform.lossyScale.x;
        if (Mathf.Abs(world.x - saved.WorldOrigin.X) > .0001f || Mathf.Abs(world.y - saved.WorldOrigin.Y) > .0001f || Mathf.Abs(cell - saved.CellSize) > .0001f)
            throw new InvalidOperationException("Saved map origin/cell size differs from scene geometry.");
        Physics2D.SyncTransforms(); m_TilemapManager.ReinitializePathfinding();
    }
    public void BindSavedPlacement(int index, GameObject view) { if (view != null) m_SavePlacementViews[index] = view; }
}
