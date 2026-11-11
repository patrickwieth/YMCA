"""Blender 3.6: first separate Heavy Blaster reconstruction, not a finished match.
Original image untouched. Reference cuts, nearest textures, no procedural wear.
"""
from pathlib import Path
import bpy,math,json
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/gdi ion cannon/attempt-01'
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene
s.render.engine='BLENDER_EEVEE';s.render.resolution_x=s.render.resolution_y=320;s.render.resolution_percentage=100
s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.render.film_transparent=True
s.render.dither_intensity=0;s.render.filter_size=.01;s.eevee.taa_render_samples=1
s.eevee.use_gtao=True;s.eevee.gtao_distance=1.5;s.eevee.gtao_factor=1.15
s.view_settings.view_transform='Standard';s.view_settings.look='Medium High Contrast';s.view_settings.exposure=0;s.view_settings.gamma=1
samples=json.loads((OUT/'source-samples.json').read_text())
def sample(x,y):
 def linear(v):
  v=v/255;return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
 return tuple(linear(v) for v in samples[f'{x},{y}'][:3])
def mat(name,xy,texture=None,glow=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();links=m.node_tree.links
 out=n.new('ShaderNodeOutputMaterial');em=n.new('ShaderNodeEmission');em.inputs[0].default_value=(*sample(*xy),1);em.inputs[1].default_value=1 if glow else .9
 if texture:
  tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'textures'/f'{texture}.png'));tex.interpolation='Closest';tex.extension='EXTEND';links.new(tex.outputs['Color'],em.inputs['Color'])
 if glow:links.new(em.outputs[0],out.inputs[0])
 else:
  diffuse=n.new('ShaderNodeBsdfDiffuse');diffuse.inputs[0].default_value=(*sample(*xy),1);diffuse.inputs[1].default_value=.8
  if texture:links.new(tex.outputs['Color'],diffuse.inputs[0])
  mix=n.new('ShaderNodeMixShader');mix.inputs[0].default_value=.12;links.new(em.outputs[0],mix.inputs[1]);links.new(diffuse.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],out.inputs[0])
 return m
materials={
 'tan':mat('source tan',(153,91)), 'brown':mat('source copper',(111,137)),
 'dark':mat('source dark seams',(112,150)), 'steel':mat('source steel',(155,184)),
 'light':mat('source pale edge',(156,88)), 'cyan':mat('source cyan',(164,140),glow=True),
}
for name in ('barrel-side','barrel-top','muzzle','receiver-side','display','side-window','roof-panel','base-front','base-side','gear','steel','support'):
 materials['tex-'+name]=mat('cut '+name,(153,91),name,name in ('display','side-window','muzzle'))
pivot=bpy.data.objects.new('turret_pivot',None);s.collection.objects.link(pivot);pivot.location=(0,0,.6)
def finish(o,name,material,rotating=True):
 o.name=name;o.data.materials.append(materials[material])
 if rotating:o.parent=pivot # coordinates are local to the bearing pivot
 return o

def box(name,pos,dims,material='tan',bevel=0,rotating=True):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.dimensions=dims;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('structural bevel','BEVEL');mod.width=bevel;mod.segments=1;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,material,rotating)

def mesh(name,verts,faces,material='tan',rotating=True):
 data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update();o=bpy.data.objects.new(name,data);s.collection.objects.link(o);return finish(o,name,material,rotating)

def profile(name,yz,x0,x1,material):
 n=len(yz);verts=[(x,y,z) for x in (x0,x1) for y,z in yz]
 return mesh(name,verts,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],material)

def cylinder(name,pos,radius,depth,material='steel',axis='Z',n=16,rotating=True):
 bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=radius,depth=depth,location=pos);o=bpy.context.object
 if axis=='X':o.rotation_euler.y=math.pi/2
 if axis=='Y':o.rotation_euler.x=math.pi/2
 return finish(o,name,material,rotating)

