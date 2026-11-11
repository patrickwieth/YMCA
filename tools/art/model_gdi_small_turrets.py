"""Separate source-textured AA/Sonic 3D studies. Native 192px, no game installs.
Background Blender only. Sources and prior passes are preserved.
"""
from pathlib import Path
import bpy,math,json,hashlib,argparse,sys
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
p=argparse.ArgumentParser();p.add_argument('--kind',choices=('aa','sonic'),required=True);p.add_argument('--pass',dest='iteration',type=int,default=1);p.add_argument('--all',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
ROOT=Path(__file__).resolve().parents[2];FOLDER=ROOT/'art-source'/('gdi aa gun' if args.kind=='aa' else 'gdi sonic turret');SOURCE=next(FOLDER.glob('*.png'));OUT=FOLDER/f'pass-{args.iteration:02d}';OUT.mkdir(exist_ok=True);K=20.;C=math.sqrt(3)/2;ORIGIN=104 if args.kind=='aa' else 116;SIZE=224 if args.kind=='sonic' and args.iteration>=2 else 192;PAD=(SIZE-128)//2
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=SIZE;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True;s.render.dither_intensity=0;s.render.filter_size=.01;s.eevee.taa_render_samples=1;s.eevee.use_gtao=False;s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=0;s.view_settings.gamma=1
moving=bpy.data.collections.new('ROTATING TURRET');fixed=bpy.data.collections.new('STATIONARY BASE');s.collection.children.link(moving);s.collection.children.link(fixed);pivot=bpy.data.objects.new('turret_pivot',None);moving.objects.link(pivot);pivot.location=(0,0,2.2 if args.kind=='aa' else 2.35)
image=bpy.data.images.load(str(SOURCE));image.pack();rgba=list(image.pixels[:]);colors=set()
for i in range(0,len(rgba),4):
 if rgba[i+3]>.99:colors.add(tuple(round(v*255) for v in rgba[i:i+3]))
# Actual source colors only, not newly synthesized texture colors.
def nearest(rgb):return min(colors,key=lambda c:sum((c[i]-rgb[i])**2 for i in range(3)))
def lin(c):
 c=c/255;return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
materials={};role_colors={}
for name,rgb in {'gold':(193,144,59),'lightgold':(239,202,106),'goldshadow':(130,87,44),'steel':(91,106,114),'lightsteel':(151,179,181),'dark':(25,29,36),'black':(8,11,14),'cyan':(30,239,239),'lightcyan':(173,251,251),'blue':(30,125,170)}.items():
 col=nearest(rgb);role_colors[name]=col;m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');em.inputs[0].default_value=(*map(lin,col),1);m.node_tree.links.new(em.outputs[0],out.inputs[0]);materials[name]=m
# AA firing flash must never become a fixed part of idle texture.
if args.kind=='aa':
 teximage=image.copy();teximage.name='SOURCE CUT / flash excluded';pix=list(teximage.pixels[:])
 for y in range(128):
  for x in range(128):
   if x>=88 and y<=27:pix[((127-y)*128+x)*4+3]=0
 teximage.pixels[:]=pix;teximage.pack()
else:teximage=image
for fallback in ('gold','steel','dark'):
 m=bpy.data.materials.new('source nearest / '+fallback);m.use_nodes=True;n=m.node_tree.nodes;n.clear();tex=n.new('ShaderNodeTexImage');tex.image=teximage;tex.interpolation='Closest';tex.extension='CLIP';mix=n.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(*map(lin,role_colors[fallback]),1);em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');links=m.node_tree.links;links.new(tex.outputs['Alpha'],mix.inputs[0]);links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);links.new(em.outputs[0],out.inputs[0]);materials['source '+fallback]=m
records=[]
def screen(v):
 x,y,z=v;return (64+K*(x+y)/math.sqrt(2),ORIGIN+K*((x-y)/math.sqrt(8)-z*C))
def invside(u,v,y):
 x=math.sqrt(2)*(u-64)/K-y;z=((x-y)/math.sqrt(8)-(v-ORIGIN)/K)/C;return (x,y,z)
def floor(u,v,z):
 a=(u-64)*math.sqrt(2)/K;b=math.sqrt(8)*((v-ORIGIN)/K+z*C);return Vector(((a+b)/2,(a-b)/2,z))
def mesh(name,verts,faces,rotating=True,material='source gold',face_uv=None,face_materials=None):
 data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update();o=bpy.data.objects.new(name,data);(moving if rotating else fixed).objects.link(o)
 if rotating:o.parent=pivot;o.location=-pivot.location
 for m in materials.values():data.materials.append(m)
 names=list(materials);uv=data.uv_layers.new()
 for poly in data.polygons:
  mat=face_materials.get(poly.index,material) if face_materials else material;poly.material_index=names.index(mat)
  pixels=face_uv.get(poly.index) if face_uv else None
  if pixels is None:
   pixels=[]
   for vi in poly.vertices:
    x,y,z=verts[vi]
    # Hidden round surfaces reuse the observed near-side texture, rather than
    # baking a reference-camera billboard. Rear details remain inferred.
    if poly.normal.dot(Vector((1,-1,0)))<-.05 and not name.startswith('SONIC receiver'):
     x,y=y,x
    u,v=screen((x,y,z))
    if args.kind=='sonic' and u>125 and v<84:u=238-u
    if args.iteration>=3 and name.startswith('SONIC receiver') and (poly.normal.z<-.35 and poly.center.z>5.1 if args.iteration>=4 else poly.normal.z>.35):
     xx=min(1,max(0,(verts[vi][0]+3.29)/4.67));yy=min(1,max(0,(verts[vi][1]+.6)/1.6));corners=((19,14),(48,26),(69,15),(39,3));u=(1-xx)*(1-yy)*corners[0][0]+xx*(1-yy)*corners[1][0]+xx*yy*corners[2][0]+(1-xx)*yy*corners[3][0];v=(1-xx)*(1-yy)*corners[0][1]+xx*(1-yy)*corners[1][1]+xx*yy*corners[2][1]+(1-xx)*yy*corners[3][1]
    if args.iteration>=2 and name.startswith('AA rounded') and poly.normal.dot(Vector((1,-1,0)))<-.05:
     theta=math.atan2(verts[vi][1],verts[vi][0]);u=45+14*((theta-math.pi/4)%(2*math.pi))/math.pi;u=min(59,max(45,u));v=68-22*(z-2.2)/1.78
    pixels.append((u,v))
  for loop,(u,v) in zip(poly.loop_indices,pixels):uv.data[loop].uv=(u/128,1-v/128)
 records.append({'name':name,'stationary':not rotating,'vertices':len(verts),'faces':len(faces)});return o

def rings(name,levels,rotating=True,n=24,material='source gold',cap='dark'):
 verts=[(r*math.cos(i*2*math.pi/n),r*math.sin(i*2*math.pi/n),z) for r,z in levels for i in range(n)];faces=[];fm={}
 for j in range(len(levels)-1):
  for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 faces.extend([tuple(range(n-1,-1,-1)),tuple((len(levels)-1)*n+i for i in range(n))]);fm[len(faces)-1]=cap;fm[len(faces)-2]='dark';return mesh(name,verts,faces,rotating,material,face_materials=fm)

def cylinder(name,a,b,r,rotating=True,material='steel',n=20,cap='dark',inner=0,project_caps=False):
 a=Vector(a);b=Vector(b);axis=(b-a).normalized();ref=Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0));u=axis.cross(ref).normalized();v=axis.cross(u).normalized();verts=[]
 for c,rr in ((a,r),(b,r)) + (((a,inner),(b,inner)) if inner else ()):
  verts += [tuple(c+rr*(u*math.cos(i*2*math.pi/n)+v*math.sin(i*2*math.pi/n))) for i in range(n)]
 faces=[];fm={}
 for i in range(n):
  faces.append((i,(i+1)%n,n+(i+1)%n,n+i));fm[len(faces)-1]=material
  if args.iteration>=2 and name.startswith('AA hollow barrel'):
   norm=u*math.cos((i+.5)*2*math.pi/n)+v*math.sin((i+.5)*2*math.pi/n);lit=norm.dot(Vector((.4,-.5,.75)).normalized());fm[len(faces)-1]=('steel' if lit>.6 else ('dark' if lit>-.3 else 'black')) if args.iteration>=4 else ('lightsteel' if lit>.4 else ('steel' if lit>-.2 else 'dark'))
 if inner:
  for i in range(n):
   j=(i+1)%n;faces.extend([(n+i,n+j,3*n+j,3*n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)])
   fm[len(faces)-3]=cap;fm[len(faces)-2]='steel' if args.iteration>=2 and name.startswith('SONIC') and not (args.iteration>=4 and name.startswith('SONIC rear')) else cap;fm[len(faces)-1]='dark'
 else:
  faces.extend([tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))]);fm[len(faces)-1]=cap;fm[len(faces)-2]=cap
 obj=mesh(name,verts,faces,rotating,material,face_materials=fm)
 if args.iteration>=2 and name.startswith(('SONIC wave emitter','SONIC outer rim')):
  # Front ring atlas, not camera projection of the foreground lens onto every ring.
  canonical=Vector((-1.44,.1,4.50))+Vector((1,0,.2)).normalized()*5.25;center=(a+b)*.5
  for poly in obj.data.polygons:
   if poly.normal.dot(axis)<.9:continue
   for loop in poly.loop_indices:
    pos=obj.data.vertices[obj.data.loops[loop].vertex_index].co-center;pos-=axis*pos.dot(axis);uu,vv=screen(canonical+pos*(2/r))
    if uu>125:uu=238-uu
    obj.data.uv_layers.active.data[loop].uv=(uu/128,1-vv/128)
 return obj

