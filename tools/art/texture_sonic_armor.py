"""Part-local coating variation at native texel density; preserve pass15 meshes."""
import bpy

def apply(c):
    materials=c['materials'];old_count=len(materials)
    source=c['c']['teximage'];src=list(source.pixels[:]);w,h=source.size
    image=bpy.data.images.new('SONIC bounded gold coating',64,64,alpha=True,float_buffer=True)
    image.colorspace_settings.name='Non-Color'
    # Broad coherent coating bands, not random RGB noise. Fine paint variation is
    # taken only from the clean gold shoulder cut, not from bearings or sensors.
    values=[]
    for y in range(64):
        for x in range(64):
            value=.90 if y<15+x//6 else .97 if y<32+x//8 else 1.04
            if c['iteration']>=19:value=.82 if y<12+x//9 else .90 if y<26+x//8 else .97 if y<42+x//8 else 1.03
            values.append(value)
    patch=[]
    for y in range(18,27):
        row=[]
        for x in range(30,43):
            k=((h-1-y)*w+x)*4;row.append(sum(src[k:k+3])/3)
        patch.append(row)
    mean=sum(map(sum,patch))/(13*9)
    if c['iteration']>=18:mean=sorted(v for row in patch for v in row)[int(13*9*.85)]
    placements=((16,33),(38,30),(3,14)) if c['iteration']>=17 else ((7,25),(37,43),(25,8))
    if c['iteration']>=19:placements=((3,14),(16,33),(38,30),(45,8),(24,49))
    for ox,oy in placements:
        for y,row in enumerate(patch):
            for x,luma in enumerate(row):
                weight=min(1,min(x+1,13-x,y+1,9-y)/2)
                ratio=luma/max(mean,1e-6)
                factor=max(.52,min(1.20,1+2.2*(ratio-1))) if c['iteration']>=17 else max(.66,min(1.16,ratio))
                if c['iteration']>=18:factor=max(.50,min(1.,1+1.6*(ratio-1)))
                values[(oy+y)*64+ox+x]*=1+(factor-1)*weight
    if c['iteration']>=19:
        # Sparse edge chips and short gravity-oriented coating streaks. Authored
        # positions, not random noise; retain broad readable painted planes.
        for x,y,length in ((2,13,4),(5,39,3),(17,45,2),(28,21,3),(40,53,4),(49,29,3),(58,13,2),(60,43,4)):
            for dy in range(length):values[(y+dy)*64+x]*=.58+.08*dy
            values[y*64+x+1]*=1.04
    if c['iteration']>=20:
        # Uneven small paint islands, with staggered outlines rather than a
        # repeated rectangular stamp. One light lip describes the chipped edge.
        islands=((6,21,3),(12,38,4),(21,29,3),(30,42,4),(39,19,3),(47,39,4),(54,26,3),(19,52,3),(35,10,3),(8,49,2))
        for x,y,width in islands:
            for dy in range(3):
                start=1 if dy==0 else 0;end=width-1 if dy==2 else width
                for dx in range(start,end):values[(y+dy)*64+x+dx]*=(.56,.66,.77)[dy]
            values[(y+3)*64+x+1]*=1.07
    pixels=[]
    for i,value in enumerate(values):
        if c['iteration']>=19:
            # Subtle warm coating in the low/worn regions, rather than merely
            # brightness changes on a single yellow swatch.
            warm=max(0,1-value)
            pixels.extend((value,value*(1-.12*warm),value*(1-.30*warm),1))
        else:pixels.extend((value,value,value,1))
    image.pixels[:]=pixels;image.pack()
    for shade in range(6):
        mat=materials[f'REWORK armor {shade}'].copy();name=f'REWORK coatedarmor {shade}';mat.name=name
        n=mat.node_tree.nodes;links=mat.node_tree.links;em=next(node for node in n if node.type=='EMISSION');up=em.inputs[0].links[0].from_socket
        uv=n.new('ShaderNodeUVMap');uv.uv_map='Native coating'
        tex=n.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest';tex.extension='EXTEND';links.new(uv.outputs[0],tex.inputs[0])
        mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;links.new(up,mix.inputs[1]);links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);materials[name]=mat
    for o in c['c']['moving'].objects:
        if o.type=='MESH':
            for mat in list(materials.values())[old_count:]:o.data.materials.append(mat)
    for index,(o,family,excluded) in enumerate(c['lit']):
        if family!='armor':continue
        # Existing roof photographs remain on their own bounded roof islands.
        if c['iteration']<17 and o.name.startswith('SONIC independent roof armor'):continue
        uv=o.data.uv_layers.new(name='Native coating')
        mins=[min(v.co[a] for v in o.data.vertices) for a in range(3)]
        for poly in o.data.polygons:
            axes=sorted(range(3),key=lambda a:abs(poly.normal[a]))[:2]
            for li in poly.loop_indices:
                p=o.data.vertices[o.data.loops[li].vertex_index].co
                uv.data[li].uv=((1+(p[axes[0]]-mins[axes[0]])*20)/64,(2+(p[axes[1]]-mins[axes[1]])*20)/64)
        c['lit'][index]=(o,'coatedarmor',set())
