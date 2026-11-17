"""Initial solid Allied quad-flak reconstruction, background Blender only.
Stationary foundation / rotating cradle and four genuinely hollow elevated barrels.
Source cuts are confined to detail plates; no whole-reference projection.
"""
from pathlib import Path
import bpy,math,json,hashlib,argparse,sys
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];FOLDER=ROOT/'art-source/allies flak';SOURCE=next(FOLDER.glob('*.png'))
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,required=True);p.add_argument('--all',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);OUT=FOLDER/f'pass-{args.iteration:02d}';OUT.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=256;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True;s.render.filter_size=.01;s.render.dither_intensity=0;s.eevee.taa_render_samples=1;s.view_settings.view_transform='Standard';s.view_settings.look='None'
fixed=bpy.data.collections.new('STATIONARY BASE');moving=bpy.data.collections.new('ROTATING TURRET');s.collection.children.link(fixed);s.collection.children.link(moving);pivot=bpy.data.objects.new('turret_pivot',None);moving.objects.link(pivot);pivot.location=(0,0,1.65)
image=bpy.data.images.load(str(SOURCE));image.pack();rgba=list(image.pixels[:])
if args.iteration>=4:
 # The supplied PNG has opaque white background: remove only border-connected
 # near-white pixels in the packed working copy, preserving enclosed light marks.
 from collections import deque
 w,h=image.size;todo=deque(list(range(w))+list(range((h-1)*w,h*w))+[y*w for y in range(h)]+[y*w+w-1 for y in range(h)]);seen=set()
 while todo:
  i=todo.popleft()
  if i in seen:continue
  seen.add(i)
  if min(rgba[i*4:i*4+3])<.95:continue
  rgba[i*4+3]=0;x,y=i%w,i//w
  for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
   if 0<=xx<w and 0<=yy<h:todo.append(yy*w+xx)
 image.pixels[:]=rgba;image.pack()
colors={tuple(round(v*255) for v in rgba[i:i+3]) for i in range(0,len(rgba),4) if rgba[i+3]>.99}
def nearest(rgb):return min(colors,key=lambda c:sum((c[i]-rgb[i])**2 for i in range(3)))
def lin(x):
 x=x/255;return x/12.92 if x<=.04045 else ((x+.055)/1.055)**2.4
mats=[]
for i,col in enumerate(((20,26,33),(43,53,63),(66,80,93),(91,110,124),(126,150,167),(167,189,201))):
 m=bpy.data.materials.new('Source steel '+str(i));m.use_nodes=True;n=m.node_tree.nodes;n.clear();em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');em.inputs[0].default_value=(*map(lin,nearest(col)),1);m.node_tree.links.new(em.outputs[0],out.inputs[0]);mats.append(m)
m=bpy.data.materials.new('Bounded source detail');m.use_nodes=True;n=m.node_tree.nodes;n.clear();tex=n.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest';em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(tex.outputs['Color'],em.inputs[0]);m.node_tree.links.new(em.outputs[0],out.inputs[0]);mats.append(m)
objects=[]
def mesh(name,vs,fs,stationary=False,bias=0,uv=None):
 data=bpy.data.meshes.new(name);data.from_pydata(vs,[],fs);data.update();o=bpy.data.objects.new(name,data);(fixed if stationary else moving).objects.link(o)
 if not stationary:o.parent=pivot;o.location=-pivot.location
 for m in mats:data.materials.append(m)
 layer=data.uv_layers.new()
 for poly in data.polygons:
  poly.material_index=6 if uv and poly.index in uv else 3
  for loop,(u,v) in zip(poly.loop_indices,uv[poly.index] if uv and poly.index in uv else [(55,85)]*len(poly.vertices)):layer.data[loop].uv=(u/image.size[0],1-v/image.size[1])
 o['fixed']=stationary;o['bias']=bias;o['textured']=list((uv or {}).keys());objects.append(o);return o

def box(name,center,size,stationary=False,bias=0,uv=None):
 x,y,z=center;a,b,c=[v/2 for v in size];vs=[(x+xx*a,y+yy*b,z+zz*c) for zz in (-1,1) for yy in (-1,1) for xx in (-1,1)]
 return mesh(name,vs,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],stationary,bias,uv)
