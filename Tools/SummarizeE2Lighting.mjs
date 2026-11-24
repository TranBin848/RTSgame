import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';

const [root, destination] = process.argv.slice(2);
assert(root && destination, 'Usage: node Tools/SummarizeE2Lighting.mjs <run-directory> <summary.json>');
const runs = ['baseline1', 'candidate1', 'baseline2', 'candidate2'].map(id => {
  const result = JSON.parse(fs.readFileSync(path.join(root, id, 'result.json'), 'utf8'));
  const candidate = id.startsWith('candidate');
  assert(result.errors.length === 0 && result.onscreen && !result.development && !result.offscreenRender
    && result.foreground && result.invalidOnscreenFrames === 0 && result.screenWidth === 1920 && result.screenHeight === 1080
    && result.screenMode === 'FullScreenWindow' && result.hudActive && result.fogActive && result.waveSeed === 8001
    && result.seed === 1 && result.hallMaxHealth === 1200 && result.e2LightingEnabled === candidate, 'Invalid run: ' + id);
  assert(candidate ? result.peakLocalLights >= 1 && result.peakLocalLights <= 8 : result.peakLocalLights === 0, 'Light counter invalid');
  const rows = fs.readFileSync(path.join(root, id, path.basename(result.csv)), 'utf8').split(/\r?\n/)
    .filter(line => /^\d+,/.test(line)).map(line => line.split(',').map(Number));
  assert(rows.length > 100 && rows.length === result.onscreenFrames
    && rows.every((row, i) => row.length === 8 && row[0] === i && row.every(Number.isFinite) && row[1] > 0)
    && rows.some(row => row[3] > 0), 'Invalid or combat-free capture');
  const seconds = rows.reduce((sum, row) => sum + row[1], 0) / 1000;
  assert(Math.abs(seconds - 90) < .5, 'Incomplete capture');
  const sorted = rows.map(row => row[1]).sort((a, b) => a - b);
  const percentile = p => sorted[Math.ceil(sorted.length * p) - 1];
  const png = fs.readFileSync(path.join(root, id, 'screenshot.png'));
  assert(png.toString('ascii', 12, 16) === 'IHDR' && png.readUInt32BE(16) === 1920 && png.readUInt32BE(20) === 1080, 'Invalid PNG');
  return { id, ...result, frames: rows.length, seconds, frameMs: {median: percentile(.5), p95: percentile(.95), p99: percentile(.99), max: sorted.at(-1)},
    overBudget: rows.filter(row => row[1] > 1000 / 60).length, gcAvailable: rows.every(row => row[5] >= 0) };
});
assert(runs.every(run => run.quality === runs[0].quality && run.vSync === runs[0].vSync && run.frameCap === runs[0].frameCap
  && run.graphicsApi === runs[0].graphicsApi && run.cpu === runs[0].cpu && run.gpu === runs[0].gpu), 'Settings varied');
const pairs = [0, 2].map(index => {
  const before = runs[index], after = runs[index + 1];
  const p95Ratio = after.frameMs.p95 / before.frameMs.p95;
  return { baseline: before.id, candidate: after.id, p95Ratio, p95IncreasePercent: (p95Ratio - 1) * 100, proposedTenPercentGatePass: p95Ratio <= 1.1 };
});
const summary = {scope: 'E2 A/B, seed1 natural first night; does not close G5 or certify eight-light Player load', warmupSeconds: 30, captureSeconds: 90,
  frameBudgetMs: 1000 / 60, pairs, runs};
fs.writeFileSync(destination, JSON.stringify(summary, null, 2) + '\n');
console.log(JSON.stringify({pairs, runs: runs.map(run => ({id: run.id, frameMs: run.frameMs, overBudget: run.overBudget, lights: run.peakLocalLights}))}, null, 2));
