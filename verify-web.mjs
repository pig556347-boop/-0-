// External smoke test of this branch's public HTML. It changes no site, Pages
// configuration, other game, or deployment. The workflow saves only its report.
import fs from 'node:fs';
import crypto from 'node:crypto';
import {chromium} from 'playwright';

const sourceCommit=process.env.PUBLIC_GAME_COMMIT||process.env.GITHUB_SHA;
const workflowCommit=process.env.GITHUB_SHA;
const sourceBranch=process.env.GITHUB_REF_NAME;
const repository=process.env.GITHUB_REPOSITORY;
const publicURL=`https://rawcdn.githack.com/${repository}/${sourceCommit}/index.html`;
const status={ok:false,utc:new Date().toISOString(),sourceCommit,workflowCommit,sourceBranch,publicURL,
  kind:'External public browser smoke test of independent browser review, not Unity WebGL',
  checks:[],errors:[],consoleErrors:[],warnings:[],externalContentNoticeConfirmed:false};
let browser,page,gameReady=false;
const check=(name,ok,actual)=>{
  status.checks.push({name,passed:!!ok,...(actual===undefined?{}:{actual})});
  if(!ok)throw Error(name);
};
async function verify(){
  check('Only the separate web branch is tested',sourceBranch==='school-escape-web',sourceBranch);
  check('Pinned commit is valid',/^[a-f0-9]{40}$/.test(sourceCommit||''));
  const manifest=JSON.parse(fs.readFileSync('build.json','utf8'));
  status.build=manifest;
  check('Manifest identifies the caretaker stamina audio release',manifest.version==='0.7'&&manifest.featureRevision==='caretaker-stamina-audio-2026-10-23',manifest.featureRevision);
  check('Manifest declares the expected room count',Number.isInteger(manifest.roomCount)&&manifest.roomCount>0,manifest.roomCount);
  const response=await fetch(publicURL,{signal:AbortSignal.timeout(30000)});
  const html=Buffer.from(await response.arrayBuffer());
  status.http={status:response.status,contentType:response.headers.get('content-type'),
    bytes:html.length,sha256:crypto.createHash('sha256').update(html).digest('hex')};
  check('Public CDN returns HTTP 200',response.status===200,status.http.status);
  check('Public CDN serves HTML for execution',/^text\/html\b/i.test(status.http.contentType||''),status.http.contentType);
  check('Public HTML matches the tested build',status.http.sha256===manifest.htmlSha256,status.http.sha256);

  browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH,headless:true,
    args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
  page=await browser.newPage({viewport:{width:1280,height:800},deviceScaleFactor:1});
  page.setDefaultTimeout(20000);
  page.on('pageerror',e=>status.errors.push(e.message));
  page.on('console',m=>{if(gameReady&&m.type()==='error')status.consoleErrors.push(m.text())});
  await page.goto(publicURL+'?qa',{waitUntil:'domcontentloaded',timeout:45000});
  const open=page.getByRole('button',{name:'Open the page',exact:true})
    .or(page.getByRole('link',{name:'Open the page',exact:true})).first();
  if(await open.count()){
    // Use the host's normal confirmation button, as a user would. No cookie
    // injection or server-notice bypass is used.
    await open.click();
    status.externalContentNoticeConfirmed=true;
  }
  await page.waitForFunction(()=>window.escapeLoaded&&window.escapeQA,null,{timeout:45000});
  gameReady=true;
  check('Game starts from the public URL',await page.locator('#begin').isVisible());
  const initial=await page.evaluate(()=>({enemy:escapeQA.pursuit?.inspect(),visual:escapeQA.pursuerVisual?.inspect(),audio:escapeQA.audio?.inspect()}));
  status.initialChase=initial;
  check('Original large caretaker is enabled by default',initial.enemy?.enabled&&initial.enemy.initial?.distanceFromPlayer>=15&&initial.visual?.kind==='original-procedural-caretaker'&&initial.visual.targetHeight===2.4&&initial.visual.hasStaffBadge,initial);
  check('Sound has no autoplay context before Start',initial.audio?.contextState==='uninitialized'&&!initial.audio?.unlocked,initial.audio);
  await page.click('#begin');
  await page.waitForFunction(()=>escapeQA.audio.inspect().unlocked&&escapeQA.audio.inspect().rms>1e-7,null,{timeout:20000});
  status.audioStarted=await page.evaluate(()=>escapeQA.audio.inspect());
  check('Actual Start click unlocks audible browser ambience',status.audioStarted.contextState==='running'&&status.audioStarted.ambienceActive&&status.audioStarted.rms>1e-7,status.audioStarted);
  await page.click('#sound-toggle');await page.waitForTimeout(200);
  const muted=await page.evaluate(()=>({audio:escapeQA.audio.inspect(),pressed:document.getElementById('sound-toggle').getAttribute('aria-pressed')}));
  check('Actual mute button silences audio and updates its state',muted.pressed==='true'&&muted.audio.muted&&muted.audio.masterGain<.005&&muted.audio.rms<.001,muted);
  await page.click('#sound-toggle');
  await page.waitForFunction(()=>!escapeQA.audio.inspect().muted&&escapeQA.audio.inspect().masterGain>.01&&escapeQA.audio.inspect().rms>1e-7);
  check('Actual sound button restores audio',await page.evaluate(()=>document.getElementById('sound-toggle').getAttribute('aria-pressed')==='false'));
  // Existing room and reflection checks below intentionally isolate geometry
  // from pursuit. The real pursuit is verified separately after these routes.
  await page.evaluate(()=>escapeQA.pursuit.setEnabled(false));
  const loaded=await page.evaluate(()=>{
    const g=escapeQA.game,maps=g.data.roomMaps,former=g.data.floors[0].rooms.find(r=>r.label==='빈교실 3');
    const formerMap=maps.find(m=>m.id===former?.interiorId);
    return {revision:g.data.revision,rooms:maps.length,active:escapeQA.inspectSpace().activeIds,
      startHidden:document.getElementById('start').hidden,classrooms:maps.filter(m=>m.kind==='classroom').length,
      libraries:maps.filter(m=>m.kind==='library').map(m=>({id:m.id,floor:m.sourceFloor})),
      formerLibrary:{label:former?.label,kind:formerMap?.kind,floor:formerMap?.sourceFloor},
      administrationDesks:maps.filter(m=>m.kind==='administration').flatMap(m=>m.fixtures).filter(o=>o.kind==='admin-desk').length,
      broadcastRooms:maps.filter(m=>m.kind==='broadcast').length,
      studentRestrooms:maps.filter(m=>m.kind==='studentRestroom').length,
      staffRestrooms:maps.filter(m=>m.kind==='staffRestroom').length,
      largeMirrors:maps.flatMap(m=>m.fixtures).filter(o=>o.kind==='mirror-surface').length};
  });
  status.loaded=loaded;
  check(`Latest ${manifest.roomCount}-room build loaded`,loaded.revision===manifest.revision&&loaded.rooms===manifest.roomCount,loaded);
  check('Start button enters play',loaded.startHidden&&loaded.active.length===1);
  check('Administration has 18 workstations',loaded.administrationDesks===18,loaded.administrationDesks);
  check('Former 1F library is a classroom',loaded.formerLibrary.kind==='classroom'&&loaded.formerLibrary.floor===1&&loaded.classrooms===19,loaded.formerLibrary);
  check('Only the 2F library remains',loaded.libraries.length===1&&loaded.libraries[0].floor===2,loaded.libraries);
  check('Broadcast and all twelve restroom mirrors loaded',loaded.broadcastRooms===1&&loaded.studentRestrooms===6&&loaded.staffRestrooms===6&&loaded.largeMirrors===12);

  await page.evaluate(()=>{const g=escapeQA.game;g.warp(1,24,9);escapeQA.render()});
  const before=await page.evaluate(()=>({x:escapeQA.game.x,y:escapeQA.game.y,time:escapeQA.game.time}));
  await page.keyboard.down('KeyW');
  try{await page.waitForFunction(t=>escapeQA.game.time>t+.4,before.time,{timeout:20000})}
  finally{await page.keyboard.up('KeyW')}
  const move=await page.evaluate(before=>{const g=escapeQA.game;return {
    meters:Math.hypot(g.x-before.x,g.y-before.y)*g.data.tileSize,
    speed:Math.hypot(g.x-before.x,g.y-before.y)*g.data.tileSize/(g.time-before.time),
    safe:!g.blocked(g.x,g.y)&&g.spaceId==='floor-1'}},before);
  status.keyboardMovement=move;
  check('Actual W key moves safely at walking speed',move.safe&&move.meters>.8&&Math.abs(move.speed-2.6)<.3,move);

  await page.evaluate(()=>{const g=escapeQA.game;g.warp(1,24,9);g.stamina=100;g.exhausted=false});
  const sprintBefore=await page.evaluate(()=>({x:escapeQA.game.x,y:escapeQA.game.y,time:escapeQA.game.time,stamina:escapeQA.game.stamina}));
  await page.keyboard.down('ShiftLeft');await page.keyboard.down('KeyW');
  try{await page.waitForFunction(t=>escapeQA.game.time>t+.4,sprintBefore.time,{timeout:20000})}
  finally{await page.keyboard.up('KeyW');await page.keyboard.up('ShiftLeft')}
  const sprint=await page.evaluate(before=>{const g=escapeQA.game;return {
    speed:Math.hypot(g.x-before.x,g.y-before.y)*g.data.tileSize/(g.time-before.time),
    staminaBefore:before.stamina,staminaAfter:g.stamina,safe:!g.blocked(g.x,g.y)}},sprintBefore);
  status.keyboardSprint=sprint;
  check('Actual Shift W sprints safely and spends stamina',sprint.safe&&Math.abs(sprint.speed-4.7)<.35&&sprint.staminaAfter<sprint.staminaBefore-5,sprint);

  const stamina=await page.evaluate(()=>{
    const q=escapeQA,g=q.game;q.setManual(true);g.autoStairsEnabled=false;
    let strip;
    for(const f of g.data.floors){
      for(let y=0;y<f.height&&!strip;y++)for(let x=0;x<f.width-5&&!strip;x++){
        if([0,1,2,3,4].every(i=>!g.blocked(x+i,y,f))&&g.segmentClear(x,y,x+4,y,f))strip={floor:f.number,x,y};
      }
      if(strip)break;
    }
    if(!strip)throw Error('No collision-clear authored corridor for stamina smoke');
    g.warp(strip.floor,strip.x+1,strip.y);g.stamina=100;g.exhausted=false;let direction=1;
    const run=(seconds,sprint=false,moving=false)=>{
      for(let elapsed=0;elapsed<seconds-1e-8;elapsed+=.025){
        if(g.x>=strip.x+3)direction=-1;if(g.x<=strip.x+1)direction=1;
        q.tick(Math.min(.025,seconds-elapsed),moving?direction:0,0,sprint);
      }
    };
    run(5.2,true,true);const empty={stamina:g.stamina,exhausted:g.exhausted,running:g.isRunning};
    run(.2,true,true);const locked={speed:g.speed,exhausted:g.exhausted,running:g.isRunning};
    run(1.8);const partial={stamina:g.stamina,exhausted:g.exhausted};
    run(1.5);const recovered={stamina:g.stamina,exhausted:g.exhausted};
    run(.1,true,true);const resumed={speed:g.speed,running:g.isRunning};
    q.setManual(false);return {strip,empty,locked,partial,recovered,resumed,safe:!g.blocked(g.x,g.y)};
  });
  status.stamina=stamina;
  check('Moving sprint exhausts stamina and held Shift falls back to walking',stamina.safe&&stamina.empty.exhausted&&stamina.empty.stamina<1&&!stamina.empty.running&&stamina.locked.exhausted&&!stamina.locked.running&&Math.abs(stamina.locked.speed-2.6)<.05,stamina);
  check('Recovery delay and quarter-stamina lock prevent instant endless sprint',stamina.partial.stamina>0&&stamina.partial.stamina<25&&stamina.partial.exhausted&&stamina.recovered.stamina>=25&&!stamina.recovered.exhausted,stamina);
  check('Recovered stamina permits sprinting again',stamina.resumed.running&&Math.abs(stamina.resumed.speed-4.7)<.05,stamina.resumed);

  status.roomVisits=[];
  for(const kind of ['broadcast','studentRestroom']){
    const room=await page.evaluate(kind=>{
      const g=escapeQA.game,m=g.data.roomMaps.find(m=>m.kind===kind&&m.sourceFloor===1);
      if(!m)throw Error('Required 1F room missing: '+kind);
      g.autoStairsEnabled=false;g.warp(1,m.returnX,m.returnY);g.lastDoorTransition=-10;escapeQA.render();
      return {id:m.id,kind,returnX:m.returnX,returnY:m.returnY,hidden:escapeQA.inspectSpace().visibleFixtureRoomIds.length===0};
    },kind);
    check(`${kind} hidden before E`,room.hidden);
    await page.keyboard.press('KeyE');
    await page.waitForFunction(id=>escapeQA.game.roomId===id&&!escapeQA.game.pendingDoor,room.id,{timeout:20000});
    const inside=await page.evaluate(()=>{const g=escapeQA.game;escapeQA.render(0);return {
      id:g.roomId,label:g.map.label,safe:!g.blocked(g.x,g.y),...escapeQA.inspectSpace()}});
    check(`Actual E enters only ${kind}`,inside.id===room.id&&inside.safe&&inside.activeIds.join()===room.id&&inside.visibleFixtureRoomIds.join()===room.id,inside);
    if(kind==='studentRestroom'){
      const mirrorRoute=await page.evaluate(()=>{
        const g=escapeQA.game,z=g.map.interiorZones.find(z=>z.label.includes('큰 거울'));
        if(!z)return {reached:false};
        const reached=g.walkTo(z.x,z.y);escapeQA.render(0);
        // Match the normal follow camera's settled position at the mirror.
        escapeQA.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);
        escapeQA.render(0);return {reached,zone:z.label,safe:!g.blocked(g.x,g.y)};
      });
      check('Reach the large student restroom mirror',mirrorRoute.reached&&mirrorRoute.safe,mirrorRoute);
      status.mirror=await page.evaluate(()=>{escapeQA.render(0);return escapeQA.inspectMirrors()});
      const active=status.mirror.mirrors.filter(m=>m.visible&&m.roomId===room.id);
      check('Only the current large mirror is visible',status.mirror.activeSpaceId===room.id&&active.length===1,status.mirror);
      check('Large mirror renders the actual explorer',active.length===1&&active.every(m=>m.renderedFrames>0&&m.textureWidth>=128&&m.textureHeight>=128&&m.actorInReflection&&m.actorInsideSurface),active);
      status.mirrorPixels=await page.evaluate(()=>escapeQA.checkMirrorPixels());
      check('Explorer changes real reflection pixels',status.mirrorPixels.roomId===room.id&&status.mirrorPixels.changedPixels>10&&status.mirrorPixels.maximumDelta>2,status.mirrorPixels);
      try{await page.screenshot({path:'web-mirror.png',timeout:15000})}
      catch(e){status.warnings.push('Optional screenshot: '+e.message)}
    }
    check(`${kind} exit approach reachable`,await page.evaluate(()=>{const g=escapeQA.game;return g.walkTo(g.map.exitX,g.map.exitY+1)}));
    await page.waitForFunction(()=>escapeQA.game.time-escapeQA.game.lastDoorTransition>.7,null,{timeout:20000});
    await page.keyboard.press('KeyE');
    await page.waitForFunction(()=>!escapeQA.game.roomId&&!escapeQA.game.pendingDoor,null,{timeout:20000});
    const returned=await page.evaluate(room=>{const g=escapeQA.game;escapeQA.render(0);return {
      space:g.spaceId,safe:!g.blocked(g.x,g.y),correctPosition:Math.hypot(g.x-room.returnX,g.y-room.returnY)<.01,
      hidden:escapeQA.inspectSpace().visibleFixtureRoomIds.length===0}},room);
    status.roomVisits.push({kind,id:room.id,inside,returned});
    check(`Actual E returns from ${kind} to its 1F doorway`,returned.space==='floor-1'&&returned.safe&&returned.correctPosition&&returned.hidden,returned);
  }

  const followSetup=await page.evaluate(()=>{
    const q=escapeQA,g=q.game,p=q.pursuit;q.setManual(true);g.reset();p.reset();q.audio.resetEvents();g.autoStairsEnabled=false;
    const f=g.data.floors[0],r=f.rooms.find(r=>r.label==='빈교실 3');
    const [dx,dy]=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing];
    g.warp(1,r.doorX+dx,r.doorY+dy);g.lastDoorTransition=-10;
    const candidates=p.grid(f).nodes.filter(n=>{const d=Math.hypot(n.x-g.x,n.y-g.y)*g.data.tileSize;
      return d>=3&&d<=6&&g.segmentClear(n.x,n.y,g.x,g.y,f,p.radius)});
    if(!candidates.length)throw Error('No real corridor approach for public door pursuit');
    candidates.sort((a,b)=>Math.hypot(a.x-g.x,a.y-g.y)-Math.hypot(b.x-g.x,b.y-g.y));
    p.place(f.id,candidates[0].x,candidates[0].y,'chase');p.graceUntil=g.time+100;
    q.render(0);return {roomId:r.interiorId,sourceId:f.id,before:p.inspect(),door:q.game.nearbyDoor()?.label};
  });
  await page.keyboard.press('KeyE');
  const following=await page.evaluate(setup=>{
    const q=escapeQA,g=q.game,p=q.pursuit;
    for(let i=0;i<11;i++)q.tick(.025);
    const entered=g.spaceId===setup.roomId,notInstant=p.spaceId===setup.sourceId,entryTime=g.time;
    const origin={x:g.x,y:g.y},far=p.grid(g.map).nodes.filter(n=>{
      const d=Math.hypot(n.x-origin.x,n.y-origin.y)*g.data.tileSize;return d>=5&&d<=8&&g.path(n.x,n.y);
    });
    far.sort((a,b)=>Math.hypot(a.x-origin.x,a.y-origin.y)-Math.hypot(b.x-origin.x,b.y-origin.y));
    if(!far.length)throw Error('No reachable room observation point');g.warpRoom(g.roomId,far[0].x,far[0].y);
    let safe=true,delay=null;
    for(let t=0;t<8;t+=.025){q.tick(.025);safe&&=!g.blocked(p.x,p.y,g.spaces.get(p.spaceId),p.radius);
      if(p.spaceId===g.spaceId){delay=g.time-entryTime;break;}}
    q.render(0);q.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);q.render(.016);
    return {entered,notInstant,delay,safe,enemy:p.inspect(),playerSpace:g.spaceId,visible:q.pursuerVisual.root.visible,
      active:q.inspectSpace().activeIds,audio:q.audio.inspect(),model:q.pursuerVisual.inspect()};
  },followSetup);
  status.doorPursuit=following;
  check('Actual E enters a room before the pursuer arrives',following.entered&&following.notInstant,following);
  check('Pursuer approaches and follows the actual used door after a delay',following.enemy.spaceId===followSetup.roomId&&following.delay>=1.2&&following.delay<8&&following.enemy.transfers===1&&following.enemy.failedTransfers===0,following);
  check('Following pursuer lands safely and renders only in the active room',following.safe&&following.visible&&following.active.join()===followSetup.roomId,following);
  await page.waitForTimeout(180);
  const chaseAudio=await page.evaluate(()=>escapeQA.audio.inspect());status.chaseAudio=chaseAudio;
  check('Real pursuit produces urgent sound and follower door sounds',chaseAudio.chaseBeats>0&&chaseAudio.chaseGain>.02&&chaseAudio.rms>1e-7&&chaseAudio.enemyDoorOpen>0&&chaseAudio.enemyDoorClose>0,chaseAudio);
  try{await page.screenshot({path:'web-play.png',timeout:15000})}
  catch(e){status.warnings.push('Optional chaser screenshot: '+e.message)}
  const caught=await page.evaluate(()=>{
    const q=escapeQA,g=q.game,p=q.pursuit;p.place(g.spaceId,g.x,g.y,'chase');p.graceUntil=p.catchGraceUntil=-1;
    q.tick(.025);q.render(0);q.setManual(false);return {caught:g.caught,enemy:p.inspect(),overlay:!document.getElementById('caught').hidden};
  });
  check('Actual contact stops play and shows the caught screen',caught.caught&&caught.enemy.caught&&caught.overlay,caught);
  await page.click('#restart');
  const restarted=await page.evaluate(()=>({caught:escapeQA.game.caught,stamina:escapeQA.game.stamina,spaceId:escapeQA.game.spaceId,
    enemy:escapeQA.pursuit.inspect(),overlay:!document.getElementById('caught').hidden,blocked:escapeQA.game.blocked(escapeQA.game.x,escapeQA.game.y)}));
  check('Actual Restart restores stamina and safe independent pursuit',!restarted.caught&&restarted.stamina===100&&restarted.spaceId==='floor-1'&&!restarted.overlay&&!restarted.blocked&&restarted.enemy.enabled&&restarted.enemy.initial.distanceFromPlayer>=15,restarted);
  check('Public browser has no runtime errors',status.errors.length===0&&status.consoleErrors.length===0);
  status.browserURL=page.url();
  status.ok=true;
}

let watchdog;
try{
  await Promise.race([verify(),new Promise((_,reject)=>{watchdog=setTimeout(()=>reject(Error('Public smoke test exceeded 185 seconds')),185000)})]);
}catch(error){status.error=error.message;status.ok=false}
finally{
  clearTimeout(watchdog);status.utc=new Date().toISOString();
  fs.writeFileSync('published-status.json',JSON.stringify(status,null,2)+'\n');
  if(browser)await Promise.race([browser.close(),new Promise(resolve=>setTimeout(resolve,5000))]).catch(()=>{});
  console.log(JSON.stringify({ok:status.ok,publicURL:status.publicURL,checks:status.checks.length,error:status.error||null}));
  process.exit(status.ok?0:1);
}
