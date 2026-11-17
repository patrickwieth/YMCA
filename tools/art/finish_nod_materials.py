"""Material-only continuation of accepted Nod pass09: plate tone and source coating."""
import bpy

def apply(c):
    mats=c['mats'];start=len(mats);source=c['image'];src=list(source.pixels[:]);w,h=source.size
    image=bpy.data.images.new('Nod part-local metal finish',128,128,alpha=True,float_buffer=True);image.colorspace_settings.name='Non-Color'
    values=[]
    for y in range(128):
        for x in range(128):
            # Controlled broad machining/coating tone changes, never RGB noise.
            value=.82 if y<12+x//8 else .94 if y<35+x//9 else 1.04
            if 29<=x<37 or 76<=x<82:value*=.91
            values.append(value)
    cut=[]
    for y in range(52,66):
        row=[]
        for x in range(102,106):
            k=((h-1-y)*w+x)*4;row.append(sum(src[k:k+3])/3)
        cut.append(row)
    mean=sum(map(sum,cut))/56
    for ox,oy in ((7,21),(24,41),(51,19),(79,47),(98,29)):
        for y,row in enumerate(cut):
            for x,luma in enumerate(row):values[(oy+y)*128+ox+x]*=max(.72,min(1.12,luma/max(mean,1e-6)))
    # Sparse finish breaks at panel edges, not rust pasted over mechanisms.
    for x,y,length in ((4,14,3),(18,32,2),(42,22,3),(65,45,2),(87,13,3),(103,38,2)):
        for dx in range(length):values[y*128+x+dx]*=.68
        values[(y+1)*128+x]*=1.12
    pixels=[]
    for value in values:pixels.extend((value,value,value,1))
    image.pixels[:]=pixels;image.pack()
    for family in ('steel','red'):
        for shade in range(6):
            name='finished'+family+str(shade);m=mats[family+str(shade)].copy();m.name=name;n=m.node_tree.nodes;links=m.node_tree.links
            em=next(node for node in n if node.type=='EMISSION');up=em.inputs[0].links[0].from_socket
            uv=n.new('ShaderNodeUVMap');uv.uv_map='Native Nod finish';tex=n.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest';tex.extension='EXTEND';links.new(uv.outputs[0],tex.inputs[0])
            mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;links.new(up,mix.inputs[1]);links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);mats[name]=m
    for o in c['objects']:
        for m in list(mats.values())[start:]:o.data.materials.append(m)
        if o.get('constant') or o['family'] not in ('steel','red'):continue
        family='finished'+o['family'];o['family']=family
        if o.get('coating_shaded'):o['texture_key']=family
        uv=o.data.uv_layers.new(name='Native Nod finish');mins=[min(v.co[a] for v in o.data.vertices) for a in range(3)]
        for p in o.data.polygons:
            axes=sorted(range(3),key=lambda a:abs(p.normal[a]))[:2]
            for li in p.loop_indices:
                v=o.data.vertices[o.data.loops[li].vertex_index].co
                uv.data[li].uv=((2+(v[axes[0]]-mins[axes[0]])*20)/128,(2+(v[axes[1]]-mins[axes[1]])*20)/128)
