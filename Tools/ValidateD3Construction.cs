// Unity MCP script-execute, D3ConstructionValidation.CheckValidation in a fresh paused Play session.
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using RTS.DOTS;
using Unity.Core;

public static class D3ConstructionValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}

    public static string CheckCompletedQueue()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var panel=(ActionQueuePanel)typeof(GameManager).GetField("m_ActionQueuePanel",Private).GetValue(game);
        int count=((ICollection)typeof(ActionQueuePanel).GetField("m_QueueEntries",Private).GetValue(panel)).Count;
        Check(count==0,"Construction UI entry remained after completion.");
        return "PASS completed construction cleared its queue entry";
    }

    public static string CheckPlacementGuard()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var map=UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var worker=UnityEngine.Object.FindFirstObjectByType<WorkerUnit>();
        var action=AssetDatabase.LoadAssetAtPath<BuildActionSo>("Assets/Game/ScriptableObjects/Units/BuildStoragePitAction.asset");
        var field=typeof(GameManager).GetField("m_PlacementProcess",Private);
        var previous=field.GetValue(game);var selected=game.ActiveUnit;var state=worker.CurrentState;
        var stateProperty=typeof(Unit).GetProperty("CurrentState");
        var events=UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        var enabled=events.Select(e=>e.enabled).ToArray();
        var temporary=new GameObject("D3 unbound placement worker");temporary.SetActive(false);
        int wood=game.Wood,gold=game.Gold,actors=bridge.Gameplay.ActorCount;
        try
        {
            foreach(var e in events)e.enabled=false;
            var point=new Vector2(Screen.width*.5f,Screen.height*.5f);
            Check(Camera.main!=null&&GameUtils.IsScreenPositionInBounds(point)&&!GameUtils.iSPointOverUIElelement(),"Placement test input preconditions failed.");
            var placement=new PlacementProcess(action,map,worker.transform.position+Vector3.right*10);
            typeof(PlacementProcess).GetField("m_HighlightPositions",Private).SetValue(placement,Array.Empty<Vector3Int>());
            field.SetValue(game,placement);
            var unbound=temporary.AddComponent<WorkerUnit>();
            foreach(Unit active in new Unit[]{null,unbound,worker})
            {
                game.ActiveUnit=active;
                if(active==worker)stateProperty.SetValue(worker,UnitState.Dead);
                typeof(GameManager).GetMethod("TryPlaceCurrentBuild",Private).Invoke(game,new object[]{point});
                Check(game.Wood==wood&&game.Gold==gold&&bridge.Gameplay.ActorCount==actors&&ReferenceEquals(field.GetValue(game),placement),
                    "Invalid worker finalized placement or spent resources.");
            }
            return "PASS placement rejects missing/unbound/dead worker before finalizing or debiting";
        }
        finally
        {
            stateProperty.SetValue(worker,state);game.ActiveUnit=selected;field.SetValue(game,previous);
            for(int i=0;i<events.Length;i++)events[i].enabled=enabled[i];
            UnityEngine.Object.DestroyImmediate(temporary);
        }
    }

    public static string CheckValidation()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var worker=UnityEngine.Object.FindFirstObjectByType<WorkerUnit>();
        var action=AssetDatabase.LoadAssetAtPath<BuildActionSo>("Assets/Game/ScriptableObjects/Units/BuildStoragePitAction.asset");
        var tower=AssetDatabase.LoadAssetAtPath<BuildActionSo>("Assets/Game/ScriptableObjects/Units/BuildTowerAction.asset");
        var position=worker.transform.position+Vector3.right*10;
        var panel=(ActionQueuePanel)typeof(GameManager).GetField("m_ActionQueuePanel",Private).GetValue(game);
        int QueueCount()=>((ICollection)typeof(ActionQueuePanel).GetField("m_QueueEntries",Private).GetValue(panel)).Count;
        int actors=bridge.Gameplay.ActorCount,queue=QueueCount();
        int wood=game.Wood,gold=game.Gold;
        int rejected=0;
        void Reject(Action create)
        {
            bool threw=false;
            try{create();}catch(ArgumentException){threw=true;}catch(InvalidOperationException){threw=true;}
            Check(threw,"Invalid construction was accepted.");
            Check(bridge.Gameplay.ActorCount==actors&&QueueCount()==queue&&game.Wood==wood&&game.Gold==gold,
                "Rejected construction changed actor, queue or wallet.");
            rejected++;
        }
        var temporary=new GameObject("D3 construction dependency fixtures");temporary.SetActive(false);
        var emptyAction=ScriptableObject.CreateInstance<BuildActionSo>();
        try
        {
            Reject(()=>new BuildingProcess(null,position,worker,game,bridge));
            Reject(()=>new BuildingProcess(emptyAction,position,worker,game,bridge));
            Reject(()=>new BuildingProcess(action,position,null,game,bridge));
            Reject(()=>new BuildingProcess(action,position,worker,null,bridge));
            Reject(()=>new BuildingProcess(action,position,worker,game,null));
            var wrongManager=temporary.AddComponent<GameManager>();
            Reject(()=>new BuildingProcess(action,position,worker,wrongManager,bridge));
            var unboundWorker=temporary.AddComponent<WorkerUnit>();
            Reject(()=>new BuildingProcess(action,position,unboundWorker,game,bridge));
            if(!bridge.Gameplay.CanUseTowers)Reject(()=>new BuildingProcess(tower,position,worker,game,bridge));
            return $"PASS rejected={rejected}; missing action/prefab/worker/manager/bridge, wrong owner, unbound worker; actors={actors},queue={queue},wallet={wood}/{gold} unchanged; "
                +(bridge.Gameplay.CanUseTowers?"legacy capabilities open":"locked tower rejected");
        }
        finally{UnityEngine.Object.DestroyImmediate(temporary);UnityEngine.Object.DestroyImmediate(emptyAction);}
    }

    public static string RunSampleScene()
    {
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SampleScene","Run in SampleScene.");
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var map=UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var worker=UnityEngine.Object.FindFirstObjectByType<WorkerUnit>();
        var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();
        var action=AssetDatabase.LoadAssetAtPath<BuildActionSo>("Assets/Game/ScriptableObjects/Units/BuildStoragePitAction.asset");
        var center=map.WalkableTilemap.WorldToCell(hall.transform.position);
        Vector3 position=default;bool found=false;
        for(int radius=5;radius<=20&&!found;radius++)
            for(int y=-radius;y<=radius&&!found;y++)for(int x=-radius;x<=radius&&!found;x++)
            {
                if(Math.Abs(x)!=radius&&Math.Abs(y)!=radius)continue;
                var cell=center+new Vector3Int(x,y,0);bool valid=true;
                for(int dy=0;dy<action.BuildingSize.y;dy++)for(int dx=0;dx<action.BuildingSize.x;dx++)
                    valid &= map.CanPlaceTiles(cell+action.OriginalOffset+new Vector3Int(dx,dy,0));
                if(valid){found=true;position=map.WalkableTilemap.CellToWorld(cell);}
            }
        Check(found,"No valid construction fixture position found.");
        int actors=bridge.Gameplay.ActorCount;int wood=game.Wood,gold=game.Gold;
        Check((bool)typeof(GameManager).GetMethod("TryDeductResources",Private).Invoke(game,new object[]{action.GoldCost,action.WoodCost}),"Construction debit rejected.");
        var process=new BuildingProcess(action,position,worker,game,bridge);
        var structure=(StructureUnit)typeof(BuildingProcess).GetField("m_Structure",Private).GetValue(process);
        Check(structure.IsDotsControlled,"Legacy scene construction lost ECS binding.");
        Physics2D.SyncTransforms();typeof(SwarmGameBridge).GetMethod("RefreshNavigation",Private).Invoke(bridge,null);
        var sync=typeof(SwarmGameBridge).GetMethod("SynchronizeGameplay",Private);
        var world=bridge.Gameplay.World;var time=world.Time;float finishedAt=0;
        try
        {
            for(int tick=1;tick<=2400;tick++)
            {
                world.SetTime(new TimeData(time.ElapsedTime+tick*.05,.05f));bridge.Gameplay.Update();
                if(tick%10==0){sync.Invoke(bridge,null);Physics2D.SyncTransforms();}
                if(process.m_isFinished){finishedAt=tick*.05f;break;}
            }
            Check(process.m_isFinished&&bridge.Gameplay.ActorCount==actors+1&&!structure.isUnderConstruction
                &&structure.SpriteRenderer.sprite==action.CompleteSprite,"SampleScene construction did not complete exactly once.");
            Check(game.Wood==wood-action.WoodCost&&game.Gold==gold-action.GoldCost,"Construction debit mismatch.");
            CheckCompletedQueue();
            return $"PASS SampleScene injected construction: actors={actors}->{bridge.Gameplay.ActorCount}; completedAt={finishedAt:F2}s; foundation->complete; debit100/100; UI queue cleared";
        }
        finally { world.SetTime(time); }
    }

    public static string CheckDestroyedView()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var worker=UnityEngine.Object.FindFirstObjectByType<WorkerUnit>();
        var action=AssetDatabase.LoadAssetAtPath<BuildActionSo>("Assets/Game/ScriptableObjects/Units/BuildStoragePitAction.asset");
        var process=new BuildingProcess(action,new Vector3(1000,1000),worker,game,bridge);
        var structure=(StructureUnit)typeof(BuildingProcess).GetField("m_Structure",Private).GetValue(process);
        var entity=structure.DotsEntity;
        UnityEngine.Object.DestroyImmediate(structure.gameObject);
        process.UpdateDotsPresentation();process.Update();process.ApplyDotsStage(true);
        Check(!bridge.Gameplay.EntityManager.Exists(entity),"Destroyed view retained its Entity.");
        return "PASS destroyed construction view/entity: presentation/update/stage calls do not dereference removed entity";
    }
}
