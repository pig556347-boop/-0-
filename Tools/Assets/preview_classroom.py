import bpy,sys
from pathlib import Path
from mathutils import Vector
kind=sys.argv[sys.argv.index('--')+1];root=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Assets/Resources/Classroom'/kind/(kind+'Model.fbx')))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
mat=bpy.data.materials.new('Base');mat.use_nodes=True;nodes=mat.node_tree.nodes;tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(root/'Assets/Resources/Classroom'/kind/'BaseColor.png'));mat.node_tree.links.new(tex.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color'])
for o in objects:o.data.materials.clear();o.data.materials.append(mat)
lo=Vector(tuple(min((o.matrix_world@Vector(c))[i] for o in objects for c in o.bound_box) for i in range(3)));hi=Vector(tuple(max((o.matrix_world@Vector(c))[i] for o in objects for c in o.bound_box) for i in range(3)));centre=(lo+hi)*.5;span=max(hi-lo)
bpy.ops.object.camera_add(location=centre+Vector((1.6,-2.4,1.7))*span);cam=bpy.context.object;cam.rotation_euler=(centre-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=span*1.6;bpy.context.scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=centre+Vector((1,-2,3))*span);bpy.context.object.data.energy=600;bpy.context.object.data.size=span*3
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=12;sc.cycles.use_denoising=False;sc.world=bpy.data.worlds.new('World');sc.world.color=(.35,.35,.35);sc.render.resolution_x=700;sc.render.resolution_y=600;sc.render.resolution_percentage=100;sc.render.filepath=str(root/'Docs/Assets'/(kind+'-inspection.png'));bpy.ops.render.render(write_still=True)
