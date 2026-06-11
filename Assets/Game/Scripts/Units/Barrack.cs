using UnityEngine;

public class Barrack : ProductionBuilding
{
    [SerializeField] private GameObject m_WarriorPrefab;

    public void EnqueueWarriorSpawn(SpawnWarriorActionSO action)
    {
        if (action == null)
        {
            return;
        }

        EnqueueProduction(action, m_WarriorPrefab, action.TrainDuration, "Warrior spawned");
    }
}
