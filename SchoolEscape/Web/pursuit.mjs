// Original SchoolEscape caretaker: local perception, remembered evidence and
// independently chosen patrol routes. It never receives a live hidden target.
export class Pursuit {
  constructor(game){
    this.game=game;this.radius=.31;this.walkSpeed=1.35;this.chaseSpeed=3.3;
    this.grids=new Map();this.enabled=true;
    this.rules={sightRange:11,fovDegrees:120,peripheralRange:1.8,investigateSpeed:1.9,
      unseenBeforeSearch:8,searchSeconds:4,memorySeconds:12,unreachableSeconds:3,exitEvidenceSeconds:2.5};
    this.reset();
  }
  random(){
    let a=this.randomState=(this.randomState+0x6D2B79F5)>>>0;
    a=Math.imul(a^(a>>>15),a|1);a^=a+Math.imul(a^(a>>>7),a|61);
    return ((a^(a>>>14))>>>0)/4294967296;
  }
  seedValue(seed){
    if(Number.isFinite(seed))return seed>>>0;
    if(typeof seed==='string'){let h=2166136261;for(const c of seed)h=Math.imul(h^c.charCodeAt(0),16777619);return h>>>0;}
    if(globalThis.crypto?.getRandomValues){const n=new Uint32Array(1);globalThis.crypto.getRandomValues(n);return n[0];}
    return (Date.now()^Math.floor(Math.random()*4294967296))>>>0;
  }
  reset(seed){
    const g=this.game;this.seed=this.seedValue(seed);this.randomState=this.seed;
    this.spaceId=`floor-${g.data.spawnFloor}`;this.x=g.data.spawnX;this.y=g.data.spawnY;
    this.direction={x:0,y:-1};this.phase=0;this.speed=0;this.state='patrol';this.caught=false;this.transfers=0;
    this.lastSensed=-100;this.lastSeen=-100;this.lastHeard=-100;this.lastKnown=null;this.lastEvidenceKind='none';
    this.graceUntil=g.time+4;this.catchGraceUntil=this.graceUntil;this.eventCursor=g.eventSerial;
    this.portalTrail=[];this.transit=null;this.path=[];this.pathUntil=-1;this.target=null;
    this.patrolIndex=0;this.patrolWait=0;this.patrolGoal=null;this.patrolPortal=null;this.localPatrols=0;
    this.searchUntil=-1;this.searchGoal=null;this.stuckSeconds=0;this.failedTransfers=0;this.lostReason='';
    this.hasSeenPlayer=false;this.lastSense={visible:false,heard:false};g.caught=false;
    this.spawnCandidates=this.designatedSpawns();
    const spawn=this.spawnCandidates[Math.floor(this.random()*this.spawnCandidates.length)];
    if(!spawn)throw Error('No safe designated caretaker spawn');
    this.spaceId=spawn.spaceId;this.x=spawn.x;this.y=spawn.y;
    const same=this.spaceId===g.spaceId;
    this.initial={spaceId:this.spaceId,x:this.x,y:this.y,spawnId:spawn.id,label:spawn.label,
      sameSpace:same,distanceFromPlayer:same?Math.hypot(this.x-g.x,this.y-g.y)*g.data.tileSize:Infinity,
      pathDistanceFromPlayer:same?spawn.pathDistance:Infinity};
  }
  designatedSpawns(){
    const g=this.game,tile=g.data.tileSize,candidates=[];
    for(const f of [...g.data.floors,...(g.data.outdoorMaps||[])]){
      const nav=this.grid(f),same=f.id===g.spaceId;
      const origin=same?this.closestNode(nav,g.x,g.y,false,f):-1;
      const distance=same?this.distances(nav,origin):null;
      const mapOrigin=this.closestNode(nav,f.spawnX,f.spawnY,false,f),connected=this.distances(nav,mapOrigin);
      const specified=(f.enemySpawnPoints||[]).concat((g.data.enemySpawnPoints||[]).filter(point=>point.spaceId===f.id));
      const points=specified.length?specified:f.landmarks||[];
      for(let i=0;i<points.length;i++){
        const landmark=points[i],x=landmark.x,y=landmark.y;
        if(/현관|통로 끝/.test(landmark.label)||g.blocked(x,y,f,this.radius))continue;
        const id=nav.byKey.get(`${x},${y}`);if(id===undefined||!connected.has(id))continue;
        if((f.stairs||[]).some(s=>Math.hypot(s.x-x,s.y-y)*tile<3.2))continue;
        if((f.rooms||[]).some(r=>Math.hypot(r.doorX-x,r.doorY-y)*tile<=2.5))continue;
        if((f.portals||[]).some(p=>Math.hypot(p.doorX-x,p.doorY-y)*tile<3.2))continue;
        if((f.barriers||[]).some(b=>Math.hypot(b.x-x,b.y-y)*tile<3))continue;
        if(g.data.escapeGate?.spaceId===f.id&&Math.hypot(g.data.escapeGate.x-x,g.data.escapeGate.y-y)*tile<4)continue;
        const pathDistance=same?(distance.get(id)??-1)*tile:Infinity;
        if(same&&(pathDistance<15||Math.hypot(x-g.x,y-g.y)*tile<15))continue;
        candidates.push({id:landmark.id||`${f.id}-landmark-${i}`,spaceId:f.id,floor:f.number,x,y,label:landmark.label,pathDistance});
      }
    }
    return candidates;
  }
  clearAwareness(){
    this.state='patrol';this.caught=false;this.portalTrail=[];this.transit=null;
    this.path=[];this.pathUntil=-1;this.target=null;this.lastKnown=null;
    this.lastSensed=this.lastSeen=this.lastHeard=-100;this.hasSeenPlayer=false;
    this.searchUntil=-1;this.searchGoal=null;this.stuckSeconds=0;
    this.patrolGoal=null;this.patrolPortal=null;this.patrolWait=this.game.time+1;
    this.eventCursor=this.game.eventSerial;this.lastEvidenceKind='none';this.lastSense={visible:false,heard:false};
  }
  setEnabled(enabled){
    const was=this.enabled;this.enabled=!!enabled;this.speed=0;
    this.game.caught=false;this.caught=false;this.clearAwareness();
    if(this.enabled)this.catchGraceUntil=this.game.time+2;
    if(was!==this.enabled)this.game.emit('enemy-toggle',{enabled:this.enabled});
  }
  // Explicit QA setup only. Normal play uses designated spawn points and routes.
  place(spaceId,x,y,state='chase'){
    const g=this.game,f=g.spaces.get(spaceId);
    if(!f||g.blocked(x,y,f,this.radius))throw Error('Blocked pursuer placement');
    this.clearAwareness();this.spaceId=spaceId;this.x=x;this.y=y;this.state=state;this.speed=0;
    this.caught=false;g.caught=false;this.catchGraceUntil=g.time+1;
    if(state!=='patrol'){
      this.lastSensed=g.time;this.lastSeen=g.time;this.lastKnown={spaceId:g.spaceId,x:g.x,y:g.y};
      this.hasSeenPlayer=true;this.lastEvidenceKind='qa-placement';
      if(state==='search')this.searchUntil=g.time+this.rules.searchSeconds;
    }
    return this.inspect();
  }
  grid(f){
    if(this.grids.has(f.id))return this.grids.get(f.id);
    const nav={map:f,nodes:[],byKey:new Map()};
    for(let y=0;y<f.height;y++)for(let x=0;x<f.width;x++)
      if(this.game.open(x,y,f)&&!this.game.blocked(x,y,f,this.radius)){
        const id=nav.nodes.length;nav.nodes.push({id,x,y,edges:[]});nav.byKey.set(`${x},${y}`,id);
      }
    for(const n of nav.nodes)for(const [dx,dy] of [[1,0],[0,1]]){
      const id=nav.byKey.get(`${n.x+dx},${n.y+dy}`);if(id===undefined)continue;
      const next=nav.nodes[id];
      if(this.game.segmentClear(n.x,n.y,next.x,next.y,f,this.radius)){n.edges.push(id);next.edges.push(n.id);}
    }
    this.grids.set(f.id,nav);return nav;
  }
  closestNode(nav,x,y,visible=false,f=nav.map){
    let best=-1,score=Infinity;
    for(const n of nav.nodes){const d=(n.x-x)**2+(n.y-y)**2;
      if(d<score&&(!visible||this.game.segmentClear(x,y,n.x,n.y,f,this.radius))){score=d;best=n.id;}
    }
    return best;
  }
  distances(nav,start){
    if(start<0)return new Map();const q=[start],d=new Map([[start,0]]);
    for(let i=0;i<q.length;i++)for(const n of nav.nodes[q[i]].edges)if(!d.has(n)){d.set(n,d.get(q[i])+1);q.push(n);}
    return d;
  }
  route(tx,ty){
    const g=this.game,f=g.spaces.get(this.spaceId),nav=this.grid(f);
    if(!g.blocked(tx,ty,f,this.radius)&&g.segmentClear(this.x,this.y,tx,ty,f,this.radius))return [{x:tx,y:ty}];
    const start=this.closestNode(nav,this.x,this.y,true);if(start<0)return [];
    const q=[start],prev=new Map([[start,-1]]);let end=start,best=Infinity;
    for(let i=0;i<q.length;i++){
      const id=q[i],n=nav.nodes[id],d=(n.x-tx)**2+(n.y-ty)**2;
      if(d<best){best=d;end=id;}if(d<.0025)break;
      for(const edge of n.edges)if(!prev.has(edge)){prev.set(edge,id);q.push(edge);}
    }
    const path=[];for(let id=end;id!==-1;id=prev.get(id))path.push({x:nav.nodes[id].x,y:nav.nodes[id].y});path.reverse();
    if(!g.blocked(tx,ty,f,this.radius)&&g.segmentClear(path.at(-1).x,path.at(-1).y,tx,ty,f,this.radius))path.push({x:tx,y:ty});
    while(path.length>1&&g.segmentClear(this.x,this.y,path[1].x,path[1].y,f,this.radius))path.shift();
    return path;
  }
  canSeePlayer(){
    const g=this.game;if(g.spaceId!==this.spaceId)return false;
    const dx=g.x-this.x,dy=g.y-this.y,n=Math.hypot(dx,dy),distance=n*g.data.tileSize;
    if(distance>this.rules.sightRange)return false;
    if(distance>this.rules.peripheralRange){
      const facing=Math.hypot(this.direction.x,this.direction.y)||1;
      const dot=(dx*this.direction.x+dy*this.direction.y)/((n||1)*facing);
      if(dot<Math.cos(this.rules.fovDegrees*Math.PI/360))return false;
    }
    return g.segmentClear(this.x,this.y,g.x,g.y,g.map,.025);
  }
  remember(spaceId,x,y,time,kind){
    this.lastKnown={spaceId,x,y};this.lastSensed=time;this.lastEvidenceKind=kind;
    this.pathUntil=-1;this.searchGoal=null;this.searchUntil=-1;this.stuckSeconds=0;
    this.patrolGoal=null;this.patrolPortal=null;this.patrolWait=0;
  }
  hearNoise(event){
    const g=this.game;
    if(event.spaceId!==this.spaceId||g.time-event.time>.75||!Number.isFinite(event.x)||!Number.isFinite(event.y))return false;
    const radius=Number.isFinite(event.radius)?event.radius:({walk:2,run:8,door:6,search:4,drop:2.5}[event.kind]||3);
    const loudness=Number.isFinite(event.loudness)?Math.max(0,event.loudness):1;
    const maxRadius=Math.min(20,radius*loudness),distance=Math.hypot(event.x-this.x,event.y-this.y)*g.data.tileSize;
    if(distance>maxRadius||maxRadius<=0)return false;
    const f=g.spaces.get(this.spaceId),clear=g.segmentClear(this.x,this.y,event.x,event.y,f,.025);
    if(!clear){
      if(distance>maxRadius*.5)return false;
      // Around a corner, only a locally reachable sound can be heard. A wall
      // never reveals a live player coordinate or an unrelated room's events.
      const path=this.route(event.x,event.y);if(!path.length)return false;
      let length=0,x=this.x,y=this.y;
      for(const p of path){length+=Math.hypot(p.x-x,p.y-y)*g.data.tileSize;x=p.x;y=p.y;}
      if(length>maxRadius||Math.hypot(x-event.x,y-event.y)*g.data.tileSize>2)return false;
    }
    if(this.transit?.portal.reason==='roam')this.transit=null;
    this.remember(this.spaceId,event.x,event.y,event.time,'sound:'+event.kind);
    this.lastHeard=event.time;this.lastSense.heard=true;
    if(this.state!=='chase')this.state=this.hasSeenPlayer&&g.time-this.lastSeen<this.rules.memorySeconds?'chase':'investigate';
    return true;
  }
  observeEvents(){
    const g=this.game;
    for(const event of g.events){
      if(event.id<=this.eventCursor)continue;this.eventCursor=event.id;
      if(event.type==='noise'){this.hearNoise(event);continue;}
      if(event.type!=='transition'||event.fromId!==this.spaceId||this.transit)continue;
      // Exit information is retained only when the enemy actually had recent
      // evidence at this doorway. Remote player transitions are not handed on.
      if(!['chase','search'].includes(this.state)||!this.lastKnown||this.lastKnown.spaceId!==event.fromId)continue;
      if(event.time-this.lastSensed>this.rules.exitEvidenceSeconds)continue;
      if(Math.hypot(this.lastKnown.x-event.sourceX,this.lastKnown.y-event.sourceY)*g.data.tileSize>3)continue;
      if(Math.hypot(this.x-event.sourceX,this.y-event.sourceY)*g.data.tileSize>this.rules.sightRange)continue;
      this.portalTrail=[{...event,eventId:event.id,reason:'chase',expires:event.time+this.rules.memorySeconds}];
      this.lastKnown={spaceId:event.fromId,x:event.sourceX,y:event.sourceY};
      this.path=[];this.pathUntil=-1;this.stuckSeconds=0;
    }
  }
  sense(){
    const g=this.game,visible=this.canSeePlayer();this.lastSense.visible=visible;
    if(visible){
      if(this.state!=='chase')g.emit('chase-start',{spaceId:this.spaceId});
      this.state='chase';this.lastSeen=g.time;this.hasSeenPlayer=true;
      this.remember(this.spaceId,g.x,g.y,g.time,'sight');
      // Seeing the player again supersedes an old guessed exit route.
      this.portalTrail=[];return true;
    }
    return false;
  }
  portals(f=this.game.spaces.get(this.spaceId)){
    const g=this.game,portals=[];
    if(f.isRoom&&!f.noExit){
      portals.push({kind:'door',fromId:f.id,toId:f.returnSpaceId||`floor-${f.sourceFloor}`,sourceX:f.exitX,sourceY:f.exitY+1,
        arrivalX:f.returnX,arrivalY:f.returnY,label:'복도로 나가기',reason:'roam'});
    }
    {
      for(const s of f.stairs||[])portals.push({kind:'stairs',fromId:f.id,toId:s.targetId||`floor-${s.target}`,
        sourceX:s.x,sourceY:s.y,arrivalX:s.arrivalX,arrivalY:s.arrivalY,label:s.label,reason:'roam'});
      for(const r of f.rooms||[]){
        const destination=g.spaces.get(r.interiorId);if(!destination)continue;
        const [dx,dy]=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[r.facing];
        portals.push({kind:'door',fromId:f.id,toId:r.interiorId,sourceX:r.doorX+dx,sourceY:r.doorY+dy,
          arrivalX:destination.spawnX,arrivalY:destination.spawnY,label:r.label,reason:'roam'});
      }
    }
    for(const portal of f.portals||[]){
      const destination=g.spaces.get(portal.targetId);if(!destination||destination.kitchenSequence)continue;
      const [dx,dy]=({north:[0,1],south:[0,-1],west:[-1,0],east:[1,0]})[portal.facing];
      portals.push({id:portal.id,kind:'door',fromId:f.id,toId:portal.targetId,
        sourceX:portal.doorX+dx,sourceY:portal.doorY+dy,arrivalX:portal.arrivalX,arrivalY:portal.arrivalY,
        label:portal.label,reason:'roam'});
    }
    return portals.filter(p=>!g.blocked(p.sourceX,p.sourceY,f,this.radius)&&
      !g.blocked(p.arrivalX,p.arrivalY,g.spaces.get(p.toId),this.radius));
  }
  patrolTarget(){
    if(this.patrolGoal)return this.patrolGoal;
    const g=this.game,f=g.spaces.get(this.spaceId),nav=this.grid(f),start=this.closestNode(nav,this.x,this.y,true);
    const reachable=this.distances(nav,start),tile=g.data.tileSize;
    let points=(f.isRoom?f.interiorZones||[]:f.landmarks||[]).filter(p=>{
      const id=nav.byKey.get(`${p.x},${p.y}`);return id!==undefined&&reachable.has(id)&&
        Math.hypot(p.x-this.x,p.y-this.y)*tile>2;
    });
    if(!points.length)points=[...reachable.keys()].map(id=>nav.nodes[id]).filter(p=>Math.hypot(p.x-this.x,p.y-this.y)*tile>2);
    const exits=this.portals(f).filter(p=>this.route(p.sourceX,p.sourceY).length>0);
    const mustLeave=f.isRoom&&this.localPatrols>=2;
    if(exits.length&&(mustLeave||this.localPatrols>=2&&this.random()<.5)){
      // Stairs get extra weight, allowing independent travel between floors.
      const weighted=exits.flatMap(p=>p.kind==='stairs'?[p,p,p]:[p]);
      this.patrolPortal={...weighted[Math.floor(this.random()*weighted.length)]};
      this.patrolGoal={x:this.patrolPortal.sourceX,y:this.patrolPortal.sourceY};
    }else{
      const point=points[Math.floor(this.random()*points.length)]||{x:this.x,y:this.y};
      this.patrolGoal={x:point.x,y:point.y};
    }
    return this.patrolGoal;
  }
  moveToward(target,speed,dt){
    const g=this.game,f=g.spaces.get(this.spaceId);
    if(!target){this.speed=0;return;}
    if(g.time>=this.pathUntil||!this.target||Math.hypot(this.target.x-target.x,this.target.y-target.y)>.7){
      this.path=this.route(target.x,target.y);this.pathUntil=g.time+.45;this.target={x:target.x,y:target.y};
    }
    let budget=speed*dt/g.data.tileSize;const ox=this.x,oy=this.y;
    while(budget>1e-8&&this.path.length){
      const next=this.path[0],dx=next.x-this.x,dy=next.y-this.y,n=Math.hypot(dx,dy);
      if(n<.015){this.path.shift();continue;}
      const step=Math.min(n,budget,.10/g.data.tileSize),sx=dx/n*step,sy=dy/n*step;
      if(g.blocked(this.x+sx,this.y+sy,f,this.radius)){this.pathUntil=-1;break;}
      this.x+=sx;this.y+=sy;budget-=step;this.direction={x:dx/n,y:dy/n};
      if(step>=n-1e-8)this.path.shift();
    }
    const distance=Math.hypot(this.x-ox,this.y-oy)*g.data.tileSize;this.speed=dt?distance/dt:0;this.phase+=distance*4.8;
    this.stuckSeconds=distance<.002&&Math.hypot(target.x-this.x,target.y-this.y)*g.data.tileSize>.6?this.stuckSeconds+dt:0;
  }
  arrival(portal){
    const g=this.game,f=g.spaces.get(portal.toId);
    for(const [dx,dy] of [[0,0],[.8,0],[-.8,0],[0,.8],[0,-.8],[.8,.8],[-.8,.8],[.8,-.8],[-.8,-.8],[0,1.6],[0,-1.6],[1.6,0],[-1.6,0]]){
      const x=portal.arrivalX+dx,y=portal.arrivalY+dy;
      if(g.blocked(x,y,f,this.radius)||!g.segmentClear(portal.arrivalX,portal.arrivalY,x,y,f,this.radius))continue;
      if(g.spaceId===portal.toId&&Math.hypot(g.x-x,g.y-y)*g.data.tileSize<1.05)continue;
      return {x,y};
    }
    return null;
  }
  beginTransit(portal){
    const g=this.game;this.speed=0;this.transit={portal,until:g.time+(portal.kind==='stairs'?1.45:1.25),started:g.time};
    this.state=portal.reason==='roam'?'patrol':'transit';this.path=[];this.pathUntil=-1;this.stuckSeconds=0;
    g.emit('enemy-door',{spaceId:this.spaceId,kind:portal.kind,x:this.x,y:this.y,reason:portal.reason});
  }
  finishTransit(){
    const g=this.game,portal=this.transit.portal,landing=this.arrival(portal);
    if(!landing){
      if(g.time-this.transit.started>3){this.failedTransfers++;this.transit=null;this.forget('blocked-arrival');return false;}
      this.transit.until=g.time+.2;return false;
    }
    this.spaceId=portal.toId;this.x=landing.x;this.y=landing.y;this.transfers++;
    this.transit=null;this.path=[];this.pathUntil=-1;this.target=null;this.stuckSeconds=0;
    this.catchGraceUntil=g.time+.8;this.patrolGoal=null;this.patrolPortal=null;this.localPatrols=0;
    if(portal.reason==='roam'){
      this.state='patrol';this.patrolWait=g.time+1+this.random()*2;
    }else{
      // Emergence alone is not evidence that the player is still here.
      this.state='chase';this.lastKnown={spaceId:this.spaceId,x:portal.arrivalX,y:portal.arrivalY};
    }
    g.emit('enemy-arrive',{spaceId:this.spaceId,kind:portal.kind,x:this.x,y:this.y,reason:portal.reason});return true;
  }
  beginSearch(){
    const g=this.game;this.state='search';this.searchUntil=Math.min(g.time+this.rules.searchSeconds,this.lastSensed+this.rules.memorySeconds);
    this.searchGoal=null;this.pathUntil=-1;this.stuckSeconds=0;
  }
  searchTarget(){
    if(this.searchGoal&&Math.hypot(this.searchGoal.x-this.x,this.searchGoal.y-this.y)*this.game.data.tileSize>.4)return this.searchGoal;
    const g=this.game,f=g.spaces.get(this.spaceId),nav=this.grid(f),origin=this.lastKnown||this;
    const possible=nav.nodes.filter(n=>Math.hypot(n.x-origin.x,n.y-origin.y)*g.data.tileSize<=3&&
      Math.hypot(n.x-this.x,n.y-this.y)*g.data.tileSize>.7&&g.segmentClear(this.x,this.y,n.x,n.y,f,this.radius));
    const point=possible[Math.floor(this.random()*possible.length)]||{x:this.x,y:this.y};
    this.searchGoal={x:point.x,y:point.y};return this.searchGoal;
  }
  forget(reason){
    const wasAlert=this.alerted;this.clearAwareness();this.lostReason=reason;
    if(wasAlert)this.game.emit('chase-end',{spaceId:this.spaceId,reason});
  }
  get alerted(){return this.hasSeenPlayer&&['chase','search','transit'].includes(this.state);}
  step(dt){
    if(!Number.isFinite(dt)||dt<0)throw Error('Invalid pursuit timestep');dt=Math.min(dt,.05);
    this.speed=0;if(!this.enabled||this.caught||this.game.caught||this.game.won)return;
    const g=this.game;this.lastSense={visible:false,heard:false};this.observeEvents();
    const sensed=this.sense();
    if(this.transit){
      if(sensed){this.transit=null;this.pathUntil=-1;}
      else{if(g.time>=this.transit.until)this.finishTransit();return;}
    }
    if(!sensed&&this.state!=='patrol'&&g.time-this.lastSensed>=this.rules.memorySeconds)this.forget('memory-expired');
    this.portalTrail=this.portalTrail.filter(p=>p.expires>g.time);
    const portal=this.portalTrail[0];
    if(portal&&portal.fromId===this.spaceId){
      this.state='chase';this.moveToward({x:portal.sourceX,y:portal.sourceY},this.chaseSpeed,dt);
      if(this.stuckSeconds>=this.rules.unreachableSeconds)this.forget('unreachable-exit');
      else if(Math.hypot(this.x-portal.sourceX,this.y-portal.sourceY)*g.data.tileSize<.6){this.portalTrail.shift();this.beginTransit(portal);}
      return;
    }
    if(this.state==='chase'||this.state==='investigate'){
      if(!this.lastKnown||this.lastKnown.spaceId!==this.spaceId)this.forget('no-local-evidence');
      else{
        this.moveToward(this.lastKnown,this.state==='chase'?this.chaseSpeed:this.rules.investigateSpeed,dt);
        if(this.stuckSeconds>=this.rules.unreachableSeconds)this.forget('unreachable-last-position');
        else if(!sensed&&(Math.hypot(this.x-this.lastKnown.x,this.y-this.lastKnown.y)*g.data.tileSize<.6||
          g.time-this.lastSensed>=this.rules.unseenBeforeSearch))this.beginSearch();
      }
    }else if(this.state==='search'){
      if(g.time>=this.searchUntil)this.forget('search-finished');
      else this.moveToward(this.searchTarget(),this.walkSpeed,dt);
    }
    if(this.state==='patrol'&&!this.transit&&g.time>=this.patrolWait){
      const target=this.patrolTarget();this.moveToward(target,this.walkSpeed,dt);
      if(this.stuckSeconds>=this.rules.unreachableSeconds){this.patrolGoal=null;this.patrolPortal=null;this.pathUntil=-1;this.stuckSeconds=0;this.patrolWait=g.time+1;}
      else if(Math.hypot(target.x-this.x,target.y-this.y)*g.data.tileSize<.4){
        if(this.patrolPortal)this.beginTransit(this.patrolPortal);
        else{this.localPatrols++;this.patrolIndex++;this.patrolGoal=null;this.pathUntil=-1;this.patrolWait=g.time+1+this.random()*2;}
      }
    }
    if(g.spaceId===this.spaceId&&this.state==='chase'&&g.time>=Math.max(this.graceUntil,this.catchGraceUntil)&&
      Math.hypot(g.x-this.x,g.y-this.y)*g.data.tileSize<.7&&g.segmentClear(this.x,this.y,g.x,g.y,g.map,.025)){
      this.caught=true;this.state='caught';this.speed=0;g.caught=true;g.speed=0;g.isRunning=false;g.pendingDoor=null;
      g.emit('caught',{spaceId:this.spaceId,x:this.x,y:this.y});
    }
  }
  inspect(){
    return {enabled:this.enabled,state:this.state,alerted:this.alerted,spaceId:this.spaceId,x:this.x,y:this.y,speed:this.speed,caught:this.caught,
      transfers:this.transfers,lastSensed:this.lastSensed,lastSeen:this.lastSeen,lastHeard:this.lastHeard,
      lastKnown:this.lastKnown?{...this.lastKnown}:null,lastEvidenceKind:this.lastEvidenceKind,lastSense:{...this.lastSense},
      graceUntil:Math.max(this.graceUntil,this.catchGraceUntil),radius:this.radius,seed:this.seed,initial:this.initial,
      spawnCandidates:this.spawnCandidates.map(p=>({...p})),rules:{...this.rules},lostReason:this.lostReason,stuckSeconds:this.stuckSeconds,
      patrolGoal:this.patrolGoal?{...this.patrolGoal}:null,patrolWait:this.patrolWait,localPatrols:this.localPatrols,
      path:this.path.map(p=>({...p})),portalTrail:this.portalTrail.map(p=>({...p})),
      transit:this.transit?{until:this.transit.until,started:this.transit.started,portal:{...this.transit.portal}}:null,
      failedTransfers:this.failedTransfers};
  }
}
