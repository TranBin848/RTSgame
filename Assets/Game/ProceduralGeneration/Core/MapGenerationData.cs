using System;
using System.Collections.Generic;

namespace RTS.ProceduralGeneration
{
    public enum MapLayoutPreset : byte { LegacyMainland, StrategicRegions }
    public enum TerrainCell : byte
    {
        Water = 0,
        Grass = 1
    }

    public enum ProceduralResourceKind : byte
    {
        Tree,
        Gold,
        Sheep
    }

    [Serializable]
    public sealed class MapGenerationSettingsData
    {
        public int Width = 128;
        public int Height = 128;
        public int Seed = 1;
        public int MaxAttempts = 8;
        public MapLayoutPreset Preset;
        public int RegionRadius = 8;
        public int Octaves = 4;
        public float NoiseScale = 34f;
        public float Persistence = 0.5f;
        public float Lacunarity = 2f;
        public float WaterThreshold = -0.22f;
        public int BorderLandWidth = 3;
        public int StartClearRadius = 7;
        public int RequiredCorridorHalfWidth = 2;
        public int StartingTreeCount = 30;
        public int TotalTreeCount = 120;
        public int StartingGoldCount = 3;
        public int TotalGoldCount = 12;
        public int StartingSheepCount = 6;
        public int TotalSheepCount = 18;
        public int StartingTreeMinDistance = 8;
        public int StartingTreeMaxDistance = 16;
        public int StartingGoldMinDistance = 12;
        public int StartingGoldMaxDistance = 22;
        public int StartingSheepMinDistance = 6;
        public int StartingSheepMaxDistance = 12;
        public int ResourcePlacementAttempts = 12000;

        public MapGenerationSettingsData Clone()
        {
            return (MapGenerationSettingsData)MemberwiseClone();
        }
    }

    [Serializable]
    public struct ResourcePlacement
    {
        public ProceduralResourceKind Kind;
        public int VariantId;
        public int X;
        public int Y;
        public int ClusterId;
        public bool IsStartingResource;
    }

    public sealed class MapValidationReport
    {
        readonly List<string> m_Errors = new();

        public int Seed { get; internal set; }
        public int Attempt { get; internal set; }
        public int GrassCells { get; internal set; }
        public int WaterCells { get; internal set; }
        public int ReachableGrassCells { get; internal set; }
        public int TreeCount { get; internal set; }
        public int GoldCount { get; internal set; }
        public int SheepCount { get; internal set; }
        public int Repairs { get; internal set; }
        public IReadOnlyList<string> Errors => m_Errors;
        public bool IsValid => m_Errors.Count == 0;

        internal void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error)) m_Errors.Add(error);
        }
    }

    public sealed class MapData
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Seed;
        public readonly int Attempt;
        public readonly int StartX;
        public readonly int StartY;
        public readonly byte[] Terrain;
        public readonly byte[] Reserved;
        public readonly List<ResourcePlacement> Resources;
        public readonly MapValidationReport Validation;
        public readonly StrategicLayout Layout;
        public int GeneratorVersion => Layout == null ? 1 : 2;

        public MapData(int width, int height, int seed, int attempt, int startX, int startY,
            byte[] terrain, byte[] reserved, List<ResourcePlacement> resources, MapValidationReport validation, StrategicLayout layout = null)
        {
            Width = width;
            Height = height;
            Seed = seed;
            Attempt = attempt;
            StartX = startX;
            StartY = startY;
            Terrain = terrain;
            Reserved = reserved;
            Resources = resources;
            Validation = validation;
            Layout = layout;
        }

        public int Index(int x, int y) => y * Width + x;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool IsGrass(int x, int y) => InBounds(x, y) && Terrain[Index(x, y)] == (byte)TerrainCell.Grass;
    }

    public static class MapGenerationSettingsValidator
    {
        public static string Validate(MapGenerationSettingsData settings)
        {
            if (settings == null) return "Settings is null.";
            if (settings.Width < 32 || settings.Height < 32) return "Map size must be at least 32 x 32.";
            if (settings.Width > 1024 || settings.Height > 1024) return "Map size exceeds the supported 1024 x 1024 limit.";
            if (settings.MaxAttempts < 1) return "MaxAttempts must be positive.";
            if (settings.Preset != MapLayoutPreset.LegacyMainland && settings.Preset != MapLayoutPreset.StrategicRegions) return "Unknown map layout preset.";
            if (settings.Preset == MapLayoutPreset.StrategicRegions
                && (settings.RegionRadius < 4 || settings.RequiredCorridorHalfWidth < 1 || settings.RequiredCorridorHalfWidth > settings.RegionRadius
                    || settings.StartClearRadius + settings.RegionRadius * 2L + 6 >= Math.Min(settings.Width, settings.Height) / 2))
                return "Strategic regions need more room between the start, outer regions and border; reduce radii or increase map size.";
            if (settings.Octaves < 1 || settings.Octaves > 8) return "Octaves must be between 1 and 8.";
            if (settings.NoiseScale <= 0f) return "NoiseScale must be positive.";
            if (settings.Persistence <= 0f || settings.Persistence > 1f) return "Persistence must be in (0, 1].";
            if (settings.Lacunarity < 1f) return "Lacunarity must be at least 1.";
            if (settings.BorderLandWidth < 1) return "BorderLandWidth must be positive for the PvE wave preset.";
            if (settings.StartClearRadius < 4) return "StartClearRadius must leave room for the TownHall and exits.";
            if (settings.StartingTreeCount < 0 || settings.TotalTreeCount < settings.StartingTreeCount) return "Tree quotas are invalid.";
            if (settings.StartingGoldCount < 0 || settings.TotalGoldCount < settings.StartingGoldCount) return "Gold quotas are invalid.";
            if (settings.StartingSheepCount < 0 || settings.TotalSheepCount < settings.StartingSheepCount) return "Sheep quotas are invalid.";
            if (settings.ResourcePlacementAttempts < 1) return "ResourcePlacementAttempts must be positive.";
            return null;
        }
    }
}
