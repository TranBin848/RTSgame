// Derive G2 from the frozen G01 policy; keep the source used for every run beside its evidence.
import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const dir = 'Docs/Modules/Progression/Evidence/G2-2026-11-19';
fs.mkdirSync(dir, {recursive: true});
let code = fs.readFileSync('Tools/PlaytestG01.cs', 'utf8');
const replace = (from, to) => { assert(code.includes(from), from); code = code.replace(from, to); };
replace('public static class G01Playtest', 'public static class G2Playtest');
replace('var map=Find<TilemapManager>();var generator=Find<ProceduralMapAuthoring>();var hall=Find<TownHall>();',
  'var map=Find<TilemapManager>();var generator=Find<ProceduralMapAuthoring>();var hall=Find<TownHall>();\n        var hallEntity=hall.DotsEntity;var hallPosition=hall.transform.position;int expectedHP=hall.MaxHealth;');
code = code.replaceAll('hall.DotsEntity', 'hallEntity');
replace('var hallEntity=hallEntity;', 'var hallEntity=hall.DotsEntity;');
code = code.replaceAll('hall.transform.position', 'hallPosition');
replace('var hallPosition=hallPosition;', 'var hallPosition=hall.transform.position;');
// Observation must also work after the last Hall dies and the view is unbound.
code = code.replaceAll('em.GetComponentData<CombatBody>(hallEntity).Position', '(float2)(Vector2)hallPosition');
replace('string dir="Docs/Modules/Progression/Evidence/G01-2026-11-18";', `string dir="${dir}";`);
replace('string runId=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+policy+"-"+generator.GeneratedMap.Seed;',
  'string runId=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-hp"+expectedHP+"-"+policy+"-"+generator.GeneratedMap.Seed;');
replace('System.IO.File.ReadAllText(dir+"/config-id.txt")', 'System.IO.File.ReadAllText(dir+"/config-hp"+expectedHP+".txt")');
replace('schema=G01-v3;', 'schema=G2-v1;hallMax={expectedHP};');
replace('if(hall.MaxHealth!=10000||hall.CurrentHealth!=10000||em.GetComponentData<CombatBody>(hallEntity).Health!=10000)',
  'if((expectedHP!=10000&&expectedHP!=1600&&expectedHP!=1200)||hall.CurrentHealth!=expectedHP||em.GetComponentData<CombatBody>(hallEntity).Health!=expectedHP)');
replace('l=>!l.StartsWith("actor ")&&!l.StartsWith("resolved=")',
  'l=>l.StartsWith("schema=")||l.StartsWith("t=")||l.StartsWith("ledger=")||l.StartsWith("outcome=")');
const codePath = `${dir}/PlaytestG2-derived.cs.txt`;
if (fs.existsSync(codePath)) assert.equal(fs.readFileSync(codePath, 'utf8'), code, 'Do not overwrite an earlier G2 harness');
else fs.writeFileSync(codePath, code);
const sha = data => crypto.createHash('sha256').update(data).digest('hex').toUpperCase();
const g01 = JSON.parse(fs.readFileSync('Docs/Modules/Progression/Evidence/G01-2026-11-18/fingerprint-v3.json', 'utf8'));
const snapshot = g01.map(row => ({path: row.path, sha256: sha(fs.readFileSync(row.path))}));
const snapshotPath = `${dir}/fingerprint.json`;
if (fs.existsSync(snapshotPath)) assert.deepEqual(JSON.parse(fs.readFileSync(snapshotPath, 'utf8')), snapshot);
else fs.writeFileSync(snapshotPath, JSON.stringify(snapshot, null, 2));
for (const hp of [10000, 1600, 1200]) {
  const id = sha(JSON.stringify({snapshot, derivedHarness: sha(code), hp, seed: 1, waveSeed: 8001}));
  fs.writeFileSync(`${dir}/config-hp${hp}.txt`, id);
}
console.log(JSON.stringify({status: 'PASS', codePath, fingerprintFiles: snapshot.length, hp: [10000,1600,1200]}));
