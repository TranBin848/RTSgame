using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Unity.Core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

// Real scene camera captures and native Move commands; presentation phase samples are explicit test probes.
public static class GameExperienceE5Capture
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Dir="Docs/Modules/GameExperience/Evidence/E5/";
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Private).Invoke(target,args);
    static void Check(bool value,string message){if(!value)throw new Exception("E5 capture: "+message);}
    static WorldDepthPresentation Depth=>Find<GameSceneCompositionRoot>().DepthPresentation;
    static void Refresh(){typeof(WorldDepthPresentation).GetField("m_NextScan",Private).SetValue(Depth,-1f);Depth.RefreshPresentation();}

    public static string Setup()
    {
        Check(EditorApplication.isPlaying&&EditorApplication.isPaused&&Find<GameSceneCompositionRoot>().IsReady,"Ready paused match required");
        var bridge=Find<SwarmGameBridge>();var hall=Find<TownHall>();var a=Find<ProceduralMapAuthoring>();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/TerrainObjects/Tree.prefab");
        var tree=bridge.SpawnGameplay(prefab,hall.transform.position+new Vector3(4,-3),Quaternion.identity);
        tree.name="E5 fixture tree";tree.transform.SetParent(a.GeneratedResourceRoot,true);
        SessionState.SetInt("E5.Tree",tree.GetInstanceID());
        SessionState.SetInt("E5.Worker",Find<GameManager>().GetFriendlyUnits(true).OfType<WorkerUnit>().First().gameObject.GetInstanceID());
        Physics2D.SyncTransforms();Call(bridge,"RefreshNavigation");
        return "Spawned/bound one test tree on cleared Start ground; step once before Routes. Test fixture is not a generated resource/quota change.";
    }

    static float Move(WorkerUnit worker,Vector3 target)
    {
        var b=Find<SwarmGameBridge>();var system=b.Gameplay;var original=system.World.Time;float elapsed=0;
        try
        {
            b.MoveActor(worker,target,true);
            for(int tick=1;tick<=2400;tick++)
            {
                elapsed=tick*.05f;system.World.SetTime(new TimeData(original.ElapsedTime+elapsed,.05f));system.Update();
                if(tick%5==0){Call(b,"SynchronizeGameplay");Physics2D.SyncTransforms();if(Vector2.Distance(worker.transform.position,target)<.8f)break;}
            }
            Check(Vector2.Distance(worker.transform.position,target)<.8f,$"Move not reached target={target},actual={worker.transform.position}");
            return elapsed;
        }
        finally{system.World.SetTime(original);}
    }

    public static string Routes()
    {
        Directory.CreateDirectory(Dir);
        var worker=((GameObject)EditorUtility.InstanceIDToObject(SessionState.GetInt("E5.Worker",0))).GetComponent<WorkerUnit>();
        var tree=(GameObject)EditorUtility.InstanceIDToObject(SessionState.GetInt("E5.Tree",0));var hall=Find<TownHall>();
        var game=Find<GameManager>();var fog=Find<FogOfWarManager>();var clock=Find<DayNightCycleManager>();var camera=Camera.main;
        var cameraControls=Find<CameraController>();cameraControls.LockCamera=true;
        if(!fog.IsDisabled)Call(fog,"ToggleFogOfWar");
        Call(game,"SetSelectedUnits",new List<Unit>{worker});
        var stages=new[]{("Tree-behind",tree.transform.position+new Vector3(0,1.5f)),("Tree-front",tree.transform.position+new Vector3(0,-1.5f)),
            ("Hall-behind",hall.transform.position+new Vector3(0,2.8f)),("Hall-front",hall.transform.position+new Vector3(1,-2))};
        var log=new System.Text.StringBuilder("E5 same four Move destinations in Day/Dusk/Night, actual ECS path movement ticked at0.05. Clock phase/progress set for presentation only, not natural transitions. Debug fog off for route visibility; extra tree bound as fixture; existing Hall. Camera1920x1080, ortho6.\n");
        camera.transform.position=(tree.transform.position+hall.transform.position)*.5f+new Vector3(0,0,-10);camera.orthographicSize=6;camera.aspect=16f/9;
        int fadedBehind=0;
        foreach(DayNightPhase phase in Enum.GetValues(typeof(DayNightPhase)))
        {
            typeof(DayNightCycleManager).GetProperty("CurrentPhase").SetValue(clock,phase);typeof(DayNightCycleManager).GetProperty("CurrentPhaseProgress").SetValue(clock,.5f);Call(clock,"UpdateEnvironmentEffects");
            var lights=Find<StructureNightLighting>();typeof(StructureNightLighting).GetField("m_NextRegistryRefresh",Private).SetValue(lights,-1f);lights.RefreshPresentation();
            foreach(var stage in stages)
            {
                float travel=Move(worker,stage.Item2);Refresh();
                var targetBlock=new MaterialPropertyBlock();(stage.Item1.StartsWith("Tree")?tree.GetComponent<SpriteRenderer>():hall.SpriteRenderer).GetPropertyBlock(targetBlock);
                float targetFade=targetBlock.GetFloat("_DepthFade");Check(stage.Item1.Contains("behind")?targetFade>.5f:targetFade==0,"named target fade does not match front/behind: "+stage.Item1);
                if(stage.Item1.Contains("behind")&&Depth.FadedOccluders>0)fadedBehind++;
                Check(Depth.SelectionRingCount==1,"selected worker ring missing");
                Save(camera,$"route-{phase}-{stage.Item1}.png");
                log.AppendLine($"{phase}/{stage.Item1}: actual={worker.transform.position}, target={stage.Item2}, travelSim={travel:F2}s (arrival tolerance0.8world), namedTargetFade={targetFade}, faded={Depth.FadedOccluders}, contacts={Depth.ContactCount}, ring={Depth.SelectionRingCount}, stretch={Depth.ShadowStretch:F2}, localLights={lights.ActiveLocalLights}");
            }
        }
        Check(fadedBehind>=3,"no roof/canopy occlusion detected along route");
        // Fog is restored and stale selection must not cause a hidden occluder to fade or emit shadows.
        Call(fog,"ToggleFogOfWar");Call(fog,"RefreshFog");Refresh();
        log.AppendLine("Fog restored after route; data-source fog mask remains enabled. Counts are camera/fixture observations, not FPS.");
        File.WriteAllText(Dir+"route-context.txt",log.ToString());return log.ToString();
    }

    public static string DenseGroup()
    {
        var b=Find<SwarmGameBridge>();var native=Find<RTS.DOTS.SwarmInstancedRenderer>();var fog=Find<FogOfWarManager>();var hall=Find<TownHall>();
        if(!fog.IsDisabled)Call(fog,"ToggleFogOfWar");
        var settings=RTS.DOTS.EnemySpawnSettings.Default;settings.Speed=0;settings.Damage=0;
        int accepted=0;for(int i=0;i<192&&accepted<96;i++)
        {
            var requested=new float2(hall.transform.position.x+3+(i%12)*.35f,hall.transform.position.y-3+(i/12)*.4f);
            if(b.Simulation.TryResolveSpawnPosition(requested,settings.Radius,out var resolved)&&b.Simulation.SpawnEnemy(resolved,settings)!=Unity.Entities.Entity.Null)accepted++;
        }
        var time=b.Simulation.World.Time;
        try{b.Simulation.World.SetTime(new TimeData(time.ElapsedTime+.05,.05f));b.Simulation.Update();}finally{b.Simulation.World.SetTime(time);}
        Call(Find<GameManager>(),"SetSelectedUnits",Find<GameManager>().GetFriendlyUnits(true).Where(u=>u!=null&&!u.IsBuilding&&u.IsPlayer).ToList());
        Refresh();native.FogEnabled=false;native.SendMessage("LateUpdate");
        Check(accepted>=64&&native.ContactInstances>=accepted,$"native group contact batch missing: accepted={accepted},contacts={native.ContactInstances},paused={b.Simulation.Paused},matchOver={b.Gameplay.IsMatchOver}");
        Save(Camera.main,"dense-native-group-Night-fog-off.png");
        string result=$"PASS dense synthetic group in real scene:{accepted} stationary zero-damage native enemies; native contacts={native.ContactInstances}, renderedInstances={native.VisibleInstances}, selected rings={Depth.SelectionRingCount}, body contacts={Depth.ContactCount}. Test-only spawn; not workload/performance acceptance.\n";
        File.WriteAllText(Dir+"dense-group.txt",result);return result;
    }

    public static string SetupPool()
    {
        var b=Find<SwarmGameBridge>();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Worker.prefab");
        var view=RuntimeObjectPool.Spawn(prefab,Find<TownHall>().transform.position+new Vector3(-3,-2),Quaternion.identity,b.gameObject.scene);b.Register(view.GetComponent<Unit>());
        SessionState.SetInt("E5.Pool",view.GetInstanceID());return "Worker spawned; step before PoolReturn";
    }
    public static string Zooms()
    {
        var camera=Camera.main;float old=camera.orthographicSize;
        foreach(float zoom in new[]{4f,6f,8f}){camera.orthographicSize=zoom;Save(camera,"zoom-"+zoom+"-Night.png");}
        camera.orthographicSize=old;File.WriteAllText(Dir+"zoom.txt","Captured actual scene at1920x1080, ortho4/6/8, same frozen camera center/Night/dense group. Point sampling and render anchor pixel snapping; physical transforms unchanged.\n");
        return "Saved actual-scene zoom4/6/8 captures";
    }
    public static string PoolReturn()
    {
        Refresh();var view=(GameObject)EditorUtility.InstanceIDToObject(SessionState.GetInt("E5.Pool",0));var worker=view.GetComponent<WorkerUnit>();
        Check(worker.IsDotsControlled,"pool worker not bound");worker.DespawnToPool();Refresh();
        Check(!worker.IsSceneInitialized&&view.GetComponent<SpriteRenderer>().sharedMaterial.shader.name!="RTS/Presentation/FootDepth","return retained scene presentation");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Units/Worker.prefab");var bridge=Find<SwarmGameBridge>();var reused=RuntimeObjectPool.Spawn(prefab,Find<TownHall>().transform.position+new Vector3(-3,2),Quaternion.identity,bridge.gameObject.scene);bridge.Register(reused.GetComponent<Unit>());
        Check(reused==view,"pool did not reuse same worker");return "PASS pool return restored source material/unbound; same view reacquired; step then CheckPool";
    }
    public static string CheckPool()
    {
        Refresh();var view=(GameObject)EditorUtility.InstanceIDToObject(SessionState.GetInt("E5.Pool",0));var worker=view.GetComponent<WorkerUnit>();var block=new MaterialPropertyBlock();worker.SpriteRenderer.GetPropertyBlock(block);
        Check(worker.IsDotsControlled&&worker.SpriteRenderer.sharedMaterial.shader.name=="RTS/Presentation/FootDepth"&&block.GetFloat("_DepthFade")==0,"pool rebind did not reset presentation");
        string result="PASS pool return removes E5 material/property state and unbinds; same view reused in same scene, re-registered with foot shader, no stale roof fade.\n";File.WriteAllText(Dir+"pool.txt",result);return result;
    }

    static void Save(Camera camera,string name)
    {
        Depth.RefreshPresentation();var native=Find<RTS.DOTS.SwarmInstancedRenderer>();native.FogEnabled=!Find<FogOfWarManager>().IsDisabled;native.EnvironmentColor=Find<DayNightCycleManager>().EnvironmentTint;native.SendMessage("LateUpdate");
        var target=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Dir+name,image.EncodeToPNG());}
        finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(image);}
    }
}
