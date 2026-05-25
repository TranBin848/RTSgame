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
        Debug.Log($"UnitStanceAction.Excute called. Stance={m_Stance} ActiveUnit={(manager.ActiveUnit != null ? manager.ActiveUnit.name : "null")}");
        if (manager.ActiveUnit != null)
        {
            manager.ActiveUnit.SetStance(this);
        }
    }
}