using System.Collections.Generic;
using UnityEngine;

public static class RuntimeObjectPool
{
    private static readonly Dictionary<GameObject, Queue<GameObject>> s_Pools = new();
    private static Transform s_PoolRoot;

    public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject spawnedObject = Spawn(prefab.gameObject, position, rotation);
        return spawnedObject != null ? spawnedObject.GetComponent<T>() : null;
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        EnsureRoot();

        bool isReused = false;
        GameObject instance = null;
        if (s_Pools.TryGetValue(prefab, out var pool))
        {
            while (pool.Count > 0 && instance == null)
            {
                instance = pool.Dequeue();
            }
        }

        if (instance != null)
        {
            isReused = true;
        }
        else
        {
            instance = Object.Instantiate(prefab);
            var pooledObject = instance.GetComponent<PooledRuntimeObject>();
            if (pooledObject == null)
            {
                pooledObject = instance.AddComponent<PooledRuntimeObject>();
            }

            pooledObject.Initialize(prefab);
        }

        instance.transform.SetParent(null);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        foreach (var pooledObject in instance.GetComponents<IPooledRuntimeObject>())
        {
            pooledObject.OnSpawnedFromPool(isReused);
        }

        return instance;
    }

    public static void Release(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        var pooledObject = instance.GetComponent<PooledRuntimeObject>();
        if (pooledObject == null || pooledObject.Prefab == null)
        {
            Object.Destroy(instance);
            return;
        }

        EnsureRoot();

        foreach (var runtimeObject in instance.GetComponents<IPooledRuntimeObject>())
        {
            runtimeObject.OnReturnedToPool();
        }

        instance.SetActive(false);
        instance.transform.SetParent(s_PoolRoot, false);

        if (!s_Pools.TryGetValue(pooledObject.Prefab, out var pool))
        {
            pool = new Queue<GameObject>();
            s_Pools.Add(pooledObject.Prefab, pool);
        }

        pool.Enqueue(instance);
    }

    private static void EnsureRoot()
    {
        if (s_PoolRoot != null)
        {
            return;
        }

        var rootObject = new GameObject("Runtime Object Pools");
        Object.DontDestroyOnLoad(rootObject);
        s_PoolRoot = rootObject.transform;
    }
}
