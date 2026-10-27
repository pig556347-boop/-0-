// Shared-data browser review kernel. This is independently implemented for
// review and does not stand in for Unity CharacterController validation.
export class Exploration {
  constructor(data) {
    this.data=data;this.floor=data.spawnFloor;this.x=data.spawnX;this.y=data.spawnY;
    this.time=0;this.lastTransition=-10;this.transitions=0;this.distance=0;this.speed=0;
    this.direction={x:0,y:-1};this.phase=0;this.lastDelta={x:0,y:0};
    this.autoStairsEnabled=true;this.arrivalLock=null;
    this.roomId='';this.currentSpaceId=`floor-${data.spawnFloor}`;this.roomTransitions=0;this.lastDoorTransition=-10;this.pendingDoor=null;
    this.maxStamina=100;this.stamina=100;this.exhausted=false;this.isRunning=false;this.lastSprintTime=-10;this.caught=false;
    this.events=[];this.eventSerial=0;this.won=false;this.speedBoostUntil=0;this.noiseDistance=0;
    this.spaces=new Map([...data.floors,...(data.roomMaps||[]),...(data.outdoorMaps||[])].map(f=>[f.id,f]));
    this.solidFixtures=Object.fromEntries([...this.spaces].map(([id,f])=>[id,(f.fixtures||[]).filter(o=>o.solid)]));
  }
  get spaceId(){return this.currentSpaceId||this.roomId||`floor-${this.floor}`}
  get map(){return this.spaces.get(this.spaceId)}
  open(x,y,f=this.map){return x>=0&&y>=0&&x<f.width&&y<f.height&&f.rows[y][x]==='.'}
  emit(type,detail={}){const event={...detail,id:++this.eventSerial,type,time:this.time};this.events.push(event);if(this.events.length>128)this.events.shift();return event}
  reset(){const serial=this.eventSerial;Object.assign(this,new Exploration(this.data));this.eventSerial=serial;this.emit('restart')}
  recoverStamina(dt){
    if(this.time-this.lastSprintTime>=1.5)this.stamina=Math.min(this.maxStamina,this.stamina+18*dt);
    if(this.exhausted&&this.stamina>=this.maxStamina*.25){this.exhausted=false;this.emit('recovered')}
  }
  overlaps(x,y,rect,radius=this.data.playerRadius) {
    const r=radius/this.data.tileSize;
    const dx=x-Math.max(rect[0],Math.min(x,rect[2])),dy=y-Math.max(rect[1],Math.min(y,rect[3]));
    return dx*dx+dy*dy<r*r-1e-10;
  }
  blocked(x,y,f=this.map,radius=this.data.playerRadius) {
    if(!f)return true;const r=radius/this.data.tileSize;
    for(let j=Math.floor(y-r-.5);j<=Math.ceil(y+r+.5);j++)for(let i=Math.floor(x-r-.5);i<=Math.ceil(x+r+.5);i++)
      if(!this.open(i,j,f)&&this.overlaps(x,y,[i-.5,j-.5,i+.5,j+.5],radius))return true;
    for(const room of [...f.rooms,...(f.portals||[])]){
      if(room.interiorOpen)continue;
      const [dx,dy]=directions[room.facing],cx=room.doorX+dx*.51,cy=room.doorY+dy*.51;
      const a=dx? .14/this.data.tileSize/2:1.02/this.data.tileSize/2;
      const b=dy? .14/this.data.tileSize/2:1.02/this.data.tileSize/2;
      if(this.overlaps(x,y,[cx-a,cy-b,cx+a,cy+b],radius))return true;
    }
    for(const item of this.solidFixtures[f.id]||[]){
      const a=item.width/this.data.tileSize/2,b=item.depth/this.data.tileSize/2;
      if(this.overlaps(x,y,[item.x-a,item.y-b,item.x+a,item.y+b],radius))return true;
    }
    for(const gate of f.barriers){
      const side=gate.x<5||gate.x>43,a=(side?.22:3.6)/this.data.tileSize/2,b=(side?3.6:.22)/this.data.tileSize/2;
      if(this.overlaps(x,y,[gate.x-a,gate.y-b,gate.x+a,gate.y+b],radius))return true;
    }
    return false;
  }
  step(ax,ay,run,dt) {
    if(!Number.isFinite(dt)||dt<0)throw Error('Invalid timestep');
    if(this.won){this.speed=0;this.isRunning=false;this.lastDelta={x:0,y:0};return;}
    const boost=this.time<this.speedBoostUntil?1.3:1;
    dt=Math.min(dt,.05);this.time+=dt;
    if(this.caught){this.speed=0;this.isRunning=false;this.lastDelta={x:0,y:0};return;}
    if(this.pendingDoor){
      this.speed=0;this.isRunning=false;this.lastDelta={x:0,y:0};this.recoverStamina(dt);
      if(this.time>=this.pendingDoor.until)this.finishDoor();
      return;
    }
    const n=Math.hypot(ax,ay);if(n>1){ax/=n;ay/=n}
    const unlimited=!!this.unlimitedStamina?.();if(unlimited){this.stamina=this.maxStamina;this.exhausted=false;}
    const sprint=!!run&&!this.exhausted&&this.stamina>0&&n>.01;
    const speed=(sprint?this.data.runSpeed:this.data.walkSpeed)*boost;
    let dx=ax*speed*dt/this.data.tileSize,dy=ay*speed*dt/this.data.tileSize;
    const count=Math.max(1,Math.ceil(Math.hypot(dx,dy)/(this.data.playerRadius/this.data.tileSize*.45)));
    dx/=count;dy/=count;const ox=this.x,oy=this.y;
    for(let i=0;i<count;i++){
      if(!this.blocked(this.x+dx,this.y))this.x+=dx;
      if(!this.blocked(this.x,this.y+dy))this.y+=dy;
    }
    this.lastDelta={x:(this.x-ox)*this.data.tileSize,y:(this.y-oy)*this.data.tileSize};
    const distance=Math.hypot(this.lastDelta.x,this.lastDelta.y);
    this.isRunning=sprint&&distance>.0001;
    if(this.isRunning&&!unlimited){
      this.stamina=Math.max(0,this.stamina-20*dt*Math.min(1,distance/(speed*dt||1)));this.lastSprintTime=this.time;
      if(this.stamina<=.00001){this.stamina=0;this.exhausted=true;this.emit('exhausted')}
    }else this.recoverStamina(dt);
    this.distance+=distance;this.speed=dt?distance/dt:0;this.phase+=distance*(this.isRunning?4.2:5.3);
    this.noiseDistance+=distance;
    if(this.noiseDistance>=(this.isRunning?1.4:.9)){this.noiseDistance=0;this.emit('noise',{spaceId:this.spaceId,x:this.x,y:this.y,radius:this.isRunning?8:2,loudness:1,kind:this.isRunning?'running':'walking'})}
    if(n>.01)this.direction={x:ax,y:ay};
    if(this.autoStairsEnabled)this.updateStairs();
  }
  currentRoom(){return this.roomId?this.map:null}
  nearby(){return this.map.isOutdoor?null:this.map.stairs.find(s=>Math.hypot(s.x-this.x,s.y-this.y)*this.data.tileSize<1.55)}
  nearbyDoor(){
    if(this.pendingDoor)return null;
    const candidates=this.roomId?(this.map.noExit?[]:[{label:this.map.exitLabel||'복도로 나가기',exit:true,targetId:this.map.returnSpaceId||`floor-${this.floor}`,doorX:this.map.exitX,doorY:this.map.exitY,facing:'north'}]):
      this.map.rooms.filter(r=>r.interiorId).map(r=>({...r,targetId:r.interiorId,exit:false}));
    candidates.push(...(this.map.portals||[]).map(portal=>({...portal,portal:true,exit:false})));
    return candidates.map(r=>{const [dx,dy]=directions[r.facing];return {...r,approachX:r.doorX+dx,approachY:r.doorY+dy,dx,dy}})
      .filter(r=>Math.hypot(r.approachX-this.x,r.approachY-this.y)*this.data.tileSize<1.55&&
        (this.x-r.doorX)*r.dx+(this.y-r.doorY)*r.dy>.45&&this.segmentClear(this.x,this.y,r.approachX,r.approachY))
      .sort((a,b)=>Math.hypot(a.approachX-this.x,a.approachY-this.y)-Math.hypot(b.approachX-this.x,b.approachY-this.y))[0]||null;
  }
  interact(){
    if(this.won||this.caught||this.pendingDoor||this.time-this.lastDoorTransition<.6)return false;
    const door=this.nearbyDoor();if(!door)return false;if(door.locked){this.emit('locked-door',{label:door.label});return false;}
    if(this.travelGuard&&!this.travelGuard(door.targetId,'door'))return false;
    this.pendingDoor={...door,sourceId:this.spaceId,started:this.time,until:this.time+.22};this.speed=0;this.isRunning=false;
    this.emit('door-open',{spaceId:this.spaceId,label:door.label,x:door.doorX,y:door.doorY});
    this.emit('noise',{spaceId:this.spaceId,x:this.x,y:this.y,radius:6,loudness:1,kind:'door'});return true;
  }
  finishDoor(){
    const door=this.pendingDoor;this.pendingDoor=null;if(!door)return;
    const fromId=this.spaceId,sourceX=door.approachX,sourceY=door.approachY;
    if(door.portal)this.setSpaceId(door.targetId,door.arrivalX,door.arrivalY);
    else if(door.exit){const from=this.map;this.setSpaceId(from.returnSpaceId||`floor-${from.sourceFloor}`,from.returnX,from.returnY)}
    else{const next=this.spaces.get(door.targetId);this.setSpace(next.id,next.sourceFloor,next.spawnX,next.spawnY)}
    this.roomTransitions++;this.lastDoorTransition=this.time;
    this.emit('transition',{kind:'door',fromId,toId:this.spaceId,sourceX,sourceY,arrivalX:this.x,arrivalY:this.y,label:door.label});
    this.emit('door-close',{spaceId:this.spaceId,label:door.label,x:this.x,y:this.y});
  }
  setSpace(roomId,floor,x,y){
    return this.setSpaceId(roomId||`floor-${floor}`,x,y);
  }
  setSpaceId(id,x,y){
    const next=this.spaces.get(id);
    if(!next||this.blocked(x,y,next))throw Error(`Blocked space landing ${id}:${x},${y}`);
    this.currentSpaceId=id;this.roomId=next.isRoom?id:'';this.floor=next.isOutdoor?0:(next.sourceFloor??next.number);
    this.x=x;this.y=y;this.speed=0;this.isRunning=false;this.arrivalLock=null;this.lastDelta={x:0,y:0};
  }
  updateStairs(){
    if(this.map.isOutdoor||this.pendingDoor)return false;
    if(this.arrivalLock){
      const s=this.map.stairs.find(s=>s.id===this.arrivalLock);
      if(s&&Math.hypot(s.x-this.x,s.y-this.y)*this.data.tileSize<=this.data.stairExitRadius)return false;
      this.arrivalLock=null;
    }
    if(this.time-this.lastTransition<this.data.stairCooldown)return false;
    const s=this.map.stairs.find(s=>Math.abs(s.x-this.x)*this.data.tileSize<this.data.stairEntryHalfWidth&&Math.abs(s.y-this.y)*this.data.tileSize<this.data.stairEntryHalfDepth);
    if(!s)return false;
    if(this.travelGuard&&!this.travelGuard(s.targetId||`floor-${s.target}`,'stairs'))return false;
    const previous=this.floor,fromId=this.spaceId;
    this.warpSpace(s.targetId||`floor-${s.target}`,s.arrivalX,s.arrivalY);
    this.arrivalLock=this.map.stairs.find(s=>(s.targetId||`floor-${s.target}`)===fromId)?.id||null;
    this.transitions++;this.lastTransition=this.time;
    this.emit('transition',{kind:'stairs',fromId,toId:this.spaceId,sourceX:s.x,sourceY:s.y,arrivalX:this.x,arrivalY:this.y,label:s.label});return true;
  }
  warp(floor,x,y){this.pendingDoor=null;this.setSpace('',floor,x,y)}
  warpRoom(id,x,y){const f=this.spaces.get(id);if(!f?.isRoom)throw Error('Unknown room '+id);this.pendingDoor=null;this.setSpace(id,f.sourceFloor,x,y)}
  warpSpace(id,x,y){this.pendingDoor=null;this.setSpaceId(id,x,y)}
  segmentClear(ax,ay,bx,by,f=this.map,radius=this.data.playerRadius){
    if(!f)return false;const r=radius/this.data.tileSize,r2=r*r-1e-10;
    const hits=rect=>segmentRectDistance2(ax,ay,bx,by,rect)<r2;
    for(let y=Math.floor(Math.min(ay,by)-r-.5);y<=Math.ceil(Math.max(ay,by)+r+.5);y++)
      for(let x=Math.floor(Math.min(ax,bx)-r-.5);x<=Math.ceil(Math.max(ax,bx)+r+.5);x++)
        if(!this.open(x,y,f)&&hits([x-.5,y-.5,x+.5,y+.5]))return false;
    for(const room of [...f.rooms,...(f.portals||[])]){
      if(room.interiorOpen)continue;
      const [dx,dy]=directions[room.facing],cx=room.doorX+dx*.51,cy=room.doorY+dy*.51;
      const a=(dx?.14:1.02)/this.data.tileSize/2,b=(dy?.14:1.02)/this.data.tileSize/2;
      if(hits([cx-a,cy-b,cx+a,cy+b]))return false;
    }
    for(const item of this.solidFixtures[f.id]||[]){
      const a=item.width/this.data.tileSize/2,b=item.depth/this.data.tileSize/2;
      if(hits([item.x-a,item.y-b,item.x+a,item.y+b]))return false;
    }
    for(const gate of f.barriers){
      const side=gate.x<5||gate.x>43,a=(side?.22:3.6)/this.data.tileSize/2,b=(side?3.6:.22)/this.data.tileSize/2;
      if(hits([gate.x-a,gate.y-b,gate.x+a,gate.y+b]))return false;
    }
    return true;
  }
  path(x,y){
    const key=(x,y)=>`${x},${y}`,start=[Math.round(this.x),Math.round(this.y)],q=[start];
    const prev=new Map([[key(...start),null]]);let end;
    for(let i=0;i<q.length;i++){
      const p=q[i];if(p[0]===x&&p[1]===y){end=p;break}
      for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]){
        const n=[p[0]+dx,p[1]+dy],k=key(...n);
        if(this.open(...n)&&!prev.has(k)&&this.segmentClear(p[0],p[1],n[0],n[1])){prev.set(k,p);q.push(n)}
      }
    }
    if(!end)return null;const result=[];
    for(let p=end;p;p=prev.get(key(...p)))result.push(p);
    return result.reverse().slice(1);
  }
  walkTo(x,y){
    const path=this.path(x,y),startSpace=this.spaceId;if(!path)return false;
    for(const [tx,ty] of path){
      let i=0;
      while(Math.hypot(tx-this.x,ty-this.y)>.015&&i++<200){
        const dx=tx-this.x,dy=ty-this.y,n=Math.hypot(dx,dy);
        this.step(dx/n,dy/n,true,Math.min(.016,n*this.data.tileSize/((this.exhausted||this.stamina<=0?this.data.walkSpeed:this.data.runSpeed)*(this.time<this.speedBoostUntil?1.3:1))));
        if(this.spaceId!==startSpace)return !this.blocked(this.x,this.y);
      }
      if(i>=200)return false;
    }
    return !this.blocked(this.x,this.y);
  }
}
export const directions={north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]};

