using System.Threading;
using UnityEngine;

public class GameManager : SingletonManager<GameManager>
{


    public void Test()
    {
        Debug.Log("GameManager Test method called.");
    }
};