// End-to-end story navigation: controller movement + actual E/F and document clicks.
// Caretaker OFF only for the navigation route; chef chase ON. Separate enemy tests cover pursuit.
const fs=require('node:fs'),crypto=require('node:crypto'),{chromium}=require(process.env.PLAYWRIGHT_MODULE||'/workspace/escape-tools/node_modules/playwright');
const out=process.env.PROOF_DIR||'/workspace/scratch/024/walkthrough',url=process.env.PLAY_URL||'http://127.0.0.1:8841/Builds/Browser/';fs.mkdirSync(out,{recursive:true});const report={ok:false,checks:[],errors:[],visits:[],method:'Continuous movement and real UI. Caretaker ON during opening, OFF during later route, giant chef ON during chase. No teleports, grants or forced completion.'};let browser,page;
function check(name,passed,actual){report.checks.push({name,passed,actual});console.log(name,passed);if(!passed)throw Error(name)}
async function move(x,y){return page.evaluate(({x,y})=>{
 const q=escapeQA,g=q.game,from=g.spaceId,nav=q.items.navigation.get(from).available;
 const drive=points=>{for(const [tx,ty] of points){let tries=0;while(Math.hypot(tx-g.x,ty-g.y)>.015&&tries++<250){const dx=tx-g.x,dy=ty-g.y,n=Math.hypot(dx,dy),danger=q.pursuit.enabled&&q.pursuit.spaceId===from&&Math.hypot(q.pursuit.x-g.x,q.pursuit.y-g.y)*g.data.tileSize<13,sprint=danger||g.stamina>g.maxStamina*.7,v=sprint&&!g.exhausted&&g.stamina>0?g.data.runSpeed:g.data.walkSpeed;q.tick(Math.min(.05,n*g.data.tileSize/v),dx/n,dy/n,sprint);if(g.spaceId!==from)return true;if(g.caught)throw Error('Caught');}if(tries>=250)throw Error('Blocked walk '+from+' at '+g.x+','+g.y+' aiming '+tx+','+ty);}return false};
 // A fractional cabinet approach can round into furniture. Walk out to a visible
 // navigation node before asking the integer-grid pathfinder for a long route.
 const start=nav.filter(p=>g.segmentClear(g.x,g.y,p.x,p.y)).sort((a,b)=>Math.hypot(a.x-g.x,a.y-g.y)-Math.hypot(b.x-g.x,b.y-g.y))[0];if(!start)throw Error('No walkable start '+from);
 if(drive([[start.x,start.y]])){q.render(0);return{changed:true,space:g.spaceId}};
 const candidates=nav.filter(p=>g.segmentClear(p.x,p.y,x,y)).sort((a,b)=>Math.hypot(a.x-x,a.y-y)-Math.hypot(b.x-x,b.y-y));let path;
 for(const p of candidates.slice(0,20)){path=g.path(p.x,p.y);if(path){path.push([x,y]);break}}if(!path)throw Error('No path '+from+' to '+x+','+y);
 const changed=drive(path);q.render(0);return{changed,space:g.spaceId};
},{x,y})}
async function go(id){for(let attempt=0;attempt<30;attempt++){const edge=await page.evaluate(id=>{const q=escapeQA;if(q.game.spaceId===id)return null;return q.items._spaceRoute(q.game.spaceId,id)?.[0]||{error:true}},id);if(!edge)return;if(edge.error)throw Error('No connected route '+id);let result;try{result=await move(edge.x,edge.y)}catch(e){if(!String(e).includes("Caught"))throw e;await page.evaluate(()=>escapeQA.render(0));check("Caretaker capture preserves checkpoint and supports retry",await page.evaluate(()=>escapeQA.survival.lives>0));report.captures=(report.captures||0)+1;await page.click("#revive");continue;}if(!result.changed&&edge.kind==='door'){await page.keyboard.press('KeyE');await page.evaluate(()=>{for(let n=0;n<20;n++)escapeQA.tick(.05);escapeQA.render(0)})}else if(!result.changed){await page.evaluate(()=>{const g=escapeQA.game;for(let n=0;n<80;n++)escapeQA.tick(.05);escapeQA.render(0)})}const now=await page.evaluate(()=>escapeQA.game.spaceId);report.visits.push(now);console.log('visit',now);if(now===id)return;if(now===edge.fromId){console.log('Retry door',await page.evaluate(()=>({busy:escapeQA.opening.busy,dialogue:escapeQA.opening.dialogueOpen,inventory:escapeQA.inventoryUI.isOpen,pending:escapeQA.game.pendingDoor,time:escapeQA.game.time,last:escapeQA.game.lastDoorTransition,hidden:escapeQA.game.hidden,focus:document.activeElement?.id})));await dismissStory();await page.evaluate(()=>{escapeQA.inventoryUI.close();for(let n=0;n<20;n++)escapeQA.tick(.05)});}}throw Error('Route loop '+id)}
async function openSource(id){const source=await page.evaluate(id=>escapeQA.items.searchById.get(id),id);await go(source.spaceId);for(const a of source.approaches){await move(a.x,a.y);if(await page.evaluate(id=>escapeQA.items.nearby()?.sourceId===id,id)){await page.keyboard.press('KeyF');await page.waitForFunction(()=>escapeQA.discovery.isOpen);return}}throw Error('Cannot select '+id)}
async function collect(itemId){const item=await page.evaluate(id=>escapeQA.items.worldItems.find(i=>i.itemId===id&&!i.picked),itemId);if(!item)throw Error('Missing '+itemId);if(item.hidden){await openSource(item.sourceId);const c=page.locator('[data-compartment="'+item.compartment+'"]');if(await c.count())await c.click()}else{await go(item.spaceId);await move(item.x,item.y);await page.keyboard.press('KeyF');await page.waitForFunction(()=>escapeQA.discovery.isOpen)}await page.click('[data-uid="'+item.uid+'"]');check('Collected through actual investigation '+itemId,await page.evaluate(id=>escapeQA.items.inventory.some(i=>i.itemId===id),itemId));await page.keyboard.press('KeyE')}

