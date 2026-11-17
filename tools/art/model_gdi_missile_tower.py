"""Background Blender reconstruction; cut-only native textures, PNG base, rotating 3D turret.
No source/runtime writes. Missing upper/rear mechanisms are explicitly inferred.
"""
from pathlib import Path
import bpy, math, json, sys, argparse
from mathutils import Vector
P=argparse.ArgumentParser();P.add_argument('--pass',dest='iteration',type=int,required=True);P.add_argument('--all',action='store_true');a=P.parse_args(sys.argv[sys.argv.index('--')+1:])
ROOT=Path(__file__).resolve().parents[2];F=ROOT/'art-source/gdi missile tower';CUT=F/'source-cuts';OUT=F/f'pass-{a.iteration:02d}';assert not (OUT/'turret.blend').exists(),'Preserve existing passes: choose a new pass number.';OUT.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=224;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True;s.render.dither_intensity=0;s.render.filter_size=.01;s.eevee.taa_render_samples=1;s.eevee.use_gtao=False;s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=0;s.view_settings.gamma=1
root=bpy.data.objects.new('turret_pivot',None);s.collection.objects.link(root)
cradle=bpy.data.objects.new('elevation_cradle',None);s.collection.objects.link(cradle);cradle.parent=root;cradle.location=(0,-10,18)
materials={};patch_sizes={}
for name in ('gold','goldlight','goldshade','steel','darksteel','black','vent','cyan','rim','goldedge','steelbright'):
 image=bpy.data.images.load(str(CUT/(name+'.png')));image.pack();patch_sizes[name]=tuple(image.size)
 m=bpy.data.materials.new('CUT / '+name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();t=n.new('ShaderNodeTexImage');t.image=image;t.interpolation='Closest';t.extension='REPEAT';e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(t.outputs['Color'],e.inputs[0]);m.node_tree.links.new(e.outputs[0],out.inputs[0]);materials[name]=m
names=list(materials);records=[];light=Vector((-.4,-.65,.65)).normalized()
def mesh(name,verts,faces,mat='gold',upper=False,face_mats=None,face_uv=None):
 data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update();o=bpy.data.objects.new(name,data);s.collection.objects.link(o);o.parent=cradle if upper else root
 if upper:o.location=(0,0,-28)
 for m in materials.values():data.materials.append(m)
 uv=data.uv_layers.new()
 for f in data.polygons:
  role=mat
  if mat=='gold':
   lit=f.normal.dot(light);role='goldlight' if lit>.5 else ('gold' if lit>-.3 else 'goldshade')
  if mat in ('steel','darksteel'):
   lit=f.normal.dot(light)
   role=('steelbright' if lit>.65 else ('steel' if lit>.15 else 'darksteel')) if mat=='steel' else ('steel' if lit>.6 else ('darksteel' if lit>-.35 else 'black'))
  if mat=='missile':
   role='steel' if f.normal.z>.92 else ('darksteel' if f.normal.z>.25 else 'black')
  if face_mats and f.index in face_mats:role=face_mats[f.index]
  f.material_index=names.index(role);w,h=patch_sizes[role]
  p0=data.vertices[f.vertices[0]].co;u=(data.vertices[f.vertices[1]].co-p0).normalized();v=f.normal.cross(u).normalized()
  for j,loop in enumerate(f.loop_indices):
   p=data.vertices[data.loops[loop].vertex_index].co
   # 1 world unit = 1 native texel; continuous strips, no per-facet restart/projection.
   xy=face_uv[f.index][j] if face_uv and f.index in face_uv else (p.dot(u),p.dot(v))
   uv.data[loop].uv=(xy[0]/w,xy[1]/h)
 records.append({'name':name,'material':mat,'upper':upper,'faces':len(faces),'vertices':len(verts)})
 return o

def box(name,c,sz,mat='gold',upper=False):
 c=Vector(c);v=[tuple(c+Vector((x*sz[0]/2,y*sz[1]/2,z*sz[2]/2))) for z in (-1,1) for y in (-1,1) for x in (-1,1)]
 return mesh(name,v,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],mat,upper)
def tube(name,start,end,levels,mat='steel',upper=False,n=20):
 start=Vector(start);end=Vector(end);axis=(end-start).normalized();ref=Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0));u=axis.cross(ref).normalized();v=axis.cross(u).normalized();verts=[]
 for t,r in levels:
  c=start+(end-start)*t;verts.extend([tuple(c+r*(u*math.cos(i*2*math.pi/n)+v*math.sin(i*2*math.pi/n))) for i in range(n)])
 faces=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(levels)-1) for i in range(n)];faces.extend([tuple(range(n-1,-1,-1)),tuple((len(levels)-1)*n+i for i in range(n))])
 strips={};length=(end-start).length;radius=max(r for t,r in levels)
 for j in range(len(levels)-1):
  for i in range(n):
   u0=i*2*math.pi*radius/n;u1=(i+1)*2*math.pi*radius/n;v0=levels[j][0]*length;v1=levels[j+1][0]*length
   strips[j*n+i]=[(u0,v0),(u1,v0),(u1,v1),(u0,v1)]
 return mesh(name,verts,faces,mat,upper,face_uv=strips)
