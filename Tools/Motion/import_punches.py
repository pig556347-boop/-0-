"""Bake the three supplied FBX clips into model-space deltas; keep existing motion data intact.
Usage: blender -b --python-exit-code 1 --python Tools/Motion/import_punches.py -- LEFT_FBX RIGHT_FBX BLOCK_FBX
"""
import bpy,sys,pathlib,struct,json
from mathutils import Matrix,Vector
root=pathlib.Path(__file__).resolve().parents[2]
args=sys.argv[sys.argv.index('--')+1:]
assert len(args)==3,'Provide left punch, right punch and inward block FBX paths'
with open(root/'Assets/Resources/Motions/PlayerPoseData.bytes','rb') as f:
 f.read(8);count=struct.unpack('<i',f.read(4))[0];names=[]
 for _ in range(count):names.append(f.read(struct.unpack('<i',f.read(4))[0]).decode())
conversion=Matrix(((-1,0,0),(0,0,1),(0,-1,0)))
clips=[];report=[]
for name,path in zip(['PunchLeft','PunchRight','InwardBlock'],args):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=path)
 arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
 rest={n:arm.matrix_world@arm.data.bones[n].matrix_local for n in names}
 scale=.95/max(.000001,rest['mixamorig:Hips'].translation.z)
 start,end=map(int,arm.animation_data.action.frame_range);frames=[];extension=[]
 for f in range(start,end+1):
  bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
  delta=conversion@((arm.matrix_world@arm.pose.bones['mixamorig:Hips'].matrix).translation-rest['mixamorig:Hips'].translation)*scale
  delta.x=delta.z=0 # Game physics owns planar motion.
  rotations=[]
  for n in names:
   q=(arm.matrix_world@arm.pose.bones[n].matrix).to_quaternion()@rest[n].to_quaternion().inverted()
   q=(conversion@q.to_matrix()@conversion.inverted()).to_quaternion();rotations.extend([q.x,q.y,q.z,q.w])
  frames.append([*delta,*rotations])
  if name!='InwardBlock':
   hand='mixamorig:'+('LeftHand' if name=='PunchLeft' else 'RightHand')
   extension.append(-(arm.matrix_world@arm.pose.bones[hand].matrix).translation.y*scale)
 clips.append((name,frames));report.append({'clip':name,'source':pathlib.Path(path).name,'fps':60,'frames':len(frames),'impactFrame':extension.index(max(extension)) if extension else None})
with open(root/'Assets/Resources/Motions/PunchPoseData.bytes','wb') as f:
 def integer(n):f.write(struct.pack('<i',n))
 def string(s):b=s.encode();integer(len(b));f.write(b)
 f.write(b'KMP1');integer(60);integer(len(names))
 for n in names:string(n)
 integer(len(clips))
 for name,frames in clips:
  string(name);integer(len(frames))
  for frame in frames:f.write(struct.pack('<'+str(len(frame))+'f',*frame))
(root/'Docs/Motion/punch-import.json').write_text(json.dumps(report,indent=2))
print('PUNCH_IMPORT',json.dumps(report),flush=True)
