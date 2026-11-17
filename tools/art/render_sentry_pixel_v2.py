"""Blender 3.6 single-view experiment: discrete source colors, no lighting gradients."""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'art-source/soviet-sentry'
OUT = BASE / 'attempt-02'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE / 'sentry-prototype.blend'))
scene = bpy.context.scene
scene.render.resolution_x = scene.render.resolution_y = 160
scene.eevee.use_gtao = False
scene.eevee.use_bloom = False
scene.eevee.taa_render_samples = 1
scene.render.filter_size = 0.01
scene.render.dither_intensity = 0
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.view_settings.exposure = 0
scene.view_settings.gamma = 1
# Each color is present in the supplied reference. Convert sRGB to scene-linear.
colors = {'ink': (42,38,39), 'dark': (53,50,52), 'shade': (65,63,65),
          'armor': (85,81,82), 'light': (121,113,105), 'edge': (133,126,119),
          'red_dark': (58,33,30), 'red': (76,49,45), 'red_light': (107,53,49)}

def linear(v):
    v /= 255
    return v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4

mats = {}
for name, rgb in colors.items():
    m = bpy.data.materials.new('pixel_' + name)
    m.use_nodes = True
    m.node_tree.nodes.clear()
    output = m.node_tree.nodes.new('ShaderNodeOutputMaterial')
    emission = m.node_tree.nodes.new('ShaderNodeEmission')
    emission.inputs['Color'].default_value = (*[linear(v) for v in rgb], 1)
    m.node_tree.links.new(emission.outputs[0], output.inputs['Surface'])
    mats[name] = m

light_direction = Vector((-0.6, -0.8, 1.4)).normalized()
pivot = bpy.data.objects['turret_pivot']
pivot.rotation_euler.z = 0
bpy.context.view_layer.update()
for obj in list(scene.objects):
    if obj.type != 'MESH':
        continue
    # Avoid projecting the already-shaded roof/base photo over new geometry.
    # Keep source star panels, processed into a clean source-color decal below.
    if obj.name.startswith('roof_texture'):
        obj.hide_render = True
        continue
    if obj.name.startswith('side_source_panel'):
        mat = bpy.data.materials.new('source_emblem_' + obj.name)
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        nodes.clear()
        output = nodes.new('ShaderNodeOutputMaterial')
        emission = nodes.new('ShaderNodeEmission')
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = bpy.data.images.load(str(OUT / 'side-panel-clean.png'), check_existing=True)
        tex.image.pack()
        tex.interpolation = 'Closest'
        mat.node_tree.links.new(tex.outputs['Color'], emission.inputs['Color'])
        mat.node_tree.links.new(emission.outputs[0], output.inputs['Surface'])
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        continue
    original = obj.data.materials[0].name if obj.data.materials else ''
    is_red = original in ('red', 'red_band') or obj.name.startswith('gun_mount.')
    is_ink = obj.name.startswith(('muzzle_dark_bore', 'armor_bolt', 'rear_vent_fin')) or original == 'dark'
    obj.data.materials.clear()
    for mat in mats.values():
        obj.data.materials.append(mat)
    keys = list(mats)
    for poly in obj.data.polygons:
        normal = (obj.matrix_world.to_3x3() @ poly.normal).normalized()
        light = normal.dot(light_direction)
        if is_ink:
            color = 'ink'
        elif is_red:
            color = 'red_light' if light > .4 else 'red' if light > -.25 else 'red_dark'
        else:
            color = 'light' if normal.z > .7 else 'armor' if light > .25 else 'shade' if light > -.3 else 'dark'
            if obj.name.startswith('rotor_collar'):
                color = 'edge' if light > .3 else 'shade'
        poly.material_index = keys.index(color)
        poly.use_smooth = False

# Discrete mechanical seams on the top: geometry, not painted/generated textures.
for y in (-.40, .58):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, y, 1.972))
    seam = bpy.context.object
    seam.name = 'roof_panel_seam'
    seam.dimensions = (1.04, .025, .015)
    seam.parent = pivot
    seam.data.materials.append(mats['ink'])
scene['pixel_v2'] = 'Unlit emission; categorical per-face source colors; source-only emblem; no AO or smooth shading.'
scene.render.filepath = str(OUT / 'render-raw.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'sentry-pixel-v2.blend'))
bpy.ops.render.render(write_still=True)
