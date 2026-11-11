"""Camera-calibrated solid reconstruction, with stationary base / turret separation.
Real extruded profiles and horizontal decks; no billboard and no generated wear.
The observed surfaces use nearest source-projected UVs. Hidden sides are inferred.
"""
from pathlib import Path
import bpy,bmesh,math,json,hashlib,sys,argparse,numpy as np
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon'
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,default=1);p.add_argument('--rotation',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
OUT=BASE/f'attempt-03/pass-{args.iteration:02d}';OUT.mkdir(parents=True,exist_ok=True)
source=BASE/'heavy blaster 3.png';digest=hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene
s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=320;s.render.resolution_percentage=100
s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True;s.render.dither_intensity=0;s.render.filter_size=.01;s.eevee.taa_render_samples=1;s.eevee.use_gtao=False
s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=0;s.view_settings.gamma=1
stationary=bpy.data.collections.new('STATIONARY BASE');moving=bpy.data.collections.new('ROTATING TURRET');s.collection.children.link(stationary);s.collection.children.link(moving)
pivot=bpy.data.objects.new('turret_pivot',None);moving.objects.link(pivot);pivot.location=(-.125,.2,.70)
image=bpy.data.images.load(str(source));image.pack();materials={};records=[]
def material(name,rgb=None):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();links=m.node_tree.links;out=n.new('ShaderNodeOutputMaterial');em=n.new('ShaderNodeEmission')
 if rgb is None:
  tex=n.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest';tex.extension='CLIP'
  if args.iteration<4:
   links.new(tex.outputs['Color'],em.inputs[0]);transparent=n.new('ShaderNodeBsdfTransparent');mix=n.new('ShaderNodeMixShader');links.new(tex.outputs['Alpha'],mix.inputs[0]);links.new(transparent.outputs[0],mix.inputs[1]);links.new(em.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],out.inputs[0]);m.blend_method='CLIP';m.alpha_threshold=.5
  else:
   # A missing source sample must not drill an alpha hole through a solid mesh.
   fallback=json.loads((BASE/'attempt-01/source-samples.json').read_text())['112,150'][:3]
   linear=[v/255/12.92 if v/255<=.04045 else ((v/255+.055)/1.055)**2.4 for v in fallback]
   mix=n.new('ShaderNodeMixRGB');mix.name='source sample or dark source backing';mix.inputs[1].default_value=(*linear,1);links.new(tex.outputs['Alpha'],mix.inputs[0]);links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);links.new(em.outputs[0],out.inputs[0])
 else:
  def lin(c):
   c=c/255;return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
  em.inputs[0].default_value=(*map(lin,rgb),1);links.new(em.outputs[0],out.inputs[0])
 materials[name]=m;return m
material('reference nearest')
# Flat colors are sampled from the actual source by prepare step, never noise.
palette=json.loads((BASE/'attempt-01/source-samples.json').read_text())
for name,key in [('steel','155,184'),('dark','112,150'),('tan','153,91'),('copper','111,137')]:material(name,palette[key][:3])
K=40.;C=math.sqrt(3)/2
# Source-image calibration (256px coordinates); camera uses the exact inverse.
def screen(v):
 x,y,z=v;return (128+K*(x+y)/math.sqrt(2),176+K*((x-y)/math.sqrt(8)-z*C))
def side(u,v,x):
 y=math.sqrt(2)*(u-128)/K-x;z=((x-y)/math.sqrt(8)-(v-176)/K)/C;return (x,y,z)
def floor(u,v,z):
 a=math.sqrt(2)*(u-128)/K;b=math.sqrt(8)*((v-176)/K+z*C);return ((a+b)/2,(a-b)/2,z)
