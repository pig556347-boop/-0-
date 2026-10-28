// Independent SchoolEscape exploration items. Furniture and floor placements
// use only this project's authored map and collision checks.
export const ITEM_CATALOG={
 'key-a':{id:'key-a',label:'열쇠 A',name:'열쇠 A',type:'key',description:'교문의 A 잠금장치를 여는 열쇠입니다.',color:'#d8b56c',icon:'A',usable:false,wearable:false},
 'key-b':{id:'key-b',label:'열쇠 B',name:'열쇠 B',type:'key',description:'교문의 B 잠금장치를 여는 열쇠입니다.',color:'#a8c5bf',icon:'B',usable:false,wearable:false},
 'key-c':{id:'key-c',label:'열쇠 C',name:'열쇠 C',type:'key',description:'교문의 C 잠금장치를 여는 열쇠입니다.',color:'#c0a8c7',icon:'C',usable:false,wearable:false},
 'vitamin-water':{id:'vitamin-water',label:'비타민워터',name:'비타민워터',type:'consumable',description:'사용하면 2초 동안 이동속도가 30% 증가합니다. 중복 사용해도 배율은 증가하지 않습니다.',color:'#d3a264',icon:'물',usable:true,wearable:false},
 'chocolate-bar':{id:'chocolate-bar',label:'초코바',name:'초코바',type:'consumable',description:'스태미나를 즉시 100% 회복하고 지침 상태를 해제합니다.',color:'#967b63',icon:'초',usable:true,wearable:false},
 'clock-second-hand':{id:'clock-second-hand',label:'시계초침',name:'시계초침',type:'component',description:'시계분침·시계본체와 함께 소지하면 나침반으로 자동 조합됩니다.',color:'#c7c1a5',icon:'초침',usable:false,wearable:false},
 'clock-minute-hand':{id:'clock-minute-hand',label:'시계분침',name:'시계분침',type:'component',description:'시계초침·시계본체와 함께 소지하면 나침반으로 자동 조합됩니다.',color:'#b5bca8',icon:'분침',usable:false,wearable:false},
 'clock-body':{id:'clock-body',label:'시계본체',name:'시계본체',type:'component',description:'초침과 분침을 모으면 세 부품이 사라지고 나침반이 생깁니다.',color:'#b6a782',icon:'시계',usable:false,wearable:false},
 'compass':{id:'compass',label:'나침반',name:'나침반',type:'tool',description:'미획득 열쇠 하나의 방향을 가리킵니다. 다른 공간이면 연결 문·계단을 가리키며, 그 열쇠를 얻으면 사라집니다.',color:'#d6c184',icon:'침',usable:false,wearable:false},
 'smartwatch':{id:'smartwatch',label:'스마트워치',name:'스마트워치',type:'wearable',description:'착용하면 같은 공간의 추격자가 가까울수록 심장박동 소리가 빨라집니다.',color:'#91aaa6',icon:'시계',usable:false,wearable:true}
};
export const ITEMS=ITEM_CATALOG;
const KEYS=['key-a','key-b','key-c'];
const PARTS=['clock-second-hand','clock-minute-hand','clock-body'];
const DIR={north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]};
const PRIMARY=new Set(['student-desk','teacher-desk','locker-bank','shelf-body','admin-desk','staff-desk','head-teacher-desk','music-desk','music-teacher-desk','old-vocal-desk','computer-workstation','computer-teacher-desk','nurse-desk','broadcast-announcer-desk','broadcast-club-desk','loan-counter','reading-table','individual-reading-desk','library-study-table','art-worktable','art-teacher-workbench','yard-trash-bin','yard-parcel-box','yard-water-station','staff-meeting-table','admin-document-worktable','art-sculpture-worktable']);
const hashSeed=value=>{if(Number.isFinite(value))return Number(value)>>>0;let h=2166136261;for(const c of String(value)){h^=c.charCodeAt(0);h=Math.imul(h,16777619);}return h>>>0};
const randomSource=seed=>{let a=seed>>>0;return()=>{a=(a+0x6d2b79f5)|0;let t=Math.imul(a^(a>>>15),1|a);t^=t+Math.imul(t^(t>>>7),61|t);return((t^(t>>>14))>>>0)/4294967296}};
const shuffle=(array,random)=>{const a=[...array];for(let i=a.length-1;i>0;i--){const j=Math.floor(random()*(i+1));[a[i],a[j]]=[a[j],a[i]];}return a};

