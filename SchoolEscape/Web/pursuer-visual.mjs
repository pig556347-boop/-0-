// Original SchoolEscape caretaker. All geometry and motion are authored here;
// no downloaded character, animation, or other game's asset is referenced.
export function createPursuerVisual(T) {
  const root=new T.Group();root.name='당직자 · 추격자';
  const visual=new T.Group();visual.name='Caretaker animated silhouette';root.add(visual);
  const material=(color,extra={})=>new T.MeshStandardMaterial({color,roughness:.96,...extra});
  const m={coat:material('#263e3c'),shoulder:material('#304642'),seam:material('#182e2c'),wear:material('#64705a'),
    skin:material('#b7bab0'),skinShade:material('#858f88'),socket:material('#162321'),eye:material('#d7d4b8'),
    hair:material('#17201e'),shirt:material('#b7ad88'),trousers:material('#242c2b'),shoe:material('#171e1d'),
    sole:material('#44483d'),metal:material('#8c8058'),badge:material('#d3c8a7')};
  const geometries={box:new T.BoxGeometry(1,1,1),sphere:new T.SphereGeometry(.5,10,8),
    cylinder:new T.CylinderGeometry(.5,.5,1,8),taper:new T.CylinderGeometry(.48,.39,1,8)};
  let meshCount=0;
  function mesh(name,parent,p,s,mat,shape='box',shadow=true){
    const o=new T.Mesh(geometries[shape],mat);o.name=name;o.position.set(...p);o.scale.set(...s);
    o.castShadow=shadow;o.receiveShadow=true;parent.add(o);meshCount++;return o;
  }
  const group=(name,parent,p)=>{const g=new T.Group();g.name=name;g.position.set(...p);parent.add(g);return g};
  const body=group('Hunched coat and shoulders',visual,[0,1.18,0]);
  mesh('Heavy crooked coat',body,[0,.42,.035],[.76,.83,.49],m.coat,'taper');
  mesh('Hunched upper back',body,[0,.69,-.075],[.87,.55,.56],m.shoulder,'sphere');
  mesh('Lower coat',body,[0,.055,.025],[.72,.31,.46],m.coat,'taper');
  // Unpaired hem pieces, stitching and worn cloth stay static: no visual flicker.
  for(let i=0;i<7;i++){
    const x=(i-3)*.091;
    const hem=mesh('Ragged coat hem',body,[x,-.11-(i%3)*.021,.19],[.086,.14+(i%2)*.03,.045],i%3?m.coat:m.shoulder);
    hem.rotation.z=(i%2?1:-1)*.035;
  }
  mesh('Front shirt collar',body,[0,.72,.22],[.34,.21,.075],m.shirt);
  for(const side of [-1,1]){
    const collar=mesh('Worn collar fold',body,[side*.135,.72,.268],[.10,.24,.045],m.shirt);
    collar.rotation.z=side*.33;
    const lapel=mesh('Coat lapel',body,[side*.085,.48,.282],[.075,.43,.04],m.shoulder);
    lapel.rotation.z=-side*.09;
  }
  mesh('Coat button seam',body,[.015,.29,.294],[.018,.68,.015],m.seam,'box',false);
  for(let i=0;i<4;i++)mesh('Old brass button',body,[.021+(i%2)*.009,.56-i*.16,.312],[.033,.033,.015],m.metal,'sphere',false);
  for(let i=0;i<14;i++){
    const x=Math.sin(i*2.4)*.27,y=.04+(i%6)*.11;
    const patch=mesh('Chalk worn fabric',body,[x,y,.278],[.012+(i%3)*.009,.022+(i%4)*.008,.008],m.wear,'box',false);
    patch.rotation.z=Math.sin(i)*.55;
  }
  mesh('Left coat pocket',body,[-.225,.24,.272],[.19,.18,.035],m.shoulder);
  mesh('Right coat pocket',body,[.225,.20,.272],[.19,.18,.035],m.shoulder);
  let badgeMaterial=m.badge;
  if(typeof document!=='undefined'){
    const canvas=document.createElement('canvas');canvas.width=128;canvas.height=384;
    const ctx=canvas.getContext('2d');ctx.fillStyle='#d3c8a7';ctx.fillRect(0,0,128,384);
    ctx.strokeStyle='#776f53';ctx.lineWidth=5;ctx.strokeRect(5,5,118,374);
    ctx.fillStyle='#373b30';ctx.textAlign='center';ctx.textBaseline='middle';ctx.font='bold 76px School, sans-serif';
    ['당','직','자'].forEach((c,i)=>ctx.fillText(c,64,79+i*108));
    const texture=new T.CanvasTexture(canvas);texture.colorSpace=T.SRGBColorSpace;
    badgeMaterial=material('#ffffff',{map:texture});
  }
  const badge=mesh('당직자 vertical staff badge',body,[-.235,.53,.311],[.125,.31,.014],badgeMaterial,'box',false);
  badge.rotation.z=-.07;
  mesh('Badge clip',body,[-.224,.706,.313],[.034,.049,.024],m.metal,'box',false);

  const head=group('Unnaturally tilted head',body,[.015,.91,.20]);
  // The face points slightly upwards so the eyes remain readable at 57 degrees.
  head.rotation.set(-.28,.02,-.30);
  mesh('Pale neck',head,[0,-.095,-.035],[.17,.29,.19],m.skinShade,'cylinder');
  mesh('Waxy skull',head,[0,.055,0],[.365,.445,.33],m.skin,'sphere');
  mesh('Long narrow jaw',head,[.005,-.075,.035],[.25,.225,.25],m.skin,'sphere');
  mesh('Sparse flattened hair',head,[0,.231,-.038],[.31,.085,.245],m.hair,'sphere');
  for(let i=0;i<5;i++){
    const hair=mesh('Sparse black hair strand',head,[(i-2)*.041,.213,.095],[.009,.091,.013],m.hair,'box',false);
    hair.rotation.z=(i-2)*.11;hair.rotation.x=-.25;
  }
  for(const side of [-1,1]){
    const socket=mesh('Deep eye socket',head,[side*.081,.056+(side<0?.008:0),.146],[.102,.061,.037],m.socket,'sphere',false);
    socket.rotation.z=side*.1;
    mesh('Tiny unfocused pale eye',head,[side*.081+(side>0?.009:0),.055,.165],[.021,.024,.011],m.eye,'sphere',false);
    mesh('Off axis pupil',head,[side*.081+(side>0?.014:-.002),.055,.171],[.009,.014,.007],m.socket,'sphere',false);
    const cheek=mesh('Sunken cheek line',head,[side*.105,-.022,.139],[.026,.105,.014],m.skinShade,'box',false);
    cheek.rotation.z=side*-.22;
    const brow=mesh('Taut brow',head,[side*.085,.102,.141],[.112,.023,.024],m.skinShade,'box',false);
    brow.rotation.z=side*-.13;
  }
  mesh('Sharp pale nose',head,[0,.014,.178],[.047,.098,.056],m.skin,'sphere');
  const mouth=mesh('Unreadable mouth slit',head,[.002,-.087,.151],[.133,.014,.018],m.socket,'box',false);mouth.rotation.z=.075;
  mesh('Tight lower lip',head,[.004,-.106,.146],[.116,.014,.018],m.skinShade,'box',false);

  const arms=[];
  for(const side of [-1,1]){
    const arm=group(side<0?'Left long arm':'Right long arm',body,[side*.405,.695+(side<0?.035:-.025),.035]);
    arm.rotation.z=side*.065;
    mesh('Heavy coat sleeve',arm,[side*.015,-.205,0],[.25,.43,.28],m.coat,'taper');
    mesh('Frayed sleeve cuff',arm,[side*.015,-.411,.005],[.24,.065,.25],m.shirt,'cylinder');
    const forearm=group('Exposed long forearm',arm,[side*.015,-.425,.004]);
    mesh('Pale heavy forearm',forearm,[0,-.218,.015],[.175,.45,.19],m.skin,'taper');
    mesh('Wrist',forearm,[0,-.432,.035],[.13,.13,.13],m.skin,'cylinder');
    const hand=group('Oversized hand',forearm,[0,-.464,.041]);
    hand.rotation.x=side>0?-.15:.06;
    mesh('Pale palm',hand,[0,-.08,0],[.18,.22,.105],m.skin,'sphere');
    for(let finger=0;finger<4;finger++){
      const fx=(finger-1.5)*.042,length=[.17,.22,.205,.16][finger];
      const first=mesh('Long separated finger',hand,[fx,-.188-length*.25,.026],[.030,length*.60,.040],m.skin,'cylinder');
      first.rotation.z=(finger-1.5)*-.07;
      const tip=mesh('Crooked fingertip',hand,[fx*1.1,-.195-length*.72,.055],[.027,length*.48,.035],m.skin,'cylinder');
      tip.rotation.x=-.38;tip.rotation.z=(finger-1.5)*-.10;
    }
    const thumb=mesh('Widely spaced thumb',hand,[-side*.116,-.114,.049],[.045,.14,.051],m.skin,'cylinder');
    thumb.rotation.z=-side*.62;thumb.rotation.x=-.18;
    for(let i=0;i<3;i++){
      const chalk=mesh('Chalk mark on forearm',forearm,[side*.062,-.10-i*.105,.090],[.013,.038,.007],m.skinShade,'box',false);
      chalk.rotation.z=side*.22;
    }
    arms.push({arm,forearm,hand,side});
  }

  const legs=[];
  for(const side of [-1,1]){
    const leg=group(side<0?'Left stalking leg':'Right stalking leg',visual,[side*.19,1.15,-.045]);
    mesh('Heavy trouser thigh',leg,[0,-.245,0],[.28,.50,.30],m.trousers,'taper');
    const knee=group('Bent knee',leg,[0,-.50,0]);
    mesh('Trouser shin',knee,[0,-.225,0],[.245,.46,.255],m.trousers,'taper');
    mesh('Scuffed large shoe',knee,[0,-.535,.085],[.29,.20,.47],m.shoe,'sphere');
    mesh('Shoe flat sole',knee,[0,-.625,.09],[.29,.045,.43],m.sole);
    mesh('Old shoe toe cap',knee,[0,-.51,.265],[.255,.11,.12],m.shoe,'sphere');
    legs.push({leg,knee,side});
  }

  let yaw=0,clock=0;
  function update(state={},dt=0){
    const safeDt=Math.max(0,Math.min(.1,Number.isFinite(dt)?dt:0));clock+=safeDt;
    const moving=Math.min(1,Math.max(0,state.speed||0)/2.6),chasing=state.state==='chase';
    const phase=Number.isFinite(state.phase)?state.phase:clock*3;
    if(state.direction){
      const {x=0,y=-1}=state.direction;
      if(Math.hypot(x,y)>.001){
        const target=Math.atan2(x,-y);
        yaw+=Math.atan2(Math.sin(target-yaw),Math.cos(target-yaw))*(1-Math.exp(-9*safeDt));
      }
    }
    root.rotation.y=yaw;
    const stride=Math.sin(phase)*(chasing?.43:.29)*moving;
    for(const item of legs){
      item.leg.rotation.x=item.side*stride;
      item.knee.rotation.x=Math.max(0,item.side*stride)*.9;
    }
    for(const item of arms){
      item.arm.rotation.x=-item.side*stride*.62-(chasing?.12:0);
      item.arm.rotation.z=item.side*.065+Math.cos(phase)*.018*moving;
      item.forearm.rotation.x=-.07+(item.side>0?-.10:0)+Math.sin(phase*.5+item.side)*.028;
      item.hand.rotation.x=(item.side>0?-.15:.06)+Math.sin(phase*.5+item.side)*.025;
    }
    const breath=Math.sin(clock*1.7)*.006;
    visual.position.y=Math.abs(Math.sin(phase))*.025*moving+breath;
    body.rotation.x=.10+(chasing?.06:0);
    body.rotation.z=Math.sin(phase)*.025*moving;
    head.rotation.z=-.30+Math.sin(clock*.63)*.012;
    head.rotation.x=-.28+Math.sin(clock*.87)*.009;
  }
  function inspect(){
    root.updateMatrixWorld(true);
    return {name:'당직자',kind:'original-procedural-caretaker',targetHeight:2.4,
      shoulderWidth:.87,meshCount,animatedLimbs:4,hasSeparateFingers:true,
      hasStaffBadge:true,geometrySource:'SchoolEscape original',
      localBounds:new T.Box3().setFromObject(visual).getSize(new T.Vector3()).toArray()};
  }
  update({},0);
  return {root,update,inspect};
}
