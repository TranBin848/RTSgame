using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class GameExperienceE5Lifecycle
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Dir="Docs/Modules/GameExperience/Evidence/E5/";
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static object Field(object target,string name)=>target.GetType().GetField(name,Private).GetValue(target);
    static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Private).Invoke(target,args);
    static void Check(bool value,string message){if(!value)throw new Exception("E5 lifecycle: "+message);}
    static string Log(string value){Directory.CreateDirectory(Dir);File.AppendAllText(Dir+"lifecycle.txt",value+"\n");return value;}
    static WorldDepthPresentation Depth=>Find<GameSceneCompositionRoot>().DepthPresentation;
    static string MeshPose()=>string.Join(";",((Mesh)Field(Depth,"m_ShadowMesh")).vertices.Select(v=>v.ToString("R")));
    static void Refresh(){typeof(WorldDepthPresentation).GetField("m_NextScan",Private).SetValue(Depth,-1f);Depth.RefreshPresentation();}

    public static string FogAndPause()
    {
        Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"lifecycle.txt","");
        var fog=Find<FogOfWarManager>();if(fog.IsDisabled)Call(fog,"ToggleFogOfWar");
        var original=(int[,])Field(fog,"m_VisibilityCounts");
        try
        {
            typeof(FogOfWarManager).GetField("m_VisibilityCounts",Private).SetValue(fog,new int[original.GetLength(0),original.GetLength(1)]);
            fog.OnFogUpdated.Invoke();Refresh();
            Check(Depth.ContactCount==0&&Depth.SelectionRingCount==0&&Depth.FadedOccluders==0,"stale selection reveals fog through contacts/rings/fade");
        }
        finally{typeof(FogOfWarManager).GetField("m_VisibilityCounts",Private).SetValue(fog,original);fog.OnFogUpdated.Invoke();Refresh();}
        Log("PASS zero-current-visibility probe: existing selection emits0contacts/0rings/0faded occluders; visibility counts restored.");
        Find<GameFlowMenu>().Pause();Refresh();SessionState.SetString("E5.PauseMesh",MeshPose());
        SessionState.SetFloat("E5.PauseProgress",Find<DayNightCycleManager>().CurrentPhaseProgress);
        return "Menu paused, snapshot stored; step then CheckPause";
    }
    public static string CheckPause()
    {
        Refresh();Check(Time.timeScale==0&&MeshPose()==SessionState.GetString("E5.PauseMesh","")
            &&Find<DayNightCycleManager>().CurrentPhaseProgress==SessionState.GetFloat("E5.PauseProgress",-1),"pause moved contact geometry/clock");
        Find<GameFlowMenu>().Resume();return Log("PASS actual menu pause + Editor step: mono contact geometry and phase progress unchanged; resumed.");
    }

    static int[] Owned()
    {
        var ids=new List<int>{Depth.GetInstanceID()};
        foreach(var pair in new[]{(target:(object)Depth,names:new[]{"m_BodyMaterial","m_ShadowMaterial","m_RingMaterial","m_ShadowMesh","m_RingMesh","m_Memory","m_ShadowView","m_RingView"}),
            (target:(object)Find<RTS.DOTS.SwarmInstancedRenderer>(),names:new[]{"m_ContactMaterial","m_ContactMesh","m_ContactView"})})
            foreach(var name in pair.names){var value=(UnityEngine.Object)Field(pair.target,name);if(value==null)continue;ids.Add(value.GetInstanceID());if(value is Component c)ids.Add(c.gameObject.GetInstanceID());}
        return ids.Distinct().ToArray();
    }
    static bool Gone(string key)=>SessionState.GetString(key,"").Split(',').Select(int.Parse).All(id=>EditorUtility.InstanceIDToObject(id)==null);

    public static string DestroyComponent()
    {
        Call(Find<GameManager>(),"SetSelectedUnits",new List<Unit>());Refresh();
        SessionState.SetString("E5.ComponentIds",string.Join(",",Owned()));
        var originals=new List<string>();
        foreach(var b in (IEnumerable)Field(Depth,"m_Bodies"))
        {
            var t=b.GetType();var r=(SpriteRenderer)t.GetField("Renderer").GetValue(b);var m=(Material)t.GetField("OriginalMaterial").GetValue(b);
            if(r!=null&&m!=null)originals.Add(r.GetInstanceID()+","+m.GetInstanceID()+","+t.GetField("OriginalOrder").GetValue(b));
        }
        SessionState.SetString("E5.Originals",string.Join(";",originals));var d=Depth;var fog=Find<FogOfWarManager>();
        UnityEngine.Object.DestroyImmediate(d);
        Check(fog.OnFogUpdated.GetInvocationList().All(c=>!ReferenceEquals(c.Target,d)),"destroy retains fog listener");
        return "Destroyed owned E5 component only; step then CheckDestroyAndMenu";
    }
    public static string CheckDestroyAndMenu()
    {
        Check(Gone("E5.ComponentIds")&&!Find<RTS.DOTS.SwarmInstancedRenderer>().HasDepthPresentation,"owned resources survived component teardown");
        foreach(var row in SessionState.GetString("E5.Originals","").Split(';'))
        {
            var values=row.Split(',').Select(int.Parse).ToArray();var r=EditorUtility.InstanceIDToObject(values[0]) as SpriteRenderer;
            if(r==null)continue;Check(r.sharedMaterial!=null&&r.sharedMaterial.GetInstanceID()==values[1]&&r.sortingOrder==values[2],"renderer source state not restored");
            var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);Check(block.GetFloat("_DepthFade")==0,"stale fade property after destroy");
        }
        var p=Find<DayNightCycleManager>().Presentation;Check(p.FootDepthMaterial!=null&&p.ContactShadowMaterial!=null&&p.NightGlowMaterial!=null,"shared assets destroyed");
        Log("PASS component-only teardown: all recorded E5/native contact IDs gone, fog listener removed, original materials/orders/property blocks restored; retained shared materials alive.");
        Find<GameFlowMenu>().ReturnToMenu();return "Loading menu after component-only teardown";
    }
    public static string MenuAndReload()
    {
        Check(Find<WorldDepthPresentation>()==null&&!ProceduralMapRuntime.IsActive,"old scene context leaked");Find<GameFlowMenu>().Play();return Log("PASS menu has no E5/old procedural context; loading new match.");
    }
    public static string CheckNewMatch()
    {
        Refresh();var fog=Find<FogOfWarManager>();var native=Find<RTS.DOTS.SwarmInstancedRenderer>();
        Check(Find<GameSceneCompositionRoot>().IsReady&&Depth.IsInitialized&&native.HasDepthPresentation&&Depth.RegisteredBodies>=150
            &&UnityEngine.Object.FindObjectsByType<WorldDepthPresentation>(FindObjectsSortMode.None).Length==1
            &&fog.OnFogUpdated.GetInvocationList().Count(c=>c.Target==Depth)==1&&Find<DayNightCycleManager>().CurrentPhase==DayNightPhase.Day,"new match context not fresh");
        return Log("PASS new match: Ready/Day, one depth controller/one fog listener, native enabled, full fresh registry.");
    }
    public static string ActiveUnload()
    {
        SessionState.SetString("E5.UnloadIds",string.Join(",",Owned()));Find<GameFlowMenu>().ReturnToMenu();return "Loading menu with E5 active and owned IDs recorded";
    }
    public static string CheckActiveUnload()
    {
        Check(Gone("E5.UnloadIds")&&Find<WorldDepthPresentation>()==null,"active scene unload leaked E5 resources");
        Find<GameFlowMenu>().Play();return Log("PASS direct scene unload with E5 active: all owned controller/material/mesh/texture/view/native contact IDs gone; loading another match.");
    }
    public static string CheckLegacy()
    {
        Check(Find<WorldDepthPresentation>()==null&&Find<DayNightCycleManager>().Presentation==null,"E5 leaked into SampleScene");
        var n=Find<RTS.DOTS.SwarmInstancedRenderer>();Check(n==null||!n.HasDepthPresentation,"legacy native contact enabled");return Log("PASS SampleScene: no E5 controller, no native depth/contact mode, legacy palette path retained.");
    }
}
