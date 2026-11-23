// Unity MCP script-execute: G4Navigation.Run in paused, Ready ProceduralGame.
using System;
using UnityEngine;

public static class G4Navigation
{
    public static string Run()
    {
        var root = UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
        var map = UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        if (!Application.isPlaying || !UnityEditor.EditorApplication.isPaused || map == null || root == null || !root.IsReady)
            throw new InvalidOperationException("Expected paused, composed ProceduralGame.");
        var tilemap = map.WalkableTilemap;
        var bounds = tilemap.cellBounds;
        var mask = LayerMask.GetMask("Unit", "GoldStone", "Tree");
        var queriesHitTriggers = Physics2D.queriesHitTriggers;
        var fixture = new GameObject("G4 temporary overlap fixture");
        int cells = 0;
        bool Original(Vector3Int cell)
        {
            var size = tilemap.cellSize;
            foreach (var hit in Physics2D.OverlapBoxAll(tilemap.CellToWorld(cell) + size / 2, size * 0.9f, 0f, mask))
            {
                if (hit == null || (hit.isTrigger && hit.CompareTag("Resource"))) continue;
                if (hit.CompareTag("Building") || hit.CompareTag("Resource")) return true;
            }
            return false;
        }
        void AssertCell(Vector3Int cell, bool? expected = null)
        {
            bool actual = map.isBlockByBuilding(cell);
            if (actual != Original(cell) || (expected.HasValue && expected.Value != actual))
                throw new InvalidOperationException($"Overlap mismatch at {cell}, trigger queries {Physics2D.queriesHitTriggers}.");
        }
        BoxCollider2D Add(Vector3 position, string tag, int layer, bool trigger)
        {
            var child = new GameObject("Fixture collider") { tag = tag, layer = layer };
            child.transform.SetParent(fixture.transform);
            child.transform.position = position;
            var collider = child.AddComponent<BoxCollider2D>();
            collider.isTrigger = trigger;
            return collider;
        }
        try
        {
            var cell = new Vector3Int(bounds.xMin - 4, bounds.yMin - 4, 0);
            var center = tilemap.CellToWorld(cell) + tilemap.cellSize / 2;
            for (int i = 0; i < 32; i++) Add(center, "Resource", LayerMask.NameToLayer("Tree"), true);
            Add(center, "Building", 0, false);
            Physics2D.SyncTransforms();
            AssertCell(cell, false);
            var building = Add(center, "Building", LayerMask.NameToLayer("Unit"), true);
            Physics2D.SyncTransforms();
            foreach (bool triggers in new[] { true, false })
            {
                Physics2D.queriesHitTriggers = triggers;
                AssertCell(cell, triggers);
                foreach (var tile in bounds.allPositionsWithin)
                {
                    if (!tilemap.HasTile(tile) || map.isInUnreachableTilemap(tile)) continue;
                    AssertCell(tile);
                    cells++;
                }
            }
            building.isTrigger = false;
            AssertCell(cell, true);
            building.enabled = false;
            AssertCell(cell, false);
            long before = GC.GetAllocatedBytesForCurrentThread();
            foreach (var tile in bounds.allPositionsWithin)
                if (tilemap.HasTile(tile) && !map.isInUnreachableTilemap(tile)) map.isBlockByBuilding(tile);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            if (cells == 0 || bytes != 0) throw new InvalidOperationException($"Expected warmed scan at 0 B, got {cells} cells and {bytes} B.");
            return $"PASS {cells} tile comparisons across both trigger settings; crowded fixture, layer mask and disabled collider agree; warmed scan {bytes} B.";
        }
        finally
        {
            Physics2D.queriesHitTriggers = queriesHitTriggers;
            UnityEngine.Object.DestroyImmediate(fixture);
        }
    }
}
