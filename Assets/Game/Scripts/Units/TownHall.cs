using UnityEngine;

public class TownHall : ProductionBuilding
{
    [SerializeField] private GameObject m_VillagerPrefab;

    public bool EnqueueVillagerSpawn(SpawnVillagerActionSO action)
    {
        if (action == null)
        {
            return false;
        }

        return EnqueueProduction(action, m_VillagerPrefab, action.TrainDuration, "Villager spawned");
    }
}
