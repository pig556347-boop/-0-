// Render source SVG diagrams and verify the local atlas in real Chromium.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'/workspace/escape-tools/node_modules/playwright');
(async()=>{
 const dir=path.resolve(__dirname,'../Docs/MapAtlas'),atlas=JSON.parse(fs.readFileSync(path.join(dir,'atlas.json'),'utf8'));
 const docs=path.dirname(dir),server=http.createServer((req,res)=>{
  const file=path.resolve(docs,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));
  if(!file.startsWith(docs+path.sep)||!fs.existsSync(file)||!fs.statSync(file).isFile()){res.writeHead(404);res.end();return}
  res.setHeader('Content-Type',({'.html':'text/html; charset=utf-8','.svg':'image/svg+xml','.png':'image/png','.json':'application/json'}[path.extname(file)]||'application/octet-stream'));fs.createReadStream(file).pipe(res);
 });await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 try{
  const page=await browser.newPage({deviceScaleFactor:1.25});
  const report={utc:new Date().toISOString(),kind:'Actual Chromium SVG diagram rendering and local atlas loading; not gameplay validation',mapSha256:atlas.mapSha256,views:[],pngs:[],errors:[]};
  page.on('pageerror',e=>report.errors.push(e.message));
  for(const d of atlas.diagrams){
   const svg=fs.readFileSync(path.join(dir,d.file),'utf8');await page.setViewportSize({width:d.width,height:d.height});
   await page.setContent('<style>html,body{margin:0;background:#101e27}</style>'+svg);await page.evaluate(()=>document.fonts.ready);
   const png=d.file.replace(/\.svg$/,'.png');await page.locator('svg').screenshot({path:path.join(dir,png)});report.pngs.push(png);
  }
  await page.goto('http://127.0.0.1:'+server.address().port+'/MapAtlas/index.html');
  for(let i=0;i<atlas.entries.length;i++){
   await page.selectOption('#plan',String(i));await page.waitForFunction(file=>{const m=document.getElementById('map');return m.getAttribute('src')===file&&m.complete&&m.naturalWidth>0},atlas.entries[i].file);
   report.views.push({file:atlas.entries[i].file,loaded:true});
  }
  report.ok=report.views.length===atlas.entries.length&&report.pngs.length===atlas.diagrams.length&&!report.errors.length;fs.writeFileSync(path.join(dir,'review.json'),JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify({ok:report.ok,views:report.views.length,pngs:report.pngs.length,errors:report.errors}));if(!report.ok)process.exitCode=1;
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve))}
})().catch(e=>{console.error(e);process.exitCode=1});
