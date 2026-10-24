#!/usr/bin/env node
// Meaningful checks of authored loot, inventory transactions and local sensing.
// This does not replace actual Chromium input/audio or native Unity Play.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {Exploration} from '../Web/kernel.mjs';
import {Pursuit} from '../Web/pursuit.mjs';
import {ItemSystem} from '../Web/items.mjs';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const data=JSON.parse(fs.readFileSync(path.join(root,'Assets/Resources/SchoolMap.json'),'utf8'));
const out=process.env.ESCAPE_RESULTS_DIR||path.join(root,'TestResults','ItemsRelease');
const checks=[],metrics={},start=performance.now();fs.mkdirSync(out,{recursive:true});
const game=new Exploration(data),items=new ItemSystem(game,{seed:1});
const keys=['key-a','key-b','key-c'],parts=['clock-second-hand','clock-minute-hand','clock-body'];
const near=(a,b,tol=1e-6)=>assert(Math.abs(a-b)<tol,`${a} != ${b}`);
const reset=seed=>{game.reset();game.autoStairsEnabled=false;items.reset(seed??1);};
const available=id=>items.worldItems.find(i=>i.itemId===id&&!i.picked);
const collect=id=>{const i=available(id);assert(i,'No available '+id);const r=items._collect(i);assert(r.ok,r.message);return i;};
const warp=(id,x,y)=>{const f=game.spaces.get(id);assert(f);f.isRoom?game.warpRoom(id,x,y):game.warp(f.number,x,y);};
const age=seconds=>{for(let n=0;n<seconds-1e-9;n+=.025)game.step(0,0,false,Math.min(.025,seconds-n));};
function check(name,fn){try{checks.push({name,passed:true,details:fn()});}catch(error){checks.push({name,passed:false,error:error.message,stack:error.stack});}}
const signature=()=>items.worldItems.map(i=>[i.itemId,i.spaceId,i.hidden,i.x,i.y,i.sourceId]);
function approach(item){
 if(item.hidden){const s=items.searchById.get(item.sourceId);assert(s);for(const a of s.approaches){warp(item.spaceId,a.x,a.y);const n=items.nearby();if(n?.sourceId===s.id)return {x:a.x,y:a.y,near:n};}}
 else{warp(item.spaceId,item.x,item.y);const n=items.nearby();if(n?.uid===item.uid)return{x:item.x,y:item.y,near:n};}
 throw Error('Cannot interact with authored placement '+item.uid);
}

check('128 seeded runs keep exactly A/B/C, one per floor, and all thirteen items',()=>{
 const signatures=new Set(),keyPositions=new Set();let ground=0,hidden=0,placements=0;
 for(let seed=1;seed<=128;seed++){
  reset(seed);assert.equal(items.worldItems.length,13);assert.equal(items.inventory.length,0);assert.equal(items.equipped,null);
  const keyItems=items.worldItems.filter(i=>keys.includes(i.itemId));assert.equal(keyItems.length,3);
  assert.deepEqual([...keyItems.map(i=>i.itemId)].sort(),keys);
  const floors=keyItems.map(i=>{const f=game.spaces.get(i.spaceId);return f.isRoom?f.sourceFloor:f.number;});assert.deepEqual(floors.sort(),[1,2,3]);
  assert(keyItems.some(i=>i.hidden)&&keyItems.some(i=>!i.hidden));
  const ids=new Set(items.worldItems.map(i=>i.uid));assert.equal(ids.size,13);
  for(const i of items.worldItems){
   const f=game.spaces.get(i.spaceId);assert(f);assert(!game.blocked(i.displayX,i.displayY,f));
   const nav=items.navigation.get(f.id);assert(nav.available.some(n=>Math.hypot(n.x-i.displayX,n.y-i.displayY)<1.5&&game.segmentClear(n.x,n.y,i.displayX,i.displayY,f)));
   if(i.hidden){const s=items.searchById.get(i.sourceId);assert.equal(s.itemUid,i.uid);assert(s.approaches.length>0);hidden++;}else ground++;
   placements++;
  }
  signatures.add(JSON.stringify(signature()));keyPositions.add(JSON.stringify(keyItems.map(i=>[i.spaceId,i.x,i.y,i.hidden])));
 }
 assert(signatures.size>120);assert(keyPositions.size>100);
 metrics.seededPlacements=placements;metrics.seeds=128;
 return{seeds:128,placements,ground,hidden,uniqueLayouts:signatures.size,uniqueKeyLayouts:keyPositions.size};
});

