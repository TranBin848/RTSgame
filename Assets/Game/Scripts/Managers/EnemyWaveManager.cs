using UnityEngine;
using UnityEngine.Events;

public class EnemyWaveManager : MonoBehaviour
{
    [SerializeField] private EnemyWaveDefinition m_Definition;
    [SerializeField] private DayNightCycleManager m_DayNightCycleManager;
    [SerializeField] private TownHall m_TownHall;
    [SerializeField] private LineRenderer m_TownHallRangeRenderer;
    [SerializeField] private LineRenderer m_EnemySpawnRangeRenderer;

    private GameManager m_GameManager;
    private TilemapManager m_TilemapManager;
    private float m_CurrentTownHallRange;
    private float m_SpawnTimer;
    private float m_WaveDelayTimer;
    private int m_CurrentWaveIndex;
    private int m_SpawnedInCurrentWave;
    private int m_StartedWaveCount;

    public UnityAction OnWaveScheduleChanged = delegate { };
    public UnityAction<int> OnWaveStarted = delegate { };
    public bool IsNightWaveActive => m_DayNightCycleManager != null
        && m_DayNightCycleManager.CurrentPhase == DayNightPhase.Night;
    public float NightProgress => IsNightWaveActive ? m_DayNightCycleManager.CurrentPhaseProgress : 0f;
    public int CurrentWaveIndex => m_CurrentWaveIndex;
    public int StartedWaveCount => m_StartedWaveCount;
    public int CurrentWaveCount => m_Definition != null ? m_Definition.GetWaveCountForDay(GetCurrentDay()) : 0;

    void Awake()
    {
        ResolveManagers();
        EnsureRangeRenderer();
    }

    void Start()
    {
        ResolveManagers();

        if (m_DayNightCycleManager == null)
        {
            m_DayNightCycleManager = FindFirstObjectByType<DayNightCycleManager>();
        }

        if (m_DayNightCycleManager != null)
        {
            m_DayNightCycleManager.OnPhaseChanged += HandlePhaseChanged;
            HandlePhaseChanged(m_DayNightCycleManager.CurrentPhase);
        }
        else
        {
            UpdateRangeVisual(false);
        }
    }

