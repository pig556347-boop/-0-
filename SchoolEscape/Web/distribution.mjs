// Enforce a per-space initial loot budget after all quest generators have run.
// Runtime drops intentionally bypass this allocator.
export function distributeLoot(game,items,progression){
 const large=f=>['playground','gym','music'].includes(f.kind),counts=new Map(),used=new Set();
 const count=id=>{if(!counts.has(id))counts.set(id,{hidden:0,ground:0});return counts.get(id)};
 const limit=(f,mode)=>large(f)?4:1;
 const accessible=new Set([...game.spaces.keys()].filter(id=>!progression.locks[id]&&game.spaces.get(id)?.kind!=='science'));
 const roomOpen=id=>accessible.has(id);
 const fixed=i=>['lab-coat','flashlight','lab-sample'].includes(i.itemId)||items.searchById.get(i.sourceId)?.kind==='kitchen-reward-crate'||Object.values(progression.locks).some(l=>l.classClue&&l.rewardUid===i.uid);
 const rank=i=>['lab-coat','flashlight','lab-sample'].includes(i.itemId)||items.searchById.get(i.sourceId)?.kind==='kitchen-reward-crate'?-50:i.uid===progression.locks[progression.firstClass].rewardUid?-45:i.itemId==='note-first-route'?-46:i.itemId.startsWith('room-key-')&&!Object.values(progression.locks).some(l=>l.rewardUid===i.uid&&l.classClue)?-5:fixed(i)?-2:0;
 const order=[...items.worldItems].sort((a,b)=>(fixed(a)?-50:rank(a))-(fixed(b)?-50:rank(b)));
 const placed=[],known=new Set(),openContainers=new Set();
 function refreshReachable(){let changed=true;while(changed){changed=false;for(const i of placed){if(known.has(i.itemId)||!accessible.has(i.spaceId)||progression.containerLocks[i.sourceId]&&!openContainers.has(progression.containerLocks[i.sourceId].id))continue;known.add(i.itemId);changed=true;}for(const l of [...Object.values(progression.locks),...Object.values(progression.containerLocks)]){const opened=l.id.startsWith('container:')?openContainers:accessible;if(opened.has(l.id))continue;if(l.need?known.has(l.need):[...known].some(id=>items.catalog[id]?.lockId===l.id)){opened.add(l.id);changed=true;}}}}
 const safe=(i,f,p)=>!i.itemId.startsWith('key-')||items._safeKeyLocation(f,p);
 function allowed(i,id,mode,src){const f=game.spaces.get(id);return count(id)[mode]<limit(f,mode)&&(!src||!used.has(src.id))&&safe(i,f,src||{x:i.x,y:i.y})}
 for(const i of order){
  let mode=i.hidden?'hidden':'ground',source=items.searchById.get(i.sourceId);
  if(!allowed(i,i.spaceId,mode,source)){
   const zone=progression.zone(i.spaceId);
   const candidates=progression.shuffle(items.searchables.filter(s=>progression.zone(s.spaceId)===zone&&roomOpen(s.spaceId)&&!game.spaces.get(s.spaceId).noLoot&&!progression.containerLocks[s.id]&&s.kind!=='library-reward-cabinet'&&allowed(i,s.spaceId,'hidden',s)))
    .sort((a,b)=>(count(a.spaceId).hidden+count(a.spaceId).ground)-(count(b.spaceId).hidden+count(b.spaceId).ground));
   source=candidates[0];
   if(source){const p=source.approaches[0];Object.assign(i,{spaceId:source.spaceId,x:source.x,y:source.y,displayX:p.x,displayY:p.y,sourceId:source.id,hidden:true,revealed:false,compartment:Math.floor(items.random()*source.compartments)});mode='hidden'}
   else{
    let placed=false;const spaces=progression.shuffle([...game.spaces.values()].filter(f=>progression.zone(f.id)===zone&&roomOpen(f.id)&&!f.noLoot&&count(f.id).ground<limit(f,'ground')));
    for(const f of spaces){const p=progression.shuffle(items.navigation.get(f.id).available).find(n=>safe(i,f,n)&&Math.hypot(n.x-f.spawnX,n.y-f.spawnY)>2&&!items.worldItems.some(j=>j!==i&&j.spaceId===f.id&&!j.hidden&&Math.hypot(n.x-j.x,n.y-j.y)<1));if(p){Object.assign(i,{spaceId:f.id,x:p.x,y:p.y,displayX:p.x,displayY:p.y,sourceId:null,hidden:false,revealed:true,compartment:0});source=null;mode='ground';placed=true;break}}
    if(!placed)throw Error('No balanced reachable loot slot: '+i.itemId+' '+zone);
   }
  }
  placed.push(i);refreshReachable();
  count(i.spaceId)[mode]++;if(source)used.add(source.id);
  // Clues follow the authoritative item, even if it moved to a floor drop.
  for(const l of Object.values(progression.locks))if(l.rewardUid===i.uid)l.sourceId=i.sourceId||'ground:'+i.uid;
  for(const c of Object.values(progression.cards))if(c.uid===i.uid)c.sourceId=i.sourceId||'ground:'+i.uid;
  const note=items.catalog[i.itemId];if(note?.type==='note'){
   const lock=progression.locks[note.lockId]||Object.values(progression.containerLocks).find(l=>l.id===note.lockId);
   if(lock&&!lock.need)lock.sourceId=i.sourceId||'ground:'+i.uid;
  }
  if(!source)items.searchById.set('ground:'+i.uid,{id:'ground:'+i.uid,spaceId:i.spaceId,x:i.x,y:i.y,label:'바닥에 놓인 물건'});
 }
 for(const s of items.searchables)s.itemUid=items.worldItems.find(i=>i.sourceId===s.id)?.uid||null;
 return Object.fromEntries(counts);
}
