// Chromium release proof. QA relocation sets up cases; E/I/buttons and short
// WASD probes use real input. Long route sweeps use production collision steps.
// These results prove the independent browser game, not native Unity Play.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'/workspace/escape-tools/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'..'),out=process.env.ESCAPE_RESULTS_DIR||path.join(root,'TestResults','PlaygroundRelease'),evidence=path.join(root,'Docs','Evidence');
fs.mkdirSync(out,{recursive:true});fs.mkdirSync(evidence,{recursive:true});
const result={kind:'Actual Chromium playground 0.10 input, geometry, loot, compass, pursuit and audible output; NOT Unity Play',passed:[],failed:[],errors:[],requests:[],samples:{},screenshots:[],utc:null};
let page,browser;
const save=()=>{result.utc=new Date().toISOString();fs.writeFileSync(path.join(out,'playground-browser-runtime.json'),JSON.stringify(result,null,2)+'\n');};
function check(ok,name,detail){(ok?result.passed:result.failed).push(name);if(detail!==undefined)result.samples[name]=detail;save();if(!ok)throw Error(name);}
async function capture(name){
 const framing=await page.evaluate(()=>{const q=escapeQA;for(let i=0;i<24;i++)q.render(.05);q.scene.updateMatrixWorld(true);q.camera.updateMatrixWorld(true);const p=q.actor.position.clone().project(q.camera);return{space:q.game.spaceId,player:[q.game.x,q.game.y],projectedPlayer:[p.x,p.y,p.z],active:q.inspectSpace().activeIds};});
 if(Math.abs(framing.projectedPlayer[0])>.1||Math.abs(framing.projectedPlayer[1])>.1)throw Error('Screenshot camera has not settled on player: '+name);
 result.screenshotSamples??={};result.screenshotSamples[name]=framing;await page.screenshot({path:path.join(out,name),timeout:45000});result.screenshots.push(name);save();
}
async function age(seconds=.7){return page.evaluate(seconds=>{const q=escapeQA;for(let t=0;t<seconds-1e-8;t+=.025)q.tick(Math.min(.025,seconds-t));q.render(0);return{space:q.game.spaceId,x:q.game.x,y:q.game.y,safe:!q.game.blocked(q.game.x,q.game.y),won:q.game.won,pending:!!q.game.pendingDoor};},seconds);}
async function place(space,x,y){await page.evaluate(({space,x,y})=>{const q=escapeQA,g=q.game;q.setManual(true);g.autoStairsEnabled=false;g.warpSpace(space,x,y);q.render(0);},{space,x,y});}
async function portalSetup(from,to){return page.evaluate(({from,to})=>{
 const q=escapeQA,g=q.game,m=g.spaces.get(from),p=m.portals.find(p=>p.targetId===to),d=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[p.facing];
 q.setManual(true);g.autoStairsEnabled=false;g.warpSpace(from,p.doorX+d[0],p.doorY+d[1]);for(let i=0;i<28;i++)q.tick(.025);q.render(0);
 const visual=q.floorRoots.find(r=>r.userData.spaceId===from).getObjectByName('Closed door: '+p.label);
 return {portal:p,position:[g.x,g.y],near:g.nearbyDoor()?.targetId,safe:!g.blocked(g.x,g.y),closed:g.blocked(p.doorX,p.doorY),view:q.inspectSpace(),visual:!!visual,closedPosition:visual?.position.toArray()};
 },{from,to});}
