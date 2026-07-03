using UnityEngine;

public interface IResourceNode
{
    ResourceType ResourceType { get; }
    bool IsClaimed { get; }
    float InteractionRadius { get; }
    Vector3 GetInteractionPoint(Vector3 requesterPosition);
    bool TryClaim();
    void Release();
    void Hit();
}
