// Reuse the G2 policy; Unity runs the clock, wave, native simulation and presentation at normal time.
import fs from 'node:fs';
import assert from 'node:assert/strict';
const dir = 'Docs/Modules/Progression/Evidence/G2-2026-11-19';
const base = fs.readFileSync(`${dir}/PlaytestG2-derived.cs.txt`, 'utf8');
const start = base.indexOf('        if(!EditorApplication.isPlaying');
const end = base.indexOf('        log.AppendLine($"schema=');
assert(start > 0 && end > start);
let init = base.slice(start, end).replaceAll('using var actors=', 'var actors=').replaceAll('using var resources=', 'var resources=').replaceAll('using var enemies=', 'var enemies=');
const policyStart = base.indexOf('                if(!control&&tick%100==1)');
const policyEnd = base.indexOf('                var oldWallet=gp.Wallet;', policyStart);
assert(policyStart > 0 && policyEnd > policyStart);
const policy = base.slice(policyStart, policyEnd).replace('if(!control&&tick%100==1)', 'if(!control&&elapsed>=nextPolicy)').replace('var before=gp.Wallet;', 'nextPolicy=elapsed+5;var before=gp.Wallet;');
const header = base.slice(0, base.indexOf('public static class'));
const code = header + `public static class G2Realtime
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static T Asset<T>(string name) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>("Assets/Game/ScriptableObjects/Units/"+name+".asset");
    static object Field(object obj,string name)=>obj.GetType().GetField(name,Private).GetValue(obj);
    static void Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Private).Invoke(obj,args);
    public static string Economy()=>Start(true);
    public static string Military()=>Start(false);
    static string Start(bool economy)
    {
        bool control=false,reserve=false;
${init}
        runId+="-realtime";
        var path=dir+"/"+runId+".txt";
        float startTime=Time.time,lastSample=-1,nextPolicy=0;
        double startedEditor=EditorApplication.timeSinceStartup;
        var previousWallet=gp.Wallet;
        bool oldBackground=Application.runInBackground;
        Application.runInBackground=true;
        UnityEngine.Random.InitState(waveSeed);
        log.AppendLine($"schema=G2-realtime-v1;runId={runId};configId={configId};hallMax={expectedHP};mapSeed={generator.GeneratedMap.Seed};waveSeed={waveSeed};policy={policy};rng=normal-shared-UnityRandom;speed=1;nativeTicks=UnityPlayerLoop;noResourceGrant;objective=5nights");
        if(hall.CurrentHealth!=expectedHP||em.GetComponentData<CombatBody>(hallEntity).Health!=expectedHP||Time.timeScale!=1)
            throw new Exception("Realtime fresh HP/timeScale mismatch.");
        Snapshot();
        EditorApplication.CallbackFunction update=null;
        void Finish(string error)
        {
            EditorApplication.update-=update;
            actors.Dispose();resources.Dispose();enemies.Dispose();
            Application.runInBackground=oldBackground;UnityEngine.Random.state=random;
            log.AppendLine($"finished wallSeconds={EditorApplication.timeSinceStartup-startedEditor:F2};error={error}");
            System.IO.File.WriteAllText(path,log.ToString());
        }
        update=()=>
        {
            try
            {
                if(!EditorApplication.isPlaying){Finish("Play stopped before terminal");return;}
                if(EditorApplication.isPaused)return;
                elapsed=Time.time-startTime;
                if(!gp.IsMatchOver&&elapsed-lastSample<.05f)return;
                lastSample=elapsed;
                var now=gp.Wallet;
                deposited[0]+=now.Wood-previousWallet.Wood;deposited[1]+=now.Gold-previousWallet.Gold;deposited[2]+=now.Meat-previousWallet.Meat;
                if(!gp.IsMatchOver)
                {
${policy}
                }
                previousWallet=gp.Wallet;Observe();
                if(phase!=day.CurrentPhase){Snapshot();phase=day.CurrentPhase;}
                var liveHall=em.Exists(hallEntity)?em.GetComponentData<CombatBody>(hallEntity).Health:0;
                var status=new {runId,hp=expectedHP,policy,elapsed,day=day.CurrentDay,phase=day.CurrentPhase.ToString(),hallHP=liveHall,outcome=gp.Outcome.ToString(),attacks,spawned=sim.TotalSpawned-startSpawned,wallSeconds=EditorApplication.timeSinceStartup-startedEditor};
                if(gp.IsMatchOver||((int)elapsed)%5==0)System.IO.File.WriteAllText(dir+"/realtime-status.json",Newtonsoft.Json.JsonConvert.SerializeObject(status));
                if(gp.IsMatchOver)
                {
                    Snapshot();
                    for(int i=0;i<3;i++)if(initial.Get(i)+deposited[i]-spent[i]!=gp.Wallet.Get(i)||spent[i]!=researchSpent[i]+trainSpent[i]+buildSpent[i])throw new Exception("Realtime ledger mismatch.");
                    log.AppendLine($"ledger=PASS;researchSpend={string.Join("/",researchSpent)};trainSpend={string.Join("/",trainSpent)};buildSpend={string.Join("/",buildSpent)};attacks={attacks};firstAttack={firstAttack};rallyCommands={rallyCommands};lineArrivals={contacted.Count};hallAttackers={hitHall.Count};retreatBeforeLine={retreatedCold.Count}");
                    log.AppendLine($"outcome={gp.Outcome};elapsed={elapsed:F2};depositedW/G/M={string.Join("/",deposited)};spentW/G/M={string.Join("/",spent)};peakWorkers={peakWorkers};peakSoldiers={peakSoldiers};trainingAccepted={trained}");
                    Finish("");return;
                }
                if(elapsed>750||EditorApplication.timeSinceStartup-startedEditor>1000)throw new Exception("Realtime deadline exceeded.");
            }
            catch(Exception ex){Finish(ex.ToString());}
        };
        EditorApplication.update+=update;
        EditorApplication.isPaused=false;
        System.IO.File.WriteAllText(dir+"/realtime-status.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {runId,hp=expectedHP,policy,status="started"}));
        return "Realtime monitor installed: "+runId;
    }
}
`;
assert(!code.includes('gp.Update()') && !code.includes('World.SetTime('));
const out = `${dir}/G2Realtime-derived.cs.txt`;
if (fs.existsSync(out)) assert.equal(fs.readFileSync(out, 'utf8'), code);
else fs.writeFileSync(out, code);
console.log(JSON.stringify({status: 'PASS', out, unityPlayerLoop: true}));
