#!/usr/bin/env node
// Deterministic checks against the authored SchoolEscape data and live kernel.
// These do not replace Chromium audio/rendering or native Unity validation.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {Exploration,directions} from '../Web/kernel.mjs';
import {Pursuit} from '../Web/pursuit.mjs';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const dataPath=path.join(root,'Assets/Resources/SchoolMap.json');
const data=JSON.parse(fs.readFileSync(dataPath,'utf8'));
const checks=[],metrics={};
const started=performance.now();
const create=()=>{const game=new Exploration(data);return {game,pursuit:new Pursuit(game)}};
const near=(actual,expected,tolerance=1e-7)=>assert(Math.abs(actual-expected)<=tolerance,`${actual} differs from ${expected}`);
function check(name,fn){
  try{const details=fn();checks.push({name,passed:true,details});}
  catch(error){checks.push({name,passed:false,error:error.message,stack:error.stack});}
}
function tick(game,pursuit,dt=.025,ax=0,ay=0,run=false){game.step(ax,ay,run,dt);pursuit.step(dt);}
function sampledClear(game,ax,ay,bx,by,map,radius){
  const count=Math.max(4,Math.ceil(Math.hypot(bx-ax,by-ay)*data.tileSize/.15));
  for(let n=1;n<=count;n++)if(game.blocked(ax+(bx-ax)*n/count,ay+(by-ay)*n/count,map,radius))return false;
  return true;
}

check('Initial placement is reachable and safely separated from the player',()=>{
  const {game:g,pursuit:p}=create(),nav=p.grid(g.map);
  const start=p.closestNode(nav,g.x,g.y),end=p.closestNode(nav,p.x,p.y);
  const routeDistance=p.distances(nav,start).get(end)*data.tileSize;
  assert(p.initial.distanceFromPlayer>=15);assert(routeDistance>=17);
  assert(!g.blocked(p.x,p.y,g.map,p.radius));
  return {spaceId:p.spaceId,x:p.x,y:p.y,straightMetres:p.initial.distanceFromPlayer,routeMetres:routeDistance};
});

check('Walking and sprinting preserve cardinal and diagonal speeds',()=>{
  const results={};
  for(const run of [false,true])for(const diagonal of [false,true]){
    const g=new Exploration(data);g.autoStairsEnabled=false;g.warp(1,24,8);
    const before={x:g.x,y:g.y};for(let n=0;n<20;n++)g.step(1,diagonal?1:0,run,.05);
    const metres=Math.hypot(g.x-before.x,g.y-before.y)*data.tileSize;
    near(metres,run?data.runSpeed:data.walkSpeed);results[`${run?'run':'walk'}${diagonal?'Diagonal':'Cardinal'}`]=metres;
  }
  return results;
});

check('Five seconds of actual sprint movement exhausts stamina and forces walking',()=>{
  const g=new Exploration(data);g.autoStairsEnabled=false;g.warp(1,7,16);
  for(let n=0;n<100;n++)g.step(1,0,true,.05);
  near(g.stamina,0);assert(g.exhausted);
  g.step(1,0,true,.05);assert(!g.isRunning);near(g.speed,data.walkSpeed);
  assert(g.events.some(e=>e.type==='exhausted'));
  return {elapsed:g.time,stamina:g.stamina,exhausted:g.exhausted,speed:g.speed};
});

check('Recovery delay and exhaustion threshold prevent immediate sprint reuse',()=>{
  const g=new Exploration(data);g.autoStairsEnabled=false;g.warp(1,7,16);
  for(let n=0;n<100;n++)g.step(1,0,true,.05);
  const depletedAt=g.time;
  for(let n=0;n<28;n++)g.step(0,0,true,.05);
  near(g.stamina,0);assert(g.exhausted);
  let recoveryAt;
  for(let n=0;n<40&&g.exhausted;n++){
    g.step(0,0,true,.05);
    if(g.stamina<25)assert(g.exhausted);
    else recoveryAt=g.time;
  }
  assert(!g.exhausted&&g.stamina>=25);assert(recoveryAt-depletedAt>=2.8);
  const recoveredStamina=g.stamina;g.step(1,0,true,.05);assert(g.isRunning);
  return {recoveryDelaySeconds:1.5,reusableAtSeconds:recoveryAt-depletedAt,recoveredStamina};
});

