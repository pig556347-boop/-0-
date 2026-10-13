"""Normalize supplied matching Mixamo rigs and bake source motion, preserving skin/UV.
Usage: blender -b --python-exit-code 1 --python Tools/Motion/import_mixamo.py
Original uploads stay outside the checkout. No humanoid approximations or combat edits.
"""
import bpy,json,math,pathlib
from mathutils import Vector,Matrix
ROOT=pathlib.Path(__file__).resolve().parents[2]
SOURCE=pathlib.Path('/workspace/asset-import/motions')
OUT=ROOT/'Assets/Resources/Motions'
REPORT=ROOT/'Docs/Motion'
OUT.mkdir(parents=True,exist_ok=True);REPORT.mkdir(parents=True,exist_ok=True)
records=[];poses={};template=None
for path in sorted(SOURCE.glob('*.fbx')):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(path))
 arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
 mesh=next(o for o in bpy.data.objects if o.type=='MESH')
 act=arm.animation_data.action
 names=[b.name for b in arm.data.bones]
 verts=[mesh.matrix_world@v.co for v in mesh.data.vertices]
 lo=min(v.z for v in verts);height=max(v.z for v in verts)-lo;k=1.85/height
 name=path.stem.split('@',1)[1].replace(' ','').replace('(','').replace(')','')
 rest={b.name:arm.matrix_world@b.matrix_local for b in arm.data.bones}
 if template is None:
  template={'names':names,'rest':rest,'verts':verts,'mesh':mesh.data.copy(),'weights':[[[(mesh.vertex_groups[g.group].name,g.weight) for g in v.groups] for v in mesh.data.vertices]][0],'k':k,'lo':lo,'tails':{b.name:arm.matrix_world@b.tail_local for b in arm.data.bones},'parents':{b.name:b.parent.name if b.parent else None for b in arm.data.bones}}
  # mesh datablock is copied across factory resets as serialized arrays below.
  template['faces']=[tuple(p.vertices) for p in mesh.data.polygons]
  template['uv']=[tuple(x.uv) for x in mesh.data.uv_layers[0].data]
  del template['mesh']
 elif names!=template['names']:
  raise RuntimeError('Candidate skeleton differs: '+path.name)
 frames=[];metrics=[]
 start,end=map(int,act.frame_range)
 for f in range(start,end+1):
  bpy.context.scene.frame_set(f)
  frame={}
  for b in arm.pose.bones:
   world=arm.matrix_world@b.matrix
   rot=world.to_quaternion().to_matrix().to_4x4()
   p=world.translation*k;p.z-=lo*k;rot.translation=p
   frame[b.name]=rot
  frames.append(frame)
  metrics.append({n:list(frame['mixamorig:'+n].translation) for n in ['Hips','LeftFoot','RightFoot','LeftHand','RightHand','Head']})
 poses[name]=frames
 rec={'name':name,'source':path.name,'fps':60,'frames':len(frames),'duration':(end-start)/60,'rootTravel':list(frames[-1]['mixamorig:Hips'].translation-frames[0]['mixamorig:Hips'].translation),'samples':metrics,'vertices':len(verts),'bones':len(names)}
 records.append(rec)
 print('EXTRACTED',name,len(frames),flush=True)
# Build normalized common rest rig (one skin, all source candidate actions).
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.object.armature_add(enter_editmode=True)
rig=bpy.context.object;rig.name='AuthoredRig';rig.data.edit_bones.remove(rig.data.edit_bones[0])
for name in template['names']:
 src=template['rest'][name];head=src.translation*template['k'];head.z-=template['lo']*template['k']
 tail=template['tails'][name]*template['k'];tail.z-=template['lo']*template['k']
 b=rig.data.edit_bones.new(name);b.head=head;b.tail=tail
 # Preserve full rest orientation, including roll, not just joint directions.
 b.align_roll(src.to_quaternion()@Vector((0,0,1)))
 parent=template['parents'][name]
 if parent:b.parent=rig.data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
vertices=[]
for v in template['verts']:
 p=v*template['k'];p.z-=template['lo']*template['k'];vertices.append(tuple(p))
data=bpy.data.meshes.new('Supplied Mixamo skin');data.from_pydata(vertices,[],template['faces']);data.update()
mesh=bpy.data.objects.new('PlayerSkin',data);bpy.context.collection.objects.link(mesh)
uv=data.uv_layers.new(name='UVMap')
for i,value in enumerate(template['uv']):uv.data[i].uv=value
for p in data.polygons:p.use_smooth=True
for name in template['names']:mesh.vertex_groups.new(name=name)
for i,groups in enumerate(template['weights']):
 selected=sorted(groups,key=lambda g:g[1],reverse=True)[:4];total=sum(w for _,w in selected)
 if total<.0001:raise RuntimeError('Unweighted vertex '+str(i))
 for name,w in selected:mesh.vertex_groups[name].add([i],w/total,'REPLACE')
mesh.parent=rig;mod=mesh.modifiers.new('Supplied skin weights','ARMATURE');mod.object=rig
mat=bpy.data.materials.new('PlayerSourceColor');mat.use_nodes=True
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/'Assets/Resources/Characters/Player/Player_BaseColor.png'))
mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);data.materials.append(mat)
rig.animation_data_create()
for rec in records:
 name=rec['name'];frames=poses[name]
 action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
 base=frames[0]['mixamorig:Hips'].translation.copy()
 for i,frame in enumerate(frames):
  # Remove planar root travel: Rigidbody is sole movement authority.
  offset=Vector((base.x-frame['mixamorig:Hips'].translation.x,base.y-frame['mixamorig:Hips'].translation.y,0))
  desired={n:m.copy() for n,m in frame.items()}
  for m in desired.values():m.translation+=offset
  for bone in rig.pose.bones:
   bone.rotation_mode='QUATERNION'
   rest=bone.bone.matrix_local
   if bone.parent:
    basis=rest.inverted()@bone.parent.bone.matrix_local@desired[bone.parent.name].inverted()@desired[bone.name]
   else:basis=rest.inverted()@desired[bone.name]
   bone.matrix_basis=basis
   if bone.parent and bone.location.length>.001:raise RuntimeError('Unexpected translated child '+bone.name+' '+str(bone.location))
   bone.location=bone.location if not bone.parent else Vector((0,0,0))
   bone.scale=Vector((1,1,1))
   bone.keyframe_insert('rotation_quaternion',frame=i+1,group=bone.name)
   if bone.name=='mixamorig:Hips':bone.keyframe_insert('location',frame=i+1,group=bone.name)
  # No animated bone scale or limb length; child offsets stay at common rest.
 for fc in action.fcurves:
  for point in fc.keyframe_points:point.interpolation='LINEAR'
 rig.animation_data.action=None
# Clip metrics preserve source data so choices can be reviewed and regenerated.
(REPORT/'candidate-inventory.json').write_text(json.dumps(records,ensure_ascii=False,indent=2))
bpy.context.scene.render.fps=60
rig.animation_data.action=bpy.data.actions.get('FightingIdle')
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'normalized-candidates.blend'),compress=True)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'PlayerMotions.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
print('NORMALIZED_MOTIONS',len(records),'skin',len(vertices),'bones',len(template['names']),flush=True)
