import bpy,pathlib,json,math,sys
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[2];SOURCE=pathlib.Path('/workspace/asset-import/motions');OUT=SOURCE/'previews';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'retargeted-player.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH')
# Emission texture, same source appearance, no noisy PBR shading.
mat=mesh.data.materials[0];nodes=mat.node_tree.nodes;tex=next(n for n in nodes if n.type=='TEX_IMAGE');emit=nodes.new('ShaderNodeEmission');mat.node_tree.links.new(tex.outputs['Color'],emit.inputs['Color']);mat.node_tree.links.new(emit.outputs[0],nodes.get('Material Output').inputs['Surface'])
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=1;sc.cycles.use_denoising=False;sc.world=bpy.data.worlds.new('World');sc.world.color=(.2,.2,.2)
sc.render.resolution_x=300;sc.render.resolution_y=360;sc.render.resolution_percentage=100
sc.view_settings.view_transform='Standard'
bpy.ops.object.camera_add(location=(3,-5,2.7));cam=bpy.context.object;target=Vector((0,0,1));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.9;sc.camera=cam
names=[a.name for a in bpy.data.actions];only=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else names
for name in only:
 action=bpy.data.actions.get(name)
 if not action:continue
 rig.animation_data.action=action;start,end=action.frame_range
 for index,t in enumerate([0,.2,.4,.6,.8,1]):
  sc.frame_set(round(start+(end-start)*t));sc.render.filepath=str(OUT/f'{name}-{index}.png');bpy.ops.render.render(write_still=True)
 print('PREVIEW_COMPLETE',name,flush=True)
