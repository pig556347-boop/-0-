"""Build web FBX/base-color copies; originals remain outside the checkout."""
import bpy,sys,json
from pathlib import Path
kind=sys.argv[sys.argv.index('--')+1]
root=Path(__file__).resolve().parents[2]
out=root/'Assets/Resources/Classroom'/kind;out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='/workspace/asset-import/classroom/'+kind+'.glb')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
target={'floor':800,'wall':4000,'board':4000,'window':6000,'furniture':16000}[kind]
for o in meshes:
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 dec=o.modifiers.new('Web simplification','DECIMATE');dec.ratio=min(1,target/max(1,len(o.data.polygons)))
 bpy.ops.object.modifier_apply(modifier=dec.name)
 for mat in o.data.materials:
  if not mat or not mat.use_nodes:continue
  bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if not bsdf:continue
  links=bsdf.inputs['Base Color'].links
  image=links[0].from_node.image if links and links[0].from_node.type=='TEX_IMAGE' else None
  if image:
   image.scale(1024,1024);image.filepath_raw=str(out/'BaseColor.png');image.file_format='PNG';image.save()
  # FBX material textures are overridden by the runtime shared material.
 bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/(kind+'Model.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
report={'asset':kind,'vertices':sum(len(o.data.vertices) for o in meshes),'triangles':sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),'fbxBytes':(out/(kind+'Model.fbx')).stat().st_size}
(out/'conversion.json').write_text(json.dumps(report,indent=2));print('CONVERTED',report,flush=True)
