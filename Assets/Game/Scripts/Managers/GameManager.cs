using System.Threading;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    protected virtual void Awake()
    {
        GameManager[] managers = FindObjectsByType<GameManager>(FindObjectsSortMode.None);
        if (managers.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        // Initialize game state, load resources, etc.
    }
    public static GameManager Get()
    {
        var tag = nameof(GameManager);
        GameObject managerObject = GameObject.FindWithTag(tag);
        if (managerObject != null)
        {
            return managerObject.GetComponent<GameManager>();
        }
        else
        {
            Debug.LogError($"No GameManager found in the scene. Please add a GameObject with the tag '{tag}' and attach a GameManager script to it.");
            return null;
        }

        // GameObject go = new(tag);
        // go.tag = tag;
        // return go.AddComponent<GameManager>();
    }

    public void Test()
    {
        Debug.Log("GameManager Test method called.");
    }
};