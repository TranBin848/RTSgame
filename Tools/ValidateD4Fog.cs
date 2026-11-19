// Unity MCP, ready paused Play session. Only creates and destroys owned fixtures.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class D4FogValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool value,string why){if(!value)throw new Exception(why);}
    public static string Run()
    {
        var fog=Find<FogOfWarManager>();var game=Find<GameManager>();var map=Find<TilemapManager>();var camera=Find<CameraController>();
        var registry=(List<FogOfWarAffectable>)typeof(FogOfWarManager).GetField("m_Affectables",Private).GetValue(fog);
        var owner=typeof(FogOfWarAffectable).GetField("m_FogOfWarManager",Private);
        var active=UnityEngine.Object.FindObjectsByType<FogOfWarAffectable>(FindObjectsSortMode.None).Where(a=>a.gameObject.scene==game.gameObject.scene).ToArray();
        Check(fog.IsInitialized&&active.Length>0,"Fog not ready.");
        Check(registry.Count==registry.Distinct().Count()&&active.All(a=>registry.Contains(a)&&owner.GetValue(a)==fog),"Active affectable not bound exactly once.");
        int baseline=registry.Count;fog.Initialize(game,map);fog.Initialize(game,map);
        Check(registry.Count==baseline,"Repeat fog init cleared/duplicated registry.");
        var fixture=new GameObject("D4 fog fixture");var renderer=fixture.AddComponent<SpriteRenderer>();renderer.color=new Color(1,1,1,.8f);
        var affectable=fixture.AddComponent<FogOfWarAffectable>();
        var other=new GameObject("D4 other dependencies");other.SetActive(false);
        int rejected=0;
        void Reject(Action action){try{action();}catch(ArgumentException){rejected++;}catch(InvalidOperationException){rejected++;}}
        try
        {
            var otherFog=other.AddComponent<FogOfWarManager>();var otherGame=other.AddComponent<GameManager>();var otherCamera=other.AddComponent<Camera>();
            Reject(()=>affectable.Initialize(null));affectable.Initialize(fog);affectable.Initialize(fog);
            Check(registry.Count==baseline+1,"Duplicate affectable registration.");
            Reject(()=>affectable.Initialize(otherFog));Reject(()=>fog.Initialize(null,map));Reject(()=>fog.Initialize(otherGame,map));
            Reject(()=>camera.Initialize(null));Reject(()=>camera.Initialize(otherCamera));
            affectable.ApplyVisibility(.5f);Check(Mathf.Abs(renderer.color.a-.4f)<.001f,"Base alpha was recaptured after fog tint.");
            fixture.SetActive(false);Check(registry.Count==baseline,"Disable retained affectable.");
            fixture.SetActive(true);Check(registry.Count==baseline+1,"Enable failed to register injected fog.");
            affectable.Unbind();Check(registry.Count==baseline&&owner.GetValue(affectable)==null&&Mathf.Abs(renderer.color.a-.8f)<.001f,"Unbind retained fog or lost original alpha.");
            Check(rejected==6,"Dependency validation rejected="+rejected);
            return $"PASS {game.gameObject.scene.name}: {active.Length}active affectables explicitly bound;repeat stable;6null/different rejects;disable/enable/unbind registry and base alpha restored";
        }
        finally{UnityEngine.Object.DestroyImmediate(fixture);UnityEngine.Object.DestroyImmediate(other);}
    }
}
