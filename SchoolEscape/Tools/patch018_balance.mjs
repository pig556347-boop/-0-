import fs from 'node:fs';import assert from 'node:assert/strict';
import {Exploration} from '../Web/kernel.mjs';import {MODES} from '../Web/run-rules.mjs';
const g=new Exploration(JSON.parse(fs.readFileSync(new URL('../Assets/Resources/SchoolMap.json',import.meta.url))));g.autoStairsEnabled=false;
const report={ok:false,checks:[],errors:[],modes:[]};const check=(name,passed)=>{assert.ok(passed,name);report.checks.push({name,passed})};
try{for(const [mode,rules] of Object.entries(MODES)){
 g.warp(1,24,9);g.maxStamina=g.stamina=rules.stamina;g.exhausted=false;g.speedBoostUntil=-1;g.lastSprintTime=-10;let elapsed=0;
 for(let n=0;n<600&&!g.exhausted;n++){g.step(Math.floor(n/8)%2?-1:1,0,true,.05);elapsed+=.05;}
 check(mode+' exhaustion time',Math.abs(elapsed-rules.stamina/16)<.06);check(mode+' cannot sprint exhausted',g.stamina===0&&g.exhausted);
 g.step(0,0,false,.8); // production clamps dt to .05; use individual frames below
 for(let n=0;n<14;n++)g.step(0,0,false,.05);check(mode+' no recovery before delay',g.stamina===0);
 for(let n=0;n<5;n++)g.step(0,0,false,.05);const before=g.stamina;for(let n=0;n<10;n++)g.step(0,0,false,.05);check(mode+' recovery is 24 per second',Math.abs(g.stamina-before-12)<.01);
 for(let n=0;n<100;n++)g.step(0,0,false,.05);check(mode+' exhaustion eventually clears',!g.exhausted);report.modes.push({mode,sprintSeconds:elapsed,maximum:rules.stamina});
}report.ok=true;}catch(e){report.errors.push(e.stack)}
fs.writeFileSync(new URL('../Docs/Evidence/patch018-balance.json',import.meta.url),JSON.stringify(report,null,2));console.log(JSON.stringify(report));if(!report.ok)process.exitCode=1;
