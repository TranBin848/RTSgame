import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';

const [root, destination] = process.argv.slice(2);
assert(root && destination, 'Usage: node Tools/SummarizeG5.mjs <run-directory> <summary.json>');
const startupIds = ['seed1-startup-first', 'seed1-run1', 'seed1-run2', 'seed1-run3', 'seed1-startup-warm'];
const heavyId = 'seed1543-military';
const json = (id, name) => JSON.parse(fs.readFileSync(path.join(root, id, name), 'utf8'));
const metric = values => {
  const ordered = [...values].sort((a, b) => a - b);
  const percentile = p => ordered[Math.ceil(ordered.length * p) - 1];
  return {median: percentile(.5), p95: percentile(.95), p99: percentile(.99), max: ordered.at(-1)};
};
const runs = [...startupIds, heavyId].map(id => {
  const result = json(id, 'result.json');
  const heavy = id === heavyId;
  assert(result.errors.length === 0 && result.seed === (heavy ? 1543 : 1)
    && result.hallMaxHealth === 1200 && result.sceneToReadyMs > 0
    && result.onscreen && !result.development && !result.offscreenRender
    && result.screenWidth === 1920 && result.screenHeight === 1080 && result.screenMode === 'FullScreenWindow'
    && result.hudActive && result.fogActive, 'Invalid Release config: ' + id);
  if (id.includes('startup')) {
    assert(result.startupOnly && !result.csv);
    return {id, ...result};
  }
  assert(!result.startupOnly && result.waveSeed === 8001 && result.foreground && result.invalidOnscreenFrames === 0
    && result.workers >= 4 && result.spawned > 0 && result.drawCalls > 0
    && (heavy ? result.extraTrainingAccepted >= 2 : result.extraTrainingAccepted === 0), 'Invalid workload: ' + id);
  const raw = fs.readFileSync(path.join(root, id, path.basename(result.csv)), 'utf8');
  const rows = raw.split(/\r?\n/).filter(line => /^\d+,/.test(line)).map(line => line.split(',').map(Number));
  assert(rows.length > 100 && rows.length < 36000 && rows.length === result.onscreenFrames
    && rows.every((row, i) => row[0] === i && row.length === 8 && row.every(Number.isFinite)
      && row[1] > 0 && row[5] >= -1), 'Invalid frame samples: ' + id);
  assert(rows.some(row => row[3] > 0), 'No enemies sampled: ' + id);
  const seconds = rows.reduce((sum, row) => sum + row[1], 0) / 1000;
  assert(Math.abs(seconds - 90) < 0.5, 'Short or incomplete capture: ' + id);
  const png = fs.readFileSync(path.join(root, id, 'screenshot.png'));
  assert(png.toString('ascii', 12, 16) === 'IHDR' && png.readUInt32BE(16) === 1920
    && png.readUInt32BE(20) === 1080, 'Invalid screenshot: ' + id);
  const frameMs = metric(rows.map(row => row[1]));
  const gcAvailable = rows.every(row => row[5] >= 0);
  return {id, ...result, samples: rows.length, seconds, frameMs, overBudget: rows.filter(row => row[1] > 1000 / 60).length,
    gcBytes: gcAvailable ? metric(rows.map(row => row[5])) : null, budgetP95Pass: frameMs.p95 <= 1000 / 60};
});
const timed = runs.filter(run => !run.startupOnly);
assert(timed.every(run => run.vSync === timed[0].vSync && run.frameCap === timed[0].frameCap
  && run.quality === timed[0].quality && run.graphicsApi === timed[0].graphicsApi), 'Display settings varied');
const smoke = json('gameplay-smoke', 'gameplay-result.json');
assert(smoke.passed && !smoke.errors.length && smoke.checks.length === 18 && smoke.checks.every(check => check.passed));
const startupValues = runs.filter(run => startupIds.includes(run.id)).map(run => run.sceneToReadyMs);
const startup = {firstLaunchMs: startupValues[0], samplesMs: startupValues, minMs: Math.min(...startupValues),
  medianMs: metric(startupValues).median, maxMs: Math.max(...startupValues)};
fs.writeFileSync(destination, JSON.stringify({frameBudgetMs: 1000 / 60, startup, runs, smoke}, null, 2) + '\n');
console.log(JSON.stringify({startup, runs: timed.map(run => ({id: run.id, samples: run.samples, frameMs: run.frameMs,
  gcBytes: run.gcBytes, p95Pass: run.budgetP95Pass, overBudget: run.overBudget}))}, null, 2));