def box(name,center,size,rotating=True,material='source gold'):
 x,y,z=center;a,b,c=[v/2 for v in size];verts=[(x+xx*a,y+yy*b,z+zz*c) for zz in (-1,1) for yy in (-1,1) for xx in (-1,1)];return mesh(name,verts,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],rotating,material)

def profile(name,outline,near,width,material='source gold'):
 front=[invside(u,v,near) for u,v in outline];back=[(x,y+width,z) for x,y,z in front];n=len(front);faces=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,n+i,n+(i+1)%n,(i+1)%n) for i in range(n)];return mesh(name,front+back,faces,material=material,face_uv={0:outline,1:list(reversed(outline))})

def patch_on_cylinder(name,theta,r,z,w,h,source_quad,rotating=False):
 theta=math.radians(theta);normal=Vector((math.cos(theta),math.sin(theta),0));tangent=Vector((-math.sin(theta),math.cos(theta),0));c=normal*r+Vector((0,0,z));verts=[tuple(c+tangent*x+Vector((0,0,zz))) for x,zz in ((-w/2,h/2),(w/2,h/2),(w/2,-h/2),(-w/2,-h/2))];return mesh(name,verts,[(0,1,2,3)],rotating,face_uv={0:source_quad})

if args.kind=='aa':
 # The foundation and its vents never rotate.
 h=1.72;outline=[(-h,-h+.2),(-h+.2,-h),(h-.2,-h),(h,-h+.2),(h,h-.2),(h-.2,h),(-h+.2,h),(-h,h-.2)]
 verts=[(x,y,z) for z in (.08,.23) for x,y in outline];mesh('AA fixed octagonal ground plate',verts,[tuple(range(7,-1,-1)),tuple(range(8,16))]+[(i,(i+1)%8,8+(i+1)%8,8+i) for i in range(8)],False,'source steel',face_materials={1:'source steel' if args.iteration>=2 else 'steel'})
 rings('AA fixed armored pedestal',[(1.72,.28),(1.90,.44),(1.90,1.60),(1.72,1.91)],False,n=12,cap='gold')
 rings('AA stationary bearing',[(1.61,1.92),(1.65,2.08),(1.59,2.23)],False,n=32,material='source steel',cap='dark')
 for theta in (-90,0,90,180):patch_on_cylinder('AA pedestal vent '+str(theta),theta,1.91,.94,.48,.88,[(35,95),(41,96),(41,110),(35,109)])
 # Lathed closed helmet with a real aperture for the elevated rotary gun.
 n=32;levels=[(1.50,2.20),(1.53,2.55),(1.42,3.00),(1.13,3.50),(.62,3.86),(.08,3.98)];verts=[(r*math.cos(i*2*math.pi/n),r*math.sin(i*2*math.pi/n),z) for r,z in levels for i in range(n)];faces=[]
 for j in range(len(levels)-1):
  for i in range(n):
   angle=(i+.5)*360/n
   if (67<angle<113 and 1<=j<4) if args.iteration>=2 else (52<angle<128 and j<4):continue
   faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 faces.append(tuple((len(levels)-1)*n+i for i in range(n)));mesh('AA rounded turret armor with gun aperture',verts,faces)
 box('AA inner gun cradle',(0,.88,2.95),(.72,.75,.8),material='dark')
 axis=Vector((0,math.cos(math.radians(25)),math.sin(math.radians(25))));root=Vector((0,.72,2.92));end=root+axis*(2.10 if args.iteration>=2 else 1.92);u=Vector((1,0,0));v=axis.cross(u).normalized()
 cylinder('AA rotary receiver',root-axis*.22,root+axis*.35,.30,material='source steel',cap='dark')
 for j in range(6):
  off=(.19 if args.iteration>=2 else .18)*(u*math.cos(j*math.pi/3)+v*math.sin(j*math.pi/3));cylinder('AA hollow barrel '+str(j),root+axis*.18+off,end+off,.06 if args.iteration>=2 else .085,material='steel',n=12,cap='lightsteel',inner=.035 if args.iteration>=2 else .052)
 if args.iteration>=2:cylinder('AA armored gun mantlet',root+axis*.25,root+axis*.55,.46,material='source steel',cap='dark')
 for t in ((.32,1.35,1.75) if args.iteration>=2 else (.32,1.55,1.86)):cylinder('AA barrel retaining collar '+str(t),root+axis*t,root+axis*(t+.075),.285,material='dark',cap='steel',inner=.22)
 cylinder('AA cradle hinge',(-.47,.77,2.91),(.47,.77,2.91),.25,material='steel',cap='source steel')
 sensor=floor(31,40,3.25);box('AA side optical rangefinder',sensor,(.38,.42,.50),material='source steel');box('AA rangefinder bracket',sensor+Vector((.15,0,-.65)),(.23,.33,.85),material='source steel')
 cylinder('AA thin antenna',(-1.6,0,2.4),(-1.6,0,4.18),.028,n=8,material='steel',cap='lightsteel')
 dish=floor(53,25,4.3);cylinder('AA radar mast',dish-Vector((0,0,.85)),dish,.065,n=8,material='dark');normal=Vector((.5,-.7,.4)).normalized();cylinder('AA radar dish',dish-normal*.06,dish+normal*.06,.29 if args.iteration>=3 else .36,n=20,material='steel',cap='source steel');cylinder('AA radar feed',dish+normal*.06,dish+normal*.24,.035,n=8,material='dark')
 if args.iteration>=3:
  lower=floor(56,30,3.95);cylinder('AA lower tracking reflector',lower-normal*.05,lower+normal*.05,.22,n=16,material='steel',cap='source steel')
