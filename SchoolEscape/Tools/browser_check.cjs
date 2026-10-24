// Real Chromium execution of the separate review implementation, not Unity.
const { chromium } = require(process.env.PLAYWRIGHT_MODULE||'/workspace/escape-tools/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..'),out=process.env.ESCAPE_RESULTS_DIR||path.join(root,'TestResults');fs.mkdirSync(out,{recursive:true});
// These are the original main-building doors that were sealed in version0.8.
// Name/floor checks prevent adding arbitrary new maps from masking a missed door.
const completionRooms=[
 {floor:1,label:'창고',kind:'storage'},{floor:1,label:'대회의실',kind:'assembly'},
 {floor:1,label:'설비관리실',kind:'maintenance'},{floor:1,label:'세미나실',kind:'seminar'},
 {floor:2,label:'준비실 1',kind:'preparation'},{floor:2,label:'자료실',kind:'archives'},{floor:2,label:'창고',kind:'storage'},
 {floor:3,label:'교직원 휴게실',kind:'lounge'},{floor:3,label:'학부모상담실',kind:'counseling'},
 {floor:3,label:'메인회의실',kind:'meeting'},{floor:3,label:'교장실',kind:'principal'},{floor:3,label:'중앙통제실',kind:'control'}
];
let browser,page,result;
(async()=>{
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 page=await browser.newPage({viewport:{width:1440,height:900},deviceScaleFactor:1});const errors=[],requests=[];
 page.setDefaultTimeout(120000);
 page.on('request',r=>{if(/^https?:/.test(r.url()))requests.push({url:r.url(),type:r.resourceType()})});
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
 page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`)});
 await page.goto(process.env.ESCAPE_URL||'http://127.0.0.1:8791/Web/?qa',{waitUntil:'networkidle',timeout:120000});
 await page.waitForFunction(()=>window.escapeLoaded,null,{timeout:120000});await page.screenshot({path:path.join(out,'browser-title.png')});
 await page.click('#begin');await page.waitForTimeout(300);
 // The chase beta has its own pursuit tests. Keep the existing whole-building
 // geometry/facility regression deterministic through the explicit QA hook.
 await page.evaluate(()=>escapeQA.pursuit?.setEnabled(false));
 const keyboard=[];
 for(const [name,keys] of [['walk',['KeyD']],['diagonal',['KeyD','KeyW']],['run',['KeyD','ShiftLeft']]]){
   await page.evaluate(()=>{const g=window.escapeQA.game;g.stamina=100;g.exhausted=false;g.warp(1,24,9)});
   const before=await page.evaluate(()=>({x:escapeQA.game.x,y:escapeQA.game.y,t:escapeQA.game.time}));
   for(const k of keys)await page.keyboard.down(k);
   await page.waitForFunction(t=>escapeQA.game.time>t+.45,before.t,{timeout:120000});
   for(const k of keys)await page.keyboard.up(k);
   const after=await page.evaluate(()=>({x:escapeQA.game.x,y:escapeQA.game.y,t:escapeQA.game.time}));
   const speed=Math.hypot(after.x-before.x,after.y-before.y)*1.25/(after.t-before.t);
   keyboard.push({name,speed,passed:Math.abs(speed-(name==='run'?4.7:2.6))<.22});
 }
 result=await page.evaluate(completionRooms=>{
   const {game:g}=escapeQA,passed=[],failed=[],spaceChecks=[],supportRoomVisits=[],facilityRoomVisits=[],completionRoomVisits=[],mirrorChecks=[],mirrorPixelChecks=[],corridorRoutes=[];const check=(ok,name)=>(ok?passed:failed).push(name);
   const supportKinds=new Set(['infirmary','administration','staff','computer']);
   const facilityKinds=new Set(['broadcast','studentRestroom','staffRestroom']);
   const completionKeys=new Set(completionRooms.map(r=>r.floor+':'+r.label));
   const pixelCheckedKinds=new Set();
   const age=(seconds=.9)=>{for(let t=0;t<seconds;t+=.016)g.step(0,0,false,.016)};
   const inspect=name=>{
     escapeQA.render();const info=escapeQA.inspectSpace();spaceChecks.push({name,space:g.spaceId,...info});
     check(info.activeIds.length===1&&info.activeIds[0]===g.spaceId,`isolated renderer roots ${name}`);
     const fixtureIds=[...new Set(info.visibleFixtureRoomIds||[])];
     check(g.roomId?fixtureIds.length===1&&fixtureIds[0]===g.spaceId:fixtureIds.length===0,`room furniture isolation ${name}`);
     check(document.getElementById('minimap').dataset.spaceId===g.spaceId,`minimap active space ${name}`);
     if(g.roomId&&g.map.kind==='library')check(info.bookShadowsDisabled&&info.bookBatches>0,`library book shadows disabled ${name}`);
   };
   g.autoStairsEnabled=false;
   // Corridor geometry stays closed; moving or waiting never enters a room.
   for(const f of g.data.floors){
     const start=f.landmarks[0];g.warp(f.number,start.x,start.y);inspect(f.id);
     for(const m of f.landmarks){check(g.walkTo(m.x,m.y),`${f.number}F route ${m.label}`);check(g.walkTo(start.x,start.y),`${f.number}F return ${m.label}`)}
     for(const r of f.rooms){
       const [dx,dy]=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing];
       check(!!r.interiorId&&!!r.interiorKind,`${f.number}F furnished portal available ${r.label}`);
       g.warp(f.number,start.x,start.y);check(g.walkTo(r.doorX+dx,r.doorY+dy),`${f.number}F reach doorway ${r.label}`);
       for(let i=0;i<80;i++)g.step(-dx,-dy,true,.016);
       check(!g.roomId&&g.spaceId===f.id&&!g.blocked(g.x,g.y)&&g.open(Math.round(g.x),Math.round(g.y)),`${f.number}F closed door blocks walking ${r.label}`);
     }
     for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){
       g.warp(f.number,start.x,start.y);for(let i=0;i<1000;i++)g.step(dx,dy,true,.016);
       check(!g.blocked(g.x,g.y)&&g.open(Math.round(g.x),Math.round(g.y)),`${f.number}F wall/bounds ${dx},${dy}`);
     }
     for(const passage of f.connectionPassages||[]){
       g.warp(f.number,passage.entryX,passage.entryY);const distance=g.distance;
       check(g.walkTo(passage.endX,passage.endY),`${f.number}F walk wing passage ${passage.id}`);
       check(g.distance-distance>=passage.minLengthMeters-.1,`${f.number}F wing passage requires real walking ${passage.id}`);
       check(g.walkTo(passage.entryX,passage.entryY),`${f.number}F return along wing passage ${passage.id}`);
     }
     for(const correction of f.corridorCorrections||[]){
       const route=[
         {label:'classroom A doorway',x:correction.classroomDoorX+1,y:correction.classroomDoorY},
         {label:'male toilet doorway',x:correction.maleApproachX,y:correction.maleApproachY},
         {label:'female toilet doorway',x:correction.femaleApproachX,y:correction.femaleApproachY},
         {label:'north west-wing join',x:correction.vertical[2],y:correction.vertical[3]-1}];
       g.warp(f.number,route[0].x,route[0].y);
       for(const point of [...route.slice(1),...route.slice(0,-1).reverse()]){
         check(g.walkTo(point.x,point.y)&&g.spaceId===f.id&&!g.blocked(g.x,g.y),`corrected L corridor walk/return ${point.label}`);
       }
       corridorRoutes.push({id:correction.id,floor:f.number,route,returned:Math.hypot(g.x-route[0].x,g.y-route[0].y)<.01});
     }
     for(const b of f.barriers){
       const side=b.x<5||b.x>43,dx=side?(b.x<5?-1:1):0,dy=side?0:(b.y<5?-1:1);
       g.warp(f.number,b.x-dx*2,b.y-dy*2);for(let i=0;i<120;i++)g.step(dx,dy,true,.016);
       check(!g.blocked(g.x,g.y)&&Math.hypot(g.x-b.x,g.y-b.y)>.3,`${f.number}F excluded exit sealed ${b.label}`);
     }
   }
   check(corridorRoutes.length===1&&corridorRoutes[0].returned,'corrected classroom/toilets L corridor round trip completed');
   // Interact schedules a brief opening, switches to a separate map and closes
   // behind the player. Every room is entered through the production portal.
   for(const f of g.data.floors)for(const r of f.rooms.filter(r=>r.interiorId)){
     const [dx,dy]=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing],room=g.data.roomMaps.find(m=>m.id===r.interiorId);
     g.warp(f.number,r.doorX+dx,r.doorY+dy);age();const oldSpace=g.spaceId,before=g.roomTransitions;
     if(supportKinds.has(room.kind)||facilityKinds.has(room.kind)||completionKeys.has(f.number+':'+room.label))inspect('before-'+room.id);
     check(g.nearbyDoor()?.targetId===room.id,`correct nearby room ${room.id}`);
     check(g.interact()&&g.spaceId===oldSpace&&g.pendingDoor,`room opening begins before transfer ${room.id}`);
     const x=g.x,y=g.y;g.step(-dx,-dy,true,.05);
     check(g.spaceId===oldSpace&&g.x===x&&g.y===y,`room opening freezes collision motion ${room.id}`);
     check(!g.interact(),`repeat interaction blocked during opening ${room.id}`);age(.3);
     check(g.roomId===room.id&&g.map===room&&g.roomTransitions===before+1&&g.floor===f.number&&!g.blocked(g.x,g.y),`enter independent room ${room.id}`);inspect(room.id);
     check(!g.interact(),`room arrival interaction cooldown ${room.id}`);
     for(const z of room.interiorZones){
       check(g.walkTo(z.x,z.y),`explore ${room.id} / ${z.label}`);
       check(g.walkTo(room.spawnX,room.spawnY),`return ${room.id} / ${z.label}`);
     }
     if(room.kind==='computer'){
       check(g.walkTo(room.width>>1,room.height-4),`computer central aisle walk ${room.id}`);
       check(g.walkTo(room.spawnX,room.spawnY),`computer central aisle return ${room.id}`);
     }
     if(room.kind==='studentRestroom'||room.kind==='staffRestroom'){
       const zone=room.interiorZones.find(z=>z.label.includes('거울'));
       check(!!zone&&g.walkTo(zone.x,zone.y),`reach large mirror viewing zone ${room.id}`);
       escapeQA.render(0);
       // walkTo advances controller time synchronously while requestAnimationFrame
       // is paused by this evaluate. Settle the normal follow-camera target as
       // those elapsed walking frames would, so this is an on-screen check.
       escapeQA.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);
       escapeQA.render(0);
       const mirrors=typeof escapeQA.inspectMirrors==='function'?escapeQA.inspectMirrors():null;
       check(!!mirrors,`real mirror inspection available ${room.id}`);
       if(mirrors){
         const active=mirrors.mirrors.filter(m=>m.visible&&m.roomId===room.id);
         check(mirrors.activeSpaceId===room.id&&active.length===1,`only current restroom mirror visible ${room.id}`);
         check(active.every(m=>m.renderedFrames>0&&m.textureWidth>=128&&m.textureHeight>=128),`mirror renders a reflection texture ${room.id}`);
         check(active.length>0&&active.every(m=>m.actorInReflection&&m.actorInsideSurface),`explorer reflected inside large mirror surface ${room.id}`);
         mirrorChecks.push({id:room.id,kind:room.kind,...mirrors});
       }
       if(!pixelCheckedKinds.has(room.kind)){
         pixelCheckedKinds.add(room.kind);
         const zoom=escapeQA.camera.zoom,beforeResize=escapeQA.inspectMirrors().mirrors[0].renderedFrames;
         escapeQA.camera.zoom=zoom*.95;escapeQA.camera.updateProjectionMatrix();escapeQA.render(0);
         check(escapeQA.inspectMirrors().mirrors[0].renderedFrames>beforeResize,`mirror updates when camera projection changes ${room.kind}`);
         escapeQA.camera.zoom=zoom;escapeQA.camera.updateProjectionMatrix();escapeQA.render(0);
         const pixels=typeof escapeQA.checkMirrorPixels==='function'?escapeQA.checkMirrorPixels():null;
         check(!!pixels,`actual mirror pixel readback available ${room.kind}`);
         if(pixels){
           check(pixels.roomId===room.id&&pixels.changedPixels>10&&pixels.maximumDelta>2,`explorer changes real reflected pixels ${room.kind}`);
           mirrorPixelChecks.push({id:room.id,kind:room.kind,...pixels});
         }
         escapeQA.render(0);
       }
     }
     // One physical contact per fixture kind; no collision-only mirror.
     const sampled=new Set();let fixtureChecks=0;
     for(const o of room.fixtures){
       if(!o.solid||sampled.has(o.kind))continue;
       const radius=g.data.playerRadius/g.data.tileSize,hw=o.width/g.data.tileSize/2,hd=o.depth/g.data.tileSize/2;
       let approach;
       for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){
         const x=o.x+dx*(hw+radius+.12),y=o.y+dy*(hd+radius+.12);
         if(!g.blocked(x,y)){approach={x,y,dx,dy};break}
       }
       if(!approach)continue;sampled.add(o.kind);fixtureChecks++;
       const {x,y,dx,dy}=approach;g.warpRoom(room.id,x,y);for(let i=0;i<45;i++)g.step(-dx,-dy,true,.016);
       const outside=dx?dx*(g.x-o.x)>=hw+radius-.002:dy*(g.y-o.y)>=hd+radius-.002;
       check(outside&&!g.blocked(g.x,g.y),`solid furniture collision ${room.id} / ${o.kind}`);
     }
     check(fixtureChecks>0,`furniture collision checks executed ${room.id}`);
     g.warpRoom(room.id,room.spawnX,room.spawnY);
     check(g.walkTo(room.exitX,room.exitY+1),`reach return door ${room.id}`);
     for(let i=0;i<80;i++)g.step(0,-1,true,.016);
     check(g.roomId===room.id&&!g.blocked(g.x,g.y),`room exit blocks walking without interaction ${room.id}`);
     check(g.walkTo(room.exitX,room.exitY+1),`return door usable after contact ${room.id}`);age();
     check(g.nearbyDoor()?.exit===true&&g.interact(),`interact with room exit ${room.id}`);age(.3);
     check(!g.roomId&&g.spaceId===f.id&&g.floor===f.number&&Math.hypot(g.x-room.returnX,g.y-room.returnY)<.01&&!g.blocked(g.x,g.y),`return to correct corridor position ${room.id}`);inspect('return-'+room.id);
     check(g.blocked(r.doorX,r.doorY)&&!g.pendingDoor,`door closed again after leaving ${room.id}`);
     age(1.2);check(!g.roomId,`idle return does not automatically reenter ${room.id}`);
     if(supportKinds.has(room.kind))supportRoomVisits.push({id:room.id,kind:room.kind,floor:f.number,zones:room.interiorZones.length,returned:g.spaceId===f.id});
     if(facilityKinds.has(room.kind))facilityRoomVisits.push({id:room.id,kind:room.kind,label:room.label,floor:f.number,zones:room.interiorZones.length,returned:g.spaceId===f.id});
     if(completionKeys.has(f.number+':'+room.label))completionRoomVisits.push({id:room.id,kind:room.kind,label:room.label,floor:f.number,zones:room.interiorZones.length,returned:g.spaceId===f.id});
     if(room.kind==='music')check(room.interiorZones.length>=4,`music four linked zones ${room.id}`);
   }
   check(supportRoomVisits.length===5&&supportRoomVisits.filter(r=>r.kind==='staff').length===2,'all five support room instances entered, explored and returned');
   check(facilityRoomVisits.length===13&&facilityRoomVisits.filter(r=>r.kind==='broadcast').length===1&&facilityRoomVisits.filter(r=>r.kind==='studentRestroom').length===6&&facilityRoomVisits.filter(r=>r.kind==='staffRestroom').length===6,'broadcast and all twelve separate restroom instances entered and returned');
   check(mirrorPixelChecks.length===2,'both student and staff mirrors underwent actual reflection-pixel checks');
   const doorCount=g.data.floors.reduce((n,f)=>n+f.rooms.length,0);
   check(g.data.roomMaps.length===doorCount&&doorCount===53,'all53 authored main-building doors have independent room maps');
   for(const target of completionRooms){
     const visits=completionRoomVisits.filter(v=>v.floor===target.floor&&v.label===target.label);
     check(visits.length===1&&visits[0].kind===target.kind&&visits[0].zones>=5&&visits[0].returned,`newly furnished room explored and returned ${target.floor}F ${target.label}`);
   }
   check(completionRoomVisits.length===completionRooms.length,'all twelve previously sealed main-building interiors visited');
   check(g.data.roomMaps.filter(r=>r.kind==='library').length===1&&g.data.roomMaps.find(r=>r.kind==='library')?.sourceFloor===2,'only the2F library remains among53 room maps');
   check(g.data.roomMaps.find(r=>r.id==='room-f1-3')?.kind==='classroom','former1F library now enters a classroom');
   // Preserve walking-only automatic stairs in the building.
   g.warp(1,24,5);g.autoStairsEnabled=true;const route=[1];
   for(const target of [2,3,2,1]){
     const from=g.floor,before=g.transitions,s=g.map.stairs.find(s=>s.target===target);age();
     check(g.walkTo(s.x,s.y+(target>from?-1:1))&&g.floor===from,`nearby stair alone does not enter ${from}`);
     check(g.walkTo(s.x,s.y)&&g.floor===target&&g.transitions===before+1&&!g.blocked(g.x,g.y),`walk-only automatic transition ${from}→${target}`);route.push(g.floor);age(2.9);
     check(g.floor===target&&g.transitions===before+1,`no idle landing loop ${target}`);
     const back=g.map.stairs.find(s=>s.target===from);check(g.walkTo(back.x,back.y)&&g.floor===target&&g.transitions===before+1,`arrival guard blocks delayed return ${target}`);
     const exit=g.map.landmarks[0];check(g.walkTo(exit.x,exit.y)&&!g.arrivalLock&&g.floor===target,`can leave arrival landing ${target}`);
   }
   check(route.join(',')==='1,2,3,2,1','full round trip');
   check(escapeQA.scene.children.filter(x=>x===escapeQA.actor).length===1,'single independent explorer');
   check(!document.body.innerText.match(/HP|궁극기|튜토리얼|몬스터/),'no combat or tutorial HUD');
   check(!document.getElementById('stair-prompt').querySelector('kbd'),'no stair interaction key in prompt');
   const doorVisibility=[];
   for(const [floor,label] of [[2,'빈교실 A'],[2,'빈교실 C'],[2,'빈교실 D'],[2,'음악실'],[2,'도서실'],[2,'미술실'],[1,'방송실']]){
     const r=g.data.floors[floor-1].rooms.find(r=>r.label===label),[dx,dy]=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing];
     g.warp(floor,r.doorX+dx*2,r.doorY+dy*2);escapeQA.render();escapeQA.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);escapeQA.render(.016);
     const probe=escapeQA.inspectDoor(label);doorVisibility.push({floor,label,...probe});check(probe.inViewport&&/Closed door:|Door glazing|Door handle/.test(probe.firstHit||''),`${floor}F closed door visible ${label}`);
   }
   g.warp(1,24,11);escapeQA.render();
   return {kind:'independent browser review runtime; NOT Unity/WebGL build',passed,failed,route,doorVisibility,spaceChecks,supportRoomVisits,facilityRoomVisits,completionRoomVisits,mirrorChecks,mirrorPixelChecks,corridorRoutes};
 },completionRooms);
 if(process.env.ESCAPE_EXPECT_SELF_CONTAINED==='1'&&requests.some(r=>r.type!=='document'))result.failed.push('Self-contained build made an external resource request');
 result.keyboard=keyboard;result.errors=errors;result.requests=requests;result.utc=new Date().toISOString();
 result.stage='synchronous map regression complete; real keyboard checks pending';result.screenshotsCompleted=false;
 fs.writeFileSync(path.join(out,'browser-runtime.json'),JSON.stringify(result,null,2));
 // A long synchronous route check can outlive the page's foreground focus.
 // Resume through the public control, preserving the normal blur/pause guard.
 await page.bringToFront();
 if(await page.locator('#pause').isVisible())await page.click('#resume');
 await page.evaluate(()=>{escapeQA.game.warp(1,26,27);escapeQA.game.lastTransition=-10;escapeQA.render()});
 result.keyboardPhaseStart=await page.evaluate(()=>({space:escapeQA.game.spaceId,x:escapeQA.game.x,y:escapeQA.game.y,time:escapeQA.game.time,autoStairsEnabled:escapeQA.game.autoStairsEnabled,caught:escapeQA.game.caught,stamina:escapeQA.game.stamina,exhausted:escapeQA.game.exhausted,keys:[...escapeQA.keys],hidden:document.hidden,focused:document.hasFocus(),activeElement:document.activeElement?.id,pauseVisible:!document.getElementById('pause').hidden}));
 fs.writeFileSync(path.join(out,'browser-runtime.json'),JSON.stringify(result,null,2));
 await page.keyboard.down('KeyW');await page.waitForFunction(()=>escapeQA.game.floor===2,null,{timeout:120000});await page.keyboard.up('KeyW');
 (await page.evaluate(()=>escapeQA.game.floor===2&&!escapeQA.game.blocked(escapeQA.game.x,escapeQA.game.y))?result.passed:result.failed).push('real keyboard W enters stairs automatically without E');
 // Held movement cannot enter a room. A real E key opens and switches maps.
 const keyRoom=await page.evaluate(()=>{
   const g=escapeQA.game,f=g.data.floors[1],r=f.rooms.find(r=>r.interiorId&&r.interiorKind==='classroom'),d=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing];
   g.warp(2,r.doorX+d[0],r.doorY+d[1]);g.lastDoorTransition=-10;escapeQA.render();return {id:r.interiorId,label:r.label,dx:d[0],dy:d[1]};
 });
 const inward=keyRoom.dx===1?'KeyA':keyRoom.dx===-1?'KeyD':keyRoom.dy===1?'KeyS':'KeyW';
 const t=await page.evaluate(()=>escapeQA.game.time);await page.keyboard.down(inward);await page.waitForFunction(t=>escapeQA.game.time>t+.7,t);await page.keyboard.up(inward);
 (await page.evaluate(()=>!escapeQA.game.roomId)?result.passed:result.failed).push('real movement key cannot pass closed room door');
 await page.keyboard.down('KeyE');await page.waitForFunction(id=>escapeQA.game.roomId===id,keyRoom.id);
 await page.evaluate(()=>{const g=escapeQA.game;g.walkTo(g.map.exitX,g.map.exitY+1);for(let i=0;i<60;i++)g.step(0,0,false,.016);escapeQA.render()});
 const beforeHeld=await page.evaluate(()=>({t:escapeQA.game.time,n:escapeQA.game.roomTransitions}));
 // Explicit repeat keydown exercises the event.repeat guard in addition to holding.
 await page.evaluate(()=>document.dispatchEvent(new KeyboardEvent('keydown',{code:'KeyE',repeat:true,bubbles:true})));
 await page.waitForFunction(t=>escapeQA.game.time>t+.95,beforeHeld.t);await page.keyboard.up('KeyE');
 (await page.evaluate(n=>escapeQA.game.roomTransitions===n&&!!escapeQA.game.roomId,beforeHeld.n)?result.passed:result.failed).push('real E opens independent room and holding does not loop');
 await page.evaluate(()=>{const g=escapeQA.game;g.walkTo(g.map.exitX,g.map.exitY+1);escapeQA.render()});await page.keyboard.press('KeyE');await page.waitForFunction(()=>!escapeQA.game.roomId);
 (await page.evaluate(()=>escapeQA.game.floor===2&&!escapeQA.game.blocked(escapeQA.game.x,escapeQA.game.y))?result.passed:result.failed).push('real E returns from room to correct floor');
 await page.waitForFunction(()=>escapeQA.game.time-escapeQA.game.lastDoorTransition>.8);await page.keyboard.press('KeyE');await page.waitForFunction(id=>escapeQA.game.roomId===id,keyRoom.id);
 result.passed.push('real E revisits same room after door closed');
 // Furnished offices/facilities and every newly opened door also use actual
 // DOM key events; all furniture-zone paths were explored by the controller above.
 result.supportKeyboard=[];
 result.completionKeyboard=[];
 const keyboardRooms=[...['infirmary','administration','staff','computer','broadcast','studentRestroom','staffRestroom'].map(kind=>({kind})),...completionRooms];
 for(const subject of keyboardRooms){
   const name=subject.label?`${subject.floor}F ${subject.label}`:subject.kind;
   const target=await page.evaluate(subject=>{
     const g=escapeQA.game,m=g.data.roomMaps.find(m=>subject.label?m.label===subject.label&&m.sourceFloor===subject.floor:m.kind===subject.kind);if(!m)return null;
     const f=g.data.floors[m.sourceFloor-1],r=f.rooms.find(r=>r.interiorId===m.id),d=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing];
     g.warp(f.number,r.doorX+d[0],r.doorY+d[1]);g.lastDoorTransition=-10;g.autoStairsEnabled=false;escapeQA.render();
     return {kind:m.kind,id:m.id,label:m.label,floor:f.number,returnX:m.returnX,returnY:m.returnY,beforeFixtures:escapeQA.inspectSpace().visibleFixtureRoomIds.length};
   },subject);
   if(!target){result.failed.push(`missing real keyboard room ${name}`);continue}
   (target.beforeFixtures===0?result.passed:result.failed).push(`new room hidden before real E ${name}`);
   await page.keyboard.press('KeyE');await page.waitForFunction(id=>escapeQA.game.roomId===id,target.id);
   const before=await page.evaluate(()=>({x:escapeQA.game.x,y:escapeQA.game.y,t:escapeQA.game.time}));
   await page.keyboard.down('KeyW');await page.waitForFunction(t=>escapeQA.game.time>t+.4,before.t);await page.keyboard.up('KeyW');
   const movement=await page.evaluate(({id,before})=>{
     const g=escapeQA.game;return {safe:g.roomId===id&&!g.blocked(g.x,g.y),meters:Math.hypot(g.x-before.x,g.y-before.y)*g.data.tileSize};
   },{id:target.id,before});
   (movement.safe&&movement.meters>.65?result.passed:result.failed).push(`real W explores new room ${name}`);
   const exitReached=await page.evaluate(()=>{const g=escapeQA.game;return g.walkTo(g.map.exitX,g.map.exitY+1)});
   (exitReached?result.passed:result.failed).push(`new room exit reachable after real movement ${name}`);
   await page.waitForFunction(()=>escapeQA.game.time-escapeQA.game.lastDoorTransition>.8);await page.keyboard.press('KeyE');await page.waitForFunction(()=>!escapeQA.game.roomId);
   const returned=await page.evaluate(t=>{const g=escapeQA.game;escapeQA.render();return g.floor===t.floor&&Math.hypot(g.x-t.returnX,g.y-t.returnY)<.01&&!g.blocked(g.x,g.y)&&escapeQA.inspectSpace().visibleFixtureRoomIds.length===0},target);
   (returned?result.passed:result.failed).push(`real E returns from new room ${name}`);
   (subject.label?result.completionKeyboard:result.supportKeyboard).push({kind:target.kind,id:target.id,label:target.label,floor:target.floor,...movement,returned});
 }
 result.screenshotsCompleted=false;fs.writeFileSync(path.join(out,'browser-runtime.json'),JSON.stringify(result,null,2));
 for(const floor of [1,2,3]){
   await page.evaluate(floor=>{const g=escapeQA.game,f=g.data.floors[floor-1],p=f.landmarks.find(p=>p.label.includes('중앙'))||f.landmarks[0];g.warp(floor,p.x,p.y);escapeQA.render()},floor);await page.waitForTimeout(350);await page.screenshot({path:path.join(out,`browser-floor-${floor}.png`)});
 }
 await page.evaluate(()=>document.getElementById('map-toggle').click());await page.screenshot({path:path.join(out,'browser-map.png')});await page.evaluate(()=>document.getElementById('map-toggle').click());
 for(const [label,name] of [['빈교실 A','door-a'],['빈교실 C','door-c'],['빈교실 D','door-d'],['음악실','music'],['도서실','library']]){
   await page.evaluate(label=>{const g=escapeQA.game,r=g.data.floors[1].rooms.find(r=>r.label===label),[dx,dy]=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing];g.warp(2,r.doorX+dx*2,r.doorY+dy*2);escapeQA.render();escapeQA.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);escapeQA.render(.016)},label);
   await page.screenshot({path:path.join(out,`browser-${name}.png`)});
 }
 for(const kind of [...new Set(['classroom','music','art','library','infirmary','administration','staff','computer','broadcast','studentRestroom','staffRestroom',...completionRooms.map(r=>r.kind)])]){
   const captured=await page.evaluate(kind=>{
     const g=escapeQA.game,r=g.data.roomMaps.find(r=>r.kind===kind&&r.sourceFloor===2)||g.data.roomMaps.find(r=>r.kind===kind);if(!r)return false;
     const z=r.interiorZones.find(z=>['studentRestroom','staffRestroom'].includes(kind)&&z.label.includes('거울'))||r.interiorZones.find(z=>z.label.includes('중앙')||z.label.includes('메인')||z.label.includes('조정'))||r.interiorZones[0];g.warpRoom(r.id,z.x,z.y);escapeQA.render();escapeQA.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);escapeQA.render(.016);return true;
   },kind);
   if(captured){await page.waitForTimeout(350);await page.screenshot({path:path.join(out,`browser-interior-${kind}.png`)})}else result.failed.push(`Missing interior screenshot subject ${kind}`);
 }
 // The newly enlarged rear locker row must read at normal character scale.
 const lockerSubject=await page.evaluate(()=>{
   const g=escapeQA.game,m=g.data.roomMaps.find(m=>m.kind==='classroom'&&m.sourceFloor===2),z=m?.interiorZones.find(z=>z.label.includes('사물함'));if(!m||!z)return false;
   g.warpRoom(m.id,z.x,z.y);escapeQA.render();escapeQA.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);escapeQA.render(.016);return true;
 });
 if(lockerSubject)await page.screenshot({path:path.join(out,'browser-rear-lockers.png')});else result.failed.push('Missing rear locker screenshot subject');
 const musicSubject=await page.evaluate(()=>{
   const g=escapeQA.game,m=g.data.roomMaps.find(m=>m.kind==='music'),z=m?.interiorZones.find(z=>z.label==='연결 통로');if(!m||!z)return false;
   g.warpRoom(m.id,z.x,z.y);escapeQA.render();escapeQA.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);escapeQA.render(.016);return true;
 });
 if(musicSubject)await page.screenshot({path:path.join(out,'browser-music-suite.png')});else result.failed.push('Missing music connecting passage screenshot subject');
 const passages=await page.evaluate(()=>escapeQA.game.data.floors[1].connectionPassages||[]);
 for(const passage of passages){
   await page.evaluate(p=>{const g=escapeQA.game;g.warp(2,p.middleX??Math.round((p.entryX+p.endX)/2),p.middleY??p.entryY);escapeQA.render();escapeQA.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);escapeQA.render(.016)},passage);
   await page.screenshot({path:path.join(out,`browser-passage-${passage.id}.png`)});
 }
 // Return to the identical library scene for the existing stable frame check.
 await page.evaluate(()=>{const g=escapeQA.game,m=g.data.roomMaps.find(m=>m.kind==='library'&&m.sourceFloor===2)||g.data.roomMaps.find(m=>m.kind==='library'),z=m.interiorZones.find(z=>z.label.includes('중앙'))||m.interiorZones[0];g.warpRoom(m.id,z.x,z.y);escapeQA.render();escapeQA.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);escapeQA.render(.016)});
 // Two consecutive idle renders of the same library view should be stable.
 // Compare the WebGL canvas only, excluding timed HTML toast/UI content.
 const stable=await page.evaluate(()=>{
   const g=escapeQA.game;g.speed=0;escapeQA.render(0);const a=escapeQA.renderer.domElement.toDataURL();escapeQA.render(0);const b=escapeQA.renderer.domElement.toDataURL();return a===b;
 });
 (stable?result.passed:result.failed).push('library idle rendered frame stable');
 result.screenshotsCompleted=true;result.stage='complete';result.utc=new Date().toISOString();fs.writeFileSync(path.join(out,'browser-runtime.json'),JSON.stringify(result,null,2));
 console.log(JSON.stringify({kind:result.kind,passed:result.passed.length,failed:result.failed,route:result.route,keyboard:result.keyboard,supportKeyboard:result.supportKeyboard,completionKeyboard:result.completionKeyboard,supportRoomVisits:result.supportRoomVisits,facilityRoomVisits:result.facilityRoomVisits,completionRoomVisits:result.completionRoomVisits,mirrorChecks:result.mirrorChecks.length,mirrorPixelChecks:result.mirrorPixelChecks,errors:result.errors,requests:result.requests,spaceChecks:result.spaceChecks.length,utc:result.utc,screenshotsCompleted:result.screenshotsCompleted},null,2));
 await browser.close();if(result.failed.length||errors.length||keyboard.some(x=>!x.passed))process.exitCode=1;
})().catch(async e=>{
 console.error(e);
 result||={kind:'independent browser runtime; NOT Unity/WebGL build',passed:[],failed:[],errors:[]};
 result.failed.push(`Fatal runtime test: ${e.stack||e.message}`);result.screenshotsCompleted=false;result.utc=new Date().toISOString();
 if(page){
   try{
     result.failureState=await Promise.race([page.evaluate(()=>{const q=window.escapeQA,g=q?.game;return{space:g?.spaceId,x:g?.x,y:g?.y,time:g?.time,autoStairsEnabled:g?.autoStairsEnabled,arrivalLock:g?.arrivalLock,lastTransition:g?.lastTransition,transitions:g?.transitions,caught:g?.caught,stamina:g?.stamina,exhausted:g?.exhausted,keys:q?[...q.keys]:[],hidden:document.hidden,focused:document.hasFocus(),activeElement:document.activeElement?.id,pauseVisible:document.getElementById('pause')?!document.getElementById('pause').hidden:null,pursuit:q?.pursuit?.inspect()};}),new Promise(resolve=>setTimeout(()=>resolve({diagnostics:'page did not answer within 5 seconds'}),5000))]);
   }catch(error){result.failureState={diagnostics:error.message};}
   try{await page.screenshot({path:path.join(out,'browser-failure.png'),timeout:10000});}catch{}
 }
 fs.writeFileSync(path.join(out,'browser-runtime.json'),JSON.stringify(result,null,2));
 console.error(JSON.stringify({failureState:result.failureState,passed:result.passed.length,failed:result.failed},null,2));
 await browser?.close();process.exitCode=1;
});
