const { chromium } = require('playwright');
const fs = require('fs');
(async () => {
  const browser = await chromium.launch({headless:true,args:['--no-sandbox','--enable-unsafe-swiftshader']});
  try {
    fs.mkdirSync('hud-test-results',{recursive:true});
    for (const [width,height] of [[1920,1080],[1280,720],[1024,768],[2560,1080]]) {
      const page = await browser.newPage({viewport:{width,height}});
      const errors=[];let passed=false;
      page.on('console', msg=>{const s=msg.text();if(s.includes('HUD_TEST_PASS')){passed=true;console.log("::notice::"+s)}if(/HUD_TEST_FAIL|Exception|Can't add component/.test(s))errors.push(s)});
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
    live.on('console',msg=>{if(/Exception|Can't add component/.test(msg.text()))runtimeErrors.push(msg.text())});
    live.on('pageerror',err=>runtimeErrors.push(String(err)));
    await live.goto('http://127.0.0.1:8780/WebGL/');
    await live.waitForTimeout(25000);
    await live.locator('canvas').click();
    await live.keyboard.down('w');await live.waitForTimeout(500);await live.keyboard.up('w');
    await live.screenshot({path:'hud-test-results/production-hud.png'});
    if(runtimeErrors.length)throw Error('Production scene runtime errors: '+runtimeErrors.join(' '));
    console.log('::notice::Production scene loaded with no runtime exceptions; keyboard input exercised');
    await live.close();
  } finally {await browser.close();}
})().catch(e=>{console.error("::error::"+String(e).replace(/\n/g," "));process.exit(1)});
