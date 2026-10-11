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
      page.on('console', msg=>{const s=msg.text();if(s.includes('HUD_TEST_PASS')){passed=true;executions.push({width,height,checks:Number(s.match(/checks=(\d+)/)[1])});console.log("::notice::"+s)}if(s.includes('MOTION_METRICS'))console.log('::notice::'+s);if(/HUD_TEST_FAIL|Exception|Can't add component/.test(s))errors.push(s)});
      page.on('pageerror',err=>errors.push(String(err)));
      await page.goto('http://127.0.0.1:8780/HUDTest/');
      // Default Unity template uses a fixed canvas. Resize the actual game canvas as the browser host would.
      await page.evaluate(()=>{const canvas=document.querySelector('canvas');canvas.style.width='100vw';canvas.style.height='100vh';canvas.style.position='fixed';canvas.style.top='0';canvas.style.left='0';});
      for(let i=0;i<180 && !passed && errors.length===0;i++)await page.waitForTimeout(1000);
      await page.screenshot({path:`hud-test-results/${width}x${height}.png`});
      if(!passed||errors.length)throw Error(`HUD runtime test failed ${width}x${height}: ${errors.join('\n')}`);
      await page.close();
    }
    const live=await browser.newPage({viewport:{width:1920,height:1080}});
    const runtimeErrors=[];
    let playerReady=false, deongchiReady=false;
    live.on('console',msg=>{if(msg.text().includes('PLAYER_MODEL_READY'))playerReady=true;if(msg.text().includes('DEONGCHI_MODEL_READY'))deongchiReady=true;if(/Exception|Can't add component/.test(msg.text()))runtimeErrors.push(msg.text())});
    live.on('pageerror',err=>runtimeErrors.push(String(err)));
    await live.goto('http://127.0.0.1:8780/WebGL/');
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
    await live.keyboard.down('w');await live.waitForTimeout(800);
    await live.screenshot({path:'hud-test-results/motion-walk.png'});await live.keyboard.up('w');
    await live.keyboard.press('Space');await live.waitForTimeout(1100);
    await live.keyboard.down('c');await live.waitForTimeout(300);
    await live.screenshot({path:'hud-test-results/motion-crouch.png'});
    await live.keyboard.up('c');await live.waitForTimeout(300);
    await live.keyboard.press('j');await live.waitForTimeout(150);
    await live.screenshot({path:'hud-test-results/motion-kick.png'});await live.waitForTimeout(500);
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
    if(!deongchiReady)throw Error('Tutorial Move/Jump/Crouch progression did not load Deongchi');
    fs.mkdirSync('build/WebGL/verification',{recursive:true});
    fs.writeFileSync('hud-test-results/summary.json',JSON.stringify({commit:process.env.GITHUB_SHA,executions,playerReady,deongchiReady,runtimeErrors,fullscreen:true,resize:true},null,2));
    for(const name of ['production-hud.png','motion-walk.png','motion-crouch.png','motion-kick.png','summary.json'])
      fs.copyFileSync('hud-test-results/'+name,'build/WebGL/verification/'+name);
    console.log('::notice::Production player model loaded; move/crouch/jump/attack input exercised with no runtime exceptions');
    await live.close();
  } finally {await browser.close();}
})().catch(e=>{console.error("::error::"+String(e).replace(/\n/g," "));process.exit(1)});
