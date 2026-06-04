public class GoldStone : ResourceNodeBase
{
    public override ResourceType ResourceType => ResourceType.Gold;

    protected override void OnInitialize()
    {
        if (Collider != null)
        {
            SetInteractionRadius(Collider.size.x / 4f);
        }
    }
}
