// Real Chromium input, rendered inventory, audible watch and complete escape.
// Explicit QA relocation sets up cases; E/F/I/buttons execute production actions.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'/workspace/escape-tools/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'..'),out=process.env.ESCAPE_RESULTS_DIR||path.join(root,'TestResults','ItemsRelease');fs.mkdirSync(out,{recursive:true});
const result={kind:'Actual SchoolEscape browser items, inventory, development toggle and escape; NOT Unity Play',passed:[],failed:[],errors:[],requests:[],samples:{},screenshots:[],utc:null};
const check=(ok,name,details)=>{(ok?result.passed:result.failed).push(name);if(details!==undefined)result.samples[name]=details;};
let browser,page,realE=0,realF=0;
const save=()=>{result.utc=new Date().toISOString();fs.writeFileSync(path.join(out,'items-browser-runtime.json'),JSON.stringify(result,null,2)+'\n');};
async function capture(name){await page.screenshot({path:path.join(out,name)});result.screenshots.push(name);save();}
async function age(seconds=.36){await page.evaluate(seconds=>{const q=escapeQA;for(let t=0;t<seconds-1e-8;t+=.025)q.tick(Math.min(.025,seconds-t));q.render(0);},seconds);}
async function bag(open){
 const current=await page.evaluate(()=>escapeQA.inventoryUI.isOpen);if(current===open)return;
 await page.keyboard.press('KeyI');await page.waitForFunction(open=>escapeQA.inventoryUI.isOpen===open,open);
}
async function select(itemId){await page.click(`#inventory-grid button[data-item-id="${itemId}"]`);}
async function pickup(itemId,uid){
 await bag(false);await age();
 const setup=await page.evaluate(({itemId,uid})=>{
  const q=escapeQA,g=q.game,it=q.items,item=it.worldItems.find(i=>!i.picked&&(uid?i.uid===uid:i.itemId===itemId));if(!item)throw Error('Missing world item '+itemId);
  const map=g.spaces.get(item.spaceId),warp=(x,y)=>map.isRoom?g.warpRoom(map.id,x,y):g.warp(map.number,x,y);
  let matched=false;
  if(item.hidden){const source=it.searchById.get(item.sourceId);for(const p of source.approaches){warp(p.x,p.y);if(it.nearby()?.sourceId===source.id){matched=true;break;}}}
  else{warp(item.x,item.y);matched=it.nearby()?.uid===item.uid;}
  if(!matched)throw Error('Unreachable pickup interaction '+item.uid);
  q.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);q.render(0);
  return{uid:item.uid,itemId:item.itemId,hidden:item.hidden,spaceId:item.spaceId,before:it.inventory.length,key:g.nearbyDoor()?'KeyF':'KeyE',gate:!!it.nearbyGate()};
 },{itemId,uid});
 if(setup.key==='KeyE')realE++;else realF++;
 await page.keyboard.press(setup.key);
 const after=await page.evaluate(uid=>{const q=escapeQA;q.render(0);return{item:q.items.worldItems.find(i=>i.uid===uid),inventory:q.items.inventory.map(i=>({...i})),action:q.items.lastAction,near:q.items.nearby()};},setup.uid);
 return{...setup,after};
}
async function reset(){await bag(false);await page.evaluate(()=>{escapeQA.reset();escapeQA.setManual(true);escapeQA.game.autoStairsEnabled=false;escapeQA.render(0);});}

