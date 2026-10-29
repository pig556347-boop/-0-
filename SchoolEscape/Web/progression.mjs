import names from './data/student-names.json' with {type:'json'};
import {rng} from './run-rules.mjs';
const zoneLabels={start:'본관 1층',floor2:'본관 2층',floor3:'본관 3층',floor4:'본관 4층',annex:'별관',west:'서관',roof:'옥상'};
const lockLabels={key:'전용 열쇠',cutter:'쇠사슬 · 절단기',code4:'4자리 비밀번호',code6:'6자리 비밀번호',direction:'방향 자물쇠'};
export class Progression {
 constructor(game,items){
  this.game=game;this.items=items;this.random=rng(items.seed^0x632af174);this.locks={};this.zones=new Set(['start']);this.classes={};this.documents={};this.readDocuments=new Set();this.lastWarning=-10;this.activeLock=null;
  this.buildClasses();this.generate();this.installNotes();this.nameCompartments();game.progression=this;items.progression=this;
  game.travelGuard=(target,kind)=>this.travel(target,kind);
 }
 shuffle(a){a=[...a];for(let i=a.length-1;i>0;i--){const j=Math.floor(this.random()*(i+1));[a[i],a[j]]=[a[j],a[i]]}return a}
 zone(id){const f=this.game.spaces.get(id);return f?.building==='annex'?'annex':f?.building==='west'?'west':f?.id==='main-rooftop'?'roof':(f?.sourceFloor||f?.number)===4?'floor4':(f?.sourceFloor||f?.number)===3?'floor3':(f?.sourceFloor||f?.number)===2?'floor2':'start'}
 register(id,label,type='tool'){this.items.catalog[id]={id,label,name:label,type,description:type==='card'?'해당 구역의 문·계단에서 사용하면 출입 권한이 유지됩니다.':type==='roomKey'?'해당 방문에서 E로 사용합니다. 열면 잠금은 해제된 채 유지됩니다.':'쇠사슬 잠금을 해제하는 도구입니다.',color:'#aebcb1',icon:type==='card'?'카드':'열쇠',usable:false,wearable:false};return id}
 place(id,source){const a=source.approaches[0],item={uid:this.items._freshUid(id),itemId:id,spaceId:source.spaceId,x:source.x,y:source.y,displayX:a.x,displayY:a.y,sourceId:source.id,hidden:true,revealed:false,compartment:Math.floor(this.random()*source.compartments),picked:false};this.items.worldItems.push(item);if(!source.itemUid)source.itemUid=item.uid;return item}
 note(source,id,title,text){const d={id,title,text,sourceId:source.id};(this.documents[source.id]??=[]).push(d);return d}
 buildClasses(){
  const pool=this.shuffle(names);let cursor=0;
  for(const room of this.game.data.roomMaps.filter(r=>r.kind==='classroom')){
   const desks=this.items.searchables.filter(s=>s.spaceId===room.id&&s.kind==='student-desk').sort((a,b)=>b.y-a.y||a.x-b.x),numbers=this.shuffle(desks.map((_,i)=>i+1));
   const students=desks.map((desk,i)=>{const no=numbers[i];desk.studentNo=no;desk.label=`${String(no).padStart(2,'0')}번 책상`;return{no,name:pool[cursor++],role:'학생',deskId:desk.id,x:desk.x,y:desk.y}});if(!students.length)throw Error('No classroom desks '+room.id);
   const roles=this.shuffle(students);roles[0].role='반장';if(roles[1])roles[1].role='부반장';
   const lectern=this.items.searchables.find(s=>s.spaceId===room.id&&s.kind.includes('teacher-desk'));if(!lectern)throw Error('No lectern '+room.id);
   this.classes[room.id]={id:room.id,label:room.label,students,lecternId:lectern.id,leader:roles[0].no};
   const roster=this.note(lectern,room.id+'-roster','출석부',students.slice().sort((a,b)=>a.no-b.no).map(s=>`${String(s.no).padStart(2,'0')}  ${s.name}  ${s.role}`).join('\n'));
   roster.roster=students.slice().sort((a,b)=>a.no-b.no);roster.classLabel=room.label;
   const rows=room.seatRows||[13,11,9,7,5],columns=room.seatColumns||[3,5,7,11,13,15];
   this.note(lectern,room.id+'-seats','자리배치도 · 번호만 표시','칠판 / 교탁 쪽 ↑\n\n'+rows.map(y=>columns.map((x,col)=>{const student=students.find(s=>Math.abs(s.y-y)<.5&&Math.abs(s.x-x)<.5);return (col===3?'  |  ':'')+(student?String(student.no).padStart(2,'0'):'--')}).join('   ')).join('\n\n')+'\n\n-- 책상 없음 · | 중앙 통로\n↓ 출입문 / 사물함');
   this.note(lectern,room.id+'-board','칠판 기록',this.shuffle(['금요일까지 학급 문고 반납.','창문 너머 종소리는 세지 말 것.','다음 주 청소 당번은 부반장.'])[0]+'\n반장 책상에 맡긴 물건을 확인하자.');
  }
  if(cursor>names.length||new Set(Object.values(this.classes).flatMap(c=>c.students.map(s=>s.name))).size!==cursor)throw Error('Student name pool exhausted');
 }
 sources(zone,openOnly=true){return this.items.searchables.filter(s=>this.zone(s.spaceId)===zone&&!this.game.spaces.get(s.spaceId).noLoot&&this.game.spaces.get(s.spaceId).kind!=='science'&&s.kind!=='library-reward-cabinet'&&(!openOnly||!this.locks[s.spaceId])&&s.kind!=='student-desk')}
 source(zone){const a=this.sources(zone).filter(s=>!this.containerLocks?.[s.id]);if(!a.length)throw Error('No prior reachable source '+zone);return this.shuffle(a)[0]}
 generate(){
  const rooms=this.game.data.roomMaps.filter(r=>!['annex','west'].includes(r.building)&&r.id!=='main-rooftop');
  for(const r of rooms.filter(r=>r.kind==='classroom'))this.locks[r.id]={id:r.id,label:r.label,type:'key',need:this.register('room-key-'+r.id,r.label+' 전용 열쇠','roomKey'),unlocked:false};
  const special=this.shuffle(rooms.filter(r=>['music','art','infirmary','broadcast','storage','archives','preparation','computer','control','staff','technology'].includes(r.kind)));let cutter=false;
  for(let i=0;i<special.length;i++){const r=special[i],type=r.id==='room-f2-12'?'code6':r.kind==='staff'?'key':(['key','code4','code6','cutter'][i%4]);const t=type==='cutter'&&cutter?'code4':type;if(t==='cutter')cutter=true;
   const lock={id:r.id,label:r.label,type:t,unlocked:false};if(t==='key')lock.need=this.register('room-key-'+r.id,r.label+' 전용 열쇠','roomKey');else if(t==='cutter')lock.need=this.register('bolt-cutter','절단기');else lock.answer=Array.from({length:t==='code4'?4:6},()=>Math.floor(this.random()*10)).join('');this.locks[r.id]=lock;
  }
  // Every dependency begins in an unlocked room in the same or earlier zone.
  // The only room-to-room chain is class leader -> matching staff room.
  for(const lock of Object.values(this.locks)){
   const room=this.game.spaces.get(lock.id),z=this.zone(lock.id);let source=this.source(z);
   if(room.kind==='staff'){
    const candidates=Object.values(this.classes).filter(c=>this.zone(c.id)===z);if(candidates.length){const c=this.shuffle(candidates)[0];source=this.items.searchById.get(c.students.find(s=>s.role==='반장').deskId);lock.classClue=c.id;this.note(this.items.searchById.get(c.lecternId),lock.id+'-role',room.label+' 열쇠 메모','교무실 열쇠는 이 반 반장 자리에 맡겼다. 출석부와 자리배치도를 대조하자.');}
   }
   if(lock.need){lock.rewardUid=this.place(lock.need,source).uid;lock.sourceId=source.id;}
   else{lock.sourceId=source.id;this.note(source,'puzzle-'+lock.id,lock.label+' 잠금 기록',`${lock.label} · 이번 잠금 번호: ${lock.answer}`);}
  }
  // Single optional directional storage puzzle; first classroom key behind it.
  const first=this.shuffle(Object.values(this.locks).filter(l=>this.zone(l.id)==='start'&&this.classes[l.id]))[0];
  const storage=this.source('start'),clue=this.shuffle(this.sources('start').filter(s=>s.id!==storage.id))[0];
  const direction={id:'container:'+storage.id,label:storage.label+' 방향 잠금',type:'direction',answer:Array.from({length:4},()=>['↑','→','↓','←'][Math.floor(this.random()*4)]).join(''),unlocked:false,sourceId:clue.id};
  this.containerLocks={[storage.id]:direction};this.note(clue,'first-route','처음 교실의 단서',`${first.label} 열쇠는 ${this.game.spaces.get(storage.spaceId).label}의 ${storage.label}에 맡겼다.\n방향 잠금: ${direction.answer}`);
  const firstItem=this.items.worldItems.find(i=>i.uid===first.rewardUid);this.items.worldItems=this.items.worldItems.filter(i=>i!==firstItem);first.rewardUid=this.place(first.need,storage).uid;first.sourceId=storage.id;this.firstClass=first.id;
  this.cards={};for(const [z,prior] of [['floor2','start'],['floor3','floor2'],['floor4','floor3'],['annex','floor2'],['west','floor2'],['roof','floor4']]){
   const id=this.register('card-'+z,zoneLabels[z]+' 카드키','card'),source=this.source(prior),item=this.place(id,source);this.cards[z]={itemId:id,uid:item.uid,sourceId:source.id,prior};
  }
  const shop=this.items.searchables.find(s=>s.spaceId==='west-school-shop'&&s.kind==='shop-stock-cabinet');if(shop){this.place('chocolate-bar',shop);this.place('vitamin-water',shop);}
  const reward=this.items.searchables.find(s=>s.kind==='kitchen-reward-crate');if(reward){reward.label='조리실 끝 보급 상자';this.place('chocolate-bar',reward);this.note(reward,'kitchen-note','배식 일지','조사를 마쳤다. 들어온 북쪽 출입문으로 돌아가자.\n무슨 일이 생기면 비상 유도선을 따라 서쪽 문으로 달려.');}
 }
 installNotes(){
  this.register('master-key','개발용 마스터키');Object.assign(this.items.catalog['master-key'],{description:'일반 문·카드키·비밀번호·쇠사슬 잠금을 해제합니다. 교문 A/B/C와 과학실 안전장비 조건은 별도입니다.',icon:'M'});
  this.items.inventory.push({uid:this.items._freshUid('master-key'),itemId:'master-key'});
  const locks=[...Object.values(this.locks),...Object.values(this.containerLocks)];
  for(const [sourceId,docs] of Object.entries(this.documents))for(const d of docs){
   if(!d.id.startsWith('puzzle-')&&d.id!=='first-route'&&!d.id.endsWith('-role'))continue;
   const lock=locks.find(l=>d.id==='puzzle-'+l.id||d.id===l.id+'-role'||(d.id==='first-route'&&l.type==='direction'));if(!lock)continue;
   const id='note-'+d.id;this.register(id,d.title+' 쪽지','note');d.itemId=id;d.lockId=lock.id;
   const map=this.game.spaces.get(lock.id);const target=map?`${map.building==='annex'?'별관':map.building==='west'?'서관':'본관'} ${map.sourceFloor||map.number}층 · ${lock.label}`:lock.label;
   Object.assign(this.items.catalog[id],{type:'note',description:'대상: '+target+'\n\n'+d.text+'\n\n해당 잠금이 해제되면 즉시 소모됩니다.',usable:true,icon:'쪽지',lockId:lock.id});
   this.place(id,this.items.searchById.get(sourceId));
  }
 }
 finishUnlock(lock){
  lock.unlocked=true;const ids=Object.values(this.items.catalog).filter(d=>d.type==='note'&&d.lockId===lock.id).map(d=>d.id);
  for(const id of ids){this.consume(id);for(const i of this.items.worldItems)if(i.itemId===id&&!i.picked){i.picked=true;i.consumed=true}}
  this.game.emit('lock-unlocked',{lockId:lock.id,kind:lock.type});
 }
 nameCompartments(){
  for(const room of this.game.data.roomMaps){
   const c=this.classes[room.id],students=c?.students.slice().sort((a,b)=>a.no-b.no);let n=0;
   for(const src of this.items.searchables.filter(s=>s.spaceId===room.id&&s.compartments>1)){
    src.compartmentNames=Array.from({length:src.compartments},(_,i)=>students?((students[n++]?.name)||'공용 보관함 '+(n)):['gatehouse','guardDorm'].includes(room.kind)?['김경비','이경비'][i%2]:room.kind==='changing'?['김지민','이서연','박수빈','최유진'][i%4]:['김민수','이정우','박지현','최수진','정지훈','강서연','조현우','윤지은'][(n++)%8]);
   }
  }
 }
 has(id){return this.items.inventory.some(i=>i.itemId===id)}
 consume(id){const i=this.items.inventory.findIndex(i=>i.itemId===id);if(i>=0)this.items.consumed.push(...this.items.inventory.splice(i,1))}
 describe(target){const z=this.zone(target);if(!this.zones.has(z))return zoneLabels[z]+' 카드키 필요';const l=this.locks[target];return l&&!l.unlocked?lockLabels[l.type]+' 잠김':''}
 allowed(target){const z=this.zone(target);return this.zones.has(z)&&(!this.locks[target]||this.locks[target].unlocked)}
 warn(message){if(this.game.time-this.lastWarning>.9){this.lastWarning=this.game.time;this.game.emit('progression-hint',{message});this.onMessage?.(message)}}
 travel(target,kind='door'){
  const z=this.zone(target);if(this.has('master-key')){this.zones.add(z);const l=this.locks[target];if(l&&!l.unlocked)this.finishUnlock(l);return true}if(!this.zones.has(z)){
   const c=this.cards[z];if(c&&this.has(c.itemId)){this.consume(c.itemId);this.zones.add(z);this.warn(zoneLabels[z]+' 출입 권한을 등록했다.');}
   else{this.warn((zoneLabels[z]||'구역')+' 카드키가 필요합니다. 가이드북에서 현재 목표를 확인하세요.');return false;}
  }
  const lock=this.locks[target];if(!lock||lock.unlocked)return true;
  if(lock.need&&this.has(lock.need)){if(lock.type==='key')this.consume(lock.need);this.finishUnlock(lock);this.warn(lock.label+' 잠금을 해제했다.');return true;}
  this.activeLock=lock;if(kind==='door'&&!lock.need)this.onLock?.(lock);else this.warn(lock.label+' · '+lockLabels[lock.type]+' 필요');return false;
 }
 unlock(lockId,value){const lock=this.locks[lockId]||Object.values(this.containerLocks).find(l=>l.id===lockId);if(!lock||lock.need)return{ok:false,message:'맞는 도구나 열쇠가 필요합니다.'};if(String(value)!==lock.answer)return{ok:false,message:'잠금이 풀리지 않는다. 기록을 다시 확인하자.'};this.finishUnlock(lock);return{ok:true,message:'잠금을 해제했습니다. E로 다시 열어 들어가세요.'}}
 checkContainer(sourceId){const l=this.containerLocks[sourceId];if(l&&this.has('master-key'))this.finishUnlock(l);if(l&&!l.unlocked){this.activeLock=l;this.onLock?.(l);return false;}return true}
 read(sourceId,id){const d=this.documents[sourceId]?.find(d=>d.id===id);if(d)this.readDocuments.add(d.id);return d}
 location(sourceId,exact){const s=this.items.searchById.get(sourceId),r=this.game.spaces.get(s.spaceId);return exact?`${r.label||r.subtitle} · ${s.label} (${Math.round(s.x)}, ${Math.round(s.y)})`:`${zoneLabels[this.zone(s.spaceId)]}의 ${r.kind==='staffRestroom'?'교직원이 손을 씻던 곳':r.kind==='administration'?'학교 서류를 관리하던 곳':r.kind==='classroom'?'학생들이 수업을 듣던 곳':'아직 열려 있는 방과 수납물'}을 살펴보자`}
 guide(hints){
  if(this.has('master-key'))return ['개발용 마스터키 소지 중 · 일반 잠금을 우회합니다. 교문에는 A·B·C를 직접 꽂아야 합니다.'];
  if(hints==='none')return ['문에 표시된 잠금 종류를 확인하고 도구와 열쇠를 찾자.'];
  const first=this.locks[this.firstClass];if(first&&!first.unlocked){const c=Object.values(this.containerLocks)[0];return ['첫 교실 단서: '+this.location(c.sourceId,hints==='exact')];}
  const next=Object.entries(this.cards).find(([z,c])=>!this.zones.has(z)&&this.zones.has(c.prior));if(next){const [z,c]=next;return [zoneLabels[z]+': '+(this.has(c.itemId)?'카드키를 확보했다. 입구로 이동하자.':this.location(this.items.worldItems.find(i=>i.uid===c.uid)?.sourceId||c.sourceId,hints==='exact'))];}
  return [];
 }

 snapshot(){return{zones:[...this.zones],unlocked:Object.values(this.locks).filter(l=>l.unlocked).map(l=>l.id),containers:Object.entries(this.containerLocks).filter(([,l])=>l.unlocked).map(([id])=>id),read:[...this.readDocuments]}}
 restore(s){this.zones=new Set(s.zones);for(const id of s.unlocked)if(this.locks[id])this.locks[id].unlocked=true;for(const id of s.containers)if(this.containerLocks[id])this.containerLocks[id].unlocked=true;this.readDocuments=new Set(s.read)}
}