check('A specified seed reproduces positions; new seeds change positions',()=>{
 reset('repeatable school');const a=signature();reset('repeatable school');assert.deepEqual(signature(),a);
 reset('different school');assert.notDeepEqual(signature(),a);return{deterministicSeed:true,differentSeedChangesLayout:true};
});

check('Sampled seeded keys can be reached by production movement and E-style interaction',()=>{
 let reached=0;
 for(let seed=1;seed<=12;seed++){
  reset(seed);
  for(const id of keys){
   const item=available(id),f=game.spaces.get(item.spaceId),a=approach(item),nav=items.navigation.get(f.id);
   const n=nav.available.filter(n=>game.segmentClear(n.x,n.y,a.x,a.y,f)).sort((u,v)=>Math.hypot(u.x-a.x,u.y-a.y)-Math.hypot(v.x-a.x,v.y-a.y))[0];assert(n);
   warp(f.id,f.spawnX,f.spawnY);assert(game.walkTo(n.x,n.y),'Production route to key approach failed');
   assert(game.segmentClear(game.x,game.y,a.x,a.y));warp(f.id,a.x,a.y);age(.35);
   const r=items.interact();assert(r.ok&&items.inventory.some(i=>i.uid===item.uid),r.message);reached++;
  }
 }
 metrics.productionKeyRoutes=reached;return{runs:12,realMovementRoutes:reached};
});

check('Desks, cabinets, lockers, teacher desks and library shelves are searchable',()=>{
 const kinds=new Set(items.searchTemplate.map(s=>s.kind));
 assert(kinds.has('student-desk'));assert(kinds.has('teacher-desk'));assert(kinds.has('locker-bank'));assert(kinds.has('shelf-body'));assert([...kinds].some(k=>k.includes('cabinet')));
 return{searchableCount:items.searchTemplate.length,kinds:[...kinds]};
});

check('Nine-slot bag rejects pickup without destroying the world item',()=>{
 reset(4);const list=items.worldItems.filter(i=>!parts.includes(i.itemId));for(const i of list.slice(0,9))assert(items._collect(i).ok);
 const tenth=list[9],before=items.inventory.map(i=>i.uid);assert.equal(items.inventory.length,9);
 const r=items._collect(tenth);assert(!r.ok);assert(!tenth.picked);assert.deepEqual(items.inventory.map(i=>i.uid),before);
 return{capacity:items.capacity,length:items.inventory.length,retainedWorldUid:tenth.uid};
});

check('Full-bag furniture discovery remains recoverable and repeated searches do not duplicate it',()=>{
 reset(9);const target=items.worldItems.find(i=>i.hidden&&!parts.includes(i.itemId));assert(target);
 const fillers=items.worldItems.filter(i=>i.uid!==target.uid&&!parts.includes(i.itemId)).slice(0,9);assert.equal(fillers.length,9);for(const i of fillers)assert(items._collect(i).ok);
 approach(target);age(.35);const full=items.interact();assert(!full.ok&&target.revealed&&!target.picked);const source=items.searchById.get(target.sourceId);assert(source.searched);
 assert(items.drop(0).ok);age(.35);approach(target);const pickup=items.interact();assert(pickup.ok&&target.picked);assert.equal(items.inventory.filter(i=>i.uid===target.uid).length,1);
 age(.35);approach({...target,hidden:true});items.interact();assert.equal(items.inventory.filter(i=>i.uid===target.uid).length,1);
 return{first:full.message,second:pickup.message,uniqueTargetCopies:1};
});

check('Equipping moves the watch into one separate slot; full-bag unequip is atomic',()=>{
 reset(3);const watch=collect('smartwatch');assert(items.equip(0).ok);assert.equal(items.equipped.uid,watch.uid);assert.equal(items.inventory.length,0);
 for(const i of items.worldItems.filter(i=>!i.picked&&!parts.includes(i.itemId)))assert(items._collect(i).ok);
 assert.equal(items.inventory.length,9);const r=items.unequip();assert(!r.ok);assert.equal(items.equipped.uid,watch.uid);assert.equal(items.inventory.length,9);
 assert(!items.equip(0).ok);assert.equal(items.equipped.uid,watch.uid);
 assert(items.drop(0).ok);assert(items.unequip().ok);assert.equal(items.equipped,null);assert.equal(items.inventory.length,9);return{watchUid:watch.uid,separateWearCapacity:items.maxEquipped};
});

