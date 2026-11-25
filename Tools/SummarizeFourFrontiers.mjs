import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';

const folder='Docs/Modules/ProceduralGeneration/Evidence/FourFrontiers';
const read=name=>JSON.parse(fs.readFileSync(`${folder}/${name}`,'utf8'));
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const stats=values=>{
  const sorted=[...values].sort((a,b)=>a-b), n=sorted.length;
  return {n,median:n%2?sorted[(n-1)/2]:(sorted[n/2-1]+sorted[n/2])/2,
    p95:sorted[Math.ceil(n*.95)-1],max:sorted[n-1]};
};
const groups={};
for(const mode of ['baseline','gameplay']){
  const first=[],reload=[];
  for(const seed of [0,1,1543]){
    const initial=read(`Player-${seed}-${mode}-first.json`), result=read(`Player-${seed}-${mode}.json`);
    if(!result.passed)throw new Error(`Failed ${mode}/${seed}`);
    first.push(initial.first);reload.push(result.reloaded);
  }
  const summary=rows=>Object.fromEntries(['coreMs','terrainMs','resourceMs','navigationMs','initializeMs','bindMs','sceneToReadyMs']
    .map(key=>[key,stats(rows.map(row=>row[key]))]));
  groups[mode]={firstMatch:summary(first),reload:summary(reload),first,reloaded:reload};
}
const build=read('Build.json');
const sourceFiles=[];
function collect(directory){
  for(const item of fs.readdirSync(directory,{withFileTypes:true})){
    const file=`${directory}/${item.name}`;
    if(item.isDirectory())collect(file);
    else if(/\.(cs|asset|asmdef|shader|json|meta)$/.test(file))sourceFiles.push(file);
  }
}
for(const directory of ['Assets/Game/ProceduralGeneration','Assets/Game/SaveLoad','Assets/Game/DOTS/Integration'])collect(directory);
sourceFiles.push('Assets/Scenes/ProceduralGame.unity','Assets/Game/Scripts/UI/GameFlowMenu.cs','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','ProjectSettings/ProjectSettings.asset','ProjectSettings/GraphicsSettings.asset','ProjectSettings/QualitySettings.asset','ProjectSettings/TimeManager.asset','ProjectSettings/TagManager.asset');
const hashes=Object.fromEntries(sourceFiles.sort().map(file=>[file,hash(file)]));
const managed=path.join(path.dirname(build.output),'RTSgame2026_Data/Managed/Assembly-CSharp.dll');
const result={build,executableSha256:hash(build.output),assemblyCSharpSha256:hash(managed),
  contentFingerprint:fs.readFileSync('Assets/Game/SaveLoad/Resources/ProceduralSaveCatalog.asset','utf8').match(/m_ContentHash: (\w+)/)?.[1],
  originalProjectRunInBackground: false, smokeRuntimeRunInBackground: true,
  resolution:[1280,720],mode:'Windows Release Mono; batchmode; hidden window; Editor co-resident during some runs',
  latencyScope:'LoadScene call to root Ready, excludes application startup. Three seeds per preset, one initial match and one reload each; p95 nearest-rank equals max for n=3. No FPS/GC/memory claim.',
  groups,sourceHashes:hashes};
fs.writeFileSync(`${folder}/player-measurements.json`,JSON.stringify(result,null,2));
console.log(JSON.stringify({v3:groups.baseline.firstMatch.sceneToReadyMs,v4:groups.gameplay.firstMatch.sceneToReadyMs,
  v3Reload:groups.baseline.reload.sceneToReadyMs,v4Reload:groups.gameplay.reload.sceneToReadyMs}));
