using UnityEngine;

public class TownHall : StructureUnit
{
    [SerializeField] private GameObject m_VillagerPrefab;
    [SerializeField] private Transform m_SpawnPoint;

    // Spawn a villager next to the town hall
    public void SpawnVillager()
    {
        if (m_VillagerPrefab == null)
        {
            Debug.LogWarning("Villager prefab not assigned on TownHall.");
            return;
        }

        Vector3 spawnPos = m_SpawnPoint != null ? m_SpawnPoint.position : (transform.position + Vector3.right);
        var go = GameObject.Instantiate(m_VillagerPrefab, spawnPos, Quaternion.identity);
        // If spawned object has Unit component, its Start will register it with GameManager automatically
    }
}
