import bpy,math,json
from mathutils import Vector,Quaternion,Matrix
from pathlib import Path
R=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Characters/Deongchi.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');sc=bpy.context.scene
bones=rig.pose.bones
home={s:bones[s+'Foot'].head.copy() for s in ['Left','Right']};basis=bones['LeftFoot'].matrix.to_quaternion();actor=Vector((0,0,.95))
def ease(a,b,p):
 t=max(0,min(1,(p-a)/(b-a)));return t*t*(3-2*t)
def pos(n):return rig.matrix_world@bones[n].head

def rotate(n,q):
 b=bones[n];w=rig.matrix_world@b.matrix
 b.matrix=rig.matrix_world.inverted()@Matrix.LocRotScale(w.translation,q@w.to_quaternion(),Vector((1,1,1)))
 bpy.context.view_layer.update()
def solve(s,target,bend,footq=None):
 names=[s+'Thigh',s+'Shin',s+'Foot'];a,b,c=[pos(n) for n in names];u=(b-a).length;l=(c-b).length;delta=target-a;d=max(abs(u-l)+.001,min(u+l-.001,delta.length));direction=delta.normalized();perp=(bend-direction*bend.dot(direction)).normalized();along=(u*u+d*d-l*l)/(2*d);h=math.sqrt(max(0,u*u-along*along));k=a+direction*along+perp*h;end=a+direction*d
 rotate(names[0],(b-a).rotation_difference(k-a));b=pos(names[1]);c=pos(names[2]);rotate(names[1],(c-b).rotation_difference(end-b))
 if footq:
  w=rig.matrix_world@bones[names[2]].matrix;rotate(names[2],footq@w.to_quaternion().inverted())
 else:bones[names[2]].rotation_quaternion=Quaternion();bpy.context.view_layer.update()

def pose(p):
 rig.rotation_euler=(0,0,0)
 for b in bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0)
 blend=ease(0,.18,p)*(1-ease(.76,1,p));yaw=360*ease(0,.45,p)+20*(1-blend);yq=Quaternion(Vector((0,0,1)),math.radians(yaw));rig.rotation_euler=(0,0,math.radians(yaw))
 ext=ease(.36,.45,p)*(1-ease(.60,.76,p));lift=ease(.18,.38,p)*(1-ease(.76,1,p))
 drop=.09*(1-ext)+.025*math.sin(math.pi*ease(0,.45,p));hip=bones['Hips'];hip.location=hip.bone.matrix_local.to_quaternion().inverted()@(yq.inverted()@Vector((.025*lift,-.035,-drop)));inv=hip.bone.matrix_local.to_quaternion().inverted();hip.rotation_quaternion=Quaternion(inv@Vector((1,0,0)),math.radians(-14*ext))@Quaternion(inv@Vector((0,-1,0)),math.radians(-30*ext))
 for side in ['Left','Right']:
  for n,deg in [('UpperArm',-58),('Forearm',-100)]:
   b=bones[side+n];b.rotation_quaternion=Quaternion(b.bone.matrix_local.to_quaternion().inverted()@Vector((1,0,0)),math.radians(deg))
 for n,deg in [('Spine',6*(1-ext)),('Chest',2.1*(1-ext))]:
  b=bones[n];b.rotation_quaternion=Quaternion(b.bone.matrix_local.to_quaternion().inverted()@Vector((1,0,0)),math.radians(deg))
 for side in ['Left','Right']:
  sign=1 if side=='Left' else -1;names=[side+'UpperArm',side+'Forearm',side+'Hand'];a,b,c=[pos(n) for n in names];target=pos('Head')+yq@Vector((sign*.14,-.17,0));u=(b-a).length;l=(c-b).length;delta=target-a;d=max(abs(u-l)+.001,min(u+l-.001,delta.length));direction=delta.normalized();bend=yq@Vector((sign*.35,-.1,-1));pole=(bend-direction*bend.dot(direction)).normalized();along=(u*u+d*d-l*l)/(2*d);k=a+direction*along+pole*math.sqrt(max(0,u*u-along*along));rotate(names[0],(b-a).rotation_difference(k-a));b=pos(names[1]);c=pos(names[2]);rotate(names[1],(c-b).rotation_difference(a+direction*d-b))
 for side in ['Left','Right']:
  b=bones[side+'Hand'];long=(bones[side+'Hand'].head-bones[side+'Forearm'].head).normalized();b.rotation_quaternion=Quaternion(b.bone.matrix_local.to_quaternion().inverted()@long,math.radians(40 if side=='Left' else -40))
 bpy.context.view_layer.update();left=home['Left'].copy();left.x*=2;left.y-=.20
 right=home['Right'].copy();right.x*=2;right.y+=.17
 solve('Left',left,Quaternion(Vector((0,0,1)),math.radians(yaw-20*(1-blend)))@Vector((0,-1,0)),Quaternion(Vector((0,0,1)),math.radians(360*ease(0,.45,p)+12))@basis)
 folded=actor+Vector((-.12,-.13,.20));sweep=-.22+.16*ease(.45,.60,p);high=actor+Vector((sweep,-.33,.70));target=right.lerp(folded.lerp(high,ext),lift)
 solve('Right',target,Vector((0,-1,0)))
 w=rig.matrix_world@bones['RightFoot'].matrix;neutral=w.to_quaternion();flat=Quaternion(Vector((0,0,1)),math.radians(-12))@basis
 rotate('RightFoot',flat.slerp(neutral,lift)@w.to_quaternion().inverted())
 return yaw
