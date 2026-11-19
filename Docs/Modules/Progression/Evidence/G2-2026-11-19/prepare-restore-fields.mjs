// Read YAML only to recover the precise pre-save fields; all writes to the scene use Unity APIs.
import fs from 'node:fs';
import assert from 'node:assert/strict';
const before=fs.readFileSync('.utmp/G2-ProceduralGame-before.unity','utf8');
const after=fs.readFileSync('Assets/Scenes/ProceduralGame.unity','utf8');
const blocks=text=>new Map([...text.matchAll(/--- !u!(\d+) &(\d+)[^\n]*\n([\s\S]*?)(?=--- !u!|$)/g)].map(m=>[m[2],{type:m[1],text:m[3]}]));
const oldBlocks=blocks(before),newBlocks=blocks(after);
const request={mode:'restore-fields',rects:[],animations:[]};
for(const [id,old] of oldBlocks){
 const now=newBlocks.get(id);assert(now);
 if(old.type==='224'){
  const read=(text,key)=>{const m=text.match(new RegExp('  '+key+': \\{x: ([^,]+), y: ([^}]+)\\}'));assert(m);return{x:Number(m[1]),y:Number(m[2])};};
  const a={id:Number(id),anchorMin:read(old.text,'m_AnchorMin'),anchorMax:read(old.text,'m_AnchorMax'),anchoredPosition:read(old.text,'m_AnchoredPosition')};
  const b={id:Number(id),anchorMin:read(now.text,'m_AnchorMin'),anchorMax:read(now.text,'m_AnchorMax'),anchoredPosition:read(now.text,'m_AnchoredPosition')};
  if(JSON.stringify(a)!==JSON.stringify(b))request.rects.push(a);
 }
 if(old.type==='1839735485'){
  const animations=text=>{const section=text.match(/  m_AnimatedTiles:\n([\s\S]*?)(?=\n  \w|$)/)?.[1]??'';return new Map([...section.matchAll(/  - first: \{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}([\s\S]*?)(?=  - first:|$)/g)].map(m=>{const speed=m[4].match(/m_AnimationSpeed: ([\d.]+)/);assert(speed);return[m[1]+','+m[2]+','+m[3],{x:Number(m[1]),y:Number(m[2]),z:Number(m[3]),speed:Number(speed[1])}];}));};
  const a=animations(old.text),b=animations(now.text);
  for(const [key,v]of a){assert(b.has(key));if(v.speed!==b.get(key).speed){assert(Math.abs(v.speed-b.get(key).speed)<.00001);request.animations.push({id:Number(id),...v});}}
 }
}
assert.equal(request.rects.length,3);assert.equal(request.animations.length,20);
const output='Docs/Modules/Progression/Evidence/G2-2026-11-19/restore-fields-request.json';
assert(!fs.existsSync(output),'Do not overwrite restore evidence');
fs.writeFileSync(output,JSON.stringify(request,null,2));
console.log(JSON.stringify({rects:request.rects.length,animations:request.animations.length,output}));
