import fs from 'node:fs';
import assert from 'node:assert/strict';
const dir='Docs/Modules/GameExperience/Evidence/E9/Realtime';
const result=JSON.parse(fs.readFileSync(dir+'/natural-result.json','utf8'));
const trace=JSON.parse(fs.readFileSync(dir+'/spawn-trace.json','utf8'));
const matrix=JSON.parse(fs.readFileSync('Docs/Modules/GameExperience/Evidence/E9/matrix-summary.json','utf8'));
const reference=matrix.rows.find(r=>r.seed===1&&r.policy==='economy');
const referenceTrace=JSON.parse(fs.readFileSync('Docs/Modules/GameExperience/Evidence/E9/Matrix/'+reference.file.replace('.txt','-spawn.json'),'utf8')).records;
const lines=fs.readFileSync(dir+'/natural-five-nights.txt','utf8').split('\n');
const keys=['Wood','Gold','Meat'];
let samples=0,dawns=0;
const sum={Deposited:{Wood:0,Gold:0,Meat:0},Spent:{Wood:0,Gold:0,Meat:0},Granted:{Wood:0,Gold:0,Meat:0},UnitsLost:0,ResearchCompleted:0,TownHallDamage:0};
let explored=0;
for(const line of lines){
  if(line.startsWith('t=')){
    const match=/wallet=(\d+)\/(\d+)\/(\d+);stats=(.+)/.exec(line);assert(match);
    const s=JSON.parse(match[4]).Statistics;
    keys.forEach((k,i)=>assert.equal(+match[i+1],s.Granted[k]+s.Deposited[k]-s.Spent[k]));
    samples++;
  }
  if(line.startsWith('dawn=')){
    const s=JSON.parse(line.slice(line.indexOf('snapshot=')+9));dawns++;explored+=s.ExploredCells;
    ['Deposited','Spent','Granted'].forEach(c=>keys.forEach(k=>sum[c][k]+=s.Statistics[c][k]));
    ['UnitsLost','ResearchCompleted','TownHallDamage'].forEach(k=>sum[k]+=s.Statistics[k]);
  }
}
assert(['Victory','Defeat'].includes(result.outcome));
assert.equal(result.timeScaleBeforeTerminal,1);assert.deepEqual(result.durations,[60,30,45]);assert(result.cameraLocked&&result.fogEnabled);
assert.equal(result.completedNights,dawns);assert.equal(result.signals,dawns);assert(samples>0);
assert.equal(trace.waveSeed,8001);
const unique=trace.records.map(r=>r.Day+'/'+r.Wave+'/'+r.Ordinal);assert.equal(unique.length,new Set(unique).size);
for(let i=0;i<trace.records.length;i++){
  const a=trace.records[i],b=referenceTrace[i];assert(b);
  assert.deepEqual([a.Day,a.Wave,a.Ordinal,a.PrefabSlot,a.RequestedGateway],[b.Day,b.Wave,b.Ordinal,b.PrefabSlot,b.RequestedGateway]);
}
const s=result.total.Statistics;
keys.forEach(k=>{assert.equal(s.Granted[k],100);assert.equal(result.wallet[k],s.Granted[k]+s.Deposited[k]-s.Spent[k]);});
const tail={};
for(const category of ['Deposited','Spent','Granted'])for(const k of keys){tail[category+'.'+k]=s[category][k]-sum[category][k];assert(tail[category+'.'+k]>=0);}
for(const k of ['UnitsLost','ResearchCompleted','TownHallDamage']){tail[k]=s[k]-sum[k];assert(tail[k]>=0);}
tail.ExploredCells=result.total.ExploredCells-explored;assert(tail.ExploredCells>=0);
if(result.outcome==='Victory'){assert.equal(dawns,5);assert.equal(trace.records.length,76);}
assert(lines.some(l=>l.startsWith('finished ')&&l.trimEnd().endsWith('error=')),'Realtime runner did not finish cleanly');
fs.writeFileSync(dir+'/audit.json',JSON.stringify({passed:true,outcome:result.outcome,samples,dawns,spawns:trace.records.length,scheduleMatchesControlledPrefix:true,
  walletReconciled:true,grantsOnlyInitial100:true,tail,failedSpawns:trace.FailedSpawnAttempts,fallbackSpawns:trace.FallbackSpawnCount,
  scope:'Normal Editor PlayerLoop command-policy playtest, not a human win rate or Player performance benchmark'},null,2));
console.log(JSON.stringify({passed:true,outcome:result.outcome,samples,dawns,spawns:trace.records.length,damage:s.TownHallDamage}));
