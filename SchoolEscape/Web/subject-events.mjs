import {distributeLoot} from './distribution.mjs';
import {Pursuit} from './pursuit.mjs';
// Authored room events use simulation time: pause/inventory freezes their clocks.
export class SubjectEvents {
 constructor(game,items,pursuit,progression){
  Object.assign(this,{game,items,pursuit,progression});game.subjects=this;
  this.maps=Object.fromEntries(game.data.roomMaps.filter(r=>r.subjectEvent).map(r=>[r.subjectEvent,r]));
  this.lastSpace='';this.music={elapsed:0,phase:'idle',monster:null};this.science={elapsed:0,phase:'idle',dark:false,blackouts:0,monsters:[],rewardTaken:false};
  this.library={step:0,solved:true,sequence:[]};
  this.equipmentSources={};this.installItems();this.distribution=distributeLoot(game,items,progression);this.special={art:null,gym:null};
  const travel=game.travelGuard;game.travelGuard=(target,kind)=>{if(!this.canEnter(target))return false;return travel(target,kind)};
  const guide=progression.guide.bind(progression);progression.guide=hints=>[...guide(hints),...this.guide(hints)];
 }
 installItems(){
  const p=this.progression,i=this.items;
  for(const [id,label,description] of [['lab-coat','실험복','과학실 출입 시 자동으로 걸칩니다. 소지품 1칸을 사용합니다.'],['flashlight','손전등','과학실 입장 장비. 소등 중에는 함께 꺼져 시야가 완전히 차단됩니다.'],['lab-sample','밀봉된 실험 표본','과학실 뒷문 캐비넷에서 회수한 표본. 획득하면 뒷문을 열 수 있습니다.']]){
   p.register(id,label);Object.assign(i.catalog[id],{description,icon:id==='flashlight'?'빛':id==='lab-coat'?'옷':'표본'});
  }
  const coat=i.searchables.find(s=>s.spaceId==='room-f2-15'&&s.kind.includes('cabinet'));
  const light=i.searchables.find(s=>s.kind==='library-reward-cabinet'),reward=i.searchables.find(s=>s.kind==='science-reward-cabinet'),record=i.searchables.find(s=>s.kind==='loan-counter');
  if(!coat||!light||!reward||!record||gameFloor(this.game,coat)!==this.maps.science.sourceFloor||gameFloor(this.game,light)!==this.maps.science.sourceFloor)throw Error('Missing subject encounter supply fixture');
  p.place('lab-coat',coat);p.place('flashlight',light);p.place('lab-sample',reward);
  this.equipmentSources={'lab-coat':coat.id,flashlight:light.id};
  p.note(coat,'science-safety','과학실 안전 수칙','실험복과 손전등을 소지하고 앞문으로 들어갈 것.\n10초마다 2초간 모든 빛이 꺼진다. 세 번째 소등이 끝나면 발소리를 경계할 것.\n뒷문 캐비넷의 표본을 회수한 뒤 뒷문으로 나갈 것.');
  p.note(light,'library-next-clue','손전등 옆 메모','실험복은 본관 3층 과학 자료실의 캐비넷에 있다.\n실험복과 손전등을 챙겨 과학실 앞문으로 들어가자.');
 }
 has(id){return this.items.inventory.some(i=>i.itemId===id)||this.items.equipped?.itemId===id}
 canEnter(target){if(target!==this.maps.science.id)return true;const missing=['lab-coat','flashlight'].filter(id=>!this.has(id));if(missing.length){this.progression.warn('과학실 입장 준비: '+missing.map(id=>this.items.catalog[id].label).join(' + ')+' 필요');return false}return true}
 intercept(door){
  if(this.game.spaceId===this.maps.science.id&&door.id==='science-rear-exit'&&!this.science.rewardTaken)return '뒷문 옆 캐비넷의 밀봉된 실험 표본을 먼저 가져가자.';
  return null;
 }
 wallSight(ax,ay,bx,by){const n=Math.max(1,Math.ceil(Math.hypot(bx-ax,by-ay)*8));for(let k=0;k<=n;k++)if(!this.game.open(Math.round(ax+(bx-ax)*k/n),Math.round(ay+(by-ay)*k/n)))return false;return true}
 createMonster(map,kind,index=0){
  const g=this.game,nav=this.items.navigation.get(map.id),candidates=nav.available.filter(n=>Math.hypot(n.x-g.x,n.y-g.y)*g.data.tileSize>7);
  const anchor=kind==='musician'?{x:6,y:16}:index?{x:2,y:12}:{x:22,y:8};
  const spawn=candidates.sort((a,b)=>Math.hypot(a.x-anchor.x,a.y-anchor.y)-Math.hypot(b.x-anchor.x,b.y-anchor.y))[0]||nav.available.at(-1);
  const m=Object.create(Pursuit.prototype);Object.assign(m,{game:g,grids:this.pursuit.grids,radius:.27,spaceId:map.id,kind,index,x:spawn.x,y:spawn.y,direction:{x:0,y:-1},speed:0,distance:0,phase:0,path:[],repath:0,grace:1.5});return m;
 }
 roamMusician(m,dt){
  if(!this.pursuit.enabled)return;
  const g=this.game,d=Math.hypot(g.x-m.x,g.y-m.y)*g.data.tileSize;
  const dot=((g.x-m.x)*m.direction.x+(g.y-m.y)*m.direction.y)/(d/g.data.tileSize||1);
  const clear=g.segmentClear(m.x,m.y,g.x,g.y,g.map,.025);
  const seen=d<10&&clear&&(d<2||dot>.3),heard=g.isRunning&&d<7&&clear;
  if(seen||heard){m.state='chase';m.lastSensed=g.time;m.lastKnown={x:g.x,y:g.y};m.goal={...m.lastKnown};}
  else if(m.state==='chase'){
   m.goal=m.lastKnown;
   if(g.time-m.lastSensed>=8||Math.hypot(m.x-m.goal.x,m.y-m.goal.y)<.45){m.state='patrol';m.goal=null;m.path=[];m.repath=0;}
  }
  if(m.state!=='chase'){
   m.state='patrol';
   if(!m.goal||Math.hypot(m.x-m.goal.x,m.y-m.goal.y)<.45){
    const nodes=this.items.navigation.get(m.spaceId).available.filter(n=>Math.hypot(n.x-m.x,n.y-m.y)>4);
    const n=nodes[Math.floor(this.items.random()*nodes.length)];m.goal=n?{x:n.x,y:n.y}:{x:m.x,y:m.y};m.path=[];m.repath=0;
   }
  }
 }
 move(m,dt,enabled){
  m.speed=0;m.grace=Math.max(0,m.grace-dt);if(!enabled)return;
  const g=this.game;m.repath-=dt;if(m.repath<=0||!m.path.length){m.path=m.route((m.goal||g).x,(m.goal||g).y);m.repath=.45}
  let speed=m.kind==='musician'?(m.state==='chase'?3.8:1.6):m.kind==='statue'?4.4:4.15;
  if(m.kind==='dunky'){const next=m.path[0],dx=next?next.x-m.x:0,dy=next?next.y-m.y:0,n=Math.hypot(dx,dy);const straight=n&&((dx*m.direction.x+dy*m.direction.y)/n)>.94;m.charge=straight?Math.min(1,(m.charge||0)+dt*.32):0;speed=2.9+4.1*m.charge;} let budget=speed*dt/g.data.tileSize,travel=0;
  while(budget>0&&m.path.length){const p=m.path[0],dx=p.x-m.x,dy=p.y-m.y,n=Math.hypot(dx,dy);if(n<.001){m.path.shift();continue}const d=Math.min(budget,n),x=m.x+dx/n*d,y=m.y+dy/n*d;if(g.blocked(x,y,g.map,m.radius)||!g.segmentClear(m.x,m.y,x,y,g.map,m.radius)){m.path=[];break}m.x=x;m.y=y;m.direction={x:dx/n,y:dy/n};budget-=d;travel+=d;if(d>=n-.001)m.path.shift()}
  m.speed=dt?travel*g.data.tileSize/dt:0;m.distance+=travel*g.data.tileSize;m.phase+=travel*5;
  if(m.grace<=0&&Math.hypot(m.x-g.x,m.y-g.y)*g.data.tileSize<.68&&g.segmentClear(m.x,m.y,g.x,g.y,g.map,.025)){g.caught=true;g.emit('caught',{source:({musician:'거두 음악가',shadow:'쉐도어',statue:'움직이는 석상',dunky:'덩키보이'})[m.kind]})}
 }
 step(dt){
  const g=this.game;dt=Math.max(0,Math.min(.05,dt));
  if(g.spaceId!==this.lastSpace){
   if(g.spaceId===this.maps.music.id)this.music={elapsed:0,phase:'melody',monster:null};
   if(g.spaceId===this.maps.science.id)Object.assign(this.science,{elapsed:0,phase:'waiting',dark:false,blackouts:0,monsters:[]});
   if(g.map.subjectEvent==='art')this.special.art=this.createMonster(g.map,'statue');
   if(g.map.subjectEvent==='gym')this.special.gym=this.createMonster(g.map,'dunky');
   this.lastSpace=g.spaceId;
  }
  if(g.caught||g.won)return;
  if(g.map.subjectEvent==='art'){const m=this.special.art,dx=m.x-g.x,dy=m.y-g.y,n=Math.hypot(dx,dy);const facing=g.lookDirection||g.direction;m.observed=n<1||((dx*facing.x+dy*facing.y)/n>Math.cos(Math.PI/3)&&this.wallSight(g.x,g.y,m.x,m.y));this.move(m,dt,this.pursuit.enabled&&!m.observed);}
  if(g.map.subjectEvent==='gym')this.move(this.special.gym,dt,this.pursuit.enabled);
  // Holding or having collected the specimen opens the rear exit for this run.
  if(this.has('lab-sample')||g.events.some(e=>e.type==='item-picked'&&e.itemId==='lab-sample'))this.science.rewardTaken=true;
  if(g.spaceId===this.maps.music.id){
   const s=this.music;s.elapsed+=dt;
   if(s.phase==='melody'&&s.elapsed>=32&&this.pursuit.enabled){s.phase='sting';g.emit('musician-sting')}
   if(s.phase==='sting'&&s.elapsed>=33){s.phase='chase';s.monster=this.createMonster(this.maps.music,'musician');g.emit('musician-spawn')}
   if(s.monster){this.roamMusician(s.monster,dt);this.move(s.monster,dt,this.pursuit.enabled);}
  }
  if(g.spaceId===this.maps.science.id){
   const s=this.science,was=s.dark;s.elapsed+=dt;s.dark=s.elapsed>=10&&(s.elapsed%10)<2;
   if(s.dark&&!was){s.blackouts++;g.emit('lab-lights-off',{count:s.blackouts})}
   if(!s.dark&&was){g.emit('lab-lights-on');if(s.blackouts>=3&&s.phase==='waiting'&&this.pursuit.enabled){s.phase='footsteps';s.spawnAt=s.elapsed+1.2;g.emit('shadow-footsteps')}}
   if(s.phase==='footsteps'&&s.elapsed>=s.spawnAt){s.phase='haunted';s.monsters=[0,1].map(n=>this.createMonster(this.maps.science,'shadow',n));g.emit('shadow-spawn')}
   for(const m of s.monsters)this.move(m,dt,s.dark&&this.pursuit.enabled);
  }
 }
 recoverEquipment(){
  // Death drops still apply. Move only the two entry tools to the outside door,
  // so a new life can retrieve them without needing the tools to enter.
  if(!this.game.caught||this.game.spaceId!==this.maps.science.id)return;
  const map=this.maps.science;
  for(const i of this.items.worldItems)if(!i.picked&&i.spaceId===map.id&&['lab-coat','flashlight'].includes(i.itemId)&&!i.sourceId){Object.assign(i,{spaceId:'floor-3',x:map.returnX,y:map.returnY,displayX:map.returnX,displayY:map.returnY,hidden:false,revealed:true})}
 }
 get dark(){return this.game.spaceId===this.maps.science.id&&this.science.dark&&!this.game.caught}
 get inEncounter(){return ['music','science','art','gym'].includes(this.game.map.subjectEvent)}
 get monsters(){if(!this.pursuit.enabled)return[];return this.game.spaceId===this.maps.music.id?(this.music.monster?[this.music.monster]:[]):this.game.spaceId===this.maps.science.id?this.science.monsters:['art','gym'].includes(this.game.map.subjectEvent)&&this.special[this.game.map.subjectEvent]?[this.special[this.game.map.subjectEvent]]:[]}
 get alerted(){return this.monsters.some(m=>m.kind!=='musician'||m.state==='chase')}
 get active(){return this.monsters.length>0}
 get nearest(){return this.monsters.slice().sort((a,b)=>Math.hypot(a.x-this.game.x,a.y-this.game.y)-Math.hypot(b.x-this.game.x,b.y-this.game.y))[0]}
 get musicTempo(){return 68+Math.min(1,this.music.elapsed/32)*152}
 get hint(){if(this.game.map.subjectEvent==='art')return '석상 · 바라보면 멈춥니다. 마우스로 시선을 돌리세요.';if(this.game.map.subjectEvent==='gym')return '덩키보이 · 직선에서 빨라집니다. 코너를 활용하세요.';if(this.game.spaceId===this.maps.music.id)return this.music.phase==='chase'?(this.music.monster?.state==='chase'?'거두 음악가가 쫓아옵니다 · 시야를 끊으세요.':'음악가가 배회합니다 · 발소리를 줄이세요.'):'피아노 선율이 점점 빨라집니다…';if(this.game.spaceId===this.maps.science.id)return (this.science.dark?'소등 · 이동할 수 있습니다.':'점등 · 쉐도어는 멈춥니다.')+' 뒷문 캐비넷에서 표본 회수 후 탈출.';return ''}
 guide(hints){if(hints==='none')return[];if(this.game.spaceId===this.maps.library.id)return ['도서실: 사서 자리와 서가를 자유롭게 조사할 수 있습니다. 안쪽 비상 물품 보관함에 손전등이 있습니다.'];if(this.game.spaceId===this.maps.science.id)return ['뒷문 옆 캐비넷에서 실험 표본을 가져온 뒤 뒷문으로 나가자.'];return []}

