// Unity MCP script-execute, D3ProductionValidation. Disposable paused Play session; do not save runtime changes.
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using UnityEngine;
using UnityEditor;
using RTS.DOTS;

public static class D3ProductionValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static T Asset<T>(string name) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>("Assets/Game/ScriptableObjects/Units/"+name+".asset");
    static int Queue(ProductionBuilding p)=>Find<SwarmGameBridge>().Gameplay.EntityManager.GetBuffer<TrainingRequest>(p.DotsEntity).Length;
    static int UiCount()
    {
        var panel=(ActionQueuePanel)typeof(GameManager).GetField("m_ActionQueuePanel",Private).GetValue(Find<GameManager>());
        return ((ICollection)typeof(ActionQueuePanel).GetField("m_QueueEntries",Private).GetValue(panel)).Count;
    }
    static int Views()=>((IDictionary)typeof(SwarmGameBridge).GetField("m_TrainingViews",Private).GetValue(Find<SwarmGameBridge>())).Count;
    static void Sync(SwarmGameBridge b)=>typeof(SwarmGameBridge).GetMethod("SynchronizeGameplay",Private).Invoke(b,null);

    public static string CheckFogBeforeRuntimeData()
    {
        var fog=Find<FogOfWarManager>();
        var affectable=UnityEngine.Object.FindObjectsByType<FogOfWarAffectable>(FindObjectsSortMode.None).First(a=>!a.AlwaysVisible);
        foreach(string name in new[]{"m_VisibilityCounts","m_Explored","m_TilemapManager"})
        {
            var field=typeof(FogOfWarManager).GetField(name,Private);var original=field.GetValue(fog);
            try{field.SetValue(fog,null);fog.ApplyVisibilityToAffectable(affectable);}
            finally{field.SetValue(fog,original);}
        }
        fog.ApplyVisibilityToAffectable(affectable);
        return "PASS fog visibility safely defers until map/arrays exist and applies again after restoration";
    }

    public static string CheckAdmission()
    {
        var game=Find<GameManager>();var b=Find<SwarmGameBridge>();var hall=Find<TownHall>();var barrack=Find<Barrack>();var range=Find<ArcheryRange>();
        var villager=Asset<SpawnVillagerActionSO>("SpawnVillagerAction");var warrior=Asset<SpawnWarriorActionSO>("SpawnWarriorAction");var archer=Asset<SpawnArcherActionSO>("SpawnArcherAction");
        Check(new ProductionBuilding[]{hall,barrack,range}.All(p=>p.IsProductionOwner(game)),"Initial production references not injected.");
        Check(UiCount()==0&&Views()==0&&Queue(hall)==0&&Queue(barrack)==0&&Queue(range)==0,"Use a fresh session with no queued training.");
        hall.InitializeProduction(game,b);hall.InitializeProduction(game,b);
        var wallet=b.Gameplay.Wallet;int actors=b.Gameplay.ActorCount,rejected=0;
        var selected=game.ActiveUnit;float scale=Time.timeScale;bool paused=b.Simulation.Paused;
        var fixture=new GameObject("D3 producer dependency fixtures");fixture.SetActive(false);
        var invalid=UnityEngine.Object.Instantiate(villager);
        void Unchanged()
        {Check(b.Gameplay.Wallet.Equals(wallet)&&b.Gameplay.ActorCount==actors&&UiCount()==0&&Views()==0&&Queue(hall)==0&&Queue(barrack)==0&&Queue(range)==0,"Rejected command changed queue/wallet/actor.");rejected++;}
        void Reject(Action action)
        {bool threw=false;try{action();}catch(ArgumentException){threw=true;}catch(InvalidOperationException){threw=true;}Check(threw,"Invalid dependency accepted.");Unchanged();}
        try
        {
            Reject(()=>hall.InitializeProduction(null,b));Reject(()=>hall.InitializeProduction(game,null));
            var wrong=fixture.AddComponent<GameManager>();Reject(()=>hall.InitializeProduction(wrong,b));
            var unbound=fixture.AddComponent<TownHall>();Reject(()=>unbound.InitializeProduction(game,b));
            Check(!unbound.EnqueueVillagerSpawn(villager),"Uninitialized producer accepted training.");Unchanged();
            wrong.ActiveUnit=hall;villager.Excute(wrong);Unchanged();
            game.ActiveUnit=hall;
            Time.timeScale=0;villager.Excute(game);Time.timeScale=scale;Unchanged();
            b.Simulation.Paused=true;villager.Excute(game);b.Simulation.Paused=paused;Unchanged();
            var hallBody=b.Gameplay.EntityManager.GetComponentData<CombatBody>(hall.DotsEntity);
            try{var dead=hallBody;dead.Health=0;b.Gameplay.EntityManager.SetComponentData(hall.DotsEntity,dead);villager.Excute(game);Unchanged();}
            finally{b.Gameplay.EntityManager.SetComponentData(hall.DotsEntity,hallBody);}
            ProceduralMapRuntime.BeginComposition();try{villager.Excute(game);}finally{ProceduralMapRuntime.CompleteComposition();}Unchanged();
            typeof(SpawnVillagerActionSO).GetField("m_TrainDuration",Private).SetValue(invalid,0f);invalid.Excute(game);Unchanged();
            var prefabField=typeof(TownHall).GetField("m_VillagerPrefab",Private);var prefab=prefabField.GetValue(hall);
            try{prefabField.SetValue(hall,null);villager.Excute(game);Unchanged();}finally{prefabField.SetValue(hall,prefab);}
            game.AddResources(0,0,-wallet.Meat);villager.Excute(game);Check(Queue(hall)==0&&UiCount()==0,"Insufficient funds queued training.");game.AddResources(0,0,wallet.Meat);Unchanged();
            if(!b.Gameplay.CanUseArchery){game.ActiveUnit=range;archer.Excute(game);Unchanged();}
            game.ActiveUnit=hall;villager.Excute(game);
            Check(Queue(hall)==1&&UiCount()==1&&Views()==1&&game.Meat==wallet.Meat-villager.MeatCost,"Accepted villager command did not debit exactly once.");
            var paid=b.Gameplay.Wallet;villager.Excute(game);
            Check(Queue(hall)==1&&UiCount()==1&&b.Gameplay.Wallet.Equals(paid),"Same-frame rejected command charged again.");
            hall.InitializeProduction(game,b);Check(Queue(hall)==1,"Repeat init cleared pending training.");
            game.ActiveUnit=barrack;warrior.Excute(game);
            Check(Queue(barrack)==1&&UiCount()==2&&Views()==2&&game.Gold==paid.Gold-warrior.GoldCost
                &&game.Wood==paid.Wood-warrior.WoodCost&&game.Meat==paid.Meat-warrior.MeatCost,"Warrior queue/debit mismatch.");
            return $"PASS production injection/repeat; rejected cases={rejected} without side effects; villager+warrior accepted/debited; same-frame duplicate not charged; pending2";
        }
        finally{Time.timeScale=scale;b.Simulation.Paused=paused;game.ActiveUnit=selected;UnityEngine.Object.DestroyImmediate(fixture);UnityEngine.Object.DestroyImmediate(invalid);}
    }

    public static string CompleteQueued()
    {
        var b=Find<SwarmGameBridge>();var gp=b.Gameplay;var world=gp.World;var time=world.Time;
        var old=UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).Select(u=>u.GetInstanceID()).ToHashSet();
        var checkedExit=new System.Collections.Generic.HashSet<int>();
        int count=gp.ActorCount,pending=Views();Check(pending>0,"Queue a request first.");
        var wallet=gp.Wallet;float completedAt=0;
        try
        {
            for(int tick=1;tick<=2400;tick++)
            {
                world.SetTime(new TimeData(time.ElapsedTime+tick*.05,.05f));gp.Update();Sync(b);
                foreach(var unit in UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
                    if(!old.Contains(unit.GetInstanceID())&&checkedExit.Add(unit.GetInstanceID()))
                        Check(gp.TryGetActor(unit.DotsEntity,out var spawned,out _)&&spawned.Moving!=0,"Factory did not send ECS exit movement at spawn.");
                if(Views()==0){completedAt=tick*.05f;break;}
            }
            Check(Views()==0&&UiCount()==0&&gp.ActorCount==count+pending&&gp.Wallet.Equals(wallet),"Training completion duplicated/lost units/UI or charged twice.");
            var units=UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).Where(u=>!old.Contains(u.GetInstanceID())).ToArray();
            Check(units.Length==pending&&units.All(u=>u.IsDotsControlled&&(!u.TryGetComponent<AIPawn>(out var pawn)||!pawn.enabled)),"Spawned view not bound or legacy AI still active.");
            Check(checkedExit.Count==pending,"Not all spawned units had exit movement checked.");
            return $"PASS training completedAt={completedAt:F2}s simulated; actors={count}->{gp.ActorCount}; newUnits={string.Join(",",units.Select(u=>u.GetType().Name))}; UI/maps empty; no second debit; ECS exit movement";
        }
        finally{world.SetTime(time);}
    }

    public static string QueueOneVillager()
    {
        var game=Find<GameManager>();var b=Find<SwarmGameBridge>();var hall=Find<TownHall>();var action=Asset<SpawnVillagerActionSO>("SpawnVillagerAction");
        var selected=game.ActiveUnit;var before=b.Gameplay.Wallet;int count=Queue(hall),ui=UiCount();
        try
        {
            game.ActiveUnit=hall;action.Excute(game);
            Check(Queue(hall)==count+1&&UiCount()==ui+1&&game.Meat==before.Meat-action.MeatCost,"Next-frame command was lost or debited incorrectly.");
            var queue=b.Gameplay.EntityManager.GetBuffer<TrainingRequest>(hall.DotsEntity);
            for(int i=1;i<queue.Length;i++)Check(queue[i].RequestId>queue[i-1].RequestId,"FIFO request order changed.");
            return $"PASS frame={Time.frameCount}; TownHall FIFO length={queue.Length}, accepted one request and one debit";
        }
        finally{game.ActiveUnit=selected;}
    }

    public static string QueueArcherAfterUnlock()
    {
        var game=Find<GameManager>();var b=Find<SwarmGameBridge>();var gp=b.Gameplay;var range=Find<ArcheryRange>();
        Check(Views()==0,"Complete other queues first.");
        var world=gp.World;var time=world.Time;var selected=game.ActiveUnit;
        try
        {
            if(!gp.CanUseArchery)
            {
                var root=Find<GameSceneCompositionRoot>();game.AddResources(1000,1000,1000);
                Check(root.Research.Start("military_archery",Find<TownHall>())==ResearchResult.Started,"Could not start real archery research.");
                for(int i=1;i<=2000&&!gp.CanUseArchery;i++){world.SetTime(new TimeData(time.ElapsedTime+i*.05,.05f));gp.Update();Sync(b);}
                Check(gp.CanUseArchery,"Archery did not unlock.");
            }
            var action=Asset<SpawnArcherActionSO>("SpawnArcherAction");var wallet=gp.Wallet;game.ActiveUnit=range;action.Excute(game);
            Check(Queue(range)==1&&Views()==1&&game.Gold==wallet.Gold-action.GoldCost&&game.Wood==wallet.Wood-action.WoodCost&&game.Meat==wallet.Meat-action.MeatCost,"Unlocked archer queue/debit failed.");
            var paid=gp.Wallet;action.Excute(game);Check(Queue(range)==1&&gp.Wallet.Equals(paid),"Duplicate archer charged twice.");
            return "PASS real archery unlock or legacy open capability; archer accepted/debited exactly once; duplicate not charged";
        }
        finally{world.SetTime(time);game.ActiveUnit=selected;}
    }

    public static string CheckPoolAndRemovedProducer()
    {
        var game=Find<GameManager>();var b=Find<SwarmGameBridge>();var gp=b.Gameplay;var action=Asset<SpawnWarriorActionSO>("SpawnWarriorAction");
        Check(Views()==0&&UiCount()==0,"Complete other queues first.");int actors=gp.ActorCount;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Buildings/Barrack.prefab");
        bool invalidScene=false;try{RuntimeObjectPool.Spawn(prefab,Vector3.zero,Quaternion.identity,default);}
        catch(ArgumentException){invalidScene=true;}
        Check(invalidScene&&gp.ActorCount==actors&&Views()==0&&UiCount()==0,"Invalid pool scene was accepted or changed the scene.");
        var first=RuntimeObjectPool.Spawn(prefab,new Vector3(1000,1000),Quaternion.identity,game.gameObject.scene);var producer=first.GetComponent<Barrack>();b.Register(producer);
        Check(producer.IsProductionOwner(game)&&producer.EnqueueWarriorSpawn(action)&&Views()==1&&UiCount()==1,"Pooled producer did not bind/queue.");
        var oldEntity=producer.DotsEntity;RuntimeObjectPool.Release(first);
        Check(!producer.IsProductionInitialized&&!gp.EntityManager.Exists(oldEntity)&&gp.ActorCount==actors&&Views()==0&&UiCount()==0,"Pool return retained producer dependency/entity/request/UI.");
        var reused=RuntimeObjectPool.Spawn(prefab,new Vector3(1000,1000),Quaternion.identity,game.gameObject.scene);var again=reused.GetComponent<Barrack>();
        Check(!again.IsSceneInitialized,"Raw pool retained the prior scene dependency.");b.Register(again);
        Check(reused==first&&reused.scene==game.gameObject.scene&&again.IsProductionOwner(game)&&again.DotsEntity!=oldEntity&&again.EnqueueWarriorSpawn(action),"Pool reuse did not inject a fresh producer in the owner scene.");
        RuntimeObjectPool.Release(reused);Check(gp.ActorCount==actors&&Views()==0&&UiCount()==0,"Reused producer leaked training.");
        var view=b.SpawnGameplay(prefab,new Vector3(1000,1000),Quaternion.identity);var removed=view.GetComponent<Barrack>();
        Check(removed.EnqueueWarriorSpawn(action)&&Views()==1,"Destroy fixture did not queue.");UnityEngine.Object.DestroyImmediate(view);
        Check(gp.ActorCount==actors&&Views()==0&&UiCount()==0,"Destroy retained producer request/UI.");
        var world=gp.World;var time=world.Time;
        try{for(int i=1;i<=400;i++){world.SetTime(new TimeData(time.ElapsedTime+i*.05,.05f));gp.Update();Sync(b);}}
        finally{world.SetTime(time);}
        Check(gp.ActorCount==actors&&Views()==0&&UiCount()==0,"Canceled producer spawned a ghost unit.");
        return "PASS producer return/unbind/reuse/rebind with new Entity; pending request/UI cleared on return/destroy; no ghost after20s simulated. Pool tested within this scene only";
    }
}