export class ItemSystem {
 constructor(game,options={}){
  this.game=game;this.catalog={...ITEM_CATALOG};this.capacity=9;this.maxEquipped=1;this.options=options;
  const yard=(game.data.outdoorMaps||[]).find(f=>f.id==='playground');
  if(yard&&!game.data.escapeGate)throw Error('Outdoor map requires an authored escape gate');
  this.escapeGate={...(game.data.escapeGate||{spaceId:`floor-${game.data.spawnFloor}`,x:game.data.spawnX,y:Math.max(1,game.data.spawnY-3),label:'교문으로 탈출',range:1.7})};
  if(!game.spaces.has(this.escapeGate.spaceId))throw Error('Unknown escape gate space '+this.escapeGate.spaceId);
  this.navigation=new Map();this.searchTemplate=[];this.connections=new Map();this._buildLocations();this.reset(options.seed??game.runSeed);
 }
 reset(seed){
  if(seed===undefined){const value=new Uint32Array(1);if(globalThis.crypto?.getRandomValues)globalThis.crypto.getRandomValues(value);else value[0]=(Date.now()^Math.floor(Math.random()*4294967296))>>>0;seed=value[0];}
  this.seed=hashSeed(seed);this.random=randomSource(this.seed);this.inventory=[];this.equipped=null;this.worldItems=[];this.consumed=[];this.lastInteraction=-100;
  this.openSearch=null;this.openCompartment=0;this.serial=0;this.escaped=false;this.lastAction=null;this.game.won=false;this.searchables=this.searchTemplate.map(s=>({...s,searched:false,itemUid:null}));
  this.searchById=new Map(this.searchables.map(s=>[s.id,s]));this.game.speedBoostUntil=0;
  const floors=shuffle(this.game.data.floors.map(f=>f.number),this.random),modes=shuffle(['hidden','ground',this.random()<.5?'hidden':'ground'],this.random);
  if(this.game.spaces.has('playground')){
   const regions=shuffle(['playground','main',this.random()<.5?'playground':'main'],this.random);
   shuffle(KEYS,this.random).forEach((id,i)=>this._spawn(id,regions[i],modes[i]));
  }else KEYS.forEach((id,i)=>this._spawn(id,floors[i],modes[i]));
  for(const floor of this.game.data.floors.map(f=>f.number)){this._spawn('vitamin-water',floor,this.random()<.6?'hidden':'ground');this._spawn('chocolate-bar',floor,this.random()<.6?'hidden':'ground');}
  shuffle(PARTS,this.random).forEach((id,i)=>this._spawn(id,floors[i],this.random()<.7?'hidden':'ground'));
  this._spawn('smartwatch',1,this.random()<.5?'hidden':'ground');
  return this.inspect();
 }
 _buildLocations(){
  // Keys stay away from arrival/door/gate use areas; the navigation below
  // still proves every selected display/interaction point is reachable.
  this.keyExclusions=new Map();
  const avoid=(id,x,y)=>{if(!id||!Number.isFinite(x)||!Number.isFinite(y))return;if(!this.keyExclusions.has(id))this.keyExclusions.set(id,[]);this.keyExclusions.get(id).push({x,y});};
  for(const f of this.game.spaces.values()){
   if(f.isRoom){avoid(f.id,f.spawnX,f.spawnY);avoid(f.id,f.exitX,f.exitY+1);avoid(f.returnSpaceId||`floor-${f.sourceFloor}`,f.returnX,f.returnY);}
   for(const r of f.rooms||[])if(r.interiorId){const [dx,dy]=DIR[r.facing],target=this.game.spaces.get(r.interiorId);avoid(f.id,r.doorX+dx,r.doorY+dy);if(target)avoid(target.id,target.spawnX,target.spawnY);}
   for(const s of f.stairs||[]){avoid(f.id,s.x,s.y);avoid(s.targetId||`floor-${s.target}`,s.arrivalX,s.arrivalY);}
   for(const p of f.portals||[]){const [dx,dy]=DIR[p.facing];avoid(f.id,p.doorX+dx,p.doorY+dy);avoid(p.targetId,p.arrivalX,p.arrivalY);}
  }
  avoid(this.escapeGate.spaceId,this.escapeGate.x,this.escapeGate.y);
  for(const f of this.game.spaces.values()){
   const nodes=[],byKey=new Map();
   for(let y=0;y<f.height;y++)for(let x=0;x<f.width;x++)if(this.game.open(x,y,f)&&!this.game.blocked(x,y,f)){
    const node={x,y,id:nodes.length,edges:[]};nodes.push(node);byKey.set(`${x},${y}`,node.id);
   }
   for(const n of nodes)for(const [dx,dy] of [[1,0],[0,1]]){const id=byKey.get(`${n.x+dx},${n.y+dy}`);if(id===undefined)continue;const next=nodes[id];if(this.game.segmentClear(n.x,n.y,next.x,next.y,f)){n.edges.push(id);next.edges.push(n.id);}}
   let start=byKey.get(`${f.spawnX},${f.spawnY}`);
   if(start===undefined){let best=Infinity;for(const n of nodes){const d=(n.x-f.spawnX)**2+(n.y-f.spawnY)**2;if(d<best){best=d;start=n.id;}}}
   const reachable=new Set(start===undefined?[]:[start]),q=start===undefined?[]:[start];for(let i=0;i<q.length;i++)for(const id of nodes[q[i]].edges)if(!reachable.has(id)){reachable.add(id);q.push(id);}
   const nav={nodes,reachable,byKey,available:nodes.filter(n=>reachable.has(n.id))};this.navigation.set(f.id,nav);
   const assemblies=new Map();for(const o of f.fixtures||[])if(o.assembly&&o.solid){if(!assemblies.has(o.assembly))assemblies.set(o.assembly,[]);assemblies.get(o.assembly).push(o);}
   for(const item of f.fixtures||[]){
    const furnitureBody=item.solid&&['-desk','-table','-workbench','-crate'].some(suffix=>item.kind.endsWith(suffix));
    if(!PRIMARY.has(item.kind)&&!furnitureBody&&!item.kind.endsWith('-cabinet')&&!['cleaning-closet','cabinet','instrument-crate','art-supply-crate'].includes(item.kind))continue;
    const parts=item.kind==='shelf-body'&&item.assembly?assemblies.get(item.assembly)||[item]:[item],tile=this.game.data.tileSize;
    const x0=Math.min(...parts.map(o=>o.x-o.width/tile/2)),x1=Math.max(...parts.map(o=>o.x+o.width/tile/2));
    const y0=Math.min(...parts.map(o=>o.y-o.depth/tile/2)),y1=Math.max(...parts.map(o=>o.y+o.depth/tile/2));
    const x=(x0+x1)/2,y=(y0+y1)/2,pad=(this.game.data.playerRadius+.12)/tile;
    const points=[{x,y:y0-pad},{x:x1+pad,y},{x,y:y1+pad},{x:x0-pad,y}];
    const approaches=points.filter(p=>!this.game.blocked(p.x,p.y,f)&&nav.available.some(n=>Math.hypot(n.x-p.x,n.y-p.y)<1.5&&this.game.segmentClear(n.x,n.y,p.x,p.y,f)));
    if(!approaches.length)continue;
    const label=item.kind==='yard-trash-bin'?'쓰레기통':item.kind==='yard-parcel-box'?'택배 상자':item.kind==='yard-water-station'?'수도 주변':item.kind==='locker-bank'?'사물함':item.kind==='shelf-body'?'책장':item.kind.includes('teacher')?'교탁':item.kind.includes('cabinet')||item.kind==='cleaning-closet'?'캐비넷':item.kind.endsWith('-workbench')?'작업대':item.kind.endsWith('-crate')?'보관 상자':'책상';
    this.searchTemplate.push({compartments:item.compartments||(item.kind==='locker-bank'?8:1),id:item.assembly||item.id,fixtureId:item.id,spaceId:f.id,kind:item.kind,label:item.label||label,x,y,width:(x1-x0)*tile,depth:(y1-y0)*tile,height:item.elevation+item.height,approaches});
   }
   const edges=[];
   if(f.isRoom&&!f.noExit)edges.push({toId:f.returnSpaceId||`floor-${f.sourceFloor}`,x:f.exitX,y:f.exitY+1,label:'복도로 나가기',kind:'door'});
   {
    for(const s of f.stairs)edges.push({toId:s.targetId||`floor-${s.target}`,x:s.x,y:s.y,label:s.label,kind:'stairs'});
    for(const r of f.rooms)if(r.interiorId){const [dx,dy]=DIR[r.facing];edges.push({toId:r.interiorId,x:r.doorX+dx,y:r.doorY+dy,label:r.label+' 입구',kind:'door'});}
   }
   for(const p of f.portals||[]){if(p.locked)continue;const [dx,dy]=DIR[p.facing];edges.push({toId:p.targetId,x:p.doorX+dx,y:p.doorY+dy,label:p.label,kind:'door'});}
   this.connections.set(f.id,edges);
  }
 }
 _floor(f){return f.isRoom?f.sourceFloor:f.number}
 _selectedSpace(f,selection){if(f.noLoot)return false;return typeof selection==='number'?this._floor(f)===selection:selection==='main'?!f.isOutdoor&&!['annex','west'].includes(f.building)&&f.id!=='playground':f.id===selection}
 _safeKeyLocation(f,p){return!(this.keyExclusions.get(f.id)||[]).some(q=>Math.hypot(q.x-p.x,q.y-p.y)*this.game.data.tileSize<4)}
 _freshUid(itemId){return`run-${this.seed.toString(16)}-${++this.serial}-${itemId}`}
 _spawn(itemId,selection,mode){
  const g=this.game,isKey=KEYS.includes(itemId),spaces=[...g.spaces.values()].filter(f=>this._selectedSpace(f,selection));
  let source;
  if(mode==='hidden')source=shuffle(this.searchables.filter(s=>this._selectedSpace(g.spaces.get(s.spaceId),selection)&&!s.itemUid&&(!isKey||(this._safeKeyLocation(g.spaces.get(s.spaceId),s)&&s.approaches.every(p=>this._safeKeyLocation(g.spaces.get(s.spaceId),p))))),this.random)[0];
  if(mode==='hidden'&&isKey&&g.spaces.has('playground')&&!source)throw Error(`No safe hidden key source in ${selection}`);
  let spot;
  if(source){const a=source.approaches.find(p=>!isKey||this._safeKeyLocation(g.spaces.get(source.spaceId),p));spot={spaceId:source.spaceId,x:source.x,y:source.y,displayX:a.x,displayY:a.y,sourceId:source.id,hidden:true,revealed:false};}
  else{
   const maps=shuffle(spaces,this.random);
   for(const f of maps){const nodes=shuffle(this.navigation.get(f.id).available,this.random);
    const p=nodes.find(n=>Math.hypot(n.x-f.spawnX,n.y-f.spawnY)*g.data.tileSize>=2&&!(f.stairs||[]).some(s=>Math.hypot(s.x-n.x,s.y-n.y)*g.data.tileSize<2.5)&&!(this.connections.get(f.id)||[]).some(e=>Math.hypot(e.x-n.x,e.y-n.y)*g.data.tileSize<1.8)&&(!isKey||this._safeKeyLocation(f,n))&&!this.worldItems.some(o=>o.spaceId===f.id&&Math.hypot(o.displayX-n.x,o.displayY-n.y)*g.data.tileSize<1.2));
    if(p){spot={spaceId:f.id,x:p.x,y:p.y,displayX:p.x,displayY:p.y,sourceId:null,hidden:false,revealed:true};break;}
   }
  }
  if(!spot)throw Error(`No reachable item placement for ${itemId} in ${selection}`);
  const item={uid:this._freshUid(itemId),itemId,...spot,compartment:source?Math.floor(this.random()*source.compartments):0,picked:false};this.worldItems.push(item);if(source)source.itemUid=item.uid;return item;
 }
 _action(ok,message,extra={}){return this.lastAction={ok,message,...extra}}
 _canAct(){return!this.game.caught&&!this.game.won&&!this.game.pendingDoor&&!this.escaped}
 _within(point,map=this.game.map){return Math.hypot(point.x-this.game.x,point.y-this.game.y)*this.game.data.tileSize<=1.55&&this.game.segmentClear(this.game.x,this.game.y,point.x,point.y,map)}
 nearby(){
  if(!this._canAct())return null;
  const g=this.game,candidates=[];
  for(const item of this.worldItems){
   if(item.picked||item.spaceId!==g.spaceId||item.hidden)continue;
   if(this._within(item))candidates.push({type:'pickup',kind:'pickup',uid:item.uid,itemId:item.itemId,label:this.catalog[item.itemId].label,prompt:'줍기',x:item.x,y:item.y,distance:Math.hypot(item.x-g.x,item.y-g.y)*g.data.tileSize});
  }
  for(const s of this.searchables){
   if(s.spaceId!==g.spaceId)continue;const p=s.approaches.filter(p=>this._within(p)).sort((a,b)=>Math.hypot(a.x-g.x,a.y-g.y)-Math.hypot(b.x-g.x,b.y-g.y))[0];if(!p)continue;
   const item=this.worldItems.find(o=>o.uid===s.itemUid&&!o.picked),revealed=!!item?.revealed;
   candidates.push({type:'search',kind:'search',sourceId:s.id,itemUid:item?.uid||null,label:s.label,prompt:revealed?this.catalog[item.itemId].label+' 꺼내기':s.searched?'다시 조사':'조사',searched:s.searched,revealed,x:s.x,y:s.y,approachX:p.x,approachY:p.y,distance:Math.hypot(p.x-g.x,p.y-g.y)*g.data.tileSize+.12+(revealed?-.06:s.searched?.08:0)});
  }
  return candidates.sort((a,b)=>a.distance-b.distance||(a.type==='pickup'?-1:1))[0]||null;
 }
 interact(){
  if(!this._canAct())return this._action(false,'지금은 조사할 수 없습니다.');
  const near=this.nearby();if(!near)return this._action(false,'가까운 조사 대상이 없습니다.');
  this.openSearch=near;return this._action(true,'',{type:'open-container',target:near});
 }
 containerItems(compartment=0){const n=this.openSearch;if(!n)return[];return this.worldItems.filter(i=>!i.picked&&(n.type==='pickup'?i.spaceId===this.game.spaceId&&!i.hidden&&Math.hypot(i.x-n.x,i.y-n.y)*this.game.data.tileSize<.65:i.sourceId===n.sourceId&&(i.compartment||0)===compartment));}
 take(uid){const i=this.containerItems(this.openCompartment||0).find(i=>i.uid===uid);if(!i||!this._canAct())return this._action(false,'가져올 수 없습니다.');const n=this.nearby();if(this.openSearch.type==='pickup'?!this._within(i):(!n||n.sourceId!==this.openSearch.sourceId))return this._action(false,'대상에서 멀어졌습니다.');return this._collect(i);}
 selectCompartment(index){const source=this.searchById.get(this.openSearch?.sourceId);this.openCompartment=Math.max(0,Math.min((source?.compartments||1)-1,index));if(source){source.searched=true;this.onSearch?.(source);this.game.emit('search',{spaceId:this.game.spaceId,x:this.game.x,y:this.game.y,sourceId:source.id});}for(const i of this.containerItems(this.openCompartment))i.revealed=true;}
 _collect(item){
  if(!item||item.picked)return this._action(false,'이미 가져간 아이템입니다.');
  const willCraft=PARTS.includes(item.itemId)&&PARTS.every(id=>id===item.itemId||this.inventory.some(i=>i.itemId===id));
  const replacesCompass=KEYS.includes(item.itemId)&&this.inventory.some(i=>i.itemId==='compass'&&i.targetKeyUid===item.uid);
  if(this.inventory.length>=this.capacity&&!willCraft&&!replacesCompass)return this._action(false,'인벤토리가 가득 찼습니다. 사용하거나 내려놓아 자리를 만드세요.',{type:'inventory-full',uid:item.uid,itemId:item.itemId});
  item.picked=true;if(item.sourceId){const source=this.searchById.get(item.sourceId);if(source?.itemUid===item.uid)source.itemUid=null;}
  const carried={uid:item.uid,itemId:item.itemId,...(item.targetKeyUid?{targetKeyUid:item.targetKeyUid}:{})};
  if(willCraft){
   const removed=this.inventory.filter(i=>PARTS.includes(i.itemId));this.inventory=this.inventory.filter(i=>!PARTS.includes(i.itemId));this.consumed.push(...removed,carried);
   const compass={uid:this._freshUid('compass'),itemId:'compass',targetKeyUid:this._chooseCompassTarget()?.uid||null};this.inventory.push(compass);
   this.game.emit('item-picked',{spaceId:item.spaceId,x:item.displayX,y:item.displayY,itemId:item.itemId,uid:item.uid});
   this.game.emit('crafted',{itemId:'compass',uid:compass.uid,targetKeyUid:compass.targetKeyUid});
   return this._action(true,'시계 부품 3개가 합쳐져 나침반을 얻었습니다.',{type:'crafted',itemId:'compass',uid:compass.uid});
  }
  if(replacesCompass)this._expireCompass(item.uid);
  this.inventory.push(carried);this.game.emit('item-picked',{spaceId:item.spaceId,x:item.displayX,y:item.displayY,itemId:item.itemId,uid:item.uid});
  if(KEYS.includes(item.itemId))this._expireCompass(item.uid);
  return this._action(true,`${this.catalog[item.itemId].label} 획득`,{type:'pickup',itemId:item.itemId,uid:item.uid});
 }
 _chooseCompassTarget(){
  const owned=new Set(this.inventory.filter(i=>KEYS.includes(i.itemId)).map(i=>i.uid));
  const options=this.worldItems.filter(i=>KEYS.includes(i.itemId)&&!i.picked&&!owned.has(i.uid));
  return options.map(item=>({item,hops:this._spaceRoute(this.game.spaceId,item.spaceId)?.length??999})).sort((a,b)=>a.hops-b.hops||a.item.itemId.localeCompare(b.item.itemId))[0]?.item||null;
 }
 _expireCompass(keyUid){
  const expired=this.inventory.filter(i=>i.itemId==='compass'&&i.targetKeyUid===keyUid);this.inventory=this.inventory.filter(i=>!(i.itemId==='compass'&&i.targetKeyUid===keyUid));
  for(const item of this.worldItems)if(item.itemId==='compass'&&!item.picked&&item.targetKeyUid===keyUid){item.picked=true;item.consumed=true;expired.push(item);}
  if(expired.length){this.consumed.push(...expired);this.game.emit('compass-expired',{targetKeyUid:keyUid});}
 }
 use(slotIndex){
  if(!this._canAct())return this._action(false,'지금은 사용할 수 없습니다.');const item=this.inventory[slotIndex];if(!item)return this._action(false,'빈 칸입니다.');
  if(item.itemId==='smartwatch')return this.equip(slotIndex);
  if(item.itemId==='vitamin-water')this.game.speedBoostUntil=this.game.time+2;
  else if(item.itemId==='chocolate-bar'){this.game.stamina=this.game.maxStamina||100;this.game.exhausted=false;this.game.lastSprintTime=-100;}
  else return this._action(false,this.catalog[item.itemId].type==='key'?'교문 앞에서 E로 열쇠를 사용하세요.':'이 아이템은 소지하면 자동으로 작동합니다.');
  this.inventory.splice(slotIndex,1);this.consumed.push(item);this.game.emit('item-used',{itemId:item.itemId,uid:item.uid});return this._action(true,item.itemId==='vitamin-water'?'2초 동안 이동속도가 30% 증가합니다.':'스태미나를 모두 회복했습니다.',{type:'use',itemId:item.itemId});
 }
 equip(slotIndex){
  if(!this._canAct())return this._action(false,'지금은 착용할 수 없습니다.');const item=this.inventory[slotIndex];if(!item||!this.catalog[item.itemId]?.wearable)return this._action(false,'착용할 수 없는 아이템입니다.');
  const previous=this.equipped;this.equipped=item;if(previous)this.inventory[slotIndex]=previous;else this.inventory.splice(slotIndex,1);
  this.game.emit('equipped',{itemId:item.itemId,uid:item.uid});return this._action(true,`${this.catalog[item.itemId].label} 착용`,{type:'equip',itemId:item.itemId});
 }
 unequip(){
  if(!this._canAct())return this._action(false,'지금은 해제할 수 없습니다.');if(!this.equipped)return this._action(false,'착용 중인 아이템이 없습니다.');if(this.inventory.length>=this.capacity)return this._action(false,'착용을 해제하려면 인벤토리에 빈 칸이 필요합니다.');
  const item=this.equipped;this.inventory.push(item);this.equipped=null;this.game.emit('unequipped',{itemId:item.itemId,uid:item.uid});return this._action(true,`${this.catalog[item.itemId].label} 착용 해제`,{type:'unequip',itemId:item.itemId});
 }
 drop(slotIndex){
  if(!this._canAct())return this._action(false,'지금은 내려놓을 수 없습니다.');const item=this.inventory[slotIndex];if(!item)return this._action(false,'빈 칸입니다.');
  const result=this._dropItem(item);if(result.ok)this.inventory.splice(slotIndex,1);return result;
 }
 dropEquipped(){if(!this._canAct()||!this.equipped)return this._action(false,'내려놓을 착용 아이템이 없습니다.');const result=this._dropItem(this.equipped);if(result.ok)this.equipped=null;return result;}
 _dropItem(item){
  const g=this.game,candidates=[[0,0],[.45,0],[-.45,0],[0,.45],[0,-.45]],point=candidates.map(([dx,dy])=>({x:g.x+dx,y:g.y+dy})).find(p=>!g.blocked(p.x,p.y)&&g.segmentClear(g.x,g.y,p.x,p.y));
  if(!point)return this._action(false,'아이템을 안전하게 내려놓을 공간이 없습니다.');
  let world=this.worldItems.find(o=>o.uid===item.uid);if(!world){world={uid:item.uid,itemId:item.itemId};this.worldItems.push(world);}
  Object.assign(world,{spaceId:g.spaceId,x:point.x,y:point.y,displayX:point.x,displayY:point.y,sourceId:null,hidden:false,revealed:true,picked:false,...(item.targetKeyUid?{targetKeyUid:item.targetKeyUid}:{})});
  this.game.emit('item-dropped',{spaceId:g.spaceId,x:point.x,y:point.y,itemId:item.itemId,uid:item.uid});return this._action(true,`${this.catalog[item.itemId].label} 내려놓음`,{type:'drop',itemId:item.itemId,uid:item.uid});
 }
 _spaceRoute(from,to){
  if(from===to)return[];const q=[from],prev=new Map([[from,null]]);
  for(let i=0;i<q.length;i++)for(const edge of this.connections.get(q[i])||[])if(!prev.has(edge.toId)){
   prev.set(edge.toId,{from:q[i],edge});if(edge.toId===to){const route=[];for(let id=to;id!==from;){const p=prev.get(id);route.push({...p.edge,fromId:p.from});id=p.from;}return route.reverse();}q.push(edge.toId);
  }
  return null;
 }
 compass(){
  const compass=this.inventory.find(i=>i.itemId==='compass');if(!compass)return{active:false};
  const target=this.worldItems.find(i=>i.uid===compass.targetKeyUid&&!i.picked);if(!target)return{active:false,unavailable:true};
  const route=this._spaceRoute(this.game.spaceId,target.spaceId);if(route===null)return{active:false,unavailable:true};
  const next=route[0]||{x:target.displayX,y:target.displayY,label:this.catalog[target.itemId].label,kind:'key'},dx=next.x-this.game.x,dy=next.y-this.game.y;
  return{active:true,targetKeyUid:target.uid,targetItemId:target.itemId,targetLabel:this.catalog[target.itemId].label,targetSpaceId:target.spaceId,spaceId:this.game.spaceId,x:next.x,y:next.y,dx,dy,angle:Math.atan2(dx,dy),distance:Math.hypot(dx,dy)*this.game.data.tileSize,via:next.kind,label:next.label,hops:route.length};
 }
 route(){return this.compass()}
 hasAllKeys(){return KEYS.every(id=>this.inventory.some(i=>i.itemId===id))}
 nearbyGate(){const g=this.game,e=this.escapeGate;return!g.caught&&!g.pendingDoor&&!this.escaped&&g.spaceId===e.spaceId&&Math.hypot(g.x-e.x,g.y-e.y)*g.data.tileSize<=e.range&&g.segmentClear(g.x,g.y,e.x,e.y)?{...e,missing:KEYS.filter(id=>!this.inventory.some(i=>i.itemId===id)).map(id=>id.slice(-1).toUpperCase())}:null}
 tryEscape(){return this.consumeEscapeKeys()}
 consumeEscapeKeys(){
  const gate=this.nearbyGate();if(!gate)return this._action(false,'운동장 교문 앞에서 사용하세요.',{missing:KEYS.filter(id=>!this.inventory.some(i=>i.itemId===id)).map(id=>id.slice(-1).toUpperCase())});
  if(!this.hasAllKeys())return this._action(false,`열쇠 ${gate.missing.join('·')}가 필요합니다.`,{missing:gate.missing});
  const keys=this.inventory.filter(i=>KEYS.includes(i.itemId));this.inventory=this.inventory.filter(i=>!KEYS.includes(i.itemId));this.consumed.push(...keys);this.escaped=true;this.game.won=true;this.game.speed=0;this.game.isRunning=false;this.game.emit('escaped',{keys:KEYS.slice()});return this._action(true,'열쇠 A·B·C로 교문을 열었습니다.',{type:'escaped',missing:[]});
 }
 step(){
  const target=this.inventory.find(i=>i.itemId==='compass')?.targetKeyUid;if(target&&this.inventory.some(i=>i.uid===target))this._expireCompass(target);
 }
 inspect(){return{seed:this.seed,capacity:this.capacity,inventory:this.inventory.map(i=>({...i})),equipped:this.equipped?{...this.equipped}:null,worldItems:this.worldItems.map(i=>({...i})),searchables:this.searchables.map(s=>({...s,approaches:s.approaches.map(p=>({...p}))})),compass:this.compass(),keys:KEYS.map(id=>({id,label:this.catalog[id].label,owned:this.inventory.some(i=>i.itemId===id)})),hasAllKeys:this.hasAllKeys(),escaped:this.escaped,escapeGate:{...this.escapeGate},speedBoostUntil:this.game.speedBoostUntil,lastAction:this.lastAction};}
}
