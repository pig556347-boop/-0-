import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
kind=sys.argv[sys.argv.index('--')+1];root=Path('/workspace/-0-')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='/workspace/asset-import/classroom/'+kind+'.glb')
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
for obj in objects:
 bpy.context.view_layer.objects.active=obj;obj.select_set(True)
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 count=len(obj.data.polygons)
 dec=obj.modifiers.new('Web mesh','DECIMATE');dec.ratio=min(1,15000/max(1,count))
 bpy.ops.object.modifier_apply(modifier=dec.name)
 for mat in obj.data.materials:
  if mat and mat.use_nodes:
   for node in mat.node_tree.nodes:
    if node.type=='TEX_IMAGE' and node.image and node.image.size[0]>1024:node.image.scale(1024,1024)
 obj.select_set(False)
lo=Vector(tuple(min((o.matrix_world@Vector(c))[i] for o in objects for c in o.bound_box) for i in range(3)));hi=Vector(tuple(max((o.matrix_world@Vector(c))[i] for o in objects for c in o.bound_box) for i in range(3)));centre=(lo+hi)*.5;span=max(hi-lo)
print('INSPECT',kind,'bounds',list(lo),list(hi),'mesh',sum(len(o.data.vertices) for o in objects),flush=True)
bpy.ops.object.camera_add(location=centre+Vector((1.6,-2.4,1.7))*span);cam=bpy.context.object;cam.rotation_euler=(centre-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=span*1.6;bpy.context.scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=centre+Vector((1,-2,3))*span);bpy.context.object.data.energy=600;bpy.context.object.data.size=span*3
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=4;sc.cycles.use_denoising=False;sc.world=bpy.data.worlds.new('Inspection world');sc.world.color=(.35,.35,.35);sc.render.resolution_x=700;sc.render.resolution_y=600;sc.render.resolution_percentage=100;sc.render.filepath=str(root/'Docs/Assets'/(kind+'-inspection.png'));bpy.ops.render.render(write_still=True)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath='/workspace/asset-import/classroom/'+kind+'-web.blend',compress=True)