else:
 rings('SONIC fixed ground plate',[(2.70,.08),(2.76,.22)],False,n=12,material='source steel',cap='steel')
 rings('SONIC fixed octagonal armor',[(2.18,.30),(2.42,.48),(2.42,1.62),(2.10,1.94)],False,n=12,cap='gold')
 rings('SONIC stationary turret bearing',[(1.58,1.96),(1.66,2.14),(1.56,2.35)],False,n=32,material='source steel',cap='dark')
 for theta in (-90,0,90,180):patch_on_cylinder('SONIC pedestal vent '+str(theta),theta,2.43,1.0,.75,1.03,[(10,91),(21,89),(21,113),(10,115)])
 outline=[(9,12),(22,4),(38,2),(58,6),(74,16),(75,32),(64,42),(57,55),(36,51),(23,42),(10,38)];profile('SONIC receiver closed source armor',outline,-.6 if args.iteration>=2 else -.45,1.6 if args.iteration>=2 else 1.12)
 arm=[(31,40),(38,33),(46,35),(51,43),(52,56),(60,73),(55,77),(45,74),(37,59),(31,52)];near=profile('SONIC near structural arm',arm,-.85 if args.iteration>=2 else -.78,.22,material='source steel');far=near.copy();far.data=near.data.copy();moving.objects.link(far);far.name='SONIC far structural arm'
 for vert in far.data.vertices:vert.co.y=.2-vert.co.y
 box('SONIC rotating root cross brace',(.2,.1,2.55) if args.iteration>=2 else (0,.1,2.49),(1.4,2.4,.40) if args.iteration>=2 else (1.4,1.9,.24),material='source steel')
 hinge=floor(40,45,4.05);cylinder('SONIC common support axle',(hinge.x,-.92,hinge.z),(hinge.x,1.12,hinge.z),.53,n=24,material='steel',cap='source steel')
 root=Vector((-1.44,.1,4.50));axis=Vector((1,0,.20)).normalized();tip=Vector((3.79,.1,5.546));cylinder('SONIC central barrel',root,root+axis*4.95 if args.iteration>=3 else tip,.88,n=32,material='source steel',cap='dark')
 for i,(distance,radius) in enumerate(((2.65,1.70),(3.90,1.88),(5.25,2.00))):
  center=root+axis*distance;cylinder('SONIC wave emitter collar '+str(i),center-axis*.11,center+axis*.11,radius,n=48,material='source steel',cap='source steel',inner=.77 if i==2 else 1.05)
  cylinder('SONIC outer rim '+str(i),center+axis*.105,center+axis*.15,radius+.045,n=48,material='lightsteel',cap='source steel',inner=radius-.075)
  if args.iteration>=3:
   # Inferred structural back: real concentric ribs and four radial braces,
   # instead of a flat grey disk or a copied cyan foreground lens.
   cylinder('SONIC rear reinforcement '+str(i),center-axis*.18,center-axis*.13,radius*.83,n=40,material='dark',cap='lightsteel',inner=radius*.83-.065)
   u=axis.cross(Vector((0,0,1))).normalized();v=axis.cross(u).normalized()
   for j in range(4):
    radial=u*math.cos(j*math.pi/2)+v*math.sin(j*math.pi/2);cylinder('SONIC rear radial brace '+str(i)+' '+str(j),center-axis*.16+radial*1.11,center-axis*.16+radial*(radius-.13),.045,n=8,material='dark' if args.iteration>=4 else 'steel',cap='lightsteel')
 # Actual recessed lens, not a flat glow painted on the outer armor.
 face=root+axis*5.25;cylinder('SONIC inner lens surround',face-axis*.17,face+axis*.13,.79,n=40,material='steel',cap='source steel',inner=.61)
 cylinder('SONIC recessed cyan lens',face-axis*.12,face-axis*.10,.61,n=40,material='cyan',cap='cyan' if args.iteration>=2 else 'source steel')
 if args.iteration>=2:
  cylinder('SONIC bright lens core',face-axis*.095,face-axis*.09,.38,n=32,material='lightcyan',cap='lightcyan')
 dish=floor(24,-2,6.15);cylinder('SONIC rear sensor mast',dish-Vector((0,0,.6)),dish,.07,n=8,material='steel');normal=Vector((.4,-.8,.3)).normalized();cylinder('SONIC rear sensor dish',dish-normal*.05,dish+normal*.05,.35,n=16,material='steel',cap='source steel')