(async()=>{
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 page=await browser.newPage({viewport:{width:1440,height:900},deviceScaleFactor:1});page.setDefaultTimeout(120000);
 page.on('pageerror',e=>result.errors.push(e.message));page.on('console',m=>{if(m.type()==='error')result.errors.push(m.text());});
 page.on('request',r=>{if(/^https?:/.test(r.url()))result.requests.push({url:r.url(),type:r.resourceType()});});page.on('response',r=>{if(r.status()>=400)result.errors.push('HTTP '+r.status()+' '+r.url());});
 const response=await page.goto(process.env.ESCAPE_URL||'http://127.0.0.1:8793/?qa',{waitUntil:'networkidle',timeout:120000});const bytes=await response.body();
 result.build={url:response.url(),bytes:bytes.length,sha256:crypto.createHash('sha256').update(bytes).digest('hex')};
 check(response.status()===200&&response.headers()['content-type'].includes('text/html'),'Latest artifact executes as actual HTML',result.build);
 if(process.env.ESCAPE_EXPECT_SHA256)check(result.build.sha256===process.env.ESCAPE_EXPECT_SHA256,'Executed artifact matches expected SHA-256');
 await page.waitForFunction(()=>window.escapeLoaded&&window.escapeQA?.items&&window.escapeQA?.inventoryUI);
 const before=await page.evaluate(()=>({items:escapeQA.items.inspect(),enemy:escapeQA.pursuit.inspect(),audio:escapeQA.audio.inspect()}));
 check(before.items.worldItems.length===13&&before.items.keys.length===3,'Thirteen initial items and exactly A/B/C keys exist',before.items.keys);
 check(before.enemy.enabled&&before.enemy.initial.spawnId,'Default enemy uses a designated random spawn',before.enemy.initial);
 check(before.audio.contextState==='uninitialized','Audio still waits for actual Start click');
 await capture('items-title.png');await page.click('#begin');
 await page.waitForFunction(()=>escapeQA.audio.inspect().unlocked&&escapeQA.audio.inspect().contextState==='running');
 await page.click('#enemy-toggle');
 check(await page.evaluate(()=>!escapeQA.pursuit.enabled&&document.getElementById('enemy-toggle').getAttribute('aria-pressed')==='false'),'Actual development toggle turns pursuit OFF');
 await bag(true);const paused=await page.evaluate(()=>({t:escapeQA.game.time,x:escapeQA.game.x,y:escapeQA.game.y,px:escapeQA.pursuit.x,py:escapeQA.pursuit.y,slots:document.querySelectorAll('#inventory-grid button').length,wear:!!document.getElementById('inventory-equipped'),audio:escapeQA.audio.inspect()}));
 await page.keyboard.down('KeyW');await page.waitForTimeout(300);await page.keyboard.up('KeyW');
 const pausedAfter=await page.evaluate(()=>({t:escapeQA.game.time,x:escapeQA.game.x,y:escapeQA.game.y,px:escapeQA.pursuit.x,py:escapeQA.pursuit.y,audio:escapeQA.audio.inspect()}));
 check(paused.slots===9&&paused.wear,'Actual I key opens nine bag slots and a separate wearable slot');
 check(paused.t===pausedAfter.t&&paused.x===pausedAfter.x&&paused.y===pausedAfter.y&&paused.px===pausedAfter.px&&paused.py===pausedAfter.py,'Inventory freezes movement, pursuit and game clock',{before:paused,after:pausedAfter});
 check(pausedAfter.audio.paused&&pausedAfter.audio.effectiveGain===0,'Inventory also pauses audible output');
 await capture('items-empty-inventory.png');await page.click('#inventory-close');await page.waitForFunction(()=>!escapeQA.inventoryUI.isOpen);
 await page.evaluate(()=>{escapeQA.setManual(true);escapeQA.game.autoStairsEnabled=false;});

 // Real pickups fill all nine individual slots. No test grant/push is used.
 const fillers=await page.evaluate(()=>escapeQA.items.worldItems.filter(i=>!i.itemId.startsWith('clock-')).sort((a,b)=>(a.itemId==='smartwatch'?-1:0)-(b.itemId==='smartwatch'?-1:0)).map(i=>({uid:i.uid,itemId:i.itemId})));
 for(const item of fillers.slice(0,9)){const r=await pickup(item.itemId,item.uid);check(r.after.item.picked,'Actual interaction collects bag item '+item.uid);}
 const full=await pickup(fillers[9].itemId,fillers[9].uid);
 check(!full.after.item.picked&&full.after.inventory.length===9&&!full.after.action.ok,'Full bag refuses pickup while retaining world item',full);
 await bag(true);check(await page.locator('#inventory-count').textContent()==='9 / 9','Rendered bag count reaches its nine-item capacity');
 await select('smartwatch');await page.click('#inventory-equip');
 check(await page.evaluate(()=>escapeQA.items.inventory.length===8&&escapeQA.items.equipped?.itemId==='smartwatch'&&document.getElementById('inventory-equipped').dataset.itemId==='smartwatch'),'Actual Equip button moves watch into one separate worn slot');
 await bag(false);const tenth=await pickup(fillers[9].itemId,fillers[9].uid);check(tenth.after.item.picked&&tenth.after.inventory.length===9,'Wearing the watch frees a bag slot for the tenth carried-or-worn item');
 await bag(true);await page.click('#inventory-equipped');await page.click('#inventory-equip');
 check(await page.evaluate(()=>escapeQA.items.inventory.length===9&&escapeQA.items.equipped?.itemId==='smartwatch'&&!escapeQA.items.lastAction.ok),'Actual full-bag Unequip refuses safely without losing watch');
 const dropId=await page.evaluate(()=>escapeQA.items.inventory.find(i=>i.itemId==='vitamin-water')?.uid);await select('vitamin-water');await page.click('#inventory-drop');
 const dropped=await page.evaluate(uid=>({world:escapeQA.items.worldItems.find(i=>i.uid===uid),bag:escapeQA.items.inventory.length,action:escapeQA.items.lastAction}),dropId);
 check(dropped.action.ok&&!dropped.world.picked&&!dropped.world.hidden&&dropped.bag===8,'Actual Drop button creates a safe visible ground pickup',dropped);
 await bag(false);const repick=await pickup('vitamin-water',dropId);check(repick.after.item.picked&&repick.after.inventory.filter(i=>i.uid===dropId).length===1,'Actual dropped item can be picked back up without duplication');
 await bag(true);await capture('items-full-inventory-watch.png');await bag(false);

 // Production restart starts a fresh run while preserving the user's dev toggle.
 const oldSeed=await page.evaluate(()=>escapeQA.items.seed);await reset();const fresh=await page.evaluate(()=>({seed:escapeQA.items.seed,enemy:escapeQA.pursuit.enabled,bag:escapeQA.items.inventory.length,wear:escapeQA.items.equipped}));
 check(fresh.seed!==oldSeed&&!fresh.enemy&&fresh.bag===0&&fresh.wear===null,'Production restart rerolls items and preserves development OFF',fresh);
 for(const id of ['clock-second-hand','clock-minute-hand','clock-body']){const r=await pickup(id);check(r.after.item.picked,'Actual search or pickup acquires '+id);}
 const crafted=await page.evaluate(()=>({inventory:escapeQA.items.inventory,compass:escapeQA.items.compass(),hidden:document.getElementById('compass-hud').hidden}));
 check(crafted.inventory.length===1&&crafted.inventory[0].itemId==='compass'&&crafted.compass.active&&!crafted.hidden,'Three real clock pickups auto-craft one active direction compass',crafted);
 check(Number.isFinite(crafted.compass.angle)&&['key','door','stairs'].includes(crafted.compass.via),'Compass gives a finite direction via the actual map connection');
 for(const id of ['vitamin-water','chocolate-bar','smartwatch']){const r=await pickup(id);check(r.after.item.picked,'Actual interaction acquires usable item '+id);}
 await bag(true);await select('vitamin-water');await page.click('#inventory-use');
 const boost=await page.evaluate(()=>({remaining:escapeQA.game.speedBoostUntil-escapeQA.game.time,used:escapeQA.items.lastAction,water:escapeQA.items.inventory.filter(i=>i.itemId==='vitamin-water').length}));
 check(boost.used.ok&&Math.abs(boost.remaining-2)<.001&&boost.water===0,'Actual Use button consumes vitamin water and grants a two-second effect',boost);
 await bag(false);
 const speed=await page.evaluate(()=>{const q=escapeQA,g=q.game;g.warp(1,24,9);q.tick(.05,1,0,false);const boosted=g.speed;for(let t=0;t<2.05;t+=.025)q.tick(.025);q.tick(.05,1,0,false);q.render(0);return{boosted,expired:g.speed};});
 check(Math.abs(speed.boosted-2.6*1.3)<.001&&Math.abs(speed.expired-2.6)<.001,'Vitamin changes real production movement by thirty percent and expires',speed);
 await page.evaluate(()=>{escapeQA.game.stamina=0;escapeQA.game.exhausted=true;escapeQA.render(0);});await bag(true);await select('chocolate-bar');await page.click('#inventory-use');
 check(await page.evaluate(()=>escapeQA.game.stamina===100&&!escapeQA.game.exhausted&&escapeQA.items.lastAction.ok),'Actual chocolate Use restores all stamina immediately');
 await select('smartwatch');await page.click('#inventory-equip');check(await page.evaluate(()=>escapeQA.items.equipped?.itemId==='smartwatch'),'Actual watch Equip enables wearable slot in fresh run');
 await capture('items-compass-and-watch.png');await bag(false);

 // Compare watch response with the same idle player and actual equipped item.
 await page.click('#enemy-toggle');
 const watch=await page.evaluate(()=>{
  const q=escapeQA,g=q.game,p=q.pursuit,m=g.data.roomMaps.find(m=>m.kind==='library');g.warpRoom(m.id,m.spawnX,m.spawnY);
  const nodes=p.grid(m).nodes,choose=(lo,hi)=>nodes.find(n=>{const d=Math.hypot(n.x-g.x,n.y-g.y)*g.data.tileSize;return d>=lo&&d<=hi;});
  const far=choose(11,13),near=choose(2.5,3.3);if(!far||!near)throw Error('No safe near/far watch test positions');
  const place=n=>{p.place(m.id,n.x,n.y,'patrol');p.direction={x:n.x-g.x,y:n.y-g.y};p.patrolWait=g.time+100;p.graceUntil=p.catchGraceUntil=g.time+100;q.tick(.05);q.render(0);return q.audio.inspect();};
  const distant=place(far),close=place(near);return{distant,close,player:{x:g.x,y:g.y},spaceId:g.spaceId};
 });
 check(watch.distant.watchEquipped&&watch.close.watchEquipped&&watch.close.watchBpm>watch.distant.watchBpm&&watch.close.watchBpm>100,'Equipped smartwatch heartbeat becomes faster as the monster approaches',watch);
 const heartbeat=await page.evaluate(async()=>{
  const q=escapeQA,a=q.audio,c=a.context;if(!globalThis.MediaRecorder||!c)throw Error('MediaRecorder audio verification unavailable');
  const restore=[];for(const node of a.continuous||[])if(node.gain){restore.push([node.gain,node.gain.value]);node.gain.setValueAtTime(0,c.currentTime);}
  for(const node of [a.ambient,a.breathBus,a.chaseBus])if(node?.gain){restore.push([node.gain,node.gain.value]);node.gain.setValueAtTime(0,c.currentTime);}
  for(const source of a.voices)try{source.stop()}catch{}
  await new Promise(resolve=>setTimeout(resolve,100));const target=c.createMediaStreamDestination();a.master.connect(target);
  const chunks=[],recorder=new MediaRecorder(target.stream,{mimeType:'audio/webm;codecs=opus'}),done=new Promise(resolve=>{recorder.onstop=resolve;});recorder.ondataavailable=e=>{if(e.data.size)chunks.push(e.data);};recorder.start();
  const before=a.inspect().watchBeats;for(let t=0;t<1.2;t+=.025)q.tick(.025);const after=a.inspect();
  await new Promise(resolve=>setTimeout(resolve,600));recorder.stop();await done;
  const encoded=await new Blob(chunks).arrayBuffer(),decoded=await c.decodeAudioData(encoded.slice(0));let sum=0,max=0,count=0;
  for(let ch=0;ch<decoded.numberOfChannels;ch++)for(const v of decoded.getChannelData(ch)){sum+=v*v;max=Math.max(max,Math.abs(v));count++;}
  a.master.disconnect(target);for(const [gain,value]of restore)gain.setValueAtTime(value,c.currentTime);
  return{beforeBeats:before,afterBeats:after.watchBeats,encodedBytes:encoded.byteLength,seconds:decoded.duration,rms:Math.sqrt(sum/Math.max(1,count)),peak:max};
 });
 check(heartbeat.afterBeats>heartbeat.beforeBeats&&heartbeat.rms>1e-6&&heartbeat.peak>1e-5,'Actual recorded smartwatch heartbeat is audible with ambience and movement silenced',heartbeat);
 await page.click('#enemy-toggle');await age(.1);check(await page.evaluate(()=>!escapeQA.pursuit.enabled&&escapeQA.audio.inspect().watchBpm===0),'Development OFF also disables proximity heartbeat warnings');

 const gateBefore=await page.evaluate(()=>{const q=escapeQA,g=q.game,e=q.items.escapeGate;g.warp(1,e.x,e.y);q.render(0);return q.items.nearbyGate();});
 await page.keyboard.press('KeyE');check(await page.evaluate(()=>!escapeQA.game.won&&!escapeQA.items.lastAction.ok),'Actual gate E refuses escape before keys are collected',gateBefore);
 const targets=[];for(const id of ['key-a','key-b','key-c']){
  const r=await pickup(id);check(r.after.item.picked,'Actual interaction acquires exit '+id);targets.push(r);
  if(id===crafted.compass.targetItemId)check(await page.evaluate(()=>!escapeQA.items.inventory.some(i=>i.itemId==='compass')&&!escapeQA.items.compass().active&&document.getElementById('compass-hud').hidden),'Collecting its target key consumes the compass and hides its HUD');
 }
 check(await page.evaluate(()=>escapeQA.items.hasAllKeys()),'A/B/C are all present in actual bag before escape');
 await page.evaluate(()=>{const q=escapeQA,g=q.game,e=q.items.escapeGate;g.warp(1,e.x,e.y);q.camera.position.set((g.x-24)*g.data.tileSize,16,-(g.y-19)*g.data.tileSize+10.4);q.render(0);});
 await page.keyboard.press('KeyE');
 check(await page.evaluate(()=>escapeQA.game.won&&escapeQA.items.escaped&&!document.getElementById('success').hidden),'Actual gate E uses all three keys and displays escape success');
 check(await page.evaluate(()=>!escapeQA.items.inventory.some(i=>i.itemId.startsWith('key-'))),'Successful escape consumes A/B/C together');
 await capture('items-escape-success.png');
 const winSeed=await page.evaluate(()=>escapeQA.items.seed);await page.click('#success-restart');
 const restart=await page.evaluate(()=>({won:escapeQA.game.won,seed:escapeQA.items.seed,bag:escapeQA.items.inventory.length,equipped:escapeQA.items.equipped,enemy:escapeQA.pursuit.enabled,successHidden:document.getElementById('success').hidden,blocked:escapeQA.game.blocked(escapeQA.game.x,escapeQA.game.y)}));
 check(!restart.won&&restart.seed!==winSeed&&restart.bag===0&&restart.equipped===null&&restart.successHidden&&!restart.blocked&&!restart.enemy,'Actual success Restart creates a fresh safe run and preserves development OFF',restart);
 await page.reload({waitUntil:'networkidle'});await page.waitForFunction(()=>window.escapeLoaded&&window.escapeQA?.items);
 check(await page.evaluate(()=>!escapeQA.pursuit.enabled&&document.getElementById('enemy-toggle').getAttribute('aria-pressed')==='false'),'Development preference also persists through page reload');
 check(realE>0,'Real E interactions were exercised',{E:realE,F:realF});
 if(process.env.ESCAPE_EXPECT_SELF_CONTAINED==='1')check(result.requests.every(r=>r.type==='document'),'Self-contained build makes no external asset requests',result.requests);
 check(result.errors.length===0,'No browser runtime, missing asset or console errors',result.errors);result.stage='complete';save();
 console.log(JSON.stringify({passed:result.passed.length,failed:result.failed,errors:result.errors,build:result.build,screenshots:result.screenshots,utc:result.utc},null,2));
 await browser.close();if(result.failed.length||result.errors.length)process.exitCode=1;
})().catch(async error=>{result.fatalError=error.stack||error.message;if(page)try{result.failureState=await page.evaluate(()=>({space:escapeQA.game.spaceId,position:[escapeQA.game.x,escapeQA.game.y],time:escapeQA.game.time,inventory:escapeQA.items.inspect(),enemy:escapeQA.pursuit.inspect(),audio:escapeQA.audio.inspect(),paused:!document.getElementById('pause').hidden,inventoryOpen:escapeQA.inventoryUI.isOpen}));await capture('items-failure.png');}catch{}save();console.error(error);await browser?.close();process.exitCode=1;});