check('Dropping and picking up preserves the same item UID without duplication',()=>{
 reset(5);const key=collect('key-a');warp('floor-1',data.spawnX,data.spawnY);assert(items.drop(0).ok);
 const dropped=items.worldItems.find(i=>i.uid===key.uid);assert(!dropped.picked&&!dropped.hidden);assert(!game.blocked(dropped.x,dropped.y));age(.35);
 assert(items.interact().ok);assert.equal(items.inventory[0].uid,key.uid);assert.equal(items.worldItems.filter(i=>i.uid===key.uid).length,1);return{uid:key.uid};
});

check('Hidden searches are single use as loot sources and publish a localized sound event',()=>{
 reset(12);const item=items.worldItems.find(i=>i.hidden);approach(item);age(.35);assert(items.interact().ok);assert(item.picked);
 const noise=game.events.find(e=>e.type==='search');assert(noise&&noise.spaceId===item.spaceId&&Number.isFinite(noise.x)&&Number.isFinite(noise.y));
 age(.35);approach({...item,hidden:true});assert(items.interact().ok);assert.equal(items.inventory.filter(i=>i.uid===item.uid).length,1);
 return{sourceId:item.sourceId,event:noise};
});

check('All three clock parts auto-craft one compass and consume all three parts',()=>{
 reset(15);const consumed=parts.map(collect);assert.equal(items.inventory.length,1);assert.equal(items.inventory[0].itemId,'compass');assert(parts.every(id=>!items.inventory.some(i=>i.itemId===id)));
 assert(consumed.every(i=>items.consumed.some(c=>c.uid===i.uid)));const c=items.compass();assert(c.active&&keys.includes(c.targetItemId));assert(Number.isFinite(c.angle));
 assert(c.hops===0?c.via==='key':['door','stairs'].includes(c.via));return c;
});

check('The final clock part can craft when the bag is full without a lost item',()=>{
 reset(20);collect(parts[0]);collect(parts[1]);
 for(const i of items.worldItems.filter(i=>!i.picked&&!parts.includes(i.itemId)).slice(0,7))assert(items._collect(i).ok);
 assert.equal(items.inventory.length,9);const r=items._collect(available(parts[2]));assert(r.ok);assert.equal(items.inventory.length,8);assert.equal(items.inventory.filter(i=>i.itemId==='compass').length,1);return{before:9,after:8};
});

check('Compass points to a real remaining key and expires only on its target',()=>{
 reset(25);parts.forEach(collect);const compass=items.inventory[0],target=items.compass();
 const other=keys.find(id=>id!==target.targetItemId);collect(other);assert(items.inventory.some(i=>i.uid===compass.uid));
 collect(target.targetItemId);assert(!items.inventory.some(i=>i.uid===compass.uid));assert(items.consumed.some(i=>i.uid===compass.uid));assert(!items.compass().active);
 return{targetUid:target.targetKeyUid,targetItemId:target.targetItemId};
});

check('A dropped compass keeps its target, and target collection also consumes the dropped compass',()=>{
 reset(26);parts.forEach(collect);const compass=items.inventory[0],target=items.compass();assert(items.drop(0).ok);
 const dropped=items.worldItems.find(i=>i.uid===compass.uid);assert.equal(dropped.targetKeyUid,target.targetKeyUid);
 collect(target.targetItemId);assert(dropped.picked&&dropped.consumed);assert(!items.compass().active);return{uid:compass.uid,target:target.targetKeyUid};
});

check('Vitamin water changes actual movement by thirty percent for two seconds only',()=>{
 reset(30);collect('vitamin-water');warp('floor-1',24,9);const use=items.use(0);assert(use.ok);near(game.speedBoostUntil-game.time,2);assert.equal(items.inventory.length,0);
 game.step(1,0,false,.05);near(game.speed,data.walkSpeed*1.3);const boostedWalk=game.speed;
 game.step(1,1,true,.05);near(game.speed,data.runSpeed*1.3);const boostedDiagonalRun=game.speed;
 age(2);game.step(1,0,false,.05);near(game.speed,data.walkSpeed);return{boostedWalk,boostedDiagonalRun,normalAfterExpiry:game.speed};
});

check('Repeated vitamin water refreshes the duration and does not multiply the speed',()=>{
 reset(31);collect('vitamin-water');collect('vitamin-water');assert(items.use(0).ok);age(.3);assert(items.use(0).ok);near(game.speedBoostUntil-game.time,2);warp('floor-1',24,9);game.step(1,0,false,.05);near(game.speed,data.walkSpeed*1.3);return{speed:game.speed};
});