 snapshot(){
  const monster=m=>m?Object.fromEntries(['kind','index','x','y','direction','speed','distance','phase','grace','charge','state','goal','lastKnown','lastSensed','patrolWait'].filter(k=>m[k]!==undefined).map(k=>[k,m[k]])):null;
  return {library:{...this.library,sequence:[...this.library.sequence]},scienceRewardTaken:this.science.rewardTaken,
   encounter:{spaceId:this.lastSpace,music:{...this.music,monster:monster(this.music.monster)},science:{...this.science,monsters:this.science.monsters.map(monster)},special:Object.fromEntries(Object.entries(this.special).map(([id,m])=>[id,monster(m)]))}};
 }
 restore(d){
  if(!d)return;Object.assign(this.library,{step:0,solved:true,sequence:[]});this.science.rewardTaken=!!d.scienceRewardTaken;
  // Older saves do not contain these clocks. New saves resume the current room
  // without firing its entry/reset logic again on the first frame after load.
  const e=d.encounter;if(!e)return;
  const monster=(v,map)=>v?Object.assign(this.createMonster(map,v.kind,v.index||0),v,{path:[],repath:0}):null;
  this.lastSpace=e.spaceId;Object.assign(this.music,e.music,{monster:monster(e.music.monster,this.maps.music)});
  Object.assign(this.science,e.science,{monsters:e.science.monsters.map(v=>monster(v,this.maps.science))});
  for(const id of ['art','gym'])this.special[id]=monster(e.special?.[id],this.maps[id]);
 }
 inspect(){return {music:{phase:this.music.phase,elapsed:this.music.elapsed,tempo:this.musicTempo},science:{phase:this.science.phase,elapsed:this.science.elapsed,dark:this.dark,blackouts:this.science.blackouts,rewardTaken:this.science.rewardTaken},library:{...this.library},monsters:this.monsters.map(m=>({kind:m.kind,x:m.x,y:m.y,speed:m.speed,distance:m.distance,state:m.state,goal:m.goal})),inEncounter:this.inEncounter}}
}

function gameFloor(g,source){const f=g.spaces.get(source.spaceId);return f.sourceFloor||f.number}
