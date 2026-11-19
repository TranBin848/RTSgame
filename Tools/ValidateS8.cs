// Run via Unity MCP script-execute in a disposable, paused Play session.
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using RTS.DOTS;

public static class S8Validation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static object Field(object target,string name)=>target.GetType().GetField(name,Private).GetValue(target);
    public static string Victory() => Finish(false);
    public static string Defeat() => Finish(true);
    static string Finish(bool defeat)
    {
        var b=Find<SwarmGameBridge>(); var gp=b.Gameplay; var day=Find<DayNightCycleManager>();
        var session=Find<MatchSession>(); var menu=Find<GameFlowMenu>(); var game=Find<GameManager>();
        Check(gp.Outcome==MatchOutcome.Running && Find<GameSceneCompositionRoot>().IsReady,"Use a fresh ready match.");
        if(defeat)
            foreach(var hall in UnityEngine.Object.FindObjectsByType<TownHall>(FindObjectsSortMode.None))
            {
                var body=gp.EntityManager.GetComponentData<CombatBody>(hall.DotsEntity);body.Health=0;
                gp.EntityManager.SetComponentData(hall.DotsEntity,body);
            }
        else typeof(DayNightCycleManager).GetProperty("CurrentDay").SetValue(day,6);
        typeof(MatchSession).GetMethod("LateUpdate",Private).Invoke(session,null);
        Check(gp.Outcome==(defeat?MatchOutcome.Defeat:MatchOutcome.Victory),"Wrong outcome.");
        Check(((GameObject)Field(session,"m_ResultPanel")).activeSelf&&Time.timeScale==0&&b.Simulation.Paused,"Result UI/simulation not stopped.");
        var wallet=gp.Wallet;int actors=gp.ActorCount,spawned=b.Simulation.TotalSpawned;
        menu.Resume();menu.Pause();menu.Back();menu.ShowOptions();game.AddResources(10,10,10);
        var hallView=Find<TownHall>();game.ActiveUnit=hallView;
        AssetDatabase.LoadAssetAtPath<SpawnVillagerActionSO>("Assets/Game/ScriptableObjects/Units/SpawnVillagerAction.asset").Excute(game);
        Check(gp.Wallet.Equals(wallet)&&gp.ActorCount==actors&&b.Simulation.TotalSpawned==spawned,"Terminal command mutated gameplay.");
        Check(Time.timeScale==0&&!((GameObject)Field(menu,"m_PausePanel")).activeSelf,"Resume/menu escaped terminal result.");
        int oldDay=day.CurrentDay;float progress=day.CurrentPhaseProgress;
        typeof(DayNightCycleManager).GetMethod("Tick",Private).Invoke(day,new object[]{999f});
        typeof(EnemyWaveManager).GetMethod("Tick",Private).Invoke(Find<EnemyWaveManager>(),new object[]{999f});
        Check(day.CurrentDay==oldDay&&day.CurrentPhaseProgress==progress&&b.Simulation.TotalSpawned==spawned,"Clock/waves progressed after result.");
        return $"PASS {gp.Outcome}; result visible; time0/nativePaused; menu/production/wallet/clock/wave gates; label={((Text)Field(session,"m_ResultLabel")).text}";
    }
    public static string Replay()
    {
        ((Button)Field(Find<MatchSession>(),"m_ReplayButton")).onClick.Invoke();
        return "Clicked actual Play again button";
    }
    public static string MainMenu()
    {
        ((Button)Field(Find<MatchSession>(),"m_MenuButton")).onClick.Invoke();
        return "Clicked actual Main menu button";
    }
    public static string Fresh()
    {
        var gp=Find<SwarmGameBridge>().Gameplay;
        Check(Find<GameSceneCompositionRoot>().IsReady&&gp.Outcome==MatchOutcome.Running&&gp.SurvivalNights==5,"New match not ready.");
        Check(gp.Wallet.Wood==100&&gp.Wallet.Gold==100&&gp.Wallet.Meat==100&&gp.Research.UnlockMask==0&&gp.Research.Active==0,"Wallet/research not reset.");
        Check(Time.timeScale==1&&!((GameObject)Field(Find<MatchSession>(),"m_ResultPanel")).activeSelf,"Result/time leaked into next match.");
        var session=Find<MatchSession>();var day=Find<DayNightCycleManager>();var game=Find<GameManager>();var menu=Find<GameFlowMenu>();
        session.Initialize(gp,day,game);session.Initialize(gp,day,game);day.InitializeMatch(gp);menu.InitializeMatch(gp);
        int rejected=0;
        void Reject(Action action){try{action();}catch(ArgumentException){rejected++;}catch(InvalidOperationException){rejected++;}}
        Reject(()=>session.Initialize(null,day,game));Reject(()=>day.InitializeMatch(null));Reject(()=>menu.InitializeMatch(null));
        using(var world=new Unity.Entities.World("S8 wrong match fixture"))
        {
            var wrong=world.GetOrCreateSystemManaged<GameplaySimulationSystem>();
            Reject(()=>session.Initialize(wrong,day,game));Reject(()=>day.InitializeMatch(wrong));Reject(()=>menu.InitializeMatch(wrong));
        }
        Check(rejected==6,"Missing/different dependency was silently accepted.");
        return $"PASS fresh match; wallet100x3; research0; running; objective5; actors={gp.ActorCount}; resources bound";
    }
}
