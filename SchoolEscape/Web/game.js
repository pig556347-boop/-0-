import {createLobby} from './lobby.mjs';
import {SubjectEvents} from './subject-events.mjs';
import {createSubjectVisuals} from './subject-visual.mjs';
import {Progression} from './progression.mjs';
import {requestedSave,saveRun,restoreRun} from './save-run.mjs';
import {KitchenSequence,createKitchenVisual} from './kitchen-sequence.mjs';
import {MODES,prepareRun,Survival} from './run-rules.mjs';
import {createDiscoveryUI} from './discovery-ui.mjs';
import * as T from './vendor/three.module.min.js';
import {Exploration,directions} from './kernel.mjs';
import {SchoolMirror} from './mirror.mjs';
import {Pursuit} from './pursuit.mjs';
import {createPursuerVisual} from './pursuer-visual.mjs';
import {SchoolAudio} from './audio.mjs';
import {ItemSystem} from './items.mjs';
import {createInventoryUI} from './inventory-ui.mjs';
import {createLootVisuals} from './loot-visual.mjs';
const $=id=>document.getElementById(id);
try {
 const embedded=globalThis.SCHOOL_ESCAPE_ASSETS;
 const response=embedded?{ok:true,json:async()=>embedded.map}:await fetch('../Assets/Resources/SchoolMap.json');if(!response.ok)throw Error('지도 불러오기 실패');
 const originalData=await response.json();const runSeed=new Uint32Array(1);crypto.getRandomValues(runSeed);const savedRun=requestedSave();if(savedRun)runSeed[0]=savedRun.seed;const data=prepareRun(originalData,runSeed[0]),game=new Exploration(data),tile=data.tileSize;
 function freshSeed(){const seed=new Uint32Array(1);if(globalThis.crypto?.getRandomValues)crypto.getRandomValues(seed);else seed[0]=(Date.now()^Math.floor(Math.random()*4294967296))>>>0;return seed[0]}
 game.runSeed=runSeed[0];
 const pursuit=new Pursuit(game),audio=new SchoolAudio(),items=new ItemSystem(game,{seed:game.runSeed});pursuit.reset(game.runSeed);const survival=new Survival(game,items,pursuit,sessionStorage.getItem('school.mode')||'easy');const kitchen=new KitchenSequence(game,pursuit,survival);const progression=new Progression(game,items);const subjects=new SubjectEvents(game,items,pursuit,progression);items.onSearch=source=>kitchen.observeSearch(source);if(savedRun)restoreRun(savedRun,{game,items,survival,kitchen,progression,pursuit,subjects});
 const canvas=$('world'),renderer=new T.WebGLRenderer({canvas,antialias:true,alpha:false});
 renderer.setPixelRatio(Math.min(devicePixelRatio,1.8));renderer.shadowMap.enabled=true;renderer.shadowMap.type=T.PCFSoftShadowMap;
 renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.ACESFilmicToneMapping;renderer.toneMappingExposure=1.18;
 const scene=new T.Scene();scene.background=new T.Color('#0c1721');scene.fog=new T.FogExp2('#0c1721',.009);
 const camera=new T.OrthographicCamera(-12,12,8.5,-8.5,.5,100);
 const ambient=new T.HemisphereLight('#9eb8d3','#444c37',1.5);scene.add(ambient);
 const moon=new T.DirectionalLight('#b9cadb',2.0);moon.position.set(-12,30,10);moon.castShadow=true;
 moon.shadow.mapSize.set(2048,2048);Object.assign(moon.shadow.camera,{left:-38,right:38,top:38,bottom:-38,near:1,far:100});moon.shadow.bias=-.001;scene.add(moon);scene.add(moon.target);
 const textures=new T.TextureLoader();
 const loadTexture=path=>{const t=textures.load(embedded?.[path]||path);t.colorSpace=T.SRGBColorSpace;t.wrapS=t.wrapT=T.RepeatWrapping;return t};
 const floorTexture=loadTexture('../Assets/Resources/FloorTexture.png'),wallTexture=loadTexture('../Assets/Resources/WallTexture.png'),yardTexture=loadTexture('../Assets/Resources/YardTexture.png');yardTexture.repeat.set(16,13);
 const mat=(color,extras={})=>new T.MeshStandardMaterial({color,roughness:.95,...extras});
 const materials={floor:mat('#8c9185',{map:floorTexture}),wall:mat('#8f9a8d',{map:wallTexture}),cap:mat('#303f43'),lower:mat('#344c49'),wood:mat('#806347',{map:wallTexture,emissive:'#46301e',emissiveIntensity:.22}),glass:mat('#294956',{metalness:.25,roughness:.5}),metal:mat('#55666a'),gold:mat('#ac925f'),lamp:mat('#cfc292',{emissive:'#a78e53',emissiveIntensity:.8}),paper:mat('#b7b3a1'),frame:mat('#b6b09a'),threshold:mat('#a79a75',{emissive:'#514329',emissiveIntensity:.16})};
 const yardMaterial=mat('#a59a7b',{map:yardTexture});
 const unit=new T.BoxGeometry(1,1,1),world=new T.Group();scene.add(world);const floorRoots=[],doorVisuals=[],mirrors=[];
 const pos=(x,y,h=0)=>new T.Vector3((x-24)*tile,h,-(y-19)*tile);
 const box=(name,parent,p,s,m,shadow=false)=>{const o=new T.Mesh(unit,m);o.name=name;o.position.copy(p);o.scale.set(...s);o.castShadow=shadow;o.receiveShadow=true;parent.add(o);return o};
 function textSprite(text,parent,p,width=2.2,height=.45,color='#d7d9c6'){
   const c=document.createElement('canvas');c.width=768;c.height=128;const ctx=c.getContext('2d');
   ctx.fillStyle='#101e25';ctx.fillRect(0,0,768,128);ctx.strokeStyle='#a9ab8655';ctx.strokeRect(4,4,760,120);
   ctx.font='64px School, sans-serif';ctx.textAlign='center';ctx.textBaseline='middle';ctx.fillStyle=color;ctx.fillText(text,384,65,730);
   const map=new T.CanvasTexture(c),s=new T.Sprite(new T.SpriteMaterial({map,depthTest:true,transparent:true}));s.position.copy(p);s.scale.set(width,height,1);parent.add(s);return s;
 }
 await document.fonts.load('20px School');
 const dummy=new T.Object3D();
 function instanceBoxes(parent,name,entries,material){
   const mesh=new T.InstancedMesh(unit,material,entries.length);mesh.name=name;mesh.receiveShadow=true;
   entries.forEach(([p,s],i)=>{dummy.position.copy(p);dummy.scale.set(...s);dummy.rotation.set(0,0,0);dummy.updateMatrix();mesh.setMatrixAt(i,dummy.matrix)});parent.add(mesh);return mesh;
 }
 for(const f of [...data.floors,...data.roomMaps,...(data.outdoorMaps||[])]){
   const root=new T.Group();root.name=f.isOutdoor||f.isRoom?f.label:`본관 ${f.number}F`;root.userData.spaceId=f.id;floorRoots.push(root);world.add(root);
   const tiles=[],walls=[],caps=[],lower=[],windows=[],mullions=[],lamps=[],papers=[],trim=[];
   const interiorWall=(x,y)=>f.isRoom;
   const doorCells=new Set([...f.rooms,...(f.portals||[])].map(r=>`${r.doorX},${r.doorY}`)),isDoor=(x,y)=>doorCells.has(`${x},${y}`);
   if(!f.voidRects)box('Ground',root,pos((f.width-1)/2,(f.height-1)/2,-.1),[f.width*tile+2,.2,f.height*tile+2],f.isOutdoor?yardMaterial:f.isRoom?mat({classroom:'#757a66',music:'#79664b',art:'#8c8874',artStorage:'#777762',library:'#786e55',infirmary:'#89988c',administration:'#7c8070',staff:'#7a8274',computer:'#737f7d',broadcast:'#7f7765',studentRestroom:'#83958a',staffRestroom:'#9b9e8c',storage:'#777a67',assembly:'#7e7665',maintenance:'#6b7976',seminar:'#7e8070',preparation:'#858171',archives:'#7d7866',lounge:'#7f806a',counseling:'#828776',meeting:'#7f7b6c',principal:'#7b715f',control:'#6b7e7d'}[f.kind]||'#757a66',{map:floorTexture}):materials.floor);
   const isVoid=(x,y)=>(f.voidRects||[]).some(r=>x>=r[0]&&y>=r[1]&&x<=r[2]&&y<=r[3]);
   for(let y=0;y<f.height;y++)for(let x=0;x<f.width;){
     if(isVoid(x,y)){x++;continue;}
     if(game.open(x,y,f)){x++;continue}const start=x,cut=isDoor(x,y),interior=interiorWall(x,y);while(x<f.width&&!isVoid(x,y)&&!game.open(x,y,f)&&isDoor(x,y)===cut&&interiorWall(x,y)===interior)x++;
     const height=cut?.18:f.isOutdoor?.3:interior?(y===f.height-1?1.4:.5):1.6;
     const p=pos((start+x-1)/2,y,height/2);walls.push([p,[(x-start)*tile,height,tile]]);
     caps.push([pos((start+x-1)/2,y,height+.01),[(x-start)*tile,.035,tile]]);
   }
   for(let y=0;y<f.height;y++)for(let x=0;x<f.width;x++){
     if(!game.open(x,y,f))continue;
     if(!f.isOutdoor)tiles.push([pos(x,y,.009),[tile-.025,.018,tile-.025]]);
     if(!f.isOutdoor&&(x*17+y*11+f.number)%43===0)papers.push([pos(x+.13,y-.19,.025),[.22,.012,.30]]);
     for(const [dx,dy] of [[-1,0],[1,0],[0,-1],[0,1]])if(!game.open(x+dx,y+dy,f)){
       if(f.isOutdoor||isDoor(x+dx,y+dy))continue;
       lower.push([pos(x+dx*.49,y+dy*.49,.24),[dx?.085:tile,.48,dy?.085:tile]]);
       trim.push([pos(x+dx*.48,y+dy*.48,.51),[dx?.09:tile,.035,dy?.09:tile]]);
       if(!interiorWall(x+dx,y+dy)&&(x+y)%5===0){windows.push([pos(x+dx*.485,y+dy*.485,1.05),[dx?.10:.8,.53,dy?.10:.8]]);mullions.push([pos(x+dx*.48,y+dy*.48,1.05),[dx?.12:.035,.53,dy?.12:.035]])}
       if(!interiorWall(x+dx,y+dy)&&(x+y)%9===0){
         box('Wall light',root,pos(x+dx*.43,y+dy*.43,1.42),[.19,.12,.19],materials.lamp);
         // Warm light pools are static mood lighting, not events.
         if((x+y)%27===0){const l=new T.PointLight('#e6be79',3.8,5,2);l.position.copy(pos(x+dx*.30,y+dy*.30,2.0));root.add(l)}
       }
     }
   }
   instanceBoxes(root,'Worn tiles',tiles,materials.floor);instanceBoxes(root,'Solid room and wall volumes',walls,materials.wall);instanceBoxes(root,'Opaque wall caps',caps,materials.cap);instanceBoxes(root,'Green wainscot',lower,materials.lower);instanceBoxes(root,'Dusty windows',windows,materials.glass);instanceBoxes(root,'Window frames',mullions,materials.metal);instanceBoxes(root,'Abandoned papers',papers,materials.paper);instanceBoxes(root,'Wall trim',trim,materials.metal);
   const batches=new Map(),geometries={box:unit,sphere:new T.SphereGeometry(.5,10,8),cylinder:new T.CylinderGeometry(.5,.5,1,12),torus:new T.TorusGeometry(.42,.035,8,24).rotateX(Math.PI/2),courtRing:new T.TorusGeometry(.49,.007,6,48).rotateX(Math.PI/2),courtArcEast:new T.TorusGeometry(.5,.003,6,48,Math.PI).rotateX(Math.PI/2).rotateY(Math.PI/2),courtArcWest:new T.TorusGeometry(.5,.003,6,48,Math.PI).rotateX(Math.PI/2).rotateY(-Math.PI/2)};
   for(const item of f.fixtures||[]){
     if(item.kind==='mirror-surface'){
       const mirror=new SchoolMirror(item,f.id,()=>({actor,visual,angle,phase:game.phase,speed:game.speed,
         extraSignature:[game.eventSerial,...(pursuerVisual.root.visible?[pursuit.x,pursuit.y,pursuerVisual.root.rotation.y,game.time]:[0])]}));
       mirror.position.copy(pos(item.x,item.y,item.elevation+item.height/2));root.add(mirror);mirrors.push(mirror);continue;
     }
     const key=item.color+'|'+(item.shape||'box')+'|'+(item.kind==='escape-gate-lamp'?'yard-lamp':item.kind==='books'||item.kind.startsWith('book-')?'book':'other');if(!batches.has(key))batches.set(key,[]);batches.get(key).push(item);
   }
   for(const [key,items] of batches){
     const [color,shape,category]=key.split('|'),mesh=new T.InstancedMesh(geometries[shape]||unit,mat(color,category==='yard-lamp'?{emissive:color,emissiveIntensity:.85}:{}),items.length);mesh.name='Interior furnishings '+key;mesh.castShadow=category!=='book';mesh.receiveShadow=category!=='book';mesh.userData.fixtureRoomId=f.isRoom?f.id:'';mesh.userData.fixtureSpaceId=f.id;mesh.userData.isBooks=category==='book';
     items.forEach((o,i)=>{dummy.position.copy(pos(o.x,o.y,o.elevation+o.height/2));dummy.scale.set(o.width,o.height,o.depth);dummy.rotation.set(0,0,0);dummy.updateMatrix();mesh.setMatrixAt(i,dummy.matrix)});root.add(mesh);
   }
   if(f.isOutdoor){
     for(const lamp of (f.fixtures||[]).filter(o=>o.kind==='escape-gate-lamp')){const light=new T.PointLight('#ecd09a',9,8,2);light.position.copy(pos(lamp.x,lamp.y+.5,2.5));root.add(light)}
     for(const portal of f.portals||[]){const light=new T.PointLight('#c9dfd5',7,7,2);light.position.copy(pos(portal.doorX,portal.doorY-1,2.5));root.add(light)}
   }
   if(progression.classes[f.id]){for(const student of progression.classes[f.id].students)textSprite(String(student.no).padStart(2,'0'),root,pos(student.x,student.y,1.02),.4,.22,'#eadcb3');const l=items.searchById.get(progression.classes[f.id].lecternId);textSprite('출석부 · 자리배치도 · 칠판 기록',root,pos(l.x,l.y,1.75),3,.35);}
   for(const seat of f.teacherSeats||[])textSprite(seat.role,root,pos(seat.x,seat.y,1.65),1.65,.26,'#e2d5ad');
   if(f.kind==='gym')textSprite('HOME 00  |  00 GUEST',root,pos(23,36.5,3.9),5,.5,'#e0d2a6');
   const musicRooms=new Set(f.isRoom&&f.kind==='music'?[f.label]:[]);
   for(const z of f.interiorZones||[])if(musicRooms.has(z.room)&&['보컬 연습실','연결 통로','악기보관창고'].includes(z.label))
     textSprite(z.label,root,pos(z.x,z.y+.65,1.15),z.label==='연결 통로'?1.05:1.55,.26);
   const doors=[...(f.isRoom?(f.noExit?[]:[{label:f.exitLabel||'복도로 나가기',doorX:f.exitX,doorY:f.exitY,facing:'north',exit:true}]):f.rooms),...(f.portals||[])];
   for(const r of doors){
     const pieces=[];const doorBox=(name,p,scale,material)=>{const o=box(name,root,p,scale,material);pieces.push(o);return o};
     const [dx,dy]=directions[r.facing],x=r.doorX+dx*.51,y=r.doorY+dy*.51,horizontal=dy!==0;
     const lx=x,ly=y;
     doorBox('Closed door: '+r.label,pos(lx,ly,.775),[horizontal?1.02:.14,1.55,horizontal?.14:1.02],materials.wood);
     for(const side of [-1,1]){
       const height=side===-1?.42:1.7;
       box('Projecting door jamb',root,pos(x+dx*.06+(horizontal?side*.59/tile:0),y+dy*.06+(horizontal?0:side*.59/tile),height/2),[horizontal?.12:.36,height,horizontal?.36:.12],materials.frame);
     }

     box('Door threshold',root,pos(x+dx*.22,y+dy*.22,.026),[horizontal?1.4:.65,.035,horizontal?.65:1.4],materials.threshold);
     box('Door locator light',root,pos(x+dx*.15,y+dy*.15,1.66),[horizontal?.24:.06,.065,horizontal?.06:.24],materials.lamp);
     for(const side of [-1,1]){
       doorBox('Door glazing',pos(lx+dx*.07*side,ly+dy*.07*side,1.13),[horizontal?.53:.035,.36,horizontal?.035:.53],materials.glass);
       doorBox('Door handle',pos(lx+dx*.10*side,ly+dy*.10*side,.72),[.11,.055,.11],materials.gold);
     }
     textSprite(r.label,root,pos(x+dx*.14+(horizontal?-.6:0),y+dy*.14+(horizontal?0:.65),1.88),2.2,.45);
     doorVisuals.push({spaceId:f.id,doorX:r.doorX,doorY:r.doorY,pieces,bases:pieces.map(o=>o.position.clone()),horizontal});
   }
   for(const s of f.stairs){
     box('Stair pad',root,pos(s.x,s.y,.018),[2.3,.03,1.8],materials.lower);
     for(let i=0;i<7;i++)box('Tread',root,pos(s.x,s.y-.6+i*.2,.04),[1.82,.045,.08],materials.metal);
     box('Handrail',root,pos(s.x-.79,s.y,.52),[.06,.07,1.95],materials.gold);
     box('Handrail',root,pos(s.x+.79,s.y,.52),[.06,.07,1.95],materials.gold);
     textSprite(s.label,root,pos(s.x,s.y,1.85),2.5,.47,'#e1c592');
     const l=new T.PointLight('#ebca8c',7,6,2);l.position.copy(pos(s.x,s.y,2.7));root.add(l);
   }
   for(const b of f.barriers){
     const side=b.x<5||b.x>43;
     box('Sealed connection',root,pos(b.x,b.y,.65),[side?.22:3.6,1.3,side?3.6:.22],materials.metal);
     textSprite(b.label,root,pos(b.x,b.y,1.7),3,.43,'#d6ad86');
   }
 }
 for(const f of data.roomMaps.filter(f=>f.courtView)){
   const lower=floorRoots.find(r=>r.userData.spaceId===f.courtView).clone(true);lower.visible=true;lower.position.y=-4.5;
   lower.traverse(o=>{o.castShadow=false;if(o.userData.fixtureSpaceId)o.userData.fixtureSpaceId=f.id;if(o.userData.fixtureRoomId)o.userData.fixtureRoomId=f.id});
   floorRoots.find(r=>r.userData.spaceId===f.id).add(lower);
 }
 const kitchenVisual=createKitchenVisual(T,kitchen,pos);floorRoots.find(r=>r.userData.spaceId===kitchen.map.id).add(kitchenVisual.root);
 // Entirely new geometric placeholder, authored to the supplied protagonist
 // reference: black fringe, pale uniform jacket, white shirt, dark trousers.
 const actor=new T.Group(),visual=new T.Group();scene.add(actor);actor.add(visual);
 const cloth=mat('#b6b8ac'),dark=mat('#29313c'),skin=mat('#d9b291'),hair=mat('#111b25'),shirt=mat('#e1e2cf');
 box('School jacket',visual,new T.Vector3(0,1.02,0),[.47,.52,.28],cloth,true);
 box('White shirt',visual,new T.Vector3(0,1.07,.151),[.19,.38,.025],shirt);
 box('Tie',visual,new T.Vector3(0,1.05,.173),[.06,.29,.025],dark);
 const sphere=(name,p,s,m)=>{const o=new T.Mesh(new T.SphereGeometry(.5,12,10),m);o.name=name;o.position.set(...p);o.scale.set(...s);o.castShadow=true;visual.add(o);return o};
 sphere('Head',[0,1.52,0],[.43,.46,.4],skin);sphere('Black hair',[0,1.65,-.035],[.47,.34,.43],hair);
 for(let i=0;i<5;i++)box('Fringe',visual,new T.Vector3((i-2)*.077,1.59-(i%2)*.035,.17),[.09,.16,.09],hair);
 const limbs=[];
 for(const side of [-1,1]){
   box('Eye',visual,new T.Vector3(side*.10,1.51,.195),[.055,.055,.025],hair);
   const arm=new T.Group();arm.position.set(side*.30,1.23,0);visual.add(arm);
   box('Sleeve',arm,new T.Vector3(0,-.20,0),[.15,.40,.17],cloth,true);box('Hand',arm,new T.Vector3(0,-.46,0),[.14,.15,.15],skin,true);
   const leg=new T.Group();leg.position.set(side*.13,.76,0);visual.add(leg);
   box('Thigh',leg,new T.Vector3(0,-.155,0),[.18,.31,.21],dark,true);
   const knee=new T.Group();knee.position.y=-.31;leg.add(knee);
   box('Shin',knee,new T.Vector3(0,-.145,0),[.17,.29,.20],dark,true);box('Shoe',knee,new T.Vector3(0,-.36,.055),[.21,.14,.32],hair,true);
   limbs.push({arm,leg,knee,side});
 }
 const playerLight=new T.PointLight('#ffe1aa',14,10,1.8);playerLight.position.set(0,3,0);actor.add(playerLight);
 const ring=new T.Mesh(new T.RingGeometry(.31,.34,40),new T.MeshBasicMaterial({color:'#ddd3b0',transparent:true,opacity:.55,side:T.DoubleSide}));ring.rotation.x=-Math.PI/2;ring.position.y=.025;actor.add(ring);
 const pursuerVisual=createPursuerVisual(T);scene.add(pursuerVisual.root);
 const subjectVisuals=createSubjectVisuals(T,subjects,pos);scene.add(subjectVisuals.root);
 const blackout=document.createElement('div');blackout.id='science-blackout';blackout.style.cssText='position:fixed;inset:0;background:#000;z-index:9;pointer-events:none;display:none';document.body.append(blackout);
 const lootVisuals=createLootVisuals(T,items,pos);scene.add(lootVisuals.root);
 const gateRoot=new T.Group();gateRoot.name='운동장 교문 · 최종 탈출';floorRoots.find(r=>r.userData.spaceId===items.escapeGate.spaceId).add(gateRoot);
 textSprite('탈출 교문 · 열쇠 A / B / C',gateRoot,pos(items.escapeGate.x,items.escapeGate.y-2,2.15),4,.48,'#e4c572');
 let started=false,paused=false,manual=false,keys=new Set(),last=performance.now(),seenFloor='',angle=0,toastTimer,chaseIntensity=0;
 let gameplayEventCursor=game.eventSerial,inventoryUI,discovery;
 const bagUI=createInventoryUI({items,game,onClose:()=>{keys.clear();last=performance.now();updateAudio(0)},onAction:result=>{if(result?.message)toast(result.message);pumpItemNoise();updateAudio(0)}});
 discovery=createDiscoveryUI({items,survival,progression,onClose:()=>{keys.clear();last=performance.now();updateAudio(0)},onAction:()=>{pumpItemNoise();bagUI.update();updateAudio(0)}});
 progression.onLock=lock=>{discovery.lock(lock);keys.clear();updateAudio(0)};progression.onMessage=toast;
 inventoryUI={...bagUI,close(){discovery.close();bagUI.close()},get isOpen(){return bagUI.isOpen||discovery.isOpen}};
 const guideButton=document.createElement('button');guideButton.id='guidebook-open';guideButton.textContent='가이드북 · 분실하지 않는 전용 칸';guideButton.onclick=()=>discovery.guide();bagUI.root.querySelector('.inv-grid').parentElement.append(guideButton);
 $('difficulty').value=survival.mode;$('difficulty').onchange=e=>{survival.setMode(e.target.value);sessionStorage.setItem('school.mode',survival.mode);$('difficulty-description').textContent=modeText()};
 function modeText(){if(document.documentElement.lang==='en')return {easy:'5 lives · 200% stamina · Detailed clues · Keep inventory',normal:'5 lives · 150% stamina · Indirect clues · Drop items at capture',hard:'3 lives · 100% stamina · No location hints · Scatter items nearby',extreme:'1 life · 100% stamina · Capture ends the run'}[survival.mode];return {easy:'목숨 5 · 스태미나 200% · 상세 위치 힌트 · 소지품 보존',normal:'목숨 5 · 스태미나 150% · 간접 단서 · 사망 지점에 소지품 드롭',hard:'목숨 3 · 스태미나 100% · 위치 힌트 없음 · 사망 지점 8m 내 무작위 드롭',extreme:'목숨 1 · 스태미나 100% · 위치 힌트 없음 · 잡히면 종료'}[survival.mode]}
 $('difficulty-description').textContent=modeText();
 let muted=false,volume=.4;
 try{muted=localStorage.getItem('schoolEscape.muted')==='true';const saved=Number(localStorage.getItem('schoolEscape.volume'));if(localStorage.getItem('schoolEscape.volume')!==null&&Number.isFinite(saved))volume=Math.max(0,Math.min(1,saved))}catch{}
 audio.setMuted(muted);audio.setVolume(volume);$('volume').value=Math.round(volume*100);
 try{if(localStorage.getItem('schoolEscape.enemyEnabled')==='false')pursuit.setEnabled(false)}catch{}
 function soundUI(){
   $('sound-toggle').textContent=muted?'소리 꺼짐':'소리 켜짐';$('sound-toggle').setAttribute('aria-pressed',String(muted));$('sound-toggle').setAttribute('aria-label',muted?'소리 켜기':'소리 끄기');
 }
 soundUI();
 function updateAudio(dt=0){
   const cooking=kitchen.active&&game.spaceId===kitchen.map.id,enemy=subjects.nearest||(cooking?kitchen:pursuit);const sameSpace=pursuit.enabled&&(subjects.active||cooking||(!subjects.inEncounter&&pursuit.spaceId===game.spaceId));
   const enemyDistance=sameSpace?Math.hypot(enemy.x-game.x,enemy.y-game.y)*tile:Infinity;
   const alerted=pursuit.enabled&&(subjects.active||cooking||(!subjects.inEncounter&&pursuit.alerted));
   const goal=alerted?(sameSpace?Math.max(.45,1-enemyDistance/22):.4):0;
   chaseIntensity+=(goal-chaseIntensity)*(1-Math.exp(-3*Math.max(dt,.001)));
   const stair=game.nearby(),onStairs=!!stair&&Math.abs(stair.x-game.x)*tile<1.2&&Math.abs(stair.y-game.y)*tile<1.05;
   audio.update(dt,{started,paused:paused||!!inventoryUI?.isOpen,speed:game.speed,isRunning:game.isRunning,stamina:game.stamina,exhausted:game.exhausted,
     enemyAlerted:alerted,enemyPresent:sameSpace,spaceId:game.spaceId,roomKind:game.map.kind,isOutdoor:!!game.map.isOutdoor,onStairs,chaseIntensity,enemyDistance,enemyMoving:sameSpace&&enemy.speed>.1,
     musicEncounter:game.map.subjectEvent==='music',musicPhase:subjects.music.phase,musicTempo:subjects.musicTempo,watchEquipped:items.equipped?.itemId==='smartwatch',caught:game.caught,won:game.won,events:game.events});
 }
 function pumpItemNoise(){for(const event of [...game.events])if(event.id>gameplayEventCursor){gameplayEventCursor=event.id;if(event.type==='search'||event.type==='item-dropped')game.emit('noise',{spaceId:event.spaceId||game.spaceId,x:event.x??game.x,y:event.y??game.y,radius:event.type==='search'?4:2.5,loudness:1,kind:event.type})}}
 function tick(dt,ax=0,ay=0,run=false){game.step(ax,ay,run,dt);items.step();pumpItemNoise();kitchen.step(dt);subjects.step(dt);if(!kitchen.active&&!subjects.inEncounter)pursuit.step(dt);updateAudio(dt)}
 function toggleInventory(){if(inventoryUI.isOpen)inventoryUI.close();else if(started&&!paused&&!game.caught&&!game.won&&!game.pendingDoor)inventoryUI.open();keys.clear();last=performance.now();updateAudio(0)}
 function interact(forceItem=false){
   if(!started||paused||inventoryUI.isOpen||game.caught||game.won)return false;
   let result;
   if(!forceItem&&items.nearbyGate())result=items.tryEscape();
   else if(!forceItem&&game.nearbyDoor()){const door=game.nearbyDoor();if(door.locked){toast('빗장과 자물쇠로 단단히 잠겨 있습니다.');return false;}const message=subjects.intercept(door)||kitchen.intercept(door);if(message){toast(message);return false;}return game.interact();}
   else{const near=items.nearby();if(near?.sourceId&&!progression.checkContainer(near.sourceId))return false;result=items.interact();if(result?.type==='open-container'){discovery.open(result.target);keys.clear();updateAudio(0);return true}}
   if(result?.message)toast(result.message);pumpItemNoise();updateAudio(0);if(game.won){keys.clear();$('success').hidden=false;$('door-prompt').hidden=true;$('stair-prompt').hidden=true;$('interaction-prompt').hidden=true}return !!result?.ok;
 }
 function setPaused(value){paused=!!value;keys.clear();$('pause').hidden=!paused||!started||game.caught;last=performance.now();updateAudio(0)}
 async function unlockAudio(){try{if(!await audio.unlock())toast('소리를 시작하지 못했습니다. 소리 버튼으로 다시 시도할 수 있습니다.')}catch(error){console.warn('Audio unavailable:',error.message);toast('소리를 시작하지 못했습니다. 소리 버튼으로 다시 시도할 수 있습니다.')}}
 async function resume(){await unlockAudio();setPaused(false)}
 function restart(seed){sessionStorage.setItem('school.mode',survival.mode);location.reload()}
 function toast(message){$('toast').textContent=message;$('toast').classList.add('visible');clearTimeout(toastTimer);toastTimer=setTimeout(()=>$('toast').classList.remove('visible'),2500)}
 function floorChanged(){
   const center=pos((game.map.width-1)/2,(game.map.height-1)/2),extent=Math.hypot(game.map.width,game.map.height)*tile/2+6;
   moon.target.position.copy(center);moon.position.copy(center).add(new T.Vector3(-12,30,10));
   Object.assign(moon.shadow.camera,{left:-extent,right:extent,top:extent,bottom:-extent,far:220});moon.shadow.camera.updateProjectionMatrix();
   floorRoots.forEach(r=>r.visible=r.userData.spaceId===game.spaceId);
   const m=game.map,label=m.isOutdoor?m.label:game.roomId?m.label:`본관 ${game.floor}층 · ${m.subtitle}`;
   scene.background.set(m.isOutdoor?'#14222a':'#0c1721');scene.fog.color.copy(scene.background);scene.fog.density=m.isOutdoor?.0045:.009;
   document.querySelector('.legend').innerHTML=m.isOutdoor?(m.id==='playground'?'<i></i>현재 위치 <b></b>탈출 교문 <em style="background:#91c8c3"></em>건물 출입구':'<i></i>현재 위치 <em></em>운동장 연결문'):'<i></i>현재 위치 <b></b>계단 <em></em>방 출입구';
   $('area').textContent=label;$('floor-label').textContent=m.isOutdoor?(m.id==='playground'?'SCHOOL YARD':'WALKWAY'):game.roomId?m.label:`MAIN BUILDING — 0${game.floor}`;
   $('route-note').textContent=m.isOutdoor?(m.id==='playground'?'북쪽: 본관 / 동쪽: 산책로 / 남쪽: 탈출 교문':'순환 산책로 · 입구에서 E로 운동장에 돌아갑니다.'):game.map.noExit?'계단 방향 표지를 따라 이동합니다.':game.roomId?'출입문 앞에서 E를 눌러 복도로 돌아갑니다.':game.floor===1?'현관문에서 E로 운동장에 나갈 수 있습니다.':'닫힌 문 앞에서 E를 눌러 방에 들어갑니다.';
   seenFloor=game.spaceId;camera.position.copy(pos(game.x,game.y,16)).add(new T.Vector3(0,0,10.4));camera.lookAt(pos(game.x,game.y));if(started)toast(label);
 }
 function drawMap(){
   const c=$('minimap');c.dataset.spaceId=game.spaceId;const ctx=c.getContext('2d'),s=Math.min(c.width/game.map.width,c.height/game.map.height),ox=(c.width-game.map.width*s)/2,top=game.map.height-1;
   const px=x=>ox+x*s,py=y=>(top-y)*s;
   ctx.clearRect(0,0,c.width,c.height);ctx.fillStyle='#111e26';ctx.fillRect(0,0,c.width,c.height);
   ctx.fillStyle='#576a65';for(let y=0;y<game.map.height;y++)for(let x=0;x<game.map.width;x++)if(game.open(x,y))ctx.fillRect(Math.floor(px(x)),Math.floor(py(y)),Math.ceil(px(x+1))-Math.floor(px(x)),Math.ceil(py(y-1))-Math.floor(py(y)));
   for(const r of game.map.rooms){ctx.strokeStyle='#475650';ctx.lineWidth=.7;ctx.strokeRect(px(r.x0),py(r.y1),(r.x1-r.x0+1)*s,(r.y1-r.y0+1)*s);ctx.fillStyle='#b69b74';ctx.fillRect(px(r.doorX),py(r.doorY),s,s);
     if($('map-panel').classList.contains('expanded')){ctx.font='10px School,sans-serif';ctx.textAlign='center';ctx.fillStyle='#d1d5c4';ctx.fillText(r.label,px((r.x0+r.x1+1)/2),py((r.y0+r.y1)/2)+s/2,(r.x1-r.x0+1)*s-1)}
   }
   if(game.map.isOutdoor||game.map.kitchenSequence||$('map-panel').classList.contains('expanded')){ctx.fillStyle=game.map.isOutdoor?'#30463d':'#b2a07b';for(const o of game.solidFixtures[game.spaceId])ctx.fillRect(px(o.x-o.width/tile/2+.5),py(o.y+o.depth/tile/2-.5),o.width/tile*s,o.depth/tile*s)}
   if(game.roomId&&!game.map.noExit){ctx.fillStyle='#b69b74';ctx.fillRect(px(game.map.exitX),py(game.map.exitY),s,s)}
   ctx.fillStyle='#91c8c3';for(const p of game.map.portals||[]){ctx.fillRect(px(p.doorX)-1,py(p.doorY)-1,s+2,s+2);if($('map-panel').classList.contains('expanded')){ctx.font='12px School,sans-serif';ctx.textAlign='center';ctx.fillText(p.label,px(p.doorX),py(p.doorY)+(p.facing==='south'?21:-8))}}
   if(game.map.isOutdoor){ctx.font='10px School,sans-serif';ctx.textAlign='left';ctx.fillStyle='#c2d7cb';ctx.fillText('북 ↑',8,15)}
   if(game.spaceId===items.escapeGate.spaceId){const e=items.escapeGate;ctx.fillStyle='#e4c572';ctx.fillRect(px(e.x)-2,py(e.y)-2,s+4,s+4);if($('map-panel').classList.contains('expanded')){ctx.font='12px School,sans-serif';ctx.textAlign='center';ctx.fillText('탈출 교문',px(e.x),py(e.y)+20)}}
   ctx.fillStyle='#d8b56c';for(const st of game.map.stairs)ctx.fillRect(px(st.x)-1,py(st.y)-1,s+2,s+2);
   ctx.beginPath();ctx.fillStyle='#f0f6e9';ctx.arc(px(game.x)+s/2,py(game.y)+s/2,3,0,Math.PI*2);ctx.fill();
 }
 function render(dt=0){
   if(game.spaceId!==seenFloor)floorChanged();
   actor.position.copy(pos(game.x,game.y));
   kitchenVisual.update();subjectVisuals.update();
   const dark=started&&subjects.dark&&!game.caught;blackout.style.display=dark?'block':'none';$('map-panel').style.visibility=dark?'hidden':'';
   if(subjects.hint)$('route-note').textContent=subjects.hint;
   if(game.map.kitchenSequence)$('route-note').textContent=kitchen.active?'잔해 사이 비상 유도선을 따라 서쪽 비상문으로 이동하세요.':['idle','armed'].includes(kitchen.phase)?(kitchen.examined?'상자 조사를 마쳤습니다. 북쪽 출입문으로 돌아가세요.':'조리실 길 끝의 보급 상자를 조사하세요.'):'무너진 물건과 배식장이 부순 길이 그대로 남아 있습니다.';
   pursuerVisual.root.visible=!subjects.inEncounter&&!kitchen.active&&started&&pursuit.enabled&&pursuit.spaceId===game.spaceId;
   const frozen=paused||inventoryUI.isOpen||game.caught||game.won;
   pursuerVisual.root.position.copy(pos(pursuit.x,pursuit.y));pursuerVisual.update(pursuit,started&&!frozen?dt:0);
   const nearbyItem=items.nearby();lootVisuals.update(game.spaceId,nearbyItem);lootVisuals.root.visible=started;
   const goal=Math.atan2(game.direction.x,-game.direction.y);angle+=Math.atan2(Math.sin(goal-angle),Math.cos(goal-angle))*(1-Math.exp(-16*dt));visual.rotation.y=angle;
   const running=game.isRunning,moving=frozen?0:Math.min(1,game.speed/data.walkSpeed),swing=Math.sin(game.phase)*(running?.73:.49)*moving;
   for(const l of limbs){l.leg.rotation.x=l.side*swing;l.knee.rotation.x=Math.max(0,l.side*swing)*(running?1.2:.8);l.arm.rotation.x=-l.side*swing*.8;l.arm.rotation.z=l.side*(running?.21:.07)}
   visual.position.y=Math.abs(Math.sin(game.phase))*.045*moving;
   const target=pos(game.x,game.y,16).add(new T.Vector3(0,0,10.4));camera.position.lerp(target,1-Math.exp(-10*Math.max(dt,.001)));
   camera.quaternion.setFromEuler(new T.Euler(-Math.atan2(16,10.4),0,0));
   const near=game.nearby();$('stair-prompt').hidden=!near||!started||game.caught;if(near)$('stair-prompt').querySelector('span').textContent=near.label+' 계단';
   if(near)$('stair-prompt').querySelector('small').textContent=progression.describe(near.targetId||`floor-${near.target}`)|| (game.arrivalLock===near.id?'계단 구역을 벗어난 뒤 다시 진입하세요':'계단 안으로 이동하면 자동 전환');
   const room=game.currentRoom();$('room-status').textContent=game.map.isOutdoor?(game.map.id==='playground'?'운동장 탐방 중 · 남쪽 교문 탈출':'산책로 탐방 중 · 연결문 E로 운동장 복귀'):room?room.label+(room.theme?' · '+room.theme:'')+' 탐방 중':'방문 기본 닫힘 · E 입장';
   const door=game.nearbyDoor();$('door-prompt').hidden=!door||!started||game.caught;if(door)$('door-prompt').querySelector('span').textContent=door.label+' · '+(kitchen.active&&door.id===kitchen.map.kitchenSequence.triggerPortal?'북쪽 문 봉쇄 · 서쪽 비상문으로':progression.describe(door.targetId)||(door.exit?'문 열고 복도로 나가기':'문 열고 입장'));
   const transit=pursuit.enabled?pursuit.transit:null,portal=transit?.portal;
   let enemyDoor=null,enemyDoorAmount=0;
   if(portal?.kind==='door'&&(portal.fromId===game.spaceId||portal.toId===game.spaceId)){
     const source=portal.fromId===game.spaceId,px=source?portal.sourceX:portal.arrivalX,py=source?portal.sourceY:portal.arrivalY;
     enemyDoor=doorVisuals.filter(d=>d.spaceId===game.spaceId).sort((a,b)=>Math.hypot(a.doorX-px,a.doorY-py)-Math.hypot(b.doorX-px,b.doorY-py))[0];
     enemyDoorAmount=Math.max(0,Math.min(1,(source?game.time-(transit.until-1.25):.35-(transit.until-game.time))/.22));
   }
   for(const d of doorVisuals)if(d.spaceId===game.spaceId){const pending=game.pendingDoor,playerAmount=pending&&pending.doorX===d.doorX&&pending.doorY===d.doorY?Math.min(1,(game.time-pending.started)/.22):0,amount=Math.max(playerAmount,enemyDoor===d?enemyDoorAmount:0);d.pieces.forEach((o,i)=>{o.position.copy(d.bases[i]);if(d.horizontal)o.position.x+=amount*1.15;else o.position.z-=amount*1.15})}
   $('area').textContent=game.map.isOutdoor?`${game.map.label}${game.map.id==='playground'?' · 남쪽 교문으로 탈출':''}`:room?`${room.building==='annex'?'별관':room.building==='west'?'서관':'본관'} ${game.floor}층 · ${room.label}${room.theme?' · '+room.theme:''}`:`본관 ${game.floor}층 · ${game.map.subtitle}`;
   survival.capture();subjects.recoverEquipment();$('lives').textContent=survival.rules.label+' · 남은 목숨 '+survival.lives+' / '+survival.rules.lives;const stamina=Math.max(0,Math.min(game.maxStamina,game.stamina));$('stamina-value').textContent=game.unlimitedStamina?.()?'∞ · 조리실 추격':Math.ceil(stamina)+' / '+game.maxStamina;$('stamina-fill').style.transform=`scaleX(${stamina/game.maxStamina})`;
   $('stamina-meter').setAttribute('aria-valuemax',String(game.maxStamina));$('stamina-meter').setAttribute('aria-valuenow',String(Math.round(stamina)));$('vitals').classList.toggle('exhausted',game.exhausted);
   $('stamina-hint').textContent=game.unlimitedStamina?.()?'조리실 추격 중 스태미나 무제한':game.exhausted?'지침 · 25% 회복 후 다시 달릴 수 있습니다':game.isRunning?'달리는 중 · 스태미나 소모':'달리면 소모 · 걸으면 회복';
   const alerted=pursuit.enabled&&(subjects.active||kitchen.active||(!subjects.inEncounter&&pursuit.alerted));$('threat-status').hidden=!started||!alerted||frozen;$('toast').hidden=false;$('toast').style.top=alerted?'82px':'30px';
   $('threat-status').textContent=subjects.active?subjects.hint:kitchen.active?'배식장이 선반을 부수며 쫓아옵니다 · 서쪽 비상문으로':pursuit.state==='transit'?'문 너머로 발소리가 다가옵니다':pursuit.state==='search'?'발소리가 아직 멀어지지 않았습니다':'당직자가 쫓아오고 있습니다';
   $('danger-vignette').style.opacity=started&&!frozen?String(chaseIntensity*.65):'0';
   $('caught').hidden=!game.caught;$('revive').hidden=survival.lives<=0;$('caught-detail').textContent=survival.lives>0?'남은 목숨 '+survival.lives+'개. '+(survival.mode==='easy'?'소지품은 그대로 유지됩니다.':'가이드북을 제외한 소지품을 붙잡힌 공간에 떨어뜨렸습니다.'):'남은 목숨이 없습니다. 새 판을 시작하세요.';$('pause').hidden=!paused||!started||game.caught;
   $('success').hidden=!game.won;
   if(game.won){$('door-prompt').hidden=true;$('stair-prompt').hidden=true}
   $('enemy-toggle').textContent=pursuit.enabled?'추적자 ON':'추적자 OFF';$('enemy-toggle').setAttribute('aria-pressed',String(pursuit.enabled));
   $('inventory-counter').textContent=`${items.inventory.length} / 9`;
   $('key-progress').textContent=['a','b','c'].map(k=>`${items.inventory.some(i=>i.itemId==='key-'+k)?'●':'○'} ${k.toUpperCase()}`).join('   ');
   const gate=items.nearbyGate(),doorNear=game.nearbyDoor();$('interaction-prompt').hidden=!started||frozen||(!nearbyItem&&!gate);
   if(gate){$('interaction-prompt').querySelector('kbd').textContent='E';$('interaction-label').textContent=gate.missing.length?'교문 잠김 · 열쇠 '+gate.missing.join('·')+' 필요':'열쇠 A·B·C 사용 · 탈출'}
   else if(nearbyItem){$('interaction-prompt').querySelector('kbd').textContent=doorNear?'F':'E / F';$('interaction-label').textContent=nearbyItem.label+' · '+nearbyItem.prompt}
   const compass=items.compass();$('compass-hud').hidden=!compass.active||!started||game.won;
   if(compass.active){$('compass-arrow').style.transform=`rotate(${compass.angle}rad)`;$('compass-target').textContent=compass.targetLabel;$('compass-route').textContent=compass.via==='key'?'열쇠가 있는 방향':compass.label+' 방향'}
   $('watch-hud').hidden=!started||items.equipped?.itemId!=='smartwatch'||game.won;
   $('watch-rate').textContent=audio.watchBpm>0?`${Math.round(audio.watchBpm)} BPM`:'주변 반응 없음';
   inventoryUI.update();
   $('gait').textContent=game.caught?'CAUGHT':paused?'PAUSED':game.speed<.1?'IDLE':running?'RUN':'WALK';drawMap();renderer.render(scene,camera);
 }
 function resize(){const w=innerWidth,h=innerHeight;renderer.setSize(w,h);camera.left=-8.5*w/h;camera.right=8.5*w/h;camera.top=8.5;camera.bottom=-8.5;camera.updateProjectionMatrix()}
 addEventListener('resize',resize);resize();floorChanged();render();
 $('begin').onclick=()=>{started=true;document.body.classList.remove('at-lobby');$('start').hidden=true;keys.clear();last=performance.now();unlockAudio();toast('Shift로 달리기 · E로 문 열기 · Esc로 일시정지')};
 $('revive').onclick=()=>{if(survival.revive()){kitchen.step(0);keys.clear();paused=false;seenFloor='';floorChanged();render(0);unlockAudio()}};
 $('restart').onclick=()=>{unlockAudio();restart()};$('resume').onclick=resume;
 $('success-restart').onclick=()=>{unlockAudio();restart()};$('new-run').onclick=()=>{unlockAudio();restart()};
 $('inventory-toggle').onclick=toggleInventory;
 const saveButton=document.createElement('button');saveButton.id='save-run';saveButton.textContent='현재 판 저장';$('new-run').after(saveButton);saveButton.onclick=()=>{try{saveRun({game,items,survival,kitchen,progression,pursuit,subjects});toast('현재 판을 저장했습니다. 시작 화면의 저장한 판 계속하기로 이어갈 수 있습니다.')}catch(e){toast(e.message)}};
 const loadButton=document.createElement('button');loadButton.id='load-run';loadButton.textContent='저장한 판 계속하기';$('begin').after(loadButton);loadButton.disabled=!localStorage.getItem('school.save016');loadButton.onclick=()=>{sessionStorage.setItem('school.load016','1');location.reload()};
 $('enemy-toggle').onclick=()=>{const enabled=!pursuit.enabled;pursuit.setEnabled(enabled);if(enabled)pursuit.reset();$('enemy-toggle').textContent=enabled?'추적자 ON':'추적자 OFF';$('enemy-toggle').setAttribute('aria-pressed',String(enabled));if(!game.caught)$('caught').hidden=true;keys.clear();chaseIntensity=0;updateAudio(0);toast(enabled?'추적자 ON · 지정 지점에서 배회합니다':'추적자 OFF · 개발 중 추격을 멈춥니다');try{localStorage.setItem('schoolEscape.enemyEnabled',String(enabled))}catch{}};
 $('sound-toggle').onclick=()=>{muted=!muted;audio.setMuted(muted);soundUI();if(!muted)unlockAudio();try{localStorage.setItem('schoolEscape.muted',String(muted))}catch{}};
 $('volume').oninput=event=>{volume=Number(event.target.value)/100;audio.setVolume(volume);try{localStorage.setItem('schoolEscape.volume',String(volume))}catch{}};
 const toggle=()=>$('map-panel').classList.toggle('expanded');$('map-toggle').onclick=toggle;
 addEventListener('keydown',e=>{
   const uiControl=e.target.matches?.('input,button');
   if(!uiControl&&['Tab','ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Space'].includes(e.code))e.preventDefault();
   if(!started||e.target.tagName==='INPUT')return;
   if(e.code==='KeyI'&&!e.repeat){e.preventDefault();toggleInventory();return}
   if(e.code==='Escape'&&!e.repeat&&inventoryUI.isOpen){e.preventDefault();inventoryUI.close();return}
   if(inventoryUI.isOpen){if(e.code==='KeyE'&&!e.repeat){e.preventDefault();inventoryUI.close()}return;}
   if(e.code==='Escape'&&!e.repeat&&!game.caught&&!game.won){e.preventDefault();if(paused)resume();else setPaused(true);return}
   if(game.won)return;
   if(game.caught){if(e.code==='KeyR'&&!e.repeat){unlockAudio();restart()}return}
   if(paused)return;keys.add(e.code);
   if(e.code==='Tab'&&!e.repeat&&!uiControl)toggle();
   if(e.code==='KeyE'&&!e.repeat)interact();
   if(e.code==='KeyF'&&!e.repeat)interact(true);
 });addEventListener('keyup',e=>keys.delete(e.code));addEventListener('blur',()=>{keys.clear();if(started&&!game.caught)setPaused(true)});document.addEventListener('visibilitychange',()=>{keys.clear();if(document.hidden&&started&&!game.caught)setPaused(true)});
 addEventListener('pagehide',event=>{if(event.persisted)setPaused(true);else audio.dispose()});
 function frame(now){const dt=Math.max(0,Math.min((now-last)/1000,.05));last=now;if(started&&!paused&&!inventoryUI.isOpen&&!manual&&!game.caught&&!game.won){const ax=+(keys.has('KeyD')||keys.has('ArrowRight'))-+(keys.has('KeyA')||keys.has('ArrowLeft')),ay=+(keys.has('KeyW')||keys.has('ArrowUp'))-+(keys.has('KeyS')||keys.has('ArrowDown'));tick(dt,ax,ay,keys.has('ShiftLeft')||keys.has('ShiftRight'))}else if(!manual)updateAudio(dt);if(!manual)render(dt);requestAnimationFrame(frame)}requestAnimationFrame(frame);
 // Exposed only in explicit QA mode; no debug teleports in the public UI.
 function inspectDoor(label){
   scene.updateMatrixWorld(true);camera.updateMatrixWorld(true);
   const root=floorRoots.find(r=>r.userData.spaceId===game.spaceId),r=game.map.rooms.find(r=>r.label===label),door=root.getObjectByName('Closed door: '+label);
   const target=door.getWorldPosition(new T.Vector3()),projected=target.clone().project(camera);
   const ray=new T.Raycaster(camera.position,target.clone().sub(camera.position).normalize());ray.camera=camera;
   const first=ray.intersectObject(root,true)[0];
   const [dx,dy]=directions[r.facing],entry=pos(r.doorX+dx*.51,r.doorY+dy*.51,.4),eray=new T.Raycaster(camera.position,entry.clone().sub(camera.position).normalize());eray.camera=camera;
   const hit=eray.intersectObject(root,true)[0],ev=entry.clone().project(camera);
   const entranceVisible=r.interiorOpen&&Math.abs(ev.x)<1&&Math.abs(ev.y)<1&&(!hit||hit.distance>=camera.position.distanceTo(entry)-.2);
   return {firstHit:first?.object.name,entranceVisible,inViewport:Math.abs(projected.x)<1&&Math.abs(projected.y)<1&&Math.abs(projected.z)<1};
 }
 function inspectSpace(){const active=floorRoots.filter(r=>r.visible),rooms=new Set(),spaces=new Set(),books=[];for(const root of active)root.traverse(o=>{if(o.userData.fixtureRoomId)rooms.add(o.userData.fixtureRoomId);if(o.userData.fixtureSpaceId)spaces.add(o.userData.fixtureSpaceId);if(o.userData.isBooks)books.push(o)});return {activeIds:active.map(r=>r.userData.spaceId),visibleFixtureRoomIds:[...rooms],visibleFixtureSpaceIds:[...spaces],pendingDoor:!!game.pendingDoor,minimapSpaceId:$('minimap').dataset.spaceId,bookShadowsDisabled:books.every(o=>!o.castShadow&&!o.receiveShadow),bookBatches:books.length}}
 function inspectMirrors(){const active=mirrors.filter(m=>m.spaceId===game.spaceId);return {count:active.length,activeSpaceId:game.spaceId,mirrors:active.map(m=>m.inspect(actor))}}
 function checkMirrorPixels(){
   const mirror=mirrors.find(m=>m.spaceId===game.spaceId);if(!mirror)return {changedPixels:0};
   const a=new Uint8Array(mirror.target.width*mirror.target.height*4),b=new Uint8Array(a.length);
   visual.visible=true;mirror.invalidate();renderer.render(scene,camera);
   renderer.readRenderTargetPixels(mirror.target,0,0,mirror.target.width,mirror.target.height,a);
   visual.visible=false;mirror.invalidate();renderer.render(scene,camera);
   renderer.readRenderTargetPixels(mirror.target,0,0,mirror.target.width,mirror.target.height,b);
   visual.visible=true;mirror.invalidate();renderer.render(scene,camera);
   let changedPixels=0,maximumDelta=0;
   for(let n=0;n<a.length;n+=4){const delta=Math.max(Math.abs(a[n]-b[n]),Math.abs(a[n+1]-b[n+1]),Math.abs(a[n+2]-b[n+2]));if(delta>2)changedPixels++;maximumDelta=Math.max(maximumDelta,delta)}
   return {changedPixels,maximumDelta,roomId:game.spaceId};
 }
 if(new URLSearchParams(location.search).has('qa'))window.escapeQA={game,pursuit,audio,survival,discovery,pursuerVisual,items,inventoryUI,lootVisuals,interact,updateAudio,tick,setPaused,reset:restart,setManual:value=>{manual=!!value;keys.clear()},
   subjects,subjectVisuals,kitchen,kitchenVisual,progression,save:()=>saveRun({game,items,survival,kitchen,progression,pursuit,subjects}),render,scene,actor,camera,renderer,inspectDoor,inspectSpace,inspectMirrors,checkMirrorPixels,floorRoots,start:()=>{started=true;$('start').hidden=true},keys};
 createLobby({getSound:()=>({muted,volume}),setSound:(next)=>{muted=next.muted;volume=next.volume;audio.setMuted(muted);audio.setVolume(volume);$('volume').value=Math.round(volume*100);soundUI();try{localStorage.setItem('schoolEscape.muted',String(muted));localStorage.setItem('schoolEscape.volume',String(volume))}catch{}},returnToLobby:()=>{inventoryUI.close();started=false;paused=false;keys.clear();$('start').hidden=false;$('pause').hidden=true;updateAudio(0)},onLanguage:()=>{$('difficulty-description').textContent=modeText()}});
 window.escapeLoaded=true;
}catch(error){console.error(error);$('fatal').hidden=false;$('fatal').textContent='실행을 시작하지 못했습니다.\n'+error.message;}
