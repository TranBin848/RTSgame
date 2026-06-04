using UnityEngine;

public interface IResourceDepot
{
    bool CanStore(ResourceType resourceType);
    Vector3 GetDeliveryPoint(Vector3 originPosition);
}
