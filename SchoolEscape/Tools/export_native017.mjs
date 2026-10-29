import fs from 'node:fs';
import {Exploration} from '../Web/kernel.mjs';import {ItemSystem} from '../Web/items.mjs';import {Progression} from '../Web/progression.mjs';import {Pursuit} from '../Web/pursuit.mjs';import {SubjectEvents} from '../Web/subject-events.mjs';
const raw=JSON.parse(fs.readFileSync(new URL('../Assets/Resources/SchoolMap.json',import.meta.url))),runs=[];
for(const seed of [1,16,2026,541,71,82,93,104,115,126,137,148,159,170,181,192]){
 const g=new Exploration(structuredClone(raw)),i=new ItemSystem(g,{seed}),p=new Pursuit(g),pr=new Progression(g,i),s=new SubjectEvents(g,i,p,pr);
 runs.push({seed,defs:Object.values(i.catalog),items:i.worldItems.map(v=>({...v,id:v.itemId,point:{x:v.displayX,y:v.displayY}})),sources:i.searchables.map(v=>({...v,point:v.approaches[0],compartmentNames:v.compartmentNames||[]})),locks:[...Object.values(pr.locks),...Object.values(pr.containerLocks)],cards:Object.entries(pr.cards).map(([zone,c])=>({zone,...c})),documents:Object.values(pr.documents).flat(),library:s.library.sequence,classes:Object.values(pr.classes)});
}
fs.writeFileSync(new URL('../Assets/Resources/RunLayouts017.json',import.meta.url),JSON.stringify({version:17,runs}));
console.log('Exported',runs.length,'validated construction layouts; Unity runs native C# logic.');
