import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const [build, output] = process.argv.slice(2);
assert(build && output && fs.existsSync(path.join(build, 'RTSgame2026.exe')), 'Usage: node Tools/FingerprintE10.mjs <build-directory> <manifest.json>');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
function files(root) {
  const stat = fs.statSync(root);
  return stat.isDirectory() ? fs.readdirSync(root).flatMap(name => files(path.join(root, name))) : [root];
}
function manifest(roots, base = '.') {
  return roots.flatMap(files).map(file => ({path: path.relative(base, file).replaceAll('\\', '/'),
    bytes: fs.statSync(file).size, sha256: hash(fs.readFileSync(file))})).sort((a, b) => a.path.localeCompare(b.path, 'en'));
}
const source = manifest(['Assets', 'ProjectSettings', 'Packages/manifest.json', 'Packages/packages-lock.json', 'Tools']);
const artifact = manifest([build], build);
assert(source.length > 100 && artifact.some(file => file.path === 'RTSgame2026_Data/Managed/Assembly-CSharp.dll'), 'Incomplete manifest');
fs.writeFileSync(output, JSON.stringify({algorithm: 'SHA-256', utc: new Date().toISOString(),
  candidate: hash(JSON.stringify(source)), artifactHash: hash(JSON.stringify(artifact)), source, artifact}, null, 2) + '\n');
console.log(JSON.stringify({sourceFiles: source.length, artifactFiles: artifact.length,
  candidate: hash(JSON.stringify(source)), artifactHash: hash(JSON.stringify(artifact))}));
