using System.Threading;
using UnityEngine;

public abstract class SingletonManager<T> : MonoBehaviour where T : MonoBehaviour
{
    protected virtual void Awake()
    {
        T[] managers = FindObjectsByType<T>(FindObjectsSortMode.None);
        if (managers.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        // Initialize game state, load resources, etc.
    }
    public static T Get()
    {
        var tag = typeof(T).Name;
        GameObject managerObject = GameObject.FindWithTag(tag);
        if (managerObject != null)
        {
            return managerObject.GetComponent<T>();
        }
        else
        {
            Debug.LogError($"No T found in the scene. Please add a GameObject with the tag '{tag}' and attach a T script to it.");
            return null;
        }

        // GameObject go = new(tag);
        // go.tag = tag;
        // return go.AddComponent<T>();
    }

    public void Test()
    {
        Debug.Log("T Test method called.");
    }
};