check('Holding sprint against a solid boundary drains no stamina',()=>{
  const g=new Exploration(data);g.autoStairsEnabled=false;g.warp(1,42.284,16);
  g.stamina=50;g.lastSprintTime=-10;const before={x:g.x,y:g.y};
  for(let n=0;n<20;n++)g.step(1,0,true,.05);
  near(g.x,before.x);near(g.y,before.y);near(g.speed,0);assert(!g.isRunning);near(g.stamina,68);
  return {staminaBefore:50,staminaAfter:g.stamina,movedMetres:0};
});

check('Stationary sprint input does not drain stamina',()=>{
  const g=new Exploration(data);g.stamina=50;g.lastSprintTime=-10;
  for(let n=0;n<20;n++)g.step(0,0,true,.05);
  near(g.stamina,68);assert(!g.isRunning);return {staminaAfter:g.stamina};
});

check('Door-opening movement lock stops both displacement and sprint drain',()=>{
  const g=new Exploration(data),door=g.map.rooms.find(r=>r.interiorId==='room-f1-3');
  const [dx,dy]=directions[door.facing];g.warp(1,door.doorX+dx,door.doorY+dy);g.stamina=60;
  assert(g.interact());const before={x:g.x,y:g.y};
  for(let n=0;n<8;n++)g.step(1,1,true,.025);
  assert(g.pendingDoor);near(g.x,before.x);near(g.y,before.y);assert(g.stamina>=60);assert(!g.isRunning);
  return {lockedSeconds:g.time,stamina:g.stamina};
});

check('Exact swept clearance rejects library corner contacts missed by coarse samples',()=>{
  const g=new Exploration(data),map=g.spaces.get('room-f2-1'),radius=.31;
  // Captured from real mid-cell library replanning, not a synthetic obstacle.
  const segments=[
    [1.6009790070812508,6.041273434006522,1,11],
    [1.4421621136159097,7.351684512389946,1,11],
    [1.2833452201505686,8.66209559077338,1,11]
  ],results=[];
  for(const [ax,ay,bx,by] of segments){
    assert(!g.blocked(ax,ay,map,radius)&&!g.blocked(bx,by,map,radius));
    assert(sampledClear(g,ax,ay,bx,by,map,radius));assert(!g.segmentClear(ax,ay,bx,by,map,radius));
    const count=Math.ceil(Math.hypot(bx-ax,by-ay)*data.tileSize/.0005);let contact;
    for(let n=0;n<=count;n++){
      const x=ax+(bx-ax)*n/count,y=ay+(by-ay)*n/count;
      if(g.blocked(x,y,map,radius)){contact={x,y};break;}
    }
    assert(contact,'Dense independent position samples must confirm the contact');
    results.push({from:{x:ax,y:ay},to:{x:bx,y:by},contact,coarseSampleWouldAccept:true,exactSweepAccepts:false});
  }
  return {spaceId:map.id,radius,segments:results};
});

check('Pursuer reaches the farthest connected navigation point in every authored space without collision',()=>{
  const {game:g,pursuit:p}=create();let frames=0;const spaces=[];
  for(const map of g.spaces.values()){
    p.place(map.id,map.spawnX,map.spawnY,'chase');
    const nav=p.grid(map),distances=p.distances(nav,p.closestNode(nav,p.x,p.y,true));
    const [endId,gridDistance]=[...distances].sort((a,b)=>b[1]-a[1])[0],target=nav.nodes[endId];
    let n=0;
    while(Math.hypot(p.x-target.x,p.y-target.y)>.05&&n++<2000){
      g.time+=.05;p.moveToward(target,p.chaseSpeed,.05);
      assert(!g.blocked(p.x,p.y,map,p.radius),`${map.id} collider contact at ${p.x},${p.y}`);
      if(p.path.length)assert(g.segmentClear(p.x,p.y,p.path[0].x,p.path[0].y,map,p.radius),`${map.id} uncleared next leg`);
      frames++;
    }
    assert(n<2000,`${map.id} did not reach its connected target`);
    spaces.push({spaceId:map.id,reachableNodes:distances.size,graphMetres:gridDistance*data.tileSize,frames:n,target:{x:target.x,y:target.y}});
  }
  assert(spaces.length===44);metrics.collisionCheckedMovementFrames=frames;metrics.authoredSpacesTraversed=spaces.length;
  return {radius:p.radius,spaces,frames,positionAndNextLegClearance:true};
});

