const { chromium } = require('playwright');
const fs = require('fs');
const { PNG } = require('pngjs');
(async () => {
  const browser = await chromium.launch({headless:true,args:['--no-sandbox','--enable-unsafe-swiftshader']});
  try {
    fs.mkdirSync('hud-test-results',{recursive:true});
    const executions=[];
    for (const [width,height] of [[1920,1080],[1280,720],[1024,768],[2560,1080]]) {
      const page = await browser.newPage({viewport:{width,height}});
      const errors=[];let passed=false;
      page.on('console', msg=>{const s=msg.text();if(s.includes('HUD_TEST_PASS')){passed=true;executions.push({width,height,checks:Number(s.match(/checks=(\d+)/)[1])});console.log("::notice::"+s)}if(s.includes('MOTION_METRICS'))console.log('::notice::'+s);if(/HUD_TEST_FAIL|Exception|Can't add component|Not allowed to access/.test(s))errors.push(s)});
      page.on('pageerror',err=>errors.push(String(err)));
      await page.goto('http://127.0.0.1:8780/HUDTest/');
      // Default Unity template uses a fixed canvas. Resize the actual game canvas as the browser host would.
      await page.evaluate(()=>{const canvas=document.querySelector('canvas');canvas.style.width='100vw';canvas.style.height='100vh';canvas.style.position='fixed';canvas.style.top='0';canvas.style.left='0';});
      for(let i=0;i<180 && !passed && errors.length===0;i++)await page.waitForTimeout(1000);
      await page.screenshot({path:`hud-test-results/${width}x${height}.png`});
      if(!passed||errors.length)throw Error(`HUD runtime test failed ${width}x${height}: ${errors.join('\n')}`);
      await page.close();
    }
    for(const pose of ['turn','impact','ready']) {
      const page=await browser.newPage({viewport:{width:1280,height:720}});
      let ready=false;const errors=[];
      page.on('console',msg=>{if(msg.text().includes('HOOK_PREVIEW_READY'))ready=true;if(/Exception|HUD_TEST_FAIL/.test(msg.text()))errors.push(msg.text())});
      page.on('pageerror',e=>errors.push(String(e)));
      await page.goto('http://127.0.0.1:8780/HUDTest/?hookPose='+pose);
      for(let i=0;i<90&&!ready&&!errors.length;i++)await page.waitForTimeout(1000);
      if(!ready||errors.length)throw Error('Hook pose preview failed '+pose+': '+errors.join(' '));
      await page.screenshot({path:'hud-test-results/hook-'+pose+'.png'});
      await page.close();
    }
    for(const pose of ['ready','stride','roundhouse']) {
      const page=await browser.newPage({viewport:{width:1280,height:720}});
      let ready=false;const errors=[];
      page.on('console',msg=>{if(msg.text().includes('FIGHT_PREVIEW_READY'))ready=true;if(/Exception|HUD_TEST_FAIL/.test(msg.text()))errors.push(msg.text())});
      page.on('pageerror',e=>errors.push(String(e)));
      await page.goto('http://127.0.0.1:8780/HUDTest/?fightPose='+pose);
      for(let i=0;i<90&&!ready&&!errors.length;i++)await page.waitForTimeout(1000);
      if(!ready||errors.length)throw Error('Fighting pose failed '+pose+': '+errors.join(' '));
      await page.screenshot({path:'hud-test-results/fight-'+pose+'.png'});await page.close();
    }
    const live=await browser.newPage({viewport:{width:1920,height:1080}});
    const runtimeErrors=[];
    let playerReady=false, deongchiReady=false, hookReady=false, lastTutorialRole="", motion=null;
    live.on('console',msg=>{if(msg.text().includes('HOOK_KICK_START combo=3'))hookReady=true;});
    live.on('console',msg=>{const s=msg.text();if(s.startsWith('MOTION_LIVE '))motion=JSON.parse(s.slice(12));});
    live.on('console',msg=>{if(msg.text().includes('PLAYER_MODEL_READY'))playerReady=true;if(msg.text().includes('DEONGCHI_MODEL_READY'))deongchiReady=true;if(msg.text().includes('TUTORIAL_ROLE')){lastTutorialRole=msg.text();console.log('::notice::'+lastTutorialRole)}if(/Exception|Can't add component/.test(msg.text()))runtimeErrors.push(msg.text())});
    live.on('pageerror',err=>runtimeErrors.push(String(err)));
    await live.goto('http://127.0.0.1:8780/WebGL/?motionVerification=1');
    await live.waitForTimeout(25000);
    const hostBounds=await live.locator('canvas').boundingBox();
    if(Math.abs(hostBounds.width-1920)>1||Math.abs(hostBounds.height-1080)>1)throw Error('Game canvas does not fill browser viewport');
    await live.getByRole('button',{name:'전체화면 ⛶',exact:true}).click();
    await live.waitForFunction(()=>document.fullscreenElement!==null);
    await live.evaluate(()=>document.exitFullscreen());
    await live.setViewportSize({width:1280,height:720});
    await live.waitForTimeout(500);
    const resized=await live.locator('canvas').boundingBox();
    if(Math.abs(resized.width-1280)>1||Math.abs(resized.height-720)>1)throw Error('Game canvas does not resize with viewport');
    await live.setViewportSize({width:1920,height:1080});
    await live.locator('canvas').click();
    await live.keyboard.down('d');await live.waitForTimeout(800);
    await live.screenshot({path:'hud-test-results/motion-walk.png'});await live.keyboard.up('d');
    if(!motion||Math.abs(motion.cameraSize-6.5)>.01||motion.sx<.1||motion.sx>.9||motion.sy<.15||motion.sy>.8)throw Error('Zoomed follow camera lost player: '+JSON.stringify(motion));
    await live.keyboard.down('Space');await live.waitForTimeout(100);await live.keyboard.up('Space');await live.waitForTimeout(1000);
    await live.keyboard.down('c');await live.waitForTimeout(300);
    await live.screenshot({path:'hud-test-results/motion-crouch.png'});
    await live.keyboard.up('c');await live.waitForTimeout(300);
    // A screenshot can take several seconds on software WebGL. Approach using
    // observed world positions, rather than assuming held-key wall time equals distance.
    async function approachTarget() {
      if(!motion)throw Error('Read-only motion telemetry missing');
      for(let i=0;i<60;i++) {
        const dx=motion.ex-motion.px,dz=motion.ez-motion.pz;
        const key=Math.abs(dz)>.25?(dz>0?'w':'s'):(dx>0?'d':'a');
        if(Math.abs(dx)<1.3&&Math.abs(dx)>.8&&Math.abs(dz)<.25) {
          await live.keyboard.down(key);await live.waitForTimeout(40);await live.keyboard.up(key);
          await live.waitForTimeout(150);return;
        }
        await live.keyboard.down(key);await live.waitForTimeout(80);await live.keyboard.up(key);
        await live.waitForTimeout(150);
      }
      throw Error('Could not approach tutorial target: '+JSON.stringify(motion));
    }
    await approachTarget();
    await live.keyboard.down('j');await live.waitForTimeout(80);await live.keyboard.up('j');
    await live.waitForTimeout(100);await live.screenshot({path:'hud-test-results/motion-kick.png'});
    // Re-enter a real three-hit sequence after the evidence capture delay.
    await live.waitForTimeout(2000);
    await approachTarget();
    for(let i=0;i<12&&!lastTutorialRole.includes('step=Heavy');i++) {
      await live.keyboard.down('j');await live.waitForTimeout(80);await live.keyboard.up('j');
      await live.waitForTimeout(140);
    }
    await live.waitForTimeout(500);
    await live.screenshot({path:'hud-test-results/production-hud.png'});
    const pixels=PNG.sync.read(await live.locator('canvas').screenshot());
    const buckets=new Map();
    for(let i=0;i<pixels.data.length;i+=4){
      const key=[pixels.data[i],pixels.data[i+1],pixels.data[i+2]].map(x=>Math.floor(x/16)).join(',');
      buckets.set(key,(buckets.get(key)||0)+1);
    }
    const large=[...buckets.values()].filter(n=>n>pixels.width*pixels.height*.025);
    if(large.length<3)throw Error('Production canvas remains blank/loading; expected classroom and HUD');
    if(runtimeErrors.length)throw Error('Production scene runtime errors: '+runtimeErrors.join(' '));
    if(!playerReady)throw Error('Production player model was not loaded');
    if(!deongchiReady)throw Error('Tutorial Move/Jump/Crouch progression did not load Deongchi: '+lastTutorialRole);
    if(!lastTutorialRole.includes('step=Heavy'))throw Error('Real basic combo did not complete the three-hit tutorial: '+lastTutorialRole);
    if(!hookReady)throw Error('Real third combo hit did not start the spinning hook animation');
    fs.mkdirSync('build/WebGL/verification',{recursive:true});
    fs.writeFileSync('hud-test-results/summary.json',JSON.stringify({commit:process.env.GITHUB_SHA,executions,playerReady,deongchiReady,hookReady,lastTutorialRole,runtimeErrors,fullscreen:true,resize:true,combatCameraZoom:true},null,2));
    for(const name of ['production-hud.png','motion-walk.png','motion-crouch.png','motion-kick.png','hook-turn.png','hook-impact.png','hook-ready.png','fight-ready.png','fight-stride.png','fight-roundhouse.png','summary.json'])
      fs.copyFileSync('hud-test-results/'+name,'build/WebGL/verification/'+name);
    console.log('::notice::Production player model loaded; move/crouch/jump/attack input exercised with no runtime exceptions');
    await live.close();
  } finally {await browser.close();}
})().catch(e=>{
  fs.mkdirSync('hud-test-results',{recursive:true});
  fs.writeFileSync('hud-test-results/failure.json',JSON.stringify({commit:process.env.GITHUB_SHA,error:String(e)},null,2));
  console.error("::error::"+String(e).replace(/\n/g," "));process.exit(1)
});
