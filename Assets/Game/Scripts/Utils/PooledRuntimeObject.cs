using UnityEngine;

public class PooledRuntimeObject : MonoBehaviour
{
    public GameObject Prefab { get; private set; }

    public void Initialize(GameObject prefab)
    {
        Prefab = prefab;
    }
}