def cyl(name,start,end,r,mat='steel',upper=False,n=20):return tube(name,start,end,[(0,r),(.12,r*1.04),(.88,r*1.04),(1,r)],mat,upper,n)
def outline(w,h,r):
 pts=[]
 for x,z,t in [(w/2-r,h/2-r,0),(-w/2+r,h/2-r,90),(-w/2+r,-h/2+r,180),(w/2-r,-h/2+r,270)]:
  for i in range(6):
   b=math.radians(t+i*90/5);pts.append((x+r*math.cos(b),z+r*math.sin(b)))
 return pts

def shell(name,cx,cz,rings,mat='gold'):
 verts=[]
 for y,w,h,r in rings:verts.extend([(cx+x,y,cz+z) for x,z in outline(w,h,r)])
 n=24;faces=[(j*n+i,(j+1)*n+i,(j+1)*n+(i+1)%n,j*n+(i+1)%n) for j in range(len(rings)-1) for i in range(n)]
 strips={};shape=outline(rings[0][1],rings[0][2],rings[0][3]);arc=[0.]
 for i in range(n):arc.append(arc[-1]+math.dist(shape[i],shape[(i+1)%n]))
 distances=[0.]
 for j in range(len(rings)-1):distances.append(distances[-1]+math.sqrt((rings[j+1][0]-rings[j][0])**2+((rings[j+1][1]-rings[j][1])/2)**2))
 for j in range(len(rings)-1):
  for i in range(n):strips[j*n+i]=[(arc[i],distances[j]),(arc[i],distances[j+1]),(arc[i+1],distances[j+1]),(arc[i+1],distances[j])]
 return mesh(name,verts,faces,mat,True,face_uv=strips)
# Rotating bearing; stationary tower is a separate copied PNG, never rebuilt as a box.
tube('bearing / stepped bronze hub',(0,0,0),(0,0,7),[(0,22),(.18,23),(.4,23),(.5,21),(.85,21),(1,18)],'darksteel',n=48)
tube('bearing / dark lower seam',(0,0,1.6),(0,0,2.6),[(0,23.3),(1,23.3)],'black',n=48)
tube('bearing / bright shoulder',(0,0,3),(0,0,4.4),[(0,22.9),(1,22.9)],'steel',n=48)
tube('bearing / upper seam',(0,0,5),(0,0,5.8),[(0,21.3),(1,21.3)],'black',n=48)
for i in range(12):
 th=i*math.pi/6;cyl('bearing / bolt%02d'%i,(18*math.cos(th),18*math.sin(th),6),(18*math.cos(th),18*math.sin(th),7.5),1.1,'steel',n=8)
for i in range(4):
 th=i*math.pi/2+math.pi/4;box('bearing / locking pad'+str(i),(19*math.cos(th),19*math.sin(th),6.8),(3,3,2),'goldshade')
box('drive gearbox',(0,0,12),(24,21,13),'gold')
box('drive gearbox / inset rear',(0,17,12),(17,1,7),'darksteel')
for z in range(9,16,2):box('drive gearbox / rear fins'+str(z),(0,18,z),(15,2,.7),'steel')
# Curved structural arms and trunnions are visible from every direction.
for side in (-1,1):
 x=side*29
 cyl('trunnion / main axle'+str(side),(x-5,-10,19),(x+5,-10,19),7.5,'darksteel',n=24)
 cyl('trunnion / cap'+str(side),(x+side*5,-10,19),(x+side*6,-10,19),6,'steel',n=24)
 cyl('trunnion / bronze boss'+str(side),(x+side*6,-10,19),(x+side*7,-10,19),3.7,'gold',n=16)
 for seg,(c,sz) in enumerate([((x,3,9),(6,13,9)),((x,-4,15),(6,14,11))]):box('yoke armored leg'+str(side)+str(seg),c,sz,'darksteel')
 cyl('yoke hydraulic ram'+str(side),(side*18,10,7),(side*28,-9,19),2.3,'steel',n=12)
 cyl('yoke piston sleeve'+str(side),(side*18,10,7),(side*23,0,13),3.5,'darksteel',n=12)
 box('yoke toe'+str(side),(side*22,4,6),(12,14,4),'gold')
