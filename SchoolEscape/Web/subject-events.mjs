import {Pursuit} from './pursuit.mjs';
// Authored room events use simulation time: pause/inventory freezes their clocks.
export class SubjectEvents {
 constructor(game,items,pursuit,progression){
  Object.assign(this,{game,items,pursuit,progression});game.subjects=this;
  this.maps=Object.fromEntries(game.data.roomMaps.filter(r=>r.subjectEvent).map(r=>[r.subjectEvent,r]));
  this.lastSpace='';this.music={elapsed:0,phase:'idle',monster:null};this.science={elapsed:0,phase:'idle',dark:false,blackouts:0,monsters:[],rewardTaken:false};
  this.library={step:0,solved:false,sequence:progression.shuffle(['000','300','400','600','800','900']).slice(0,3)};
  this.equipmentSources={};this.installItems();
  const travel=game.travelGuard;game.travelGuard=(target,kind)=>{if(!this.canEnter(target))return false;return travel(target,kind)};
  const container=progression.checkContainer.bind(progression);progression.checkContainer=id=>{const src=items.searchById.get(id);if(src?.kind==='library-reward-cabinet'&&!this.library.solved){progression.warn('반납 기록 순서대로 분류 서가에 책을 되돌려 놓아야 한다.');return false}return container(id)};
  const guide=progression.guide.bind(progression);progression.guide=hints=>[...guide(hints),...this.guide(hints)];
 }
 installItems(){
  const p=this.progression,i=this.items;
  for(const [id,label,description] of [['lab-coat','실험복','과학실 출입 시 자동으로 걸칩니다. 소지품 1칸을 사용합니다.'],['flashlight','손전등','과학실 입장 장비. 소등 중에는 함께 꺼져 시야가 완전히 차단됩니다.'],['lab-sample','밀봉된 실험 표본','과학실 뒷문 캐비넷에서 회수한 표본. 획득하면 뒷문을 열 수 있습니다.']]){
   p.register(id,label);Object.assign(i.catalog[id],{description,icon:id==='flashlight'?'빛':id==='lab-coat'?'옷':'표본'});
  }
  const coat=i.searchables.find(s=>s.spaceId==='room-f2-15'&&s.kind.includes('cabinet'));
  const light=i.searchables.find(s=>s.kind==='library-reward-cabinet'),reward=i.searchables.find(s=>s.kind==='science-reward-cabinet'),record=i.searchables.find(s=>s.kind==='loan-counter');
  if(!coat||!light||!reward||!record)throw Error('Missing subject encounter supply fixture');
  p.place('lab-coat',coat);p.place('flashlight',light);p.place('vitamin-water',light);p.place('lab-sample',reward);p.place('chocolate-bar',reward);
  this.equipmentSources={'lab-coat':coat.id,flashlight:light.id};
  p.note(coat,'science-safety','과학실 안전 수칙','실험복과 손전등을 소지하고 앞문으로 들어갈 것.\n10초마다 2초간 모든 빛이 꺼진다. 세 번째 소등이 끝나면 발소리를 경계할 것.\n뒷문 캐비넷의 표본을 회수한 뒤 뒷문으로 나갈 것.');
  const titles={'000':'색인 없는 사전','300':'사라진 학생의 사회','400':'그림자의 생물학','600':'끝나지 않는 악보','800':'마지막 동화','900':'폐교의 연대기'};
  this.bookTitles=titles;
  p.note(record,'library-return-log','도서 반납 기록',this.library.sequence.map((code,n)=>`${n+1}. ${titles[code]} · 분류 ${code}`).join('\n')+'\n\n기록 순서대로 각 분류 서가에서 같은 책을 반납한다. 틀린 책은 소리를 낸다. 세 권을 마치면 안쪽 보관함이 열린다.');
  p.note(light,'library-next-clue','손전등 옆 메모','실험복은 본관 3층 과학 자료실의 캐비넷에 있다.\n실험복과 손전등을 챙겨 과학실 앞문으로 들어가자.');
 }
 has(id){return this.items.inventory.some(i=>i.itemId===id)||this.items.equipped?.itemId===id}
 canEnter(target){if(target!==this.maps.science.id)return true;const missing=['lab-coat','flashlight'].filter(id=>!this.has(id));if(missing.length){this.progression.warn('과학실 입장 준비: '+missing.map(id=>this.items.catalog[id].label).join(' + ')+' 필요');return false}return true}
 intercept(door){
  if(this.game.spaceId===this.maps.science.id&&door.id==='science-rear-exit'&&!this.science.rewardTaken)return '뒷문 옆 캐비넷의 밀봉된 실험 표본을 먼저 가져가자.';
  return null;
 }
 chooseBook(source,code){
  const fixture=this.maps.library.fixtures.find(f=>f.id===source?.fixtureId);
  if(this.game.spaceId!==this.maps.library.id||!fixture?.bookCode||!this.items.nearby()||this.items.nearby().sourceId!==source.id)return {ok:false,message:'분류 서가 가까이에서 조사하세요.'};
  if(this.library.solved)return {ok:true,message:'세 권의 반납이 끝났다. 안쪽 보관함을 열자.'};
  const correct=fixture.bookCode===code&&this.library.sequence[this.library.step]===code;
  if(!correct){this.library.step=0;this.game.emit('library-mistake');this.game.emit('noise',{spaceId:this.game.spaceId,x:this.game.x,y:this.game.y,radius:18,loudness:1,kind:'wrong-book'});return {ok:false,message:'책이 바닥에 쏟아졌다. 반납 순서를 처음부터 다시 확인하자.'}}
  this.library.step++;this.library.solved=this.library.step===3;this.game.emit('library-correct',{complete:this.library.solved});return {ok:true,message:this.library.solved?'찰칵. 안쪽 반납 완료 보관함이 열렸다.':`${this.library.step} / 3권 반납. 다음 기록을 확인하자.`};
 }
 createMonster(map,kind,index=0){
  const g=this.game,nav=this.items.navigation.get(map.id),candidates=nav.available.filter(n=>Math.hypot(n.x-g.x,n.y-g.y)*g.data.tileSize>7);
  const anchor=kind==='musician'?{x:6,y:16}:index?{x:2,y:12}:{x:22,y:8};
  const spawn=candidates.sort((a,b)=>Math.hypot(a.x-anchor.x,a.y-anchor.y)-Math.hypot(b.x-anchor.x,b.y-anchor.y))[0]||nav.available.at(-1);
  const m=Object.create(Pursuit.prototype);Object.assign(m,{game:g,grids:this.pursuit.grids,radius:.27,spaceId:map.id,kind,index,x:spawn.x,y:spawn.y,direction:{x:0,y:-1},speed:0,distance:0,phase:0,path:[],repath:0,grace:1.5});return m;
 }
 move(m,dt,enabled){
  m.speed=0;m.grace=Math.max(0,m.grace-dt);if(!enabled)return;
  const g=this.game;m.repath-=dt;if(m.repath<=0||!m.path.length){m.path=m.route(g.x,g.y);m.repath=.45}
  const speed=m.kind==='musician'?3.15:3.6;let budget=speed*dt/g.data.tileSize,travel=0;
  while(budget>0&&m.path.length){const p=m.path[0],dx=p.x-m.x,dy=p.y-m.y,n=Math.hypot(dx,dy);if(n<.001){m.path.shift();continue}const d=Math.min(budget,n),x=m.x+dx/n*d,y=m.y+dy/n*d;if(g.blocked(x,y,g.map,m.radius)||!g.segmentClear(m.x,m.y,x,y,g.map,m.radius)){m.path=[];break}m.x=x;m.y=y;m.direction={x:dx/n,y:dy/n};budget-=d;travel+=d;if(d>=n-.001)m.path.shift()}
  m.speed=dt?travel*g.data.tileSize/dt:0;m.distance+=travel*g.data.tileSize;m.phase+=travel*5;
  if(m.grace<=0&&Math.hypot(m.x-g.x,m.y-g.y)*g.data.tileSize<.68&&g.segmentClear(m.x,m.y,g.x,g.y,g.map,.025)){g.caught=true;g.emit('caught',{source:m.kind==='musician'?'거두 음악가':'쉐도어'})}
 }
 step(dt){
  const g=this.game;dt=Math.max(0,Math.min(.05,dt));
  if(g.spaceId!==this.lastSpace){
   if(g.spaceId===this.maps.music.id)this.music={elapsed:0,phase:'melody',monster:null};
   if(g.spaceId===this.maps.science.id)Object.assign(this.science,{elapsed:0,phase:'waiting',dark:false,blackouts:0,monsters:[]});
   this.lastSpace=g.spaceId;
  }
  if(g.caught||g.won)return;
  // Holding or having collected the specimen opens the rear exit for this run.
  if(this.has('lab-sample')||g.events.some(e=>e.type==='item-picked'&&e.itemId==='lab-sample'))this.science.rewardTaken=true;
  if(g.spaceId===this.maps.music.id){
   const s=this.music;s.elapsed+=dt;
   if(s.phase==='melody'&&s.elapsed>=32&&this.pursuit.enabled){s.phase='sting';g.emit('musician-sting')}
   if(s.phase==='sting'&&s.elapsed>=33){s.phase='chase';s.monster=this.createMonster(this.maps.music,'musician');g.emit('musician-spawn')}
   if(s.monster)this.move(s.monster,dt,this.pursuit.enabled);
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
 get inEncounter(){return ['music','science'].includes(this.game.map.subjectEvent)}
 get monsters(){if(!this.pursuit.enabled)return[];return this.game.spaceId===this.maps.music.id?(this.music.monster?[this.music.monster]:[]):this.game.spaceId===this.maps.science.id?this.science.monsters:[]}
 get active(){return this.monsters.length>0}
 get nearest(){return this.monsters.slice().sort((a,b)=>Math.hypot(a.x-this.game.x,a.y-this.game.y)-Math.hypot(b.x-this.game.x,b.y-this.game.y))[0]}
 get musicTempo(){return 68+Math.min(1,this.music.elapsed/32)*152}
 get hint(){if(this.game.spaceId===this.maps.music.id)return this.music.phase==='chase'?'거두 음악가 · 출입문으로 도망치세요.':'피아노 선율이 점점 빨라집니다…';if(this.game.spaceId===this.maps.science.id)return (this.science.dark?'소등 · 이동할 수 있습니다.':'점등 · 쉐도어는 멈춥니다.')+' 뒷문 캐비넷에서 표본 회수 후 탈출.';return ''}
 guide(hints){const lines=['과학실: 실험복 + 손전등 소지 → 앞문 입장 → 뒷문 캐비넷 표본 회수 → 뒷문 퇴장.','도서실: 반납 기록의 순서와 분류번호를 맞추면 보관함이 열린다.'];if(hints!=='none')lines.push('손전등은 도서실 반납 퍼즐 보상, 실험복은 본관 3층 과학 자료실 캐비넷.','소등은 10초마다 2초. 세 번째 소등 이후 두 쉐도어가 어둠에서만 움직인다.');return lines}
 snapshot(){return {library:{...this.library,sequence:[...this.library.sequence]},scienceRewardTaken:this.science.rewardTaken}}
 restore(d){if(!d)return;Object.assign(this.library,d.library);this.science.rewardTaken=!!d.scienceRewardTaken}
 inspect(){return {music:{phase:this.music.phase,elapsed:this.music.elapsed,tempo:this.musicTempo},science:{phase:this.science.phase,elapsed:this.science.elapsed,dark:this.dark,blackouts:this.science.blackouts,rewardTaken:this.science.rewardTaken},library:{...this.library},monsters:this.monsters.map(m=>({kind:m.kind,x:m.x,y:m.y,speed:m.speed,distance:m.distance})),inEncounter:this.inEncounter}}
}
