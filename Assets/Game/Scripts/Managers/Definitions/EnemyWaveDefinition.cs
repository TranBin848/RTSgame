using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EnemyWaveDefinition", menuName = "Game/Definitions/Enemy Wave")]
public class EnemyWaveDefinition : ScriptableObject
{
    [Header("Town Hall Range")]
    [SerializeField] private float m_MinimumTownHallRange = 8f;
    [SerializeField] private float m_StructureRangePadding = 2f;

    [Header("Enemy Spawn")]
    [SerializeField] private GameObject[] m_EnemyPrefabs;
    [FormerlySerializedAs("m_EnemyCountPerNight")]
    [SerializeField] private int m_BaseEnemyCountPerWave = 8;
    [SerializeField] private int m_AdditionalEnemiesPerWave = 2;
    [SerializeField] private float m_EnemyCountIncreasePerDay = 1.5f;
    [SerializeField] private float m_SpawnInterval = 2f;
    [SerializeField] private float m_SpawnIntervalReductionPerDay = 0.05f;
    [SerializeField] private float m_MinimumSpawnInterval = 0.35f;
    [SerializeField] private float m_SpawnExtraRange = 6f;
    [SerializeField] private int m_SpawnRingSearchSteps = 48;

    [Header("Night Waves")]
    [SerializeField] private int m_BaseWavesPerNight = 1;
    [SerializeField] private float m_WaveCountIncreasePerDay = 0.25f;
    [SerializeField] private float m_DelayBetweenWaves = 8f;

    [Header("Range Visual")]
    [SerializeField] private bool m_ShowRangeOnlyAtNight = true;
    [SerializeField] private int m_RangeCircleSegments = 96;
    [SerializeField] private float m_RangeLineWidth = 0.08f;
    [SerializeField] private Color m_DayRangeColor = new Color(1f, 0.9f, 0.35f, 0.15f);
    [SerializeField] private Color m_NightRangeColor = new Color(1f, 0.9f, 0.35f, 0.9f);
    [SerializeField] private bool m_ShowEnemySpawnRange = true;
    [SerializeField] private float m_SpawnRangeLineWidth = 0.06f;
    [SerializeField] private Color m_EnemySpawnRangeColor = new Color(1f, 0.25f, 0.05f, 0.9f);

    public float MinimumTownHallRange => Mathf.Max(0f, m_MinimumTownHallRange);
    public float StructureRangePadding => Mathf.Max(0f, m_StructureRangePadding);
    public GameObject[] EnemyPrefabs => m_EnemyPrefabs;
    public float DelayBetweenWaves => Mathf.Max(0f, m_DelayBetweenWaves);
    public float SpawnExtraRange => Mathf.Max(0f, m_SpawnExtraRange);
    public int SpawnRingSearchSteps => Mathf.Max(8, m_SpawnRingSearchSteps);
    public bool ShowRangeOnlyAtNight => m_ShowRangeOnlyAtNight;
    public int RangeCircleSegments => Mathf.Max(12, m_RangeCircleSegments);
    public float RangeLineWidth => Mathf.Max(0.01f, m_RangeLineWidth);
    public Color DayRangeColor => m_DayRangeColor;
    public Color NightRangeColor => m_NightRangeColor;
    public bool ShowEnemySpawnRange => m_ShowEnemySpawnRange;
    public float SpawnRangeLineWidth => Mathf.Max(0.01f, m_SpawnRangeLineWidth);
    public Color EnemySpawnRangeColor => m_EnemySpawnRangeColor;

    public int GetWaveCountForDay(int day)
    {
        int safeDay = Mathf.Max(1, day);
        return Mathf.Max(0, m_BaseWavesPerNight + Mathf.FloorToInt((safeDay - 1) * m_WaveCountIncreasePerDay));
    }

    public int GetEnemyCountForWave(int day, int waveIndex)
    {
        int safeDay = Mathf.Max(1, day);
        int safeWaveIndex = Mathf.Max(0, waveIndex);
        int dayBonus = Mathf.FloorToInt((safeDay - 1) * m_EnemyCountIncreasePerDay);
        int waveBonus = safeWaveIndex * Mathf.Max(0, m_AdditionalEnemiesPerWave);
        return Mathf.Max(0, m_BaseEnemyCountPerWave + dayBonus + waveBonus);
    }

    public float GetSpawnIntervalForDay(int day)
    {
        int safeDay = Mathf.Max(1, day);
        float scaledInterval = m_SpawnInterval - ((safeDay - 1) * Mathf.Max(0f, m_SpawnIntervalReductionPerDay));
        return Mathf.Max(Mathf.Max(0.05f, m_MinimumSpawnInterval), scaledInterval);
    }
}
