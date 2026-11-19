import fs from 'node:fs';
import assert from 'node:assert/strict';
const dir = 'Docs/Modules/Progression/Evidence/G2-2026-11-19';
const rows = [];
const fields = line => Object.fromEntries(line.split(';').map(x => {const i=x.indexOf('=');return [x.slice(0,i),x.slice(i+1)];}));
const values = value => value.split('/').map(Number);
for (const file of fs.readdirSync(dir).filter(f => /^\d.*\.txt$/.test(f))) {
  const lines = fs.readFileSync(`${dir}/${file}`, 'utf8').split(/\r?\n/);
  if (!lines[0].startsWith('schema=G2-v1;') && !lines[0].startsWith('schema=G2-realtime-v1;')) continue;
  assert(!file.includes('FAILED'), `${file}: failed run`);
  const meta = fields(lines[0]);
  const end = fields(lines.filter(l=>l.startsWith('t=')).at(-1));
  const result = fields(lines.find(l=>l.startsWith('outcome=')) ?? 'outcome=Incomplete');
  assert.notEqual(result.outcome, 'Incomplete', file);
  const ledger = fields(lines.find(l=>l.startsWith('ledger=')));
  assert.equal(ledger.ledger, 'PASS', file);
  const spent = values(result['spentW/G/M']);
  const deposited = values(result['depositedW/G/M']);
  const wallet = values(end['W/G/M']);
  for(let i=0;i<3;i++) {
    assert.equal(100+deposited[i]-spent[i],wallet[i],`${file}: wallet ${i}`);
    assert.equal(['researchSpend','trainSpend','buildSpend'].reduce((n,k)=>n+values(ledger[k])[i],0),spent[i]);
  }
  assert.equal(meta.mapSeed, '1');assert.equal(meta.waveSeed,'8001');
  assert.equal(meta.configId, fs.readFileSync(`${dir}/config-hp${meta.hallMax}.txt`,'utf8').trim());
  const realtime=meta.schema==='G2-realtime-v1';
  if(!realtime) {
    const resolved=JSON.parse(lines.find(l=>l.startsWith('resolved=')).slice(9));
    assert.equal(resolved.hallMax,Number(meta.hallMax));
    assert.equal(resolved.hallCurrent,resolved.hallMax);assert.equal(resolved.entityHealth,resolved.hallMax);
    const schedules=lines.filter(l=>l.startsWith('schedule ')).map(fields);
    const eligible=schedules.reduce((n,r)=>n+Number(r.eligibleSlots),0);
    if(result.outcome==='Victory')assert.equal(Number(end.spawned),eligible);
    else assert(Number(end.spawned)<=eligible);
  } else assert(lines.at(-2)?.startsWith('finished ') && lines.at(-2).endsWith('error='), `${file}: realtime error`);
  if(meta.policy==='control'){assert.deepEqual(spent,[0,0,0]);assert.equal(ledger.rallyCommands,'0');}
  else assert(Number(ledger.attacks)>0, `${file}: no engagement`);
  if(result.outcome==='Victory')assert.equal(Number(end.day),6);
  rows.push({file,hp:Number(meta.hallMax),policy:meta.policy,realtime,config:meta.configId,outcome:result.outcome,
    elapsed:Number(result.elapsed),day:Number(end.day),hallHP:Number(end.hallHP),attacks:Number(ledger.attacks),
    deposited,spent,wallet,spawned:Number(end.spawned),soldiers:Number(end.soldiers)});
}
rows.sort((a,b)=>b.hp-a.hp||a.policy.localeCompare(b.policy));
if(process.argv.includes('--complete')) {
  for(const hp of [10000,1600,1200])for(const policy of ['control','economy','military'])
    assert.equal(rows.filter(r=>r.hp===hp&&r.policy===policy&&!r.realtime).length,1,`${hp}/${policy}`);
}
if(process.argv.includes('--compact'))console.table(rows.map(({hp,policy,realtime,outcome,elapsed,day,hallHP,attacks,spawned})=>({hp,policy,realtime,outcome,elapsed,day,hallHP,attacks,spawned})));
else console.log(JSON.stringify({checks:'PASS',rows},null,2));
