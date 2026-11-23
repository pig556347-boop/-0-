// Public verification runs the same opening and post-opening browser suites as the local build.
import fs from 'node:fs';import path from 'node:path';import {spawnSync} from 'node:child_process';
const commit=process.env.PUBLIC_GAME_COMMIT||process.env.GITHUB_SHA,url=process.env.ESCAPE_URL||`https://rawcdn.githack.com/${process.env.GITHUB_REPOSITORY}/${commit}/index.html`,manifest=JSON.parse(fs.readFileSync('build.json'));
const report={ok:true,utc:new Date().toISOString(),sourceCommit:commit,publicURL:url,checks:[],errors:[],suites:[]};
for(const [name,file] of [['story','verify-story024.cjs'],['kitchen','verify-kitchen024.cjs']]){
 const out=path.resolve('proof',name);fs.mkdirSync(out,{recursive:true});const result=spawnSync(process.execPath,[file],{env:{...process.env,PLAYWRIGHT_MODULE:path.resolve('node_modules/playwright/index.js'),PLAY_URL:url,PROOF_DIR:out,ENEMIES_ON:'1'},encoding:'utf8',timeout:780000,maxBuffer:8*1024*1024});process.stdout.write(result.stdout||'');process.stderr.write(result.stderr||'');
 let r;try{r=JSON.parse(fs.readFileSync(path.join(out,'report.json')))}catch{r={ok:false,error:result.error?.message||'No report',checks:[]}}
 report.suites.push({name,ok:r.ok,hash:r.sha256,checks:r.checks.length});report.checks.push(...r.checks.map(c=>({...c,name:name+': '+c.name})));report.errors.push(...r.errors||[]);if(!r.ok||result.status!==0||r.sha256!==manifest.htmlSha256){report.ok=false;report.errors.push(name+': '+(r.error||'failure/hash mismatch'));}
}
fs.writeFileSync('published-status.json',JSON.stringify(report,null,2)+'\n');if(!report.ok)process.exitCode=1;