sc.render.engine='CYCLES';sc.cycles.samples=8;sc.cycles.use_denoising=False;sc.render.resolution_x=600;sc.render.resolution_y=640;sc.render.resolution_percentage=100;sc.world.color=(.4,.4,.4)
bpy.ops.object.camera_add(location=(3,-5,2));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.9))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.5;sc.camera=cam
bpy.ops.object.light_add(type='AREA',location=(-2,-3,4));bpy.context.object.data.energy=450;bpy.context.object.data.size=5

cam.location=(4,-1,2);cam.rotation_euler=(Vector((0,0,.9))-cam.location).to_track_quat('-Z','Y').to_euler()
handaxes={side:bones[side+'Hand'].matrix.to_quaternion().inverted()@(bones[side+'Hand'].head-bones[side+'Forearm'].head).normalized() for side in ['Left','Right']}
footbases={side:(rig.matrix_world@bones[side+'Foot'].matrix).to_quaternion() for side in ['Left','Right']}
def setbone(n,deg):
 b=bones[n];b.rotation_quaternion=Quaternion(b.bone.matrix_local.to_quaternion().inverted()@Vector((1,0,0)),math.radians(deg));bpy.context.view_layer.update()
def arm(side,target):
 sign=1 if side=='Left' else -1;names=[side+'UpperArm',side+'Forearm',side+'Hand'];a,b,c=[pos(n) for n in names];u=(b-a).length;l=(c-b).length;delta=target-a;d=max(abs(u-l)+.001,min(u+l-.001,delta.length));direction=delta.normalized();bend=rig.matrix_world.to_quaternion()@Vector((sign*.35,-.1,-1));pole=(bend-direction*bend.dot(direction)).normalized();along=(u*u+d*d-l*l)/(2*d);k=a+direction*along+pole*math.sqrt(max(0,u*u-along*along));rotate(names[0],(b-a).rotation_difference(k-a));b=pos(names[1]);c=pos(names[2]);rotate(names[1],(c-b).rotation_difference(a+direction*d-b))
def guard():
 q=rig.matrix_world.to_quaternion()
 for side in ['Left','Right']:
  sign=1 if side=='Left' else -1;setbone(side+'UpperArm',-58);setbone(side+'Forearm',-100);arm(side,pos('Head')+q@Vector((sign*.22,-.22,0)))
  b=bones[side+'Hand'];b.rotation_quaternion=Quaternion();bpy.context.view_layer.update();long=(b.head-bones[side+'Forearm'].head).normalized();b.rotation_quaternion=Quaternion(handaxes[side],math.radians(sign*40))