// Minimum squared distance between a segment and an axis-aligned rectangle.
// Exact capsule clearance avoids a sampled ray accepting tiny corner contacts.
function segmentRectDistance2(ax,ay,bx,by,rect){
  const [x0,y0,x1,y1]=rect,dx=bx-ax,dy=by-ay;let lo=0,hi=1,intersects=true;
  for(const [a,d,min,max] of [[ax,dx,x0,x1],[ay,dy,y0,y1]]){
    if(Math.abs(d)<1e-12){if(a<min||a>max){intersects=false;break;}}
    else{const p=(min-a)/d,q=(max-a)/d;lo=Math.max(lo,Math.min(p,q));hi=Math.min(hi,Math.max(p,q));if(lo>hi){intersects=false;break;}}
  }
  if(intersects)return 0;
  const pointRect=(x,y)=>(x-Math.max(x0,Math.min(x,x1)))**2+(y-Math.max(y0,Math.min(y,y1)))**2;
  let best=Math.min(pointRect(ax,ay),pointRect(bx,by)),length2=dx*dx+dy*dy;
  for(const [x,y] of [[x0,y0],[x0,y1],[x1,y0],[x1,y1]]){
    const t=length2?Math.max(0,Math.min(1,((x-ax)*dx+(y-ay)*dy)/length2)):0;
    best=Math.min(best,(x-ax-t*dx)**2+(y-ay-t*dy)**2);
  }
  return best;
}