    void OnDestroy()
    {
        if (m_DayNightCycleManager != null)
        {
            m_DayNightCycleManager.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    void Update()
    {
        ResolveManagers();

        if (m_Definition == null || m_DayNightCycleManager == null)
        {
            return;
        }

        bool isNight = m_DayNightCycleManager.CurrentPhase == DayNightPhase.Night;
        UpdateRangeVisual(isNight);

        if (!isNight)
        {
            return;
        }

        UpdateNightEnemySpawning();
    }

    void HandlePhaseChanged(DayNightPhase phase)
    {
        if (phase == DayNightPhase.Night)
        {
            m_SpawnTimer = 0f;
            m_WaveDelayTimer = 0f;
            m_CurrentWaveIndex = 0;
            m_SpawnedInCurrentWave = 0;
            m_StartedWaveCount = 0;
            UpdateRangeVisual(true);
            OnWaveScheduleChanged.Invoke();
            return;
        }

        if (phase == DayNightPhase.Day)
        {
            RetreatActiveEnemiesToSpawnRing();
        }

        OnWaveScheduleChanged.Invoke();
        UpdateRangeVisual(false);
    }

    void ResolveManagers()
    {
        if (m_GameManager == null)
        {
            m_GameManager = FindFirstObjectByType<GameManager>();
        }

        if (m_TilemapManager == null)
        {
            m_TilemapManager = FindFirstObjectByType<TilemapManager>();
        }
    }

    void UpdateNightEnemySpawning()
    {
        if (!TryGetTownHall(out var townHall))
        {
            return;
        }

        int currentDay = GetCurrentDay();
        int waveCount = m_Definition.GetWaveCountForDay(currentDay);
        if (m_CurrentWaveIndex >= waveCount)
        {
            return;
        }

        int enemiesInCurrentWave = m_Definition.GetEnemyCountForWave(currentDay, m_CurrentWaveIndex);
        if (m_SpawnedInCurrentWave >= enemiesInCurrentWave)
        {
            m_CurrentWaveIndex++;
            m_SpawnedInCurrentWave = 0;
            m_WaveDelayTimer = m_Definition.DelayBetweenWaves;
            return;
        }

        if (m_WaveDelayTimer > 0f)
        {
            m_WaveDelayTimer -= Time.deltaTime;
            return;
        }

        m_SpawnTimer -= Time.deltaTime;
        if (m_SpawnTimer > 0f)
        {
            return;
        }

        if (SpawnEnemy(townHall))
        {
            if (m_SpawnedInCurrentWave == 0)
            {
                MarkWaveStarted(m_CurrentWaveIndex);
            }

            m_SpawnedInCurrentWave++;
            m_SpawnTimer = m_Definition.GetSpawnIntervalForDay(currentDay);
            return;
        }

        m_SpawnTimer = 0.5f;
    }

    int GetCurrentDay()
    {
        return m_DayNightCycleManager != null ? m_DayNightCycleManager.CurrentDay : 1;
    }

    void MarkWaveStarted(int waveIndex)
    {
        if (waveIndex < m_StartedWaveCount)
        {
            return;
        }

        m_StartedWaveCount = waveIndex + 1;
        OnWaveStarted.Invoke(waveIndex);
    }

    public float GetWaveStartNormalized(int waveIndex)
    {
        if (m_Definition == null)
        {
            return 0f;
        }

        int safeWaveIndex = Mathf.Max(0, waveIndex);
        float nightDuration = GetNightDurationForSchedule();
        if (nightDuration <= 0.0001f)
        {
            return 0f;
        }

        return Mathf.Clamp01(GetWaveStartTime(GetCurrentDay(), safeWaveIndex) / nightDuration);
    }

    float GetWaveStartTime(int day, int waveIndex)
    {
        float waveStartTime = 0f;
        float spawnInterval = m_Definition.GetSpawnIntervalForDay(day);

        for (int i = 0; i < waveIndex; i++)
        {
            int enemyCount = m_Definition.GetEnemyCountForWave(day, i);
            if (enemyCount > 0)
            {
                waveStartTime += Mathf.Max(0, enemyCount - 1) * spawnInterval;
            }

            waveStartTime += m_Definition.DelayBetweenWaves;
        }

        return waveStartTime;
    }

    float GetNightDurationForSchedule()
    {
        if (m_DayNightCycleManager != null && m_DayNightCycleManager.Definition != null)
        {
            return m_DayNightCycleManager.Definition.NightDuration;
        }

        int currentDay = GetCurrentDay();
        int waveCount = m_Definition.GetWaveCountForDay(currentDay);
        if (waveCount <= 0)
        {
            return 1f;
        }

        int lastWaveIndex = waveCount - 1;
        int lastEnemyCount = m_Definition.GetEnemyCountForWave(currentDay, lastWaveIndex);
        float lastWaveDuration = Mathf.Max(0, lastEnemyCount - 1) * m_Definition.GetSpawnIntervalForDay(currentDay);
        return Mathf.Max(1f, GetWaveStartTime(currentDay, lastWaveIndex) + lastWaveDuration);
    }

    bool SpawnEnemy(TownHall townHall)
    {
        GameObject enemyPrefab = GetRandomEnemyPrefab();
        if (enemyPrefab == null)
        {
            return false;
        }

        if (!TryGetEnemySpawnPosition(townHall.transform.position, out Vector3 spawnPosition))
        {
            return false;
        }

        GameObject enemyObject = RuntimeObjectPool.Spawn(enemyPrefab, spawnPosition, Quaternion.identity);
        if (enemyObject == null)
        {
            return false;
        }

        if (!enemyObject.TryGetComponent<Unit>(out var enemyUnit))
        {
            RuntimeObjectPool.Release(enemyObject);
            return false;
        }

        enemyUnit.SetTarget(townHall);
        enemyUnit.SetTask(UnitTask.Attack);
        enemyUnit.MoveTo(townHall.transform.position);
        return true;
    }

    void RetreatActiveEnemiesToSpawnRing()
    {
        if (m_GameManager == null || !TryGetTownHall(out var townHall))
        {
            return;
        }

        m_CurrentTownHallRange = CalculateTownHallRange(townHall);
        var enemyUnits = new System.Collections.Generic.List<Unit>(m_GameManager.GetFriendlyUnits(false));
        foreach (var unit in enemyUnits)
        {
            if (unit == null || unit.CurrentState == UnitState.Dead || unit is not EnemyUnit enemyUnit)
            {
                continue;
            }

            if (TryGetEnemyRetreatPosition(townHall.transform.position, enemyUnit.transform.position, out Vector3 retreatPosition))
            {
                enemyUnit.ReturnToSpawnRingAndDespawn(retreatPosition);
            }
            else
            {
                enemyUnit.DespawnToPool();
            }
        }
    }

    GameObject GetRandomEnemyPrefab()
    {
        GameObject[] enemyPrefabs = m_Definition.EnemyPrefabs;
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return null;
        }

        int startIndex = Random.Range(0, enemyPrefabs.Length);
        for (int offset = 0; offset < enemyPrefabs.Length; offset++)
        {
            GameObject candidate = enemyPrefabs[(startIndex + offset) % enemyPrefabs.Length];
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    bool TryGetEnemySpawnPosition(Vector3 townHallPosition, out Vector3 spawnPosition)
    {
        spawnPosition = townHallPosition;
        float spawnRadius = m_CurrentTownHallRange + m_Definition.SpawnExtraRange;

        if (m_TilemapManager == null || m_TilemapManager.WalkableTilemap == null)
        {
            float fallbackAngle = Random.Range(0f, Mathf.PI * 2f);
            spawnPosition = GetPointOnSpawnRing(townHallPosition, spawnRadius, fallbackAngle);
            return true;
        }

        float startAngle = Random.Range(0f, Mathf.PI * 2f);
        return TryGetWalkablePointOnSpawnRingNearAngle(townHallPosition, spawnRadius, startAngle, out spawnPosition);
    }

    bool TryGetEnemyRetreatPosition(Vector3 townHallPosition, Vector3 enemyPosition, out Vector3 retreatPosition)
    {
        Vector3 direction = enemyPosition - townHallPosition;
        float angle = direction.sqrMagnitude <= 0.001f
            ? Random.Range(0f, Mathf.PI * 2f)
            : Mathf.Atan2(direction.y, direction.x);

        float spawnRadius = m_CurrentTownHallRange + m_Definition.SpawnExtraRange;
        if (m_TilemapManager == null || m_TilemapManager.WalkableTilemap == null)
        {
            retreatPosition = GetPointOnSpawnRing(townHallPosition, spawnRadius, angle);
            return true;
        }

        return TryGetWalkablePointOnSpawnRingNearAngle(townHallPosition, spawnRadius, angle, out retreatPosition);
    }

    bool TryGetWalkablePointOnSpawnRingNearAngle(Vector3 center, float radius, float startAngle, out Vector3 spawnPosition)
    {
        if (TryGetWalkablePointOnSpawnRing(center, radius, startAngle, out spawnPosition))
        {
            return true;
        }

        float angleStep = (Mathf.PI * 2f) / m_Definition.SpawnRingSearchSteps;
        for (int offsetIndex = 1; offsetIndex <= m_Definition.SpawnRingSearchSteps / 2; offsetIndex++)
        {
            float angleOffset = angleStep * offsetIndex;
            if (TryGetWalkablePointOnSpawnRing(center, radius, startAngle + angleOffset, out spawnPosition))
            {
                return true;
            }

            if (TryGetWalkablePointOnSpawnRing(center, radius, startAngle - angleOffset, out spawnPosition))
            {
                return true;
            }
        }

        return false;
    }

    bool TryGetWalkablePointOnSpawnRing(Vector3 center, float radius, float angle, out Vector3 spawnPosition)
    {
        spawnPosition = GetPointOnSpawnRing(center, radius, angle);
        Vector3Int spawnCell = m_TilemapManager.WalkableTilemap.WorldToCell(spawnPosition);
        return m_TilemapManager.CamWalkAtTile(spawnCell);
    }

    Vector3 GetPointOnSpawnRing(Vector3 center, float radius, float angle)
    {
        return center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
    }

    void UpdateRangeVisual(bool isNight)
    {
        if (m_Definition == null || m_TownHallRangeRenderer == null || !TryGetTownHall(out var townHall))
        {
            SetRangeRenderersVisible(false);
            return;
        }

        m_CurrentTownHallRange = CalculateTownHallRange(townHall);
        bool shouldShow = isNight || !m_Definition.ShowRangeOnlyAtNight;
        SetRangeRenderersVisible(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        m_TownHallRangeRenderer.startColor = isNight ? m_Definition.NightRangeColor : m_Definition.DayRangeColor;
        m_TownHallRangeRenderer.endColor = m_TownHallRangeRenderer.startColor;
        m_TownHallRangeRenderer.startWidth = m_Definition.RangeLineWidth;
        m_TownHallRangeRenderer.endWidth = m_Definition.RangeLineWidth;
        DrawCircle(m_TownHallRangeRenderer, townHall.transform.position, m_CurrentTownHallRange);

        bool shouldShowSpawnRange = isNight && m_Definition.ShowEnemySpawnRange;
        SetRendererVisible(m_EnemySpawnRangeRenderer, shouldShowSpawnRange);
        if (shouldShowSpawnRange)
        {
            m_EnemySpawnRangeRenderer.startColor = m_Definition.EnemySpawnRangeColor;
            m_EnemySpawnRangeRenderer.endColor = m_Definition.EnemySpawnRangeColor;
            m_EnemySpawnRangeRenderer.startWidth = m_Definition.SpawnRangeLineWidth;
            m_EnemySpawnRangeRenderer.endWidth = m_Definition.SpawnRangeLineWidth;
            DrawCircle(
                m_EnemySpawnRangeRenderer,
                townHall.transform.position,
                m_CurrentTownHallRange + m_Definition.SpawnExtraRange);
        }
    }

    float CalculateTownHallRange(TownHall townHall)
    {
        float range = m_Definition.MinimumTownHallRange;
        if (m_GameManager == null)
        {
            return range;
        }

        foreach (var structure in m_GameManager.GetPlayerStructures())
        {
            if (structure == null || structure.CurrentState == UnitState.Dead)
            {
                continue;
            }

            float distance = Vector3.Distance(townHall.transform.position, structure.transform.position);
            range = Mathf.Max(range, distance + m_Definition.StructureRangePadding);
        }

        return range;
    }

    bool TryGetTownHall(out TownHall townHall)
    {
        if (m_TownHall != null && m_TownHall.CurrentState != UnitState.Dead)
        {
            townHall = m_TownHall;
            return true;
        }

        townHall = null;
        if (m_GameManager == null)
        {
            return false;
        }

        if (m_GameManager.TryFindClosestTownHall(Vector3.zero, out townHall))
        {
            m_TownHall = townHall;
            return true;
        }

        return false;
    }

    void EnsureRangeRenderer()
    {
        if (m_TownHallRangeRenderer != null)
        {
            ConfigureRangeRenderer(m_TownHallRangeRenderer);
        }
        else
        {
            m_TownHallRangeRenderer = CreateRangeRenderer("Town Hall Night Range");
        }

        if (m_EnemySpawnRangeRenderer != null)
        {
            ConfigureRangeRenderer(m_EnemySpawnRangeRenderer);
        }
        else
        {
            m_EnemySpawnRangeRenderer = CreateRangeRenderer("Enemy Spawn Night Range");
        }
    }

    LineRenderer CreateRangeRenderer(string rendererName)
    {
        var rangeObject = new GameObject(rendererName);
        rangeObject.transform.SetParent(transform, false);
        var rangeRenderer = rangeObject.AddComponent<LineRenderer>();
        ConfigureRangeRenderer(rangeRenderer);
        return rangeRenderer;
    }

    void ConfigureRangeRenderer(LineRenderer rangeRenderer)
    {
        rangeRenderer.useWorldSpace = true;
        rangeRenderer.loop = true;
        rangeRenderer.numCapVertices = 4;
        rangeRenderer.numCornerVertices = 4;
        rangeRenderer.sortingOrder = 100;

        if (rangeRenderer.sharedMaterial == null)
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                rangeRenderer.sharedMaterial = new Material(spriteShader);
            }
        }
    }

    void DrawCircle(LineRenderer rangeRenderer, Vector3 center, float radius)
    {
        int segments = m_Definition.RangeCircleSegments;
        rangeRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 position = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            rangeRenderer.SetPosition(i, position);
        }
    }

    void SetRangeRenderersVisible(bool isVisible)
    {
        SetRendererVisible(m_TownHallRangeRenderer, isVisible);
        SetRendererVisible(m_EnemySpawnRangeRenderer, isVisible && m_Definition != null && m_Definition.ShowEnemySpawnRange);
    }

    void SetRendererVisible(LineRenderer rangeRenderer, bool isVisible)
    {
        if (rangeRenderer != null && rangeRenderer.enabled != isVisible)
        {
            rangeRenderer.enabled = isVisible;
        }
    }
}
