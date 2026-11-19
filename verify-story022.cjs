// End-to-end story navigation: controller movement + actual E/F and document clicks.
// Caretaker OFF only for the navigation route; chef chase ON. Separate enemy tests cover pursuit.
const fs=require('node:fs'),crypto=require('node:crypto'),{chromium}=require(process.env.PLAYWRIGHT_MODULE||'/workspace/escape-tools/node_modules/playwright');
const out=process.env.PROOF_DIR||'/workspace/scratch/022/walkthrough',url=process.env.PLAY_URL||'http://127.0.0.1:8841/Builds/Browser/';fs.mkdirSync(out,{recursive:true});const report={ok:false,checks:[],errors:[],visits:[],method:'Continuous movement and real UI. Caretaker OFF during navigation, giant chef ON during chase. No teleports, grants or forced completion.'};let browser,page;
function check(name,passed,actual){report.checks.push({name,passed,actual});console.log(name,passed);if(!passed)throw Error(name)}
async function move(x,y){return page.evaluate(({x,y})=>{
 const q=escapeQA,g=q.game,from=g.spaceId,nav=q.items.navigation.get(from).available;
 const drive=points=>{for(const [tx,ty] of points){let tries=0;while(Math.hypot(tx-g.x,ty-g.y)>.015&&tries++<250){const dx=tx-g.x,dy=ty-g.y,n=Math.hypot(dx,dy),v=g.exhausted||g.stamina<=0?g.data.walkSpeed:g.data.runSpeed;q.tick(Math.min(.05,n*g.data.tileSize/v),dx/n,dy/n,true);if(g.spaceId!==from)return true;if(g.caught)throw Error('Caught');}if(tries>=250)throw Error('Blocked walk '+from+' at '+g.x+','+g.y+' aiming '+tx+','+ty);}return false};
 // A fractional cabinet approach can round into furniture. Walk out to a visible
 // navigation node before asking the integer-grid pathfinder for a long route.
 const start=nav.filter(p=>g.segmentClear(g.x,g.y,p.x,p.y)).sort((a,b)=>Math.hypot(a.x-g.x,a.y-g.y)-Math.hypot(b.x-g.x,b.y-g.y))[0];if(!start)throw Error('No walkable start '+from);
 if(drive([[start.x,start.y]])){q.render(0);return{changed:true,space:g.spaceId}};
 const candidates=nav.filter(p=>g.segmentClear(p.x,p.y,x,y)).sort((a,b)=>Math.hypot(a.x-x,a.y-y)-Math.hypot(b.x-x,b.y-y));let path;
 for(const p of candidates.slice(0,20)){path=g.path(p.x,p.y);if(path){path.push([x,y]);break}}if(!path)throw Error('No path '+from+' to '+x+','+y);
 const changed=drive(path);q.render(0);return{changed,space:g.spaceId};
},{x,y})}
async function go(id){for(let attempt=0;attempt<30;attempt++){const edge=await page.evaluate(id=>{const q=escapeQA;if(q.game.spaceId===id)return null;return q.items._spaceRoute(q.game.spaceId,id)?.[0]||{error:true}},id);if(!edge)return;if(edge.error)throw Error('No connected route '+id);const result=await move(edge.x,edge.y);if(!result.changed&&edge.kind==='door'){await page.keyboard.press('KeyE');await page.evaluate(()=>{for(let n=0;n<20;n++)escapeQA.tick(.05);escapeQA.render(0)})}else if(!result.changed){await page.evaluate(()=>{const g=escapeQA.game;for(let n=0;n<80;n++)escapeQA.tick(.05);escapeQA.render(0)})}const now=await page.evaluate(()=>escapeQA.game.spaceId);report.visits.push(now);console.log('visit',now);if(now===id)return;if(now===edge.fromId)throw Error('Transition refused '+edge.label)}throw Error('Route loop '+id)}
async function openSource(id){const source=await page.evaluate(id=>escapeQA.items.searchById.get(id),id);await go(source.spaceId);for(const a of source.approaches){await move(a.x,a.y);if(await page.evaluate(id=>escapeQA.items.nearby()?.sourceId===id,id)){await page.keyboard.press('KeyF');await page.waitForFunction(()=>escapeQA.discovery.isOpen);return}}throw Error('Cannot select '+id)}
async function collect(itemId){const item=await page.evaluate(id=>escapeQA.items.worldItems.find(i=>i.itemId===id&&!i.picked),itemId);if(!item)throw Error('Missing '+itemId);if(item.hidden){await openSource(item.sourceId);const c=page.locator('[data-compartment="'+item.compartment+'"]');if(await c.count())await c.click()}else{await go(item.spaceId);await move(item.x,item.y);await page.keyboard.press('KeyF');await page.waitForFunction(()=>escapeQA.discovery.isOpen)}await page.click('[data-uid="'+item.uid+'"]');check('Collected through actual investigation '+itemId,await page.evaluate(id=>escapeQA.items.inventory.some(i=>i.itemId===id),itemId));await page.keyboard.press('KeyE')}

