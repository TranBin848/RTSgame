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

    private void Awake()
    {
        m_SpawnArea = GetComponent<BoxCollider2D>();
        m_SpawnArea.isTrigger = true;
        gameObject.layer = 2; // Gán Layer là 2 (Ignore Raycast) để tia click đi xuyên qua

        ClearExistingResources();
        SpawnResources();

        m_SpawnArea.enabled = false; // Tắt collider sau khi dùng để tránh chặn click chuột vật lý
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            RegenerateResources();
        }
    }

    private void RegenerateResources()
    {
        m_SpawnArea.enabled = true;

        ClearExistingResources();
        SpawnResources();

        m_SpawnArea.enabled = false;

        // 1. Dựng lại lưới tìm đường A* từ đầu dựa trên tài nguyên mới sinh
        var tilemapManager = TilemapManager.Get();
        if (tilemapManager != null)
        {
            tilemapManager.ReinitializePathfinding();
        }

        // 2. Xóa cache định vị tài nguyên cũ và dừng công việc hiện tại của Worker tránh lỗi
        var gameManager = GameManager.Get();
        if (gameManager != null)
        {
            gameManager.ClearResourceNodeCache();
        }

        Debug.Log("[ResourceSpawner] Resources regenerated dynamically!");
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
        var tilemapManager = TilemapManager.Get();
        if (tilemapManager == null)
        {
            Debug.LogError("TilemapManager not found! Cannot spawn resources dynamically.");
            return;
        }

        Bounds bounds = m_SpawnArea.bounds;

        SpawnResourceGroup(m_TreePrefab, m_TreeContainer, m_TreeCount, bounds, tilemapManager);
        SpawnResourceGroup(m_GoldStonePrefab, m_GoldStoneContainer, m_GoldStoneCount, bounds, tilemapManager);
        SpawnResourceGroup(m_SheepPrefab, m_SheepContainer, m_SheepCount, bounds, tilemapManager);
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
                
                Instantiate(prefab, spawnPos, Quaternion.identity, container);
                
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