def tube(name,a,b,r,inner=0,n=24,stationary=False,bias=0):
 a,b=Vector(a),Vector(b);axis=(b-a).normalized();u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized();v=axis.cross(u);vs=[]
 for center,rr in [(a,r),(b,r)]+([(a,inner),(b,inner)] if inner else []):vs.extend(tuple(center+rr*(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))) for i in range(n))
 fs=[]
 for i in range(n):
  j=(i+1)%n;fs.append((i,j,n+j,n+i))
  if inner:fs.extend([(n+i,n+j,3*n+j,3*n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)])
 if not inner:fs.extend([tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))])
 return mesh(name,vs,fs,stationary,bias)
# Plate is below 1x1 ground extent; no bib or gameplay actor is installed.
box('Fixed ground plate',(0,0,.12),(3.70,3.70,.16),True,bias=0)
tube('Fixed armored pedestal',(0,0,.20),(0,0,1.32),1.96,n=8,stationary=True)
tube('Fixed sloped cap',(0,0,1.32),(0,0,1.48),1.98,n=8,stationary=True,bias=1)
tube('Fixed rotation bearing',(0,0,1.48),(0,0,1.65),1.30,n=32,stationary=True,bias=-1)
for t in range(8):
 theta=(t+.5 if args.iteration>=2 else t)*math.pi/4;normal=Vector((math.cos(theta),math.sin(theta),0));tangent=Vector((-normal.y,normal.x,0));center=normal*1.825+Vector((0,0,.77));vs=[tuple(center+tangent*x+Vector((0,0,z))) for x,z in ((-.23,.38),(.23,.38),(.23,-.38),(-.23,-.38))]
 mesh('Fixed vent plate '+str(t),vs,[(0,1,2,3)],True,uv={0:[(33,86),(41,88),(41,102),(33,100)]})
# Four barrels arranged in a true 2x2 cross-section perpendicular to their axis.
tube('Rotating mount',(0,0,1.65),(0,0,1.81),1.17,n=32,bias=-1)
for x in (-.69,.69):
 box('Cradle support '+str(x),(x,.10,2.28),(.28,1.08,1.02),bias=0)
 tube('Trunnion '+str(x),(x-.18,-.02,2.95),(x+.18,-.02,2.95),.43,n=24,bias=1)
box('Receiver',(0,.13,3.08),(1.22,1.34,1.10),bias=-1)
box('Receiver upper armor',(0,.21,3.66),(1.40,1.26,.18),bias=0)
if args.iteration>=2:
 for side in (-1,1):
  tube('Trunnion dark inset '+str(side),(side*.89,-.02,2.95),(side*.94,-.02,2.95),.30,n=24,bias=-2)
  tube('Trunnion axle cap '+str(side),(side*.95,-.02,2.95),(side*.99,-.02,2.95),.17,n=16,bias=1)
 for yy in (-.25,.02,.29,.56):box('Receiver roof rib',(0,yy,3.78),(1.10,.09,.08),bias=1)
 # Small individual fasteners rather than projected photo fragments on the receiver.
 for xx in (-.42,.42):
  box('Rear access plate',(xx,.815,3.08),(.30,.045,.57),bias=-2)
  for zz in (2.9,3.25):tube('Access fastener',(xx,.84,zz),(xx,.87,zz),.04,n=8,bias=1)
for side in (-1,1):
 box('Side feed box '+str(side),(side*.91,.28,3.0),(.43,.85,.67),bias=-1)
 for yy in (-.02,.18,.38,.58):box('Feed cooling rib',(side*1.14,yy,3.04),(.055,.07,.48),bias=1)
axis=Vector((0,-math.cos(math.radians(40)),math.sin(math.radians(40))));u=Vector((1,0,0));v=axis.cross(u);root=Vector((0,-.30,3.20))
for i,(xx,zz) in enumerate(((-.40,-.30),(.40,-.30),(-.40,.30),(.40,.30))):
 r=root+u*xx+v*zz
 tube('Barrel receiver '+str(i),r-axis*.25,r+axis*.55,.20,n=16,bias=-1)
 tube('Hollow flak barrel '+str(i),r+axis*.48,r+axis*4.10,.115,inner=.065,n=16,bias=0)
 for distance in (.60,2.0,3.92):tube(f'Barrel collar {i} {distance}',r+axis*distance,r+axis*(distance+.09),.145,inner=.105,n=16,bias=1)
