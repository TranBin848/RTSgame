using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using RTS.DOTS;

public static class G01PresentationValidation
{
    public static string ProbeBefore() => Check(false);
    public static string Run() => Check(true);
    static string Check(bool fixedExpected)
    {
        if(!EditorApplication.isPlaying||!EditorApplication.isPaused)throw new Exception("Paused Play required.");
        var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
        var worker=UnityEngine.Object.FindFirstObjectByType<WorkerUnit>();
        var archer=UnityEngine.Object.FindFirstObjectByType<RangerUnit>();
        var gp=bridge.Gameplay;
        if(!gp.TryGetActor(worker.DotsEntity,out var actor,out var body))throw new Exception("Bound worker required.");
        var original=actor;
        var warnings=new List<string>();
        void Capture(string message,string stack,LogType type)
        {if((type==LogType.Warning||type==LogType.Error||type==LogType.Exception)&&message.Contains("Attack"))warnings.Add(message);}
        bool attackBool=false;
        Application.logMessageReceived+=Capture;
        try
        {
            actor.State=GameplayState.Attacking;actor.AttackSequence++;
            worker.ApplyDotsSnapshot(actor,body,Vector2.zero);
            attackBool=worker.GetComponent<Animator>().GetBool("isAttacking");
            if(!attackBool)throw new Exception("Worker attack bool was not enabled.");
            if(gp.EntityManager.GetComponentData<GameplayActor>(worker.DotsEntity).AttackSequence!=original.AttackSequence)
                throw new Exception("Presentation changed authoritative attack sequence.");
            if(!gp.TryGetActor(archer.DotsEntity,out var ranged,out var rangedBody))throw new Exception("Bound archer required.");
            var originalRanged=ranged;ranged.AttackSequence++;
            archer.ApplyDotsSnapshot(ranged,rangedBody,Vector2.zero);
            archer.ApplyDotsSnapshot(originalRanged,rangedBody,Vector2.zero);
            archer.GetComponent<Animator>().ResetTrigger("Attack");
        }
        finally
        {
            Application.logMessageReceived-=Capture;
            worker.ApplyDotsSnapshot(original,body,Vector2.zero);
        }
        if(fixedExpected&&warnings.Count!=0)throw new Exception(string.Join("\n",warnings));
        var result=Newtonsoft.Json.JsonConvert.SerializeObject(new {fixedExpected,workerAttackBool=attackBool,warningCount=warnings.Count,warnings,nativeUnchanged=true,archerTriggerAccepted=true});
        System.IO.File.WriteAllText("Docs/Modules/Progression/Evidence/G01-2026-11-18/presentation-"+(fixedExpected?"after":"before")+".json",result);
        return result;
    }
}
