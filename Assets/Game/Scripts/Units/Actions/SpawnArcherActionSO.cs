using UnityEngine;

[CreateAssetMenu(fileName = "SpawnArcherAction", menuName = "Game/Actions/SpawnArcher")]
public class SpawnArcherActionSO : ActionSO, IResourceCostAction
{
    [SerializeField] private int m_GoldCost = 0;
    [SerializeField] private int m_WoodCost = 0;
    [SerializeField] private int m_MeatCost = 0;
    [SerializeField] private float m_TrainDuration = 8f;

    public int GoldCost => m_GoldCost;
    public int WoodCost => m_WoodCost;
    public int MeatCost => m_MeatCost;
    public float TrainDuration => m_TrainDuration;

    public override void Excute(GameManager manager)
    {
        if (manager == null)
        {
            Debug.LogWarning("GameManager is null when executing spawn archer action.");
            return;
        }

        var active = manager.ActiveUnit;
        if (active == null)
        {
            manager.ShowTextPopup("No active archery range selected.", Color.red, Vector3.zero);
            return;
        }

        if (active is not ArcheryRange archeryRange)
        {
            manager.ShowTextPopup("Select an Archery Range to train archers.", Color.yellow, active.transform.position);
            return;
        }

        if (manager.Gold < m_GoldCost || manager.Wood < m_WoodCost || manager.Meat < m_MeatCost)
        {
            manager.ShowTextPopup("Not enough resources", Color.red, archeryRange.transform.position);
            return;
        }

        manager.AddResources(-m_GoldCost, -m_WoodCost, -m_MeatCost);
        archeryRange.EnqueueArcherSpawn(this);
        manager.ShowTextPopup("Archer queued", Color.green, archeryRange.transform.position);
    }
}
