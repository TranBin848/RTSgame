// Temporary Editor-only runner. Install with the frozen harness, remove after the checkpoint is closed.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class G2EditorRunner
{
    const string Dir="Docs/Modules/Progression/Evidence/G2-2026-11-19";
    const string Request=".utmp/G2-editor-request.json";
    const string Key="RTS.G2.EditorRunner.";
    [Serializable] class RectFix { public long id; public Vector2 anchorMin,anchorMax,anchoredPosition; }
    [Serializable] class AnimationFix { public long id; public int x,y,z; public float speed; }
    [Serializable] class Job { public string mode="pilot"; public int hp; public RectFix[] rects; public AnimationFix[] animations; }
    static G2EditorRunner()
    {
        const string deferred=".utmp/G2-deferred-request.json";
        if(File.Exists(deferred))
        {
            File.Copy(deferred,Request,true);File.Delete(deferred);SessionState.SetInt(Key+"phase",0);
        }
        EditorApplication.update+=Pump;
    }
    static void Status(string text)=>File.WriteAllText(Dir+"/editor-runner-status.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {text,phase=SessionState.GetInt(Key+"phase",0),playing=EditorApplication.isPlaying,paused=EditorApplication.isPaused,utc=DateTime.UtcNow}));
    static void LoadCandidate(int hp,int nextPhase)
    {
        if(!EditorApplication.isPlaying||!EditorApplication.isPaused)throw new Exception("Paused Play required for reload.");
        SessionState.SetBool(Key+"bound",false);
        UnityEngine.Events.UnityAction<Scene,LoadSceneMode> loaded=null;
        loaded=(scene,mode)=>
        {
            SceneManager.sceneLoaded-=loaded;
            try
            {
                var root=UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
                var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();
                if(root.IsReady||hall.IsDotsControlled)throw new Exception("Expected pre-bind lifecycle.");
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(Unit).GetField("m_Health",flags).SetValue(hall,hp);
                typeof(Unit).GetField("m_CurrentHealth",flags).SetValue(hall,hp);
                SessionState.SetBool(Key+"bound",true);
            }
            catch(Exception ex){Fail(ex);}
        };
        SceneManager.sceneLoaded+=loaded;
        AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>("Assets/Game/ProceduralGeneration/Settings/StrategicProceduralMapSettings.asset").Seed=1;
        Time.timeScale=1;
        SessionState.SetInt(Key+"phase",nextPhase);
        SceneManager.LoadScene("ProceduralGame");
        EditorApplication.Step();
    }
    static bool Ready()=>SessionState.GetBool(Key+"bound",false)&&UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>()?.IsReady==true;
    static string[] Runs(int hp,string policy)=>Directory.GetFiles(Dir,"*-hp"+hp+"-"+policy+"-1.txt");
    static bool Outcome(int hp,string policy,string outcome)=>Runs(hp,policy).Any(p=>File.ReadAllText(p).Contains("outcome="+outcome+";"));
    static SerializedProperty AnimationProperty(SerializedObject data,Vector3Int cell)
    {
        var entries=data.FindProperty("m_AnimatedTiles");
        if(entries==null||!entries.isArray)throw new Exception("Expected serialized animation map.");
        for(int i=0;i<entries.arraySize;i++)
        {
            var entry=entries.GetArrayElementAtIndex(i);
            var key=entry.FindPropertyRelative("first");
            if(key==null)throw new Exception("Missing animation map key.");
            if(key.vector3IntValue==cell)return entry.FindPropertyRelative("second.m_AnimationSpeed")??throw new Exception("Missing animation speed field.");
        }
        throw new Exception("Missing animation cell: "+cell);
    }
    static void Fail(Exception ex)
    {
        File.WriteAllText(Dir+"/editor-runner-FAILED.txt",ex.ToString());
        if(!EditorApplication.isPlaying)
        {
            SessionState.SetInt(Key+"phase",0);File.Delete(Request);Status("FAILED in Edit Mode; stopped without saving further changes");return;
        }
        SessionState.SetInt(Key+"phase",8);
        Status("FAILED; restoring Edit baseline");
    }
    static void Pump()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try
        {
            int phase=SessionState.GetInt(Key+"phase",0);
            if(phase==0)
            {
                var job=JsonUtility.FromJson<Job>(File.ReadAllText(Request));
                if(job.mode=="restore-fields")
                {
                    if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/ProceduralGame.unity")throw new Exception("Expected Edit scene for scoped field restore.");
                    if(job.rects.Length!=3||job.animations.Length!=20)throw new Exception("Unexpected field restore scope.");
                    var rects=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    var maps=UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    var readback=job.animations.Select(fix=>new{fix.id,fix.x,fix.y,fix.z,expected=fix.speed,actual=AnimationProperty(new SerializedObject(maps.Single(m=>GlobalObjectId.GetGlobalObjectIdSlow(m).targetObjectId==(ulong)fix.id)),new Vector3Int(fix.x,fix.y,fix.z)).floatValue}).ToArray();
                    File.WriteAllText(Dir+"/animation-readback-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(readback));
                    if(readback.Any(row=>Mathf.Abs(row.actual-row.expected)>.0001f))throw new Exception("Unexpected animation speed difference; no fields changed.");
                    foreach(var fix in job.rects)
                    {
                        var rect=rects.Single(r=>GlobalObjectId.GetGlobalObjectIdSlow(r).targetObjectId==(ulong)fix.id);
                        var data=new SerializedObject(rect);
                        data.FindProperty("m_AnchorMin").vector2Value=fix.anchorMin;
                        data.FindProperty("m_AnchorMax").vector2Value=fix.anchorMax;
                        data.FindProperty("m_AnchoredPosition").vector2Value=fix.anchoredPosition;
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    foreach(var fix in job.animations)
                    {
                        var map=maps.Single(m=>GlobalObjectId.GetGlobalObjectIdSlow(m).targetObjectId==(ulong)fix.id);
                        var cell=new Vector3Int(fix.x,fix.y,fix.z);
                        var data=new SerializedObject(map);
                        var speed=AnimationProperty(data,cell);
                        if(Mathf.Abs(speed.floatValue-fix.speed)>.0001f)throw new Exception("Unexpected animation speed difference.");
                        speed.floatValue=fix.speed;data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new Exception("Field restore SaveScene failed.");
                    File.WriteAllText(Dir+"/fields-restored.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{rects=job.rects.Length,animations=job.animations.Length,dirty=SceneManager.GetActiveScene().isDirty,utc=DateTime.UtcNow}));
                    File.Delete(Request);Status("Restored unrelated UI/animation authoring fields through Unity API");return;
                }
                if(job.mode=="cleanup")
                {
                    if(EditorApplication.isPlaying||EditorApplication.isCompiling||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit required for cleanup.");
                    var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();
                    var seed=AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>("Assets/Game/ProceduralGeneration/Settings/StrategicProceduralMapSettings.asset").Seed;
                    File.WriteAllText(Dir+"/editor-final.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{hp=hall.MaxHealth,seed,scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,playing=EditorApplication.isPlaying,paused=EditorApplication.isPaused,compiling=EditorApplication.isCompiling,utc=DateTime.UtcNow}));
                    File.Delete(Request);
                    foreach(var path in new[]{"Assets/Game/DOTS/Editor/G2EditorPlaytest.cs","Assets/Game/DOTS/Editor/G2EditorRealtime.cs","Assets/Game/DOTS/Editor/G2EditorRunner.cs"})
                        if(!AssetDatabase.DeleteAsset(path))throw new Exception("Temporary runner cleanup failed: "+path);
                    AssetDatabase.Refresh();Status("Temporary Editor runners removed");return;
                }
                if(job.mode=="author")
                {
                    if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!="Assets/Scenes/ProceduralGame.unity"||SceneManager.GetActiveScene().isDirty||job.hp!=1600&&job.hp!=1200)throw new Exception("Author guard.");
                    var selected=JsonUtility.FromJson<Job>(File.ReadAllText(Dir+"/shortlist.json"));
                    if(selected.hp!=job.hp||!Outcome(job.hp,"control","Defeat")||!Directory.GetFiles(Dir,"*-hp"+job.hp+"-*-1-realtime.txt").Any(p=>{var raw=File.ReadAllText(p);return raw.Contains("outcome=Victory;")&&raw.TrimEnd().EndsWith("error=");}))throw new Exception("Native and realtime gates must pass before authoring.");
                    var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();
                    if(hall.MaxHealth!=10000)throw new Exception("Expected baseline10000.");
                    var serialized=new SerializedObject(hall);serialized.FindProperty("m_Health").intValue=job.hp;serialized.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(hall);
                    EditorSceneManager.MarkSceneDirty(hall.gameObject.scene);
                    if(!EditorSceneManager.SaveScene(hall.gameObject.scene))throw new Exception("SaveScene failed.");
                    File.WriteAllText(Dir+"/authored.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{hp=hall.MaxHealth,scene=hall.gameObject.scene.path,dirty=hall.gameObject.scene.isDirty,utc=DateTime.UtcNow}));
                    File.Delete(Request);Status("Authored selected Hall HP");return;
                }
                if(job.mode!="pilot")throw new Exception("Unknown runner mode.");
                Status("Starting local pilot; no SaveScene/SaveAssets");
                SessionState.SetInt(Key+"phase",1);
                if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;
                return;
            }
            if(phase==1)
            {
                if(EditorApplication.isPlaying)return;
                if(SceneManager.GetActiveScene().path!="Assets/Scenes/ProceduralGame.unity"||SceneManager.GetActiveScene().isDirty||UnityEngine.Object.FindFirstObjectByType<TownHall>().MaxHealth!=10000)throw new Exception("Clean baseline scene required.");
                SessionState.SetInt(Key+"phase",2);EditorApplication.isPaused=true;EditorApplication.isPlaying=true;return;
            }
            if(phase==2)
            {
                if(!EditorApplication.isPlaying)return;
                SessionState.SetInt(Key+"phase",3);return;
            }
            if(phase==3)
            {
                foreach(int hp in new[]{10000,1600,1200})foreach(string policy in new[]{"control","economy","military"})
                {
                    if(Runs(hp,policy).Length>0)continue;
                    SessionState.SetInt(Key+"hp",hp);SessionState.SetString(Key+"policy",policy);Status("Preparing HP"+hp+"/"+policy);
                    LoadCandidate(hp,4);return;
                }
                int winner=new[]{1600,1200}.FirstOrDefault(hp=>Outcome(hp,"control","Defeat")&&(Outcome(hp,"economy","Victory")||Outcome(hp,"military","Victory")));
                if(winner==0){Status("No shortlist; restore baseline");SessionState.SetInt(Key+"phase",8);return;}
                string strategy=Outcome(winner,"economy","Victory")?"economy":"military";
                SessionState.SetInt(Key+"winner",winner);SessionState.SetString(Key+"strategy",strategy);
                File.WriteAllText(Dir+"/shortlist.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{hp=winner,strategy,rule="highest preregistered HP with controlDefeat and activeVictory; realtime pending"}));
                Status("Preparing realtime HP"+winner+"/"+strategy);LoadCandidate(winner,6);return;
            }
            if(phase==4)
            {
                if(!Ready())return;
                string policy=SessionState.GetString(Key+"policy","");Status("Running native HP"+SessionState.GetInt(Key+"hp",0)+"/"+policy);
                if(policy=="control")G2EditorPlaytest.Control();else if(policy=="economy")G2EditorPlaytest.Economy();else G2EditorPlaytest.Military();
                SessionState.SetInt(Key+"phase",3);Status("Native run completed");return;
            }
            if(phase==6)
            {
                if(!Ready())return;
                if(SessionState.GetString(Key+"strategy","")=="economy")G2EditorRealtime.Economy();else G2EditorRealtime.Military();
                SessionState.SetInt(Key+"phase",7);Status("Realtime running in normal Unity PlayerLoop");return;
            }
            if(phase==7)
            {
                if(!EditorApplication.isPlaying){throw new Exception("Realtime stopped externally.");}
                var bridge=UnityEngine.Object.FindFirstObjectByType<SwarmGameBridge>();
                if(bridge.Gameplay.IsMatchOver&&Directory.GetFiles(Dir,"*-realtime.txt").Any(p=>File.ReadAllText(p).Contains("finished wallSeconds=")))
                {SessionState.SetInt(Key+"phase",8);Status("Realtime terminal; restoring baseline before review");}
                return;
            }
            if(phase==8)
            {
                SessionState.SetInt(Key+"phase",9);if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;return;
            }
            if(phase==9)
            {
                if(EditorApplication.isPlaying)return;
                var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();
                if(hall.MaxHealth!=10000)throw new Exception("Edit baseline changed unexpectedly.");
                AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>("Assets/Game/ProceduralGeneration/Settings/StrategicProceduralMapSettings.asset").Seed=1;
                EditorApplication.isPaused=false;
                File.WriteAllText(Dir+"/restore-local.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{hp=hall.MaxHealth,scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,playing=EditorApplication.isPlaying,paused=EditorApplication.isPaused,utc=DateTime.UtcNow}));
                SessionState.SetInt(Key+"phase",0);File.Delete(Request);Status("Complete; baseline restored; awaiting evidence review");
            }
        }
        catch(Exception ex){Fail(ex);}
    }
}
