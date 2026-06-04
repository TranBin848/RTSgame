using System;
using System.Collections.Generic;
using UnityEngine;

public class SceneResourceDepotLocator : IResourceDepotLocator
{
    private readonly Func<IEnumerable<StructureUnit>> m_StructureProvider;

    public SceneResourceDepotLocator(Func<IEnumerable<StructureUnit>> structureProvider)
    {
        m_StructureProvider = structureProvider;
    }

    public bool TryFindClosestDepot(Vector3 originPosition, ResourceType resourceType, out IResourceDepot depot)
    {
        depot = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (var structure in m_StructureProvider.Invoke())
        {
            if (structure == null || structure.CurrentState == UnitState.Dead)
            {
                continue;
            }

            if (structure is not IResourceDepot candidateDepot || !candidateDepot.CanStore(resourceType))
            {
                continue;
            }

            float distanceSqr = (structure.transform.position - originPosition).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                depot = candidateDepot;
            }
        }

        return depot != null;
    }
}
