// Capture whole-room review views from the real browser renderer. This only
// changes the QA camera; the delivered game's camera and controls stay intact.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'/workspace/escape-tools/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path');
const out=process.env.ESCAPE_RESULTS_DIR||path.resolve(__dirname,'../TestResults/SupportRoomsRelease');
let browser;
(async()=>{
 fs.mkdirSync(out,{recursive:true});
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const page=await browser.newPage({viewport:{width:1440,height:1000},deviceScaleFactor:1});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(process.env.ESCAPE_URL||'http://127.0.0.1:8793/?qa',{waitUntil:'networkidle'});
 await page.waitForFunction(()=>window.escapeLoaded,null,{timeout:120000});
 await page.click('#begin');
 await page.evaluate(()=>{window.requestAnimationFrame=()=>0});
 await page.waitForTimeout(200);
 await page.addStyleTag({content:'#map-panel,footer,#toast,#door-prompt,#stair-prompt{display:none!important} header{pointer-events:none}'});
 const subjects=(process.env.ESCAPE_CAPTURE_SPACES||process.env.ESCAPE_CAPTURE_KINDS||'infirmary,administration,staff,computer').split(',');
 for(const kind of subjects){
  await page.evaluate(kind=>{
   const {game:g,camera,renderer,scene}=escapeQA,t=g.data.tileSize;
   if(kind==='corridor-corner'){
    g.warp(2,33,9);escapeQA.render(0);camera.zoom=1;
    camera.position.set((g.x-24)*t,16,-(g.y-19)*t+10.4);camera.updateProjectionMatrix();renderer.render(scene,camera);return;
   }
   const m=g.data.roomMaps.find(m=>m.id===kind)||g.data.roomMaps.find(m=>m.kind===kind);
   if(!m)throw Error('Missing overview subject: '+kind);
   const z=m.interiorZones.find(z=>z.label.includes('중앙'))||m.interiorZones.find(z=>z.label.includes('업무 책상 남쪽'))||m.interiorZones[0];
   g.warpRoom(m.id,z.x,z.y);escapeQA.render(0);
   const cx=((m.width-1)/2-24)*t,cz=-((m.height+1)/2-19)*t;
   camera.zoom=Math.min(17*(innerWidth/innerHeight)/(m.width*t+4),17/(m.height*t*.84+5));
   camera.position.set(cx,16,cz+10.4);camera.lookAt(cx,0,cz);camera.updateProjectionMatrix();renderer.render(scene,camera);
   if(escapeQA.inspectSpace().activeIds.join()!==m.id)throw Error('Overview space leak: '+m.id);
  },kind);
  await page.screenshot({path:path.join(out,`browser-overview-${kind}.png`),timeout:120000});
 }
 if(errors.length)throw Error(errors.join('\n'));
 await browser.close();console.log('Captured '+subjects.length+' isolated room overview views');
})().catch(async e=>{console.error(e);await browser?.close();process.exitCode=1});
