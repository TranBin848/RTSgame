// Unity MCP script-execute; invoke GameFlowMenu.Play/ReturnToMenu between checks, then Editor Step for Start.
using System;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS.DOTS;

public static class S6LifecycleValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string CheckReady()
    {
        var root=UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var day=UnityEngine.Object.FindFirstObjectByType<DayNightCycleManager>();
        var wave=UnityEngine.Object.FindFirstObjectByType<EnemyWaveManager>();
        var generator=UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
        Check(SceneManager.GetActiveScene().name=="ProceduralGame"&&root!=null&&root.IsReady&&bridge!=null&&bridge.IsSceneBound,
            "Composition not ready; let the loaded scene execute Start before checking.");
        Check(UnityEngine.Object.FindObjectsByType<SwarmGameBridge>(FindObjectsSortMode.None).Length==1&&bridge.Gameplay.ActorCount==25,"Duplicate bridge/actors.");
        var nodes=UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None);
        Check(nodes.Length==132&&nodes.All(n=>n.IsInitialized),"Resource nodes missing or unbound.");
        Check(generator.GeneratedMap.GeneratorVersion==3&&ProceduralMapRuntime.IsActive&&ProceduralMapRuntime.IsReady&&!ProceduralMapRuntime.IsCompositionBlocked,"Map readiness stale.");
        Check(wave.IsInitialized&&day.OnPhaseChanged.GetInvocationList().Count(x=>ReferenceEquals(x.Target,wave))==1,"Wave listener not unique.");
        var wallet=bridge.Gameplay.Wallet;
        Check(wallet.Wood==100&&wallet.Gold==100&&wallet.Meat==100,"New match wallet did not reset.");
        root.Initialize();bridge.BindScene();
        var manager=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        manager.BeginGameplay();manager.BeginGameplay();
        Check(bridge.Gameplay.Wallet.Equals(wallet)&&day.OnPhaseChanged.GetInvocationList().Count(x=>ReferenceEquals(x.Target,manager))==1,
            "Repeated gameplay startup duplicated wallet or day/night listener.");
        Check(bridge.Gameplay.ActorCount==25,"Repeated composition added actors.");
        return "PASS ProceduralGame Ready: one bridge/25actors/132injected nodes/one wave listener/wallet100x3";
    }
    public static string CheckMenu()
    {
        Check(SceneManager.GetActiveScene().name=="MainMenu"&&SwarmGameBridge.Instance==null,"Gameplay bridge retained in menu.");
        var world=World.DefaultGameObjectInjectionWorld;
        var game=world.GetExistingSystemManaged<GameplaySimulationSystem>();
        var swarm=world.GetExistingSystemManaged<SwarmSimulationSystem>();
        using var resources=world.EntityManager.CreateEntityQuery(typeof(GameplayResource));
        Check(game.ActorCount==0&&swarm.AgentCount==0&&swarm.ProjectileCount==0&&resources.CalculateEntityCount()==0,"Scene-owned entities leaked.");
        Check(!ProceduralMapRuntime.IsActive&&!ProceduralMapRuntime.IsReady&&!ProceduralMapRuntime.IsCompositionBlocked&&!ProceduralMapRuntime.HasFailed&&ProceduralMapRuntime.LastError==null,"Procedural readiness leaked into menu.");
        var wallet=game.Wallet;
        Check(wallet.Wood==0&&wallet.Gold==0&&wallet.Meat==0,"Wallet retained in menu.");
        return "PASS MainMenu: no bridge/actors/resources/agents/projectiles; wallet0x3; procedural flags cleared";
    }
}
