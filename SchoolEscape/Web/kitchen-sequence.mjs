// Original authored set-piece. Broken geometry belongs to the current run only.
export class KitchenSequence {
 constructor(game,pursuit,survival){this.game=game;this.pursuit=pursuit;this.survival=survival;this.map=[...game.spaces.values()].find(f=>f.kitchenSequence);this.phase='idle';this.blocks=[];this.revision=0;this.destroyed=0;this.path=this.map.kitchenSequence.path;this.x=this.path[0][0];this.y=this.path[0][1]+1.5;this.index=-1;this.distance=0;this.startedAt=0;this.speed=0;this.direction={x:0,y:-1};this.walkPhase=0;this.message='';this.staticSolids=[...game.solidFixtures[this.map.id]];}
 get active(){return this.phase==='active'}
 intercept(door){
  if(this.game.spaceId!==this.map.id)return null;
  if(this.phase==='idle'&&this.pursuit.enabled){
   if(door.id===this.map.kitchenSequence.escapePortal)return '비상문 잠금장치가 움직이지 않는다. 들어왔던 북쪽 문을 확인하자.';
   if(door.id===this.map.kitchenSequence.triggerPortal){this.begin();return '선반이 무너졌다! 비상 유도선을 따라 서쪽 비상문으로!';}
  }
  if(this.active&&door.id===this.map.kitchenSequence.triggerPortal)return '북쪽 문이 뒤틀려 열리지 않는다. 서쪽 비상문으로!';
  return null;
 }
 begin(){
  if(this.phase!=='idle')return false;const g=this.game,keep=new Set(this.path.map(p=>p.join(',')));
  for(let y=2;y<this.map.height-1;y++)for(let x=1;x<this.map.width-1;x++){
   if(keep.has(`${x},${y}`)||g.blocked(x,y,this.map))continue;
   this.blocks.push({id:`collapse-${x}-${y}`,x,y,width:g.data.tileSize,depth:g.data.tileSize,height:1.1+((x*7+y*13)%5)*.16,active:true});
  }
  this.phase='active';this.startedAt=g.time;this.sync();g.emit('kitchen-collapse',{spaceId:this.map.id});return true;
 }
 sync(){this.game.solidFixtures[this.map.id]=[...this.staticSolids,...this.blocks.filter(b=>b.active)];this.revision++;}
 clearAround(){let count=0;for(const b of this.blocks)if(b.active&&Math.hypot(b.x-this.x,b.y-this.y)<1.65){b.active=false;count++;}if(count){this.destroyed+=count;this.sync();this.game.emit('kitchen-smash',{spaceId:this.map.id,count,x:this.x,y:this.y});}}
 step(dt){
  if(!this.active)return;const g=this.game;
  if(!this.pursuit.enabled){this.phase='bypassed';this.speed=0;return;}
  if(g.caught){this.phase='failed';this.speed=0;return;}
  if(g.spaceId!==this.map.id){this.phase='cleared';this.speed=0;g.emit('kitchen-escaped');return;}
  if(g.time-this.startedAt<this.map.kitchenSequence.leadSeconds){this.speed=0;return;}
  const gap=Math.hypot(g.x-this.x,g.y-this.y)*g.data.tileSize;
  let playerIndex=0,nearest=Infinity;for(let i=0;i<this.path.length;i++){const q=this.path[i],d=Math.hypot(g.x-q[0],g.y-q[1]);if(d<nearest){nearest=d;playerIndex=i;}}
  const routeGap=(playerIndex-this.index)*g.data.tileSize;
  this.speed=({easy:2,normal:2.25,hard:2.5,extreme:2.65})[this.survival.mode]+(routeGap>12?1.2:0);
  let move=this.speed*dt/g.data.tileSize;this.walkPhase+=dt*this.speed*4;
  while(move>0&&this.index<this.path.length-1){const p=this.path[this.index+1],dx=p[0]-this.x,dy=p[1]-this.y,n=Math.hypot(dx,dy),step=Math.min(move,n);if(n){this.direction={x:dx/n,y:dy/n};this.x+=dx/n*step;this.y+=dy/n*step;}this.distance+=step*g.data.tileSize;move-=step;if(n<=step+.00001)this.index++;this.clearAround();}
  if(Math.hypot(g.x-this.x,g.y-this.y)*g.data.tileSize<1.12){g.caught=true;g.emit('caught',{source:'배식장'});this.phase='failed';this.speed=0;}
 }
 inspect(){return{phase:this.phase,active:this.active,x:this.x,y:this.y,pathIndex:this.index,destroyed:this.destroyed,remaining:this.blocks.filter(b=>b.active).length,revision:this.revision,monster:'배식장'};}
}

