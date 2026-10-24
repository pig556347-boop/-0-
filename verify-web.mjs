// External Chromium proof for the isolated SchoolEscape web branch. QA setup
// positions the real player; collection, inventory and victory use real input.
import fs from 'node:fs';
import crypto from 'node:crypto';
import {chromium} from 'playwright';
const sourceCommit=process.env.PUBLIC_GAME_COMMIT||process.env.GITHUB_SHA;
const workflowCommit=process.env.GITHUB_SHA,sourceBranch=process.env.GITHUB_REF_NAME;
const repository=process.env.GITHUB_REPOSITORY;
const publicURL=`https://rawcdn.githack.com/${repository}/${sourceCommit}/index.html`;
const status={ok:false,utc:new Date().toISOString(),sourceCommit,workflowCommit,sourceBranch,publicURL,
 kind:'External public Chromium smoke test; independent browser game, not Unity WebGL',checks:[],errors:[],consoleErrors:[],warnings:[],externalContentNoticeConfirmed:false};
const newRoomLabels={1:['창고','대회의실','설비관리실','세미나실'],2:['준비실 1','자료실','창고'],3:['교직원 휴게실','학부모상담실','메인회의실','교장실','중앙통제실']};
let browser,page,gameReady=false;
function check(name,ok,actual){status.checks.push({name,passed:!!ok,...(actual===undefined?{}:{actual})});if(!ok)throw Error(name);}
async function visitRoom(floor,label){
 const setup=await page.evaluate(({floor,label})=>{
  const q=escapeQA,g=q.game,base=g.data.floors.find(f=>f.number===floor),door=base.rooms.find(r=>r.label===label),room=g.spaces.get(door.interiorId);
  if(!room)throw Error('Missing authored room '+floor+' '+label);
  q.setManual(true);g.autoStairsEnabled=false;g.warp(floor,room.returnX,room.returnY);for(let i=0;i<28;i++)q.tick(.025);q.render(0);
  const root=q.floorRoots.find(r=>r.userData.spaceId===base.id),visual=root.getObjectByName('Closed door: '+label);
  return {floor,label,id:room.id,sourceId:base.id,safe:!g.blocked(g.x,g.y),near:g.nearbyDoor()?.targetId,
   before:q.inspectSpace(),hidden:!q.floorRoots.find(r=>r.userData.spaceId===room.id).visible,closedPosition:visual.position.toArray()};
 },{floor,label});
 check('New room stays hidden behind its closed door: '+floor+'F '+label,setup.safe&&setup.near===setup.id&&setup.hidden&&setup.before.activeIds.join()===setup.sourceId,setup);
 await page.keyboard.press('KeyE');check('Real E begins entry: '+label,await page.evaluate(id=>escapeQA.game.pendingDoor?.targetId===id,setup.id));
 const entered=await page.evaluate(()=>{const q=escapeQA;for(let i=0;i<12;i++)q.tick(.025);q.render(0);return {spaceId:q.game.spaceId,safe:!q.game.blocked(q.game.x,q.game.y),view:q.inspectSpace(),furnishings:q.game.map.fixtures.length}});
 check('New interior is furnished and shown alone: '+label,entered.spaceId===setup.id&&entered.safe&&entered.furnishings>0&&entered.view.activeIds.join()===setup.id&&entered.view.minimapSpaceId===setup.id&&entered.view.visibleFixtureRoomIds.join()===setup.id,entered);
 const start=await page.evaluate(()=>{const q=escapeQA;q.setManual(false);return {x:q.game.x,y:q.game.y,time:q.game.time}});
 await page.keyboard.down('KeyW');try{await page.waitForFunction(t=>escapeQA.game.time>t+.3,start.time)}finally{await page.keyboard.up('KeyW')}
 const walked=await page.evaluate(p=>{const q=escapeQA,g=q.game;q.setManual(true);return {spaceId:g.spaceId,distance:Math.hypot(g.x-p.x,g.y-p.y)*g.data.tileSize,safe:!g.blocked(g.x,g.y)}},start);
 check('Real W explores the new room safely: '+label,walked.spaceId===setup.id&&walked.distance>.3&&walked.safe,walked);
 await page.evaluate(()=>{const q=escapeQA,g=q.game,m=g.map;g.warpRoom(m.id,m.exitX,m.exitY+1);for(let i=0;i<28;i++)q.tick(.025);q.render(0)});
 await page.keyboard.press('KeyE');const returned=await page.evaluate(label=>{
  const q=escapeQA,g=q.game;for(let i=0;i<12;i++)q.tick(.025);q.render(0);
  const root=q.floorRoots.find(r=>r.userData.spaceId===g.spaceId),visual=root.getObjectByName('Closed door: '+label);
  return {spaceId:g.spaceId,safe:!g.blocked(g.x,g.y),pending:!!g.pendingDoor,view:q.inspectSpace(),closedPosition:visual?.position.toArray(),near:g.nearbyDoor()?.label};
 },label);
 check('Real E returns and closes the door: '+label,returned.spaceId===setup.sourceId&&returned.safe&&!returned.pending&&returned.view.activeIds.join()===setup.sourceId&&returned.view.minimapSpaceId===setup.sourceId&&returned.near===label&&returned.closedPosition?.every((n,i)=>Math.abs(n-setup.closedPosition[i])<1e-6),returned);
 return {setup,entered,walked,returned};
}
async function pickup(itemId){
 const setup=await page.evaluate(id=>{
  const q=escapeQA,g=q.game,s=q.items,item=s.worldItems.find(i=>i.itemId===id&&!i.picked);if(!item)throw Error('Missing authored item '+id);
  const f=g.spaces.get(item.spaceId),source=item.sourceId?s.searchById.get(item.sourceId):null;let chosen;
  for(const p of source?source.approaches:[item]){
   if(f.isRoom)g.warpRoom(f.id,p.x,p.y);else g.warp(f.number,p.x,p.y);
   const near=s.nearby();if(near&&(near.uid===item.uid||near.itemUid===item.uid)){chosen=p;break;}
  }
  if(!chosen)throw Error('No matching reachable interaction '+id);
  g.autoStairsEnabled=false;for(let i=0;i<14;i++)q.tick(.025);q.render(0);
  return {uid:item.uid,itemId:id,spaceId:f.id,hidden:item.hidden,sourceId:item.sourceId,key:g.nearbyDoor()||s.nearbyGate()?'KeyF':'KeyE',safe:!g.blocked(g.x,g.y)};
 },itemId);
 check('Reach real '+itemId+' interaction',setup.safe,setup);await page.keyboard.press(setup.key);
 await page.waitForFunction(uid=>escapeQA.items.worldItems.find(i=>i.uid===uid)?.picked,setup.uid);
 const got=await page.evaluate(s=>{
  const q=escapeQA;q.render(0);return {carried:q.items.inventory.some(i=>i.uid===s.uid),crafted:s.itemId.startsWith('clock-')&&q.items.inventory.some(i=>i.itemId==='compass'),
   active:q.inspectSpace().activeIds,capacity:q.items.inventory.length,copies:q.items.worldItems.filter(i=>i.uid===s.uid).length,
   isolated:q.lootVisuals.inspect().items.filter(i=>i.visible).every(i=>i.spaceId===q.game.spaceId)};
 },setup);
 check('Actual '+setup.key+' obtains '+itemId+' without duplication',(got.carried||got.crafted)&&got.capacity<=9&&got.copies===1&&got.active.join()===setup.spaceId&&got.isolated,got);return setup;
}
async function inventoryAction(id,button){
 if(!await page.evaluate(()=>escapeQA.inventoryUI.isOpen))await page.keyboard.press('KeyI');
 const index=await page.evaluate(id=>escapeQA.items.inventory.findIndex(i=>i.itemId===id),id);if(index<0)throw Error('Inventory item missing '+id);
 await page.click('#inventory-slot-'+index);await page.click(button);await page.keyboard.press('Escape');
}
async function verify(){
 check('Only the separate web branch is tested',sourceBranch==='school-escape-web',sourceBranch);
 check('Pinned commit is valid',/^[a-f0-9]{40}$/.test(sourceCommit||''));
 const manifest=JSON.parse(fs.readFileSync('build.json','utf8'));status.build=manifest;
 check('Manifest identifies completed main rooms 0.9',manifest.version==='0.9'&&manifest.featureRevision==='remaining-main-rooms-playground-plan-2026-10-24',manifest.featureRevision);
 const response=await fetch(publicURL,{signal:AbortSignal.timeout(30000)}),html=Buffer.from(await response.arrayBuffer());
 status.http={status:response.status,contentType:response.headers.get('content-type'),bytes:html.length,sha256:crypto.createHash('sha256').update(html).digest('hex')};
 check('Public CDN returns HTTP 200',response.status===200,status.http.status);
 check('Public CDN serves executable HTML',/^text\/html\b/i.test(status.http.contentType||''),status.http.contentType);
 check('Public HTML matches the tested build',status.http.sha256===manifest.htmlSha256,status.http.sha256);
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH,headless:true,args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 page=await browser.newPage({viewport:{width:1280,height:800},deviceScaleFactor:1});page.setDefaultTimeout(20000);
 page.on('pageerror',e=>status.errors.push(e.message));page.on('console',m=>{if(gameReady&&m.type()==='error')status.consoleErrors.push(m.text())});
 await page.goto(publicURL+'?qa',{waitUntil:'domcontentloaded',timeout:45000});
 const open=page.getByRole('button',{name:'Open the page',exact:true}).or(page.getByRole('link',{name:'Open the page',exact:true})).first();
 if(await open.count()){await open.click();status.externalContentNoticeConfirmed=true;}
 await page.waitForFunction(()=>window.escapeLoaded&&window.escapeQA,null,{timeout:45000});gameReady=true;
 check('Public game has a usable Start button',await page.locator('#begin').isVisible());
 const initial=await page.evaluate(()=>{
  const q=escapeQA,g=q.game,s=q.items,m=g.data.roomMaps;
  return {seed:s.seed,enemy:q.pursuit.inspect(),visual:q.pursuerVisual.inspect(),audio:q.audio.inspect(),capacity:s.capacity,maxEquipped:s.maxEquipped,
   itemCount:s.worldItems.length,keys:s.worldItems.filter(i=>i.itemId.startsWith('key-')).map(i=>({id:i.itemId,hidden:i.hidden,spaceId:i.spaceId})),
   rooms:m.length,revision:g.data.revision,libraries:m.filter(m=>m.kind==='library').map(m=>m.sourceFloor),
   roomCoverage:g.data.floors.flatMap(f=>f.rooms.map(r=>{const interior=g.spaces.get(r.interiorId);return {floor:f.number,label:r.label,id:r.interiorId||'',kind:interior?.kind||'',sourceFloor:interior?.sourceFloor,fixtures:interior?.fixtures.length||0,fixtureKinds:new Set((interior?.fixtures||[]).map(o=>o.kind)).size,solidFixtures:(interior?.fixtures||[]).filter(o=>o.solid).length}})),
   adminDesks:m.filter(m=>m.kind==='administration').flatMap(m=>m.fixtures).filter(o=>o.kind==='admin-desk').length,mirrors:m.flatMap(m=>m.fixtures).filter(o=>o.kind==='mirror-surface').length};
 });status.initial=initial;
 check('Caretaker uses a designated safe random spawn',initial.enemy.enabled&&initial.enemy.spawnCandidates.length>=3&&initial.enemy.spawnCandidates.some(p=>p.id===initial.enemy.initial.spawnId&&p.spaceId===initial.enemy.initial.spaceId)&&(initial.enemy.initial.spaceId!=='floor-1'||initial.enemy.initial.distanceFromPlayer>=15)&&initial.visual.targetHeight===2.4,initial.enemy);
 check('Inventory has nine carry and one worn slots',initial.capacity===9&&initial.maxEquipped===1);
 check('Run has thirteen real pickups and exactly A B C',initial.itemCount===13&&initial.keys.map(k=>k.id).sort().join()==='key-a,key-b,key-c',initial.keys);
 check('Keys mix furniture and ground locations',initial.keys.some(k=>k.hidden)&&initial.keys.some(k=>!k.hidden),initial.keys);
 check('All 53 rooms and existing facility fixtures remain',initial.rooms===manifest.roomCount&&initial.rooms===53&&initial.revision===manifest.revision&&initial.revision==='remaining-main-rooms-2026-10-24'&&initial.libraries.join()==='2'&&initial.adminDesks===18&&initial.mirrors===12,initial);
 check('Every main-building door has a furnished interior',initial.roomCoverage.length===53&&initial.roomCoverage.every(r=>r.id&&r.sourceFloor===r.floor&&r.fixtures>0&&r.fixtureKinds>=3&&r.solidFixtures>0),initial.roomCoverage);
 const newRooms=Object.entries(newRoomLabels).flatMap(([floor,labels])=>labels.map(label=>initial.roomCoverage.find(r=>r.floor===Number(floor)&&r.label===label)));
 check('Twelve remaining rooms are furnished on their intended floors',newRooms.length===12&&newRooms.every(r=>r?.id&&r.fixtures>0&&r.fixtureKinds>=3&&r.solidFixtures>0),newRooms);
 check('No sound starts before Start',initial.audio.contextState==='uninitialized'&&!initial.audio.unlocked,initial.audio);
 await page.click('#begin');await page.waitForFunction(()=>escapeQA.audio.inspect().contextState==='running'&&escapeQA.audio.inspect().rms>1e-7);
 status.startedAudio=await page.evaluate(()=>escapeQA.audio.inspect());check('Real Start unlocks audible ambience',status.startedAudio.unlocked&&status.startedAudio.rms>1e-7,status.startedAudio);
 await page.click('#sound-toggle');await page.waitForFunction(()=>escapeQA.audio.inspect().muted&&escapeQA.audio.inspect().masterGain<.005);
 check('Real mute button silences output',await page.evaluate(()=>document.getElementById('sound-toggle').getAttribute('aria-pressed')==='true'&&escapeQA.audio.inspect().rms<.001));
 await page.click('#sound-toggle');await page.waitForFunction(()=>!escapeQA.audio.inspect().muted&&escapeQA.audio.inspect().rms>1e-7);
 await page.click('#enemy-toggle');await page.waitForFunction(()=>!escapeQA.pursuit.enabled&&document.getElementById('enemy-toggle').getAttribute('aria-pressed')==='false');
 check('Real developer toggle disables pursuit',await page.evaluate(()=>!escapeQA.pursuit.enabled&&document.getElementById('enemy-toggle').getAttribute('aria-pressed')==='false'));
 const seedBefore=await page.evaluate(()=>escapeQA.items.seed);await page.keyboard.press('Escape');await page.click('#new-run');
 const reset=await page.evaluate(()=>({seed:escapeQA.items.seed,enabled:escapeQA.pursuit.enabled,items:escapeQA.items.worldItems.length,carry:escapeQA.items.inventory.length}));
 check('New run rerolls loot and keeps developer OFF',reset.seed!==seedBefore&&!reset.enabled&&reset.items===13&&reset.carry===0,reset);
 await page.keyboard.press('KeyI');const time=await page.evaluate(()=>escapeQA.game.time);await page.waitForTimeout(180);
 check('Real I opens nine slots and pauses time',await page.evaluate(t=>escapeQA.inventoryUI.isOpen&&document.querySelectorAll('#inventory-grid button').length===9&&escapeQA.game.time===t,time));
 await page.keyboard.press('Escape');check('Escape closes inventory',!await page.evaluate(()=>escapeQA.inventoryUI.isOpen));
 await page.evaluate(()=>{const q=escapeQA;q.game.autoStairsEnabled=false;q.game.warp(1,24,9);q.game.stamina=100;q.render(0)});
 const start=await page.evaluate(()=>({x:escapeQA.game.x,y:escapeQA.game.y,time:escapeQA.game.time}));
 await page.keyboard.down('ShiftLeft');await page.keyboard.down('KeyW');
 try{await page.waitForFunction(t=>escapeQA.game.time>t+.35,start.time)}finally{await page.keyboard.up('KeyW');await page.keyboard.up('ShiftLeft')}
 const sprint=await page.evaluate(a=>{const g=escapeQA.game;return {speed:Math.hypot(g.x-a.x,g.y-a.y)*g.data.tileSize/(g.time-a.time),stamina:g.stamina,safe:!g.blocked(g.x,g.y)}},start);
 check('Real Shift W sprints and spends stamina',sprint.safe&&Math.abs(sprint.speed-4.7)<.35&&sprint.stamina<95,sprint);await page.evaluate(()=>escapeQA.setManual(true));
 status.newRoomVisits=[];for(const [floor,label] of [[1,'설비관리실'],[2,'자료실'],[3,'교장실'],[3,'중앙통제실']])status.newRoomVisits.push(await visitRoom(floor,label));
 await page.evaluate(()=>{const q=escapeQA,e=q.items.escapeGate;q.game.warp(1,e.x,e.y);q.render(0)});await page.keyboard.press('KeyE');
 check('Real gate E cannot win without three keys',await page.evaluate(()=>!escapeQA.game.won&&escapeQA.items.nearbyGate().missing.length===3));status.pickups=[];
 status.pickups.push(await pickup('vitamin-water'));await inventoryAction('vitamin-water','#inventory-use');
 const boost=await page.evaluate(()=>{const q=escapeQA,g=q.game;g.warp(1,24,9);q.tick(.025,1,0,false);const boosted=g.speed;for(let i=0;i<84;i++)q.tick(.025);q.tick(.025,1,0,false);return {boosted,after:g.speed,remaining:g.speedBoostUntil-g.time,safe:!g.blocked(g.x,g.y)};});
 check('Real drink UI boosts movement thirty percent for two seconds',boost.safe&&Math.abs(boost.boosted-3.38)<.03&&Math.abs(boost.after-2.6)<.03&&boost.remaining<0,boost);
 status.pickups.push(await pickup('chocolate-bar'));await page.evaluate(()=>{escapeQA.game.stamina=3;escapeQA.game.exhausted=true});await inventoryAction('chocolate-bar','#inventory-use');
 check('Real chocolate UI fills stamina and clears exhaustion',await page.evaluate(()=>escapeQA.game.stamina===100&&!escapeQA.game.exhausted));
 const dropped=await pickup('chocolate-bar');await inventoryAction('chocolate-bar','#inventory-drop');
 const loose=await page.evaluate(uid=>{const q=escapeQA,i=q.items.worldItems.find(i=>i.uid===uid);q.render(0);return {picked:i.picked,hidden:i.hidden,sourceId:i.sourceId,safe:!q.game.blocked(i.x,i.y),carried:q.items.inventory.some(i=>i.uid===uid)}},dropped.uid);
 check('Real drop UI preserves a safe loose item identity',!loose.picked&&!loose.hidden&&!loose.sourceId&&!loose.carried&&loose.safe,loose);
 const recovered=await pickup('chocolate-bar');check('Real pickup recovers the same dropped item',recovered.uid===dropped.uid,{dropped:dropped.uid,recovered:recovered.uid});
 for(const id of ['clock-second-hand','clock-minute-hand','clock-body'])status.pickups.push(await pickup(id));
 const crafted=await page.evaluate(()=>({inventory:escapeQA.items.inventory,compass:escapeQA.items.compass()}));status.compass=crafted;
 check('Three picked clock parts automatically become one compass',crafted.inventory.filter(i=>i.itemId==='compass').length===1&&!crafted.inventory.some(i=>i.itemId.startsWith('clock-'))&&crafted.compass.active,crafted);
 status.pickups.push(await pickup('smartwatch'));await inventoryAction('smartwatch','#inventory-equip');
 check('Real equip UI uses the separate watch slot',await page.evaluate(()=>escapeQA.items.equipped?.itemId==='smartwatch'&&!escapeQA.items.inventory.some(i=>i.itemId==='smartwatch')));
 await page.click('#enemy-toggle');const watch=await page.evaluate(()=>{
  const q=escapeQA,g=q.game,p=q.pursuit;g.warp(1,24,9);const f=g.map,nodes=p.grid(f).nodes;
  const choose=(lo,hi)=>nodes.find(n=>{const d=Math.hypot(n.x-g.x,n.y-g.y)*g.data.tileSize;return d>=lo&&d<=hi});const far=choose(10,12),near=choose(2,3);if(!far||!near)throw Error('No authored watch sensor positions');
  const play=point=>{p.place(f.id,point.x,point.y,'patrol');for(let i=0;i<30;i++)q.updateAudio(.05);return q.audio.inspect()};return {far:play(far),near:play(near)};
 });await page.waitForTimeout(180);watch.signal=await page.evaluate(()=>escapeQA.audio.inspect());status.watch=watch;
 check('Worn watch heartbeat accelerates near a real enemy',watch.near.watchBpm>watch.far.watchBpm&&watch.far.watchBpm>0&&watch.signal.watchBeats>0&&watch.signal.rms>1e-7,watch);
 await page.click('#enemy-toggle');check('Developer OFF removes watch threat heartbeat',await page.evaluate(()=>escapeQA.audio.inspect().watchBpm===0&&!escapeQA.pursuit.enabled));
 const first=crafted.compass.targetItemId;
 for(const id of [first,...['key-a','key-b','key-c'].filter(id=>id!==first)]){
  status.pickups.push(await pickup(id));if(id===first)check('Picking the compass target consumes the compass',await page.evaluate(()=>!escapeQA.items.inventory.some(i=>i.itemId==='compass')&&!escapeQA.items.compass().active));
 }
 check('All three distinct real keys are held',await page.evaluate(()=>escapeQA.items.hasAllKeys()&&escapeQA.items.inventory.filter(i=>i.itemId.startsWith('key-')).length===3));
 const away=await page.evaluate(()=>{const q=escapeQA;q.game.warp(1,24,9);return q.items.tryEscape()});check('Keys cannot win away from the gate',!away.ok&&!await page.evaluate(()=>escapeQA.game.won),away);
 await page.evaluate(()=>{const q=escapeQA,e=q.items.escapeGate;q.game.warp(1,e.x,e.y);q.render(0)});await page.keyboard.press('KeyE');
 await page.waitForFunction(()=>escapeQA.game.won&&!document.getElementById('success').hidden);
 const won=await page.evaluate(()=>({won:escapeQA.game.won,escaped:escapeQA.items.escaped,seed:escapeQA.items.seed,keys:escapeQA.items.inventory.filter(i=>i.itemId.startsWith('key-')).length,active:escapeQA.inspectSpace().activeIds}));
 check('Real gate E consumes A B C and shows Escape Success',won.won&&won.escaped&&won.keys===0&&won.active.join()==='floor-1',won);
 try{await page.screenshot({path:'web-play.png',timeout:15000})}catch(e){status.warnings.push('Optional screenshot: '+e.message)}
 await page.click('#success-restart');const restarted=await page.evaluate(()=>({won:escapeQA.game.won,seed:escapeQA.items.seed,carry:escapeQA.items.inventory.length,equipped:escapeQA.items.equipped,enabled:escapeQA.pursuit.enabled,keys:escapeQA.items.worldItems.filter(i=>i.itemId.startsWith('key-')&&!i.picked).length,spaceId:escapeQA.game.spaceId}));
 check('Real victory restart rerolls loot, clears inventory and keeps OFF',restarted.seed!==won.seed&&!restarted.won&&!restarted.carry&&!restarted.equipped&&!restarted.enabled&&restarted.keys===3&&restarted.spaceId==='floor-1',restarted);
 await page.evaluate(()=>escapeQA.setManual(false));check('No public runtime or console errors',!status.errors.length&&!status.consoleErrors.length);status.browserURL=page.url();status.ok=true;
}
let watchdog;
try{await Promise.race([verify(),new Promise((_,reject)=>{watchdog=setTimeout(()=>reject(Error('Public smoke test exceeded 185 seconds')),185000)})]);}
catch(error){status.error=error.message;status.ok=false;}
finally{
 clearTimeout(watchdog);status.utc=new Date().toISOString();fs.writeFileSync('published-status.json',JSON.stringify(status,null,2)+'\n');
 if(browser)await Promise.race([browser.close(),new Promise(resolve=>setTimeout(resolve,5000))]).catch(()=>{});
 console.log(JSON.stringify({ok:status.ok,publicURL,checks:status.checks.length,error:status.error||null}));process.exit(status.ok?0:1);
}
