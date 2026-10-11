import bpy,math,json
from mathutils import Vector,Quaternion
from pathlib import Path
reports=[]
for kind in ['Player','Deongchi']:
 bpy.ops.wm.open_mainfile(filepath='/workspace/'+kind+'_Repaired.blend')
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');obj=next(o for o in bpy.context.scene.objects if o.type=='MESH');scene=bpy.context.scene
 scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=False;scene.render.resolution_x=420;scene.render.resolution_y=520;scene.render.resolution_percentage=100;scene.world.color=(.45,.45,.45)
 bpy.ops.object.camera_add(location=(3,-5,2));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.85))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.35;scene.camera=cam
 bpy.ops.object.light_add(type='AREA',location=(-2,-3,4));bpy.context.object.data.energy=450;bpy.context.object.data.size=5
 def pose(name,deg):
  b=rig.pose.bones[name];b.rotation_mode='QUATERNION';axis=b.bone.matrix_local.to_quaternion().inverted()@Vector((1,0,0));b.rotation_quaternion=Quaternion(axis,math.radians(deg))
 def leg(side,down,forward):
  d=min(.7799,math.hypot(down,forward));a=math.acos((.4**2+d*d-.38**2)/(.8*d));k=math.pi-math.acos((.4**2+.38**2-d*d)/(2*.4*.38));t=-math.atan2(forward,down)-a
  pose(side+'Thigh',math.degrees(t));pose(side+'Shin',math.degrees(k));pose(side+'Foot',-math.degrees(t+k))
 for state in ['Idle','Walk','Chamber','Kick','Crouch','Guard']:
  for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0)
  for side in ['Left','Right']:pose(side+'UpperArm',-20);pose(side+'Forearm',-55)
  if state=='Walk':leg('Left',.74,.21);leg('Right',.78,-.21)
  if state=='Chamber':pose('RightThigh',-62);pose('RightShin',100)
  if state=='Kick':pose('RightThigh',-86);pose('RightShin',9)
  if state=='Crouch':
   # pose translation expressed in bone local coordinates
   rig.pose.bones['Hips'].location=rig.data.bones['Hips'].matrix_local.to_quaternion().inverted()@Vector((0,.11,-.31))
   for side in ['Left','Right']:leg(side,.47,.11)
   pose('Spine',9);pose('Chest',6);pose('Head',-8)
  if state=='Guard':
   for side in ['Left','Right']:pose(side+'UpperArm',-32);pose(side+'Forearm',-105)
  bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh()
  verts=[v.co for v in mesh.vertices];maxstretch=0
  for e in obj.data.edges:
   a,b=e.vertices;rest=(obj.data.vertices[a].co-obj.data.vertices[b].co).length
   if rest>.008:maxstretch=max(maxstretch,(verts[a]-verts[b]).length/rest)
  assert all(math.isfinite(c) for v in verts for c in v)
  reports.append(dict(model=kind,pose=state,edge_stretch=round(maxstretch,3),min_z=round(min(v.z for v in verts),3),max_z=round(max(v.z for v in verts),3)))
  evaluated.to_mesh_clear();scene.render.filepath='/workspace/-0-/Docs/Motion/'+kind+'_'+state+'.png';bpy.ops.render.render(write_still=True)
Path('/workspace/-0-/Docs/Motion/skin-audit.json').write_text(json.dumps(reports,indent=2))