def panel(name,corners,material,rotating=True):
 o=mesh(name,corners,[(0,1,2,3)],material,rotating);uv=o.data.uv_layers.new()
 for loop,coord in zip(o.data.polygons[0].loop_indices,((0,1),(1,1),(1,0),(0,0))):uv.data[loop].uv=coord
 return o

def side_panel(name,x,y0,y1,z0,z1,material):
 return panel(name,[(x,y0,z1),(x,y1,z1),(x,y1,z0),(x,y0,z0)],material)
# Stationary armored plinth: chamfered plan and source-cut side details.
box('stationary plinth',(0,0,.24),(3.5,3.3,.48),'steel',.25,False)
box('stationary top plate',(0,0,.49),(3.45,3.25,.13),'dark',.25,False)
for x in (-1.48,1.48):
 for y in (-1.35,1.35):box('stationary corner armor',(x,y,.27),(.48,.48,.55),'steel',.08,False)
for x0,x1 in ((-1.3,-.35),(.35,1.3)):
 panel('source-cut base front',[(x0,-1.659,.44),(x1,-1.659,.44),(x1,-1.659,.04),(x0,-1.659,.04)],'tex-base-front',False)
panel('source-cut base gear',[(-.36,-1.666,.5),(.36,-1.666,.5),(.36,-1.666,0),(-.36,-1.666,0)],'tex-gear',False)
for y0,y1 in ((-1.2,-.25),(.25,1.2)):
 panel('source-cut base side',[(1.759,y0,.45),(1.759,y1,.45),(1.759,y1,.03),(1.759,y0,.03)],'tex-base-side',False)
cylinder('stationary socket',(0,0,.58),1.23,.13,'dark',n=32,rotating=False)
cylinder('rotating bearing',(0,0,.08),1.16,.18,'steel',n=32)
cylinder('bearing upper',(0,0,.18),1.08,.08,'brown',n=32)
# A-frame trunnion supports; receiver passes between the two arms.
for sign in (-1,1):
 x0,x1=sorted((sign*.72,sign*1.03))
 profile('angled trunnion support',[(-.95,.15),(-.97,.63),(-.04,1.79),(.47,1.71),(.58,1.07),(.21,.2)],x0,x1,'tan')
 profile('support inset',[(-.75,.33),(-.76,.57),(.05,1.57),(.31,1.5),(.37,1.03),(.11,.35)],sign*1.035,sign*1.04,'brown')
 cylinder('trunnion dark surround',(sign*1.045,.05,1.67),.31,.09,'dark','X')
 cylinder('trunnion pale rim',(sign*1.10,.05,1.67),.28,.10,'light','X')
 cylinder('trunnion copper cap',(sign*1.16,.05,1.67),.235,.025,'brown','X')
 for y in (-.16,.13,.42):
  cylinder('capacitor cyan core',(sign*1.05,y,.65),.065,.45,'cyan',n=12)
  for z in (.4,.9):cylinder('capacitor cap',(sign*1.05,y,z),.09,.07,'steel',n=12)
 # Thin physical cyan channel on the arm, not a painted generated glow.
 rod=box('support cyan conductor',(sign*1.045,-.45,1.02),(.018,.075,.58),'cyan');rod.rotation_euler.x=-.58