check('Every exterior room approach and canonical return landing fits the pursuer',()=>{
  const g=new Exploration(data),rooms=[];
  for(const floor of data.floors)for(const door of floor.rooms.filter(r=>r.interiorId)){
    const [dx,dy]=directions[door.facing],room=g.spaces.get(door.interiorId);
    assert(!g.blocked(door.doorX+dx,door.doorY+dy,floor,.31),`${door.label} approach`);
    assert(!g.blocked(room.returnX,room.returnY,floor,.31),`${door.label} return`);
    assert(!g.blocked(room.spawnX,room.spawnY,room,.31),`${door.label} room arrival`);
    rooms.push(room.id);
  }
  assert(rooms.length===41);return {roomCount:rooms.length,clearanceChecks:rooms.length*3,radius:.31};
});

check('A quiet player across a solid wall cannot be visually detected',()=>{
  const {game:g,pursuit:p}=create();g.warp(1,23,23);p.place('floor-1',18,23,'patrol');g.isRunning=false;
  const distance=Math.hypot(g.x-p.x,g.y-p.y)*data.tileSize;assert(distance<12);
  assert(!g.segmentClear(p.x,p.y,g.x,g.y,g.map,.025));assert(!p.sense());assert(p.state==='patrol');
  assert(!g.events.some(e=>e.type==='chase-start'));return {distanceMetres:distance,state:p.state};
});

check('Last-seen pursuit and search expire within their configured timeouts',()=>{
  const {game:g,pursuit:p}=create();g.warp(1,24,10);p.place('floor-1',24,13,'chase');
  // QA relocation isolates the memory timer; unlike a genuine door transition,
  // it emits no portal breadcrumb. Door/stair continuity is checked separately.
  g.warpRoom('room-f1-3',9,3);const lastSensed=p.lastSensed;
  for(let n=0;n<245;n++)tick(g,p,.05);
  assert(p.state==='patrol');assert(!p.portalTrail.length&&!p.transit);
  const ended=g.events.find(e=>e.type==='chase-end');assert(ended);assert(ended.time-lastSensed>=12&&ended.time-lastSensed<=12.15);
  return {sensoryIsolation:'QA relocation without transition event',lastSeenSeconds:10,searchSeconds:2,endedAfterSeconds:ended.time-lastSensed};
});

check('A genuine E door transition is followed physically, after delay, without player overlap',()=>{
  const {game:g,pursuit:p}=create();g.autoStairsEnabled=false;g.warp(1,32,30);p.place('floor-1',34,30,'chase');
  assert(g.interact());
  while(!g.roomId&&g.time<1)tick(g,p);
  assert(g.roomId==='room-f1-3');assert(p.spaceId==='floor-1'&&p.transfers===0);
  const playerTransition=g.events.find(e=>e.type==='transition');assert(playerTransition?.kind==='door');
  while(!p.transit&&g.time<2)tick(g,p);
  assert(p.transit);const expectedUntil=p.transit.until,enemyDeparture=g.events.find(e=>e.type==='enemy-door');
  assert(enemyDeparture);near(expectedUntil-enemyDeparture.time,1.25);
  assert(Math.hypot(p.x-playerTransition.sourceX,p.y-playerTransition.sourceY)*data.tileSize<.6);
  while(g.time+.025<expectedUntil-1e-9){tick(g,p);assert(p.spaceId==='floor-1'&&p.transfers===0);}
  for(let n=0;n<3&&p.transfers===0;n++)tick(g,p);
  assert(p.transfers===1&&p.spaceId===g.spaceId);assert(!g.blocked(p.x,p.y,g.map,p.radius));
  const separation=Math.hypot(p.x-g.x,p.y-g.y)*data.tileSize;assert(separation>=1.05);
  assert(!g.caught);const arrival=g.events.find(e=>e.type==='enemy-arrive');assert(arrival.time>=expectedUntil-1e-9);
  return {playerTransferAt:playerTransition.time,enemyReachedDoorAt:enemyDeparture.time,enemyArrivalAt:arrival.time,doorDelay:1.25,separationMetres:separation};
});

