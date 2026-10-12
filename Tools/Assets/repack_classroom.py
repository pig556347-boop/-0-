import bpy,sys,math
from pathlib import Path
kind=sys.argv[sys.argv.index('--')+1];out=Path(__file__).resolve().parents[2]/'Assets/Resources/Classroom'/kind
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(out/(kind+'Model.fbx')))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes:
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 while len(o.data.uv_layers):o.data.uv_layers.remove(o.data.uv_layers[0])
 o.data.uv_layers.new(name='BakedUV')
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(45),island_margin=.0005);bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/(kind+'Model.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
