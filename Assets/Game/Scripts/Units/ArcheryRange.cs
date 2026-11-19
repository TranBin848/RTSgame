using UnityEngine;

public class ArcheryRange : ProductionBuilding
{
    [SerializeField] private GameObject m_ArcherPrefab;

    public bool EnqueueArcherSpawn(SpawnArcherActionSO action)
    {
        if (action == null)
        {
            return false;
        }

        return EnqueueProduction(action, m_ArcherPrefab, action.TrainDuration, "Archer spawned");
    }
}
