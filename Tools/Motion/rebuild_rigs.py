import bpy,math,json
from pathlib import Path
from mathutils import Vector

def prepare(kind,blend,height,width):
 bpy.ops.wm.open_mainfile(filepath=blend)
 obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
 for o in list(bpy.context.scene.objects):
  if o!=obj:bpy.data.objects.remove(o,do_unlink=True)
 obj.parent=None;obj.matrix_world.identity();obj.modifiers.clear();obj.vertex_groups.clear()
 bounds=[v.co for v in obj.data.vertices];lo=min(v.z for v in bounds);hi=max(v.z for v in bounds)
 k=height/(hi-lo)
 for v in obj.data.vertices:v.co=Vector((v.co.x*k,v.co.y*k,(v.co.z-lo)*k))
 bpy.ops.object.armature_add(enter_editmode=True);rig=bpy.context.object;rig.name=kind+'_Rig';arm=rig.data;arm.edit_bones.remove(arm.edit_bones[0])
 specs=[('Hips',(0,0,.88),(0,0,1.02),None),('Spine',(0,0,1.02),(0,0,1.25),'Hips'),('Chest',(0,0,1.25),(0,0,1.45),'Spine'),('Neck',(0,0,1.45),(0,0,1.59),'Chest'),('Head',(0,0,1.59),(0,0,1.83),'Neck')]
 for side,s in [('Left',1),('Right',-1)]:
  specs.extend([(side+'Shoulder',(0,0,1.44),(s*.19*width,0,1.43),'Chest'),(side+'UpperArm',(s*.19*width,0,1.43),(s*.32*width,0,1.15),side+'Shoulder'),(side+'Forearm',(s*.32*width,0,1.15),(s*.41*width,-.01,.91),side+'UpperArm'),(side+'Hand',(s*.41*width,-.01,.91),(s*.43*width,-.01,.79),side+'Forearm'),(side+'Thigh',(s*.13*width,0,.88),(s*.13*width,0,.48),'Hips'),(side+'Shin',(s*.13*width,0,.48),(s*.13*width,0,.10),side+'Thigh'),(side+'Foot',(s*.13*width,0,.10),(s*.13*width,-.14,.05),side+'Shin')])
 for name,a,b,parent in specs:
  bone=arm.edit_bones.new(name);bone.head=a;bone.tail=b
  if parent:bone.parent=arm.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT')
 # Bind on a watertight voxel proxy, then transfer smooth skin back to UV mesh.
 from mathutils.kdtree import KDTree
 proxy=obj.copy();proxy.data=obj.data.copy();bpy.context.collection.objects.link(proxy)
 bpy.ops.object.select_all(action='DESELECT');proxy.select_set(True);bpy.context.view_layer.objects.active=proxy
 remesh=proxy.modifiers.new('Watertight binding proxy','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.018;remesh.use_smooth_shade=True
 bpy.ops.object.modifier_apply(modifier=remesh.name)
 rig.select_set(True);bpy.context.view_layer.objects.active=rig
 bpy.ops.object.parent_set(type='ARMATURE_AUTO')
 weighted=[v for v in proxy.data.vertices if sum(g.weight for g in v.groups)>.0001]
 if len(weighted)<len(proxy.data.vertices)*.98:raise RuntimeError('Voxel heat binding coverage insufficient')
 kd=KDTree(len(weighted))
 for i,v in enumerate(weighted):kd.insert(v.co,i)
 kd.balance()
 groups={g.name:obj.vertex_groups.new(name=g.name) for g in proxy.vertex_groups}
 for v in obj.data.vertices:
  weights={}
  for _,i,d in kd.find_n(v.co,4):
   for g in weighted[i].groups:
    name=proxy.vertex_groups[g.group].name;weights[name]=weights.get(name,0)+g.weight/(.005+d)**2
  chosen=sorted(weights.items(),key=lambda item:item[1],reverse=True)[:4];total=sum(w for _,w in chosen)
  for name,w in chosen:groups[name].add([v.index],w/total,'REPLACE')
 bpy.data.objects.remove(proxy,do_unlink=True)
 obj.parent=rig;mod=obj.modifiers.new('Anatomical skin','ARMATURE');mod.object=rig
 root=Path('/workspace/-0-/Assets/Resources/Characters')/kind;root.mkdir(parents=True,exist_ok=True)
 for mat in obj.data.materials:
  bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
  col=next((l.from_node.image for l in mat.node_tree.links if l.to_node==bsdf and l.to_socket.name=='Base Color' and l.from_node.type=='TEX_IMAGE'),None)
  if col is None:col=next((n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and 'basecolor' in n.image.name.lower()),None)
  if col:
   col.scale(1024,1024);col.filepath_raw=str(root/(kind+'_BaseColor.png'));col.file_format='PNG';col.save()
   tex=next(n for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image==col);mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
  for n in list(mat.node_tree.nodes):
   if n.type=='TEX_IMAGE' and n.image!=col:mat.node_tree.nodes.remove(n)
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
 bpy.ops.file.pack_all()
 bpy.ops.wm.save_as_mainfile(filepath='/workspace/'+kind+'_Repaired.blend')
 bpy.ops.export_scene.fbx(filepath=str(root/(kind+'Model.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
 print('RIG_REPAIRED',kind,len(obj.data.vertices),flush=True)
prepare('Player','/workspace/Player_Prepared.blend',1.85,1)
prepare('Deongchi','/workspace/Deongchi_Source.blend',1.85,1.45)
