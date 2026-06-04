using UnityEngine;

public interface IResourceDepotLocator
{
    bool TryFindClosestDepot(Vector3 originPosition, ResourceType resourceType, out IResourceDepot depot);
}
