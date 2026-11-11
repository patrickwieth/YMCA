"""Blender 3.6 geometry study: rebuild visible sentry features, one direction only.
Source image and earlier prototypes remain untouched. Hidden mechanics are inferred.
"""
from pathlib import Path
import math
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art-source/soviet-sentry/attempt-03'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.eevee.taa_render_samples = 1
scene.eevee.use_gtao = False
scene.render.resolution_x = scene.render.resolution_y = 192
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.filter_size = .01
scene.render.dither_intensity = 0
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.world.color = (0, 0, 0)

colors = {'ink': (42,38,39), 'dark': (53,50,52), 'shade': (65,63,65),
          'armor': (85,81,82), 'light': (121,113,105), 'edge': (133,126,119),
          'red_dark': (58,33,30), 'red': (76,49,45), 'red_light': (107,53,49)}
materials = {}
for name, rgb in colors.items():
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.node_tree.nodes.clear()
    emit = mat.node_tree.nodes.new('ShaderNodeEmission')
    def linear(x):
        x /= 255
        return x / 12.92 if x <= .04045 else ((x + .055) / 1.055) ** 2.4
    emit.inputs['Color'].default_value = (*map(linear, rgb), 1)
    output = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
    mat.node_tree.links.new(emit.outputs[0], output.inputs['Surface'])
    materials[name] = mat
pivot = bpy.data.objects.new('turret_pivot', None)
scene.collection.objects.link(pivot)


def finish(obj, name, family='gray'):
    obj.name = name
    obj.parent = pivot
    for m in materials.values():
        obj.data.materials.append(m)
    keys = list(materials)
    bpy.context.view_layer.update()
    key = Vector((-.6, -.8, 1.4)).normalized()
    for face in obj.data.polygons:
        normal = (obj.matrix_world.to_3x3() @ face.normal).normalized()
        dot = normal.dot(key)
        if family in materials:
            c = family
        elif family == 'redmetal':
            c = 'red_light' if dot > .4 else 'red' if dot > -.25 else 'red_dark'
        else:
            c = 'light' if normal.z > .65 else 'armor' if dot > .2 else 'shade' if dot > -.3 else 'dark'
        face.material_index = keys.index(c)
        face.use_smooth = False
    return obj