def mesh(name,verts,faces,rotating=True,face_uv=None,flat=None):
 data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update();o=bpy.data.objects.new(name,data);(moving if rotating else stationary).objects.link(o)
 if rotating:
  o.parent=pivot;o.location=-pivot.location # input vertices are calibrated world coordinates
 data.materials.append(materials['reference nearest']);data.materials.append(materials[flat or 'dark']);uv=data.uv_layers.new()
 for i,f in enumerate(data.polygons):
  pixels=face_uv.get(i) if face_uv else None
  if pixels is False:f.material_index=1;continue
  if pixels is None:pixels=[screen(verts[j]) for j in f.vertices]
  for loop,(u,v) in zip(f.loop_indices,pixels):uv.data[loop].uv=(u/256,1-v/256)
 records.append({'name':name,'stationary':not rotating,'vertices':len(verts),'faces':len(faces),'uv':'reference camera projection; mirrored hidden profile side where noted'})
 return o

def profile(name,outline,near=.25,width=.75,rotating=True):
 front=[side(u,v,near) for u,v in outline];back=[(x-width,y,z) for x,y,z in front];n=len(front)
 faces=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,n+i,n+(i+1)%n,(i+1)%n) for i in range(n)]
 # Unseen side repeats the observed side texture, explicitly not source-verified.
 return mesh(name,front+back,faces,rotating,{1:list(reversed(outline))})

def front_profile(name,outline,y=-.70,depth=.18,rotating=True):
 def front(u,v):
  x=math.sqrt(2)*(u-128)/K-y;z=((x-y)/math.sqrt(8)-(v-176)/K)/C;return (x,y,z)
 front=[front(u,v) for u,v in outline];back=[(x,y+depth,z) for x,y,z in front];n=len(front)
 return mesh(name,front+back,[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,n+i,n+(i+1)%n,(i+1)%n) for i in range(n)],rotating,{1:list(reversed(outline))})

def mirror(obj,name):
 data=obj.data.copy();o=bpy.data.objects.new(name,data);moving.objects.link(o);o.parent=pivot;o.location=-pivot.location
 for vert in data.vertices:vert.co.x=-.25-vert.co.x
 return o

def deck(name,outline,z,thickness,rotating=False,texture_top=True):
 top=[floor(u,v,z) for u,v in outline];bottom=[(x,y,z-thickness) for x,y,z in top];n=len(top)
 faces=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,n+i,n+(i+1)%n,(i+1)%n) for i in range(n)]
 return mesh(name,top+bottom,faces,rotating,{0:None if texture_top else False,1:False},'steel')

def patch(name,outline,z):
 return mesh(name,[floor(u,v,z) for u,v in outline],[tuple(range(len(outline)))],False)
# Stationary plate, bevel/armor, no source turret baked into the deck.
base_outline=[(42,163),(149,110),(159,110),(240,143),(240,151),(141,200),(125,200),(42,170)]
deck('base armored plinth',base_outline,.65,.65,texture_top=False)
# Observed deck regions only. Occluded center remains a clean source-color deck.
patch('base left visible plating',[(43,162),(58,153),(77,155),(94,164),(102,172),(83,177),(59,170)],.653)
patch('base right visible plating',[(186,128),(193,126),(233,142),(234,150),(186,174),(164,174),(183,160)],.653)
if args.iteration>=2:
 patch('base front visible bevel and rivets',[(78,173),(97,175),(123,180),(143,178),(169,166),(188,153),(194,157),(184,174),(142,200),(125,200),(80,182)],.654)
# Small raised edge plates, with their own source projection; not tiny repeated tiles.
for name,poly in [('front gear housing',[(58,176),(77,182),(86,194),(85,213),(63,203),(56,193)]),('front right pipe block',[(102,193),(122,201),(122,220),(103,212)]),('side pipe housing',[(181,183),(196,176),(198,196),(183,204)])]:
 profile(name,poly,near=.05,width=.12,rotating=False)
