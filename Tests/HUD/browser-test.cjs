const { chromium } = require('playwright');
const fs = require('fs');
const { PNG } = require('pngjs');
(async () => {
  const browser = await chromium.launch({headless:true,args:['--no-sandbox','--enable-unsafe-swiftshader']});
  try {
    fs.mkdirSync('hud-test-results',{recursive:true});
    const minionPage=await browser.newPage({viewport:{width:1280,height:720}});
    let minionPassed=false,minionChecks=0;const minionErrors=[],minionPreviews=[],minionMetrics=[];
    minionPage.on('console',msg=>{const s=msg.text();
      if(s.includes('MINION1_LAYOUT'))minionMetrics.push(s);
      if(s.includes('MINION_PREVIEW_READY'))minionPreviews.push(s.match(/clip=(\w+)/)[1]);
      if(s.includes('MINION_TEST_PASS')){minionPassed=true;minionChecks=Number(s.match(/checks=(\d+)/)[1]);}
      if(/MINION_TEST_FAIL|Exception/.test(s))minionErrors.push(s);});
    minionPage.on('pageerror',e=>minionErrors.push(String(e)));
    await minionPage.goto('http://127.0.0.1:8780/HUDTest/?minionReview=1');
    for(let i=0;i<320&&!minionPassed&&!minionErrors.length;i++){
      while(minionPreviews.length)await minionPage.screenshot({path:'hud-test-results/minion-'+minionPreviews.shift()+'.png'});
      await minionPage.waitForTimeout(150);
    }
    if(!minionPassed||minionErrors.length){await minionPage.screenshot({path:'hud-test-results/minion-failed.png'});throw Error('Minion1 test failed: '+minionErrors.join(' ')+' '+minionMetrics.join(' '));}
    fs.writeFileSync('hud-test-results/minion1.json',JSON.stringify({commit:process.env.GITHUB_SHA,checks:minionChecks,passed:true},null,2));
    await minionPage.close();
    const punchPage=await browser.newPage({viewport:{width:1280,height:720}});
    let punchPassed=false,punchChecks=0,punchInput=false;const punchErrors=[],punchPreviews=[];
    punchPage.on('console',msg=>{const s=msg.text();
      if(s.includes('PUNCH_INPUT_READY'))punchInput=true;
      if(s.includes('PUNCH_PREVIEW_READY'))punchPreviews.push(Number(s.match(/kind=(\d+)/)[1]));
      if(s.includes('PUNCH_TEST_PASS')){punchPassed=true;punchChecks=Number(s.match(/checks=(\d+)/)[1]);}
      if(/PUNCH_TEST_FAIL|Exception/.test(s))punchErrors.push(s);});
    punchPage.on('pageerror',e=>punchErrors.push(String(e)));
    await punchPage.goto('http://127.0.0.1:8780/HUDTest/?punchReview=1');
    for(let i=0;i<320&&!punchPassed&&!punchErrors.length;i++){
      if(punchInput){punchInput=false;await punchPage.keyboard.press('i');}
      while(punchPreviews.length)await punchPage.screenshot({path:'hud-test-results/punch-kind-'+punchPreviews.shift()+'.png'});
      await punchPage.waitForTimeout(250);
    }
    if(!punchPassed||punchErrors.length)throw Error('Punch test failed: '+punchErrors.join(' '));
    fs.writeFileSync('hud-test-results/punches.json',JSON.stringify({commit:process.env.GITHUB_SHA,checks:punchChecks,passed:true},null,2));
    await punchPage.close();
    const duelPage=await browser.newPage({viewport:{width:1280,height:720}});
    let duelPassed=false,duelChecks=0;const duelErrors=[],duelEvents=[];
    duelPage.on('console',msg=>{const s=msg.text();
      if(/DUEL_WALK_PREVIEW_READY|DUEL_RETRY_READY|DUEL_VICTORY_READY|CAMPAIGN_MINION_PREVIEW_READY|CAMPAIGN_CLEANUP_PREVIEW_READY|CLEANUP_ADVANCE_READY/.test(s))duelEvents.push(s);
      if(s.includes('DUEL_TEST_PASS')){duelPassed=true;duelChecks=Number(s.match(/checks=(\d+)/)[1]);}
      if(/DUEL_TEST_FAIL|Exception/.test(s))duelErrors.push(s);});
    duelPage.on('pageerror',e=>duelErrors.push(String(e)));
    await duelPage.goto('http://127.0.0.1:8780/HUDTest/?duelReview=1');
    for(let i=0;i<400&&!duelPassed&&!duelErrors.length;i++){
      while(duelEvents.length){const event=duelEvents.shift();
        if(event.includes('CAMPAIGN_MINION'))await duelPage.screenshot({path:'hud-test-results/campaign-minions.png'});
        if(event.includes('CAMPAIGN_CLEANUP'))await duelPage.screenshot({path:'hud-test-results/campaign-cleaned.png'});
        if(event.includes('CLEANUP_ADVANCE'))await duelPage.keyboard.press('Enter');
        if(event.includes('WALK'))await duelPage.screenshot({path:'hud-test-results/deongchi-walking.png'});
        if(event.includes('RETRY')){await duelPage.screenshot({path:'hud-test-results/deongchi-defeat.png'});await duelPage.keyboard.press('r');}
        if(event.includes('VICTORY')){await duelPage.screenshot({path:'hud-test-results/deongchi-victory.png'});await duelPage.keyboard.press('Enter');}
      }
      await duelPage.waitForTimeout(250);
    }
    if(!duelPassed||duelErrors.length)throw Error('Deongchi duel failed: '+duelErrors.join(' '));
    fs.writeFileSync('hud-test-results/deongchi-duel.json',JSON.stringify({commit:process.env.GITHUB_SHA,checks:duelChecks,passed:true},null,2));
    await duelPage.close();
    const corpsePage=await browser.newPage({viewport:{width:1280,height:720}});
    let corpsePassed=false,corpsePreview=false,corpseChecks=0;const corpseErrors=[];
    corpsePage.on('console',msg=>{const s=msg.text();if(s.includes('CORPSE_PREVIEW_READY'))corpsePreview=true;
      if(s.includes('CORPSE_TEST_PASS')){corpsePassed=true;corpseChecks=Number(s.match(/checks=(\d+)/)[1]);}
      if(/CORPSE_TEST_FAIL|Exception/.test(s))corpseErrors.push(s);});
    corpsePage.on('pageerror',e=>corpseErrors.push(String(e)));
    await corpsePage.goto('http://127.0.0.1:8780/HUDTest/?corpseReview=1');
    for(let i=0;i<180&&!corpsePassed&&!corpseErrors.length;i++){
      if(corpsePreview){await corpsePage.screenshot({path:'hud-test-results/corpses-grounded.png'});corpsePreview=false;}
      await corpsePage.waitForTimeout(250);
    }
    if(!corpsePassed||corpseErrors.length)throw Error('Corpse grounding test failed: '+corpseErrors.join(' '));
    fs.writeFileSync('hud-test-results/corpses.json',JSON.stringify({commit:process.env.GITHUB_SHA,checks:corpseChecks,passed:true},null,2));
    await corpsePage.close();
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
    for(const pose of ['ready','stride','jab','cross']) {
      const page=await browser.newPage({viewport:{width:1280,height:720}});
      let ready=false;const errors=[];
      page.on('console',msg=>{if(msg.text().includes('DEONGCHI_PREVIEW_READY'))ready=true;if(/Exception|HUD_TEST_FAIL/.test(msg.text()))errors.push(msg.text())});
      page.on('pageerror',e=>errors.push(String(e)));
      await page.goto('http://127.0.0.1:8780/HUDTest/?deongchiPose='+pose);
      for(let i=0;i<90&&!ready&&!errors.length;i++)await page.waitForTimeout(1000);
      if(!ready||errors.length)throw Error('Deongchi pose failed '+pose+': '+errors.join(' '));
      await page.screenshot({path:'hud-test-results/deongchi-'+pose+'.png'});await page.close();
    }
    const authoredReviews=[];
    for(const [review,width,height] of [[1,1280,720],[2,1024,768]]) {
      const page=await browser.newPage({viewport:{width,height}});
      let passed=false;const errors=[];
      page.on('console',msg=>{
        const text=msg.text();
        if(text.includes('AUTHORED_TEST_PASS')) {
          passed=true;authoredReviews.push({review,width,height,checks:Number(text.match(/checks=(\d+)/)[1])});
          console.log('::notice::'+text);
        }
        if(/AUTHORED_TEST_FAIL|Exception|Can't add component|Not allowed to access/.test(text))errors.push(text);
      });
      page.on('pageerror',e=>errors.push(String(e)));
      await page.goto('http://127.0.0.1:8780/HUDTest/?authoredReview='+review);
      for(let i=0;i<180&&!passed&&!errors.length;i++)await page.waitForTimeout(1000);
      await page.screenshot({path:'hud-test-results/authored-review-'+review+'.png'});
      if(!passed||errors.length)throw Error('Authored motion review '+review+' failed: '+errors.join(' '));
      await page.close();
    }
    for(const pose of ['FightingIdle','RoundhouseKick','MmaKick1','CenterBlock','Jumping1','Sitting','Stunned']) {
      const page=await browser.newPage({viewport:{width:1280,height:720}});
      let ready=false;const errors=[];
      page.on('console',msg=>{if(msg.text().includes('AUTHORED_PREVIEW_READY'))ready=true;if(/AUTHORED_TEST_FAIL|Exception/.test(msg.text()))errors.push(msg.text());});
      page.on('pageerror',e=>errors.push(String(e)));
      await page.goto('http://127.0.0.1:8780/HUDTest/?authoredReview=1&authoredPose='+pose);
      for(let i=0;i<90&&!ready&&!errors.length;i++)await page.waitForTimeout(1000);
      if(!ready||errors.length)throw Error('Authored preview '+pose+' failed: '+errors.join(' '));
      await page.screenshot({path:'hud-test-results/authored-'+pose+'.png'});await page.close();
    }
    for(const phase of [0,1,5]) {
      const page=await browser.newPage({viewport:{width:1280,height:720}});
      let ready=false;const errors=[];
      page.on('console',msg=>{if(msg.text().includes('FLURRY_PREVIEW_READY'))ready=true;if(/AUTHORED_TEST_FAIL|Exception/.test(msg.text()))errors.push(msg.text());});
      page.on('pageerror',e=>errors.push(String(e)));
      await page.goto('http://127.0.0.1:8780/HUDTest/?authoredReview=1&flurryPose='+phase);
      for(let i=0;i<90&&!ready&&!errors.length;i++)await page.waitForTimeout(500);
      if(!ready||errors.length)throw Error('Flurry preview '+phase+' failed: '+errors.join(' '));
      await page.screenshot({path:'hud-test-results/flurry-'+phase+'.png'});await page.close();
    }
    const live=await browser.newPage({viewport:{width:1920,height:1080}});
    const runtimeErrors=[];
    let audioStatus="";
    const audioEvents=new Set();
    live.on('console',msg=>{if(msg.text().startsWith('GAME_AUDIO '))audioEvents.add(msg.text().slice(11).trim());if(msg.text().startsWith('GAME_AUDIO_STATE '))audioStatus=msg.text();});
    await live.addInitScript(()=>{
      window.__audioMeters=[];window.__audioSources=[];window.__audioGains=[];
      const start=AudioBufferSourceNode.prototype.start;
      AudioBufferSourceNode.prototype.start=function(...args){window.__audioSources.push(this);return start.apply(this,args);};
      const gain=BaseAudioContext.prototype.createGain;
      BaseAudioContext.prototype.createGain=function(...args){const g=gain.apply(this,args);window.__audioGains.push(g);return g;};
      const connect=AudioNode.prototype.connect;
      AudioNode.prototype.connect=function(destination,...args){
        if(destination instanceof AudioDestinationNode){
          const analyser=this.context.createAnalyser();analyser.fftSize=2048;
          window.__audioMeters.push(analyser);
          connect.call(analyser,destination);
          return connect.call(this,analyser,...args);
        }
        return connect.call(this,destination,...args);
      };
    });
    async function audioPeak(){
      let peak=0;
      for(let i=0;i<12;i++){
        peak=Math.max(peak,await live.evaluate(()=>Math.max(0,...window.__audioMeters.map(a=>{
          const b=new Float32Array(a.fftSize);a.getFloatTimeDomainData(b);
          return Math.sqrt(b.reduce((n,v)=>n+v*v,0)/b.length);
        }))));
        await live.waitForTimeout(60);
      }
      return peak;
    }
    const audioChecks={};
    let authoredReady=false, openingSitting=false, openingStanding=false, sittingSeen=false, standingSeen=false;
    let lobby=null;
    const jumpStages=new Set();
    const restrictions={};
    let playerReady=false, deongchiReady=false, hookReady=false, lastTutorialRole="", motion=null, classroomReady=false;
    live.on('console',msg=>{if(msg.text().includes('OPENING_MOTION clip=Sitting'))openingSitting=true;if(msg.text().includes('OPENING_MOTION clip=SitToStand'))openingStanding=true;if(msg.text().includes('CLASSROOM_ASSETS_READY count=5'))classroomReady=true;if(msg.text().includes('AUTHORED_PLAYER_READY'))authoredReady=true;});
    live.on('console',msg=>{if(msg.text().includes('SUPPLIED_MOTION_ONLY combo=3 clip=MmaKick1'))hookReady=true;});
    live.on('console',msg=>{const s=msg.text();if(s.startsWith('MOTION_LIVE ')){motion=JSON.parse(s.slice(12));jumpStages.add(motion.jumpStage);sittingSeen ||= motion.visualMotion==='Sitting';standingSeen ||= motion.visualMotion==='SitToStand';}});
    live.on('console',msg=>{if(msg.text().includes('PLAYER_MODEL_READY'))playerReady=true;if(msg.text().includes('DEONGCHI_MODEL_READY'))deongchiReady=true;if(msg.text().includes('TUTORIAL_ROLE')){lastTutorialRole=msg.text();console.log('::notice::'+lastTutorialRole)}if(/Exception|Can't add component|KICK_BACK_AUDIO_ERROR/.test(msg.text()))runtimeErrors.push(msg.text())});
    live.on('pageerror',err=>runtimeErrors.push(String(err)));
    live.on('console',msg=>{if(msg.text().startsWith('LOBBY_UI '))lobby=JSON.parse(msg.text().slice(9));});
    await live.goto('http://127.0.0.1:8780/WebGL/?motionVerification=1');
    async function waitLobby(predicate,label) {
      for(let i=0;i<180;i++){if(lobby&&predicate(lobby))return;await live.waitForTimeout(250);}
      throw Error('Lobby timeout '+label+': '+JSON.stringify(lobby));
    }
    async function menuClick(id) {
      await waitLobby(x=>x.buttons.some(b=>b.id===id),'button '+id);
      const expected={Language:'Language',Settings:'Settings',Credits:'Credits',Back:'Main'}[id];
      for(let attempt=0;attempt<3;attempt++) {
        const b=lobby.buttons.find(b=>b.id===id),v=live.viewportSize();
        await live.mouse.click(b.x*v.width,b.y*v.height,{delay:100});await live.waitForTimeout(650);
        if(!expected||lobby.page===expected)return;
      }
      throw Error('Menu navigation click did not open '+id);
    }
    await waitLobby(x=>x.page==='Main','initial main menu');
    if(playerReady||openingSitting)throw Error('Gameplay started before clicking Start');
    for(const [width,height] of [[1920,1080],[1280,720],[1024,768],[2560,1080]]) {
      await live.setViewportSize({width,height});await live.waitForTimeout(450);
      for(const b of lobby.buttons)if(b.x<0||b.x>1||b.y<0||b.y>1)throw Error('Lobby control outside viewport: '+b.id);
      await live.screenshot({path:`hud-test-results/lobby-${width}x${height}.png`});
    }
    await live.setViewportSize({width:1920,height:1080});await live.waitForTimeout(300);
    await menuClick('Language');await waitLobby(x=>x.page==='Language','language page');
    await menuClick('English');await menuClick('Back');
    audioChecks.lobbyRms=0;
    for(let retry=0;retry<6&&audioChecks.lobbyRms<.0001;retry++)audioChecks.lobbyRms=Math.max(audioChecks.lobbyRms,await audioPeak());
    audioChecks.contexts=await live.evaluate(()=>({meters:window.__audioMeters.length,states:(window.kickBackAudioContexts||[]).map(c=>({state:c.state,time:c.currentTime})),gains:window.__audioGains.map(g=>g.gain.value),sources:window.__audioSources.map(s=>({loop:s.loop,duration:s.buffer?.duration,rate:s.playbackRate.value,rms:s.buffer?Math.sqrt(s.buffer.getChannelData(0).reduce((n,v)=>n+v*v,0)/s.buffer.length):null}))}));
    if(audioChecks.lobbyRms<.0001)throw Error('Lobby music produced no browser audio output: '+JSON.stringify({...audioChecks.contexts,audioStatus,events:[...audioEvents]}));
    await waitLobby(x=>x.english&&x.text.includes('START GAME'),'English UI');
    await live.screenshot({path:'hud-test-results/lobby-english.png'});
    await menuClick('Settings');await waitLobby(x=>x.page==='Settings','settings page');
    for(const [id,value] of [['Master',.35],['Music',.6],['Effects',.8]]) {
      await waitLobby(x=>x.buttons.some(b=>b.id===id),'slider '+id);
      const b=lobby.buttons.find(b=>b.id===id),v=live.viewportSize();
      await live.mouse.click((b.x-b.w*.5+b.w*value)*v.width,b.y*v.height,{delay:100});await live.waitForTimeout(300);
    }
    await waitLobby(x=>Math.abs(x.master-.35)<.03&&Math.abs(x.music-.6)<.03&&Math.abs(x.effects-.8)<.03,'volume values');
    // Mute only music after the menu transient has ended, then restore it.
    {
      const b=lobby.buttons.find(b=>b.id==='Music'),v=live.viewportSize();
      // Drag the actual handle beyond the track to clamp to zero. Clicking the
      // exact float-valued left border can fall outside Unity's RectTransform.
      await live.mouse.move((b.x-b.w*.5+b.w*lobby.music)*v.width,b.y*v.height);
      await live.mouse.down();await live.waitForTimeout(250);
      await live.mouse.move((b.x-b.w*.5)*v.width-30,b.y*v.height,{steps:8});
      await live.waitForTimeout(150);await live.mouse.up();
      await waitLobby(x=>x.music<.0001,'music slider clamped to zero');
      await live.waitForTimeout(500);audioChecks.mutedRms=await audioPeak();
      if(audioChecks.mutedRms>.00001)throw Error('Music mute did not silence browser output: '+JSON.stringify({rms:audioChecks.mutedRms,volume:lobby.music,gains:await live.evaluate(()=>window.__audioGains.map(g=>g.gain.value))}));
      await live.mouse.click((b.x-b.w*.5+b.w*.6)*v.width,b.y*v.height,{delay:100});
      await live.waitForTimeout(300);
      if(await audioPeak()<.0001)throw Error('Music did not resume after restoring volume');
      audioChecks.musicMute=true;
    }
    await live.screenshot({path:'hud-test-results/lobby-settings.png'});await menuClick('Back');
    await menuClick('Credits');await waitLobby(x=>x.page==='Credits'&&x.text.includes('pig556347-boop'),'credits');
    await live.screenshot({path:'hud-test-results/lobby-credits.png'});await menuClick('Back');
    lobby=null;await live.reload();await waitLobby(x=>x.page==='Main'&&x.english&&Math.abs(x.master-.35)<.03,'saved preferences');
    await menuClick('Language');await menuClick('Korean');await menuClick('Back');
    await waitLobby(x=>!x.english&&x.text.includes('게임시작'),'Korean UI');
    if(playerReady||openingSitting)throw Error('Lobby navigation initialized gameplay');
    const lobbyChecks={responsive:true,language:true,volume:true,persistence:true,credits:true,explicitStart:true};
    await menuClick('Start');
    for(let i=0;i<20&&!openingSitting;i++)await live.waitForTimeout(500);
    await live.waitForTimeout(600);
    if(!motion||motion.visualMotion!=="Sitting")throw Error("Opening did not display the supplied sitting clip");
    await live.screenshot({path:"hud-test-results/opening-sitting.png"});
    for(let i=0;i<20&&!openingStanding;i++)await live.waitForTimeout(250);
    await live.waitForTimeout(400);
    if(motion&&motion.visualMotion==="SitToStand")await live.screenshot({path:"hud-test-results/opening-standing.png"});
    await live.waitForTimeout(25000);
    if(!openingSitting||!openingStanding||!sittingSeen||!standingSeen)throw Error("Supplied sitting and standing opening motions did not execute");
    const hostBounds=await live.locator('canvas').boundingBox();
    if(Math.abs(hostBounds.width-1920)>1||Math.abs(hostBounds.height-1080)>1)throw Error('Game canvas does not fill browser viewport');
    await live.getByRole('button',{name:'전체화면 ⛶',exact:true}).click();
    await live.waitForFunction(()=>document.fullscreenElement!==null);
    await live.evaluate(()=>document.exitFullscreen());
    await live.setViewportSize({width:1280,height:720});
    await live.waitForTimeout(500);
    const resized=await live.locator('canvas').boundingBox();
    if(Math.abs(resized.width-1280)>1||Math.abs(resized.height-720)>1)throw Error('Game canvas does not resize with viewport');
    await live.setViewportSize({width:1024,height:768});
    await live.waitForTimeout(500);
    if(!motion||Math.abs(motion.cameraSize-11.25)>.01)throw Error('Fixed room does not fit a 4:3 viewport');
    await live.setViewportSize({width:1920,height:1080});
    await live.waitForTimeout(500);
    await live.locator('canvas').click();
    if(!motion)throw Error('Camera telemetry missing');
    const cameraOrigin=[motion.cameraX,motion.cameraY,motion.cameraZ];
    await live.keyboard.down('d');await live.waitForTimeout(800);
    await live.screenshot({path:'hud-test-results/motion-walk.png'});await live.keyboard.up('d');
    if(!motion||Math.abs(motion.cameraSize-9.4)>.01||motion.sx<.1||motion.sx>.9||motion.sy<.15||motion.sy>.8)throw Error('Fixed classroom camera lost player: '+JSON.stringify(motion));
    if([motion.cameraX,motion.cameraY,motion.cameraZ].some((v,i)=>Math.abs(v-cameraOrigin[i])>.001))throw Error('Classroom camera followed player movement');
    for(let i=0;i<60&&(!motion||motion.tutorialStep!=='Jump');i++)await live.waitForTimeout(100);
    if(!motion||motion.tutorialStep!=='Jump')throw Error('Move did not advance to Jump lesson');
    const jumpOrigin=[motion.px,motion.pz];
    for(const key of ['j','k','l','i','q','c']){await live.keyboard.down(key);await live.waitForTimeout(60);await live.keyboard.up(key);}
    await live.keyboard.down('d');await live.waitForTimeout(250);await live.keyboard.up('d');await live.waitForTimeout(150);
    if(motion.state!=='IDLE'||motion.crouching||Math.hypot(motion.px-jumpOrigin[0],motion.pz-jumpOrigin[1])>.08)throw Error('Jump lesson accepted unrelated controls: '+JSON.stringify(motion));
    restrictions.jumpOnly=true;
    await live.screenshot({path:'hud-test-results/tutorial-jump-prompt.png'});
    await live.keyboard.down('Space');await live.waitForTimeout(350);
    await live.screenshot({path:'hud-test-results/motion-jump.png'});
    await live.keyboard.up('Space');await live.waitForTimeout(1000);
    for(const stage of ['Rising','Falling','Landing'])if(!jumpStages.has(stage))throw Error('Jump presentation stage missing: '+stage);
    if(!motion||motion.jumpStage!=='Grounded')throw Error('Jump did not recover to grounded presentation');
    if(motion.tutorialStep!=='Crouch')throw Error('Jump lesson did not wait for landing then advance');
    await live.keyboard.down('j');await live.waitForTimeout(100);await live.keyboard.up('j');
    if(motion.state==='ATTACK')throw Error('Crouch lesson allowed attack');
    await live.keyboard.down('c');await live.waitForTimeout(250);
    if(!motion.crouching)throw Error('Crouch lesson did not accept C');
    await live.screenshot({path:'hud-test-results/motion-crouch.png'});
    await live.waitForTimeout(700);await live.keyboard.up('c');await live.waitForTimeout(200);
    if(motion.tutorialStep!=='Basic')throw Error('Crouch hold did not advance to Basic');
    restrictions.crouchOnly=true;
    // A screenshot can take several seconds on software WebGL. Approach using
    // observed world positions, rather than assuming held-key wall time equals distance.
    async function approachTarget() {
      if(!motion)throw Error('Read-only motion telemetry missing');
      for(let i=0;i<80;i++) {
        const dx=motion.ex-motion.px,dz=motion.ez-motion.pz;
        // Keep within real strike bounds and face the target. Exact lane alignment
        // causes oscillation when software WebGL advances several physics steps per frame.
        if(Math.abs(dx)<1.35&&Math.abs(dx)>.45&&Math.abs(dz)<.6&&motion.fx*dx>0&&Math.abs(motion.fx)>.9)return;
        const key=Math.abs(dz)>=.6?(dz>0?'w':'s'):(dx>0?'d':'a');
        const distance=key==='w'||key==='s'?Math.abs(dz):Math.abs(dx);
        const pulse=distance>2?70:30;
        await live.keyboard.down(key);await live.waitForTimeout(pulse);await live.keyboard.up(key);
        await live.waitForTimeout(250);
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
    if(!lastTutorialRole.includes('step=Heavy'))throw Error('Basic combo did not complete');
    await approachTarget();await live.keyboard.down('c');await live.keyboard.down('k');
    let heavyAttack=false;
    for(let i=0;i<20;i++){await live.waitForTimeout(30);if(motion&&motion.state==='ATTACK'&&motion.visualMotion==='MmaKick1'){heavyAttack=true;break;}}
    await live.keyboard.up('k');await live.keyboard.up('c');
    if(!heavyAttack||motion.crouching)throw Error('Heavy lesson did not restrict controls correctly');
    for(let i=0;i<40&&!lastTutorialRole.includes('step=Guard');i++)await live.waitForTimeout(100);
    if(!lastTutorialRole.includes('step=Guard'))throw Error('Heavy hit did not advance to Guard');
    restrictions.heavyOnly=true;
    audioChecks.battleRms=await audioPeak();
    if(audioChecks.battleRms<.0001)throw Error('Battle produced no browser audio output');
    for(const id of ['LobbyMusic','BattleMusic','Menu','Whoosh','Hit','Success'])
      if(!audioEvents.has(id))throw Error('Missing real gameplay audio event '+id);
    audioChecks.events=[...audioEvents];
    fs.writeFileSync('hud-test-results/audio.json',JSON.stringify({commit:process.env.GITHUB_SHA,...audioChecks},null,2));
    await live.waitForTimeout(500);
    await live.screenshot({path:'hud-test-results/production-hud.png'});
    const pixels=PNG.sync.read(await live.locator('canvas').screenshot());
    // Probe unobstructed wall/board interiors in the fixed room view. This catches
    // the large triangle patches and white texture contamination seen previously.
    function roomPixel(x,y,z) {
      const sx=Math.round((.5+x/(2*9.4*(pixels.width/pixels.height)))*pixels.width);
      const sy=Math.round((.5-((y-3.2)*Math.cos(35*Math.PI/180)+z*Math.sin(35*Math.PI/180))/(2*9.4))*pixels.height);
      const i=(sy*pixels.width+sx)*4;return [...pixels.data.slice(i,i+3)];
    }
    const wallLuma=[],boardColors=[];
    for(let x=0;x<20;x++)for(let y=0;y<20;y++) {
      const c=roomPixel(-4+x/20,2+y*.8/20,8.54);
      wallLuma.push(c[0]*.2126+c[1]*.7152+c[2]*.0722);
      boardColors.push(roomPixel(-.7+x*1.4/20,2+y*.7/20,8.29));
    }
    wallLuma.sort((a,b)=>a-b);
    const wallLumaRange=wallLuma[360]-wallLuma[40];
    const boardGreenFraction=boardColors.filter(c=>c[1]>c[0]*1.15&&c[1]>c[2]*1.05).length/boardColors.length;
    if(wallLumaRange>20)throw Error('Wall still has large shading/texture patches: '+wallLumaRange);
    if(boardGreenFraction<.99)throw Error('Board interior still has white texture contamination: '+boardGreenFraction);
    const visualChecks={wallLumaRange,boardGreenFraction};
    const buckets=new Map();
    for(let i=0;i<pixels.data.length;i+=4){
      const key=[pixels.data[i],pixels.data[i+1],pixels.data[i+2]].map(x=>Math.floor(x/16)).join(',');
      buckets.set(key,(buckets.get(key)||0)+1);
    }
    const large=[...buckets.values()].filter(n=>n>pixels.width*pixels.height*.025);
    if(large.length<3)throw Error('Production canvas remains blank/loading; expected classroom and HUD');
    if(runtimeErrors.length)throw Error('Production scene runtime errors: '+runtimeErrors.join(' '));
    if(!classroomReady)throw Error('Five textured classroom assets were not loaded');
    if(!playerReady)throw Error('Production player model was not loaded');
    if(!authoredReady)throw Error('Production authored motion library did not load');
    if(!deongchiReady)throw Error('Tutorial Move/Jump/Crouch progression did not load Deongchi: '+lastTutorialRole);
    if(!lastTutorialRole.includes('step=Guard'))throw Error('Restricted tutorial did not reach Guard: '+lastTutorialRole);
    if(!hookReady)throw Error('Real third combo hit did not use the supplied MmaKick1');
    lobby=null;await live.keyboard.press('Escape');await waitLobby(x=>x.page==='Paused','pause navigation');
    await live.waitForTimeout(400);audioChecks.pauseRms=await audioPeak();
    if(audioChecks.pauseRms>.00001)throw Error('Pause did not stop music: '+audioChecks.pauseRms);
    await live.keyboard.press('Escape');await live.waitForTimeout(500);
    audioChecks.resumeRms=await audioPeak();
    if(audioChecks.resumeRms<.0001)throw Error('Resume did not restore music');
    audioChecks.pauseResume=true;
    lobby=null;await live.keyboard.press('Escape');await waitLobby(x=>x.page==='Paused','pause again');
    await menuClick('ReturnLobby');await waitLobby(x=>x.page==='Main','return to lobby');
    lobbyChecks.returnToLobby=true;
    fs.writeFileSync('hud-test-results/lobby-tutorial.json',JSON.stringify({commit:process.env.GITHUB_SHA,lobbyChecks,restrictions,errors:runtimeErrors},null,2));
    if(runtimeErrors.length)throw Error('Runtime errors after returning to lobby: '+runtimeErrors.join(' '));
    fs.mkdirSync('build/WebGL/verification',{recursive:true});
    fs.writeFileSync('hud-test-results/audio.json',JSON.stringify({commit:process.env.GITHUB_SHA,...audioChecks},null,2));
    fs.writeFileSync('hud-test-results/summary.json',JSON.stringify({commit:process.env.GITHUB_SHA,executions,authoredReviews,authoredReady,openingSitting,openingStanding,sittingSeen,standingSeen,jumpStages:[...jumpStages],lobbyChecks,restrictions,heavyAttack,visualChecks,classroomReady,playerReady,deongchiReady,hookReady,lastTutorialRole,runtimeErrors,audioChecks,fullscreen:true,resize:true,fixedClassroomCamera:true},null,2));
    for(const name of ['production-hud.png','motion-walk.png','motion-crouch.png','motion-kick.png','hook-turn.png','hook-impact.png','hook-ready.png','fight-ready.png','fight-stride.png','fight-roundhouse.png','deongchi-ready.png','deongchi-stride.png','deongchi-jab.png','deongchi-cross.png','authored-review-1.png','authored-review-2.png','authored-FightingIdle.png','authored-RoundhouseKick.png','authored-MmaKick1.png','authored-CenterBlock.png','authored-Jumping1.png','authored-Sitting.png','authored-Stunned.png','summary.json'])
      fs.copyFileSync('hud-test-results/'+name,'build/WebGL/verification/'+name);
    console.log('::notice::Production player model loaded; move/crouch/jump/attack input exercised with no runtime exceptions');
    await live.close();
  } finally {await browser.close();}
})().catch(e=>{
  fs.mkdirSync('hud-test-results',{recursive:true});
  fs.writeFileSync('hud-test-results/failure.json',JSON.stringify({commit:process.env.GITHUB_SHA,error:String(e)},null,2));
  console.error("::error::"+String(e).replace(/\n/g," "));process.exit(1)
});
