// Actual Chromium checks for this project's chase beta. This is not Unity Play.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'/workspace/escape-tools/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'..'),out=process.env.ESCAPE_RESULTS_DIR||path.join(root,'TestResults','ChaseRelease');
fs.mkdirSync(out,{recursive:true});
const result={kind:'SchoolEscape independent browser chase runtime; NOT Unity/WebGL',passed:[],failed:[],errors:[],requests:[],samples:{},screenshots:[],utc:null};
const check=(ok,name,detail)=>{(ok?result.passed:result.failed).push(name);if(detail!==undefined)result.samples[name]=detail;};
let browser,page;
const save=()=>{result.utc=new Date().toISOString();fs.writeFileSync(path.join(out,'chase-browser-runtime.json'),JSON.stringify(result,null,2));};
async function capture(name){await page.screenshot({path:path.join(out,name)});result.screenshots.push(name);}
(async()=>{
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 page=await browser.newPage({viewport:{width:1440,height:900},deviceScaleFactor:1});page.setDefaultTimeout(120000);
 page.on('pageerror',e=>result.errors.push(e.message));
 page.on('console',m=>{if(m.type()==='error')result.errors.push(m.text());});
 page.on('request',r=>{if(/^https?:/.test(r.url()))result.requests.push({url:r.url(),type:r.resourceType()});});
 page.on('response',r=>{if(r.status()>=400)result.errors.push(`HTTP ${r.status()} ${r.url()}`);});
 const documentResponse=await page.goto(process.env.ESCAPE_URL||'http://127.0.0.1:8793/index.html?qa',{waitUntil:'networkidle',timeout:120000});
 const documentBytes=await documentResponse.body();result.build={url:documentResponse.url(),bytes:documentBytes.length,sha256:crypto.createHash('sha256').update(documentBytes).digest('hex')};
 check(documentResponse.status()===200&&documentResponse.headers()['content-type']?.includes('text/html'),'actual chase document is served as playable HTML',result.build);
 if(process.env.ESCAPE_EXPECT_SHA256)check(result.build.sha256===process.env.ESCAPE_EXPECT_SHA256,'executed build matches final artifact SHA-256');
 await page.waitForFunction(()=>window.escapeLoaded&&window.escapeQA);
 const initial=await page.evaluate(()=>({enemy:escapeQA.pursuit?.inspect(),audio:escapeQA.audio?.inspect(),visual:escapeQA.pursuerVisual?.inspect(),tick:typeof escapeQA.tick,manual:typeof escapeQA.setManual}));
 check(!!initial.enemy?.enabled,'pursuer enabled by default',initial.enemy);
 check(initial.enemy?.initial?.distanceFromPlayer>=15,'initial pursuer placement has safe separation');
 check(initial.audio?.contextState==='uninitialized'&&!initial.audio?.unlocked,'audio has no autoplay context before start',initial.audio);
 check(initial.visual?.kind==='original-procedural-caretaker'&&initial.visual.targetHeight>2.1&&initial.visual.hasSeparateFingers&&initial.visual.hasStaffBadge,'original large caretaker mesh is present',initial.visual);
 check(initial.tick==='function'&&initial.manual==='function','production QA tick and manual mode available');
 await capture('chase-title.png');
 await page.click('#begin');await page.waitForTimeout(250);
 await page.waitForFunction(()=>{const a=escapeQA.audio.inspect();return a.unlocked&&a.started&&a.contextState==='running';});
 const unlocked=await page.evaluate(()=>escapeQA.audio.inspect());
 check(unlocked.unlocked&&unlocked.contextState==='running','real start click unlocks browser audio',unlocked);
 check(unlocked.ambienceActive&&unlocked.rms>1e-7,'base ambience produces a real audio signal');
 await page.click('#sound-toggle');await page.waitForTimeout(200);
 const muted=await page.evaluate(()=>({audio:escapeQA.audio.inspect(),pressed:document.getElementById('sound-toggle').getAttribute('aria-pressed')}));
 check(muted.audio.muted&&muted.pressed==='true'&&muted.audio.masterGain<.005,'real mute button silences output gain',muted);
 await page.click('#sound-toggle');await page.waitForTimeout(200);
 check(await page.evaluate(()=>!escapeQA.audio.inspect().muted&&escapeQA.audio.inspect().masterGain>.01),'real mute button restores output');
 await page.evaluate(()=>{escapeQA.pursuit.setEnabled(false);escapeQA.game.autoStairsEnabled=false;});
 const keyboard=[];
 for(const [name,keys] of [['walk',['KeyD']],['diagonal',['KeyD','KeyW']],['run',['KeyD','ShiftLeft']]]){
   await page.evaluate(()=>{const g=escapeQA.game;g.stamina=100;g.exhausted=false;g.warp(1,24,9);});
   const a=await page.evaluate(()=>({x:escapeQA.game.x,y:escapeQA.game.y,t:escapeQA.game.time,stamina:escapeQA.game.stamina}));
   for(const key of keys)await page.keyboard.down(key);
   await page.waitForFunction(t=>escapeQA.game.time>=t+.45,a.t);
   for(const key of keys)await page.keyboard.up(key);
   const b=await page.evaluate(()=>({x:escapeQA.game.x,y:escapeQA.game.y,t:escapeQA.game.time,stamina:escapeQA.game.stamina,tile:escapeQA.game.data.tileSize}));
   const speed=Math.hypot(b.x-a.x,b.y-a.y)*b.tile/(b.t-a.t);keyboard.push({name,speed,staminaBefore:a.stamina,staminaAfter:b.stamina});
   check(Math.abs(speed-(name==='run'?4.7:2.6))<.25,`real keyboard ${name} speed normalized`);
   if(name==='run')check(b.stamina<a.stamina-5,'real keyboard sprint drains stamina');
 }
 result.samples.keyboard=keyboard;
 await page.keyboard.press('Escape');await page.waitForTimeout(100);
 const pauseTime=await page.evaluate(()=>escapeQA.game.time);await page.waitForTimeout(250);
 const pauseState=await page.evaluate(()=>({time:escapeQA.game.time,audio:escapeQA.audio.inspect(),keys:escapeQA.keys.size}));
 check(Math.abs(pauseState.time-pauseTime)<.001&&pauseState.keys===0,'real Escape key freezes simulation and clears movement',pauseState);
 check(pauseState.audio.paused&&pauseState.audio.contextState==='suspended'&&pauseState.audio.effectiveGain===0,'pause silences and suspends audio');
 await page.click('#resume');
 // Software WebGL can take longer than100ms for the next complete frame.
 // Observe a resumed simulation frame rather than treating a slow render as
 // a pause failure; the production resume action and assertion stay the same.
 await page.waitForFunction(t=>escapeQA.game.time>t&&escapeQA.audio.inspect().contextState==='running',pauseTime);
 check(await page.evaluate(t=>escapeQA.game.time>t&&escapeQA.audio.inspect().contextState==='running',pauseTime),'resume restores simulation and audio');

 // Real DOM E key exercises the same opening and sound event path as play.
 const realDoor=await page.evaluate(()=>{
   const q=escapeQA,g=q.game,m=g.data.roomMaps.find(m=>m.sourceFloor===1&&m.kind==='classroom'),f=g.data.floors[0],r=f.rooms.find(r=>r.interiorId===m.id);
   const [dx,dy]=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing];
   g.warp(1,r.doorX+dx,r.doorY+dy);g.lastDoorTransition=-10;q.render();return{id:m.id,audio:q.audio.inspect()};
 });
 await page.keyboard.press('KeyE');await page.waitForFunction(id=>escapeQA.game.roomId===id,realDoor.id);
 await page.waitForTimeout(120);
 check(await page.evaluate(id=>escapeQA.game.spaceId===id&&!escapeQA.game.blocked(escapeQA.game.x,escapeQA.game.y),realDoor.id),'real E enters independent room safely');
 const afterDoor=await page.evaluate(()=>escapeQA.audio.inspect());
 check(afterDoor.doorOpen>realDoor.audio.doorOpen&&afterDoor.doorClose>realDoor.audio.doorClose,'real room opening and closing schedule original sounds',afterDoor);
 await page.evaluate(()=>{const g=escapeQA.game;g.walkTo(g.map.exitX,g.map.exitY+1);escapeQA.render();});
 await page.waitForFunction(()=>escapeQA.game.time-escapeQA.game.lastDoorTransition>.7);
 await page.keyboard.press('KeyE');await page.waitForFunction(()=>!escapeQA.game.roomId);
 check(await page.evaluate(()=>escapeQA.game.floor===1&&!escapeQA.game.blocked(escapeQA.game.x,escapeQA.game.y)),'real E leaves room safely');

 // Manual mode uses the production tick while stopping concurrent rAF updates.
 await page.evaluate(()=>{
   const q=escapeQA;q.setManual(true);q.keys.clear();q.pursuit.setEnabled(false);
   q.chaseHarness={
     run(seconds,ax=0,ay=0,run=false){for(let t=0;t<seconds-1e-8;t+=.025)q.tick(Math.min(.025,seconds-t),ax,ay,run);},
     settle(){const g=q.game;q.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);q.render(.016);},
     warp(id,x,y){const g=q.game;if(id.startsWith('room-'))g.warpRoom(id,x,y);else g.warp(Number(id.split('-')[1]),x,y);},
     runway(){
       const g=q.game;let best=null;
       for(const f of g.data.floors)for(let y=0;y<f.height;y++)for(let x=0;x<f.width;x++)if(!g.blocked(x,y,f))for(const [dx,dy]of[[1,0],[0,1]]){
         let n=0;while(n<45&&!g.blocked(x+dx*(n+1),y+dy*(n+1),f)&&g.segmentClear(x+dx*n,y+dy*n,x+dx*(n+1),y+dy*(n+1),f))n++;
         if(!best||n>best.n)best={floor:f.number,x,y,dx,dy,n};
       }return best;
     },
     placeNear(min=3,max=6){
       const g=q.game,p=q.pursuit,nodes=p.grid(g.map).nodes;
       const candidates=nodes.filter(n=>{const d=Math.hypot(n.x-g.x,n.y-g.y)*g.data.tileSize;return d>=min&&d<=max&&g.segmentClear(n.x,n.y,g.x,g.y,g.map,p.radius);});
       candidates.sort((a,b)=>Math.hypot(a.x-g.x,a.y-g.y)-Math.hypot(b.x-g.x,b.y-g.y));
       if(!candidates.length)throw Error('No clear controlled pursuer placement');
       const n=candidates[0];p.setEnabled(true);p.place(g.spaceId,n.x,n.y,'chase');return n;
     },
     relocatePlayerFar(min=5,max=11){
       const g=q.game,p=q.pursuit,origin={x:g.x,y:g.y};
       const candidates=p.grid(g.map).nodes.filter(n=>{const d=Math.hypot(n.x-origin.x,n.y-origin.y)*g.data.tileSize;return d>=min&&d<=max&&g.path(n.x,n.y);});
       candidates.sort((a,b)=>Math.hypot(a.x-origin.x,a.y-origin.y)-Math.hypot(b.x-origin.x,b.y-origin.y));
       if(!candidates.length)throw Error('No safe room follow observation location');
       this.warp(g.spaceId,candidates[0].x,candidates[0].y);return candidates[0];
     }
   };
 });
 const stamina=await page.evaluate(()=>{
   const q=escapeQA,g=q.game,h=q.chaseHarness;g.reset();q.pursuit.setEnabled(false);g.autoStairsEnabled=false;
   const ray=h.runway();if(!ray||ray.n*g.data.tileSize<27)throw Error('No long authored corridor for full stamina run');
   g.warp(ray.floor,ray.x,ray.y);const start={x:g.x,y:g.y,time:g.time};h.run(5.15,ray.dx,ray.dy,true);
   const exhausted={value:g.stamina,exhausted:g.exhausted,running:g.isRunning,speed:g.speed,x:g.x,y:g.y,time:g.time};
   h.run(.3,ray.dx,ray.dy,true);const locked={value:g.stamina,exhausted:g.exhausted,running:g.isRunning,speed:g.speed};
   h.run(1.8);const partial={value:g.stamina,exhausted:g.exhausted};
   h.run(1.5);const recovered={value:g.stamina,exhausted:g.exhausted};
   const before=g.stamina;h.run(.2,ray.dx,ray.dy,true);const resumed={value:g.stamina,before,running:g.isRunning,speed:g.speed};
   g.stamina=55;g.exhausted=false;g.lastSprintTime=g.time;h.run(.5,0,0,true);const idle={value:g.stamina,running:g.isRunning};
   // Find a real wall contact, then verify Shift against it consumes no stamina.
   let wall;
   for(let y=0;y<g.map.height&&!wall;y++)for(let x=0;x<g.map.width&&!wall;x++)if(!g.blocked(x,y))for(const [dx,dy]of[[1,0],[-1,0],[0,1],[0,-1]])if(g.blocked(x+dx,y+dy)){wall={x,y,dx,dy};break;}
   if(!wall)throw Error('No authored wall contact');g.warp(g.floor,wall.x,wall.y);h.run(1,wall.dx,wall.dy,false);
   g.stamina=55;g.lastSprintTime=g.time;const at={x:g.x,y:g.y};h.run(.5,wall.dx,wall.dy,true);
   const blocked={value:g.stamina,moved:Math.hypot(g.x-at.x,g.y-at.y),running:g.isRunning};
   return{ray,start,exhausted,locked,partial,recovered,resumed,idle,blocked};
 });
 check(stamina.exhausted.exhausted&&stamina.exhausted.value<1&&!stamina.exhausted.running,'five-second moving sprint exhausts stamina',stamina);
 check(stamina.locked.exhausted&&!stamina.locked.running&&Math.abs(stamina.locked.speed-2.6)<.03,'exhausted held Shift falls back to walking');
 check(stamina.partial.value>0&&stamina.partial.value<25&&stamina.partial.exhausted,'partial recovery keeps exhaustion lock');
 check(stamina.recovered.value>=25&&!stamina.recovered.exhausted,'recovery above quarter stamina releases exhaustion lock');
 check(stamina.resumed.running&&stamina.resumed.value<stamina.resumed.before&&Math.abs(stamina.resumed.speed-4.7)<.03,'sprinting resumes after recovery');
 check(stamina.idle.value>=55&&!stamina.idle.running,'holding Shift at rest does not drain stamina');
 check(stamina.blocked.moved<.005&&stamina.blocked.value>=54.9&&!stamina.blocked.running,'pushing into solid wall does not drain stamina');
 const exhaustionHUD=await page.evaluate(ray=>{
   const q=escapeQA,g=q.game,h=q.chaseHarness;g.reset();q.pursuit.setEnabled(false);g.autoStairsEnabled=false;g.warp(ray.floor,ray.x,ray.y);
   h.run(5.15,ray.dx,ray.dy,true);h.settle();
   return{value:document.getElementById('stamina-value').textContent,aria:document.getElementById('stamina-meter').getAttribute('aria-valuenow'),exhausted:document.getElementById('vitals').classList.contains('exhausted'),hint:document.getElementById('stamina-hint').textContent};
 },stamina.ray);
 check(exhaustionHUD.value==='0%'&&exhaustionHUD.aria==='0'&&exhaustionHUD.exhausted&&exhaustionHUD.hint.includes('25%'),'actual exhaustion is readable in stamina HUD and accessibility value',exhaustionHUD);
 await capture('chase-stamina-exhausted.png');

 const chase=await page.evaluate(()=>{
   const q=escapeQA,g=q.game,h=q.chaseHarness,p=q.pursuit;g.reset();p.reset();g.autoStairsEnabled=false;
   g.warp(1,24,9);p.place(g.spaceId,24,15,'patrol');const before={x:p.x,y:p.y};h.run(.4);
   const detected=p.state==='chase',distance=Math.hypot(p.x-before.x,p.y-before.y)*g.data.tileSize;
   let safe=true;for(let i=0;i<100&&!g.caught;i++){q.tick(.025,1,0,true);safe&&=!g.blocked(p.x,p.y,g.spaces.get(p.spaceId),p.radius);}
   h.settle();return{detected,distance,safe,enemy:p.inspect(),audio:q.audio.inspect(),visual:q.pursuerVisual.inspect(),visible:q.pursuerVisual.root.visible,active:q.inspectSpace(),stamina:g.stamina};
 });
 check(chase.detected&&chase.distance>.3,'nearby visible player starts real pursuit and movement',chase);
 check(chase.safe,'moving pursuer stays outside authored collision geometry');
 check(chase.visible&&chase.active.activeIds.length===1,'current-space pursuer renders with one active map');
 check(chase.audio.chaseIntensity>.5&&chase.audio.chaseBeats>0,'chase schedules urgent layered audio');
 await page.waitForTimeout(180);
 const chaseAudio=await page.evaluate(()=>escapeQA.audio.inspect());
 check(chaseAudio.chaseGain>.05&&chaseAudio.rms>1e-7,'chase audio has nonzero real gain and signal',chaseAudio);
 await capture('chase-live-corridor.png');
 await page.evaluate(()=>{escapeQA.camera.zoom=1.8;escapeQA.camera.updateProjectionMatrix();escapeQA.render(0);});
 await capture('chase-caretaker-close.png');
 await page.evaluate(()=>{escapeQA.camera.zoom=1;escapeQA.camera.updateProjectionMatrix();escapeQA.chaseHarness.settle();});

 const portal=await page.evaluate(()=>{
   const q=escapeQA,g=q.game,h=q.chaseHarness,p=q.pursuit;g.reset();p.reset();g.autoStairsEnabled=false;
   const f=g.data.floors[0],r=f.rooms.find(r=>r.label==='빈교실 3'),[dx,dy]=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing];
   g.warp(1,r.doorX+dx,r.doorY+dy);h.placeNear(3,6);const before=p.inspect(),opened=g.interact();h.run(.25);
   const entered=g.spaceId===r.interiorId,notInstant=p.spaceId===f.id,entryTime=g.time;
   h.relocatePlayerFar(5,8);let safe=true,followTime=null;
   for(let t=0;t<8&&!g.caught;t+=.025){q.tick(.025);safe&&=!g.blocked(p.x,p.y,g.spaces.get(p.spaceId),p.radius);if(p.spaceId===g.spaceId){followTime=g.time-entryTime;break;}}
   const followed=p.spaceId===r.interiorId,after=p.inspect();h.settle();
   return{before,opened,entered,notInstant,followed,followTime,safe,after,events:g.events.map(e=>({id:e.id,type:e.type,kind:e.kind})),audio:q.audio.inspect()};
 });
 check(portal.opened&&portal.entered&&portal.notInstant,'room transition does not instantly teleport enemy',portal);
 check(portal.followed&&portal.followTime>=1.2&&portal.followTime<8,'pursuer follows the used door after approach and delay');
 check(portal.safe&&portal.after.transfers===1,'door-follow landing and route avoid walls and furniture');
 check(portal.events.some(e=>e.type==='enemy-door')&&portal.events.some(e=>e.type==='enemy-arrive'),'door-follow emits departure and arrival events');
 await capture('chase-room-arrival.png');

 const back=await page.evaluate(()=>{
   const q=escapeQA,g=q.game,h=q.chaseHarness,p=q.pursuit,m=g.map;
   g.warpRoom(m.id,m.exitX,m.exitY+1);g.lastDoorTransition=g.time-1;h.placeNear(3,6);const before=p.transfers,opened=g.interact();h.run(.25);
   const entered=!g.roomId,notInstant=p.spaceId===m.id,entry=g.time;h.relocatePlayerFar(5,9);let safe=true;
   for(let t=0;t<8&&!g.caught;t+=.025){q.tick(.025);safe&&=!g.blocked(p.x,p.y,g.spaces.get(p.spaceId),p.radius);if(p.spaceId===g.spaceId)break;}
   return{opened,entered,notInstant,safe,followed:p.spaceId===g.spaceId,elapsed:g.time-entry,transfers:p.transfers-before,enemy:p.inspect()};
 });
 check(back.opened&&back.entered&&back.notInstant&&back.followed&&back.transfers===1&&back.elapsed>=1.2,'pursuer continues through a real room exit',back);
 check(back.safe,'returning pursuer lands outside corridor walls');

 const stairs=await page.evaluate(()=>{
   const q=escapeQA,g=q.game,h=q.chaseHarness,p=q.pursuit,reports=[];g.reset();p.reset();
   for(const [from,to]of[[1,2],[2,3],[3,2],[2,1]]){
     const f=g.data.floors[from-1],s=f.stairs.find(s=>s.target===to);g.warp(from,s.x,s.y+(to>from?-1:1));g.autoStairsEnabled=false;h.placeNear(3,6);
     g.autoStairsEnabled=true;g.lastTransition=g.time-10;g.arrivalLock=null;
     // Place on the actual trigger and let production automatic-stair logic run.
     g.x=s.x;g.y=s.y;const count=g.transitions,before=p.transfers;q.tick(.025);const player=g.floor===to&&g.transitions===count+1,notInstant=p.spaceId===f.id;
     const target=g.spaceId;g.autoStairsEnabled=false;h.relocatePlayerFar(6,10);let safe=true;const time=g.time;
     for(let t=0;t<8&&!g.caught;t+=.025){q.tick(.025);safe&&=!g.blocked(p.x,p.y,g.spaces.get(p.spaceId),p.radius);if(p.spaceId===target)break;}
     const arrived=p.spaceId===target,elapsed=g.time-time,transfers=p.transfers-before;
     p.setEnabled(false);h.run(2);reports.push({from,to,player,notInstant,arrived,elapsed,transfers,safe,stable:g.floor===to&&g.transitions===count+1,enemy:p.inspect()});
   }return{reports,audio:q.audio.inspect()};
 });
 for(const s of stairs.reports){
   check(s.player&&s.notInstant&&s.arrived&&s.elapsed>=1.4,`pursuer uses automatic stairs with delay ${s.from}→${s.to}`,s);
   check(s.safe&&s.transfers===1&&s.stable,`stair landing is safe without idle transition loop ${s.from}→${s.to}`);
 }
 check(stairs.audio.stairTransitions>=4&&stairs.audio.stairSteps>=4,'stair travel schedules original stair sounds',stairs.audio);

 const caught=await page.evaluate(()=>{
   const q=escapeQA,g=q.game,h=q.chaseHarness,p=q.pursuit;g.reset();p.reset();g.autoStairsEnabled=false;g.warp(1,24,9);p.place(g.spaceId,24,9.42,'chase');h.run(4.2);
   const before={x:g.x,y:g.y};h.run(.5,1,1,true);h.settle();
   return{caught:g.caught&&p.caught,state:p.state,visible:!document.getElementById('caught').hidden,stopped:Math.hypot(g.x-before.x,g.y-before.y)<1e-8,audio:q.audio.inspect()};
 });
 check(caught.caught&&caught.state==='caught'&&caught.visible,'near contact shows caught overlay',caught);
 check(caught.stopped&&caught.audio.caughtStingers>=1,'caught state stops player and plays one catch sound');
 await capture('chase-caught.png');
 await page.click('#restart');
 const restarted=await page.evaluate(()=>({caught:escapeQA.game.caught,enemy:escapeQA.pursuit.inspect(),stamina:escapeQA.game.stamina,floor:escapeQA.game.floor,hidden:document.getElementById('caught').hidden}));
 check(!restarted.caught&&restarted.enemy.enabled&&!restarted.enemy.caught&&restarted.stamina===100&&restarted.floor===1&&restarted.hidden,'real restart clears catch and restores stamina/safe enemy spawn',restarted);

 // Validate blur handler with held real movement, then resume normally.
 await page.evaluate(()=>escapeQA.setManual(false));await page.keyboard.down('KeyD');
 await page.evaluate(()=>window.dispatchEvent(new Event('blur')));await page.keyboard.up('KeyD');await page.waitForTimeout(120);
 const blur=await page.evaluate(()=>({keys:escapeQA.keys.size,audio:escapeQA.audio.inspect()}));
 check(blur.keys===0&&blur.audio.paused&&blur.audio.effectiveGain===0,'blur clears held movement and silences audio',blur);
 if(await page.locator('#resume').isVisible())await page.click('#resume');
 if(process.env.ESCAPE_EXPECT_SELF_CONTAINED==='1')check(result.requests.every(r=>r.type==='document'),'self-contained chase build makes no external asset requests');
 check(result.errors.length===0,'no browser exceptions, missing assets or console errors');
 save();console.log(JSON.stringify({passed:result.passed.length,failed:result.failed,errors:result.errors,screenshots:result.screenshots,utc:result.utc},null,2));
 await browser.close();if(result.failed.length||result.errors.length)process.exitCode=1;
})().catch(async error=>{
 result.failed.push(`Fatal runtime test: ${error.stack||error.message}`);save();console.error(error);
 if(page)try{await capture('chase-failure.png');save();}catch{}
 await browser?.close();process.exitCode=1;
});
