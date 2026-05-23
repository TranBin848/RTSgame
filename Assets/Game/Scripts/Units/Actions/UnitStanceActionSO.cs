using UnityEngine;

public enum UnitStance
{
    Offensive,
    Defensive,
}
[CreateAssetMenu(fileName = "UnitStanceAction", menuName = "Game/Actions/UnitStanceAction")]

public class UnitStanceActionSO : ActionSO
{
    [SerializeField] private UnitStance m_Stance;
    public UnitStance UnitStance => m_Stance;
    public override void Excute(GameManager manager)
    {
        if (manager.ActiveUnit != null)
        {
            Debug.Log($"Setting {manager.ActiveUnit.name} stance to {m_Stance}");
        }
    }
}