"""Part-bounded source coating for the accepted Nod geometry. No vertex changes.
Wheel UVs sample their own reference wheel; barrel UVs sample only the barrel.
Non-metal colored source marks are excluded from the metal coating so red straps
remain actual separate meshes, not duplicated photographed overlays.
"""
import math
import bpy

def apply_projection(c):
    image=c['image'];rgba=list(image.pixels[:]);metal=image.copy();metal.name='SOURCE / neutral metal coating only'
    for i in range(0,len(rgba),4):
        r,g,b=[round(x*255) for x in rgba[i:i+3]]
        # Keep blue/neutral metal and fine dark engraving, reject red/copper paint.
        if r>g+9 or r>b+9:rgba[i+3]=0
    metal.pixels[:]=rgba;metal.pack()
    m=bpy.data.materials.new('SOURCE / bounded metal coating');m.use_nodes=True;n=m.node_tree.nodes;n.clear();tex=n.new('ShaderNodeTexImage');tex.image=metal;tex.interpolation='Closest';tex.extension='CLIP';mix=n.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(*map(c['lin'],c['pal']['steel3']),1);em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');links=m.node_tree.links;links.new(tex.outputs['Alpha'],mix.inputs[0]);links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);links.new(em.outputs[0],out.inputs[0]);c['mats']['coating']=m
    for o in c['objects']:o.data.materials.append(m)
    def project(x,y,z):return 80+20*(x+y)/math.sqrt(2),138+20*((x-y)/math.sqrt(8)-z*math.sqrt(3)/2)
    report=[]
    for o in c['objects']:
        wheel=o.name.startswith(('Main trunnion','Lower bearing'))
        barrel=o.name=='Stepped armored barrel jacket'
        if not (wheel or barrel):continue
        selected=[]
        side=-1 if ' -1' in o.name else 1
        for poly in o.data.polygons:
            if wheel and poly.normal.x*side<.15:continue
            # Bore walls and end faces keep physical shading; coat the exterior jacket.
            if barrel and abs(poly.normal.y)>.85:continue
            selected.append(poly.index)
            for loop in poly.loop_indices:
                p=o.data.vertices[o.data.loops[loop].vertex_index].co
                # Hidden opposite metal face reuses its corresponding visible part,
                # not another object or a global reference-camera billboard.
                u,v=project(abs(p.x),p.y,p.z)
                o.data.uv_layers.active.data[loop].uv=(u/161,1-v/161)
        if selected:
            o['textured_faces']=selected;o['texture_key']='coating';report.append({'object':o.name,'coated_faces':len(selected),'source_component':'matching bearing' if wheel else 'barrel jacket'})
    return report

def apply(c):
    if c['args'].iteration == 7:
        return apply_projection(c)
    # Pass 7 review rejected photographed gear edges on modeled teeth. Only a
    # narrow undecorated metal strip is used now, with geometry-owned shading.
    image=c['image']; materials=[]
    for shade in range(6):
        m=bpy.data.materials.new('SOURCE / restrained metal '+str(shade));m.use_nodes=True
        n=m.node_tree.nodes;n.clear();tex=n.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest';tex.extension='CLIP'
        mix=n.new('ShaderNodeMixRGB');mix.inputs[0].default_value=.22
        mix.inputs[1].default_value=(*map(c['lin'],c['pal']['steel'+str(shade)]),1)
        em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');links=m.node_tree.links
        links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);links.new(em.outputs[0],out.inputs[0])
        c['mats']['coating'+str(shade)]=m;materials.append(m)
    if c['args'].iteration>=9:
        # Broad metal/paint faces need coherent tonal modeling as well as edge
        # lines. Local height modulation complements world-normal lighting;
        # source-palette quantization retains stepped pixel-art tones, not noise.
        for key,m in c['mats'].items():
            if not key.startswith(('steel','red','coating')):continue
            n=m.node_tree.nodes;links=m.node_tree.links;em=next(node for node in n if node.type=='EMISSION')
            old=em.inputs[0].links[0] if em.inputs[0].links else None
            coord=n.new('ShaderNodeTexCoord');xyz=n.new('ShaderNodeSeparateXYZ');links.new(coord.outputs['Generated'],xyz.inputs[0])
            ramp=n.new('ShaderNodeMapRange');ramp.inputs['From Min'].default_value=0;ramp.inputs['From Max'].default_value=1;ramp.inputs['To Min'].default_value=.84;ramp.inputs['To Max'].default_value=1.08;links.new(xyz.outputs['Z'],ramp.inputs['Value'])
            mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
            if old:
                upstream=old.from_socket;links.remove(old);links.new(upstream,mix.inputs[1])
            else:mix.inputs[1].default_value=em.inputs[0].default_value
            links.new(ramp.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0])
    report=[]
    for o in c['objects']:
        for m in materials:o.data.materials.append(m)
        if not o.name.startswith(('Main trunnion','Lower bearing','Stepped armored barrel jacket')):continue
        selected=[]
        for poly in o.data.polygons:
            if o.name.startswith('Stepped') and abs(poly.normal.y)>.85:continue
            if not o.name.startswith('Stepped') and abs(poly.normal.x)>.85:continue
            # Clean source strip only: no teeth, red straps, vents or bore details.
            for j,loop in enumerate(poly.loop_indices):
                u,v=((102.5,52.5),(105.5,52.5),(105.5,65.5),(102.5,65.5))[j%4]
                o.data.uv_layers.active.data[loop].uv=(u/161,1-v/161)
            selected.append(poly.index)
        o['textured_faces']=selected;o['texture_key']='coating';o['coating_shaded']=True
        report.append({'object':o.name,'coated_faces':len(selected),'source_cut':[102,52,106,66],'blend':.22,'geometry_changed':False})
    return report
