// Original procedural placeholders, authored only for SchoolEscape.
export function createSubjectVisuals(T,events,pos){
 const root=new T.Group();root.name='음악가와 쉐도어';
 const material=(c,extra={})=>new T.MeshStandardMaterial({color:c,roughness:.96,...extra});
 const skin=material('#b3b29b'),black=material('#162229'),cloth=material('#3e4544'),ivory=material('#d2c9a7'),shadow=material('#070c11');
 function part(parent,name,p,s,m,sphere=false){const o=new T.Mesh(sphere?new T.SphereGeometry(.5,16,12):new T.BoxGeometry(1,1,1),m);o.name=name;o.position.set(...p);o.scale.set(...s);o.castShadow=true;parent.add(o);return o}
 const musician=new T.Group();musician.name='거두 음악가 · 뒤틀린 지휘자';root.add(musician);
 part(musician,'낡은 연주복',[0,1.1,0],[.65,1.35,.42],cloth);
 const head=part(musician,'비정상적으로 거대한 머리',[0,2.65,.04],[1.85,1.95,1.45],skin,true);
 part(musician,'비어 있는 눈 왼쪽',[-.38,2.85,.64],[.38,.52,.15],black,true);part(musician,'비어 있는 눈 오른쪽',[.38,2.82,.64],[.28,.48,.15],black,true);
 part(musician,'건반 같은 입',[0,2.15,.69],[.96,.27,.12],black);
 for(let n=0;n<9;n++)part(musician,'불규칙한 건반 이빨',[(n-4)*.10,2.15+(n%2)*.035,.77],[.07,.18-(n%3)*.02,.04],ivory);
 for(let n=0;n<6;n++)part(musician,'두피의 갈라짐',[(n-2.5)*.19,3.25,.55],[.025,.4+(n%2)*.16,.03],cloth);
 const limbs=[];for(const side of [-1,1]){limbs.push(part(musician,'길게 늘어진 연주자의 팔',[side*.55,.95,.12],[.16,1.7,.18],cloth));limbs.push(part(musician,'뒤틀린 구두',[side*.20,.23,.12],[.23,.46,.4],black));for(let n=0;n<4;n++)part(musician,'긴 손가락',[side*.55+(n-1.5)*.055,.12,.14],[.04,.5,.055],skin)}
 part(musician,'지휘봉',[.63,.55,.35],[.035,1.5,.035],ivory);
 const shades=[0,1].map(n=>{const r=new T.Group();r.name='쉐도어 '+(n+1);root.add(r);part(r,'그림자 몸통',[0,1.15,0],[.48,1.8,.3],shadow,true);part(r,'불빛 없는 얼굴',[0,2.18,.06],[.4,.55,.36],shadow,true);for(const side of [-1,1]){part(r,'늘어진 팔',[side*.33,.95,0],[.11,1.7,.13],shadow);part(r,'가느다란 다리',[side*.14,.35,0],[.12,.7,.13],shadow)}return r});
 function update(){const m=events.music.monster;musician.visible=events.monsters.includes(m);if(m&&musician.visible){musician.position.copy(pos(m.x,m.y));musician.rotation.y=Math.atan2(m.direction.x,-m.direction.y);head.rotation.z=Math.sin(events.music.elapsed*2)*.09;limbs.forEach((l,n)=>l.rotation.x=Math.sin(m.phase+n%2*Math.PI)*.25)}shades.forEach((r,n)=>{const m=events.science.monsters[n];r.visible=!!m&&events.monsters.includes(m);if(m){r.position.copy(pos(m.x,m.y));r.rotation.y=Math.atan2(m.direction.x,-m.direction.y)}})}
 return {root,musician,shades,update};
}