# Socket ring is stationary; it must not turn with the upper assembly.
def ring(name,center,rx,ry,z,depth,rotating=False):
 n=48;verts=[]
 for zz in (z,z-depth):
  verts.extend([(center[0]+rx*math.cos(i*2*math.pi/n),center[1]+ry*math.sin(i*2*math.pi/n),zz) for i in range(n)])
 faces=[tuple(range(n))]+[(i,n+i,n+(i+1)%n,(i+1)%n) for i in range(n)]
 return mesh(name,verts,faces,rotating,{0:False},'dark')
ring('fixed bearing socket',(-.125,.2),1.28,1.28,.77,.14)
# Low rotating cross-brace, with panel seams copied from the observed support deck.
if args.iteration==1:
 profile('lower cross brace',[(83,139),(121,153),(141,154),(142,164),(127,167),(87,151)],near=.48,width=1.30)
else:
 front_profile('lower cross brace',[(83,139),(121,153),(137,153),(140,162),(127,167),(87,151)])
# A real closed gun housing, not V1's two widely separated open rails.
# The channel/bevels are separate small solids above this main enclosure.
gun=[(39,128),(90,102),(107,96),(113,82),(130,74),(137,62),(174,44),(198,34),(210,38),(217,48),(218,70),(214,86),(205,94),(179,108),(163,113),(146,119),(124,130),(114,124),(103,137),(47,164),(42,162)]
profile('closed barrel and receiver housing',gun)
# Near armor splits have their own thickness, rather than putting all detail on
# a generic rectangular block. The silhouette and panel edges follow the image.
profile('barrel near upper armor',[(40,127),(92,102),(99,91),(106,95),(110,104),(104,115),(45,146),(41,145)],near=.275,width=.15)
profile('barrel lower copper armor',[(44,148),(104,118),(112,123),(105,136),(48,164),(44,162)],near=.285,width=.12)
profile('side socket armored badge',[(112,101),(135,90),(144,92),(145,111),(137,120),(121,129),(113,124),(109,112)],near=.34,width=.14)
profile('receiver raised window strip',[(177,55),(207,39),(215,45),(216,58),(178,77)],near=.29,width=.10)
# Roof channel and pale armored shoulders: shallow stepped actual geometry.
profile('roof conduit casing',[(91,94),(88,88),(99,78),(110,74),(117,58),(128,62),(121,80),(106,86),(97,96)],near=-.18,width=.18)
# Two separated structural arms (not a large filled A-shaped slab).
support=[(124,162),(121,145),(130,127),(145,103),(151,91),(163,91),(172,101),(171,115),(158,136),(149,157),(136,165)]
arm=profile('near trunnion arm',support,near=.56 if args.iteration==1 else .72,width=.20)
# Mirrored physical arm, not a source-camera billboard.
mirror(arm,'far trunnion arm')
cap=profile('trunnion cap',[(157,96),(163,93),(170,97),(175,106),(174,113),(168,119),(162,116),(157,108)],near=.60 if args.iteration==1 else .77,width=.075)
mirror(cap,'far trunnion cap')
profile('receiver side monitor',[(177,80),(204,67),(207,73),(207,94),(179,108),(176,100)],near=.40,width=.12)
# Source-shaped separate capacitors; metal caps and glow remain source pixels.
for name,poly in [('capacitor 1',[(148,140),(154,138),(156,140),(156,158),(151,161),(148,159)]),('capacitor 2',[(158,133),(164,131),(166,133),(166,151),(161,154),(158,152)]),('capacitor 3',[(169,128),(174,126),(177,128),(177,146),(172,149),(169,147)])]:
 profile(name,poly,near=.63 if args.iteration==1 else .85,width=.12)
