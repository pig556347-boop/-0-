"""Transfer source colors using closest surface points (no projection ray misses)."""
import bpy,sys,math,time,json
import numpy as np
from pathlib import Path
from mathutils import Vector,geometry
from mathutils.bvhtree import BVHTree
kind=sys.argv[sys.argv.index('--')+1];root=Path(__file__).resolve().parents[2];out=root/'Assets/Resources/Classroom'/kind
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='/workspace/asset-import/classroom/'+kind+'.glb')
high=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(high)==1
source=high[0]
bsdf=next(n for n in source.data.materials[0].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
image=bsdf.inputs['Base Color'].links[0].from_node.image
if max(image.size)>2048:image.scale(2048,2048)
w,h=image.size;colors=np.empty(w*h*4,dtype=np.float32);image.pixels.foreach_get(colors);colors=colors.reshape(h,w,4)
bvh=BVHTree.FromObject(source,bpy.context.evaluated_depsgraph_get());mesh=source.data;uv=mesh.uv_layers.active.data
existing=set(bpy.context.scene.objects);bpy.ops.import_scene.fbx(filepath=str(out/(kind+'Model.fbx')))
low=[o for o in bpy.context.scene.objects if o.type=='MESH' and o not in existing]
size=2048;atlas=np.zeros((size,size,4),dtype=np.float32);atlas[:,:,3]=1;mask=np.zeros((size,size),dtype=bool)
started=time.time();samples=0
board_green=None
if kind=='board':
 rgb=colors[:,:,:3]
 green=(rgb[:,:,1]>rgb[:,:,0]*1.3)&(rgb[:,:,1]>rgb[:,:,2]*1.1)&(rgb[:,:,1]>.1)
 board_green=np.median(rgb[green],axis=0)
 assert np.isfinite(board_green).all()
for obj in low:
 local=source.matrix_world.inverted()@obj.matrix_world
 target_uv=obj.data.uv_layers.active.data
 for f in obj.data.polygons:
  assert len(f.vertices)==3
  coord=np.array([tuple(target_uv[i].uv) for i in f.loop_indices])*size-.5
  a,b,c=coord;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
  if abs(den)<1e-8:continue
  x0,y0=np.maximum(0,np.floor(coord.min(axis=0)).astype(int));x1,y1=np.minimum(size-1,np.ceil(coord.max(axis=0)).astype(int))
  yy,xx=np.mgrid[y0:y1+1,x0:x1+1]
  v0=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
  v1=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;v2=1-v0-v1
  inside=(v0>=-1e-5)&(v1>=-1e-5)&(v2>=-1e-5)
  pts=[local@obj.data.vertices[i].co for i in f.vertices]
  for j,i in zip(*np.nonzero(inside)):
   point=pts[0]*float(v0[j,i])+pts[1]*float(v1[j,i])+pts[2]*float(v2[j,i])
   near,normal,index,distance=bvh.find_nearest(point)
   sf=mesh.polygons[index];li=list(sf.loop_indices);vi=list(sf.vertices)
   mapped=geometry.barycentric_transform(near,mesh.vertices[vi[0]].co,mesh.vertices[vi[1]].co,mesh.vertices[vi[2]].co,
    Vector((*uv[li[0]].uv,0)),Vector((*uv[li[1]].uv,0)),Vector((*uv[li[2]].uv,0)))
   sx=min(w-1,max(0,round(mapped.x*(w-1))));sy=min(h-1,max(0,round(mapped.y*(h-1))))
   color=colors[sy,sx].copy()
   if kind=='board':
    surface=source.matrix_world@point
    # Restore the front writing surface while retaining the supplied frame.
    # Reduction/projection can incorrectly sample the pale internal backing.
    if surface.x>0 and abs(surface.y)<.445 and .055<surface.z<.700:
     color[:3]=board_green
   if kind=='furniture':
    # The source base-color map itself has noisy marks on painted metal.
    # Preserve wood and rubber feet, but restore a clean uniform frame finish.
    height=(source.matrix_world@point).z
    r,g,b=color[:3];wood=(r>.18 and r>g*1.15 and g>b*1.2)
    if .08<height<.70 and (height<.32 or not wood):color[:3]=(.72,.73,.74)
   atlas[y0+j,x0+i]=color;mask[y0+j,x0+i]=True;samples+=1
# Extend colors outside UV islands; genuine black source surfaces stay untouched.
for n in range(24):
 old=mask.copy();values=atlas.copy()
 for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)]:
  adjacent=np.roll(old,(dy,dx),(0,1));take=(~mask)&adjacent
  atlas[take]=np.roll(values,(dy,dx),(0,1))[take];mask[take]=True
result=bpy.data.images.new(kind+' transferred color',width=size,height=size,alpha=True)
result.colorspace_settings.name=image.colorspace_settings.name
result.pixels.foreach_set(atlas.ravel());result.filepath_raw=str(out/'BaseColor.png');result.file_format='PNG';result.save()
bpy.ops.object.select_all(action='DESELECT')
for obj in low:obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/(kind+'Model.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP')
report={'asset':kind,'vertices':sum(len(o.data.vertices) for o in low),'triangles':sum(len(f.vertices)-2 for o in low for f in o.data.polygons),'method':'source base color transferred by nearest surface, padded coherent UV atlas','samples':samples}
(out/'conversion.json').write_text(json.dumps(report,indent=2));print('TRANSFERRED',report,'seconds',time.time()-started,flush=True)
