"""Material-only follow-up to the accepted pass13 Allied flak geometry.
Separate neutral pedestal paint, cut metal and dark vent cavities. No new meshes,
whole-image projection or noise. Source wear is bounded to existing armor stiles.
"""
import bpy

def apply(c):
    mats=c['mats'];original_count=len(mats)
    families={
        'pedestal':[(20,25,28),(36,42,46),(57,65,69),(78,87,91),(103,114,119),(133,147,153)],
        'machined':[(18,23,29),(36,43,51),(60,70,80),(88,103,113),(124,144,155),(167,189,198)],
        'cavity':[(12,18,23),(21,28,34),(32,42,49),(47,58,65),(67,82,90),(94,113,122)],
    }
    offsets={}
    for family,colors in families.items():
        offsets[family]=len(mats)
        for i,rgb in enumerate(colors):
            m=bpy.data.materials.new('Allied finish '+family+' '+str(i));m.use_nodes=True
            n=m.node_tree.nodes;n.clear();links=m.node_tree.links
            em=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');links.new(em.outputs[0],out.inputs[0])
            col=tuple(c['lin'](v) for v in c['nearest'](rgb))+(1,)
            coord=n.new('ShaderNodeTexCoord');xyz=n.new('ShaderNodeSeparateXYZ');links.new(coord.outputs['Generated'],xyz.inputs[0])
            ramp=n.new('ShaderNodeMapRange');ramp.inputs['To Min'].default_value=.67 if family=='pedestal' else .82;ramp.inputs['To Max'].default_value=1.04
            links.new(xyz.outputs['Z'],ramp.inputs['Value'])
            tone=n.new('ShaderNodeMixRGB');tone.blend_type='MULTIPLY';tone.inputs[0].default_value=1;tone.inputs[1].default_value=col
            links.new(ramp.outputs[0],tone.inputs[2]);links.new(tone.outputs[0],em.inputs[0]);mats.append(m)
    # Source grain belongs on small painted panel faces, never across the whole
    # pedestal or around cylindrical barrels. UV islands retain native texel scale.
    offsets['paint']=len(mats)
    for i in range(6):
        m=mats[offsets['pedestal']+i].copy();m.name='Allied bounded pedestal paint '+str(i)
        n=m.node_tree.nodes;links=m.node_tree.links;em=next(x for x in n if x.type=='EMISSION');up=em.inputs[0].links[0].from_socket
        uv=n.new('ShaderNodeUVMap');uv.uv_map='Bounded pedestal paint'
        tex=n.new('ShaderNodeTexImage');tex.image=c['image'];tex.interpolation='Closest';tex.extension='CLIP';links.new(uv.outputs[0],tex.inputs[0])
        mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.32
        links.new(up,mix.inputs[1]);links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);mats.append(m)
    for o in c['objects']:
        for m in mats[original_count:]:o.data.materials.append(m)
        name=o.name
        # Leave the original blue identification band, copper conduits and pennant
        # on their own materials. Steel must not inherit their blue saturation.
        if o.get('family_base',0)==0:o['family_base']=offsets['machined']
        if name.startswith(('Fixed beveled','Fixed broad armored stile','Fixed stepped stile foot','Fixed corner reinforcing','Fixed bounded paint','Fixed inset service','Fixed panel')):
            o['family_base']=offsets['pedestal']
        if name.startswith(('Fixed vent recess','Fixed vent louver','Fixed service hatch border','Breech rear machined cap','Lower elevation drive inset')):
            o['family_base']=offsets['cavity']
        if name.startswith('Fixed broad armored stile'):
            o['family_base']=offsets['paint'];layer=o.data.uv_layers.new(name='Bounded pedestal paint')
            # The untextured paint cut contains only worn gray coating, no vent,
            # bolt or identifiable mechanical fragment duplicated onto the model.
            for p in o.data.polygons:
                coords=[o.data.vertices[o.data.loops[i].vertex_index].co for i in p.loop_indices]
                spans=[max(v[a] for v in coords)-min(v[a] for v in coords) for a in range(3)]
                axes=sorted(range(3),key=lambda a:spans[a],reverse=True)[:2]
                for li,v in zip(p.loop_indices,coords):
                    u=(v[axes[1]]-min(q[axes[1]] for q in coords))/max(spans[axes[1]],1e-9)
                    z=(v[axes[0]]-min(q[axes[0]] for q in coords))/max(spans[axes[0]],1e-9)
                    layer.data[li].uv=((43+6*u)/128,1-(91-6*z)/128)
        if name.startswith(('Fixed beveled upper cap','Fixed beveled plinth')):o['bias']=-1