# Recessed source-shaped muzzle face (the housing is genuinely thick).
# Its frontmost -Y end already contains the projected aperture; a thin outer rim
# follows that end. No front-facing camera billboard is used.
# Iteration adjustments are retained in separate output directories.
if args.iteration>=3:
 # Remove the support/monitor silhouette baked into the single-view housing UV.
 # Those details are actual separate objects; hidden housing gets clean source
 # material, with visible armor panels added separately and mirrored physically.
 housing=bpy.data.objects['closed barrel and receiver housing']
 housing.data.materials[1]=materials['copper']
 housing.data.polygons[0].material_index=1;housing.data.polygons[1].material_index=1
 profile('receiver sloped vent armor',[(137,63),(174,45),(178,76),(154,89),(145,105),(136,100)],near=.265,width=.08)
 for name in ('barrel near upper armor','barrel lower copper armor','side socket armored badge','receiver raised window strip','receiver sloped vent armor','receiver side monitor'):
  if args.iteration>=4 and name=='barrel near upper armor':continue
  mirror(bpy.data.objects[name],'inferred opposite '+name)
if args.iteration>=4:
 # Retain the visible source housing detail, masking only independently modeled
 # foreground parts. The mask contains no RGB painting or invented weathering.
 occluders=[support,[(154,91),(170,93),(177,105),(175,119),(161,121),(154,108)],[(175,79),(206,65),(210,73),(210,95),(178,111),(173,101)]]
 yy,xx=np.mgrid[0:256,0:256];xx=xx+.5;yy=yy+.5;hidden=np.zeros((256,256),bool)
 for polygon in occluders:
  inside=np.zeros((256,256),bool)
  for (x1,y1),(x2,y2) in zip(polygon,polygon[1:]+polygon[:1]):
   if y1!=y2:inside^=((y1>yy)!=(y2>yy))&(xx<(x2-x1)*(yy-y1)/(y2-y1)+x1)
  hidden|=inside
 rgba=np.ones((256,256,4),dtype=np.float32);rgba[:,:,:3]=(~hidden)[:,:,None]
 mask=bpy.data.images.new('source housing occlusion mask',256,256,alpha=True);mask.colorspace_settings.name='Non-Color';mask.pixels=rgba[::-1].ravel();mask.filepath_raw=str(OUT/'housing-occlusion-mask.png');mask.file_format='PNG';mask.save();mask.pack()
 clean=materials['reference nearest'].copy();clean.name='housing source without foreground objects';n=clean.node_tree.nodes;links=clean.node_tree.links
 tex=next(node for node in n if node.type=='TEX_IMAGE');masknode=n.new('ShaderNodeTexImage');masknode.image=mask;masknode.interpolation='Closest'
 blend=next(node for node in n if node.name=='source sample or dark source backing');multiply=n.new('ShaderNodeMath');multiply.operation='MULTIPLY';links.new(tex.outputs['Alpha'],multiply.inputs[0]);links.new(masknode.outputs['Color'],multiply.inputs[1]);links.new(multiply.outputs[0],blend.inputs[0])
 # Reuse the exact source copper material color in hidden housing regions.
 color=next(node for node in materials['copper'].node_tree.nodes if node.type=='EMISSION').inputs[0].default_value
 blend.inputs[1].default_value=color
 housing.data.materials[0]=clean;housing.data.polygons[0].material_index=0;housing.data.polygons[1].material_index=0
 # Turn hidden lower/back edges into clean material surfaces, instead of
 # projecting unrelated foreground/background image pixels onto them.
 view=Vector((1,-1,math.sqrt(2/3))).normalized()
 for o in list(moving.objects):
  if o.type!='MESH':continue
  center=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
  for face in o.data.polygons:
   if len(face.vertices)!=4:continue
   normal=face.normal.copy()
   if normal.dot(face.center-center)<0:normal=-normal
   if normal.dot(view)<-.15:face.material_index=1
