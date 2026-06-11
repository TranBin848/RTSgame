using UnityEngine;

public class TownHall : ProductionBuilding
{
    [SerializeField] private GameObject m_VillagerPrefab;

    public void EnqueueVillagerSpawn(SpawnVillagerActionSO action)
    {
        if (action == null)
        {
            return;
        }

        EnqueueProduction(action, m_VillagerPrefab, action.TrainDuration, "Villager spawned");
    }
}
