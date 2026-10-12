"""Bake supplied floor colors onto a low-poly tile without collapsing its UV seams."""
import bpy,bmesh
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];out=root/'Assets/Resources/Classroom/floor'
bpy.ops.wm.open_mainfile(filepath='/workspace/asset-import/classroom/floor-web.blend')
source=[o for o in bpy.context.scene.objects if o.type=='MESH']
lo=Vector(tuple(min((o.matrix_world@Vector(c))[i] for o in source for c in o.bound_box) for i in range(3)));hi=Vector(tuple(max((o.matrix_world@Vector(c))[i] for o in source for c in o.bound_box) for i in range(3)))
bpy.ops.mesh.primitive_plane_add(size=2,location=((lo.x+hi.x)/2,(lo.y+hi.y)/2,hi.z))
plane=bpy.context.object;plane.name='Floor surface';plane.scale=((hi.x-lo.x)/2,(hi.y-lo.y)/2,1);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
mat=bpy.data.materials.new('Baked floor');mat.use_nodes=True;plane.data.materials.append(mat)
image=bpy.data.images.new('Floor color',width=1024,height=1024);node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;mat.node_tree.nodes.active=node
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=1;sc.cycles.use_denoising=False
sc.render.bake.use_selected_to_active=True;sc.render.bake.cage_extrusion=.1;sc.render.bake.max_ray_distance=.2;sc.render.bake.use_pass_direct=False;sc.render.bake.use_pass_indirect=False;sc.render.bake.use_pass_color=True;sc.render.bake.margin=8
bpy.ops.object.select_all(action='DESELECT')
for o in source:o.select_set(True)
plane.select_set(True);bpy.context.view_layer.objects.active=plane
bpy.ops.object.bake(type='DIFFUSE')
image.filepath_raw=str(out/'BaseColor.png');image.file_format='PNG';image.save()
# The same shallow slab dimensions are retained; it is visual only.
bpy.ops.mesh.primitive_cube_add(size=1,location=(lo+hi)/2);slab=bpy.context.object;slab.name='Floor slab';slab.scale=hi-lo;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
bm=bmesh.new();bm.from_mesh(slab.data);bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.normal.z>.9],context='FACES');bm.to_mesh(slab.data);bm.free()
slab.data.materials.append(mat)
bpy.ops.object.select_all(action='DESELECT');plane.select_set(True);slab.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/'floorModel.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
(out/'conversion.json').write_text('{"asset":"floor","vertices":12,"triangles":12,"method":"selected-to-active diffuse bake, 1024px"}\n')
