// Unity MCP script-execute, fresh paused Play session after Start. Discard fixtures on Exit Play.
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Entities;
using RTS.DOTS;

public static class D3UnitValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string CheckBindings()
    {
        var b=Find<SwarmGameBridge>();var game=Find<GameManager>();var map=Find<TilemapManager>();
        var units=UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Check(Application.isPlaying&&b!=null,"Enter Play and let scene composition finish before CheckBindings.");
        Check(b.IsSceneBound&&units.Length==25&&b.Gameplay.ActorCount==25,"Use a fresh bound scene.");
        foreach(var unit in units)
        {
            Check(unit.IsSceneInitialized&&unit.IsDotsControlled&&unit.SceneBridge==b,"Initial Unit not injected/bound.");
            unit.InitializeScene(game,map,b);b.Register(unit);b.Register(unit);
            Check(typeof(Unit).GetField("m_GameManager",Private).GetValue(unit)==game,"Wrong Unit manager.");
            var pawn=unit.GetComponent<AIPawn>();
            if(pawn!=null)Check(!pawn.enabled&&typeof(AIPawn).GetField("m_TilemapManager",Private).GetValue(pawn)==map,"Pawn deps/disabled state wrong.");
        }
        var friends=game.GetFriendlyUnits(true);var structures=game.GetPlayerStructures();
        Check(friends.Count==units.Count(u=>u.IsPlayer&&!u.IsBuilding)&&structures.Count==units.Count(u=>u.IsPlayer&&u.IsBuilding),"Manager registry omitted initial player units/structures.");
        Check(friends.Count==friends.Distinct().Count()&&structures.Count==structures.Distinct().Count()&&b.Gameplay.ActorCount==25,"Repeat registration duplicated lists/entities.");
        int rejected=0;
        foreach(Action action in new Action[]{()=>units[0].InitializeScene(null,map,b),()=>units[0].InitializeScene(game,null,b),()=>units[0].InitializeScene(game,map,null)})
        {try{action();}catch(ArgumentException){rejected++;}catch(InvalidOperationException){rejected++;}}
        Check(rejected==3,"Invalid/replacement context accepted.");
        return $"PASS {game.gameObject.scene.name}:25Units injected; repeat registration no duplicate;3invalid contexts rejected; AIPawn map injected/disabled";
    }
    public static string CheckWithoutGlobalBridge()
    {
        var b=Find<SwarmGameBridge>();var worker=Find<WorkerUnit>();var sheep=Find<SheepUnit>();var node=Find<Tree>();
        var field=typeof(SwarmGameBridge).GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);
        var saved=field.GetValue(null);
        try
        {
            field.SetValue(null,null);
            Check(worker.IsDotsControlled&&sheep.IsDotsControlled,"Unit still relies on global bridge.");
            worker.MoveTo(worker.transform.position+Vector3.right,DestinationSource.PlayerClick);
            Check(b.Gameplay.TryGetActor(worker.DotsEntity,out var actor,out _)&&actor.Moving!=0,"Injected movement failed.");
            worker.GetComponent<AIPawn>().Stop();
            Check(worker.TryAssignResourceNode(node),"Injected resource assignment failed.");
            worker.CancelActiveWork();worker.SetTarget(sheep);worker.SetTarget(null);
            Check(!worker.IsSheltered&&!sheep.IsClaimed,"Injected query failed.");
            return "PASS Unit move/target, AIPawn stop, Worker resource/cancel/query, Sheep query while global bridge is null";
        }
        finally{field.SetValue(null,saved);}
    }
    public static string CheckPool()
    {
        var b=Find<SwarmGameBridge>();var game=Find<GameManager>();int baseline=b.Gameplay.ActorCount;
        var sources=new Unit[]{Find<WorkerUnit>(),Find<RangerUnit>(),Find<SheepUnit>(),Find<Barrack>()};
        foreach(var source in sources)
        {
            string path=source is WorkerUnit ? "Assets/Game/Prefabs/Units/Worker.prefab"
                : source is RangerUnit ? "Assets/Game/Prefabs/Units/Archer.prefab"
                : source is SheepUnit ? "Assets/Game/Prefabs/TerrainObjects/Sheep/Sheep.prefab"
                : "Assets/Game/Prefabs/Units/Buildings/Barrack.prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject view=null;
            try
            {
                view=RuntimeObjectPool.Spawn(prefab,source.transform.position,Quaternion.identity,game.gameObject.scene);
                var unit=view.GetComponent<Unit>();
                Check(!unit.IsSceneInitialized&&!unit.IsDotsControlled,"Raw pool retained dependencies.");
                int navigation=Find<TilemapManager>().NavigationVersion;
                b.Register(unit);var old=unit.DotsEntity;
                var fog=Find<FogOfWarManager>();
                var fogOwner=typeof(FogOfWarAffectable).GetField("m_FogOfWarManager",Private);
                var affectables=unit.GetComponentsInChildren<FogOfWarAffectable>(true);
                Check(affectables.All(a=>fogOwner.GetValue(a)==fog),"Pooled unit did not receive scene fog.");
                if(unit is StructureUnit)Check(Find<TilemapManager>().NavigationVersion>navigation,"Structure binding did not invalidate navigation.");
                Check(unit.IsSceneInitialized&&unit.IsDotsControlled&&b.Gameplay.ActorCount==baseline+1,"Acquire/register failed.");
                if(unit is WorkerUnit worker)Check(worker.TryAssignResourceNode(Find<Tree>()),"Pooled worker cannot receive work.");
                navigation=Find<TilemapManager>().NavigationVersion;
                RuntimeObjectPool.Release(view);
                Check(affectables.All(a=>fogOwner.GetValue(a)==null),"Pool retained fog owner.");
                if(unit is StructureUnit)Check(Find<TilemapManager>().NavigationVersion>navigation,"Structure return did not invalidate navigation.");
                Check(!unit.IsSceneInitialized&&unit.SceneBridge==null&&!unit.IsDotsControlled&&!b.Gameplay.EntityManager.Exists(old)&&b.Gameplay.ActorCount==baseline,"Return leaked context/entity.");
                Check(typeof(Unit).GetField("m_GameManager",Private).GetValue(unit)==null,"Return retained manager.");
                var again=RuntimeObjectPool.Spawn(prefab,source.transform.position,Quaternion.identity,game.gameObject.scene);
                Check(again==view&&!unit.IsSceneInitialized,"Reuse did not start unbound.");b.Register(unit);
                Check(unit.DotsEntity!=old&&unit.SceneBridge==b&&unit.IsDotsControlled,"Reuse failed to bind fresh context/entity.");
                RuntimeObjectPool.Release(view);
                Check(b.Gameplay.ActorCount==baseline,"Reuse leaked actor.");
            }
            finally{if(view!=null)UnityEngine.Object.DestroyImmediate(view);}
        }
        return "PASS Worker/Ranger/Sheep/Barrack raw acquire guard, explicit register, return clears scene refs/Entity/fog, reuse binds new Entity; baseline actor count preserved";
    }

    public static string PrepareReloadPool()
    {
        var b=Find<SwarmGameBridge>();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Worker.prefab");
        var view=RuntimeObjectPool.Spawn(prefab,Find<WorkerUnit>().transform.position,Quaternion.identity,b.gameObject.scene);
        view.name="D3ReloadWorker";var unit=view.GetComponent<Unit>();b.Register(unit);RuntimeObjectPool.Release(view);
        Check(!unit.IsSceneInitialized&&unit.SceneBridge==null,"Return retained old scene context.");
        return "PASS prepared inactive D3ReloadWorker with no scene references; now menu->game before CheckReloadPool";
    }
    public static string CheckDirectUnregister()
    {
        var b=Find<SwarmGameBridge>();var game=Find<GameManager>();int actors=b.Gameplay.ActorCount,friends=game.GetFriendlyUnits(true).Count;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Worker.prefab");
        var view=b.SpawnGameplay(prefab,Find<WorkerUnit>().transform.position,Quaternion.identity);var unit=view.GetComponent<Unit>();
        try
        {
            Check(game.GetFriendlyUnits(true).Count==friends+1,"Factory missed manager registration.");
            b.Unregister(unit);
            Check(!unit.IsSceneInitialized&&b.Gameplay.ActorCount==actors&&game.GetFriendlyUnits(true).Count==friends,"Direct bridge unregister left manager entry.");
            b.Register(unit);Check(unit.IsSceneInitialized&&game.GetFriendlyUnits(true).Count==friends+1,"Direct rebind missed manager entry.");
        }
        finally{UnityEngine.Object.DestroyImmediate(view);}
        Check(game.GetFriendlyUnits(true).Count==friends&&b.Gameplay.ActorCount==actors,"Destroy after rebind leaked registry.");
        return "PASS direct bridge unregister/rebind/destroy keeps manager registry and Entity ownership synchronized";
    }
    public static string CheckReloadPool()
    {
        var pooled=Resources.FindObjectsOfTypeAll<PooledRuntimeObject>().Single(p=>p.name=="D3ReloadWorker");
        var old=pooled.gameObject;var b=Find<SwarmGameBridge>();var game=Find<GameManager>();int count=b.Gameplay.ActorCount;
        Check(!old.GetComponent<Unit>().IsSceneInitialized,"Pool retained previous gameplay scene.");
        var view=RuntimeObjectPool.Spawn(pooled.Prefab,Find<WorkerUnit>().transform.position,Quaternion.identity,game.gameObject.scene);
        try
        {
            Check(view==old&&view.scene==game.gameObject.scene,"Pool did not transfer the saved worker into the new gameplay scene.");
            var unit=view.GetComponent<WorkerUnit>();b.Register(unit);
            var owner=typeof(FogOfWarAffectable).GetField("m_FogOfWarManager",Private);
            var affectables=unit.GetComponentsInChildren<FogOfWarAffectable>(true);
            Check(affectables.All(a=>owner.GetValue(a)==Find<FogOfWarManager>()),"Reload reuse retained prior scene fog.");
            Check(unit.SceneBridge==b&&unit.IsDotsControlled&&typeof(Unit).GetField("m_GameManager",Private).GetValue(unit)==game,"Reused worker references prior scene services.");
            Check(unit.TryAssignResourceNode(Find<Tree>()),"Reused worker cannot work in new scene.");
            RuntimeObjectPool.Release(view);Check(b.Gameplay.ActorCount==count&&!unit.IsSceneInitialized,"Reload reuse leaked actor/dependency.");
            Check(affectables.All(a=>owner.GetValue(a)==null),"Reload return retained fog.");
            return "PASS pooled worker survived menu unload unbound, reused same view in new owner scene, bound new bridge/game, received resource work, returned cleanly";
        }
        finally{UnityEngine.Object.DestroyImmediate(old);}
    }
}
