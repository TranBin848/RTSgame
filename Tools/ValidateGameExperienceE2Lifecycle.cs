using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Run through script-execute; start from a Ready paused ProceduralGame. Step once after each scene transition.
public static class GameExperienceE2LifecycleValidation
{
    public static string CaptureAndReturnMenu()
    {
        var r=UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>(); if(!r.IsReady)throw new Exception("Root not ready");var l=r.NightLighting;l.RefreshPresentation();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var ids=new System.Collections.Generic.List<int>();foreach(var name in new[]{"m_GlowTexture","m_CoreTexture","m_GlowSprite","m_CoreSprite","m_RuntimeMaterial"}){var value=(UnityEngine.Object)typeof(StructureNightLighting).GetField(name,flags).GetValue(l);if(value==null)throw new Exception("Missing owned resource "+name);ids.Add(value.GetInstanceID());}ids.Add(l.GetInstanceID());foreach(var s in UnityEngine.Object.FindFirstObjectByType<GameManager>().GetPlayerStructures()){var group=s.transform.Find("Night light presentation");if(group!=null)ids.Add(group.gameObject.GetInstanceID());}UnityEditor.SessionState.SetString("E2.OwnedIds",string.Join(",",ids));UnityEngine.Object.FindFirstObjectByType<GameFlowMenu>().ReturnToMenu();return $"Saved {ids.Count} owned IDs and returned to menu";
    }

    public static string CheckMenuAndStartNewMatch()
    {
        var ids=UnityEditor.SessionState.GetString("E2.OwnedIds","").Split(',').Select(int.Parse).ToArray();if(ids.Any(id=>EditorUtility.InstanceIDToObject(id)!=null))throw new Exception("Night resource survived scene unload");if(UnityEngine.Object.FindFirstObjectByType<StructureNightLighting>()!=null||ProceduralMapRuntime.IsActive)throw new Exception("Old night/map context leaked");UnityEngine.Object.FindFirstObjectByType<GameFlowMenu>().Play();return "PASS menu cleanup: controller, sprites, textures, material and groups destroyed; loading match2";
    }

    public static string CheckNewMatch()
    {
        var r=UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();if(!r.IsReady||r.NightLighting==null||!r.NightLighting.IsInitialized||r.Research.State.UnlockMask!=0||UnityEngine.Object.FindObjectsByType<StructureNightLighting>(FindObjectsSortMode.None).Length!=1)throw new Exception("E2 match2 invalid");r.Initialize();return "PASS E2 menu→game→menu→game: fresh Ready context, one lighting controller, research reset, repeated root init, old owned resources destroyed.";
    }

    public static string CheckLegacySample()
    {
        if(UnityEngine.Object.FindFirstObjectByType<StructureNightLighting>()!=null)throw new Exception("E2 leaked into SampleScene");var d=UnityEngine.Object.FindFirstObjectByType<DayNightCycleManager>();if(d.Presentation!=null)throw new Exception("Candidate leaked into legacy clock");return "PASS SampleScene: no E2 controller and legacy palette path retained.";
    }

}
