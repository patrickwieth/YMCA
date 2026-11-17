"""Blender 3.6: source-textured twin rotary sentry, fixed-light 32-facing PNG render.
Run with blender --background --python this_file.py. Does not modify game bindings.
"""
import bpy
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art-source/soviet-sentry'
FRAMES = OUT / 'renders'
FRAMES.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.eevee.taa_render_samples = 1
scene.eevee.use_gtao = True
scene.eevee.gtao_distance = 2
scene.eevee.gtao_factor = 1.15
scene.render.resolution_x = 128
scene.render.resolution_y = 128
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.film_transparent = True
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'Medium High Contrast'
scene.view_settings.exposure = 0
scene.view_settings.gamma = 1
scene.world.color = (0.25, 0.25, 0.25)

materials = {}
for p in sorted((OUT / 'textures').glob('*.png')):
    mat = bpy.data.materials.new(p.stem)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get('Principled BSDF')
    bsdf.inputs['Roughness'].default_value = 1
    bsdf.inputs['Specular'].default_value = 0
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(str(p))
    tex.image.pack()
    tex.interpolation = 'Closest'
    tex.extension = 'EXTEND'
    mat.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    mat.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Emission'])
    bsdf.inputs['Emission Strength'].default_value = 0.55
    materials[p.stem] = mat

pivot = bpy.data.objects.new('turret_pivot', None)
scene.collection.objects.link(pivot)
pivot['description'] = 'Only this hierarchy rotates; camera and world light stay fixed.'


def finish(obj, name, mat):
    obj.name = name
    obj.parent = pivot
    obj.data.materials.append(materials[mat])
    return obj


def box(name, pos, size, mat='armor', bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    o = bpy.context.object
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('armor_chamfers', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o, name, mat)


def cylinder(name, pos, radius, depth, mat='steel', vertices=12, barrel=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                      end_fill_type='NGON', location=pos)
    o = bpy.context.object
    if barrel:
        o.rotation_euler[0] = math.pi / 2
    return finish(o, name, mat)


def panel(name, vertices, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], [(0, 1, 2, 3)])
    mesh.uv_layers.new()
    for loop, uv in zip(mesh.uv_layers.active.data, [(0, 0), (1, 0), (1, 1), (0, 1)]):
        loop.uv = uv
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    return finish(obj, name, material)

# Mount/bearing belongs to the turret; supplied stationary base is NOT recreated.
cylinder('bearing_bottom', (0, 0, 0.12), 0.88, 0.24, 'dark', 16)
cylinder('bearing_red_band', (0, 0, 0.38), 0.80, 0.42, 'red_band', 16)
cylinder('bearing_upper_lip', (0, 0, 0.61), 0.86, 0.10, 'edge', 16)
box('armored_body', (0, 0.10, 1.22), (1.72, 1.65, 1.30), 'armor', 0.17)
box('rear_equipment', (0, 0.98, 1.22), (1.40, 0.26, 0.96), 'red', 0.06)
box('roof_center', (0, 0.12, 1.91), (1.10, 1.28, 0.10), 'edge', 0.04)
panel('roof_texture', [(-.52, -.48, 1.967), (.52, -.48, 1.967),
                       (.52, .72, 1.967), (-.52, .72, 1.967)], 'roof')
for side in (-1, 1):
    box('side_armor_' + str(side), (side * .88, .12, 1.24), (.16, 1.46, 1.16), 'edge', .07)
    # Both sides use copied source panel, not a newly painted emblem.
    x = side * .966
    vertices = [(x, -.55, .76), (x, .76, .76), (x, .76, 1.72), (x, -.55, 1.72)]
    if side < 0:
        vertices.reverse()
    panel('side_source_panel_' + str(side), vertices, 'side_panel')
    for y in (-.54, .74):
        for z in (.82, 1.65):
            box('armor_bolt', (side * .977, y, z), (.025, .065, .065), 'dark')
    # Two three-tube rotary gun groups, with shared mechanical collars.
    x = side * .43
    cylinder('gun_mount', (x, -.80, 1.27), .31, .23, 'red', 12, True)
    cylinder('gun_mount_inner', (x, -.95, 1.27), .235, .12, 'dark', 12, True)
    for j in range(3):
        angle = 2 * math.pi * j / 3 + math.pi / 2
        bx, bz = x + .137 * math.cos(angle), 1.27 + .137 * math.sin(angle)
        cylinder('barrel_tube', (bx, -1.64, bz), .073, 1.40, 'steel', 8, True)
        cylinder('muzzle_dark_bore', (bx, -2.35, bz), .049, .018, 'dark', 8, True)
    for y in (-1.18, -1.70, -2.24):
        cylinder('rotor_collar', (x, y, 1.27), .225, .10, 'edge', 10, True)
    # Visible vent fins on the reconstructed rear; source colors only.
    for z in (.91, 1.08, 1.25, 1.42):
        box('rear_vent_fin', (side * .40, 1.125, z), (.55, .035, .055), 'dark')

bpy.ops.object.camera_add(location=(7, -10, 8))
camera = bpy.context.object
camera.name = 'fixed_isometric_camera'
# 30-degree ground elevation, consistent diamond proportions.
target = Vector((0, 0, .65))
camera.location = target + Vector((7, -7, math.sqrt(98) / math.sqrt(3)))
camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 5.8
scene.camera = camera
bpy.ops.object.light_add(type='AREA', location=(-3, -4, 8))
light = bpy.context.object
light.name = 'fixed_world_key_light'
light.data.energy = 450
light.data.size = 5
light.rotation_euler = (Vector((0, 0, 1)) - light.location).to_track_quat('-Z', 'Y').to_euler()
scene.render.image_settings.color_depth = '8'
scene['facing_count'] = 32
scene['texture_policy'] = 'Nearest-neighbour cut/paste from supplied vorlage.png; no generated texture art.'
scene['prototype'] = 'Unreviewed geometry; rear and hidden surfaces are inferred.'
# All texture images are packed, so the source .blend is self-contained.
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'sentry-prototype.blend'))
for facing in range(32):
    pivot.rotation_euler.z = facing * math.tau / 32
    scene.render.filepath = str(FRAMES / f'turret-{facing:02d}.png')
    bpy.context.view_layer.update()
    bpy.ops.render.render(write_still=True)
pivot.rotation_euler.z = 0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'sentry-prototype.blend'))
print('SENTRY_RENDER_COMPLETE: 32 frames; packed Blender source saved.')