if args.kind=='sonic' and args.iteration>=2:
 # Fit the completed cropped foundation inside the 1x1 ground diamond. Keep
 # bearing/support fit intact; only outer stationary pedestal and vent XY shrink.
 for obj in fixed.objects:
  if obj.type=='MESH' and 'bearing' not in obj.name:
   for vert in obj.data.vertices:vert.co.x*=.8;vert.co.y*=.8
   obj.data.update()
relight=None
if args.iteration>=6:
 sys.path.insert(0,str(Path(__file__).resolve().parent))
 from small_turret_rework import rebuild
 relight=rebuild(globals())
# Orthographic source calibration with extra native canvas padding, not scaling.
target=Vector((0,0,(ORIGIN-64)/(K*C)));bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='native source-calibrated camera';cam.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=SIZE/K;s.camera=cam
bpy.context.view_layer.update()
if args.iteration>=3:
 # Fit every yaw at unchanged pixels/world-unit. Increase transparent canvas,
 # never shrink an already rendered sprite to hide clipping.
 lo=[1e9,1e9];hi=[-1e9,-1e9]
 for i in range(32):
  pivot.rotation_euler.z=i*math.pi/16;bpy.context.view_layer.update()
  for obj in list(moving.objects)+list(fixed.objects):
   if obj.type!='MESH':continue
   for vert in obj.data.vertices:
    p=world_to_camera_view(s,cam,obj.matrix_world@vert.co);xy=(p.x*SIZE,(1-p.y)*SIZE)
    for j in range(2):lo[j]=min(lo[j],xy[j]);hi[j]=max(hi[j],xy[j])
 extra=max(0,6-min(lo),max(hi)-(SIZE-6))
 if extra>0:
  SIZE+=2*math.ceil(extra/16)*16;PAD=(SIZE-128)//2;s.render.resolution_x=s.render.resolution_y=SIZE;cam.data.ortho_scale=SIZE/K
 pivot.rotation_euler.z=0;bpy.context.view_layer.update()
