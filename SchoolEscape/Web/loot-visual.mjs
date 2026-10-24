import {ITEM_CATALOG} from './items.mjs';

// Small original pickup meshes; no physics body or visibility into other maps.
export function createLootVisuals(T,items,pos) {
 const root=new T.Group();root.name='SchoolEscape · current-space pickups';
 const entries=new Map(),labelMaterials=new Map(),materials=new Map();
 const geometries={box:new T.BoxGeometry(1,1,1),sphere:new T.SphereGeometry(.5,8,6),
  cylinder:new T.CylinderGeometry(.5,.5,1,10),ring:new T.TorusGeometry(.05,.013,5,16)};
 const material=(color,extras={})=>{
  const key=color+JSON.stringify(extras);if(!materials.has(key))materials.set(key,new T.MeshStandardMaterial({color,roughness:.78,...extras}));return materials.get(key);
 };
 const gold=material('#bca46c',{metalness:.45,roughness:.62}),dark=material('#24383c'),paper=material('#d4ccb0'),red=material('#a35e4b'),green=material('#7f9a79');
 function mesh(name,parent,p,s,mat,shape='box'){
  const m=new T.Mesh(geometries[shape],mat);m.name=name;m.position.set(...p);m.scale.set(...s);m.castShadow=true;m.receiveShadow=true;parent.add(m);return m;
 }
 function ring(parent,p,scale=1){const m=new T.Mesh(geometries.ring,gold);m.name='Key bow';m.position.set(...p);m.rotation.x=-Math.PI/2;m.scale.setScalar(scale);m.castShadow=true;parent.add(m);return m}
 function keyLabel(letter){
  if(!labelMaterials.has(letter)){
   const c=document.createElement('canvas');c.width=128;c.height=128;const ctx=c.getContext('2d');
   ctx.fillStyle='#12232ade';ctx.fillRect(14,14,100,100);ctx.strokeStyle='#bcaa77';ctx.lineWidth=4;ctx.strokeRect(16,16,96,96);
   ctx.font='bold 76px monospace';ctx.textAlign='center';ctx.textBaseline='middle';ctx.fillStyle='#e6d29b';ctx.fillText(letter,64,68);
   const map=new T.CanvasTexture(c);map.colorSpace=T.SRGBColorSpace;
   labelMaterials.set(letter,new T.SpriteMaterial({map,transparent:true,depthTest:true,opacity:.93}));
  }
  const sprite=new T.Sprite(labelMaterials.get(letter));sprite.name='Key '+letter+' marker';sprite.position.set(0,.36,0);sprite.scale.set(.25,.25,1);return sprite;
 }
 function triangle(parent,points,mat,name){
  const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(points.flat(),3));g.computeVertexNormals();
  const m=new T.Mesh(g,mat);m.name=name;m.userData.ownedGeometry=true;parent.add(m);return m;
 }
 function build(item){
  const group=new T.Group();group.name='Pickup · '+item.itemId;group.userData.itemUid=item.uid;group.userData.itemId=item.itemId;
  const model=new T.Group();group.add(model);
  if(item.itemId.startsWith('key-')){
   ring(model,[-.075,.033,0]);mesh('Key stem',model,[.035,.033,0],[.15,.026,.03],gold);
   mesh('Key tooth one',model,[.075,.033,.025],[.025,.026,.047],gold);mesh('Key tooth two',model,[.025,.033,.025],[.023,.026,.04],gold);
   model.rotation.y=-.48;group.add(keyLabel(item.itemId.slice(-1).toUpperCase()));
  }else if(item.itemId==='vitamin-water'){
   mesh('Vitamin bottle',model,[0,.113,0],[.105,.18,.105],material('#6a9e9d',{roughness:.3}),'cylinder');
   mesh('Bottle label',model,[0,.106,0],[.108,.075,.108],paper,'cylinder');
   mesh('Bottle neck',model,[0,.216,0],[.063,.035,.063],material('#8eaf9f'),'cylinder');
   mesh('Bottle cap',model,[0,.24,0],[.068,.025,.068],gold,'cylinder');
   mesh('Bottle vitamin mark',model,[0,.109,.057],[.04,.045,.007],green);
  }else if(item.itemId==='chocolate-bar'){
   mesh('Chocolate wrapper',model,[0,.035,0],[.24,.055,.13],material('#8e7764'));
   mesh('Wrapper paper band',model,[.025,.066,0],[.13,.01,.135],paper);
   for(let x=0;x<2;x++)for(let z=0;z<2;z++)mesh('Chocolate exposed square',model,[-.07+x*.039,.07,-.036+z*.07],[.035,.018,.058],material('#514333'));
  }else if(item.itemId==='smartwatch'){
   mesh('Watch strap',model,[0,.029,0],[.075,.035,.27],material('#46575b'));
   mesh('Watch case',model,[0,.058,0],[.13,.055,.15],gold);mesh('Watch glass',model,[0,.09,0],[.105,.012,.12],dark);
   mesh('Watch heartbeat screen',model,[0,.099,0],[.07,.009,.012],green);
   const beat=mesh('Watch pulse line',model,[.005,.1,0],[.012,.008,.055],green);beat.rotation.y=.23;
  }else if(item.itemId==='clock-body'){
   mesh('Clock rim',model,[0,.041,0],[.235,.055,.235],gold,'cylinder');
   mesh('Clock face without hands',model,[0,.075,0],[.197,.014,.197],paper,'cylinder');
   for(let i=0;i<4;i++){const a=i*Math.PI/2,tick=mesh('Clock face mark',model,[Math.sin(a)*.077,.087,Math.cos(a)*.077],[.013,.006,.029],dark);tick.rotation.y=a;}
   mesh('Empty clock spindle',model,[0,.09,0],[.022,.012,.022],gold,'cylinder');
  }else if(item.itemId==='clock-second-hand'||item.itemId==='clock-minute-hand'){
   const second=item.itemId==='clock-second-hand',color=second?red:gold;
   mesh('Clock hand spindle',model,[0,.027,.044],[.042,.018,.042],gold,'cylinder');
   const hand=mesh('Loose clock hand',model,[0,.035,-.035],[second?.012:.025,.013,.22],color);hand.rotation.y=.38;
  }else if(item.itemId==='compass'){
   mesh('Compass rim',model,[0,.036,0],[.19,.045,.19],gold,'cylinder');mesh('Compass face',model,[0,.064,0],[.158,.012,.158],paper,'cylinder');
   triangle(model,[[-.025,.074,0],[0,.074,-.065],[.025,.074,0]],red,'Compass north needle');
   triangle(model,[[.025,.075,0],[0,.075,.065],[-.025,.075,0]],dark,'Compass south needle');
  }else mesh('Small found object',model,[0,.07,0],[.16,.12,.15],material(ITEM_CATALOG[item.itemId]?.color||'#a2a38b'));
  const halo=new T.Mesh(new T.RingGeometry(.145,.17,24),new T.MeshBasicMaterial({color:'#c4b77e',transparent:true,opacity:.46,depthWrite:false,side:T.DoubleSide}));
  halo.name='Pickup floor locator';halo.rotation.x=-Math.PI/2;halo.position.y=.012;halo.userData.ownedGeometry=true;halo.userData.ownedMaterial=true;group.add(halo);
  root.add(group);return {group,halo,itemId:item.itemId};
 }
 function remove(entry){
  root.remove(entry.group);entry.group.traverse(o=>{if(o.userData.ownedGeometry)o.geometry?.dispose();if(o.userData.ownedMaterial)o.material?.dispose()});
 }
 function update(spaceId,nearest=null){
  const live=new Set();let visible=0;
  for(const item of items.worldItems||[]){
   if(item.picked||item.hidden&&!item.revealed)continue;live.add(item.uid);
   let entry=entries.get(item.uid);
   if(entry&&entry.itemId!==item.itemId){remove(entry);entries.delete(item.uid);entry=null;}
   if(!entry){entry=build(item);entries.set(item.uid,entry);}
   const x=Number.isFinite(item.displayX)?item.displayX:item.x,y=Number.isFinite(item.displayY)?item.displayY:item.y;
   entry.group.position.copy(pos(x,y,.018));entry.group.visible=item.spaceId===spaceId;
   entry.group.userData.spaceId=item.spaceId;entry.group.userData.sourceId=item.sourceId||null;
   const selected=nearest&&(nearest.itemUid===item.uid||nearest.uid===item.uid||nearest.item?.uid===item.uid||nearest.worldItem?.uid===item.uid);
   entry.halo.material.opacity=selected?.78:.46;if(entry.group.visible)visible++;
  }
  for(const [uid,entry] of entries)if(!live.has(uid)){remove(entry);entries.delete(uid)}
  root.userData.spaceId=spaceId;root.userData.visiblePickups=visible;
 }
 function inspect(){return {activeSpaceId:root.userData.spaceId,visiblePickups:root.userData.visiblePickups||0,
  items:[...entries].map(([uid,e])=>({uid,itemId:e.itemId,spaceId:e.group.userData.spaceId,visible:e.group.visible,
   position:e.group.position.toArray(),sourceId:e.group.userData.sourceId})),externalTextures:0,originalProcedural:true};}
 return {root,update,inspect};
}
