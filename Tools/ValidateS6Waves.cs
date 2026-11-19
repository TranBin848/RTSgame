// Unity MCP script-execute: S6WaveValidation.Run in a fresh paused Play session.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RTS.DOTS;

public static class S6WaveValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;

    public static string Run()
    {
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var day=UnityEngine.Object.FindFirstObjectByType<DayNightCycleManager>();
        var wave=UnityEngine.Object.FindFirstObjectByType<EnemyWaveManager>();
        var generator=UnityEngine.Object.FindFirstObjectByType<ProceduralMapAuthoring>();
        var map=UnityEngine.Object.FindFirstObjectByType<TilemapManager>();
        var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();
        var sim=bridge.Simulation;var em=sim.EntityManager;var world=sim.World;
        var oldTime=world.Time;var random=UnityEngine.Random.state;
        const float dt=.02f;
        Check(bridge.IsSceneBound&&wave.IsInitialized&&day.CurrentDay==1&&day.CurrentPhase==DayNightPhase.Day,"Need fresh day1 scene.");
        var updateDay=typeof(DayNightCycleManager).GetMethod("Tick",Private);
        var updateWave=typeof(EnemyWaveManager).GetMethod("Tick",Private);
        var updateBridge=typeof(SwarmGameBridge).GetMethod("Update",Private);
        var refresh=typeof(SwarmGameBridge).GetMethod("RefreshNavigation",Private);
        using var query=em.CreateEntityQuery(typeof(SwarmAgent),typeof(CombatBody));
        var initial=new HashSet<Entity>();
        using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var e in entities)initial.Add(e);
        var born=new Dictionary<Entity,float>();var arrived=new Dictionary<Entity,float>();
        var startTimes=new List<string>();var waveTimes=new List<float>();
        float elapsed=0;int peak=0,navVersion=map.NavigationVersion;
        Action<int> onWave=index=>{startTimes.Add($"{index}@{elapsed:F2}s");waveTimes.Add(elapsed);};
        UnityEngine.Events.UnityAction<int> listener=index=>onWave(index);
        wave.OnWaveStarted+=listener;
        int baselineSpawned=sim.TotalSpawned;
        int expected=Enumerable.Range(0,wave.Definition.GetWaveCountForDay(1)).Sum(i=>wave.Definition.GetEnemyCountForWave(1,i));
        var expectedTimes=Enumerable.Range(0,wave.Definition.GetWaveCountForDay(1)).Select(i=>wave.GetWaveStartNormalized(i)*day.Definition.NightDuration).ToArray();
        int retreatAlive=0;float cleanedAt=-1;string approaches="";
        try
        {
            UnityEngine.Random.InitState(2026);
            typeof(DayNightCycleManager).GetMethod("SetPhase",Private).Invoke(day,new object[]{DayNightPhase.Night,false});
            Check(wave.IsNightWaveActive,"Night event did not reach wave.");
            if(generator!=null&&generator.GatewayCount>0)
            {
                float radius=(float)typeof(EnemyWaveManager).GetField("m_CurrentTownHallRange",Private).GetValue(wave)+wave.Definition.SpawnExtraRange;
                Check(!generator.TryGetGatewayApproachPosition(-1,hall.transform.position,radius,out _)
                    &&!generator.TryGetGatewayApproachPosition(2,hall.transform.position,radius,out _)
                    &&!generator.TryGetGatewayApproachPosition(0,hall.transform.position,float.NaN,out _)
                    &&!generator.TryGetGatewayApproachPosition(0,hall.transform.position,-1,out _)
                    &&!generator.TryGetGatewayApproachPosition(0,hall.transform.position,100000,out _),"Invalid approach input accepted.");
                var positions=new List<Vector3>();
                for(int i=0;i<generator.GatewayCount;i++)
                {
                    Check(generator.TryGetGatewayApproachPosition(i,hall.transform.position,radius,out var point),"Approach missing.");
                    float distance=Vector2.Distance(point,hall.transform.position);
                    Check(distance>=radius&&distance<radius+1.5f,"Approach moved inside the spawn radius or too far outside.");
                    foreach(var prefab in wave.Definition.EnemyPrefabs.Where(p=>p!=null))
                        Check(sim.TryResolveSpawnPosition((Vector2)point,prefab.GetComponent<Unit>().GetDotsSettings().Radius,out var resolved)
                            &&math.distance(resolved,(float2)(Vector2)point)<.01f,"Enemy radius did not fit protected approach.");
                    positions.Add(point);
                }
                Check(Vector2.Distance(positions[0],positions[1])>radius,"Gateway directions collapsed to one approach.");
                approaches=$"radius={radius:F2} gates={positions[0]}/{positions[1]}; ";
            }
            foreach(float invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity})
            {updateDay.Invoke(day,new object[]{invalid});updateWave.Invoke(wave,new object[]{invalid});}
            Check(sim.TotalSpawned==baselineSpawned&&day.CurrentPhaseProgress==0,"Invalid/paused tick advanced timers or spawned an enemy.");
            int tick=0;
            while(day.CurrentPhase==DayNightPhase.Night && elapsed<day.Definition.NightDuration+dt*3)
            {
                tick++;elapsed=tick*dt;
                world.SetTime(new TimeData(oldTime.ElapsedTime+elapsed,dt));
                updateDay.Invoke(day,new object[]{dt});updateWave.Invoke(wave,new object[]{dt});
                bridge.Gameplay.Update();sim.Update();updateBridge.Invoke(bridge,null);
                if(tick%10==0&&navVersion!=map.NavigationVersion){refresh.Invoke(bridge,null);navVersion=map.NavigationVersion;}
                using var entities=query.ToEntityArray(Allocator.Temp);
                peak=Math.Max(peak,entities.Length);
                var hallBody=em.GetComponentData<CombatBody>(hall.DotsEntity);
                foreach(var entity in entities)
                {
                    if(initial.Contains(entity))continue;
                    if(!born.ContainsKey(entity))born.Add(entity,elapsed);
                    var body=em.GetComponentData<CombatBody>(entity);
                    if(body.Health>0&&math.distance(body.Position,hallBody.Position)<10&&!arrived.ContainsKey(entity))arrived.Add(entity,elapsed);
                }
            }
            Check(day.CurrentPhase==DayNightPhase.Day&&day.CurrentDay==2,"Night did not transition to day2.");
            Check(sim.TotalSpawned-baselineSpawned==expected&&born.Count==expected,"Wave quota/spawn tracking mismatch.");
            Check(startTimes.Count==wave.Definition.GetWaveCountForDay(1),"Wave start count mismatch.");
            for(int i=0;i<waveTimes.Count;i++)
                Check(Math.Abs(waveTimes[i]-expectedTimes[i])<.25f,
                    "Wave started late relative to the UI schedule: "+string.Join(",",startTimes));
            using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var entity in entities)
            {
                var body=em.GetComponentData<CombatBody>(entity);
                if(body.Health<=0)continue;
                retreatAlive++;
                Check(em.GetComponentData<SwarmAgent>(entity).State==SwarmState.Retreating&&body.Targetable==0,"Day did not retreat/unselect a living enemy.");
            }
            float nightEnded=elapsed;
            for(int i=1;i<=(int)Math.Ceiling(15/dt);i++)
            {
                world.SetTime(new TimeData(oldTime.ElapsedTime+nightEnded+i*dt,dt));
                bridge.Gameplay.Update();sim.Update();updateBridge.Invoke(bridge,null);
                if(sim.AgentCount==0){cleanedAt=i*dt;break;}
            }
            Check(sim.AgentCount==0,"Enemies leaked after retreat timeout + cleanup.");
            Check(bridge.Gameplay.ActorCount<=25,"Wave created duplicate gameplay actors.");
            var label=generator!=null?$"{generator.GeneratedMap.Seed}/v{generator.GeneratedMap.GeneratorVersion}":"SampleScene";
            return $"PASS map={label}; {approaches}dt={dt:F4}; night={nightEnded:F2}s; spawned={born.Count}/{expected}; waves={string.Join(",",startTimes)}; "
                +$"arrivedWithin10={arrived.Count}/{born.Count}; firstArrival={(arrived.Count>0?arrived.Values.Min():-1):F2}s; peakAgents={peak}; retreatAlive={retreatAlive}; cleanupAfterDay={cleanedAt:F2}s; "
                +"perSpawn="+string.Join(";",born.OrderBy(p=>p.Value).Select(p=>$"{p.Value:F2}->{(arrived.TryGetValue(p.Key,out var t)?t.ToString("F2"):"none")}"));
        }
        finally{wave.OnWaveStarted-=listener;world.SetTime(oldTime);UnityEngine.Random.state=random;}
    }
}