# Receiver and long split shroud, forward is -Y (reference view points SW).
profile('rear receiver',[(-.85,1.52),(-.7,2.22),(-.32,2.65),(.95,2.65),(1.39,2.33),(1.4,1.68),(.8,1.44)],-.68,.68,'tan')
box('receiver underside',(0,.45,1.5),(1.20,1.7,.23),'dark',.08)
profile('barrel lower spine',[(-3.15,1.37),(-3.15,1.66),(-.75,1.88),(-.45,1.6),(-.7,1.3)],-.59,.59,'brown')
for sign in (-1,1):
 x0,x1=sorted((sign*.23,sign*.64))
 profile('split barrel upper rail',[(-3.12,1.73),(-3.12,2.11),(-1.0,2.25),(-.5,2.1),(-.8,1.78)],x0,x1,'tan')
 side_panel('source barrel armor',sign*.648,-2.95,-.96,1.54,2.1,'tex-barrel-side')
 # Split top rails remain separated by a deep central channel.
 panel('source upper rail',[(x0,-3.0,2.12),(x0,-1.02,2.26),(x1,-1.02,2.26),(x1,-3.0,2.12)],'tex-barrel-top')
 box('barrel cyan lower conductor',(sign*.65,-1.99,1.56),(.035,2.15,.065),'cyan')
box('central dark channel',(0,-1.9,1.99),(.34,2.2,.08),'dark')
box('central cyan rail',(0,-1.9,2.038),(.11,1.9,.035),'cyan')
# Recessed rectangular aperture, with genuine depth and structural collar.
box('muzzle aperture dark backing',(0,-3.165,1.75),(1.11,.035,.78),'dark')
for x in (-.60,.60):box('muzzle upright rim',(x,-3.20,1.76),(.075,.12,.86),'steel')
for z in (1.34,2.18):box('muzzle horizontal rim',(0,-3.20,z),(1.27,.12,.075),'light')
panel('source muzzle aperture',[(-.48,-3.188,2.08),(.48,-3.188,2.08),(.48,-3.188,1.45),(-.48,-3.188,1.45)],'tex-muzzle')
# Source-derived receiver detailing, roof plate, blue windows and side monitor.
for sign in (-1,1):
 side_panel('source receiver armor',sign*.688,-.3,1.14,1.87,2.44,'tex-receiver-side')
 side_panel('source side window',sign*.704,-.79,-.3,1.67,1.98,'tex-side-window')
 box('monitor dark frame',(sign*.77,.84,1.89),(.17,.7,.57),'dark',.035)
 side_panel('source cyan monitor',sign*.868,.57,1.10,1.68,2.08,'tex-display')
 for y in (.13,.47,.81):
  box('upper dark vent',(sign*.51,y,2.665),(.25,.23,.06),'dark')
  box('upper cyan vent',(sign*.51,y,2.702),(.16,.17,.018),'cyan')
panel('source roof panel',[(-.32,-.28,2.662),(-.32,.45,2.662),(.32,.45,2.662),(.32,-.28,2.662)],'tex-roof-panel')
# Orthographic source-like projection. Render studies only; source is not overpainted.
target=Vector((0,-.25,1.25));bpy.ops.object.camera_add();cam=bpy.context.object
cam.name='reference_camera';cam.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=7.0;s.camera=cam # 32px extra padding per side; density unchanged
bpy.ops.object.light_add(type='SUN',location=(-3,-5,8));light=bpy.context.object;light.rotation_euler=(.5,-.35,-.45);light.data.energy=1.1;light.data.angle=0
s.world=bpy.data.worlds.new('neutral world');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[0].default_value=(.4,.4,.4,1);s.world.node_tree.nodes['Background'].inputs[1].default_value=.3
for image in bpy.data.images:
 if image.source=='FILE':image.pack()
bpy.context.view_layer.update();v=world_to_camera_view(s,cam,pivot.matrix_world.translation)
(OUT/'attachment.json').write_text(json.dumps({'frame_size':[320,320],'bearing_pivot_screen':[v.x*320,(1-v.y)*320],'status':'first geometry study, not game calibrated','source':'heavy blaster 3.png'},indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'heavy-blaster-v1.blend'))
for angle in (0,45,180,315):
 pivot.rotation_euler.z=math.radians(angle);bpy.context.view_layer.update();s.render.filepath=str(OUT/f'render-{angle:03d}-raw.png');bpy.ops.render.render(write_still=True)
print('HEAVY_BLASTER_V1_STUDY_COMPLETE')
