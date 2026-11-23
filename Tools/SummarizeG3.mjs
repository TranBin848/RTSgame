import fs from 'node:fs';
import assert from 'node:assert/strict';

const dir = 'Docs/Modules/Progression/Evidence/G3-2026-11-21';
const fields = line => Object.fromEntries(line.split(';').map(field => {
  const split = field.indexOf('=');
  return [field.slice(0, split), field.slice(split + 1)];
}));
const amounts = field => field.split('/').map(Number);
const median = numbers => {
  if (!numbers.length) return null;
  numbers.sort((a, b) => a - b);
  return (numbers[Math.floor((numbers.length - 1) / 2)] + numbers[Math.ceil((numbers.length - 1) / 2)]) / 2;
};
const rows = [];
for (const file of fs.readdirSync(dir).filter(name => /^\d.*\.txt$/.test(name))) {
  if (file.includes('FAILED')) continue;
  const lines = fs.readFileSync(dir + '/' + file, 'utf8').split(/\r?\n/);
  const line = prefix => lines.find(value => value.startsWith(prefix));
  const meta = fields(line('schema='));
  const paired = meta.policy.startsWith('logistics-') && meta.schema === 'G3-v4';
  const end = fields(lines.filter(value => value.startsWith('t=')).at(-1));
  const outcome = fields(line('outcome='));
  const ledger = fields(line('ledger='));
  const resolved = JSON.parse(line('resolved=').slice(9));
  const hp = Number(meta.hallMax);
  assert.equal(resolved.hallMax, hp);
  assert.equal(resolved.hallCurrent, hp);
  assert.equal(resolved.entityHealth, hp);
  assert.equal(ledger.ledger, 'PASS');
  assert.equal(meta.waveSeed, '8001');
  const deposit = amounts(outcome['depositedW/G/M']);
  const spend = amounts(outcome['spentW/G/M']);
  const wallet = amounts(end['W/G/M']);
  wallet.forEach((balance, i) => assert.equal(balance, 100 + deposit[i] - spend[i], file));
  const trips = lines.filter(value => value.startsWith('delivery ')).map(fields);
  if (paired) {
    assert.equal(ledger.trainingAccepted ?? outcome.trainingAccepted, '0', file);
    assert.equal(ledger.rallyCommands, '0', file);
    assert.deepEqual(amounts(ledger.researchSpend), [0, 0, 0], file);
    assert(trips.every(trip => trip.type === '1'), file);
  }
  rows.push({
    file, schema: meta.schema, config: meta.configId, hp, seed: Number(meta.mapSeed), policy: meta.policy,
    outcome: outcome.outcome, day: Number(end.day), elapsed: Number(outcome.elapsed), hall: Number(end.hallHP),
    attacks: Number(ledger.attacks), arrivals: Number(ledger.lineArrivals), spawned: Number(end.spawned),
    deposit, spend, unlocks: outcome.unlockSeconds, initialRange: line('initialTownHallRange='),
    logisticsNode: line('logisticsNode='), paired,
    depotPlaced: line('depotPlaced '), depotCompleted: line('depotCompleted '), depot: line('depotSummary '),
    hallTripSeconds: median(trips.filter(trip => trip.destination === 'hall').map(trip => Number(trip.seconds))),
    landmarkTripSeconds: median(trips.filter(trip => trip.destination === 'landmark').map(trip => Number(trip.seconds))),
    trips,
  });
}
assert.equal(new Set(rows.map(row => row.file)).size, rows.length);
if (process.argv.includes('--compact')) console.table(rows.map(({ seed, hp, policy, outcome, day, hall, attacks, arrivals, spawned, depot }) =>
  ({ seed, hp, policy, outcome, day, hall, attacks, arrivals, spawned, depot })));
else console.log(JSON.stringify({ checks: 'PASS', rows }, null, 2));
