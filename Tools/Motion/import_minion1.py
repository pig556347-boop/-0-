"""Import the four supplied, matching rigged student FBXs. Keep source skin weights and UVs.
Usage: blender -b --python-exit-code 1 --python Tools/Motion/import_minion1.py -- SOURCE_FOLDER
"""
import bpy,pathlib,sys,math,struct,json
from mathutils import Matrix,Vector
ROOT=pathlib.Path(__file__).resolve().parents[2];SOURCE=pathlib.Path(sys.argv[sys.argv.index('--')+1])
OUT=ROOT/'Assets/Resources/Characters/Minion1';OUT.mkdir(parents=True,exist_ok=True)
DOC=ROOT/'Docs/Minion1';DOC.mkdir(parents=True,exist_ok=True)
up=Matrix.Rotation(-math.pi/2,4,'X');conversion=Matrix(((-1,0,0),(0,0,1),(0,-1,0)))
samples={};report=[];template=None
for filename,name in [('Punching.fbx','PunchRight'),('Punching (1).fbx','PunchLeft'),('Mma Kick.fbx','MmaKick'),('Kicking.fbx','Kicking')]:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(SOURCE/filename))
 arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH')
 names=[b.name for b in arm.data.bones];assert len(names)==33
 if template is None:
  # These FBXs contain a Y-down bind pose, while the animated skeleton is Z-up.
  # Rebuild only the bind frame; preserve the original animation's world-space axes.
  positions=[up@(mesh.matrix_world@v.co) for v in mesh.data.vertices]
  low=min(v.z for v in positions);height=max(v.z for v in positions)-low;k=1.85/height
  template={'names':names,'rest':{b.name:up@(arm.matrix_world@b.matrix_local) for b in arm.data.bones},
   'tail':{b.name:up@(arm.matrix_world@b.tail_local) for b in arm.data.bones},'parents':{b.name:b.parent.name if b.parent else None for b in arm.data.bones},
   'vertices':[tuple(Vector((v.x*k,v.y*k,(v.z-low)*k))) for v in positions],
   'faces':[tuple(p.vertices) for p in mesh.data.polygons],'uv':[tuple(v.uv) for v in mesh.data.uv_layers[0].data],
   'weights':[[ (mesh.vertex_groups[g.group].name,g.weight) for g in v.groups] for v in mesh.data.vertices], 'k':k,'low':low}
  assert all(template['weights'])
  image=next(i for i in bpy.data.images if 'BaseColor' in i.filepath);image.scale(2048,2048);image.filepath_raw=str(OUT/'Minion1_BaseColor.png');image.file_format='PNG';image.save()
 else:assert names==template['names'] and len(mesh.data.vertices)==len(template['vertices'])
 rest=template['rest'];frames=[];metrics=[];end=int(arm.animation_data.action.frame_range[1])
 for f in range(1,end+1):
  bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
  pose={b.name:arm.matrix_world@b.matrix for b in arm.pose.bones}
  delta=(pose['mixamorig:Hips'].translation-rest['mixamorig:Hips'].translation)*template['k'];delta=conversion@delta;delta.x=delta.z=0
  rotations=[]
  for n in names:
   q=pose[n].to_quaternion()@rest[n].to_quaternion().inverted();q=(conversion@q.to_matrix()@conversion.inverted()).to_quaternion();rotations.extend([q.x,q.y,q.z,q.w])
  frames.append([*delta,*rotations])
  if name.startswith('Punch'):
   side='Left' if name=='PunchLeft' else 'Right';shoulder=pose['mixamorig:'+side+'Arm'].translation;elbow=pose['mixamorig:'+side+'ForeArm'].translation;hand=pose['mixamorig:'+side+'Hand'].translation
   metrics.append(((shoulder-elbow).angle(hand-elbow)*180/math.pi,-hand.y,f-1))
  else:
   metrics.append((max((pose['mixamorig:'+s+'Foot'].translation.z for s in ['Left','Right'])),0,f-1))
 best=max(metrics);report.append({'clip':name,'source':filename,'frames':end,'fps':60,'impactFrame':best[2],'maxElbowDegrees':round(best[0],2) if name.startswith('Punch') else None,'weightedVertices':len(mesh.data.vertices)})
 samples[name]=frames
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.object.armature_add(enter_editmode=True);rig=bpy.context.object;rig.name='Minion1_Rig';rig.data.edit_bones.remove(rig.data.edit_bones[0])
def position(v):return Vector((v.x*template['k'],v.y*template['k'],(v.z-template['low'])*template['k']))
for n in template['names']:
 b=rig.data.edit_bones.new(n);b.head=position(template['rest'][n].translation);b.tail=position(template['tail'][n]);b.align_roll(template['rest'][n].to_quaternion()@Vector((0,0,1)))
 if template['parents'][n]:b.parent=rig.data.edit_bones[template['parents'][n]]
bpy.ops.object.mode_set(mode='OBJECT')
data=bpy.data.meshes.new('Minion1 supplied skin');data.from_pydata(template['vertices'],[],template['faces']);data.update();
for p in data.polygons:p.use_smooth=True
uv=data.uv_layers.new(name='UVMap')
for item,value in zip(uv.data,template['uv']):item.uv=value
mesh=bpy.data.objects.new('Minion1_Skin',data);bpy.context.collection.objects.link(mesh)
groups={n:mesh.vertex_groups.new(name=n) for n in template['names']}
for i,weights in enumerate(template['weights']):
 weights=sorted(weights,key=lambda pair:pair[1],reverse=True)[:4];total=sum(w for _,w in weights);assert total>1e-6
 for n,w in weights:groups[n].add([i],w/total,'REPLACE')
mesh.parent=rig;mod=mesh.modifiers.new('Supplied skin weights','ARMATURE');mod.object=rig
mat=bpy.data.materials.new('Minion1');mat.use_nodes=True;tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Minion1_BaseColor.png'));mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);mesh.data.materials.append(mat)
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'Minion1Model.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
with open(OUT/'Minion1PoseData.bytes','wb') as stream:
 def integer(n):stream.write(struct.pack('<i',n))
 def string(s):b=s.encode();integer(len(b));stream.write(b)
 stream.write(b'KMP1');integer(60);integer(33)
 for n in template['names']:string(n)
 integer(4)
 for name,frames in samples.items():
  string(name);integer(len(frames))
  for frame in frames:stream.write(struct.pack('<'+str(len(frame))+'f',*frame))
(DOC/'import.json').write_text(json.dumps(report,indent=2));print('MINION1_IMPORTED',json.dumps(report),flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE.parent/'Minion1_GameRig.blend'),compress=True)
