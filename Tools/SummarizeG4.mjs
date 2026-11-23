import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';

const [folder, output] = process.argv.slice(2);
assert(folder && output, 'Usage: node Tools/SummarizeG4.mjs <capture-directory> <summary.json>');
const load = name => fs.readFileSync(path.join(folder, name), 'utf8');
const result = JSON.parse(load('result.json'));
assert(result.errors.length === 0 && result.hallMaxHealth === 1200 && result.waveSeed === 8001
  && result.seed === 1 && result.generatorVersion === 3 && result.workers === 4 && result.spawned > 0 && result.offscreenRender,
  'Unexpected config, errors or missing gameplay');
const csv = load(path.basename(result.csv));
const rows = csv.split(/\r?\n/).filter(line => /^\d+,/.test(line)).map(line => line.split(',').map(Number));
assert(rows.length > 100 && rows.length < 36000
  && rows.every((row, i) => row.length === 8 && row[0] === i && row.every(Number.isFinite) && row[1] > 0 && row[5] >= 0),
  'Incomplete capture or invalid frame/GC data');
const seconds = rows.reduce((sum, row) => sum + row[1], 0) / 1000;
assert(Math.abs(seconds - 113) < 0.5 && Math.max(...rows.map(row => row[3])) > 0, 'Capture missed the requested window or combat');
const stats = values => {
  const sorted = [...values].sort((a, b) => a - b);
  const percentile = p => sorted[Math.ceil(sorted.length * p) - 1];
  return {median: percentile(.5), p95: percentile(.95), p99: percentile(.99), max: sorted.at(-1)};
};
const top = column => [...rows].sort((a, b) => b[column] - a[column]).slice(0, 8)
  .map(([index, frameMs, tickMs, enemies, projectiles, gcBytes, unityAllocatedBytes, tick]) =>
    ({index, frameMs, tickMs, enemies, projectiles, gcBytes, unityAllocatedBytes, tick}));
const summary = {
  ...result, header: csv.split(/\r?\n/).filter(line => line.startsWith('#')),
  frames: rows.length, seconds, frameMs: stats(rows.map(row => row[1])), gcBytes: stats(rows.map(row => row[5])),
  allocatedBytes: {first: rows[0][6], last: rows.at(-1)[6]},
  topFrameSpikes: top(1), topGcSpikes: top(5)
};
fs.writeFileSync(output, JSON.stringify(summary, null, 2) + '\n');
console.log(JSON.stringify({frames: summary.frames, seconds, frameMs: summary.frameMs, gcBytes: summary.gcBytes, spawned: result.spawned}));
