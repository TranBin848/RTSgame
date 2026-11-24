import fs from 'node:fs';
import assert from 'node:assert/strict';
const dir='Docs/Modules/GameExperience/Evidence/E9/Matrix';
const files=fs.existsSync(dir)?fs.readdirSync(dir).filter(p=>p.endsWith('.txt')&&!p.includes('FAILED')):[];
const rows=files.map(file=>{
  const text=fs.readFileSync(dir+'/'+file,'utf8');
  const schema=text.split('\n').find(l=>l.startsWith('schema='));
  const get=key=>new RegExp(key+'=([^;\\r\\n]+)').exec(schema)[1];
  const seed=+get('mapSeed'),policy=get('policy');
  const statistics=JSON.parse(/^nativeStatistics=(.+)$/m.exec(text)[1]);
  const final=[...text.matchAll(/^t=([\d.]+);.*W\/G\/M=(\d+)\/(\d+)\/(\d+);.*hallHP=([\d.]+);spawned=(\d+)/gm)].at(-1);
  assert(final,'Missing final sample: '+file);
  ['Wood','Gold','Meat'].forEach((key,i)=>assert.equal(+final[i+2],statistics.Granted[key]+statistics.Deposited[key]-statistics.Spent[key],file));
  assert(text.includes('ledger=PASS'),file);
  assert.equal(statistics.Granted.Wood,100);assert.equal(statistics.Granted.Gold,100);assert.equal(statistics.Granted.Meat,100);
  const trace=JSON.parse(fs.readFileSync(dir+'/'+file.replace('.txt','-spawn.json'),'utf8'));
  const keys=trace.records.map(r=>r.Day+'/'+r.Wave+'/'+r.Ordinal);
  assert.equal(keys.length,new Set(keys).size,'Duplicate spawn event: '+file);
  assert.equal(trace.records.length,+final[6],'Trace/spawn count mismatch: '+file);
  assert.equal(trace.waveSeed,8001);
  const outcome=/^outcome=(\w+)/m.exec(text)[1];
  return {file,seed,policy,outcome,seconds:+final[1],hallHP:+final[5],spawned:+final[6],statistics,
    failedSpawns:trace.FailedSpawnAttempts,fallbackSpawns:trace.FallbackSpawnCount,trace};
});
let deterministic=true;
for(const seed of [...new Set(rows.map(r=>r.seed))]){
  const group=rows.filter(r=>r.seed===seed);
  for(let i=0;i<group.length;i++)for(let j=i+1;j<group.length;j++){
    const a=group[i].trace.records,b=group[j].trace.records;
    for(let n=0;n<Math.min(a.length,b.length);n++)
      if(JSON.stringify([a[n].Day,a[n].Wave,a[n].Ordinal,a[n].PrefabSlot,a[n].RequestedGateway])!==JSON.stringify([b[n].Day,b[n].Wave,b[n].Ordinal,b[n].PrefabSlot,b[n].RequestedGateway]))deterministic=false;
  }
}
assert(deterministic,'Schedule selection changed between policies sharing map/wave seed');
const gates=[0,1,1543].map(seed=>({seed,complete:['control','economy','military','expansion'].every(p=>rows.some(r=>r.seed===seed&&r.policy===p)),
  controlLoses:rows.some(r=>r.seed===seed&&r.policy==='control'&&r.outcome==='Defeat'),
  activeWin:rows.some(r=>r.seed===seed&&r.policy!=='control'&&r.outcome==='Victory')}));
const summary={schema:'E9-matrix-v1',scope:'Controlled Editor .05s policy ticks, not human win rate or Player performance',runs:rows.length,deterministic,
  rows:rows.map(({trace,...r})=>r),trainingGates:gates,holdouts:rows.filter(r=>[137,901].includes(r.seed)).map(({trace,...r})=>r)};
fs.writeFileSync('Docs/Modules/GameExperience/Evidence/E9/matrix-summary.json',JSON.stringify(summary,null,2));
console.log(JSON.stringify({runs:rows.length,rows:rows.map(r=>({seed:r.seed,policy:r.policy,outcome:r.outcome,hp:r.hallHP})),gates,deterministic}));
