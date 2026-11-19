// Unity MCP, paused ready match; checks UI DI without replacing live dependencies.
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class D4PresentationValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string Run()
    {
        var game=Find<GameManager>();var map=Find<TilemapManager>();var day=Find<DayNightCycleManager>();
        var waves=Find<EnemyWaveManager>();var fog=Find<FogOfWarManager>();var camera=Find<CameraController>();var bridge=Find<SwarmGameBridge>();
        Check(bridge.IsSceneBound,"Scene not ready.");
        var types=new[]{typeof(DayCounterUI),typeof(DayNightClockUI),typeof(WaveProgressUI),typeof(SelectedUnitsInfoPanelUI),typeof(ResourceRequirementsDisplay),typeof(IdleWorkerPanelUI),typeof(MinimapFogUI),typeof(CursorManager)};
        var targets=game.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(p=>types.Contains(p.GetType())).ToArray();
        Check(types.All(t=>targets.Any(p=>p.GetType()==t)),"A presentation type is missing from scene.");
        foreach(var p in targets)Check((bool)p.GetType().GetField("m_IsInitialized",Private).GetValue(p),"Uninitialized "+p.GetType().Name);
        var counts=new[]{day.OnDayChanged.GetInvocationList().Length,waves.OnWaveScheduleChanged.GetInvocationList().Length,waves.OnWaveStarted.GetInvocationList().Length,game.OnSelectionChanged.GetInvocationList().Length,fog.OnFogUpdated.GetInvocationList().Length};
        var texture=typeof(MinimapFogUI).GetField("m_MinimapTexture",Private).GetValue(Find<MinimapFogUI>());
        GameSceneCompositionRoot.ComposePresentation(game,map,fog,day,waves,camera,bridge);
        GameSceneCompositionRoot.ComposePresentation(game,map,fog,day,waves,camera,bridge);
        var after=new[]{day.OnDayChanged.GetInvocationList().Length,waves.OnWaveScheduleChanged.GetInvocationList().Length,waves.OnWaveStarted.GetInvocationList().Length,game.OnSelectionChanged.GetInvocationList().Length,fog.OnFogUpdated.GetInvocationList().Length};
        Check(counts.SequenceEqual(after),"Repeat composition duplicated listeners.");
        Check(ReferenceEquals(texture,typeof(MinimapFogUI).GetField("m_MinimapTexture",Private).GetValue(Find<MinimapFogUI>()))&&texture!=null,"Repeat composition changed/lost minimap texture.");
        int rejected=0;
        void Reject(Action action){try{action();}catch(ArgumentException){rejected++;}catch(InvalidOperationException){rejected++;}}
        Reject(()=>Find<DayCounterUI>().Initialize(null));Reject(()=>Find<DayNightClockUI>().Initialize(null));
        Reject(()=>Find<WaveProgressUI>().Initialize(null));Reject(()=>Find<SelectedUnitsInfoPanelUI>().Initialize(null));
        Reject(()=>Find<ResourceRequirementsDisplay>().Initialize(null));Reject(()=>Find<IdleWorkerPanelUI>().Initialize(null,camera));
        Reject(()=>Find<MinimapFogUI>().Initialize(fog,map,camera,Camera.main,null));Reject(()=>Find<CursorManager>().Initialize(null,bridge,Camera.main));
        var fixture=new GameObject("D4 different dependency fixtures");fixture.SetActive(false);
        try
        {
            var otherDay=fixture.AddComponent<DayNightCycleManager>();var otherGame=fixture.AddComponent<GameManager>();
            var otherWaves=fixture.AddComponent<EnemyWaveManager>();var otherBridge=fixture.AddComponent<SwarmGameBridge>();
            Reject(()=>Find<DayCounterUI>().Initialize(otherDay));Reject(()=>Find<DayNightClockUI>().Initialize(otherDay));
            Reject(()=>Find<WaveProgressUI>().Initialize(otherWaves));Reject(()=>Find<SelectedUnitsInfoPanelUI>().Initialize(otherGame));
            Reject(()=>Find<ResourceRequirementsDisplay>().Initialize(otherGame));Reject(()=>Find<IdleWorkerPanelUI>().Initialize(otherGame,camera));
            Reject(()=>Find<MinimapFogUI>().Initialize(fog,map,camera,Camera.main,otherBridge));Reject(()=>Find<CursorManager>().Initialize(otherGame,bridge,Camera.main));
        }
        finally{UnityEngine.Object.DestroyImmediate(fixture);}
        Check(rejected==16,"Dependency admission regression, rejected="+rejected);
        return $"PASS scene={game.gameObject.scene.name};{targets.Length}views/8types initialized including inactive;repeat stable listeners and texture;16null/different dependency rejects;existing views preserved";
    }
}
