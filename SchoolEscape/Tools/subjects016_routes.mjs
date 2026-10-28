import fs from 'node:fs';
import {Exploration} from '../Web/kernel.mjs';
import {ItemSystem} from '../Web/items.mjs';
import {prepareRun} from '../Web/run-rules.mjs';
const data=JSON.parse(fs.readFileSync(new URL('../Assets/Resources/SchoolMap.json',import.meta.url))),failures=[],checks=[];
function check(label,ok,detail){if(!ok)failures.push({label,detail});else checks.push(label)}
for(const seed of [1,15,2026]){
 const g=new Exploration(prepareRun(structuredClone(data),seed)),i=new ItemSystem(g,{seed});
 for(const [id,f] of g.spaces){
  const nav=i.navigation.get(id),reachable=(x,y)=>!g.blocked(x,y,f)&&nav.available.some(n=>Math.hypot(n.x-x,n.y-y)<1.5&&g.segmentClear(n.x,n.y,x,y,f));
  for(const z of [...f.landmarks||[],...f.interiorZones||[]])check(`${seed} zone ${id} ${z.label}`,reachable(z.x,z.y),z);
  check(`${seed} spawn ${id}`,!g.blocked(f.spawnX,f.spawnY,f),{x:f.spawnX,y:f.spawnY});
  for(const edge of i.connections.get(id)||[]){check(`${seed} route ${id} -> ${edge.toId}`,reachable(edge.x,edge.y),edge)}
  for(const stair of f.stairs){const target=g.spaces.get(stair.targetId||`floor-${stair.target}`);check(`${seed} stair landing ${id}`,target&&!g.blocked(stair.arrivalX,stair.arrivalY,target),stair)}
  for(const door of f.portals||[]){if(door.locked)continue;const t=g.spaces.get(door.targetId);check(`${seed} door landing ${id} -> ${door.targetId}`,t&&!g.blocked(door.arrivalX,door.arrivalY,t),door)}
  if(f.kind==='staff'){
   check(`${seed} 10 teacher seats ${id}`,f.teacherSeats?.length===10);
   for(const s of f.teacherSeats||[])check(`${seed} teacher desk reachable ${id} ${s.role}`,i.searchables.some(t=>t.fixtureId===s.fixtureId),s);
  }
 }
 const ids=i.searchables.map(s=>s.id);check(`${seed} unique searchable IDs`,new Set(ids).size===ids.length);
 for(let grade=1;grade<=3;grade++){const f=g.data.floors[grade];for(let n=1;n<=6;n++)check(`${seed} grade ${grade} class ${n}`,f.rooms.some(r=>r.label===`${grade}학년 ${n}반`))}
 check(`${seed} only two empty rooms`,g.data.floors.flatMap(f=>f.rooms).filter(r=>r.label.startsWith('빈교실')).length===2);
 check(`${seed} links remain floor 2`,g.data.floors.filter(f=>f.portals?.some(p=>['annex-2','west-2'].includes(p.targetId))).every(f=>f.number===2));
}
const report={ok:!failures.length,checks:checks.length,failures};fs.writeFileSync(new URL('../Docs/Evidence/subjects016-routes.json',import.meta.url),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));if(failures.length)process.exitCode=1;
