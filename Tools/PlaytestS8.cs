// Scripted policy measurements, not a substitute for human playtest or performance profiling.
// Run a fresh ProceduralGame in paused Play Mode using script-execute, Economy or Military.
using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using RTS.DOTS;

public static class S8Playtest
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static T Asset<T>(string name) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>("Assets/Game/ScriptableObjects/Units/"+name+".asset");
    static object Field(object obj,string name)=>obj.GetType().GetField(name,Private).GetValue(obj);
    static void Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Private).Invoke(obj,args);
    public static string Economy()=>Run(true);
    public static string Military()=>Run(false);
    public static string Control()=>Run(false,true);
    static string Run(bool economy,bool control=false)
    {
        var b=Find<SwarmGameBridge>();var gp=b.Gameplay;var sim=b.Simulation;var em=gp.EntityManager;
        var day=Find<DayNightCycleManager>();var waves=Find<EnemyWaveManager>();var game=Find<GameManager>();
        var map=Find<TilemapManager>();var generator=Find<ProceduralMapAuthoring>();var hall=Find<TownHall>();
        if(!Find<GameSceneCompositionRoot>().IsReady||gp.IsMatchOver||gp.Wallet.Wood!=100||gp.Wallet.Gold!=100||gp.Wallet.Meat!=100||day.CurrentDay!=1)
            throw new Exception("Fresh, ready match with wallet100x3 required.");
        var order=economy?new[]{"economy_tools","economy_cargo","military_archery","military_training","expansion_tower","expansion_fortify"}
            :new[]{"military_archery","military_training","economy_tools","economy_cargo","expansion_tower","expansion_fortify"};
        var unlock=new Dictionary<string,float>();var log=new StringBuilder();
        int[] deposited=new int[3],gathered=new int[3],spent=new int[3];int trained=0,peakWorkers=0,peakSoldiers=0;
        float elapsed=0;int startSpawned=sim.TotalSpawned,nav=map.NavigationVersion,previousDay=1;
        bool expansion=false;float expansionAt=-1;string expansionAttempt="not affordable yet";
        var time=gp.World.Time;var random=UnityEngine.Random.state;
        var trainWorker=Asset<SpawnVillagerActionSO>("SpawnVillagerAction");
        var trainWarrior=Asset<SpawnWarriorActionSO>("SpawnWarriorAction");var trainArcher=Asset<SpawnArcherActionSO>("SpawnArcherAction");
        var workerPrefab=(GameObject)Field(hall,"m_VillagerPrefab");
        var barrack=Find<Barrack>();var archery=Find<ArcheryRange>();
        var warriorPrefab=(GameObject)Field(barrack,"m_WarriorPrefab");var archerPrefab=(GameObject)Field(archery,"m_ArcherPrefab");
        var depotAction=Asset<BuildActionSo>("BuildStoragePitAction");
        using var actors=em.CreateEntityQuery(typeof(GameplayActor),typeof(CombatBody));
        using var resources=em.CreateEntityQuery(typeof(GameplayResource));
        Entity[] Living(GameplayKind kind)
        {
            using var list=actors.ToEntityArray(Allocator.Temp);
            return list.ToArray().Where(e=>em.GetComponentData<CombatBody>(e).Health>0&&em.GetComponentData<GameplayActor>(e).Kind==kind).ToArray();
        }
        int Soldiers()=>Living(GameplayKind.Soldier).Length+Living(GameplayKind.Archer).Length;
        void Snapshot()
        {
            var wallet=gp.Wallet;float health=em.Exists(hall.DotsEntity)?em.GetComponentData<CombatBody>(hall.DotsEntity).Health:0;
            log.AppendLine($"t={elapsed:F2};day={day.CurrentDay};phase={day.CurrentPhase};W/G/M={wallet.Wood}/{wallet.Gold}/{wallet.Meat};workers={Living(GameplayKind.Worker).Length};soldiers={Soldiers()};hallHP={health:F1};spawned={sim.TotalSpawned-startSpawned}");
        }
        bool Train(ProductionBuilding producer,ActionSO action,GameObject prefab,float duration,IResourceCostAction cost)
        {
            if(producer==null||!gp.TryGetActor(producer.DotsEntity,out _,out var body)||body.Health<=0)return false;
            if(em.GetBuffer<TrainingRequest>(producer.DotsEntity).Length>=1)return false;
            var wallet=gp.Wallet;
            if(wallet.Wood<cost.WoodCost||wallet.Gold<cost.GoldCost||wallet.Meat<cost.MeatCost)return false;
            if(!b.EnqueueTraining(producer,action,prefab,duration,""))return false;
            gp.AddResources(-cost.WoodCost,-cost.GoldCost,-cost.MeatCost);trained++;return true;
        }
        log.AppendLine($"seed={generator.GeneratedMap.Seed};policy={(control?"no-actions":economy?"economy-first":"military-first")};dt=.05;normalHP;noResourceGrant;objective=5nights");
        try
        {
            UnityEngine.Random.InitState(generator.GeneratedMap.Seed+8000);
            typeof(DayNightCycleManager).GetProperty("CurrentDay").SetValue(day,1);Call(day,"SetPhase",DayNightPhase.Day,true);
            Snapshot();
            for(int tick=1;tick<=15000&&!gp.IsMatchOver;tick++)
            {
                elapsed=tick*.05f;gp.World.SetTime(new TimeData(time.ElapsedTime+elapsed,.05f));
                if(!control&&tick%100==1)
                {
                    var before=gp.Wallet;
                    if(gp.Research.Active==0)
                    {
                        var next=order.FirstOrDefault(id=>!gp.IsTechnologyUnlocked(id));
                        if(next!=null)gp.StartResearch(next,hall.DotsEntity);
                    }
                    var workers=Living(GameplayKind.Worker);peakWorkers=Math.Max(peakWorkers,workers.Length);peakSoldiers=Math.Max(peakSoldiers,Soldiers());
                    if(workers.Length<(economy?6:4))Train(hall,trainWorker,workerPrefab,trainWorker.TrainDuration,trainWorker);
                    if(Living(GameplayKind.Soldier).Length<8)Train(barrack,trainWarrior,warriorPrefab,trainWarrior.TrainDuration,trainWarrior);
                    if(gp.CanUseArchery&&Living(GameplayKind.Archer).Length<6)Train(archery,trainArcher,archerPrefab,trainArcher.TrainDuration,trainArcher);
                    using var nodes=resources.ToEntityArray(Allocator.Temp);
                    if(day.CurrentPhase==DayNightPhase.Day)
                        for(int i=0;i<workers.Length;i++)
                        {
                            var worker=workers[i];var data=em.GetComponentData<GameplayWorker>(worker);
                            if(data.Phase!=WorkPhase.None&&em.Exists(data.Node))continue;
                            int type=i%4<2?0:i%4==2?1:2;var pos=em.GetComponentData<CombatBody>(worker).Position;
                            var node=nodes.ToArray().Where(e=>{var r=em.GetComponentData<GameplayResource>(e);return r.Type==type&&r.Claims<r.Capacity;})
                                .OrderBy(e=>math.distancesq(pos,em.GetComponentData<GameplayResource>(e).Position)).FirstOrDefault();
                            if(node!=Entity.Null)gp.AssignResource(worker,node);
                        }
                    if(!expansion&&elapsed>180&&day.CurrentPhase==DayNightPhase.Day&&workers.Length>0
                        &&gp.Wallet.Wood>=depotAction.WoodCost&&gp.Wallet.Gold>=depotAction.GoldCost)
                    {
                        var landmark=generator.GeneratedMap.Layout.Landmark;
                        var cell=generator.OriginCell+new Vector3Int(landmark.X,landmark.Y,0);
                        var position=map.WalkableTilemap.CellToWorld(cell);
                        var placement=new PlacementProcess(depotAction,map,position);placement.ShowPlacementOutline();
                        typeof(PlacementProcess).GetMethod("HighLightTiles",Private).Invoke(placement,new object[]{position});
                        if(placement.TryFinalizePlacement(out var accepted))
                        {
                            var workerView=b.ViewFor(workers.Last()) as WorkerUnit;
                            if(workerView==null)throw new Exception("Expansion worker has no view.");
                            game.AddResources(-depotAction.GoldCost,-depotAction.WoodCost,0);
                            new BuildingProcess(depotAction,accepted,workerView,game,b);expansion=true;expansionAt=elapsed;expansionAttempt="landmark depot ordered";
                        }
                        else{placement.CleanUp();expansionAttempt="landmark placement rejected";}
                    }
                    var after=gp.Wallet;spent[0]+=before.Wood-after.Wood;spent[1]+=before.Gold-after.Gold;spent[2]+=before.Meat-after.Meat;
                }
                var oldWallet=gp.Wallet;
                var cargo=Living(GameplayKind.Worker).ToDictionary(e=>e,e=>em.GetComponentData<GameplayWorker>(e).Carried);
                Call(day,"Tick",.05f);Call(waves,"Tick",.05f);gp.Update();sim.Update();
                var newWallet=gp.Wallet;deposited[0]+=Math.Max(0,newWallet.Wood-oldWallet.Wood);deposited[1]+=Math.Max(0,newWallet.Gold-oldWallet.Gold);deposited[2]+=Math.Max(0,newWallet.Meat-oldWallet.Meat);
                foreach(var pair in cargo)
                    if(em.Exists(pair.Key)){var data=em.GetComponentData<GameplayWorker>(pair.Key);gathered[data.ResourceType]+=Math.Max(0,data.Carried-pair.Value);}
                Call(b,"SynchronizeGameplay");
                if(tick%10==0&&nav!=map.NavigationVersion){Call(b,"RefreshNavigation");nav=map.NavigationVersion;}
                foreach(var id in order)if(gp.IsTechnologyUnlocked(id)&&!unlock.ContainsKey(id))unlock[id]=elapsed;
                gp.EvaluateMatch(day.CurrentDay);
                if(day.CurrentDay!=previousDay){Snapshot();previousDay=day.CurrentDay;}
            }
            Snapshot();
            log.AppendLine($"outcome={gp.Outcome};elapsed={elapsed:F2};unlockSeconds={string.Join(",",unlock.Select(p=>$"{p.Key}:{p.Value:F2}"))};gatheredW/G/M={string.Join("/",gathered)};depositedW/G/M={string.Join("/",deposited)};spentW/G/M={string.Join("/",spent)};trainingAccepted={trained};peakWorkers={peakWorkers};peakSoldiers={peakSoldiers};expansion={expansionAttempt};expansionAt={expansionAt:F2}");
            if(!gp.IsMatchOver)throw new Exception("Match did not finish within750 simulated seconds.\n"+log);
            string path=control?"Docs/Modules/Progression/S8-ControlRuns-2026-11-17.txt":"Docs/Modules/Progression/S8-ScriptedRuns-2026-11-16.txt";
            System.IO.File.AppendAllText(path,log+Environment.NewLine);
            return log.ToString();
        }
        finally{gp.World.SetTime(time);UnityEngine.Random.state=random;}
    }
}
