// MCP script-execute, disposable paused Play session; run economy/construction before these checks.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RTS.DOTS;

public static class S6AcceptanceValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string CheckInput()
    {
        var game=Find<GameManager>();var b=Find<SwarmGameBridge>();var worker=Find<WorkerUnit>();
        var minimap=Find<MinimapFogUI>();var camera=Camera.main;var before=camera.transform.position;
        var selection=game.SelectedUnits.ToList();
        var select=typeof(GameManager).GetMethod("SetSelectedUnits",Private);
        try
        {
            Check(EventSystem.current!=null,"No live EventSystem.");
            select.Invoke(game,new object[]{new List<Unit>{worker}});
            Check(game.ActiveUnit==worker&&game.SelectedUnits.Count==1&&worker.isTargeted,"Selection adapter did not select worker.");
            typeof(GameManager).GetMethod("handleClickOnGround",Private).Invoke(game,new object[]{(Vector2)(worker.transform.position+Vector3.right*2)});
            Check(b.Gameplay.TryGetActor(worker.DotsEntity,out var actor,out _)&&actor.Moving!=0,"Ground click adapter did not send movement.");
            worker.GetComponent<AIPawn>().Stop();
            Canvas.ForceUpdateCanvases();
            var image=(RawImage)typeof(MinimapFogUI).GetField("m_MinimapImage",Private).GetValue(minimap);
            var rect=image.rectTransform;var canvas=image.canvas.rootCanvas;
            var raycaster=canvas.GetComponent<GraphicRaycaster>();
            foreach(var uv in new[]{new Vector2(.25f,.75f),new Vector2(.75f,.25f)})
            {
                var local=new Vector2(Mathf.Lerp(rect.rect.xMin,rect.rect.xMax,uv.x),Mathf.Lerp(rect.rect.yMin,rect.rect.yMax,uv.y));
                var screen=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(local));
                var evt=new PointerEventData(EventSystem.current){position=screen,pointerPressRaycast=new RaycastResult{module=raycaster}};
                if(uv.x<.5f)minimap.OnPointerDown(evt);else minimap.OnDrag(evt);
                var expected=(Vector3)typeof(MinimapFogUI).GetMethod("GetWorldPositionFromNormalized",Private).Invoke(minimap,new object[]{uv});
                Check(Vector2.Distance(camera.transform.position,expected)<.05f&&camera.transform.position.z==before.z,"Minimap pointer/drag camera mapping failed.");
            }
            return $"PASS seed={Find<ProceduralMapAuthoring>().GeneratedMap.Seed}; selection/ground-click adapter reaches ECS; minimap pointer+drag25/75percent focuses camera, z preserved; live EventSystem";
        }
        finally{camera.transform.position=before;select.Invoke(game,new object[]{selection});}
    }

    public static string RunDays()
    {
        var b=Find<SwarmGameBridge>();var day=Find<DayNightCycleManager>();var wave=Find<EnemyWaveManager>();var map=Find<TilemapManager>();
        var hall=Find<TownHall>();var generator=Find<ProceduralMapAuthoring>();var sim=b.Simulation;var gp=b.Gameplay;var em=gp.EntityManager;
        Check(sim.AgentCount<=2,"Use a scene without an active wave.");
        typeof(DayNightCycleManager).GetProperty("CurrentDay").SetValue(day,1);
        typeof(DayNightCycleManager).GetMethod("SetPhase",Private).Invoke(day,new object[]{DayNightPhase.Day,true});
        var world=gp.World;var time=world.Time;var random=UnityEngine.Random.state;
        var hallBody=em.GetComponentData<CombatBody>(hall.DotsEntity);var protectedHall=hallBody;protectedHall.Health=1000000;
        var tickDay=typeof(DayNightCycleManager).GetMethod("Tick",Private);var tickWave=typeof(EnemyWaveManager).GetMethod("Tick",Private);
        var sync=typeof(SwarmGameBridge).GetMethod("SynchronizeGameplay",Private);var refresh=typeof(SwarmGameBridge).GetMethod("RefreshNavigation",Private);
        var range=typeof(EnemyWaveManager).GetMethod("CalculateTownHallRange",Private);
        float radius=(float)range.Invoke(wave,new object[]{hall})+wave.Definition.SpawnExtraRange;
        Check(Find<GameManager>().GetPlayerStructures().Any(s=>s is not TownHall&&s.CanStoreWood),"Build the landmark depot before RunDays so expansion radius is exercised.");
        for(int i=0;i<generator.GatewayCount;i++)Check(generator.TryGetGatewayApproachPosition(i,hall.transform.position,radius,out _),"Expanded base has no approach.");
        int initialActors=gp.ActorCount,spawned=sim.TotalSpawned,expected=0,configured=0,nav=map.NavigationVersion,cycles=0;
        var byDay=new int[3];
        var scheduled=typeof(EnemyWaveManager).GetMethod("GetWaveStartTime",Private);
        for(int d=1;d<=3;d++)
            for(int w=0;w<wave.Definition.GetWaveCountForDay(d);w++)
            {
                int quota=wave.Definition.GetEnemyCountForWave(d,w);configured+=quota;
                float start=(float)scheduled.Invoke(wave,new object[]{d,w});
                for(int i=0;i<quota;i++)if(start+i*wave.Definition.GetSpawnIntervalForDay(d)<day.Definition.NightDuration)expected++;
            }
        float elapsed=0,cleanupStart=-1;const float dt=.05f;
        try
        {
            em.SetComponentData(hall.DotsEntity,protectedHall);
            using var query=em.CreateEntityQuery(typeof(SwarmAgent),typeof(CombatBody));
            using var initialEntities=query.ToEntityArray(Allocator.Temp);
            var initial=initialEntities.ToArray().ToHashSet();
            var seen=new HashSet<Entity>();
            int previousDay=day.CurrentDay;
            for(int tick=1;tick<=24000;tick++)
            {
                elapsed=tick*dt;world.SetTime(new TimeData(time.ElapsedTime+elapsed,dt));
                int beforeSpawn=sim.TotalSpawned;
                tickDay.Invoke(day,new object[]{dt});tickWave.Invoke(wave,new object[]{dt});gp.Update();sim.Update();sync.Invoke(b,null);
                if(day.CurrentDay<=3)byDay[day.CurrentDay-1]+=sim.TotalSpawned-beforeSpawn;
                if(tick%10==0&&map.NavigationVersion!=nav){refresh.Invoke(b,null);nav=map.NavigationVersion;}
                if(day.CurrentDay!=previousDay){cycles++;previousDay=day.CurrentDay;}
                using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var entity in entities)
                    if(!initial.Contains(entity)&&seen.Add(entity))
                    {
                        var body=em.GetComponentData<CombatBody>(entity);
                        Check(sim.TryResolveSpawnPosition(body.Position,body.Radius,out var position),"Spawn is not on navigable terrain.");
                    }
                Check(gp.ActorCount<=initialActors,"Day cycle duplicated gameplay actors.");
                if(day.CurrentDay>=4)
                {
                    if(cleanupStart<0)cleanupStart=elapsed;
                    if(sim.AgentCount==0)break;
                    Check(elapsed-cleanupStart<16,"Retreat leaked enemies into next day.");
                }
            }
            Check(day.CurrentDay==4&&cycles==3&&sim.AgentCount==0,"Three natural clock cycles did not complete/clean.");
            Check(sim.TotalSpawned-spawned==expected&&seen.Count==expected,$"Three-night quota mismatch: actual={sim.TotalSpawned-spawned},seen={seen.Count},expected={expected},byDay={string.Join(",",byDay)},radius={radius},actors={gp.ActorCount},elapsed={elapsed}.");
            return $"PASS seed={generator.GeneratedMap.Seed}; day1->4 elapsed={elapsed:F2}s simulated; expanded radius={radius:F2}; enemies={seen.Count}/{expected}scheduledWithinNight, configured={configured}, byDay={string.Join(",",byDay)}; cleanup0agents; actors={initialActors}->{gp.ActorCount}; clock reset to day1 then natural Tick phases, real ECS; only hall health protected";
        }
        finally
        {
            world.SetTime(time);UnityEngine.Random.state=random;
            if(em.Exists(hall.DotsEntity)){var body=em.GetComponentData<CombatBody>(hall.DotsEntity);body.Health=hallBody.Health;em.SetComponentData(hall.DotsEntity,body);}
        }
    }
}