def readyfoot(side,m=0):
 v=home[side].copy();v.x*=2*(1-m)+1.55*m;v.y+=(-.20 if side=='Left' else .17)*(1-m);return v

def punch(p,right=False):
 pose(0);side='Right' if right else 'Left';sign=-1 if right else 1;ext=ease(.26,.45,p)*(1-ease(.52,.88,p));ant=ease(0,.25,p)*(1-ext)*(1-ease(.75,1,p));goal=45 if right else -8;yaw=20+(goal-20)*ext;yq=Quaternion(Vector((0,0,1)),math.radians(yaw));rig.rotation_euler=(0,0,math.radians(yaw));hip=bones['Hips'];hip.location=hip.bone.matrix_local.to_quaternion().inverted()@(yq.inverted()@Vector((-sign*.025*ext,-.035-.055*ext+.015*ant,-.09)));bpy.context.view_layer.update();setbone('Spine',6+4*ext);setbone('Chest',2.1+2*ext);rotate('Chest',Quaternion(Vector((0,0,1)),math.radians(-sign*8*ext)))
 for leg in ['Left','Right']:
  pivot=(goal-20)*ext if leg==side else 0;footq=Quaternion(Vector((0,0,1)),math.radians(pivot+(12 if leg=='Left' else -12)))@footbases[leg];solve(leg,readyfoot(leg),Vector((0,-1,0)),footq)
 guard();hand=pos(side+'Hand');target=hand+Vector((0,.025*ant,0));shoulder=pos(side+'UpperArm');elbow=pos(side+'Forearm');reach=(elbow-shoulder).length+(hand-elbow).length;strike=shoulder+Vector((-sign*.03,-reach*.98,.09));arm(side,target.lerp(strike,ext))

def walk(p):
 pose(0);rig.rotation_euler=(0,0,0);hip=bones['Hips'];hip.location=hip.bone.matrix_local.to_quaternion().inverted()@Vector((.018*math.sin(p),0,-.17-.012*math.cos(2*p)));bpy.context.view_layer.update();setbone('Spine',10);setbone('Chest',3.5);guard()
 for i,side in enumerate(['Left','Right']):
  phase=((p+i*math.pi)%(2*math.pi))/(2*math.pi);plant=phase<.55;t=phase/.55 if plant else (phase-.55)/.45;f=.48-.96*t if plant else -.48+.96*(t*t*(3-2*t));v=readyfoot(side,1);v.y-=f;v.z+=0 if plant else .075*math.sin(math.pi*t);q=Quaternion(Vector((0,0,1)),math.radians(12 if side=='Left' else -12))@footbases[side];solve(side,v,Vector((0,-1,0)),q)
report=[]
for mode in ['left','right','walk']:
 for i in range(21):
  p=i/20
  if mode=='walk':walk(p*2*math.pi)
  else:punch(p,mode=='right')
  dep=bpy.context.evaluated_depsgraph_get();e=obj.evaluated_get(dep);me=e.to_mesh();verts=[obj.matrix_world@v.co for v in me.vertices];stretch=0
  for edge in obj.data.edges:
   a,b=edge.vertices;rest=(obj.data.vertices[a].co-obj.data.vertices[b].co).length
   if rest>.008:stretch=max(stretch,(verts[a]-verts[b]).length/rest)
  report.append(dict(mode=mode,phase=p,stretch=round(stretch,3),min_z=round(min(v.z for v in verts),4)))
  assert all(math.isfinite(c) for v in verts for c in v),report[-1]
  assert max(v.length for v in verts)<3,report[-1]
  assert min(v.z for v in verts)>-.07,report[-1]
  e.to_mesh_clear()
  if (mode in ['left','right'] and i in [0,9,20]) or (mode=='walk' and i==6):
   sc.render.filepath=str(R/'Docs/Motion'/('Deongchi_%s_%02d.png'%(mode,i)));bpy.ops.render.render(write_still=True)
(R/'Docs/Motion/deongchi-skin-audit.json').write_text(json.dumps(report,indent=2));print('DEONGCHI_AUDIT_PASS',len(report),'maxstretch',max(x['stretch'] for x in report),flush=True)
