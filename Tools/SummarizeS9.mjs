// Node standard library only: validate and summarize a progression smoke CSV/result.
import fs from 'node:fs';
import path from 'node:path';

const [directory, destination] = process.argv.slice(2);
if (!directory || !destination) throw new Error('Usage: node Tools/SummarizeS9.mjs <capture-directory> <summary.json>');
const result = JSON.parse(fs.readFileSync(path.join(directory, 'result.json'), 'utf8'));
if (result.errors.length || !result.offscreenRender || result.seed !== 1 || result.generatorVersion !== 3 || result.spawned < 1)
  throw new Error('Capture did not validate progression, rendering, seed or combat.');
for (const name of ['sceneToReadyMs','generatorAwakeToReadyMs','coreMs','terrainMs','resourceMs','navigationMs','initializeMs','bindMs'])
  if (!(result[name] > 0) || !Number.isFinite(result[name])) throw new Error('Invalid timing: ' + name);
const csv = fs.readFileSync(result.csv, 'utf8');
const rows = csv.split(/\r?\n/).filter(line => /^\d+,/.test(line)).map(line => line.split(',').map(Number));
if (rows.length < 100 || rows.some(row => row.length !== 8 || row.some(value => !Number.isFinite(value)) || row[1] <= 0))
  throw new Error('Too few or malformed frame samples.');
const stats = values => {
  const sorted = [...values].sort((a,b) => a-b);
  const percentile = p => sorted[Math.max(0,Math.ceil(sorted.length*p)-1)];
  return {median:percentile(.5),p95:percentile(.95),max:sorted.at(-1),mean:values.reduce((a,b)=>a+b,0)/values.length};
};
const enemies = rows.map(row=>row[3]);
if (Math.max(...enemies) < 1) throw new Error('No enemies were present during the sampled frames.');
const ticks = new Map(rows.map(row=>[row[7],row[2]]));
const gcAvailable = rows.every(row=>row[5]>=0);
const summary = {
  ...result,
  header:csv.split(/\r?\n/).filter(line=>line.startsWith('#')),
  samples:rows.length,requestedWarmupSeconds:30,requestedCaptureSeconds:90,
  sampledFrameSeconds:rows.reduce((sum,row)=>sum+row[1],0)/1000,
  hitRecorderCapacity:rows.length===36000,
  frameMs:stats(rows.map(row=>row[1])),
  sampledLastSwarmTickMs:stats([...ticks.values()]),sampledSwarmTickIds:ticks.size,
  enemyMin:Math.min(...enemies),enemyMax:Math.max(...enemies),projectileMax:Math.max(...rows.map(row=>row[4])),
  gcCounterAvailable:gcAvailable,gcBytesPerFrame:gcAvailable?stats(rows.map(row=>row[5])):null,
  unityAllocatedBytesFirst:rows[0][6],unityAllocatedBytesLast:rows.at(-1)[6],
  limitations:'Development Player, offscreen world render with explicit render request; overlay UI GPU cost is not established by the PNG. Swarm tick samples exclude gameplay ECS and are not total frame CPU. Percentiles use nearest-rank; a single scene startup is not a startup distribution.'
};
fs.writeFileSync(destination,JSON.stringify(summary,null,2)+'\n');
process.stdout.write(JSON.stringify({samples:summary.samples,seconds:summary.sampledFrameSeconds,frameMs:summary.frameMs,enemies:summary.enemyMax,projectiles:summary.projectileMax,sceneToReadyMs:summary.sceneToReadyMs,gc:summary.gcBytesPerFrame,capacity:summary.hitRecorderCapacity},null,2));