# Twin three-round magazines. Shape/spacing from visible source; roofs/rears inferred.
for side in (-1,1):
 x=side*17;z=45
 shell('magazine / rounded armored case'+str(side),x,z,[(-23,23,43,6),(-21,26,46,7),(3,26,46,7),(6,22,42,6)])
 shell('magazine / inked front seam'+str(side),x,z,[(-24.6,24.8,44.8,6.7),(-24.6,22.8,42.8,5.7)],'black')
 shell('magazine / front lip'+str(side),x,z,[(-24,23,43,6),(-24.5,20,40,5),(-22,18,38,4)],'goldlight')
 shell('magazine / recessed steel mouth'+str(side),x,z,[(-22,18,38,4),(-16,18,38,4)],'darksteel')
 box('magazine / inner backplate'+str(side),(x,-15,45),(18,1,35),'black',True)
 box('magazine / rear lid'+str(side),(x,6,45),(20,2,38),'goldshade',True)
 box('magazine / rear service hatch'+str(side),(x,7.5,45),(13,1,24),'darksteel',True)
 for zz in range(37,55,3):box('magazine / rear cooling slat'+str(side)+'/'+str(zz),(x,8.5,zz),(11,1,1),'steel',True)
 for yy in (-14,-2):
  box('magazine / side reinforcement'+str(side)+'/'+str(yy),(x+side*13.2,yy,45),(1.8,2,29),'goldlight',True)
  for zz in (33,57):cyl('magazine / screw'+str(side)+str(yy)+str(zz),(x+side*13,yy,zz),(x+side*14.5,yy,zz),1,'steel',True,n=8)
 # Closed roof, individual access panel and bolts; not a copied missile face on the back.
 box('magazine / roof'+str(side),(x,-6,67),(13,21,2),'gold',True)
 box('magazine / roof inset'+str(side),(x,-6,68.2),(8,13,.8),'darksteel',True)
 for j in (-1,0,1):
  zz=45+j*12
  cyl('launch tube / sleeve'+str(side)+str(j),(x,-10,zz),(x,-23,zz),4.3,'darksteel',True,n=24)
  cyl('launch tube / mouth ring'+str(side)+str(j),(x,-22.5,zz),(x,-25,zz),4.6,'darksteel',True,n=24)
  tube('missile / rounded nose'+str(side)+str(j),(x,-20,zz),(x,-40,zz),[(0,3.3),(.65,3.3),(.78,3.0),(.91,2.4),(.98,1.3),(1,.3)],'missile',True,n=20)
  cyl('missile / collar'+str(side)+str(j),(x,-23.2,zz),(x,-24.2,zz),3.6,'goldshade',True,n=20)
  box('tube / separator rail'+str(side)+str(j),(x+side*8,-21,zz),(2,4,9),'steel',True)
# Central service spine, elevation drive, upper tracking head.
box('central magazine spine / dark recess',(0,-25,47),(11,5,44),'black',True)
box('central magazine spine / recessed web',(0,-27.4,47),(4,1,40),'darksteel',True)
for x in (-4,4):box('spine / machined edge'+str(x),(x,-28.5,47),(1.5,1,42),'steel',True)
for z in (33,46,59):
 cyl('spine / retained hinge pin'+str(z),(0,-27,z),(0,-30,z),1.7,'goldshade',True,n=10)
for zz in (30,42,54,64):box('spine / latch'+str(zz),(0,-29,zz),(9,2,2),'darksteel',True)
box('rear receiver crossbrace',(0,10,46),(57,9,13),'gold',True)
box('rear junction box',(0,16,47),(14,5,11),'darksteel',True)
for side in (-1,1):
 cyl('receiver / service conduit'+str(side),(side*5,18,48),(side*22,14,39),1.6,'steel',True,n=10)
 box('receiver / control badge'+str(side),(side*22,15,49),(7,1,4),'cyan',True)
box('tracking head / armored pedestal',(0,9,67),(13,13,9),'gold',True)
box('tracking head / body',(0,10,75),(24,18,12),'gold',True)
box('tracking head / right bezel',(12.5,10,75),(1,10,7),'darksteel',True)
box('tracking head / right optical window',(13.2,10,75),(1,6,3),'cyan',True)
box('tracking head / roof plate',(0,10,81.5),(21,15,1),'goldlight',True)
box('tracking head / face bezel',(0,.5,75),(11,1.5,6),'darksteel',True)
box('tracking head / optical window',(0,-.5,75),(7,1,3),'cyan',True)
for side in (-1,1):cyl('tracking head / pivot'+str(side),(side*12,15,75),(side*14,15,75),2.3,'steel',True,n=12)
cradle.rotation_euler.x=math.radians(-27)
target=Vector((0,0,14/(math.sqrt(3)/2)));bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='source camera / native pixels';cam.location=target+Vector((140,-140,math.sqrt(39200)/math.sqrt(3)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=224;s.camera=cam
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'turret.blend'))
(OUT/'geometry.json').write_text(json.dumps({'pass':a.iteration,'objects':records,'texture_policy':'native rectangular source cuts, closest sampling, one texel per model unit, no lighting/recolor/projection','camera':{'native_size':224,'source_offset':[48,80],'pivot_screen':[112,126]},'inferred':['upper magazine roofs','tracking-head upper casing','rear access hatches and conduits','occluded swivel bearing']},indent=2)+'\n')
angles=[i*11.25 for i in range(32)] if a.all else list(range(0,360,45))
for i,angle in enumerate(angles):
 root.rotation_euler.z=math.radians(angle);bpy.context.view_layer.update();s.render.filepath=str(OUT/f'turret-{round(angle*100):05d}.png');bpy.ops.render.render(write_still=True)
print('GDI_MISSILE_TOWER_PASS_COMPLETE',a.iteration,len(records),flush=True)
