"""Multi-view mechanical and material refinement; preserve V3 and fixed base.
Blender background script. --pass 1 uses source texture; pass 2 tests native LOD.
"""
from pathlib import Path
import bpy,bmesh,math,json,sys,argparse
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'art-source/gdi ion cannon';V3=BASE/'attempt-03/pass-05'
p=argparse.ArgumentParser();p.add_argument('--pass',dest='iteration',type=int,default=1);p.add_argument('--all',action='store_true');args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
OUT=BASE/f'attempt-04/pass-{args.iteration:02d}';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(V3/'heavy-blaster-v3.blend'));s=bpy.context.scene;pivot=bpy.data.objects['turret_pivot'];pivot.rotation_euler.z=0;collection=bpy.data.collections['ROTATING TURRET']
source=bpy.data.images.get('heavy blaster 3.png');assert source
roles=json.loads((BASE/'attempt-04/material-source.json').read_text());mats={}
def plain(name,rgb):
 m=bpy.data.materials.new('V4 '+name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');o=n.new('ShaderNodeOutputMaterial')
 def linear(c):
  c=c/255;return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
 e.inputs[0].default_value=(*map(linear,rgb),1);m.node_tree.links.new(e.outputs[0],o.inputs[0]);mats[name]=m;return m
for name,rgb in roles['colors'].items():plain(name,rgb)
reference=bpy.data.materials['reference nearest'];removed=[]
# These V3 shapes contain painted trunnions stretched over extruded polygon caps.
# Replace them with coaxial mechanical parts and genuinely seated, paired arms.
for name in ('near trunnion arm','far trunnion arm','trunnion cap','far trunnion cap','lower cross brace'):
 o=bpy.data.objects[name];removed.append(name);bpy.data.objects.remove(o,do_unlink=True)
records=[]
def projection(p):
 x,y,z=p;return (128+40*(x+y)/math.sqrt(2),176+40*((x-y)/math.sqrt(8)-z*math.sqrt(3)/2))
def mesh(name,verts,faces,face_uv=None):
 d=bpy.data.meshes.new(name);d.from_pydata(verts,[],faces);d.update();o=bpy.data.objects.new(name,d);collection.objects.link(o);o.parent=pivot;o.location=-pivot.location
 for m in (reference,mats['steel-mid'],mats['steel-light'],mats['steel-dark'],mats['tan'],mats['copper']):d.materials.append(m)
 uv=d.uv_layers.new()
 for i,f in enumerate(d.polygons):
  coords=face_uv.get(i) if face_uv else None
  if isinstance(coords,int):f.material_index=coords;continue
  if coords is None:coords=[projection(verts[j]) for j in f.vertices]
  for loop,(u,v) in zip(f.loop_indices,coords):uv.data[loop].uv=(u/256,1-v/256)
 records.append(name);return o

def arm(name,x0,x1):
 yz=[(-.90,.80),(-.90,1.24),(.20,2.40),(.50,2.50),(.84,2.36),(.96,2.10),(.86,1.88),(.05,.80)]
 verts=[(x,y,z) for x in (x0,x1) for y,z in yz];n=len(yz)
 # Observed near-side UVs are reused on the opposite arm, not projected through
 # the source camera onto unrelated pixels. All narrow edge faces are clean.
 pixels=[projection((.72,y,z)) for y,z in yz]
 return mesh(name,verts,[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,n+i,n+(i+1)%n,(i+1)%n) for i in range(n)],{0:pixels,1:list(reversed(pixels)),**{i+2:4 for i in range(n)}})
arm('V4 near structural arm',.72,.50);arm('V4 far structural arm',-.97,-.75)
# Cylinders are truly round and share exactly the same Y/Z axis with the gun.
AXIS=(.57,2.13)
def cylinder(name,x0,x1,radius,face='steel',segments=20):
 y,z=AXIS;verts=[(x,y+radius*math.cos(i*2*math.pi/segments),z+radius*math.sin(i*2*math.pi/segments)) for x in (x0,x1) for i in range(segments)]
 faces=[tuple(range(segments)),tuple(range(2*segments-1,segments-1,-1))]+[(i,(i+1)%segments,(i+1)%segments+segments,i+segments) for i in range(segments)]
 uv={0:1,1:1}
 if face=='source':
  pixels=[projection((.79,y+radius*math.cos(i*2*math.pi/segments),z+radius*math.sin(i*2*math.pi/segments))) for i in range(segments)]
  uv[0]=pixels;uv[1]=list(reversed(pixels))
 elif face=='copper':uv[0]=uv[1]=5
 elif face=='dark':uv[0]=uv[1]=3
 for i in range(segments):
  a=(i+.5)*2*math.pi/segments;light=-.5*math.cos(a)+.866*math.sin(a);uv[i+2]=3 if face=='dark' else (2 if light>.45 else (1 if light>-.35 else 3))
 return mesh(name,verts,faces,uv)
cylinder('V4 common trunnion shaft',-.97,.72,.19)
for side,x0,x1 in (('near',.27,.77),('far',-.52,-1.02)):
 cylinder('V4 '+side+' bearing housing',x0,x1,.35)
 outward=.035 if side=='near' else -.035
 if args.iteration>=3:
  cylinder('V4 '+side+' bearing seal',x1,x1+outward*.4,.32,'dark')
  cylinder('V4 '+side+' bearing rim',x1+outward*.4,x1+outward,.28)
  cylinder('V4 '+side+' bearing cap',x1+outward,x1+outward*1.5,.235,'copper')
 else:
  cylinder('V4 '+side+' bearing rim',x1,x1+outward,.29)
  cylinder('V4 '+side+' bearing cap',x1+outward,x1+outward*1.5,.245,'source')
