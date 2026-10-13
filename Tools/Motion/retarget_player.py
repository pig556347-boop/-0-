"""Transfer supplied skeletal motion onto the existing textured player, never source UV guesses."""
import bpy,pathlib,math,json
from mathutils import Vector,Quaternion
ROOT=pathlib.Path(__file__).resolve().parents[2];SOURCE=pathlib.Path('/workspace/asset-import/motions')
MAP={'Hips':'Hips','Spine':'Spine','Chest':'Spine2','Neck':'Neck','Head':'Head'}
for side in ['Left','Right']:
 MAP.update({side+'Shoulder':side+'Shoulder',side+'UpperArm':side+'Arm',side+'Forearm':side+'ForeArm',side+'Hand':side+'Hand',side+'Thigh':side+'UpLeg',side+'Shin':side+'Leg',side+'Foot':side+'Foot'})
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'normalized-candidates.blend'))
src=next(o for o in bpy.data.objects if o.type=='ARMATURE')
srcRest={n:(src.matrix_world@src.data.bones['mixamorig:'+n].matrix_local).to_quaternion() for n in MAP.values()}
hipRest=(src.matrix_world@src.data.bones['mixamorig:Hips'].matrix_local).translation
sampled={}
for action in bpy.data.actions:
 src.animation_data.action=action;frames=[]
 for f in range(1,int(action.frame_range[1])+1):
  bpy.context.scene.frame_set(f)
  frames.append({'rotation':{n:(src.matrix_world@src.pose.bones['mixamorig:'+n].matrix).to_quaternion() for n in MAP.values()},'hip':src.matrix_world@src.pose.bones['mixamorig:Hips'].head})
 sampled[action.name]=frames
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SourceArt/Characters/Player.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH')
rig.animation_data_clear();rig.name='AuthoredRig'
for old,new in MAP.items():
 rig.data.bones[old].name='mixamorig:'+new
 if old in mesh.vertex_groups:mesh.vertex_groups[old].name='mixamorig:'+new
rest={b.name:(rig.matrix_world@b.matrix_local).to_quaternion() for b in rig.data.bones}
correction={}
for n in MAP.values():
 q=rest['mixamorig:'+n]
 correction[n]=(q@Vector((0,1,0))).rotation_difference(srcRest[n]@Vector((0,1,0)))@q
hip=rig.data.bones['mixamorig:Hips'].head_local.copy()
rig.animation_data_create()
for name,frames in sampled.items():
 act=bpy.data.actions.new(name);act.use_fake_user=True;rig.animation_data.action=act
 for i,frame in enumerate(frames):
  wanted={n:frame['rotation'][n]@srcRest[n].inverted()@correction[n] for n in MAP.values()}
  for b in rig.pose.bones:
   n=b.name[10:];b.rotation_mode='QUATERNION'
   if b.parent:
    local=rest[b.name].inverted()@rest[b.parent.name]@wanted[b.parent.name[10:]].inverted()@wanted[n]
   else:local=rest[b.name].inverted()@wanted[n]
   b.rotation_quaternion=local;b.location=Vector((0,0,0));b.scale=Vector((1,1,1))
   if n=='Hips':
    delta=Vector((0,0,(frame['hip'].z-hipRest.z)*.86))
    b.location=rest[b.name].inverted()@delta
    b.keyframe_insert('location',frame=i+1,group=b.name)
   b.keyframe_insert('rotation_quaternion',frame=i+1,group=b.name)
  bpy.context.view_layer.update()
 for fc in act.fcurves:
  for point in fc.keyframe_points:point.interpolation='LINEAR'
 rig.animation_data.action=None
rig.animation_data.action=bpy.data.actions.get('FightingIdle');bpy.context.scene.render.fps=60;bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'retargeted-player.blend'),compress=True)
# Export model-space world rotation deltas, independent of FBX bone axes/scale.
import struct
from mathutils import Matrix
conversion=Matrix(((-1,0,0),(0,0,1),(0,-1,0)))
names=[b.name for b in rig.data.bones]
rest_world={b.name:rig.matrix_world@b.matrix_local for b in rig.data.bones}
with open(ROOT/'Assets/Resources/Motions/PlayerPoseData.bytes','wb') as stream:
 def integer(n):stream.write(struct.pack('<i',n))
 def string(text):
  encoded=text.encode('utf8');integer(len(encoded));stream.write(encoded)
 stream.write(b'KMP1');integer(60);integer(len(names))
 for n in names:string(n)
 actions=sorted(bpy.data.actions,key=lambda a:a.name);integer(len(actions))
 for action in actions:
  rig.animation_data.action=action;string(action.name);integer(int(action.frame_range[1]))
  for frame in range(1,int(action.frame_range[1])+1):
   bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
   delta=conversion@((rig.matrix_world@rig.pose.bones['mixamorig:Hips'].matrix).translation-rest_world['mixamorig:Hips'].translation)
   stream.write(struct.pack('<3f',*delta))
   for n in names:
    pose=(rig.matrix_world@rig.pose.bones[n].matrix).to_quaternion()
    rotation=pose@rest_world[n].to_quaternion().inverted()
    q=(conversion@rotation.to_matrix()@conversion.inverted()).to_quaternion()
    stream.write(struct.pack('<4f',q.x,q.y,q.z,q.w))
rig.animation_data.action=None
for bone in rig.pose.bones:bone.matrix_basis.identity()
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Resources/Motions/PlayerMotions.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
print('RETARGET_COMPLETE',len(sampled),'character vertices',len(mesh.data.vertices),'bones',len(rig.data.bones),flush=True)
