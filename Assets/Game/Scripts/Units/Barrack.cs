using UnityEngine;

public class Barrack : ProductionBuilding
{
    [SerializeField] private GameObject m_WarriorPrefab;

    public bool EnqueueWarriorSpawn(SpawnWarriorActionSO action)
    {
        if (action == null)
        {
            return false;
        }

        return EnqueueProduction(action, m_WarriorPrefab, action.TrainDuration, "Warrior spawned");
    }
}
