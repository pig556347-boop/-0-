import bpy,math,json
from mathutils import Vector,Quaternion,Matrix
from pathlib import Path
R=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceArt/Characters/Player.blend'))
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
 yaw=360*ease(0,.45,p);yq=Quaternion(Vector((0,0,1)),math.radians(yaw));rig.rotation_euler=(0,0,math.radians(yaw))
 ext=ease(.36,.45,p)*(1-ease(.60,.76,p));lift=ease(.18,.38,p)*(1-ease(.76,1,p))
 hip=bones['Hips'];inv=hip.bone.matrix_local.to_quaternion().inverted();hip.rotation_quaternion=Quaternion(inv@Vector((1,0,0)),math.radians(-14*ext))@Quaternion(inv@Vector((0,-1,0)),math.radians(-30*ext))
 for side in ['Left','Right']:
  for n,deg in [('UpperArm',-55),('Forearm',-110)]:
   b=bones[side+n];b.rotation_quaternion=Quaternion(b.bone.matrix_local.to_quaternion().inverted()@Vector((1,0,0)),math.radians(deg))
 bpy.context.view_layer.update();solve('Left',home['Left'],yq@Vector((0,-1,0)),yq@basis)
 folded=actor+Vector((-.12,-.13,.20));sweep=-.22+.16*ease(.45,.60,p);high=actor+Vector((sweep,-.27,.70));target=home['Right'].lerp(folded.lerp(high,ext),lift)
 solve('Right',target,Vector((0,-1,0)))
 return yaw
sc.render.engine='CYCLES';sc.cycles.samples=8;sc.cycles.use_denoising=False;sc.render.resolution_x=600;sc.render.resolution_y=640;sc.render.resolution_percentage=100;sc.world.color=(.4,.4,.4)
bpy.ops.object.camera_add(location=(3,-5,2));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.9))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.5;sc.camera=cam
bpy.ops.object.light_add(type='AREA',location=(-2,-3,4));bpy.context.object.data.energy=450;bpy.context.object.data.size=5
report=[]
for i in range(21):
 p=i/20;y=pose(p);dep=bpy.context.evaluated_depsgraph_get();e=obj.evaluated_get(dep);me=e.to_mesh();verts=[obj.matrix_world@v.co for v in me.vertices];stretch=0
 for edge in obj.data.edges:
  a,b=edge.vertices;rest=(obj.data.vertices[a].co-obj.data.vertices[b].co).length
  if rest>.008:stretch=max(stretch,(verts[a]-verts[b]).length/rest)
 err=(pos('LeftFoot')-home['Left']).length
 report.append(dict(phase=p,yaw=y,support_error=round(err,4),right_ankle_height=round(pos('RightFoot').z,4),stretch=round(stretch,3),min_z=round(min(v.z for v in verts),3)))
 assert err<.06,report[-1]
 assert stretch<4,report[-1]
 assert min(v.z for v in verts)>-.06,report[-1]
 assert all(math.isfinite(c) for v in verts for c in v)
 assert max(v.length for v in verts)<3
 e.to_mesh_clear()
 if i in [0,6,9,12,16,20]:sc.render.filepath=str(R/'Docs/Motion'/('Hook_phase_%02d.png'%i));bpy.ops.render.render(write_still=True)
(R/'Docs/Motion/hook-skin-audit.json').write_text(json.dumps(report,indent=2));print('HOOK_AUDIT_PASS',len(report),'maxstretch',max(x['stretch'] for x in report),'maxsupport',max(x['support_error'] for x in report),flush=True)
