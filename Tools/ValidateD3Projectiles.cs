// Run via Unity MCP script-execute in disposable paused Play Mode after scene Start.
using System;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS.DOTS;

public static class D3ProjectileValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static int Views()=>UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
    static StructureUnit Fixture(string name)=>(StructureUnit)Fixture(typeof(StructureUnit),name);
    static Unit Fixture(Type type,string name)
    {
        var go=new GameObject(name);go.SetActive(false);
        go.AddComponent<CapsuleCollider2D>();go.AddComponent<SpriteRenderer>();
        var unit=(Unit)go.AddComponent(type);go.SetActive(true);
        unit.InitializeScene(Find<GameManager>(),Find<TilemapManager>(),null);return unit;
    }
    public static string CheckFactory()
    {
        var game=Find<GameManager>();var bridge=Find<SwarmGameBridge>();var sim=bridge.Simulation;
        var owner=Find<RangerUnit>();var target=Find<SheepUnit>();var prefab=owner.DotsProjectilePrefab;
        Check(bridge.IsSceneBound&&owner.IsDotsControlled&&target.IsDotsControlled&&sim.ProjectileCount==0,"Use a fresh, bound scene without projectiles.");
        var world=sim.World;var time=world.Time;float scale=Time.timeScale;bool paused=sim.Paused;
        int actors=bridge.Gameplay.ActorCount,views=Views(),rejected=0;
        sim.TryGetBody(target.DotsEntity,out var body);var originalBody=body;
        var unbound=Fixture("D3 unbound projectile target");
        void Reject(bool accepted)
        {Check(!accepted&&sim.ProjectileCount==0&&Views()==views&&bridge.Gameplay.ActorCount==actors,"Rejected shot spawned/bound/allocated a projectile.");rejected++;}
        try
        {
            Reject(game.TrySpawnProjectile(null,owner,target,7,target.transform.position));
            Reject(game.TrySpawnProjectile(prefab,null,target,7,target.transform.position));
            Reject(game.TrySpawnProjectile(prefab,owner,null,7,target.transform.position));
            Reject(game.TrySpawnProjectile(prefab,owner,target,0,target.transform.position));
            Reject(game.TrySpawnProjectile(prefab,owner,unbound,7,target.transform.position));
            Time.timeScale=0;Reject(game.TrySpawnProjectile(prefab,owner,target,7,target.transform.position));Time.timeScale=scale;
            sim.Paused=true;Reject(game.TrySpawnProjectile(prefab,owner,target,7,target.transform.position));sim.Paused=paused;
            ProceduralMapRuntime.BeginComposition();try{Reject(game.TrySpawnProjectile(prefab,owner,target,7,target.transform.position));}finally{ProceduralMapRuntime.CompleteComposition();}
            body.Health=0;sim.EntityManager.SetComponentData(target.DotsEntity,body);
            Reject(game.TrySpawnProjectile(prefab,owner,target,7,target.transform.position));sim.EntityManager.SetComponentData(target.DotsEntity,originalBody);
            var bridgeField=typeof(GameManager).GetField("m_Bridge",Private);
            bridgeField.SetValue(game,null);
            try{Reject(game.TrySpawnProjectile(prefab,owner,target,7,target.transform.position));}
            finally{bridgeField.SetValue(game,bridge);}
            Check(game.TrySpawnProjectile(prefab,owner,target,7,new Vector3(originalBody.Position.x,originalBody.Position.y,0)),"Valid ECS shot was rejected.");
            Check(sim.ProjectileCount==1&&Views()==views&&bridge.Gameplay.ActorCount==actors,"ECS shot allocated a legacy view or extra actor.");
            using(var query=sim.EntityManager.CreateEntityQuery(typeof(SwarmProjectile)))
            {
                var shot=query.GetSingleton<SwarmProjectile>();
                Check(shot.Owner==owner.DotsEntity&&shot.Target==target.DotsEntity&&shot.Damage==7,"Projectile owner/target/damage mapping changed.");
            }
            for(int i=1;i<=40;i++){world.SetTime(new TimeData(time.ElapsedTime+i*.05,.05f));sim.Update();}
            Check(sim.TryGetBody(target.DotsEntity,out var after)&&after.Health==originalBody.Health-7&&sim.ProjectileCount==0,"ECS damage was lost or applied twice.");
            return $"PASS {game.gameObject.scene.name}: rejected={rejected} without allocation/binding; one ECS projectile, zero legacy views; owner/target/damage match; exactly7damage after2s simulated";
        }
        finally
        {
            Time.timeScale=scale;sim.Paused=paused;world.SetTime(time);
            sim.EntityManager.SetComponentData(target.DotsEntity,originalBody);
            UnityEngine.Object.DestroyImmediate(unbound.gameObject);
        }
    }

    public static string CheckLegacyPool()
    {
        var game=Find<GameManager>();var bridgeField=typeof(GameManager).GetField("m_Bridge",Private);var saved=bridgeField.GetValue(game);
        var prefab=Find<RangerUnit>().DotsProjectilePrefab;var owner=Fixture("D3 legacy owner");var target=Fixture("D3 legacy target");
        Projectile first=null,second=null;var foreign=SceneManager.CreateScene("D3 projectile foreign scene");
        int native=Find<SwarmGameBridge>().Simulation.ProjectileCount;
        var impact=typeof(Projectile).GetMethod("OnTriggerEnter2D",Private);
        try
        {
            bridgeField.SetValue(game,null);
            Check(game.TrySpawnProjectile(prefab,owner,target,7,owner.transform.position),"Explicit legacy shot rejected.");
            first=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Single();
            Check(first.gameObject.scene==game.gameObject.scene,"Legacy projectile has wrong scene.");
            int health=target.CurrentHealth;impact.Invoke(first,new object[]{target.Collider});impact.Invoke(first,new object[]{target.Collider});
            Check(target.CurrentHealth==health-7&&!first.gameObject.activeSelf,"Legacy impact applied damage twice or did not release.");
            Check(game.TrySpawnProjectile(prefab,owner,target,9,owner.transform.position),"Legacy reuse rejected.");
            second=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Single();
            Check(second==first,"Expected same pooled projectile instance.");
            impact.Invoke(second,new object[]{target.Collider});Check(target.CurrentHealth==health-16,"Reused projectile retained previous damage/hit state.");
            SceneManager.MoveGameObjectToScene(target.gameObject,foreign);
            Check(!game.TrySpawnProjectile(prefab,owner,target,7,owner.transform.position),"Cross-scene target accepted.");
            SceneManager.MoveGameObjectToScene(target.gameObject,game.gameObject.scene);
            first.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(first.gameObject,foreign);
            Check(game.TrySpawnProjectile(prefab,owner,target,3,owner.transform.position),"Cross-scene pooled reuse rejected.");
            Check(first.gameObject.scene==game.gameObject.scene&&first.gameObject.activeSelf,"Pool did not move projectile back to owner scene.");
            RuntimeObjectPool.Release(first.gameObject);
            Check(Find<SwarmGameBridge>().Simulation.ProjectileCount==native,"Legacy branch wrote native projectiles.");
            return "PASS explicit legacy branch: damage7 then9 exactly once, same pooled instance reset; foreign target rejected; pooled projectile reacquired from foreign scene into owner scene; no ECS projectile";
        }
        finally
        {
            bridgeField.SetValue(game,saved);
            if(first!=null)UnityEngine.Object.DestroyImmediate(first.gameObject);
            UnityEngine.Object.DestroyImmediate(owner.gameObject);UnityEngine.Object.DestroyImmediate(target.gameObject);
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(foreign);
        }
    }

    public static string CheckLegacyCallers()
    {
        var game=Find<GameManager>();var field=typeof(GameManager).GetField("m_Bridge",Private);var saved=field.GetValue(game);
        var prefab=Find<RangerUnit>().DotsProjectilePrefab;var target=Fixture("D3 caller target");
        try
        {
            field.SetValue(game,null);
            foreach(var type in new[]{typeof(TowerUnit),typeof(RangerUnit),typeof(EnemyRangerUnit)})
            {
                var owner=Fixture(type,"D3 "+type.Name);Projectile shot=null;
                try
                {
                    type.GetField("m_ProjectilePrefab",Private).SetValue(owner,prefab);
                    if(type==typeof(TowerUnit))type.GetMethod("OnAttackReady",Private).Invoke(owner,new object[]{target});
                    else
                    {
                        type.GetField("m_PendingShotTarget",Private).SetValue(owner,target);
                        type.GetField("m_PendingShotDamage",Private).SetValue(owner,7);
                        type.GetField("m_HasPendingShot",Private).SetValue(owner,true);
                        type.GetMethod("TryFirePendingShot",Private).Invoke(owner,null);
                        type.GetMethod("TryFirePendingShot",Private).Invoke(owner,null);
                    }
                    shot=UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Single();
                    Check(typeof(Projectile).GetField("m_Owner",Private).GetValue(shot)==owner,"Caller passed wrong owner.");
                    Check(typeof(Projectile).GetField("m_Target",Private).GetValue(shot)==target,"Caller passed wrong target.");
                }
                finally
                {
                    if(shot!=null)UnityEngine.Object.DestroyImmediate(shot.gameObject);
                    UnityEngine.Object.DestroyImmediate(owner.gameObject);
                }
            }
            return "PASS TowerUnit/RangerUnit/EnemyRangerUnit launch through manager factory with correct owner/target; repeated ranger pending callback creates one shot";
        }
        finally{field.SetValue(game,saved);UnityEngine.Object.DestroyImmediate(target.gameObject);}
    }
}
