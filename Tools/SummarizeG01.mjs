// Read-only checks for G01 raw data; no Unity or npm dependencies.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';

const dir = 'Docs/Modules/Progression/Evidence/G01-2026-11-18';
const rows = [];
for (const name of fs.readdirSync(dir).filter(n => n.endsWith('.txt'))) {
  const lines = fs.readFileSync(path.join(dir, name), 'utf8').split(/\r?\n/);
  if (!lines[0].startsWith('schema=G01-v3;')) continue;
  assert(!name.includes('FAILED'), `Failed run: ${name}`);
  const fields = line => Object.fromEntries(line.split(';').map(f => {
    const i = f.indexOf('='); return [f.slice(0, i), f.slice(i + 1)];
  }));
  const meta = fields(lines[0]);
  const resolved = JSON.parse(lines.find(l => l.startsWith('resolved=')).slice(9));
  const ledger = fields(lines.find(l => l.startsWith('ledger=')));
  const result = fields(lines.find(l => l.startsWith('outcome=')));
  const end = fields(lines.filter(l => l.startsWith('t=')).at(-1));
  if (result.outcome === 'Victory') assert.equal(Number(end.day), 6, `${name}: five-night objective`);
  const values = text => text.split('/').map(Number);
  const initial = ['Wood', 'Gold', 'Meat'].map(k => resolved.initial[k]);
  const deposited = values(result['depositedW/G/M']);
  const spend = values(result['spentW/G/M']);
  const final = values(end['W/G/M']);
  const categories = ['researchSpend', 'trainSpend', 'buildSpend'].map(k => values(ledger[k]));
  for (let i = 0; i < 3; i++) {
    assert.equal(initial[i] + deposited[i] - spend[i], final[i], `${name}: wallet ${i}`);
    assert.equal(categories.reduce((s, a) => s + a[i], 0), spend[i], `${name}: spend ${i}`);
  }
  assert.equal(ledger.ledger, 'PASS');
  assert.equal(resolved.hallMax, resolved.hallCurrent);
  assert.equal(resolved.hallCurrent, resolved.entityHealth);
  assert.equal(meta.waveSeed, '8001');
  if (meta.policy === 'control') {
    assert.equal(Number(ledger.rallyCommands), 0);
    assert.deepEqual(spend, [0, 0, 0]);
    assert.equal(result.expansion, 'not attempted (control)');
  } else assert(Number(ledger.attacks) > 0, `${name}: no army engagement`);
  const schedules = lines.filter(l => l.startsWith('schedule ')).map(fields);
  const eligible = schedules.reduce((s, r) => s + Number(r.eligibleSlots), 0);
  assert.equal(Number(end.spawned), eligible, `${name}: eligible/spawn mismatch`);
  const rallyTravel = lines.filter(l => l.startsWith('army atHallLine='))
    .map(l => Number(l.match(/sinceRally=([\d.]+)/)[1]));
  const suppressedArmySamples = lines.filter(l => l.startsWith('actor ') &&
    /kind=(Soldier|Archer);/.test(l) && l.includes('suppress=1')).length;
  assert.equal(suppressedArmySamples, 0, `${name}: acquisition suppressed`);
  rows.push({file: name, config: meta.configId, seed: Number(meta.mapSeed), policy: meta.policy,
    outcome: result.outcome, hallHP: Number(end.hallHP), attacks: Number(ledger.attacks),
    firstAttack: Number(ledger.firstAttack), rally: Number(ledger.rallyCommands),
    workers: Number(end.workers), soldiers: Number(end.soldiers),
    peakWorkers: Number(result.peakWorkers), peakSoldiers: Number(result.peakSoldiers),
    deposited, spend, final, spawn: Number(end.spawned), eligible,
    arrivals: Number(ledger.lineArrivals), hallAttackers: Number(ledger.hallAttackers),
    retreatBeforeLine: Number(ledger.retreatBeforeLine), expansion: result.expansion,
    expansionAt: Number(result.expansionAt), stalls: lines.filter(l => l.startsWith('stalled ')),
    rallyObserved: rallyTravel.length,
    rallyTravelMin: rallyTravel.length ? Math.min(...rallyTravel) : null,
    rallyTravelMax: rallyTravel.length ? Math.max(...rallyTravel) : null,
    initialEnemies: lines.filter(l => l.startsWith('enemy ') && l.endsWith('initial=True')).length});
}
rows.sort((a, b) => a.seed - b.seed || a.policy.localeCompare(b.policy));
if (process.argv.includes('--complete')) {
  assert.equal(rows.length, 12, 'Expected exactly twelve final runs');
  for (const seed of [0, 1, 1543]) for (const policy of ['control', 'economy', 'military', 'expansion'])
    assert.equal(rows.filter(r => r.seed === seed && r.policy === policy).length, 1, `${seed}/${policy}`);
  assert.equal(new Set(rows.map(r => r.config)).size, 1, 'Mixed config IDs');
}
if (process.argv.includes('--compact'))
  console.table(rows.map(r => ({seed: r.seed, policy: r.policy, outcome: r.outcome,
    hallHP: r.hallHP, attacks: r.attacks, deposit: r.deposited.join('/'),
    soldiers: r.soldiers, spawn: r.spawn, expansionAt: r.expansionAt, stalls: r.stalls.length})));
else console.log(JSON.stringify({checks: 'PASS', count: rows.length, rows}, null, 2));
