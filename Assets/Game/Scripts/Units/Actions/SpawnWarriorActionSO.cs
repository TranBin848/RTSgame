using UnityEngine;

[CreateAssetMenu(fileName = "SpawnWarriorAction", menuName = "Game/Actions/SpawnWarrior")]
public class SpawnWarriorActionSO : ActionSO, IResourceCostAction
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
            Debug.LogWarning("GameManager is null when executing spawn warrior action.");
            return;
        }

        var active = manager.ActiveUnit;
        if (active == null)
        {
            manager.ShowTextPopup("No active barrack selected.", Color.red, Vector3.zero);
            return;
        }

        if (active is not Barrack barrack)
        {
            manager.ShowTextPopup("Select a Barrack to train warriors.", Color.yellow, active.transform.position);
            return;
        }

        if (manager.Gold < m_GoldCost || manager.Wood < m_WoodCost || manager.Meat < m_MeatCost)
        {
            manager.ShowTextPopup("Not enough resources", Color.red, barrack.transform.position);
            return;
        }

        manager.AddResources(-m_GoldCost, -m_WoodCost, -m_MeatCost);
        barrack.EnqueueWarriorSpawn(this);
        manager.ShowTextPopup("Warrior queued", Color.green, barrack.transform.position);
    }
}