check('Chocolate immediately fully restores stamina and releases exhaustion',()=>{
 reset(32);collect('chocolate-bar');game.stamina=0;game.exhausted=true;game.lastSprintTime=game.time;assert(items.use(0).ok);
 near(game.stamina,100);assert(!game.exhausted);warp('floor-1',24,9);game.step(1,0,true,.05);assert(game.isRunning);return{runningAfterUse:game.isRunning};
});

check('Keys cannot be used away from the gate or escape with a missing key',()=>{
 reset(35);collect('key-a');warp(items.escapeGate.spaceId,items.escapeGate.x,items.escapeGate.y);const missing=items.tryEscape();assert(!missing.ok);assert.deepEqual(missing.missing,['B','C']);assert(items.inventory.some(i=>i.itemId==='key-a'));
 collect('key-b');collect('key-c');const p=game.spaces.get('floor-2');warp(p.id,p.spawnX,p.spawnY);assert(!items.tryEscape().ok);assert(items.hasAllKeys()&&!game.won);
 return{missing:missing.missing,wrongFloorRefused:true};
});

check('All A/B/C keys open the gate atomically and the won game stays frozen',()=>{
 reset(36);keys.forEach(collect);warp(items.escapeGate.spaceId,items.escapeGate.x,items.escapeGate.y);const r=items.tryEscape();assert(r.ok&&game.won&&items.escaped);assert(!items.inventory.some(i=>keys.includes(i.itemId)));
 const p={x:game.x,y:game.y};game.step(1,1,true,.05);near(game.x,p.x);near(game.y,p.y);assert(!items.interact().ok);assert(!items.tryEscape().ok);return{won:game.won,keysConsumed:3};
});

check('Restart replaces run items, clears worn inventory and restores a playable game',()=>{
 reset(40);collect('smartwatch');items.equip(0);collect('vitamin-water');items.use(0);game.won=true;
 const old=items.seed;game.reset();items.reset(41);assert(!game.won&&!game.caught);assert.equal(items.inventory.length,0);assert.equal(items.equipped,null);assert.equal(game.speedBoostUntil,0);assert.equal(items.worldItems.length,13);assert.notEqual(items.seed,old);return{oldSeed:old,newSeed:items.seed};
});

const pursuer=new Pursuit(game);
check('Random designated monster spawns cover multiple safe authored places and are seeded',()=>{
 const spawns=new Set(),floors=new Set();reset(1);
 for(let seed=1;seed<=96;seed++){
  pursuer.reset(seed);assert(pursuer.spawnCandidates.some(s=>s.id===pursuer.initial.spawnId));const f=game.spaces.get(pursuer.spaceId);assert(!game.blocked(pursuer.x,pursuer.y,f,pursuer.radius));
  if(pursuer.spaceId===game.spaceId)assert(pursuer.initial.distanceFromPlayer>=15&&pursuer.initial.pathDistanceFromPlayer>=15);
  assert(!(f.stairs||[]).some(s=>Math.hypot(s.x-pursuer.x,s.y-pursuer.y)*data.tileSize<3.2));spawns.add(pursuer.initial.spawnId);floors.add(f.number);
 }
 assert(spawns.size>=3&&floors.size===3);pursuer.reset('same');const a=pursuer.initial;pursuer.reset('same');assert.deepEqual(pursuer.initial,a);return{runs:96,spawnCount:spawns.size,floors:[...floors]};
});

check('Development OFF persists through reset and prevents movement or capture',()=>{
 reset(1);pursuer.setEnabled(false);pursuer.reset(42);assert.equal(pursuer.enabled,false);const p={x:pursuer.x,y:pursuer.y,id:pursuer.spaceId};
 for(let t=0;t<10;t+=.05){game.step(0,0,false,.05);pursuer.step(.05);}near(pursuer.x,p.x);near(pursuer.y,p.y);assert.equal(pursuer.spaceId,p.id);assert(!game.caught);
 pursuer.setEnabled(true);assert(pursuer.enabled&&pursuer.state==='patrol'&&!pursuer.lastKnown);return{persists:true};
});

