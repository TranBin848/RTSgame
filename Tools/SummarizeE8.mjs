import fs from 'node:fs';
import assert from 'node:assert/strict';
const dir='Docs/Modules/GameExperience/Evidence/E8';
const files=fs.readdirSync(`${dir}/Logistics`).filter(p=>p.endsWith('.txt')&&!p.includes('FAILED'));
const rows=files.map(file=>{
  const text=fs.readFileSync(`${dir}/Logistics/${file}`,'utf8');
  const header=text.split('\n').find(l=>l.startsWith('schema='));
  assert(header.includes('mapSeed=0;waveSeed=8001;')&&header.includes('noResourceGrant'));
  const policy=/policy=([^;]+)/.exec(header)[1];
  const statistics=JSON.parse(/^nativeStatistics=(.+)$/m.exec(text)[1]);
  const final=[...text.matchAll(/^t=([\d.]+);.*W\/G\/M=(\d+)\/(\d+)\/(\d+);.*hallHP=([\d.]+);spawned=(\d+)/gm)].at(-1);
  ['Wood','Gold','Meat'].forEach((key,i)=>assert.equal(+final[i+2],statistics.Granted[key]+statistics.Deposited[key]-statistics.Spent[key]));
  const deliveries=[...text.matchAll(/^delivery t=([\d.]+);.*amount=(\d+);seconds=([\d.]+);destination=(\w+)/gm)]
    .map(m=>({time:+m[1],amount:+m[2],seconds:+m[3],destination:m[4]})).filter(d=>d.time<=535);
  const seconds=deliveries.map(d=>d.seconds).sort((a,b)=>a-b);
  const ranges=[...text.matchAll(/(?:initialTownHallRange|townHallRange)=([\d.]+)/g)].map(m=>+m[1]);
  assert(ranges.every(r=>r===14.13));
  assert(text.includes('ledger=PASS'));
  return {file,policy,seed:0,waveSeed:8001,dt:.05,windowSeconds:535,deliveries:deliveries.length,
    medianDeliverySeconds:seconds.length?seconds[Math.floor(seconds.length/2)]:null,goldDeposited:deliveries.reduce((n,d)=>n+d.amount,0),
    netWood:statistics.Deposited.Wood-statistics.Spent.Wood,netGold:deliveries.reduce((n,d)=>n+d.amount,0)-statistics.Spent.Gold,
    statistics,wallet:{Wood:+final[2],Gold:+final[3],Meat:+final[4]},outcome:/outcome=(\w+)/.exec(text)[1],
    elapsed:+final[1],spawned:+final[6],townHallRange:ranges,ledgerPassed:true};
});
assert.equal(rows.length,3);
const hall=rows.find(r=>r.policy==='logistics-hall'),depot=rows.find(r=>r.policy==='logistics-depot'),empty=rows.find(r=>r.policy==='empty-depot');
assert.equal(hall.goldDeposited,80);assert.equal(depot.goldDeposited,240);assert.equal(empty.statistics.Deposited.Gold,0);
assert.equal(depot.statistics.Spent.Wood,100);assert.equal(depot.statistics.Spent.Gold,100);
fs.writeFileSync(`${dir}/logistics-summary.json`,JSON.stringify({scope:'Controlled Editor ECS policy; not Player performance or manual G3 acceptance',passed:true,rows,
  comparison:{extraGoldDeposited:depot.goldDeposited-hall.goldDeposited,extraNetGold:depot.netGold-hall.netGold,extraWoodCost:100},
  limitations:['One map/wave seed only','No research/training/rally defence in logistics policy','Building uses an additional builder as the intervention','All three policies lose on night4','G3 human acceptance explicitly remains open']},null,2));
console.log(JSON.stringify({passed:true,rows:rows.map(r=>({policy:r.policy,deliveries:r.deliveries,median:r.medianDeliverySeconds,gold:r.goldDeposited,netGold:r.netGold,outcome:r.outcome}))}));
