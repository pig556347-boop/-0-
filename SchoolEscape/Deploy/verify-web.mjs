// Verify the public fixed URL, then compare with the locally tested build.
import fs from 'node:fs';import path from 'node:path';import {spawnSync} from 'node:child_process';
const commit=process.env.PUBLIC_GAME_COMMIT||process.env.GITHUB_SHA,url=process.env.ESCAPE_URL||`https://rawcdn.githack.com/${process.env.GITHUB_REPOSITORY}/${commit}/index.html`,manifest=JSON.parse(fs.readFileSync('build.json'));
const report={ok:true,utc:new Date().toISOString(),sourceCommit:commit,publicURL:url,checks:[],errors:[],suites:[]};
for(const [name,file] of [['patch017','verify-patch017.cjs'],['lobby','verify-lobby.mjs'],['selfdiag','verify-selfdiag017.cjs']]){
 const out=path.resolve('proof',name);fs.mkdirSync(out,{recursive:true});const result=spawnSync(process.execPath,[file],{env:{...process.env,PLAYWRIGHT_MODULE:path.resolve('node_modules/playwright/'+(name==='lobby'?'index.mjs':'index.js')),PLAY_URL:url,ESCAPE_URL:url,PROOF_DIR:out,ESCAPE_RESULTS_DIR:out,EXPECTED_SHA256:manifest.htmlSha256,SKIP_SHOTS:'1'},encoding:'utf8',timeout:360000,maxBuffer:8*1024*1024});process.stdout.write(result.stdout||'');process.stderr.write(result.stderr||'');
 let r;try{r=JSON.parse(fs.readFileSync(path.join(out,name==='lobby'?'published-status.json':'report.json')))}catch{r={ok:false,error:result.error?.message||'No report',checks:[]}}
 const hash=r.sha256||r.http?.sha256;report.suites.push({name,ok:r.ok,hash,checks:r.checks.length});report.checks.push(...r.checks.map(c=>({...c,name:name+': '+c.name})));report.errors.push(...r.errors||[],...r.consoleErrors||[]);if(!r.ok||result.status!==0||hash!==manifest.htmlSha256){report.ok=false;report.errors.push(name+': '+(r.error||'failure/hash mismatch'));}
}
fs.writeFileSync('published-status.json',JSON.stringify(report,null,2)+'\n');if(!report.ok)process.exitCode=1;
