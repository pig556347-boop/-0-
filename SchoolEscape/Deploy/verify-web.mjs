// Public-origin Chromium verification of the exact standalone release.
import fs from'node:fs';import path from'node:path';import {spawnSync} from'node:child_process';
const sha=process.env.PUBLIC_GAME_COMMIT||process.env.GITHUB_SHA,url=process.env.ESCAPE_URL||`https://rawcdn.githack.com/${process.env.GITHUB_REPOSITORY}/${sha}/index.html`;
const result={ok:true,utc:new Date().toISOString(),publicURL:url,sourceCommit:sha,checks:[],errors:[],consoleErrors:[],suites:[]};
for(const [name,file] of [['subjects','verify-subjects.mjs'],['lobby','verify-lobby.mjs']]){
 const out=path.resolve('proof',name);fs.mkdirSync(out,{recursive:true});const run=spawnSync(process.execPath,[file],{env:{...process.env,ESCAPE_URL:url,ESCAPE_RESULTS_DIR:out,SKIP_SHOTS:'1'},encoding:'utf8',timeout:300000,maxBuffer:8*1024*1024});process.stdout.write(run.stdout||'');process.stderr.write(run.stderr||'');
 let report;try{report=JSON.parse(fs.readFileSync(path.join(out,'published-status.json')))}catch{report={ok:false,error:run.error?.message||'No suite report',checks:[]}}
 result.suites.push({name,ok:report.ok,checks:report.checks?.length,hash:report.http?.sha256});result.checks.push(...(report.checks||[]).map(c=>({...c,name:name+': '+c.name})));result.errors.push(...(report.errors||[]));result.consoleErrors.push(...(report.consoleErrors||[]));if(!result.http)result.http=report.http;
 if(run.status!==0||!report.ok){result.ok=false;result.errors.push(name+': '+(report.error||run.error?.message||'Suite failed'))}
 if(fs.existsSync('build.json')&&report.http?.sha256!==JSON.parse(fs.readFileSync('build.json')).htmlSha256){result.ok=false;result.errors.push(name+': public SHA256 mismatch')}
}
fs.writeFileSync('published-status.json',JSON.stringify(result,null,2)+'\n');if(!result.ok)process.exitCode=1;
