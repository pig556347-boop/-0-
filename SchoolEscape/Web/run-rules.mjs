export const MODES={easy:{label:'이지',lives:5,stamina:200,hints:'exact'},normal:{label:'노말',lives:5,stamina:150,hints:'clue'},hard:{label:'하드',lives:3,stamina:100,hints:'none'},extreme:{label:'익스트림',lives:1,stamina:100,hints:'none'}};
export function rng(seed){let x=seed>>>0;return()=>{x=(Math.imul(x,1664525)+1013904223)>>>0;return x/4294967296}}
export function prepareRun(data,seed){
 const random=rng(seed),shuffle=a=>{a=[...a];for(let n=a.length-1;n>0;n--){const j=Math.floor(random()*(n+1));[a[n],a[j]]=[a[j],a[n]]}return a};
 for(const floor of data.floors){
  const doors=floor.rooms.filter(d=>['classroom','staff'].includes(d.interiorKind)),original=doors.map(d=>({label:d.label,interiorId:d.interiorId,interiorKind:d.interiorKind}));
  shuffle(original).forEach((entry,i)=>{const door=doors[i];Object.assign(door,entry);const room=data.roomMaps.find(r=>r.id===entry.interiorId),[dx,dy]=({north:[0,1],south:[0,-1],east:[1,0],west:[-1,0]})[door.facing];room.returnX=door.doorX+dx;room.returnY=door.doorY+dy});
 }
 for(const room of data.roomMaps.filter(r=>r.kind==='classroom')){
  const themes=['일반 교실','파손된 교실','핏자국 교실','비어 있는 교실'];room.theme=themes[Math.floor(random()*themes.length)];
  const desks=room.fixtures.filter(o=>o.kind==='student-desk').map(o=>({...o,keep:random()>(room.theme==='비어 있는 교실'?.8:room.theme==='파손된 교실'?.45:.2),dx:(random()-.5)*.36,dy:0}));
  room.fixtures=room.fixtures.filter(o=>{const desk=desks.find(d=>Math.abs(d.x-o.x)<.50&&o.y-d.y>-.92&&o.y-d.y<.4);if(!desk)return true;if(!desk.keep)return false;o.x+=desk.dx;o.y+=desk.dy;if(room.theme==='파손된 교실'&&o.kind==='student-desk')o.color='#514a3e';return true});
  if(room.theme==='파손된 교실')for(let n=0;n<8;n++)room.fixtures.push({id:room.id+'-splinter-'+n,room:room.label,kind:'broken-desk-plank',x:3+random()*12,y:5+random()*9,width:.55+random()*.6,depth:.12,height:.06,elevation:.03,color:'#71604a',solid:false,shape:'box'});
  if(room.theme==='핏자국 교실')for(let n=0;n<6;n++)room.fixtures.push({id:room.id+'-stain-'+n,room:room.label,kind:'old-blood-stain',x:3+random()*12,y:5+random()*9,width:.4+random()*.8,depth:.25+random()*.6,height:.008,elevation:.028,color:'#492b29',solid:false,shape:'sphere'});
 }
 return data;
}
const clues={gym:'높은 천장 아래 공이 튀던 넓은 마룻바닥의 흔적.',fitness:'무거운 철과 운동기구가 있던 자리.',cafeteria:'식판과 배식대, 오래된 음식 냄새가 남아 있다.',foodStorage:'식자재 상자와 배식 물품을 보관하던 곳.',annexCorridor:'체육 시설을 연결하는 복도와 진열장 부근.',sportsStorage:'공과 체육용품을 보관하던 먼지 낀 공간.',gymControl:'작은 방의 조명과 음향 스위치 곁.',studentLounge:'학생들이 운동 후 쉬던 소파와 책 주변.',sportsClass:'운동 전술을 배우던 칠판과 책상 주변.',sportsPractice:'넓은 매트와 연습용 바가 있던 곳.',classroom:'분필 가루와 오래된 공책 냄새. 학생들이 수업을 듣던 곳을 살펴보자.',staff:'채점하던 종이와 교직원들의 흔적이 남아 있다.',infirmary:'약품 냄새와 하얀 천의 흔적. 몸을 돌보던 방이 떠오른다.',preparation:'약품인지 미술 재료인지 알 수 없는 냄새. 수업 준비 공간이 생각난다.',art:'마른 물감과 석고 가루가 묻어 있다.',artStorage:'물감과 오래 보관한 도구 냄새가 섞여 있다.',music:'건반과 악보, 오래된 악기 소리가 떠오른다.',library:'묵은 종이 냄새가 유난히 짙다.',archives:'오래된 종이와 기록이 쌓여 있는 곳의 흔적이다.',computer:'꺼진 화면과 전선, 먼지 낀 기계 냄새가 남아 있다.',broadcast:'목소리를 멀리 보내던 장비 곁에 단서가 있을 것 같다.',administration:'서류와 도장, 학교의 살림을 맡던 흔적이다.',studentRestroom:'물기와 타일, 넓은 거울이 있는 학생들의 공간.',staffRestroom:'물기와 타일 냄새. 교직원이 쓰던 조용한 공간.',storage:'오래 보관한 상자와 먼지의 냄새.',playground:'흙과 바깥바람 냄새. 건물 밖을 살펴보자.'};
export class Survival {
 constructor(game,items,pursuit,mode='easy'){this.game=game;this.items=items;this.pursuit=pursuit;this.setMode(mode)}
 setMode(mode){this.mode=MODES[mode]?mode:'easy';this.rules=MODES[this.mode];this.lives=this.rules.lives;this.game.maxStamina=this.rules.stamina;this.game.stamina=this.rules.stamina;this.handled=false;this.lastDeath=null}
 capture(){if(!this.game.caught||this.handled)return;this.handled=true;this.lives=Math.max(0,this.lives-1);this.lastDeath={spaceId:this.game.spaceId,x:this.game.x,y:this.game.y};
  if(this.mode!=='easy'&&this.mode!=='extreme'){
   const carried=[...this.items.inventory,...(this.items.equipped?[this.items.equipped]:[])];
   const points=this.items.navigation.get(this.game.spaceId).available.filter(p=>Math.hypot(p.x-this.game.x,p.y-this.game.y)*this.game.data.tileSize<=8);
   for(const item of carried){let point=this.lastDeath;if(this.mode==='hard'&&points.length)point=points[Math.floor(this.items.random()*points.length)];let world=this.items.worldItems.find(i=>i.uid===item.uid);if(!world){world={...item};this.items.worldItems.push(world)}Object.assign(world,{...item,spaceId:this.lastDeath.spaceId,x:point.x,y:point.y,displayX:point.x,displayY:point.y,picked:false,hidden:false,revealed:true,sourceId:null})}
   this.items.inventory=[];this.items.equipped=null;
  }
 }
 revive(){if(!this.handled||this.lives<=0)return false;const g=this.game;g.caught=false;g.pendingDoor=null;g.warp(1,g.data.spawnX,g.data.spawnY);g.stamina=g.maxStamina;g.exhausted=false;g.speedBoostUntil=0;this.pursuit.reset((this.items.seed+this.lives*71)>>>0);this.pursuit.catchGraceUntil=g.time+5;this.handled=false;g.emit('revived');return true}
 guide(){const g=this.game,s=this.items,lines=['해야 할 일: 본관과 운동장에서 열쇠 A·B·C를 모아 운동장 남쪽 교문에서 E로 사용한다.','현관 E: 본관 ↔ 운동장. 수납함은 E로 연 뒤 칸과 아이템을 직접 클릭한다.'];
  if(this.rules.hints==='none')return [...lines,'이 난이도에서는 열쇠 위치 단서가 제공되지 않는다.'];
  for(const id of ['key-a','key-b','key-c']){const owned=s.inventory.some(i=>i.itemId===id),item=s.worldItems.find(i=>i.itemId===id&&!i.picked);if(owned){lines.push(s.catalog[id].label+': 확보했다.');continue}if(!item)continue;const map=g.spaces.get(item.spaceId),source=item.sourceId?s.searchById.get(item.sourceId):null;
   const where=map.isOutdoor?'운동장':`${map.building==='annex'?'별관':'본관'} ${map.sourceFloor||map.number}층`;
   lines.push(s.catalog[id].label+': '+(this.rules.hints==='exact'?`${where} · ${map.label||map.subtitle}. ${source?source.label+'의 '+(item.compartment+1)+'번 칸':'바닥'}을 확인하자.`:`${where}에서 ${clues[map.kind]||'사람들이 머물던 방의 가구와 주변을 살펴보자.'}`));
  }
  if(this.lastDeath&&this.mode!=='easy')lines.push('떨어뜨린 소지품은 마지막으로 붙잡힌 공간에서 다시 찾을 수 있다.');return lines;
 }
}
