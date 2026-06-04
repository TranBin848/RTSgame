using System.Collections.Generic;
using UnityEngine;

public class SceneResourceNodeLocator : IResourceNodeLocator
{
    private readonly Transform[] m_ResourceContainers;
    private readonly List<IResourceNode> m_ResourceNodes = new();
    private bool m_IsCacheBuilt;

    public SceneResourceNodeLocator(params Transform[] resourceContainers)
    {
        m_ResourceContainers = resourceContainers;
    }

    public bool TryFindClosestAvailable(Vector3 originPosition, ResourceType resourceType, out IResourceNode resourceNode)
    {
        BuildCacheIfNeeded();

        float closestDistanceSqr = float.MaxValue;
        resourceNode = null;

        foreach (var node in m_ResourceNodes)
        {
            if (node == null || node.IsClaimed || node.ResourceType != resourceType)
            {
                continue;
            }

            float distanceSqr = (GetNodePosition(node) - originPosition).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                resourceNode = node;
            }
        }

        return resourceNode != null;
    }

    public bool TryGetNodeFromHit(RaycastHit2D hit, out IResourceNode resourceNode)
    {
        resourceNode = null;
        if (hit.collider == null)
        {
            return false;
        }

        if (TryGetNodeFromComponent(hit.collider, out resourceNode))
        {
            return true;
        }

        if (TryGetNodeFromComponent(hit.collider.transform.parent, out resourceNode))
        {
            return true;
        }

        return TryGetNodeFromChildren(hit.collider.transform, out resourceNode);
    }

    private void BuildCacheIfNeeded()
    {
        if (m_IsCacheBuilt)
        {
            return;
        }

        m_ResourceNodes.Clear();
        foreach (var container in m_ResourceContainers)
        {
            if (container == null)
            {
                continue;
            }

            var behaviours = container.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IResourceNode resourceNode)
                {
                    m_ResourceNodes.Add(resourceNode);
                }
            }
        }

        m_IsCacheBuilt = true;
    }

    private static bool TryGetNodeFromComponent(Component component, out IResourceNode resourceNode)
    {
        resourceNode = null;
        if (component == null)
        {
            return false;
        }

        var behaviours = component.GetComponents<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour is IResourceNode node)
            {
                resourceNode = node;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetNodeFromChildren(Transform root, out IResourceNode resourceNode)
    {
        resourceNode = null;
        if (root == null)
        {
            return false;
        }

        var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var behaviour in behaviours)
        {
            if (behaviour is IResourceNode node)
            {
                resourceNode = node;
                return true;
            }
        }

        return false;
    }

    private static Vector3 GetNodePosition(IResourceNode resourceNode)
    {
        if (resourceNode is MonoBehaviour behaviour)
        {
            return behaviour.transform.position;
        }

        return Vector3.zero;
    }
}
