import fs from 'node:fs';import assert from 'node:assert/strict';
import {Exploration} from '../Web/kernel.mjs';import {ItemSystem} from '../Web/items.mjs';import {Progression} from '../Web/progression.mjs';import {Pursuit} from '../Web/pursuit.mjs';import {SubjectEvents} from '../Web/subject-events.mjs';import {Survival} from '../Web/run-rules.mjs';import {KitchenSequence} from '../Web/kitchen-sequence.mjs';
const data=JSON.parse(fs.readFileSync(new URL('../Assets/Resources/SchoolMap.json',import.meta.url))),report={ok:false,checks:[],errors:[]};
function check(label,value){assert.ok(value,label);report.checks.push(label)}
try{for(const seed of [1,16,2026,541,71,82,93,104,115,126,137,148,159,170,181,192]){
 const g=new Exploration(structuredClone(data)),i=new ItemSystem(g,{seed}),p=new Pursuit(g),pr=new Progression(g,i),s=new SubjectEvents(g,i,p,pr),sv=new Survival(g,i,p,'normal'),k=new KitchenSequence(g,p,sv);
 check(seed+' master occupies one slot',i.inventory.length===1&&i.inventory[0].itemId==='master-key');
 for(const f of g.spaces.values()){const loot=i.worldItems.filter(i=>i.spaceId===f.id&&!i.picked),max=['playground','gym','music'].includes(f.kind)?4:1;check(seed+' loot budget '+f.id,loot.filter(i=>i.hidden).length<=max&&loot.filter(i=>!i.hidden).length<=max)}
 for(const id of ['lab-coat','flashlight'])check(seed+' same-floor '+id,g.spaces.get(i.worldItems.find(v=>v.itemId===id).spaceId).sourceFloor===s.maps.science.sourceFloor);
 for(const l of Object.values(pr.locks).filter(l=>l.classClue)){const c=pr.classes[l.classClue],leader=c.students.find(s=>s.role==='반장');check(seed+' class leader clue stays truthful '+l.id,i.worldItems.find(x=>x.uid===l.rewardUid).sourceId===leader.deskId)}
 // Abstract unlimited-capacity collection verifies dependency order; no master.
 const owned=new Set(),zones=new Set(['start']),opened=new Set(),containers=new Set();let changed=true;
 while(changed){changed=false;for(const item of i.worldItems){if(owned.has(item.itemId)||!zones.has(pr.zone(item.spaceId))||pr.locks[item.spaceId]&&!opened.has(item.spaceId))continue;const cl=pr.containerLocks[item.sourceId];if(cl&&!containers.has(cl.id))continue;owned.add(item.itemId);changed=true;}
  for(const [z,c] of Object.entries(pr.cards))if(owned.has(c.itemId)&&!zones.has(z)){zones.add(z);changed=true;}
  for(const l of [...Object.values(pr.locks),...Object.values(pr.containerLocks)]){const set=l.id.startsWith('container:')?containers:opened;const solved=l.need?owned.has(l.need):[...owned].some(id=>i.catalog[id]?.lockId===l.id);if(solved&&!set.has(l.id)){set.add(l.id);changed=true;}}
 }
 check(seed+' no circular lock dependency',Object.values(pr.locks).every(l=>opened.has(l.id))&&Object.values(pr.cards).every(c=>owned.has(c.itemId)));
 for(const l of [...Object.values(pr.locks),...Object.values(pr.containerLocks)].filter(l=>l.answer)){
  const note=i.worldItems.find(x=>i.catalog[x.itemId]?.lockId===l.id);check(seed+' target-bound physical note '+l.id,!!note&&i.catalog[note.itemId].description.includes(l.answer));i.inventory.push({uid:note.uid,itemId:note.itemId});check(seed+' correct code unlocks '+l.id,pr.unlock(l.id,l.answer).ok);check(seed+' note consumed before entry '+l.id,!i.inventory.some(x=>x.uid===note.uid)&&note.picked);
 }
 g.warpSpace('playground',34,3);i.inventory=[];for(const id of ['key-a','key-b','key-c']){i.inventory.push({uid:id,itemId:id});const result=i.tryEscape();check(seed+' insert '+id,result.ok&&i.insertedKeys.includes(id)&&!i.inventory.some(x=>x.itemId===id)&&!g.won)}check(seed+' separate final escape',i.tryEscape().ok&&g.won);
}
report.ok=true}catch(e){report.errors.push(e.stack)}
fs.writeFileSync(new URL('../Docs/Evidence/patch017-kernel.json',import.meta.url),JSON.stringify(report,null,2));console.log(JSON.stringify({ok:report.ok,checks:report.checks.length,errors:report.errors}));process.exitCode=report.ok?0:1;