export function createKitchenVisual(T,sequence,pos){
 const root=new T.Group();root.name='조리실 붕괴와 배식장';const metal=new T.MeshStandardMaterial({color:'#657771',roughness:.9}),wood=new T.MeshStandardMaterial({color:'#84704e',roughness:1});
 const rubble=new T.InstancedMesh(new T.BoxGeometry(1,1,1),new T.MeshStandardMaterial({color:'#ffffff',roughness:1}),sequence.map.width*sequence.map.height);rubble.count=0;rubble.frustumCulled=false;root.add(rubble);const dummy=new T.Object3D();let lastRevision=-1;
 const chef=new T.Group();chef.name='배식장 · Boiler steward';root.add(chef);
 const m=c=>new T.MeshStandardMaterial({color:c,roughness:.95});const apron=m('#b4b09a'),flesh=m('#88988c'),dark=m('#283e3b'),rust=m('#756447');
 function part(name,p,s,material,shape='box'){const mesh=new T.Mesh(shape==='sphere'?new T.SphereGeometry(.5,10,8):shape==='cylinder'?new T.CylinderGeometry(.5,.5,1,12):new T.BoxGeometry(1,1,1),material);mesh.name=name;mesh.position.set(...p);mesh.scale.set(...s);mesh.castShadow=true;chef.add(mesh);return mesh;}
 part('Massive boiler-like torso',[0,1.45,0],[1.65,1.65,1.1],dark,'sphere');part('Grease-stained heavy apron',[0,1.25,.52],[1.25,1.6,.10],apron);
 for(let i=0;i<10;i++)part('Apron scorch marks',[(i%3-1)*.33,.6+(i%5)*.25,.58],[.18,.23,.025],rust);
 part('Dented lid face',[0,2.48,.12],[.78,.58,.6],metal,'cylinder');part('Dark lid slit',[0,2.5,.44],[.52,.06,.04],dark);part('Lid handle',[0,2.83,.1],[.32,.12,.12],rust);
 const limbs=[];for(const side of [-1,1]){limbs.push(part('Thick dragging forearm',[side*.98,1.1,.12],[.42,1.65,.5],flesh));limbs.push(part('Heavy kitchen boot',[side*.48,.27,.18],[.5,.55,.75],dark));}
 part('Ladle shaft',[1.13,.7,.35],[.08,1.4,.08],metal);part('Huge serving ladle',[1.13,.14,.45],[.48,.20,.48],metal,'sphere');
 const arrows=new T.InstancedMesh(new T.BoxGeometry(.18,.025,.18),new T.MeshStandardMaterial({color:'#d1bd72',emissive:'#a37c24',emissiveIntensity:.3}),sequence.path.length);arrows.count=0;arrows.frustumCulled=false;root.add(arrows);
 sequence.path.forEach((p,i)=>{dummy.position.copy(pos(p[0],p[1],.045));dummy.scale.set(1,1,1);dummy.updateMatrix();arrows.setMatrixAt(i,dummy.matrix)});
 function update(){
  const active=sequence.active,fall=active?Math.max(0,1-(sequence.game.time-sequence.startedAt)/.8):0;
  if(lastRevision!==sequence.revision||fall>0){let n=0;for(const b of sequence.blocks)if(b.active){dummy.position.copy(pos(b.x,b.y,b.height/2+fall*4));dummy.scale.set(b.width*.9,b.height,b.depth*.9);dummy.rotation.set(0,((b.x+b.y)%3-1)*.07,0);dummy.updateMatrix();rubble.setColorAt(n,new T.Color(['#657771','#8a7959','#788577'][(b.x+b.y*3)%3]));rubble.setMatrixAt(n++,dummy.matrix)}rubble.count=n;rubble.instanceMatrix.needsUpdate=true;if(rubble.instanceColor)rubble.instanceColor.needsUpdate=true;lastRevision=fall>0?-1:sequence.revision;}
  arrows.count=sequence.phase==='idle'?0:sequence.path.length;chef.visible=active;chef.position.copy(pos(sequence.x,sequence.y));chef.rotation.y=Math.atan2(sequence.direction.x,-sequence.direction.y);
  limbs.forEach((l,i)=>l.rotation.x=Math.sin(sequence.walkPhase+(i%2)*Math.PI)*.18);chef.position.y+=Math.abs(Math.sin(sequence.walkPhase))*.035;
 }
 return{root,chef,update};
}
