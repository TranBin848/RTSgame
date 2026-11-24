// Unity MCP script-execute, fresh paused Play seed1; runtime probes are discarded on Exit Play.
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RTS.DOTS;

public static class S7Validation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string CheckPresentation()
    {
        var p=Find<ProceduralMapPresentation>();var a=Find<ProceduralMapAuthoring>();var map=Find<TilemapManager>();var fog=Find<FogOfWarManager>();var b=Find<SwarmGameBridge>();
        Check(Find<GameSceneCompositionRoot>().IsReady&&p.IsInitialized,"Root/presentation not ready.");
        Check(b.Gameplay.ActorCount==25&&a.GeneratedMap.Resources.Count==150,"Visuals changed actor/resource budget.");
        Check(p.Decorations.Count>0&&p.Decorations.Count<=45+ProceduralMapPresentation.DaytimeDetailBudget&&p.Decorations.Count(r=>r.name=="Old Watchpost")==1,"Missing/duplicate landmark or decoration budget exceeded.");
        Check(p.Decorations.All(r=>r.GetComponent<Collider2D>()==null&&r.GetComponent<Unit>()==null),"Decor added gameplay/collision components.");
        var landmark=a.GeneratedMap.Layout.Landmark;
        foreach(var decor in p.Decorations)
        {
            var cell=map.WalkableTilemap.WorldToCell(decor.transform.position)-a.OriginCell;
            Check(!(Mathf.Abs(cell.x-landmark.X)<=2&&Mathf.Abs(cell.y-landmark.Y)<=2),"Decor occupies landmark build pad.");
            Check(decor.sortingOrder<25||(decor.name=="Old Watchpost"&&decor.sharedMaterial.shader.name=="RTS/Presentation/FootDepth"),"Decor covers unit/resource presentation.");
        }
        int count=p.Decorations.Count,nav=map.NavigationVersion;var terrain=a.GeneratedMap.Terrain.ToArray();
        p.Initialize(a.GeneratedMap,map,a.OriginCell,fog);p.Initialize(a.GeneratedMap,map,a.OriginCell,fog);
        Check(p.Decorations.Count==count&&map.NavigationVersion==nav&&terrain.SequenceEqual(a.GeneratedMap.Terrain),"Repeat visual init changed geometry/navigation or duplicated decor.");
        Check(fog.OnFogUpdated.GetInvocationList().Count(d=>ReferenceEquals(d.Target,p))==1,"Presentation subscribed twice.");
        Check(!p.IsSiteDiscovered(1)&&!p.IsSiteDiscovered(2)&&p.Decorations.Where(r=>r.enabled).All(r=>fog.IsWorldPositionExplored(r.transform.position)),"Unexplored map leaked sites/decor.");
        bool rejected=false;try{p.Initialize(null,map,a.OriginCell,fog);}catch(ArgumentException){rejected=true;}
        Check(rejected,"Missing presentation dependency accepted.");
        return $"PASS seed={a.GeneratedMap.Seed}; decor={count},one watchpost; pad clear; no Collider2D/Unit; actors25/resources150; repeat init stable/nav unchanged/one listener; unknown sites hidden; missing dependency rejected";
    }
    public static string CheckDiscovery()
    {
        var p=Find<ProceduralMapPresentation>();var fog=Find<FogOfWarManager>();var map=Find<TilemapManager>();
        var texture=new Texture2D(fog.Bounds.size.x,fog.Bounds.size.y);var untouched=new Color(.2f,.3f,.4f);
        try
        {
            var a=Find<ProceduralMapAuthoring>();var cell=map.WalkableTilemap.WorldToCell(p.LandmarkPosition);
            int x=cell.x-fog.Bounds.xMin,y=cell.y-fog.Bounds.yMin;
            texture.SetPixel(x,y,untouched);p.DrawMinimapMarkers(texture,fog.Bounds);
            Check(Vector4.Distance(texture.GetPixel(x,y),untouched)<.02f,"Unknown landmark marker leaked.");
            typeof(FogOfWarManager).GetMethod("RevealAroundWorldPosition",Private).Invoke(fog,new object[]{p.LandmarkPosition,9});
            fog.OnFogUpdated.Invoke();
            Check(p.IsSiteDiscovered(2)&&p.Decorations.Any(r=>r.name=="Old Watchpost"&&r.enabled),"Exploration did not reveal landmark.");
            p.DrawMinimapMarkers(texture,fog.Bounds);
            Check(Vector4.Distance(texture.GetPixel(x,y),untouched)>.1f,"Discovered landmark has no minimap glyph.");
            typeof(FogOfWarManager).GetMethod("RefreshFog",Private).Invoke(fog,null);
            Check(p.IsSiteDiscovered(2),"Explored landmark was forgotten after leaving vision.");
            var post=p.Decorations.Single(r=>r.name=="Old Watchpost");Check(post.enabled&&Mathf.Approximately(post.color.a,.5f),"Explored silhouette should remain dimmed.");
            return "PASS landmark hidden before exploration; reveal shows world silhouette+minimap glyph; leaving vision retains explored marker and dim silhouette";
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    public static string SetupCapture()
    {
        var fog=Find<FogOfWarManager>();if(!fog.IsDisabled)typeof(FogOfWarManager).GetMethod("ToggleFogOfWar",Private).Invoke(fog,null);
        var p=Find<ProceduralMapPresentation>().LandmarkPosition;Find<CameraController>().LockCamera=true;
        Camera.main.transform.position=new Vector3(p.x,p.y,-10);Camera.main.orthographicSize=9;
        return "Capture fixed seed1/landmark/ortho9; debug fog disabled for before-after comparison only";
    }
    public static string BaselineVisuals()
    {
        var p=Find<ProceduralMapPresentation>();var map=Find<TilemapManager>().WalkableTilemap;var a=Find<ProceduralMapAuthoring>();
        var callback=(UnityEngine.Events.UnityAction)Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction),p,typeof(ProceduralMapPresentation).GetMethod("RefreshVisibility",Private));
        Find<FogOfWarManager>().OnFogUpdated-=callback;
        foreach(var decor in p.Decorations)decor.enabled=false;
        var minimap=Find<MinimapFogUI>();typeof(MinimapFogUI).GetField("m_MapPresentation",Private).SetValue(minimap,null);
        typeof(MinimapFogUI).GetMethod("RefreshMinimap",Private).Invoke(minimap,null);
        for(int y=0;y<a.GeneratedMap.Height;y++)for(int x=0;x<a.GeneratedMap.Width;x++)
            if(a.GeneratedMap.IsGrass(x,y))map.SetColor(a.OriginCell+new Vector3Int(x,y,0),Color.white);
        return "Controlled baseline: same map/camera, S7 tint/decor disabled, no gameplay data changed";
    }
    public static string RestoreVisuals()
    {
        var p=Find<ProceduralMapPresentation>();typeof(ProceduralMapPresentation).GetMethod("PaintGround",Private).Invoke(p,null);
        var callback=(UnityEngine.Events.UnityAction)Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction),p,typeof(ProceduralMapPresentation).GetMethod("RefreshVisibility",Private));
        Find<FogOfWarManager>().OnFogUpdated-=callback;Find<FogOfWarManager>().OnFogUpdated+=callback;
        typeof(ProceduralMapPresentation).GetMethod("RefreshVisibility",Private).Invoke(p,null);
        var minimap=Find<MinimapFogUI>();minimap.InitializeLandmarks(p);
        typeof(MinimapFogUI).GetMethod("RefreshMinimap",Private).Invoke(minimap,null);
        return "S7 presentation restored";
    }
    public static string NightBefore()=>SetNight(true);
    public static string NightAfter()=>SetNight(false);
    static string SetNight(bool before)
    {
        var day=Find<DayNightCycleManager>();var definition=day.Definition;
        typeof(DayNightCycleDefinition).GetField("m_NightLightColor",Private).SetValue(definition,before?new Color(.2f,.2f,.4f):new Color(.6f,.68f,.86f));
        typeof(DayNightCycleDefinition).GetField("m_NightLightIntensity",Private).SetValue(definition,before?.1f:.35f);
        typeof(DayNightCycleDefinition).GetField("m_NightVignette",Private).SetValue(definition,before?.5f:.28f);
        typeof(DayNightCycleDefinition).GetField("m_NightColorFilter",Private).SetValue(definition,before?new Color(.6f,.7f,1):new Color(.8f,.86f,1));
        typeof(DayNightCycleManager).GetMethod("SetPhase",Private).Invoke(day,new object[]{DayNightPhase.Night,true});
        return before?"Baseline night values in memory only":"Authored S7 night values restored in memory";
    }
}