def box(name, pos, dims, family='gray', bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    o = bpy.context.object
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('plate_chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o, name, family)


def mesh(name, verts, faces, family='gray'):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    return finish(obj, name, family)


def profile(name, yz, left, right, family='gray'):
    # Cross-section in Y/Z, extruded across X. Explicit slope instead of a beveled cube.
    n = len(yz)
    verts = [(x, y, z) for x in (left, right) for y, z in yz]
    faces = [tuple(range(n-1, -1, -1)), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh(name, verts, faces, family)


def cylinder(name, pos, radius, depth, family='gray', vertices=16, forward=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=pos)
    obj = bpy.context.object
    if forward:
        obj.rotation_euler.x = math.pi/2
    return finish(obj, name, family)


def ring(name, x, y, z, outer, inner, thickness, family='gray', n=12, forward=False):
    verts = []
    for depth, radius in [(-thickness/2,outer),(thickness/2,outer),(-thickness/2,inner),(thickness/2,inner)]:
        for i in range(n):
            angle = 2*math.pi*i/n
            a, b = radius*math.cos(angle), radius*math.sin(angle)
            verts.append((x+a,y+depth,z+b) if forward else (x+a,y+b,z+depth))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),
                      (i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    return mesh(name,verts,faces,family)


def rod(name,a,b,radius=.014,family='ink'):
    a,b=Vector(a),Vector(b)
    obj=cylinder(name,(a+b)/2,radius,(b-a).length,family,8)
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return obj

# Distinct bearing tiers and red vertical segments, not a featureless dark cylinder.
cylinder('bearing_lower_ring',(0,.12,.09),.90,.18,'dark',20)
cylinder('bearing_lower_highlight',(0,.12,.19),.91,.055,'gray',20)
cylinder('bearing_red_drum',(0,.12,.40),.83,.37,'redmetal',20)
ring('bearing_upper_steel_lip',0,.12,.60,.89,.73,.075,'gray',20)
for i in range(12):
    a=i*math.tau/12
    x,y=.837*math.cos(a),.12+.837*math.sin(a)
    rod('drum_segment_seam',(x,y,.25),(x,y,.52),.012,'red_dark')

# Side profile: low vertical nose, long inclined shoulder, flat rear roof.
body=[(-.93,.70),(.85,.70),(.98,.86),(.98,1.86),(.70,2.00),(-.28,2.00),(-.95,1.38)]
profile('sloped_main_housing',body,-.79,.79,'redmetal')
# Front gun carrier is separate from the sloped sensor/dach section.
box('front_receiver_rim',(0,-.951,1.13),(1.53,.105,.57),'dark',.045)
box('front_receiver_red_plate',(0,-1.012,1.14),(1.40,.035,.46),'redmetal',.025)
# Frontal angled red panel with a recessed dark rectangular sensor/window.
profile('front_sloping_red_armor',[(-.87,1.46),(-.32,1.97),(-.30,2.015),(-.90,1.49)],-.48,.48,'redmetal')
window=[(-.77,1.57),(-.57,1.76),(-.59,1.79),(-.80,1.60)]
profile('sensor_window_frame',window,-.33,.33,'dark')
profile('sensor_window_glass',[(-.75,1.61),(-.59,1.76),(-.60,1.775),(-.77,1.63)],-.27,.27,'shade')

# Separate thick chamfered side plates, inset face, perimeter seams and fasteners.
plate=[(-.86,.76),(.71,.76),(.91,.91),(.91,1.79),(.66,1.92),(-.31,1.92),(-.88,1.37)]
inset=[(-.73,.87),(.65,.87),(.79,1.00),(.79,1.71),(.59,1.80),(-.27,1.80),(-.76,1.32)]
for sign in (-1,1):
    lo,hi=sorted((sign*.80,sign*.99))
    profile('side_thick_plate',plate,lo,hi)
    lo,hi=sorted((sign*.995,sign*1.015))
    profile('side_inset_armor',inset,lo,hi,'armor')
    for j,(y,z) in enumerate(inset):
        yy,zz=inset[(j+1)%len(inset)]
        rod('side_plate_outline',(sign*1.018,y,z),(sign*1.018,yy,zz),.012)
    for y,z in [(-.65,.97),(.65,1.02),(.62,1.64),(-.28,1.69)]:
        bolt=cylinder('side_recessed_bolt',(sign*1.027,y,z),.040,.018,'dark',8)
        bolt.rotation_euler.y=math.pi/2
        bolt=cylinder('bolt_center',(sign*1.041,y,z),.017,.01,'edge',6)
        bolt.rotation_euler.y=math.pi/2
    # A narrow additional rear equipment/vent assembly, visible behind the side plate.
    box('rear_side_equipment',(sign*.86,1.06,1.34),(.24,.24,.87),'dark',.025)
    for z in (1.01,1.16,1.31,1.46,1.61):
        box('rear_vent_rib',(sign*1.0,1.06,z),(.025,.22,.045),'gray')

# Stepped roof: a rear ledge and a separately framed central plate.
box('rear_roof_ledge',(0,.81,2.015),(1.60,.39,.105),'redmetal',.025)
box('roof_plate_underlay',(0,.19,2.025),(1.24,.99,.035),'ink',.02)
box('roof_main_plate',(0,.19,2.060),(1.17,.92,.065),'gray',.035)
for y in (-.20,.57):
    for x in (-.49,.49):
        cylinder('roof_fastener',(x,y,2.099),.025,.014,'dark',6)

# Two actual multi-tube gun assemblies. Collars are OPEN rings, not solid discs.
# Six tubes are a plausible reconstruction; the exact hidden barrel count is uncertain.
for sign in (-1,1):
    x=sign*.43;z=1.15
    cylinder('gun_socket_rim',(x,-1.07,z),.285,.13,'dark',12,True)
    ring('gun_socket_steel',x,-1.15,z,.25,.19,.08,'gray',12,True)
    for j in range(6):
        a=j*math.tau/6
        bx,bz=x+.145*math.cos(a),z+.145*math.sin(a)
        cylinder('separate_barrel_tube',(bx,-1.86,bz),.055,1.48,'gray',8,True)
        ring('individual_muzzle_rim',bx,-2.615,bz,.068,.036,.09,'gray',8,True)
        cylinder('recessed_muzzle_bore',(bx,-2.601,bz),.035,.008,'ink',8,True)
    for y in (-1.38,-1.95,-2.40):
        ring('open_barrel_cluster_collar',x,y,z,.229,.181,.085,'gray',12,True)
        for a in (0,math.pi/2,math.pi,3*math.pi/2):
            bx,bz=x+.213*math.cos(a),z+.213*math.sin(a)
            box('collar_fastener',(bx,y-.05,bz),(.045,.025,.045),'dark')
    cylinder('central_spindle',(x,-1.85,z),.032,1.42,'dark',8,True)

# Fixed orthographic viewpoint; no lights or textures obscure geometry evaluation.
target=Vector((0,-.18,.95))
bpy.ops.object.camera_add()
camera=bpy.context.object
camera.name='reference_view_camera'
camera.location=target+Vector((7,-7,math.sqrt(98)/math.sqrt(3)))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'
camera.data.ortho_scale=5.4
scene.camera=camera
scene['study']='v3 geometry only: no source emblem/texture, inferred rear and six-tube mechanism'
scene.render.filepath=str(OUT/'geometry-source-colors.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'sentry-geometry-v3.blend'))
bpy.ops.render.render(write_still=True)
# A second neutral view makes the lack of new texturing explicit.
for obj in scene.objects:
    if obj.type=='MESH':
        keys=list(materials)
        for f in obj.data.polygons:
            name=keys[f.material_index]
            if name in ('red','red_dark','red_light'):
                f.material_index=keys.index({'red':'armor','red_dark':'dark','red_light':'light'}[name])
scene.render.filepath=str(OUT/'geometry-neutral.png')
bpy.ops.render.render(write_still=True)
print('SENTRY_V3_GEOMETRY_COMPLETE')
