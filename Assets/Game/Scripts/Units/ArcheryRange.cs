using UnityEngine;

public class ArcheryRange : ProductionBuilding
{
    [SerializeField] private GameObject m_ArcherPrefab;

    public void EnqueueArcherSpawn(SpawnArcherActionSO action)
    {
        if (action == null)
        {
            return;
        }

        EnqueueProduction(action, m_ArcherPrefab, action.TrainDuration, "Archer spawned");
    }
}
