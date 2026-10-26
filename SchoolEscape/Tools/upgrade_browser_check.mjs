import fs from 'node:fs';import crypto from 'node:crypto';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const commit=process.env.PUBLIC_GAME_COMMIT||process.env.GITHUB_SHA,url=process.env.ESCAPE_URL||`https://rawcdn.githack.com/${process.env.GITHUB_REPOSITORY}/${commit}/index.html`,out=process.env.ESCAPE_RESULTS_DIR||'.';fs.mkdirSync(out,{recursive:true});
const report={ok:false,sourceCommit:commit||null,utc:new Date().toISOString(),kind:'Actual Chromium 0.11 input/loot/difficulty/piano/geometry validation; not Unity WebGL',checks:[],errors:[],consoleErrors:[],publicURL:url};let browser,page;
const save=()=>fs.writeFileSync(out+'/published-status.json',JSON.stringify(report,null,2)+'\n');
function check(name,passed,actual){report.checks.push({name,passed:!!passed,actual});save();if(!passed)throw Error(name)}
async function image(name){await page.evaluate(()=>{for(let i=0;i<24;i++)escapeQA.render(.05)});await page.screenshot({path:out+'/'+name+'.png'})}
async function pickup(uid){
 const s=await page.evaluate(uid=>{const q=escapeQA,g=q.game,it=q.items,i=it.worldItems.find(i=>i.uid===uid);q.inventoryUI.close();const source=it.searchById.get(i.sourceId),points=source?source.approaches:[i];let chosen;for(const p of points){g.warpSpace(i.spaceId,p.x,p.y);const n=it.nearby();if(n&&(n.uid===uid||n.itemUid===uid||(!source&&n.type==='pickup'&&Math.hypot(n.x-i.x,n.y-i.y)*g.data.tileSize<.65))){chosen=p;break}}if(!chosen)throw Error('No reachable item '+uid);q.render(0);return {uid,compartment:i.compartment||0,source:!!source,slots:source?.compartments||1,key:g.nearbyDoor()||it.nearbyGate()?'KeyF':'KeyE',before:it.inventory.length}},uid);
 await page.keyboard.press(s.key);await page.waitForFunction(()=>escapeQA.discovery.isOpen);
 check('E opens container without automatic pickup '+uid,await page.evaluate(uid=>!escapeQA.items.worldItems.find(i=>i.uid===uid).picked,uid));
 if(s.slots>1)await page.click('[data-compartment="'+s.compartment+'"]');await page.click('[data-uid="'+uid+'"]');
 check('Click takes exactly one item '+uid,await page.evaluate(uid=>escapeQA.items.worldItems.filter(i=>i.uid===uid).length===1&&escapeQA.items.worldItems.find(i=>i.uid===uid).picked,uid));await page.keyboard.press('Escape');return s;
}
async function guide(){await page.keyboard.press('KeyI');await page.click('#guidebook-open');const t=await page.locator('#discovery-body').innerText();await page.keyboard.press('Escape');await page.keyboard.press('Escape');return t}
try{
 const res=await fetch(url),body=Buffer.from(await res.arrayBuffer());report.http={status:res.status,contentType:res.headers.get('content-type'),bytes:body.length,sha256:crypto.createHash('sha256').update(body).digest('hex')};check('HTTP executable HTML',res.ok&&report.http.contentType.includes('text/html'),report.http);
 if(fs.existsSync('build.json')){const manifest=JSON.parse(fs.readFileSync('build.json'));check('Published bytes equal tested build',manifest.htmlSha256===report.http.sha256)}
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});page=await browser.newPage({viewport:{width:1440,height:900}});page.setDefaultTimeout(45000);page.on('pageerror',e=>report.errors.push(e.message));page.on('console',m=>{if(m.type()==='error')report.consoleErrors.push(m.text())});
 await page.goto(url+(url.includes('?')?'&':'?')+'qa',{waitUntil:'domcontentloaded'});const open=page.getByRole('button',{name:'Open the page',exact:true}).or(page.getByRole('link',{name:'Open the page',exact:true})).first();if(await open.count())await open.click();await page.waitForFunction(()=>window.escapeLoaded&&window.escapeQA);
 await page.selectOption('#difficulty','easy');await page.click('#begin');await page.click('#enemy-toggle');await page.evaluate(()=>escapeQA.setManual(true));
 check('Easy selection gives five lives and 200 stamina',await page.evaluate(()=>escapeQA.survival.lives===5&&escapeQA.game.maxStamina===200));
 const first=await page.evaluate(()=>{const g=escapeQA.game;return{seed:g.runSeed,layout:g.data.floors.map(f=>f.rooms.map(r=>r.interiorId)),themes:g.data.roomMaps.filter(r=>r.kind==='classroom').map(r=>r.theme)}});report.initial=first;
 const text=await guide();check('Inventory guidebook gives actual detailed keys without consuming carry slots',text.includes('열쇠 A:')&&text.includes('열쇠 B:')&&text.includes('열쇠 C:')&&await page.evaluate(()=>escapeQA.items.inventory.length===0),text);
 await page.evaluate(()=>{const q=escapeQA;q.game.warpSpace('playground',34,50);q.render(0)});await image('upgrade-school-exterior');
 check('Outdoor classroom cabinets removed; school wings and outdoor search objects exist',await page.evaluate(()=>{const f=escapeQA.game.map,k=f.fixtures.map(o=>o.kind);return!k.some(k=>k.includes('cabinet')||k.includes('locker'))&&['school-main-facade','school-west-facade','school-annex-facade','yard-trash-bin','yard-parcel-box','yard-water-station'].every(a=>k.includes(a))}));
 const cabinet=await page.evaluate(()=>{const q=escapeQA,it=q.items,s=it.searchables.find(s=>s.compartments===8&&!it.worldItems.some(i=>i.sourceId===s.id)),p=s.approaches[0];q.game.warpSpace(s.spaceId,p.x,p.y);q.render(0);return s.id});await page.keyboard.press('KeyF');check('Locker presents eight individually selectable compartments and empty slots',await page.locator('[data-compartment]').count()===8&&await page.locator('.container-empty').count()===9,cabinet);await page.click('[data-compartment="7"]');await image('upgrade-locker');await page.keyboard.press('Escape');
 const keys=await page.evaluate(()=>escapeQA.items.worldItems.filter(i=>i.itemId.startsWith('key-')).map(i=>i.uid));for(const uid of keys)await pickup(uid);
 check('All three keys collected by clicks from both regions',await page.evaluate(()=>escapeQA.items.hasAllKeys()));
 for(const mode of ['easy','normal','hard','extreme']){
  await page.evaluate(mode=>{const q=escapeQA;q.survival.setMode(mode);q.game.warp(1,24,9);q.render(0)},mode);
  const hint=await guide();check('Guide specificity '+mode,mode==='easy'?hint.includes('확보'):mode==='normal'?hint.includes('확보'):hint.includes('위치 단서가 제공되지'),hint);
  const before=await page.evaluate(()=>{const q=escapeQA;return{carry:q.items.inventory.length,lives:q.survival.lives}});
  const caught=await page.evaluate(()=>{const q=escapeQA,g=q.game,p=q.pursuit;p.setEnabled(true);p.place(g.spaceId,g.x+.4,g.y,'chase');p.direction={x:-1,y:0};p.graceUntil=p.catchGraceUntil=-100;q.tick(.05);q.render(0);return{caught:g.caught,lives:q.survival.lives,carry:q.items.inventory.length,drops:q.items.worldItems.filter(i=>!i.picked&&i.itemId.startsWith('key-')).map(i=>({space:i.spaceId,x:i.x,y:i.y})),death:q.survival.lastDeath}});
  check('Real pursuer collision removes exactly one life '+mode,caught.caught&&caught.lives===before.lives-1,caught);
  if(mode==='easy')check('Easy keeps carried keys',caught.carry===before.carry);if(mode==='normal')check('Normal drops keys at death point',caught.carry===0&&caught.drops.every(p=>p.space===caught.death.spaceId&&Math.hypot(p.x-caught.death.x,p.y-caught.death.y)<1e-8));
  if(mode==='hard')check('Hard drops remain inside radius and same map',caught.drops.every(p=>p.space===caught.death.spaceId&&Math.hypot(p.x-caught.death.x,p.y-caught.death.y)*1.25<=8.0001));
  if(mode==='extreme'){check('Extreme has no continue option',caught.lives===0&&!await page.locator('#revive').isVisible());break}
  await page.click('#revive');await page.evaluate(()=>escapeQA.pursuit.setEnabled(false));check('Actual continue safely preserves this run '+mode,await page.evaluate(()=>!escapeQA.game.caught&&escapeQA.game.spaceId==='floor-1'&&!escapeQA.game.blocked(escapeQA.game.x,escapeQA.game.y)));
  for(const uid of keys){if(await page.evaluate(uid=>!escapeQA.items.worldItems.find(i=>i.uid===uid).picked,uid))await pickup(uid)}
 }
 // A new page is a new run; the public retry button exercises that path.
 await page.click('#restart');await page.waitForFunction(()=>window.escapeLoaded&&window.escapeQA);await page.selectOption('#difficulty','normal');await page.click('#begin');await page.evaluate(()=>{escapeQA.pursuit.setEnabled(false);escapeQA.setManual(true)});
 check('New round changes room assignment while preserving every floor',await page.evaluate(old=>{const g=escapeQA.game;return g.runSeed!==old.seed&&JSON.stringify(g.data.floors.map(f=>f.rooms.map(r=>r.interiorId)))!==JSON.stringify(old.layout)&&g.data.floors.every(f=>f.rooms.every(r=>g.spaces.get(r.interiorId).sourceFloor===f.number))},first));
 const normalGuide=await guide();check('Normal gives atmospheric location clues',normalGuide.includes('열쇠 A:')&&!normalGuide.includes('번 칸을 확인'),normalGuide);
 for(const [space,x,y] of [['floor-1',24,9],['playground',34,50]]){
  const beforeNotes=await page.evaluate(({space,x,y})=>{const q=escapeQA;q.game.warpSpace(space,x,y);q.setManual(false);return q.audio.musicNotes},{space,x,y});await page.waitForFunction(n=>escapeQA.audio.musicNotes>n&&escapeQA.audio.context?.state==='running',beforeNotes);const a=await page.evaluate(()=>escapeQA.audio.inspect());check('Distinct piano theme scheduled '+space,a.musicTheme===(space==='playground'?'outdoor':'indoor')&&a.musicNotes>0&&a.contextState==='running',a);
  await page.waitForTimeout(150);const signal=await page.evaluate(()=>escapeQA.audio.inspect());check('Audio output signal '+space,signal.rms>1e-8,signal.rms);
 }
 const routes=await page.evaluate(()=>{const q=escapeQA,g=q.game;q.setManual(true);let count=0;for(const f of g.data.roomMaps){g.warpRoom(f.id,f.spawnX,f.spawnY);for(const z of f.interiorZones){if(!g.walkTo(z.x,z.y))throw Error(f.id+' '+z.label);count++}}return count});check('Every interior zone still reachable on a generated run',routes>300,routes);
 const next=await page.evaluate(()=>escapeQA.items.worldItems.filter(i=>i.itemId.startsWith('key-')).map(i=>i.uid));for(const uid of next)await pickup(uid);
 await page.evaluate(()=>{const q=escapeQA,e=q.items.escapeGate;q.game.warpSpace(e.spaceId,e.x,e.y);q.render(0)});await page.keyboard.press('KeyE');check('Real gate E still escapes after new collection flow',await page.evaluate(()=>escapeQA.game.won));await image('upgrade-victory');
 check('No runtime or console errors',!report.errors.length&&!report.consoleErrors.length);report.ok=true;
}catch(e){report.error=e.message;try{if(page)await page.screenshot({path:out+'/failure.png'})}catch{}}
finally{report.utc=new Date().toISOString();save();if(browser)await browser.close();console.log(JSON.stringify({ok:report.ok,checks:report.checks.length,error:report.error}));process.exitCode=report.ok?0:1}