(async()=>{try{
 report.sha256=crypto.createHash('sha256').update(Buffer.from(await(await fetch(url)).arrayBuffer())).digest('hex');
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox','--use-angle=swiftshader','--enable-unsafe-swiftshader']});page=await browser.newPage({viewport:{width:1280,height:800}});page.setDefaultTimeout(120000);
 await page.addInitScript(seed=>{const native=crypto.getRandomValues.bind(crypto);crypto.getRandomValues=array=>{if(array instanceof Uint32Array&&array.length===1){array[0]=seed;return array}return native(array)}},Number(process.env.RUN_SEED||10365085));
 page.on('pageerror',e=>report.errors.push(e.message));page.on('console',e=>{if(e.type()==='error')report.errors.push(e.text())});
 await page.goto(url+'?qa=1');const hostedOpen=page.getByRole('button',{name:'Open the page',exact:true}).or(page.getByRole('link',{name:'Open the page',exact:true})).first();if(await hostedOpen.count())await hostedOpen.click();
 await page.waitForFunction(()=>window.escapeLoaded);await page.click('#begin');if(process.env.ENEMIES_ON!=='1')await page.click('#enemy-toggle');await page.evaluate(()=>escapeQA.setManual(true));report.seed=await page.evaluate(()=>escapeQA.game.runSeed);report.caretakerEnabled=process.env.ENEMIES_ON==='1';if(report.caretakerEnabled)report.method='Continuous controller route, actual document/UI clicks; caretaker and chef enabled throughout, no teleports, grants or forced completion.';
 check('Empty starting inventory, no testing keys or equipment',await page.evaluate(()=>escapeQA.items.inventory.length===0));
 check('Only hiding nodes remain; no minigame UI or special monsters',await page.evaluate(()=>escapeQA.subjects.nodes.every(n=>n.type==='hide')&&!document.querySelector('#scanner-view')&&!document.querySelector('#mission-screen')));
 // Explore the final box first: pure luck cannot reveal the gate key.
 const box=await page.evaluate(()=>escapeQA.progression.keySource.id);await openSource(box);
 check('Final box cannot reveal key before the story',await page.evaluate(()=>!escapeQA.items.worldItems.some(i=>i.itemId==='escape-key')));await page.keyboard.press('KeyE');
 for(const id of ['story-broadcast','story-office','story-duty','story-kitchen']){
  const source=await page.evaluate(id=>Object.values(escapeQA.progression.documents).flat().find(d=>d.id===id).sourceId,id);await openSource(source);await page.click('[data-document="'+id+'"]');
  check('Read actual document '+id,await page.evaluate(id=>escapeQA.progression.readDocuments.has(id),id));
  if(id==='story-duty')await page.screenshot({path:out+'/duty-record.png'});
  await page.keyboard.press('KeyE');
 }
 check('All documents without chef escape still produce no key',await page.evaluate(()=>!escapeQA.items.worldItems.some(i=>i.itemId==='escape-key')));
 if(!await page.evaluate(()=>escapeQA.pursuit.enabled))await page.click('#enemy-toggle');const exit=await page.evaluate(()=>({x:escapeQA.kitchen.normal.exitX,y:escapeQA.kitchen.normal.exitY+1}));await move(exit.x,exit.y);
 if(!await page.evaluate(()=>escapeQA.kitchen.active)){await page.keyboard.press('KeyE');await page.evaluate(()=>{for(let n=0;n<20;n++)escapeQA.tick(.05)})}
 check('Returning from the kitchen triggers the giant chef',await page.evaluate(()=>escapeQA.kitchen.active&&escapeQA.pursuit.enabled));
 await page.evaluate(()=>escapeQA.render(5));await page.screenshot({path:out+'/chef-chase.png'});
 await page.evaluate(()=>{const q=escapeQA,g=q.game,k=q.kitchen;for(const [x,y] of k.path.slice(1)){let count=0;while(Math.hypot(x-g.x,y-g.y)>.015&&count++<250){const dx=x-g.x,dy=y-g.y,n=Math.hypot(dx,dy);q.tick(Math.min(.05,n*g.data.tileSize/g.data.runSpeed),dx/n,dy/n,true);if(g.caught)throw Error('Chef capture during clean run')}if(count>=250)throw Error('Chase route blocked')}q.render(0)});
 await page.keyboard.press('KeyE');await page.evaluate(()=>{for(let i=0;i<20;i++)escapeQA.tick(.05);escapeQA.render(5)});
 check('Actual kitchen exit clears the chase and reveals exactly one key',await page.evaluate(()=>escapeQA.kitchen.phase==='cleared'&&escapeQA.game.spaceId==='annex-cafeteria'&&escapeQA.progression.ready&&escapeQA.items.worldItems.filter(i=>i.itemId==='escape-key').length===1));
 if(process.env.ENEMIES_ON!=='1')await page.click('#enemy-toggle');await collect('escape-key');
 // Save via the visible pause menu, then reload via the visible lobby control.
 await page.keyboard.press('Escape');await page.click('#save-run');check('Saved run button enabled immediately',await page.evaluate(()=>!document.getElementById('load-run').disabled));await page.click('#return-lobby');await page.click('#load-run');
 await page.waitForFunction(()=>window.escapeLoaded&&escapeQA.progression.ready);await page.click('#begin');await page.evaluate(()=>escapeQA.setManual(true));
 check('Reload retains all four clues, chef completion and exactly one carried key',await page.evaluate(()=>['story-broadcast','story-office','story-duty','story-kitchen'].every(id=>escapeQA.progression.readDocuments.has(id))&&escapeQA.items.inventory.filter(i=>i.itemId==='escape-key').length===1&&escapeQA.kitchen.phase==='cleared'));
 await go('playground');await move(34,3);await page.keyboard.press('KeyE');await page.evaluate(()=>escapeQA.render(5));
 check('One key and one final E opens the gate',await page.evaluate(()=>escapeQA.game.won&&escapeQA.items.insertedKeys.length===1&&!escapeQA.items.inventory.some(i=>i.itemId==='escape-key')));
 check('Success screen is visible',await page.locator('#success').isVisible());check('No browser exceptions or resource errors',!report.errors.length,report.errors);
 report.distanceMeters=await page.evaluate(()=>escapeQA.game.distance);report.simulatedSeconds=await page.evaluate(()=>escapeQA.game.time);await page.screenshot({path:out+'/escaped.png'});report.ok=true;
}catch(e){report.error=e.stack;try{report.state=await page.evaluate(()=>({space:escapeQA.game.spaceId,x:escapeQA.game.x,y:escapeQA.game.y,door:escapeQA.game.nearbyDoor(),item:escapeQA.items.nearby(),kitchen:escapeQA.kitchen.phase,clues:[...escapeQA.progression.readDocuments]}));await page.screenshot({path:out+'/failure.png',timeout:15000})}catch{}}finally{fs.writeFileSync(out+'/report.json',JSON.stringify(report,null,2));console.log(JSON.stringify(report));await browser?.close();process.exitCode=report.ok?0:1}})();