async function transition(from,to,hold=false){
 const setup=await portalSetup(from,to);check(setup.near===to&&setup.safe&&setup.closed&&setup.view.activeIds.join()===from&&setup.visual,'Closed portal setup '+from+' to '+to,setup);
 await page.keyboard.down('KeyE');check(await page.evaluate(to=>escapeQA.game.pendingDoor?.targetId===to,to),'Actual E begins transition '+from+' to '+to);
 const arrived=await age(.3);const view=await page.evaluate(()=>escapeQA.inspectSpace());
 check(arrived.space===to&&arrived.safe&&!arrived.pending&&view.activeIds.join()===to&&view.minimapSpaceId===to,'E arrival is safe and only destination is visible '+to,{arrived,view});
 if(hold){await page.keyboard.down('KeyE');const idle=await age(2);check(idle.space===to&&!idle.pending&&Math.hypot(idle.x-arrived.x,idle.y-arrived.y)<1e-9,'Held E and idle time do not bounce through the portal '+to,idle);}
 await page.keyboard.up('KeyE');return{setup,arrived,view};
}
async function inputProbe(name,keys,seconds=.45){
 await page.evaluate(()=>{const q=escapeQA;q.game.stamina=100;q.game.exhausted=false;q.setManual(false);});
 for(const key of keys)await page.keyboard.down(key);let sample;
 try{
  await page.waitForFunction(()=>escapeQA.game.speed>.01);
  const start=await page.evaluate(()=>({x:escapeQA.game.x,y:escapeQA.game.y,time:escapeQA.game.time,distance:escapeQA.game.distance}));
  await page.waitForFunction(({t,s})=>escapeQA.game.time>t+s,{t:start.time,s:seconds});
  sample=await page.evaluate(a=>{const g=escapeQA.game;return{speed:(g.distance-a.distance)/(g.time-a.time),distance:Math.hypot(g.x-a.x,g.y-a.y)*g.data.tileSize,dx:g.x-a.x,dy:g.y-a.y,seconds:g.time-a.time,stamina:g.stamina,safe:!g.blocked(g.x,g.y),space:g.spaceId};},start);
 }finally{for(const key of keys)await page.keyboard.up(key);await page.evaluate(()=>escapeQA.setManual(true));}
 check(sample.safe&&sample.space==='playground'&&Math.abs(sample.speed-(keys.includes('ShiftLeft')?4.7:2.6))<.08,'Real keyboard '+name+' has correct outdoor speed',sample);return sample;
}
async function pickup(uid){
 await age(.4);const setup=await page.evaluate(uid=>{
  const q=escapeQA,g=q.game,it=q.items,i=it.worldItems.find(i=>i.uid===uid&&!i.picked);if(!i)throw Error('Missing pickup '+uid);
  const source=i.sourceId?it.searchById.get(i.sourceId):null;let matched=false;
  for(const p of source?source.approaches:[i]){g.warpSpace(i.spaceId,p.x,p.y);const n=it.nearby();if(n&&(n.uid===i.uid||n.itemUid===i.uid)){matched=true;break;}}
  q.render(0);return {uid,itemId:i.itemId,space:i.spaceId,hidden:i.hidden,sourceId:i.sourceId,matched,safe:!g.blocked(g.x,g.y),key:g.nearbyDoor()||it.nearbyGate()?'KeyF':'KeyE',visible:q.inspectSpace()};
 },uid);
 check(setup.matched&&setup.safe&&setup.visible.activeIds.join()===setup.space,'Reach authored pickup '+setup.itemId,setup);
 await page.keyboard.press(setup.key);
 const got=await page.evaluate(uid=>{const q=escapeQA;q.render(0);const i=q.items.worldItems.find(i=>i.uid===uid);return{picked:i.picked,copies:q.items.worldItems.filter(i=>i.uid===uid).length,inventory:q.items.inventory.map(i=>({uid:i.uid,itemId:i.itemId})),action:q.items.lastAction};},uid);
 check(got.picked&&got.copies===1,'Actual '+setup.key+' collects '+setup.itemId+' in '+setup.space,got);return setup;
}
async function observedFollow(from,to,enemyX,enemyY,escapeX,escapeY){
 await portalSetup(from,to);
 const observed=await page.evaluate(({from,enemyX,enemyY})=>{const q=escapeQA,g=q.game,p=q.pursuit;p.place(from,enemyX,enemyY,'patrol');p.direction={x:g.x-p.x,y:g.y-p.y};q.tick(.025);return p.inspect();},{from,enemyX,enemyY});
 check(observed.lastSense.visible&&observed.lastEvidenceKind==='sight'&&observed.state==='chase','Enemy actually sees player before outdoor portal '+from,observed);
 await page.keyboard.press('KeyE');await age(.3);check(await page.evaluate(to=>escapeQA.game.spaceId===to,to),'Observed real E transfers player '+from+' to '+to);
 await place(to,escapeX,escapeY);
 const follow=await page.evaluate(to=>{const q=escapeQA,p=q.pursuit,g=q.game,trace=[];for(let i=0;i<360&&p.spaceId!==to;i++){q.tick(.025);if(i%12===0)trace.push({time:g.time,space:p.spaceId,state:p.state,x:p.x,y:p.y,transit:!!p.transit});}q.render(0);return {enemy:p.inspect(),trace,caught:g.caught,events:g.events.filter(e=>e.type==='enemy-door'||e.type==='enemy-arrive')};},to);
 check(follow.enemy.spaceId===to&&follow.events.some(e=>e.type==='enemy-arrive'&&e.spaceId===to&&e.reason==='chase')&&!follow.caught,'Observed pursuer physically follows portal '+from+' to '+to,follow);
 return follow;
}
(async()=>{
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 page=await browser.newPage({viewport:{width:1440,height:900},deviceScaleFactor:1});page.setDefaultTimeout(60000);
 page.on('pageerror',e=>result.errors.push(e.message));page.on('console',m=>{if(m.type()==='error')result.errors.push(m.text());});
 page.on('request',r=>{if(/^https?:/.test(r.url()))result.requests.push({url:r.url(),type:r.resourceType()});});page.on('response',r=>{if(r.status()>=400)result.errors.push('HTTP '+r.status()+' '+r.url());});
 const response=await page.goto(process.env.ESCAPE_URL||'http://127.0.0.1:8810/index.html?qa',{waitUntil:'networkidle',timeout:120000}),bytes=await response.body();
 result.build={url:response.url(),bytes:bytes.length,sha256:crypto.createHash('sha256').update(bytes).digest('hex')};
 check(response.status()===200&&response.headers()['content-type']?.includes('text/html'),'Release executes as real browser HTML',result.build);
 if(process.env.ESCAPE_EXPECT_SHA256)check(result.build.sha256===process.env.ESCAPE_EXPECT_SHA256,'Executed artifact matches final release SHA-256');
 await page.waitForFunction(()=>window.escapeLoaded&&window.escapeQA);
 const initial=await page.evaluate(()=>{const q=escapeQA,g=q.game;return{revision:g.data.revision,rooms:g.data.roomMaps.length,outdoor:g.data.outdoorMaps.map(m=>({id:m.id,width:m.width,height:m.height})),gate:q.items.escapeGate,keys:q.items.worldItems.filter(i=>i.itemId.startsWith('key-')),enemy:q.pursuit.inspect(),audio:q.audio.inspect()};});
 check(initial.revision==='playground-gate-2026-10-24'&&initial.rooms===53&&initial.outdoor.length===1&&initial.outdoor[0].id==='playground'&&initial.gate.spaceId==='playground','Release retains 53 interiors and adds one independent playground',initial);
 check(initial.keys.map(i=>i.itemId).sort().join()==='key-a,key-b,key-c'&&initial.keys.some(i=>i.spaceId==='playground')&&initial.keys.some(i=>i.spaceId!=='playground')&&initial.keys.some(i=>i.hidden)&&initial.keys.some(i=>!i.hidden),'Actual run has distinct A/B/C in both regions and mixed hiding modes',initial.keys);
 check(initial.enemy.enabled&&initial.enemy.initial.spawnId&&initial.audio.contextState==='uninitialized'&&!initial.audio.unlocked,'Enemy defaults ON and sound waits for public Start');
 await capture('playground-title.png');await page.click('#begin');await page.waitForFunction(()=>{const a=escapeQA.audio.inspect();return a.unlocked&&a.contextState==='running'&&a.rms>1e-7;});
 check(true,'Public Start unlocks actual audible ambience',await page.evaluate(()=>escapeQA.audio.inspect()));
 await page.click('#sound-toggle');await page.waitForFunction(()=>{const a=escapeQA.audio.inspect();return a.muted&&a.masterGain<.005&&a.rms<.001;});check(true,'Public mute button silences actual output');
 await page.click('#sound-toggle');await page.waitForFunction(()=>!escapeQA.audio.inspect().muted&&escapeQA.audio.inspect().rms>1e-7);
 await page.click('#enemy-toggle');check(await page.evaluate(()=>!escapeQA.pursuit.enabled&&document.getElementById('enemy-toggle').getAttribute('aria-pressed')==='false'),'Public developer button turns enemy OFF');
 await portalSetup('floor-1','playground');await capture('playground-main-entrance.png');
 await page.evaluate(()=>escapeQA.setManual(false));const doorTime=await page.evaluate(()=>escapeQA.game.time);await page.keyboard.down('KeyS');
 try{await page.waitForFunction(t=>escapeQA.game.time>t+.7,doorTime)}finally{await page.keyboard.up('KeyS');await page.evaluate(()=>escapeQA.setManual(true));}
 check(await page.evaluate(()=>escapeQA.game.spaceId==='floor-1'&&!escapeQA.game.pendingDoor&&!escapeQA.game.blocked(escapeQA.game.x,escapeQA.game.y)),'Real walking into closed main entrance cannot enter without E');
 await transition('floor-1','playground',true);await capture('playground-north-arrival.png');
 const hud=await page.evaluate(()=>{const header=document.querySelector('header').getBoundingClientRect(),goal=document.getElementById('escape-goal').getBoundingClientRect(),area=document.getElementById('area').getBoundingClientRect();return {header:header.toJSON(),goal:goal.toJSON(),area:area.toJSON(),overlap:Math.min(header.right,goal.right)>Math.max(header.left,goal.left)&&Math.min(header.bottom,goal.bottom)>Math.max(header.top,goal.top),text:document.getElementById('area').textContent};});
 check(!hud.overlap&&hud.text.includes('운동장')&&hud.goal.right<=1440&&hud.goal.left>=0,'Outdoor location header and key progress remain readable without overlap',hud);
 const lights=await page.evaluate(()=>{const q=escapeQA,root=q.floorRoots.find(r=>r.userData.spaceId==='playground'),lamps=[];root.traverse(o=>{if(o.isPointLight)lamps.push({position:o.position.toArray(),intensity:o.intensity,distance:o.distance})});return lamps;});
 check(lights.length>=3&&lights.every(l=>l.intensity>0&&l.distance>0),'Rendered playground has active entrance and gate lamps',lights);
 check(await page.evaluate(()=>escapeQA.inspectSpace().visibleFixtureSpaceIds.includes('playground')&&!escapeQA.inspectSpace().visibleFixtureRoomIds.length),'Yard fixtures are rendered without leaking main-building interiors');
 await transition('playground','floor-1',true);
 check(await page.evaluate(()=>Math.hypot(escapeQA.game.x-24,escapeQA.game.y-3)<1e-9),'Outdoor return lands at intended main entrance 24,3');
 await transition('floor-1','playground');
 // All authored waypoints are independently reached by continuous production steps.
 const traversal=await page.evaluate(()=>{
  const q=escapeQA,g=q.game,m=g.map,landmarks=[],routes=[];let unsafe=0;
  for(const landmark of m.landmarks){g.warpSpace(m.id,m.spawnX,m.spawnY);const before=g.distance,ok=g.walkTo(landmark.x,landmark.y);landmarks.push({label:landmark.label,ok,distance:g.distance-before,error:Math.hypot(g.x-landmark.x,g.y-landmark.y)*g.data.tileSize,safe:!g.blocked(g.x,g.y),space:g.spaceId});}
  for(const route of m.explorationRoutes){g.warpSpace(m.id,route.points[0].x,route.points[0].y);const before=g.distance;let steps=0,reached=true;
   for(const target of route.points.slice(1)){let budget=20000;while(Math.hypot(target.x-g.x,target.y-g.y)>.015&&budget-->0){const dx=target.x-g.x,dy=target.y-g.y,n=Math.hypot(dx,dy),dt=Math.min(.025,n*g.data.tileSize/g.data.walkSpeed);g.step(dx/n,dy/n,false,dt);steps++;if(g.blocked(g.x,g.y))unsafe++;}if(budget<=0)reached=false;}
   const goal=route.points.at(-1);routes.push({id:route.id,reached,steps,distance:g.distance-before,error:Math.hypot(g.x-goal.x,g.y-goal.y)*g.data.tileSize,safe:!g.blocked(g.x,g.y),space:g.spaceId});
  }q.render(0);return{landmarks,routes,unsafe};
 });
 check(traversal.landmarks.length>=20&&traversal.landmarks.every(p=>p.ok&&p.safe&&p.error<.03&&p.space==='playground'),'Production movement reaches every outdoor landmark from the entrance',traversal.landmarks);
 check(traversal.routes.length===3&&!traversal.unsafe&&traversal.routes.every(r=>r.reached&&r.safe&&r.space==='playground'&&r.error<.03&&r.distance>55),'Central and both side routes traverse continuously around real obstacles',traversal);
 const collision=await page.evaluate(()=>{const q=escapeQA,g=q.game,m=g.map,kinds=['yard-cover-planter','yard-stage','bench-storage-cabinet','yard-guard-hut','escape-gate-panel'],probes=[];
  for(const kind of kinds){const fixtures=m.fixtures.filter(f=>f.kind===kind);for(const f of kind==='yard-cover-planter'?fixtures:fixtures.slice(0,1)){
   const side=kind==='escape-gate-panel'?1:-1,edge=f.y+side*(f.depth/g.data.tileSize/2+g.data.playerRadius/g.data.tileSize),y=edge+side*.12;if(g.blocked(f.x,y))throw Error('Unsafe collision probe '+f.id);g.warpSpace(m.id,f.x,y);for(let i=0;i<80;i++)g.step(0,-side,true,.025);
   probes.push({kind,id:f.id,safe:!g.blocked(g.x,g.y),stoppedBelow:side*(g.y-edge)>=-.001,x:g.x,y:g.y});
  }}g.warpSpace(m.id,1,20);for(let i=0;i<100;i++)g.step(-1,0,true,.025);probes.push({kind:'outer-fence',safe:!g.blocked(g.x,g.y),stoppedBelow:g.x>=.5+g.data.playerRadius/g.data.tileSize-.001,x:g.x,y:g.y});q.render(0);return probes;});
 check(collision.length===7&&collision.every(p=>p.safe&&p.stoppedBelow),'Cover, stage, benches, hut, gate and outer boundary stop the player',collision);
 await place('playground',34,43);const walk=await inputProbe('W walk',['KeyW']);await place('playground',34,43);const diagonal=await inputProbe('W+D diagonal',['KeyW','KeyD']);await place('playground',34,43);const run=await inputProbe('Shift+W run',['ShiftLeft','KeyW']);
 check(Math.abs(diagonal.speed-walk.speed)<.03&&run.stamina<95,'Diagonal is normalized and outdoor sprint spends stamina',{walk,diagonal,run});
 for(const [name,x,y] of [['central',34,26],['west',10,28],['east',58,28],['gate',34,5]]){await place('playground',x,y);await capture('playground-'+name+'.png');}
 // Web Audio recording proves that the running context produces a real signal.
 const recording=await page.evaluate(async()=>{const a=escapeQA.audio,c=a.context,dest=c.createMediaStreamDestination(),chunks=[];a.master.connect(dest);const recorder=new MediaRecorder(dest.stream,{mimeType:'audio/webm;codecs=opus'}),done=new Promise(resolve=>recorder.onstop=resolve);recorder.ondataavailable=e=>{if(e.data.size)chunks.push(e.data)};recorder.start();await new Promise(resolve=>setTimeout(resolve,650));recorder.stop();await done;a.master.disconnect(dest);const blob=new Blob(chunks,{type:'audio/webm'}),buffer=await blob.arrayBuffer(),decoded=await c.decodeAudioData(buffer.slice(0)),samples=decoded.getChannelData(0);let energy=0,peak=0;for(const n of samples){energy+=n*n;peak=Math.max(peak,Math.abs(n));}let binary='';for(const n of new Uint8Array(buffer))binary+=String.fromCharCode(n);return{bytes:buffer.byteLength,duration:decoded.duration,rms:Math.sqrt(energy/samples.length),peak,base64:btoa(binary),audio:a.inspect()};});
 fs.writeFileSync(path.join(out,'playground-audio.webm'),Buffer.from(recording.base64,'base64'));delete recording.base64;
 check(recording.bytes>1000&&recording.duration>.4&&recording.rms>1e-7&&recording.peak>1e-6&&!recording.audio.lastError,'Actual outdoor audio recording contains a nonzero signal',recording);
 await page.click('#enemy-toggle');await observedFollow('floor-1','playground',24,6,34,44);await capture('playground-pursuer-arrival.png');await observedFollow('playground','floor-1',34,48,24,9);
 await page.click('#enemy-toggle');const disabled=await page.evaluate(()=>{const q=escapeQA,p=q.pursuit,g=q.game;g.warpSpace(p.spaceId,p.x,p.y);const before={x:p.x,y:p.y,space:p.spaceId};for(let i=0;i<200;i++)q.tick(.025);return{before,after:{x:p.x,y:p.y,space:p.spaceId},enabled:p.enabled,caught:g.caught,audio:q.audio.inspect()};});
 check(!disabled.enabled&&!disabled.caught&&JSON.stringify(disabled.before)===JSON.stringify(disabled.after)&&disabled.audio.watchBpm===0,'Developer OFF stops movement, contact capture and watch threat output',disabled);
 const gate=initial.gate;await place(gate.spaceId,gate.x,gate.y);await page.keyboard.press('KeyE');check(await page.evaluate(()=>!escapeQA.game.won&&!escapeQA.items.lastAction.ok&&escapeQA.items.nearbyGate().missing.length===3),'Actual outdoor gate E refuses escape before A/B/C');await capture('playground-gate-locked.png');
 const mainKeys=await page.evaluate(()=>escapeQA.items.worldItems.filter(i=>i.itemId.startsWith('key-')&&i.spaceId!=='playground').map(i=>i.uid));for(const uid of mainKeys)await pickup(uid);
 await place(gate.spaceId,gate.x,gate.y);await page.keyboard.press('KeyE');check(await page.evaluate(()=>!escapeQA.game.won&&!escapeQA.items.lastAction.ok&&escapeQA.items.nearbyGate().missing.length>=1),'Keys collected only in the building still cannot escape');
 const parts=await page.evaluate(()=>escapeQA.items.worldItems.filter(i=>i.itemId.startsWith('clock-')).map(i=>i.uid));for(const uid of parts)await pickup(uid);
 await place('floor-1',24,9);const compass=await page.evaluate(()=>{escapeQA.render(0);return{value:escapeQA.items.compass(),visible:!document.getElementById('compass-hud').hidden,bag:escapeQA.items.inventory.map(i=>i.itemId)}});
 check(compass.visible&&compass.bag.includes('compass')&&!compass.bag.some(id=>id.startsWith('clock-'))&&compass.value.active&&compass.value.targetSpaceId==='playground'&&compass.value.via==='door'&&compass.value.hops>=1&&compass.value.x===24&&compass.value.y===2,'Real collected clock parts guide the remaining outdoor key through main entrance',compass);
 await capture('playground-compass-building.png');await transition('floor-1','playground');const outdoorCompass=await page.evaluate(()=>escapeQA.items.compass());
 check(outdoorCompass.active&&outdoorCompass.targetKeyUid===compass.value.targetKeyUid&&outdoorCompass.via==='key'&&outdoorCompass.hops===0,'Compass switches to the same real key after crossing outdoors',outdoorCompass);
 const yardKeys=await page.evaluate(()=>escapeQA.items.worldItems.filter(i=>i.itemId.startsWith('key-')&&i.spaceId==='playground').map(i=>i.uid));for(const uid of yardKeys)await pickup(uid);
 check(await page.evaluate(()=>escapeQA.items.hasAllKeys()&&!escapeQA.items.compass().active&&!escapeQA.items.inventory.some(i=>i.itemId==='compass')),'Main and outdoor searches produce exactly three keys and consume target compass');
 await page.keyboard.press('KeyI');await capture('playground-all-keys.png');await page.keyboard.press('Escape');
 await portalSetup('floor-1','playground');check(await page.evaluate(()=>!escapeQA.items.nearbyGate()&&escapeQA.items.hasAllKeys()),'Three keys do not create an escape trigger at the former 1F gate');
 await page.keyboard.press('KeyE');const oldExit=await age(.3);check(oldExit.space==='playground'&&!oldExit.won&&await page.evaluate(()=>escapeQA.items.hasAllKeys()),'Actual E at former 1F exit enters the yard without winning or consuming keys');
 await place('playground',gate.x+3,gate.y);await page.keyboard.press('KeyE');check(await page.evaluate(()=>!escapeQA.game.won&&escapeQA.items.hasAllKeys()&&!escapeQA.items.nearbyGate()),'Outdoor gate interaction requires its authored range');
 await place('playground',gate.x,gate.y);await page.keyboard.press('KeyE');await page.waitForFunction(()=>escapeQA.game.won&&!document.getElementById('success').hidden);
 const won=await page.evaluate(()=>({won:escapeQA.game.won,escaped:escapeQA.items.escaped,keys:escapeQA.items.inventory.filter(i=>i.itemId.startsWith('key-')).length,seed:escapeQA.items.seed,space:escapeQA.game.spaceId}));
 check(won.won&&won.escaped&&won.keys===0&&won.space==='playground','Actual outdoor gate E consumes A/B/C and displays victory',won);await capture('playground-victory.png');
 await page.click('#success-restart');const restart=await page.evaluate(()=>({space:escapeQA.game.spaceId,won:escapeQA.game.won,caught:escapeQA.game.caught,seed:escapeQA.items.seed,inventory:escapeQA.items.inventory.length,equipped:escapeQA.items.equipped,enabled:escapeQA.pursuit.enabled,keys:escapeQA.items.worldItems.filter(i=>i.itemId.startsWith('key-'))}));
 check(restart.space==='floor-1'&&!restart.won&&!restart.caught&&restart.seed!==won.seed&&!restart.inventory&&!restart.equipped&&!restart.enabled&&restart.keys.length===3&&restart.keys.some(i=>i.spaceId==='playground')&&restart.keys.some(i=>i.spaceId!=='playground'),'Public victory restart clears the run and rerolls keys across both regions',restart);
 check(result.errors.length===0,'No browser, console or HTTP errors',result.errors);result.ok=true;save();
 for(const name of result.screenshots)fs.copyFileSync(path.join(out,name),path.join(evidence,name));fs.copyFileSync(path.join(out,'playground-browser-runtime.json'),path.join(evidence,'playground-browser-runtime.json'));
 console.log(JSON.stringify({ok:true,checks:result.passed.length,failed:result.failed,build:result.build,screenshots:result.screenshots},null,2));await browser.close();
})().catch(async error=>{result.ok=false;result.fatalError=error.stack||error.message;if(page)try{result.failureState=await page.evaluate(()=>({space:escapeQA.game.spaceId,x:escapeQA.game.x,y:escapeQA.game.y,time:escapeQA.game.time,pending:escapeQA.game.pendingDoor,enemy:escapeQA.pursuit.inspect(),items:escapeQA.items.inspect(),audio:escapeQA.audio.inspect()}));await capture('playground-failure.png');}catch{}save();console.error(error);await browser?.close();process.exitCode=1;});