check('Rear and wall-occluded players do not trigger sight; a front encounter does',()=>{
 reset(1);pursuer.setEnabled(true);warp('floor-1',24,9);pursuer.place('floor-1',24,13,'patrol');pursuer.direction={x:0,y:1};assert(!pursuer.canSeePlayer());
 pursuer.direction={x:0,y:-1};assert(pursuer.canSeePlayer());pursuer.step(.025);assert.equal(pursuer.state,'chase');
 const f=game.spaces.get('floor-1'),nodes=pursuer.grid(f).nodes;let wall;
 outer:for(const a of nodes)for(const [dx,dy]of[[2,0],[0,2],[3,0],[0,3]]){const x=a.x+dx,y=a.y+dy;if(!game.blocked(x,y,f)&&Math.hypot(dx,dy)*data.tileSize<11&&!game.segmentClear(a.x,a.y,x,y,f,.025)){wall={a,x,y};break outer;}}
 assert(wall);warp(f.id,wall.x,wall.y);pursuer.place(f.id,wall.a.x,wall.a.y,'patrol');pursuer.direction={x:wall.x-wall.a.x,y:wall.y-wall.a.y};assert(!pursuer.canSeePlayer());return{wall};
});

check('Noise investigation remembers the sound snapshot rather than hidden live player coordinates',()=>{
 reset(1);pursuer.setEnabled(true);warp('floor-1',24,9);pursuer.place('floor-1',24,13,'patrol');pursuer.direction={x:0,y:1};
 game.emit('noise',{spaceId:'floor-1',x:24,y:10,radius:8,kind:'run'});game.step(0,0,false,.025);pursuer.step(.025);
 assert.equal(pursuer.state,'investigate');assert.deepEqual(pursuer.lastKnown,{spaceId:'floor-1',x:24,y:10});
 const hidden=game.spaces.get('floor-2');warp(hidden.id,hidden.spawnX,hidden.spawnY);game.step(0,0,false,.025);pursuer.step(.025);assert.deepEqual(pursuer.lastKnown,{spaceId:'floor-1',x:24,y:10});
 const before={...pursuer.lastKnown};game.emit('noise',{spaceId:'floor-2',x:24,y:13,radius:20,kind:'run'});game.step(0,0,false,.025);pursuer.step(.025);assert.deepEqual(pursuer.lastKnown,before);return{remembered:pursuer.lastKnown};
});

check('After loss of local evidence the monster searches and gives up within the configured memory',()=>{
 reset(1);pursuer.setEnabled(true);warp('floor-1',24,9);pursuer.place('floor-1',24,13,'chase');pursuer.direction={x:0,y:-1};pursuer.step(.025);
 const last={...pursuer.lastKnown};const f=game.spaces.get('floor-2');warp(f.id,f.spawnX,f.spawnY);const begin=game.time;let searched=false;
 for(let t=0;t<13;t+=.025){game.step(0,0,false,.025);pursuer.step(.025);searched||=pursuer.state==='search';if(pursuer.state==='patrol')break;}
 assert(searched);assert.equal(pursuer.state,'patrol');assert.equal(pursuer.lastKnown,null);assert(game.time-begin<=12.1);return{lastEvidence:last,gaveUpAfter:game.time-begin,reason:pursuer.lostReason};
});

check('Independent patrol goals are unchanged when only the distant unseen player moves',()=>{
 reset(1);pursuer.setEnabled(true);pursuer.reset(2026);const f=game.spaces.get(pursuer.spaceId),other=data.floors.find(f=>f.id!==pursuer.spaceId);warp(other.id,other.spawnX,other.spawnY);
 const goal={...pursuer.patrolTarget()};game.x+=.1;game.y+=.1;assert.deepEqual(pursuer.patrolTarget(),goal);assert.equal(pursuer.lastKnown,null);return{enemySpace:f.id,playerSpace:other.id,goal};
});

const report={kind:'SchoolEscape item transactions, seeded reachable loot and local pursuit kernel; NOT Unity Play',utc:new Date().toISOString(),passed:checks.filter(c=>c.passed).length,failed:checks.filter(c=>!c.passed),checks,metrics,durationMs:performance.now()-start,sources:{}};
for(const p of ['Assets/Resources/SchoolMap.json','Web/kernel.mjs','Web/items.mjs','Web/pursuit.mjs'])report.sources[p]=crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex');
fs.writeFileSync(path.join(out,'items-kernel.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({passed:report.passed,failed:report.failed.map(c=>({name:c.name,error:c.error})),metrics,durationMs:report.durationMs},null,2));
if(report.failed.length)process.exitCode=1;
