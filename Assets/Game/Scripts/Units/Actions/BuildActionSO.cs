using UnityEngine;

[CreateAssetMenu(fileName = "BuildAction", menuName = "Game/Actions/BuildAction")]
public class BuildActionSo : ActionSO
{
    [SerializeField] private StructureUnit m_StructureUnitPrefab;
    [SerializeField] private Sprite m_PlacementSprite;
    [SerializeField] private Sprite m_FoundationSprite;
    [SerializeField] private Sprite m_MiddleSprite;
    [SerializeField] private Sprite m_CompleteSprite;
    [SerializeField] private Vector3Int m_BuildingSize;
    [SerializeField] private Vector3Int m_OriginalOffset;
    [SerializeField] private int m_GoldCost;
    [SerializeField] private int m_WoodCost;
    public StructureUnit StructureUnitPrefab => m_StructureUnitPrefab;
    public Sprite PlacementSprite => m_PlacementSprite;
    public Sprite FoundationSprite => m_FoundationSprite;
    public Sprite MiddleSprite => m_MiddleSprite;
    public Sprite CompleteSprite => m_CompleteSprite;
    public Vector3Int BuildingSize => m_BuildingSize;
    public Vector3Int OriginalOffset => m_OriginalOffset;
    public int GoldCost => m_GoldCost;
    public int WoodCost => m_WoodCost;
    public override void Excute(GameManager manager)
    {
        manager.StartBuildProcess(this);
    }
}