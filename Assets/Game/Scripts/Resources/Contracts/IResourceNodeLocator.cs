using UnityEngine;

public interface IResourceNodeLocator
{
    bool TryFindClosestAvailable(Vector3 originPosition, ResourceType resourceType, out IResourceNode resourceNode);
    bool TryGetNodeFromHit(RaycastHit2D hit, out IResourceNode resourceNode);
}