check('Automatic stair following respects its delay and standing arrivals do not loop',()=>{
  const {game:g,pursuit:p}=create();g.warp(1,26,27);p.place('floor-1',24,27,'chase');
  p.graceUntil=60; // Isolate transitions; caught behavior has a separate check.
  for(let n=0;n<40&&g.floor===1;n++)tick(g,p,.025,0,1);
  assert(g.floor===2);assert(p.spaceId==='floor-1');
  const transition=g.events.find(e=>e.type==='transition');assert(transition?.kind==='stairs');
  for(let n=0;n<100&&!p.transit;n++)tick(g,p);
  assert(p.transit);const expectedUntil=p.transit.until,departure=g.events.find(e=>e.type==='enemy-door');
  near(expectedUntil-departure.time,1.45);
  while(g.time+.025<expectedUntil-1e-9){tick(g,p);assert(p.spaceId==='floor-1'&&p.transfers===0);}
  for(let n=0;n<3&&p.transfers===0;n++)tick(g,p);
  assert(p.spaceId==='floor-2'&&p.transfers===1);assert(!g.blocked(p.x,p.y,g.map,p.radius));
  const separationAtArrival=Math.hypot(p.x-g.x,p.y-g.y)*data.tileSize;assert(separationAtArrival>=1.05);
  for(let n=0;n<80;n++)tick(g,p);
  assert(g.floor===2&&g.transitions===1&&p.transfers===1);assert(!p.transit);
  return {stairDelay:1.45,separationAtArrival,playerTransitions:g.transitions,enemyTransfers:p.transfers,standingObservationSeconds:2};
});

check('Catch freezes input and E, while reset restores a safe complete run',()=>{
  const {game:g,pursuit:p}=create();g.autoStairsEnabled=false;g.warp(1,24,10);p.place('floor-1',24,10.35,'chase');
  p.graceUntil=0;p.catchGraceUntil=0;tick(g,p,.05);assert(g.caught&&p.caught&&p.state==='caught');
  const before={x:g.x,y:g.y},serial=g.eventSerial;tick(g,p,.05,1,1,true);
  near(g.x,before.x);near(g.y,before.y);assert(!g.interact());assert(!g.isRunning);
  g.reset();p.reset();assert(!g.caught&&!p.caught);assert(g.stamina===100&&!g.exhausted);
  assert(g.spaceId===`floor-${data.spawnFloor}`);assert(g.eventSerial>serial);assert(p.initial.distanceFromPlayer>=15);
  return {inputFrozen:true,resetStamina:g.stamina,resetSpaceId:g.spaceId,eventIdsRemainMonotonic:true};
});

check('Events remain bounded and disabling pursuit releases a caught QA state',()=>{
  const {game:g,pursuit:p}=create();for(let n=0;n<200;n++)g.emit('qa-event');assert(g.events.length===128);
  for(let n=1;n<g.events.length;n++)assert(g.events[n].id>g.events[n-1].id);
  g.caught=true;p.caught=true;p.setEnabled(false);assert(!p.enabled&&!p.caught&&!g.caught);
  return {retainedEvents:g.events.length,lastSerial:g.eventSerial,disabledCaughtReleased:true};
});

const sources={};
for(const file of ['Assets/Resources/SchoolMap.json','Web/kernel.mjs','Web/pursuit.mjs'])sources[file]=crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex');
const report={schemaVersion:1,generatedAt:new Date().toISOString(),scope:'Authored-data Node kernel/pursuit checks; not native Unity or browser audio/render validation',
  passed:checks.filter(c=>c.passed).length,failed:checks.filter(c=>!c.passed).length,durationMs:Math.round(performance.now()-started),metrics,sources,checks};
const outIndex=process.argv.indexOf('--out');
const output=outIndex>=0?path.resolve(process.argv[outIndex+1]):path.join(root,'TestResults/ChaseRelease/pursuit-kernel.json');
fs.mkdirSync(path.dirname(output),{recursive:true});fs.writeFileSync(output,JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({passed:report.passed,failed:report.failed,durationMs:report.durationMs,metrics,output},null,2));
for(const c of checks.filter(c=>!c.passed))console.error(`${c.name}: ${c.error}`);
if(report.failed)process.exitCode=1;