async function dismissStory(){for(let n=0;n<30;n++){await page.evaluate(()=>escapeQA.storyUI.update(.05,true));if(!await page.locator('#story-dialogue').isVisible())return;await page.click('#story-next');}throw Error('Dialogue did not close')}
async function openingRoute(){
 check('Renamed title',await page.title().then(t=>t.includes('이스케이프 투 데이')));
 check('Dialogue freezes movement and gameplay audio',await page.evaluate(()=>{const q=escapeQA,x=q.game.x,y=q.game.y;q.tick(.05,1,0,true);q.updateAudio(.01);return q.game.x===x&&q.game.y===y&&q.audio.paused}));
 check('Wake inside classroom',await page.evaluate(()=>escapeQA.game.spaceId==='room-f1-99'));
 await page.evaluate(()=>escapeQA.render(5));await page.screenshot({path:out+'/awakening.png'});fs.writeFileSync(out+'/protagonist.png',Buffer.from((await page.evaluate(()=>escapeQA.storyUI.portrait)).split(',')[1],'base64'));await dismissStory();
 await move(9,2);await page.keyboard.press('KeyE');check('Classroom door locked and protagonist reacts',await page.evaluate(()=>escapeQA.opening.doorTried&&escapeQA.game.spaceId==='room-f1-99'&&!escapeQA.game.pendingDoor));await dismissStory();
 await move(7,3);await page.keyboard.press('KeyE');check('Window requires its clue',await page.evaluate(()=>!escapeQA.opening.escaped));
 const memo=await page.evaluate(()=>escapeQA.opening.memoSource.id);await openSource(memo);await page.click('[data-document="opening-window-note"]');await page.keyboard.press('KeyE');await dismissStory();
 // Save the classroom checkpoint and restore through the UI.
 await page.keyboard.press('Escape');await page.click('#save-run');await page.click('#return-lobby');await page.click('#load-run');await page.waitForFunction(()=>window.escapeLoaded);await page.click('#begin');await page.evaluate(()=>escapeQA.setManual(true));
 check('Classroom save preserves clue and door state without replaying awakening',await page.evaluate(()=>escapeQA.opening.doorTried&&!escapeQA.opening.escaped&&escapeQA.progression.readDocuments.has('opening-window-note')&&escapeQA.opening.queue.length===0));
 await move(7,3);await page.keyboard.press('KeyE');check('Window exits into first floor and starts protected sighting',await page.evaluate(()=>escapeQA.game.spaceId==='floor-1'&&escapeQA.opening.sighting>0&&escapeQA.opening.protected));
 await page.evaluate(()=>{for(let i=0;i<25;i++)escapeQA.storyUI.update(.05,true);escapeQA.render(5)});await page.screenshot({path:out+'/caretaker-glimpse.png'});
 await page.evaluate(()=>{for(let i=0;i<65;i++)escapeQA.storyUI.update(.05,true)});await dismissStory();check('Sighting completes without losing a life',await page.evaluate(()=>escapeQA.opening.seen&&escapeQA.survival.lives===5&&!escapeQA.game.caught));
 check('Bathrooms open but main rooms and upper floor locked',await page.evaluate(()=>escapeQA.progression.travel('room-f1-9')&&escapeQA.progression.travel('room-f1-1')&&!escapeQA.progression.travel('room-f1-5')&&!escapeQA.progression.travel('floor-2')));
 await go('room-f1-9');await go('floor-1');await openSource('opening-board');await page.click('[data-document="opening-board-note"]');await page.screenshot({path:out+'/notice-board.png'});await page.keyboard.press('KeyE');await dismissStory();
 check('Board updates guide to maintenance room',await page.evaluate(()=>escapeQA.progression.guide()[0].includes('설비관리실')));
 await openSource(await page.evaluate(()=>escapeQA.opening.panelSource.id));check('Power action requires reading procedure',await page.locator('#restore-entry-power').isDisabled());await page.click('[data-document="opening-panel-note"]');await page.click('#restore-entry-power');await dismissStory();
 check('Physical switch opens main rooms and reconnects story route',await page.evaluate(()=>escapeQA.opening.powered&&escapeQA.progression.travel('room-f1-5')&&escapeQA.progression.travel('floor-2')&&escapeQA.progression.guide()[0].includes('운동장')));
}