if args.iteration>=5:
 # The unseen rear needs actual service-panel detail, not the large flat copper
 # patch exposed by the rotation review. Reuse a source vent plate explicitly;
 # this is inferred rear design, not a claim that the reference shows the rear.
 housing=bpy.data.objects['closed barrel and receiver housing'];housing.data.materials.append(materials['reference nearest']);idx=len(housing.data.materials)-1
 for face_index,pixels in ((11,[(144,70),(172,56),(177,74),(150,88)]),(12,[(180,183),(193,177),(196,194),(183,201)])):
  face=housing.data.polygons[face_index];face.material_index=idx
  for loop,(u,v) in zip(face.loop_indices,pixels):housing.data.uv_layers.active.data[loop].uv=(u/256,1-v/256)
 # True inset front aperture: four collar strips and a slightly recessed face.
 # Main-view color still comes from the supplied muzzle, with no fake glow.
 bm=bmesh.new();bm.from_mesh(housing.data);bm.faces.ensure_lookup_table();bm.faces.remove(bm.faces[len(gun)+1]);bm.to_mesh(housing.data);bm.free();housing.data.update()
 x0,x1=-.50,.25;y=-3.405;z0,z1=1.975,2.87;t=.045
 mesh('muzzle recessed emitter',[(x0+t,y+.065,z1-t),(x1-t,y+.065,z1-t),(x1-t,y+.065,z0+t),(x0+t,y+.065,z0+t)],[(0,1,2,3)])
 for name,verts in [('left',[(x0,y,z1),(x0+t,y,z1),(x0+t,y,z0),(x0,y,z0)]),('right',[(x1-t,y,z1),(x1,y,z1),(x1,y,z0),(x1-t,y,z0)]),('upper',[(x0,y,z1),(x1,y,z1),(x1,y,z1-t),(x0,y,z1-t)]),('lower',[(x0,y,z0+t),(x1,y,z0+t),(x1,y,z0),(x0,y,z0)])]:
  mesh('muzzle collar '+name,verts,[(0,1,2,3)])
 opening=[(x0+t,y,z1-t),(x1-t,y,z1-t),(x1-t,y,z0+t),(x0+t,y,z0+t)]
 for i in range(4):
  a=opening[i];b=opening[(i+1)%4];mesh('muzzle inner wall '+str(i),[a,b,(b[0],y+.065,b[2]),(a[0],y+.065,a[2])],[(0,1,2,3)])
# Camera calibration: source (128,176) is world origin; +32px study padding.
target=Vector((0,0,48/(K*C)));bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='calibrated reference camera';cam.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=8;s.camera=cam
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'heavy-blaster-v3.blend'))
objects=[o for o in s.objects if o.type=='MESH']
def render(name,size,part='assembly',angle=0):
 pivot.rotation_euler.z=math.radians(angle)
 for o in objects:o.hide_render=(part=='base' and o.parent==pivot) or (part=='turret' and o.parent!=pivot)
 bpy.context.view_layer.update();s.render.resolution_x=s.render.resolution_y=size;s.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
for part in ('assembly','base','turret'):render(f'{part}-320-raw.png',320,part)
render('assembly-160-raw.png',160)
render('base-160-raw.png',160,'base')
for angle in (0,45,90,180,270,315):render(f'turret-{angle:03d}-160-raw.png',160,'turret',angle)
if args.rotation:
 for i in range(32):render(f'rotation-{i:03d}-raw.png',160,'turret',i*11.25)
px,py=screen(pivot.location)
(OUT/'geometry-provenance.json').write_text(json.dumps({'source_sha256':digest,'method':'calibrated solid profiles with source-projected nearest UVs; hidden profile sides reused/inferred','iteration':args.iteration,'camera_pixels_per_world_unit_study':40,'camera_ortho':8,'study_frame':320,'native_frame':160,'pivot_world':list(pivot.location),'pivot_native':[(px+32)/2,(py+32)/2],'base_stationary':True,'export_parts':['base','turret'],'mesh_objects':records,'limitations':['single-view texture projection is not independent multi-view accuracy evidence','hidden surfaces inferred from visible source faces','no game actor or weapon binding yet']},indent=2)+'\n')
assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
print('HEAVY_BLASTER_V3_PASS_COMPLETE',args.iteration)
