using UnityEngine;

[CreateAssetMenu(fileName = "SpawnVillagerAction", menuName = "Game/Actions/SpawnVillager")]
public class SpawnVillagerActionSO : ActionSO
{
    [SerializeField] private int m_MeatCost = 0;
    [SerializeField] private float m_TrainDuration = 8f;

    public int MeatCost => m_MeatCost;
    public float TrainDuration => m_TrainDuration;

    public override void Excute(GameManager manager)
    {
        if (manager == null)
        {
            Debug.LogWarning("GameManager is null when executing spawn villager action.");
            return;
        }

        var active = manager.ActiveUnit;
        if (active == null)
        {
            manager.ShowTextPopup("No active town hall selected.", Color.red, Vector3.zero);
            return;
        }

        if (active is TownHall townHall)
        {
            if (manager.Meat < m_MeatCost)
            {
                manager.ShowTextPopup("Not enough resources", Color.red, townHall.transform.position);
                return;
            }

            // Deduct resources
            manager.AddResources(0, 0, -m_MeatCost);

            townHall.EnqueueVillagerSpawn(this);
            manager.ShowTextPopup("Villager queued", Color.green, townHall.transform.position);
        }
        else
        {
            manager.ShowTextPopup("Select a Town Hall to spawn villagers.", Color.yellow, active.transform.position);
        }
    }
}