if relight:relight()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(args.kind+'-source-model.blend')))
mov=[o for o in moving.objects if o.type=='MESH'];sta=[o for o in fixed.objects if o.type=='MESH']
original_base_materials={o.name:list(o.data.materials) for o in sta}
holdout=bpy.data.materials.new('EXPORT ONLY / stationary base occlusion');holdout.use_nodes=True;n=holdout.node_tree.nodes;n.clear();h=n.new('ShaderNodeHoldout');out=n.new('ShaderNodeOutputMaterial');holdout.node_tree.links.new(h.outputs[0],out.inputs[0])
def render(name,angle=0,part='turret'):
 pivot.rotation_euler.z=math.radians(angle)
 for o in mov:o.hide_render=part=='base'
 for o in sta:
  o.hide_render=part=='turret' and args.iteration<5
  for i,mat in enumerate(original_base_materials[o.name]):o.data.materials[i]=holdout if args.iteration>=5 and part=='turret' else mat
 bpy.context.view_layer.update()
 if relight:relight()
 s.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
render('base-raw.png',part='base');render('base-control-raw.png',123.75,'base');render('assembly-reference-raw.png',part='assembly')
for i in (range(32) if args.all else range(0,32,4)):render(f'turret-{i:03d}-raw.png',i*11.25)
(OUT/'geometry.json').write_text(json.dumps({'kind':args.kind,'pass':args.iteration,'source':SOURCE.name,'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'frame_size':[SIZE,SIZE],'native_density':K,'runtime_scale':1,'ground_origin_source':[64,ORIGIN],'ground_origin_frame':[SIZE/2,ORIGIN+PAD],'pivot_world':list(pivot.location),'pivot_frame':[x+PAD for x in screen(pivot.location)],'objects':records,'palette_samples':role_colors,'facings':32 if args.all else 8,'base_occlusion_holdout':args.iteration>=5,'footprint_target':'1x1 with separate original GDI bib; not installed','limitations':['explicit UV islands and source-palette directional shading; unseen surfaces inferred' if args.iteration>=6 else 'single-view projected source texturing; unseen panels inferred/reused','sonic source cropped at top/right/bottom; geometry completes those edges','no live game validation']},indent=2)+'\n');print('SMALL_TURRET_STUDY_RENDERED',args.kind,args.iteration)
