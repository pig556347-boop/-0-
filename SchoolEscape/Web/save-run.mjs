// Versioned manual run saves; random puzzle answers/names rebuild from the saved seed.
export function requestedSave(){if(sessionStorage.getItem('school.load017')!=='1')return null;sessionStorage.removeItem('school.load017');try{const s=JSON.parse(localStorage.getItem('school.save017'));return s?.version===17?s:null}catch{return null}}
export function saveRun({game:g,items:i,survival:s,kitchen:k,progression:p,pursuit,subjects}){
 if(g.caught||g.won||g.pendingDoor||k.active||subjects?.active)throw Error('추격 장면·문 이동·포획 중에는 저장할 수 없습니다.');
 const data={version:17,seed:g.runSeed,mode:s.mode,lives:s.lives,lastDeath:s.lastDeath,time:g.time,spaceId:g.spaceId,x:g.x,y:g.y,stamina:g.stamina,exhausted:g.exhausted,speedBoostUntil:g.speedBoostUntil,inventory:i.inventory,equipped:i.equipped,worldItems:i.worldItems,consumed:i.consumed,serial:i.serial,insertedKeys:i.insertedKeys,searchables:i.searchables.map(s=>({id:s.id,searched:s.searched,itemUid:s.itemUid})),subjects:subjects?.snapshot(),progression:p.snapshot(),kitchen:{phase:k.phase,examined:k.examined,attempts:k.attempts,blocks:k.blocks,destroyed:k.destroyed},enemyEnabled:pursuit.enabled};
 localStorage.setItem('school.save017',JSON.stringify(data));return data;
}
export function restoreRun(d,{game:g,items:i,survival:s,kitchen:k,progression:p,pursuit,subjects}){
 i.insertedKeys=[...(d.insertedKeys||[])];s.setMode(d.mode);s.lives=d.lives;s.lastDeath=d.lastDeath;g.time=d.time;g.stamina=d.stamina;g.exhausted=d.exhausted;g.speedBoostUntil=d.speedBoostUntil;
 for(const name of ['inventory','equipped','worldItems','consumed','serial'])i[name]=d[name];for(const a of d.searchables){const src=i.searchById.get(a.id);if(src)Object.assign(src,a)}p.restore(d.progression);subjects?.restore(d.subjects);
 Object.assign(k,d.kitchen);k.sync();g.warpSpace(d.spaceId,d.x,d.y);k.lastSpace=d.spaceId;pursuit.setEnabled(d.enemyEnabled);pursuit.catchGraceUntil=g.time+5;
}
