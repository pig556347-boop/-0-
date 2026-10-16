"""Audit actual deformed student skin after baking; run via Blender with -- Minion3 (or Minion4)."""
import bpy,struct,pathlib,math,json,sys
from mathutils import Matrix,Vector,Quaternion
root=pathlib.Path(__file__).resolve().parents[2];kind=sys.argv[sys.argv.index('--')+1]
bpy.ops.wm.open_mainfile(filepath='/workspace/asset-import/'+kind+'_GameRig.blend')
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH')
rest={b.name:b.matrix_local.to_quaternion() for b in rig.data.bones};conversion=Matrix(((-1,0,0),(0,0,1),(0,-1,0)))
f=open(root/('Assets/Resources/Characters/'+kind+'/'+kind+'PoseData.bytes'),'rb')
def integer():return struct.unpack('<i',f.read(4))[0]
def string():return f.read(integer()).decode()
assert f.read(4)==b'KMP1';fps=integer();names=[string() for _ in range(integer())];clips={};width=3+len(names)*4
for _ in range(integer()):
 name=string();clips[name]=[struct.unpack('<'+str(width)+'f',f.read(width*4)) for i in range(integer())]
assert not f.read(1)
def sample(values):
 wanted={}
 for j,n in enumerate(names):
  x,y,z,w=values[3+j*4:7+j*4];q=Quaternion((w,x,y,z));delta=(conversion.inverted()@q.to_matrix()@conversion).to_quaternion();wanted[n]=delta@rest[n]
 for b in rig.pose.bones:
  b.rotation_mode='QUATERNION';b.location=Vector((0,0,0));b.scale=Vector((1,1,1))
  b.rotation_quaternion=rest[b.name].inverted()@(rest[b.parent.name]@wanted[b.parent.name].inverted() if b.parent else Quaternion())@wanted[b.name]
  if b.name=='mixamorig:Hips':b.location=rest[b.name].inverted()@(conversion.inverted()@Vector(values[:3]))
 bpy.context.view_layer.update()
report=[]
for review,count in [(1,11),(2,21)]:
 for name,frames in clips.items():
  extents=[]
  for phase in range(count):
   sample(frames[round((len(frames)-1)*phase/(count-1))]);vs=[mesh.matrix_world@v.co for v in mesh.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]
   assert all(all(math.isfinite(x) and abs(x)<4 for x in v) for v in vs)
   lo=[min(v[i] for v in vs) for i in range(3)];hi=[max(v[i] for v in vs) for i in range(3)];extents.append([hi[i]-lo[i] for i in range(3)])
  assert max(max(e) for e in extents)<2.5
  report.append({'review':review,'clip':name,'samples':count,'maxExtent':max(max(e) for e in extents),'passed':True})
impacts={r['clip']:r['impactFrame'] for r in json.loads((root/('Docs/'+kind+'/import.json')).read_text())}
elbows={}
for clip,side in [('PunchLeft','Left'),('PunchRight','Right')]:
 sample(clips[clip][impacts[clip]]);a=rig.pose.bones['mixamorig:'+side+'Arm'].head;b=rig.pose.bones['mixamorig:'+side+'ForeArm'].head;c=rig.pose.bones['mixamorig:'+side+'Hand'].head;elbows[clip]=(a-b).angle(c-b)*180/math.pi
bpy.ops.object.camera_add(location=(3,-5,2.3));camera=bpy.context.object;camera.rotation_euler=(Vector((0,-.1,.9))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=2.7
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.color_type='TEXTURE';scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
for name,frames in clips.items():
 sample(frames[len(frames)//4 if name=='Walking' else impacts[name]]);scene.render.filepath=str(root/('Docs/'+kind+'/source-'+name+'.png'));bpy.ops.render.render(write_still=True)
(root/('Docs/'+kind+'/offline-audit.json')).write_text(json.dumps({'reviews':report,'elbows':elbows},indent=2));print('OFFLINE_AUDIT',json.dumps({'reviews':report,'elbows':elbows}),flush=True)
