import bpy, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
for kind in ['Player','Deongchi']:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.fbx(filepath=str(root/'Assets/Resources/Characters'/kind/(kind+'Model.fbx')))
 mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 assert len(rig.data.bones)==19
 assert len(mesh.data.vertices)<25000
 for v in mesh.data.vertices:
  assert 1<=len(v.groups)<=4
  assert abs(sum(g.weight for g in v.groups)-1)<.001
  assert all(math.isfinite(c) for c in v.co)
 box=[mesh.matrix_world@Vector(v) for v in mesh.bound_box]
 height=max(v.z for v in box)-min(v.z for v in box)
 assert abs(height-1.85)<.01,(kind,height)
 print('FBX_ROUNDTRIP_PASS',kind,'vertices',len(mesh.data.vertices),'bones',len(rig.data.bones),'height',height,flush=True)