# Brace actually spans between the arm roots. A short rear brace closes the
# mechanical support rather than leaving two apparently disconnected stilts.
def brace(name,y,z,depth=.18):
 verts=[(x,yy,zz) for zz in (z,z+.20) for yy in (y,y+depth) for x in (-.97,.72)]
 faces=[(0,1,5,4),(2,6,7,3),(4,5,7,6),(0,2,3,1),(0,4,6,2),(1,3,7,5)]
 # One clean source copper panel; hidden faces use source metal colors.
 return mesh(name,verts,faces,{0:[(86,151),(126,167),(126,160),(86,142)],1:5,2:5,3:3,4:4,5:4})
brace('V4 front root brace',-.84,.80);brace('V4 rear root brace',-.12,.80,.14)
# Keep the feet on the same plane as the socket, with short actual anchor pads.
# This also gives grazing views a stable horizontal contact instead of needles.
for x in (.61,-.86):
 verts=[(xx,yy,zz) for zz in (.78,.86) for yy in (-.92,.12) for xx in (x-.16,x+.16)]
 mesh('V4 arm anchor pad',verts,[(0,1,5,4),(2,6,7,3),(4,5,7,6),(0,2,3,1),(0,4,6,2),(1,3,7,5)],{i:3 if i==3 else 1 for i in range(6)})
# The V3 duplicated thin barrel panels create jagged competing edges at grazing
# angles. The full hull already carries their source detail; retain only badges.
if args.iteration>=2:
 for name in ('barrel near upper armor','barrel lower copper armor','inferred opposite barrel lower copper armor','roof conduit casing'):
  o=bpy.data.objects.get(name)
  if o:removed.append(name);bpy.data.objects.remove(o,do_unlink=True)
 # Source-only material LOD, NOT downsampling rendered sprites. Compare with pass 1.
 lod=bpy.data.images.load(str(BASE/'attempt-04/source-native-lod.png'));lod.pack();copies={}
 for o in collection.objects:
  if o.type!='MESH':continue
  if args.iteration>=5:continue # reject source-texture decimation after comparison
  if args.iteration>=3 and o.name!='closed barrel and receiver housing':continue
  for slot in o.material_slots:
   old=slot.material
   if old is None or not old.use_nodes:continue
   if not any(n.type=='TEX_IMAGE' and n.image==source for n in old.node_tree.nodes):continue
   if old.name not in copies:
    new=old.copy();new.name='native texture LOD '+old.name
    for n in new.node_tree.nodes:
     if n.type=='TEX_IMAGE' and n.image==source:n.image=lod;n.interpolation='Closest'
    copies[old.name]=new
   slot.material=copies[old.name]
if args.iteration==4:
 # Grazing/end-on views exposed a badly registered top strip. Give that actual
 # roof quad its own observed armor/channel cut instead of borrowing muzzle and
 # neighboring image pixels through a camera projection.
 housing=bpy.data.objects['closed barrel and receiver housing'];roof=housing.data.polygons[2]
 for loop,(u,v) in zip(roof.loop_indices,[(40,125),(20,115),(81,84),(101,94)]):housing.data.uv_layers.active.data[loop].uv=(u/256,1-v/256)
if args.iteration>=4:
 # Explicit checks on the modeled mechanism, not just an image similarity score.
 axes=[]
 for o in collection.objects:
  if o.type=='MESH' and (o.name=='V4 common trunnion shaft' or 'bearing ' in o.name):
   center=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
   assert abs(center.y-AXIS[0])<1e-5 and abs(center.z-AXIS[1])<1e-5,o.name
   axes.append(o.name)
 pads=[o for o in collection.objects if o.name.startswith('V4 arm anchor pad')]
 assert len(pads)==2 and all(abs(min(v.co.z for v in o.data.vertices)-.78)<1e-5 for o in pads)
 (OUT/'mechanism-check.json').write_text(json.dumps({'coaxial_parts':axes,'axis_yz':AXIS,'two_anchor_pads':True,'anchor_bottom_z':.78,'fixed_socket_top_z':.77,'gap_world_units':.01,'gap_native_pixels':.01*20*math.sqrt(3)/2},indent=2)+'\n')
# Restrained lighting is baked into source texture detail; no procedural noise.
s.render.resolution_x=s.render.resolution_y=160;bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'heavy-blaster-v4.blend'))
base=[o for o in bpy.data.collections['STATIONARY BASE'].objects if o.type=='MESH'];turret=[o for o in collection.objects if o.type=='MESH']
def render(name,size,angle=0,part='assembly'):
 pivot.rotation_euler.z=math.radians(angle)
 for o in base:o.hide_render=part=='turret'
 for o in turret:o.hide_render=part=='base'
 s.render.resolution_x=s.render.resolution_y=size;bpy.context.view_layer.update();s.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)
render('assembly-320-raw.png',320);render('base-160-raw.png',160,part='base')
for i in (range(32) if args.all else range(0,32,4)):render(f'turret-{i:03d}-raw.png',160,i*11.25,'turret')
render('base-rotation-control.png',160,123.75,'base')
(OUT/'changes.json').write_text(json.dumps({'iteration':args.iteration,'source_sha256':roles['source_sha256'],'new_parts':records,'removed_parts':removed,'common_trunnion_axis_yz':AXIS,'anchor_bottom_z':.78,'base_changed':False,'source_texture_lod':(1024 if args.iteration>=5 else ('housing only: 128; detail parts: 1024' if args.iteration>=3 else (128 if args.iteration==2 else 1024))),'render_size':160,'sprite_resampling':False,'status':'multi-view study, not gameplay integration'},indent=2)+'\n')
print('HEAVY_BLASTER_V4_PASS_COMPLETE',args.iteration)