# Paired return braces and rear optical/identification mast.
for x in (-.46,.46):tube('Return linkage '+str(x),(x,.53,1.85),(x,.74,2.89),.075,n=12,bias=1)
tube('Flag mast',(.40,1.0,3.37),(.40,1.0,5.10),.027,n=8,bias=1)
mesh('Source blue pennant',[(.43,1.0,5.10),(.94,1.0,5.10),(.94,1.0,4.69),(.43,1.0,4.72)],[(0,1,2,3)],uv={0:[(82,7),(92,8),(92,17),(82,16)]})
if args.iteration>=3:
 sys.path.insert(0,str(Path(__file__).resolve().parent))
 from rebuild_allied_flak import rebuild
 rebuild(globals())
if args.iteration>=8:
 from detail_allied_flak import apply
 apply(globals())
if args.iteration>=14:
 from refine_allied_flak_source_base import apply as refine_source_mount
 refine_source_mount(globals())
# Native orthographic projection (20 px/model-unit), ground registration (128,162).
target=Vector((0,0,34/(20*math.sqrt(3)/2)));bpy.ops.object.camera_add();cam=bpy.context.object;cam.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=12.8;s.camera=cam
hold=bpy.data.materials.new('EXPORT holdout');hold.use_nodes=True;n=hold.node_tree.nodes;n.clear();h=n.new('ShaderNodeHoldout');out=n.new('ShaderNodeOutputMaterial');hold.node_tree.links.new(h.outputs[0],out.inputs[0])
def light():
 light=Vector((-.25,-.50,.85)).normalized()
 for o in objects:
  for f in o.data.polygons:
   f.material_index=6 if f.index in o['textured'] else o.get('family_base',0)+max(0,min(5,int(((o.matrix_world.to_3x3()@f.normal).normalized().dot(light)+1)*2.7)+o['bias']))
bpy.context.view_layer.update();light();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'allied-flak-model.blend'))
id_mats={}
if args.iteration>=4:
 for i,o in enumerate(objects,1):
  assert i<(65536 if args.iteration>=8 else 256)
  m=bpy.data.materials.new('Surface '+str(i));m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(lin(i%256),lin(i//256),0,1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);id_mats[o.name]=m

def render(name,i=0,part='turret',mask=False):
 pivot.rotation_euler.z=i*math.pi/16;bpy.context.view_layer.update();light()
 for o in objects:
  o.hide_render=(part=='base' and not o['fixed']) or (args.iteration>=14 and part=='turret' and o['fixed'])
  for j,m in enumerate(mats):o.data.materials[j]=hold if part=='turret' and o['fixed'] else id_mats[o.name] if mask else m
 s.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
render('base-raw.png',part='base');render('base-control-raw.png',11,'base');render('assembly-reference-raw.png',part='assembly')
if args.iteration>=5:
 render('base-ids.png',part='base',mask=True);render('base-control-ids.png',11,'base',mask=True)
for i in range(32) if args.all else range(0,32,4):
 render(f'turret-{i:03d}-raw.png',i)
 if args.iteration>=4:render(f'turret-{i:03d}-ids.png',i,mask=True)
for i in (4,12,20,28):render(f'assembly-control-{i:03d}-raw.png',i,'assembly')
(OUT/'geometry.json').write_text(json.dumps({'kind':'allied-flak','surface_id_channels':2 if args.iteration>=8 else 1,'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'frame_size':[256,256],'native_density':20,'runtime_scale':1,'ground_origin_frame':[128,162],'pivot_world':list(pivot.location),'facings':32 if args.all else 8,'fixed_meshes':sum(o['fixed'] for o in objects),'rotating_meshes':sum(not o['fixed'] for o in objects),'hollow_barrels':4,'game_installed':False,'limitations':['first reconstruction, not approved','hidden faces inferred','no firing animation or actor binding']},indent=2)+'\n');print('ALLIED_FLAK_RENDERED',args.iteration)
