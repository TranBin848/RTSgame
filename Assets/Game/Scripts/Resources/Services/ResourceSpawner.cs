using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ResourceSpawner : MonoBehaviour
{
    [Header("Resource Prefabs")]
    [SerializeField] private GameObject m_TreePrefab;
    [SerializeField] private GameObject m_GoldStonePrefab;
    [SerializeField] private GameObject m_SheepPrefab;

    [Header("Resource Containers")]
    [SerializeField] private Transform m_TreeContainer;
    [SerializeField] private Transform m_GoldStoneContainer;
    [SerializeField] private Transform m_SheepContainer;

    [Header("Spawn Settings")]
    [SerializeField] private int m_TreeCount = 20;
    [SerializeField] private int m_GoldStoneCount = 5;
    [SerializeField] private int m_SheepCount = 4;
    [SerializeField] private int m_MaxSpawnAttemptsPerItem = 100;

    private BoxCollider2D m_SpawnArea;
    private GameManager m_Game;
    private TilemapManager m_Map;
    private SwarmGameBridge m_Bridge;
    private bool m_Suppressed;
    public bool IsInitialized { get; private set; }
    public bool IsSuppressed => m_Suppressed;

    private void Awake()
    {
        ConfigureSpawnArea();
    }

    private void ConfigureSpawnArea()
    {
        m_SpawnArea = GetComponent<BoxCollider2D>();
        m_SpawnArea.isTrigger = true;
        gameObject.layer = 2; // Gán Layer là 2 (Ignore Raycast) để tia click đi xuyên qua
        m_SpawnArea.enabled = false; // Tắt collider sau khi dùng để tránh chặn click chuột vật lý
    }

    public void Initialize(GameManager game, TilemapManager map, SwarmGameBridge bridge)
    {
        if (game == null || map == null || map.WalkableTilemap == null || bridge == null || !bridge.IsInitialized)
            throw new System.ArgumentException("ResourceSpawner requires game, map and an initialized bridge.");
        if (bridge.gameObject != game.gameObject || game.gameObject.scene != gameObject.scene || map.gameObject.scene != gameObject.scene)
            throw new System.ArgumentException("ResourceSpawner dependencies must belong to its scene and GameManager.");
        if (IsInitialized)
        {
            if (m_Game != game || m_Map != map || m_Bridge != bridge)
                throw new System.InvalidOperationException("ResourceSpawner already has different dependencies.");
            return;
        }
        ConfigureSpawnArea();
        ValidateConfiguration();
        m_Game = game; m_Map = map; m_Bridge = bridge;
        if (!m_Suppressed) GenerateResources();
        IsInitialized = true;
    }

    private void ValidateConfiguration()
    {
        if (m_TreeCount < 0 || m_GoldStoneCount < 0 || m_SheepCount < 0 || m_MaxSpawnAttemptsPerItem < 1
            || m_SpawnArea.size.x <= 0 || m_SpawnArea.size.y <= 0)
            throw new System.InvalidOperationException("ResourceSpawner requires non-negative counts, positive attempts and area size.");
        if ((m_TreeCount > 0 && (m_TreePrefab == null || m_TreeContainer == null))
            || (m_GoldStoneCount > 0 && (m_GoldStonePrefab == null || m_GoldStoneContainer == null))
            || (m_SheepCount > 0 && (m_SheepPrefab == null || m_SheepContainer == null)))
            throw new System.InvalidOperationException("Every requested resource group requires its prefab and container.");
    }

    private void Update()
    {
        if (!IsInitialized || m_Suppressed) return;

        if (Input.GetKeyDown(KeyCode.P))
        {
            TryRegenerateResources();
        }
    }

    public void DisableForProceduralGeneration()
    {
        ConfigureSpawnArea();
        m_Suppressed = true;
        ClearExistingResources();
        enabled = false;
    }

    internal void RestoreLegacyGeneration()
    {
        m_Suppressed = false;
        enabled = true;
    }

    public bool TryRegenerateResources()
    {
        if (!IsInitialized || m_Suppressed || m_Game == null || m_Map == null || m_Map.WalkableTilemap == null
            || m_Bridge == null || !m_Bridge.IsSceneBound || ProceduralMapRuntime.IsCompositionBlocked) return false;
        ValidateConfiguration();
        GenerateResources();
        Debug.Log("[ResourceSpawner] Resources regenerated dynamically!");
        return true;
    }

    private void GenerateResources()
    {
        m_SpawnArea.enabled = true;
        try
        {
            ClearExistingResources();
            SpawnResources();
        }
        finally { m_SpawnArea.enabled = false; Physics2D.SyncTransforms(); }
        m_Map.ReinitializePathfinding();
        m_Game.ClearResourceNodeCache();
    }

    private void ClearExistingResources()
    {
        ClearContainer(m_TreeContainer);
        ClearContainer(m_GoldStoneContainer);
        ClearContainer(m_SheepContainer);
        Physics2D.SyncTransforms(); // Đồng bộ vật lý ngay sau khi xóa để dọn dẹp collider trong engine
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        
        List<GameObject> children = new();
        foreach (Transform child in container)
        {
            children.Add(child.gameObject);
        }

        // Dùng DestroyImmediate để xóa ngay lập tức các collider cũ
        // giúp các hàm truy vấn vật lý Physics2D.OverlapBox sau đó không bị nhận diện nhầm
        foreach (var child in children)
        {
            DestroyImmediate(child);
        }
    }

    private void SpawnResources()
    {
        Bounds bounds = m_SpawnArea.bounds;

        SpawnResourceGroup(m_TreePrefab, m_TreeContainer, m_TreeCount, bounds, m_Map);
        SpawnResourceGroup(m_GoldStonePrefab, m_GoldStoneContainer, m_GoldStoneCount, bounds, m_Map);
        SpawnResourceGroup(m_SheepPrefab, m_SheepContainer, m_SheepCount, bounds, m_Map);
    }

    private void SpawnResourceGroup(GameObject prefab, Transform container, int count, Bounds bounds, TilemapManager tilemapManager)
    {
        if (prefab == null || container == null) return;

        int spawnedCount = 0;
        int attempts = 0;
        int maxAttempts = count * m_MaxSpawnAttemptsPerItem;

        while (spawnedCount < count && attempts < maxAttempts)
        {
            attempts++;
            
            // Chọn một vị trí ngẫu nhiên trong vùng BoxCollider2D
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            Vector3 randomPos = new Vector3(randomX, randomY, 0f);

            // Chuyển sang tọa độ ô lưới (Cell coordinate)
            Vector3Int tilePos = tilemapManager.WalkableTilemap.WorldToCell(randomPos);
            
            // Kiểm tra xem ô này có trống và hợp lệ để đặt vật thể không
            // CanPlaceTiles kiểm tra: có gạch walkable, không nằm trong vùng unreachable, không bị đè bởi GameObject khác (nhà, tài nguyên khác)
            if (tilemapManager.CanPlaceTiles(tilePos))
            {
                // Căn giữa ô gạch
                Vector3 spawnPos = tilemapManager.WalkableTilemap.GetCellCenterWorld(tilePos);
                
                // Initial scene composition binds these views after the first navigation capture.
                var view = m_Bridge.IsSceneBound ? m_Bridge.SpawnGameplay(prefab, spawnPos, Quaternion.identity, container)
                    : Instantiate(prefab, spawnPos, Quaternion.identity, container);
                if (view == null) continue;
                
                // Đồng bộ vật lý ngay lập tức để OverlapBox của ô tiếp theo nhận diện được vật thể vừa sinh ra
                Physics2D.SyncTransforms();

                spawnedCount++;
            }
        }

        Debug.Log($"[ResourceSpawner] Spawned {spawnedCount}/{count} of {prefab.name} in {container.name}");
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        }
    }
}
