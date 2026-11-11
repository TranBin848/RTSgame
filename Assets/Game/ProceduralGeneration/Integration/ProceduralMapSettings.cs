using RTS.ProceduralGeneration;
using UnityEngine;

[CreateAssetMenu(fileName = "ProceduralMapSettings", menuName = "Game/Procedural Generation/Map Settings")]
public sealed class ProceduralMapSettings : ScriptableObject
{
    [Header("Map")]
    [SerializeField] MapLayoutPreset m_Preset;
    [SerializeField, Min(4)] int m_RegionRadius = 8;
    [SerializeField, Min(32)] int m_Width = 128;
    [SerializeField, Min(32)] int m_Height = 128;
    [SerializeField] int m_Seed = 1;
    [SerializeField, Min(1)] int m_MaxAttempts = 8;

    [Header("Terrain")]
    [SerializeField, Range(1, 8)] int m_Octaves = 4;
    [SerializeField, Min(1f)] float m_NoiseScale = 34f;
    [SerializeField, Range(0.01f, 1f)] float m_Persistence = 0.5f;
    [SerializeField, Min(1f)] float m_Lacunarity = 2f;
    [SerializeField, Range(-1f, 1f)] float m_WaterThreshold = -0.22f;
    [SerializeField, Min(1)] int m_BorderLandWidth = 3;
    [SerializeField, Min(4)] int m_StartClearRadius = 7;
    [SerializeField, Min(1)] int m_RequiredCorridorHalfWidth = 2;

    [Header("Resource Quotas")]
    [SerializeField, Min(0)] int m_StartingTreeCount = 30;
    [SerializeField, Min(0)] int m_TotalTreeCount = 120;
    [SerializeField, Min(0)] int m_StartingGoldCount = 3;
    [SerializeField, Min(0)] int m_TotalGoldCount = 12;
    [SerializeField, Min(0)] int m_StartingSheepCount = 6;
    [SerializeField, Min(0)] int m_TotalSheepCount = 18;

    [Header("Starting Resource Path Distance")]
    [SerializeField, Min(0)] int m_StartingTreeMinDistance = 8;
    [SerializeField, Min(1)] int m_StartingTreeMaxDistance = 16;
    [SerializeField, Min(0)] int m_StartingGoldMinDistance = 12;
    [SerializeField, Min(1)] int m_StartingGoldMaxDistance = 22;
    [SerializeField, Min(0)] int m_StartingSheepMinDistance = 6;
    [SerializeField, Min(1)] int m_StartingSheepMaxDistance = 12;
    [SerializeField, Min(100)] int m_ResourcePlacementAttempts = 12000;

    public int Seed { get => m_Seed; set => m_Seed = value; }

    public MapGenerationSettingsData ToData()
    {
        return new MapGenerationSettingsData
        {
            Width = m_Width,
            Preset = m_Preset,
            RegionRadius = m_RegionRadius,
            Height = m_Height,
            Seed = m_Seed,
            MaxAttempts = m_MaxAttempts,
            Octaves = m_Octaves,
            NoiseScale = m_NoiseScale,
            Persistence = m_Persistence,
            Lacunarity = m_Lacunarity,
            WaterThreshold = m_WaterThreshold,
            BorderLandWidth = m_BorderLandWidth,
            StartClearRadius = m_StartClearRadius,
            RequiredCorridorHalfWidth = m_RequiredCorridorHalfWidth,
            StartingTreeCount = m_StartingTreeCount,
            TotalTreeCount = m_TotalTreeCount,
            StartingGoldCount = m_StartingGoldCount,
            TotalGoldCount = m_TotalGoldCount,
            StartingSheepCount = m_StartingSheepCount,
            TotalSheepCount = m_TotalSheepCount,
            StartingTreeMinDistance = m_StartingTreeMinDistance,
            StartingTreeMaxDistance = m_StartingTreeMaxDistance,
            StartingGoldMinDistance = m_StartingGoldMinDistance,
            StartingGoldMaxDistance = m_StartingGoldMaxDistance,
            StartingSheepMinDistance = m_StartingSheepMinDistance,
            StartingSheepMaxDistance = m_StartingSheepMaxDistance,
            ResourcePlacementAttempts = m_ResourcePlacementAttempts
        };
    }
}