async function read(id){const source=await page.evaluate(id=>Object.values(escapeQA.progression.documents).flat().find(d=>d.id===id).sourceId,id);await openSource(source);await page.click('[data-document="'+id+'"]');await page.keyboard.press('KeyE');await dismissStory();}
async function act(sourceName,id){await openSource(await page.evaluate(n=>escapeQA.progression[n].id,sourceName));await page.click('[data-story-action="'+id+'"]');await dismissStory();}
(async()=>{try{
 report.sha256=crypto.createHash('sha256').update(Buffer.from(await(await fetch(url)).arrayBuffer())).digest('hex');
 report.method='Continuous controller movement through the actual map graph, real document/action/door UI; caretaker and chef ON throughout. No teleports, item grants or forced story completion.';
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox','--use-angle=swiftshader','--enable-unsafe-swiftshader']});page=await browser.newPage({viewport:{width:1280,height:800}});page.setDefaultTimeout(120000);
 await page.addInitScript(seed=>{const native=crypto.getRandomValues.bind(crypto);crypto.getRandomValues=a=>{if(a instanceof Uint32Array&&a.length===1){a[0]=seed;return a}return native(a)}},Number(process.env.RUN_SEED||10365085));
 page.on('pageerror',e=>report.errors.push(e.message));page.on('console',e=>{if(e.type()==='error')report.errors.push(e.text())});
 await page.goto(url+'?qa=1');await page.waitForFunction(()=>window.escapeLoaded);await page.click('#begin');await page.evaluate(()=>escapeQA.setManual(true));
 check('Exactly one key is physically present before reading clues',await page.evaluate(()=>escapeQA.items.worldItems.filter(i=>i.itemId==='escape-key').length===1&&!escapeQA.progression.shutterOpen));
 await openingRoute();
 await go('playground');await move(34,3);await page.keyboard.press('KeyE');check('Initial gate remains locked',await page.evaluate(()=>!escapeQA.game.won));
 await read('story-handover');await collect('duty-card');await dismissStory();
 await read('story-broadcast');await read('story-office');await read('story-duty');
 await act('centralSource','authorize-maintenance');check('Card physically inserted and consumed',await page.evaluate(()=>escapeQA.progression.authorized&&!escapeQA.progression.has('duty-card')));
 await read('story-west');await act('breakerSource','isolate-fault');await act('westSource','open-shutter');
 check('West operations actually open kitchen collision',await page.evaluate(()=>escapeQA.progression.shutterOpen&&!escapeQA.game.blocked(12,14,escapeQA.game.spaces.get('annex-kitchen'))));
 await read('story-evacuation');await read('story-kitchen');check('Reading crate does not start chef',await page.evaluate(()=>escapeQA.kitchen.phase==='idle'));
 await page.screenshot({path:out+'/handover.png'});await collect('escape-key');await dismissStory();
 check('Key pickup arms chef before gate escape becomes available',await page.evaluate(()=>escapeQA.kitchen.phase==='armed'&&escapeQA.progression.keyTaken&&!escapeQA.progression.ready));
 await page.keyboard.press('Escape');await page.click('#save-run');await page.click('#return-lobby');await page.click('#load-run');await page.waitForFunction(()=>window.escapeLoaded);await page.click('#begin');await page.evaluate(()=>escapeQA.setManual(true));
 check('Saved armed state preserves key, open shutter and caretaker ON',await page.evaluate(()=>escapeQA.progression.has('escape-key')&&escapeQA.progression.shutterOpen&&escapeQA.kitchen.phase==='armed'&&escapeQA.pursuit.enabled));
 const exit=await page.evaluate(()=>({x:escapeQA.kitchen.normal.exitX,y:escapeQA.kitchen.normal.exitY+1}));await move(exit.x,exit.y);
 if(!await page.evaluate(()=>escapeQA.kitchen.active)){await page.keyboard.press('KeyE');await page.evaluate(()=>{for(let n=0;n<20;n++)escapeQA.tick(.05)})}
 check('Returning to the entrance triggers the chef',await page.evaluate(()=>escapeQA.kitchen.active));await page.evaluate(()=>escapeQA.render(5));await page.screenshot({path:out+'/chef-chase.png'});
 await page.evaluate(()=>{const q=escapeQA,g=q.game,k=q.kitchen;for(const [x,y] of k.path.slice(1)){let count=0;while(Math.hypot(x-g.x,y-g.y)>.015&&count++<250){const dx=x-g.x,dy=y-g.y,n=Math.hypot(dx,dy);q.tick(Math.min(.05,n*g.data.tileSize/g.data.runSpeed),dx/n,dy/n,true);if(g.caught)throw Error('Caught in chef route')}if(count>=250)throw Error('Blocked chase')}q.render(0)});
 await page.keyboard.press('KeyE');await page.evaluate(()=>{for(let n=0;n<20;n++)escapeQA.tick(.05);escapeQA.render(0)});await dismissStory();
 check('Actual emergency exit completes chase',await page.evaluate(()=>escapeQA.kitchen.phase==='cleared'&&escapeQA.progression.ready&&escapeQA.game.spaceId==='annex-cafeteria'));
 await go('playground');await move(34,3);await page.keyboard.press('KeyE');await page.evaluate(()=>escapeQA.render(5));
 check('One key opens final gate and is consumed',await page.evaluate(()=>escapeQA.game.won&&escapeQA.items.insertedKeys.length===1&&!escapeQA.progression.has('escape-key')));
 check('Visited main west annex and yard', ['floor-1','west-floor-2','annex-2','playground'].every(s=>report.visits.includes(s)));
 check('No runtime or resource errors',report.errors.length===0,report.errors);report.distanceMeters=await page.evaluate(()=>escapeQA.game.distance);report.simulatedSeconds=await page.evaluate(()=>escapeQA.game.time);report.lives=await page.evaluate(()=>escapeQA.survival.lives);await page.screenshot({path:out+'/escaped.png'});report.ok=true;
}catch(e){report.error=e.stack;try{report.state=await page.evaluate(()=>({space:escapeQA.game.spaceId,x:escapeQA.game.x,y:escapeQA.game.y,enemy:escapeQA.pursuit.inspect(),kitchen:escapeQA.kitchen.phase,guide:escapeQA.progression.guide(),caught:escapeQA.game.caught}));await page.screenshot({path:out+'/failure.png'})}catch{}}finally{fs.writeFileSync(out+'/report.json',JSON.stringify(report,null,2));console.log(JSON.stringify(report));await browser?.close();process.exitCode=report.ok?0:1}})();
