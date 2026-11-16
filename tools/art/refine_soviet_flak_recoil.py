"""Separate axial recoil rig and restrained painted-steel wear; no mesh redesign."""
import bpy,json
from mathutils import Vector

def apply(c):
    pivot=c['pivot'];objects=c['objects'];axis=c['axis'];scene=c['s']
    rig=bpy.data.objects.new('barrel_recoil',None);c['moving'].objects.link(rig);rig.parent=pivot
    members=[]
    for o in objects:
        if o.name.startswith(('Hollow main barrel','Stepped barrel jacket','Red barrel retaining band','Upper recoil rod','Upper auxiliary retaining band')):
            o.parent=rig;members.append(o.name)
    # Local rest transform is identity: parenting does not alter any mesh or its
    # placement. The sleeves, receiver, yokes and entire foundation stay put.
    keys=((0,0),(1,.24),(2,.24),(4,.17),(6,.10),(8,.045),(10,0),(14,0))
    for frame,travel in keys:
        rig.location=-axis*travel;rig.keyframe_insert(data_path='location',frame=frame)
    for fc in rig.animation_data.action.fcurves:
        for key in fc.keyframe_points:key.interpolation='LINEAR'
    scene.frame_start=0;scene.frame_end=14;scene.render.fps=25;scene.frame_set(0)
    # Authored, sparse chipped-coating marks. No procedural RGB noise; no sensor
    # or mechanism copied from the source onto unrelated faces. Native-density UVs.
    image=bpy.data.images.new('Soviet bounded steel wear',64,64,alpha=True,float_buffer=True)
    image.colorspace_settings.name='Non-Color';pixels=[1.,1.,1.,1.]*(64*64)
    marks=((4,9,3),(17,5,2),(30,13,3),(49,7,2),(10,26,2),(26,33,3),(43,28,2),(57,38,3),(7,47,3),(23,55,2),(39,49,2),(53,59,3))
    for x,y,length in marks:
        for dx in range(length):
            for yy,value in ((y,.64),(y+1,1.12)):
                k=(yy*64+x+dx)*4;pixels[k:k+3]=[value]*3
    image.pixels[:]=pixels;image.pack()
    original=list(c['mats'].values())
    for i in range(6):
        m=c['mats']['steel'+str(i)].copy();m.name='wornsteel'+str(i);n=m.node_tree.nodes;links=m.node_tree.links
        em=next(n for n in n if n.type=='EMISSION');up=em.inputs[0].links[0].from_socket
        tex=n.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest';tex.extension='EXTEND'
        uv=n.new('ShaderNodeUVMap');uv.uv_map='Native steel wear';links.new(uv.outputs[0],tex.inputs[0])
        mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;links.new(up,mix.inputs[1]);links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);c['mats'][m.name]=m
    for o in objects:
        for m in list(c['mats'].values())[len(original):]:o.data.materials.append(m)
        if not o.name.startswith(('Sculpted elevation yoke','Sloped front pedestal shield','Rear receiver armored closure','Fixed radial outrigger','Fixed center armored ring','Breech main receiver')):continue
        o['family']='wornsteel';uv=o.data.uv_layers.new(name='Native steel wear')
        for p in o.data.polygons:
            axes=sorted(range(3),key=lambda a:abs(p.normal[a]))[:2]
            for li in p.loop_indices:
                v=o.data.vertices[o.data.loops[li].vertex_index].co
                uv.data[li].uv=((v[axes[0]]*20+32)/64,(v[axes[1]]*20+8)/64)
    report={'barrel_members':members,'axis_local':list(axis),'maximum_travel':.24,'keyframes':keys,'fixed_footprint':'1x1 unchanged','runtime_integrated':False,'engine_note':'Stock WithSpriteBarrel recoils horizontally in turret local X; 42-degree axial recoil and changing occlusion require an angle-aware binding or rendered recoil states, not an unmodified flat sprite shift.'}
    (c['OUT']/'recoil.json').write_text(json.dumps(report,indent=2)+'\n')
    return rig

def render_preview(c):
    scene=c['s'];rig=c['recoil_rig'];out=c['OUT'];render=c['render']
    (out/'recoil').mkdir(exist_ok=True)
    for facing in (0,8,16,24):
        for frame in (0,1,2,4,6,8,10,14):
            scene.frame_set(frame);render(f'recoil/assembly-{facing:03d}-{frame:02d}.png',facing,'assembly')
    scene.frame_set(0);c['pivot'].rotation_euler.z=0
