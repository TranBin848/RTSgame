// Unity MCP script-execute in a disposable paused Play session. Regeneration touches runtime views only.
using System;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEditor;
using RTS.DOTS;
using Unity.Core;

public static class D3SpawnerValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static int Count<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;
    static int EntityCount<T>(SwarmGameBridge b) where T:unmanaged,IComponentData
    {using var query=b.Gameplay.EntityManager.CreateEntityQuery(typeof(T));return query.CalculateEntityCount();}

    public static string CheckProcedural()
    {
        var root=UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
        var generator=UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var map=UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        Check(root!=null&&root.IsReady&&bridge.IsSceneBound,"Scene did not become ready.");
        Check(generator.LegacySpawners.Count==1,"Expected the explicit ResourceManager spawner reference.");
        int resources=EntityCount<GameplayResource>(bridge),actors=bridge.Gameplay.ActorCount,version=map.NavigationVersion;
        var spawner=generator.LegacySpawners[0];
        Check(spawner.IsInitialized&&spawner.IsSuppressed&&!spawner.enabled&&!spawner.GetComponent<BoxCollider2D>().enabled,"Procedural spawner not initialized/suppressed.");
        spawner.Initialize(game,map,bridge);root.Initialize();bridge.BindScene();
        var fixture=new GameObject("D3 alternate map dependency");fixture.SetActive(false);
        try
        {
            var alternate=fixture.AddComponent<TilemapManager>();
            typeof(TilemapManager).GetField("m_WalkableTilemap",Private).SetValue(alternate,map.WalkableTilemap);
            bool rejected=false;try{spawner.Initialize(game,alternate,bridge);}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"Replacing an injected map was accepted.");
        }
        finally { UnityEngine.Object.DestroyImmediate(fixture); }
        Check(!spawner.TryRegenerateResources(),"Suppressed spawner regenerated.");
        Check(bridge.Gameplay.ActorCount==actors&&EntityCount<GameplayResource>(bridge)==resources&&map.NavigationVersion==version,"Repeat initialization or suppression changed world state.");
        Check(Count<Tree>()==120&&Count<GoldStone>()==12&&Count<SheepUnit>()==18&&actors==25&&resources==150,"Resource/actor counts changed.");
        Check(UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None).All(n=>n.IsInitialized),"Procedural nodes not bound.");
        return $"PASS procedural seed={generator.GeneratedMap.Seed}; explicit spawner initialized/suppressed; repeat init/regenerate gate; trees120/gold12/sheep18/actors25/resourceEntities150";
    }

    public static string CheckValidation()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var map=UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        int actors=bridge.Gameplay.ActorCount,resources=EntityCount<GameplayResource>(bridge),rejected=0;
        var go=new GameObject("D3 resource spawner validation fixture");go.SetActive(false);
        var marker=new GameObject("Preserve resource until configuration validates");marker.transform.SetParent(go.transform);
        try
        {
            var spawner=go.AddComponent<ResourceSpawner>();
            Check(!spawner.TryRegenerateResources(),"Uninitialized spawner ran.");
            void Reject(Action action)
            {
                bool threw=false;try{action();}catch(ArgumentException){threw=true;}catch(InvalidOperationException){threw=true;}
                Check(threw&&!spawner.IsInitialized&&marker!=null&&bridge.Gameplay.ActorCount==actors&&EntityCount<GameplayResource>(bridge)==resources,"Invalid init mutated resources or became ready.");
                rejected++;
            }
            Reject(()=>spawner.Initialize(null,map,bridge));
            Reject(()=>spawner.Initialize(game,null,bridge));
            Reject(()=>spawner.Initialize(game,map,null));
            typeof(ResourceSpawner).GetField("m_TreeContainer",Private).SetValue(spawner,go.transform);
            Reject(()=>spawner.Initialize(game,map,bridge));
            var authoring=UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
            if(authoring!=null)
            {
                var arrayField=typeof(ProceduralMapAuthoring).GetField("m_LegacySpawners",Private);
                var original=arrayField.GetValue(authoring);
                try
                {
                    arrayField.SetValue(authoring,new ResourceSpawner[]{null});
                    Check(((string)typeof(ProceduralMapAuthoring).GetMethod("ValidateAuthoring",Private).Invoke(authoring,null)).Contains("spawner"),"Null spawner reference accepted.");
                    var actual=UnityEngine.Object.FindFirstObjectByType<ResourceSpawner>();
                    arrayField.SetValue(authoring,new[]{actual,actual});
                    Check(((string)typeof(ProceduralMapAuthoring).GetMethod("ValidateAuthoring",Private).Invoke(authoring,null)).Contains("spawner"),"Duplicate spawner reference accepted.");
                }
                finally{arrayField.SetValue(authoring,original);}
            }
            return $"PASS uninitialized gate; invalid dependency/config cases={rejected}; no resource clear/entity side effects; authoring null/duplicate refs checked when present";
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    public static string RegenerateLegacy()
    {
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SampleScene","Run legacy regeneration in SampleScene.");
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var map=UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<ResourceSpawner>();
        Check(spawner.IsInitialized&&!spawner.IsSuppressed&&bridge.IsSceneBound,"Legacy spawner not initialized.");
        int trees=Count<Tree>(),gold=Count<GoldStone>(),sheep=Count<SheepUnit>(),actors=bridge.Gameplay.ActorCount,resources=EntityCount<GameplayResource>(bridge);
        int Expected(string field)=>(int)typeof(ResourceSpawner).GetField(field,Private).GetValue(spawner);
        Check(trees==Expected("m_TreeCount")&&gold==Expected("m_GoldStoneCount")&&sheep==Expected("m_SheepCount"),"Legacy initial quota mismatch.");
        spawner.Initialize(game,map,bridge);
        Check(bridge.Gameplay.ActorCount==actors&&EntityCount<GameplayResource>(bridge)==resources,"Repeat init regenerated resources.");
        foreach(var name in new[]{"m_Game","m_Map"})
        {
            var field=typeof(ResourceSpawner).GetField(name,Private);var original=field.GetValue(spawner);
            try
            {
                field.SetValue(spawner,null);
                Check(!spawner.TryRegenerateResources()&&EntityCount<GameplayResource>(bridge)==resources&&bridge.Gameplay.ActorCount==actors,
                    "Lost dependency cleared resources during regeneration.");
            }
            finally{field.SetValue(spawner,original);}
        }
        var worker=UnityEngine.Object.FindFirstObjectByType<WorkerUnit>();
        var tree=UnityEngine.Object.FindFirstObjectByType<Tree>();
        Check(bridge.AssignResource(worker,tree),"Could not assign old tree before regeneration.");
        var oldNodes=UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None);
        var oldEntities=oldNodes.Select(n=>bridge.ResourceEntity(n)).Concat(UnityEngine.Object.FindObjectsByType<SheepUnit>(FindObjectsSortMode.None).Select(s=>s.DotsEntity)).ToArray();
        int version=map.NavigationVersion;
        Check(spawner.TryRegenerateResources(),"Legacy regeneration was rejected.");
        Check(oldNodes.All(n=>n==null)&&oldEntities.All(e=>!bridge.Gameplay.EntityManager.Exists(e)),"Old view/entity leaked.");
        Check(Count<Tree>()==trees&&Count<GoldStone>()==gold&&Count<SheepUnit>()==sheep
            &&bridge.Gameplay.ActorCount==actors&&EntityCount<GameplayResource>(bridge)==resources,"Regeneration duplicated/lost resources or actors.");
        Check(map.NavigationVersion>version&&!spawner.GetComponent<BoxCollider2D>().enabled,"Navigation/click blocker not restored.");
        Check(UnityEngine.Object.FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None).All(n=>n.IsInitialized),"New resource dependency missing.");
        Check(bridge.Gameplay.TryGetWorker(worker.DotsEntity,out var state)&&state.Phase==WorkPhase.None&&state.Node==Entity.Null,"Worker retained old resource assignment.");
        typeof(SwarmGameBridge).GetMethod("RefreshNavigation",Private).Invoke(bridge,null);
        Check(bridge.Gameplay.IsConfigured&&bridge.Simulation.IsConfigured,"Both navigation systems must be configured.");
        return $"PASS legacy initial/repeat/regenerate; trees={trees},gold={gold},sheep={sheep},actors={actors},resourceEntities={resources}; old nodes/entities removed,worker canceled,new nodes injected,nav refreshed,area collider disabled";
    }

    public static string GatherAfterRegeneration()
    {
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var map=UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var worker=UnityEngine.Object.FindFirstObjectByType<WorkerUnit>();
        var tree=UnityEngine.Object.FindObjectsByType<Tree>(FindObjectsSortMode.None).OrderBy(t=>Vector2.Distance(t.DotsCenter,worker.transform.position)).First();
        var gameplay=bridge.Gameplay;var world=gameplay.World;var time=world.Time;
        var em=gameplay.EntityManager;var originalBody=em.GetComponentData<CombatBody>(worker.DotsEntity);
        var protectedBody=originalBody;protectedBody.Health=1000000;em.SetComponentData(worker.DotsEntity,protectedBody);
        int before=gameplay.Wallet.Wood;int deposited=0;float at=0;
        var sync=typeof(SwarmGameBridge).GetMethod("SynchronizeGameplay",Private);
        var refresh=typeof(SwarmGameBridge).GetMethod("RefreshNavigation",Private);
        try
        {
            Check(bridge.AssignResource(worker,tree),"New tree assignment failed.");
            refresh.Invoke(bridge,null);int version=map.NavigationVersion;
            while(gameplay.TryTakeEvent(out _)) { }
            for(int tick=1;tick<=12000;tick++)
            {
                world.SetTime(new TimeData(time.ElapsedTime+tick*.05,.05f));gameplay.Update();
                while(gameplay.TryTakeEvent(out var evt))
                    if(evt.Kind==GameplayEventKind.Deposit&&evt.Entity==worker.DotsEntity&&evt.ResourceType==0){deposited+=evt.Amount;at=tick*.05f;}
                if(tick%10==0)
                {
                    sync.Invoke(bridge,null);Physics2D.SyncTransforms();
                    if(version!=map.NavigationVersion){refresh.Invoke(bridge,null);version=map.NavigationVersion;}
                }
                if(deposited>0)break;
            }
            Check(deposited>0&&gameplay.Wallet.Wood>=before+deposited,"Worker could not gather/deposit from regenerated tree.");
            return $"PASS worker gathers/deposits from regenerated tree; wood+={deposited}; firstDepositAt={at:F2}s simulated; wallet={gameplay.Wallet.Wood}";
        }
        finally
        {
            world.SetTime(time);
            if(em.Exists(worker.DotsEntity)){var body=em.GetComponentData<CombatBody>(worker.DotsEntity);body.Health=originalBody.Health;em.SetComponentData(worker.DotsEntity,body);}
        }
    }
}
