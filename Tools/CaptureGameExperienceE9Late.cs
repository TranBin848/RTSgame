using System;
using UnityEngine;
using UnityEditor;

public static class E9LateCapture
{
    public static string Start()
    {
        var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycleManager>();
        bool dusk=false,night=false;
        double started=EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction update=null;
        update=()=>{
            if(!EditorApplication.isPlaying||clock==null||EditorApplication.timeSinceStartup-started>230)
            {EditorApplication.update-=update;return;}
            if(clock.CurrentDay!=5)return;
            if(!dusk&&clock.CurrentPhase==DayNightPhase.Dusk&&clock.CurrentPhaseProgress>.5f)
            {E9GameViewCapture.Save("night5-briefing.png");dusk=true;}
            if(!night&&clock.CurrentPhase==DayNightPhase.Night&&clock.CurrentPhaseProgress>.4f)
            {E9GameViewCapture.Save("night5-mixed.png");night=true;}
            if(dusk&&night)EditorApplication.update-=update;
        };
        EditorApplication.update+=update;
        return "Armed read-only Dusk5/Night5 captures; no gameplay/clock/RNG edits.";
    }
}
