import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {execFileSync} from 'node:child_process';

const [root, output] = process.argv.slice(2);
assert(root && output, 'Usage: node Tools/SummarizeE10.mjs <run-directory> <summary.json>');
execFileSync(process.execPath, ['Tools/SummarizeG5.mjs', path.join(root, 'g5'), path.join(root, 'g5-summary.json')], {stdio: 'inherit'});
const g5 = JSON.parse(fs.readFileSync(path.join(root, 'g5-summary.json')));
for (const run of g5.runs.filter(run => !run.startupOnly)) {
  const samples = fs.readFileSync(path.join(root, 'g5', run.id, path.basename(run.csv)), 'utf8').split(/\r?\n/)
    .filter(line => /^\d+,/.test(line)).map(line => line.split(',').map(Number));
  run.peakEnemies = Math.max(...samples.map(row => row[3]));
  run.peakProjectiles = Math.max(...samples.map(row => row[4]));
}
const lifecycle = JSON.parse(fs.readFileSync(path.join(root, 'lifecycle/lifecycle-result.json')));
assert(lifecycle.passed && !lifecycle.development && lifecycle.checks.length === 15 && lifecycle.errors.length === 0, 'Release lifecycle failed');
const folder = path.join(root, 'night-stress');
const stress = JSON.parse(fs.readFileSync(path.join(folder, 'result.json')));
assert(stress.errors.length === 0 && stress.e10NightStress && stress.onscreen && !stress.development && !stress.offscreenRender
  && stress.foreground && stress.invalidOnscreenFrames === 0 && stress.screenWidth === 1920 && stress.screenHeight === 1080
  && stress.hudActive && stress.fogActive && stress.peakLocalLights === 8 && stress.peakPulses === 32, 'Night stress invalid');
const rows = fs.readFileSync(path.join(folder, path.basename(stress.csv)), 'utf8').split(/\r?\n/)
  .filter(line => /^\d+,/.test(line)).map(line => line.split(',').map(Number));
assert(rows.length > 100 && rows.length === stress.onscreenFrames && rows.every((row, i) => row.length === 8 && row[0] === i
  && row.every(Number.isFinite) && row[1] > 0), 'Stress CSV incomplete');
const seconds = rows.reduce((n, row) => n + row[1], 0) / 1000;
// SpawnEnemy can reject a blocked cell; 96 is the fixture's target/cap, not a guaranteed live count.
assert(Math.abs(seconds - 90) < .5 && rows.every(row => row[3] > 0 && row[3] <= 96), 'Stress duration/enemy samples invalid');
const values = rows.map(row => row[1]).sort((a, b) => a - b);
const percentile = p => values[Math.ceil(values.length * p) - 1];
const frameMs = {median: percentile(.5), p95: percentile(.95), p99: percentile(.99), max: values.at(-1)};
const timed = g5.runs.filter(run => !run.startupOnly);
assert(timed.every(run => run.quality === stress.quality && run.vSync === stress.vSync && run.frameCap === stress.frameCap
  && run.graphicsApi === stress.graphicsApi), 'Stress settings differed from G5');
const result = {frameBudgetMs: 1000 / 60, g5, lifecycle, stress: {...stress, samples: rows.length, seconds, frameMs,
  enemyTarget: 96, enemyTargetReached: rows.some(row => row[3] === 96),
  minEnemies: Math.min(...rows.map(row => row[3])), peakEnemies: Math.max(...rows.map(row => row[3])), peakProjectiles: Math.max(...rows.map(row => row[4])),
  overBudget: rows.filter(row => row[1] > 1000 / 60).length, gcAvailable: rows.every(row => row[5] >= 0),
  budgetP95Pass: frameMs.p95 <= 1000 / 60},
  allMeasuredP95Pass: timed.every(run => run.budgetP95Pass) && frameMs.p95 <= 1000 / 60};
fs.writeFileSync(output, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify({stress: result.stress, allMeasuredP95Pass: result.allMeasuredP95Pass}, null, 2));
