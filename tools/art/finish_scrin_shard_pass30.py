"""Coherent native-density bevel/recess material fields. No RGB noise or repeated image stamps."""
import bpy,math

def apply(c):
 mats=c['mats'];added=[]
 targets=[o for o in c['objects'] if o['family']=='shell' and o.name.startswith(('Accelerator overlapping dorsal armor','Main receiver cocoon','Receiver sculpted overlapping plate','Fixed convex standing shin'))]
 for index,o in enumerate(targets):
  vertical=o.name.startswith('Fixed ');axis=2 if vertical else 1;lo=min(v.co[axis] for v in o.data.vertices);hi=max(v.co[axis] for v in o.data.vertices)
  width=max(8,round((hi-lo)*c['D']));height=64;image=bpy.data.images.new('Chitin bevel and inset field '+o.name,width,height,alpha=True,float_buffer=True);image.colorspace_settings.name='Non-Color';pixels=[]
  for y in range(height):
   for x in range(width):
    # Surface pigmentation follows a shallow inset and its physical edge relief.
    value=1.0
    if x==0 or x==width-1:value=.68
    elif x==1:value=1.18
    elif x==2:value=1.06
    elif x==width-2:value=.83
    elif x in (4,width-5) and 12<y<51:value=.88
    elif 6<=x<width-5 and 18<y<47:value=.96
    pixels.extend((value,value,value,1))
  image.pixels[:]=pixels;image.pack();family='relief'+str(index)
  for shade in range(9):
   m=mats['shell'+str(shade)].copy();m.name=family+str(shade);nodes=m.node_tree.nodes;links=m.node_tree.links;em=next(n for n in nodes if n.type=='EMISSION');color=tuple(em.inputs[0].default_value)
   uv=nodes.new('ShaderNodeUVMap');uv.uv_map='Native chitin edge relief';tex=nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Closest';tex.extension='EXTEND';links.new(uv.outputs[0],tex.inputs[0]);mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[1].default_value=color;links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);mats[family+str(shade)]=m;added.append(m)
  uv=o.data.uv_layers.new(name='Native chitin edge relief');xc=sum(v.co.x for v in o.data.vertices)/len(o.data.vertices);zc=sum(v.co.z for v in o.data.vertices)/len(o.data.vertices)
  for face in o.data.polygons:
   for li in face.loop_indices:
    p=o.data.vertices[o.data.loops[li].vertex_index].co
    u=(p[axis]-lo)/max(.001,hi-lo)
    if abs(face.normal[axis])>.85:u=.5
    v=(math.atan2(p.y,p.x)+math.pi)/math.tau if vertical else (math.atan2(p.z-zc,p.x-xc)+math.pi)/math.tau
    uv.data[li].uv=(u,v)
  o['family']=family;o['material_revision']='native bevel / recessed panel field, no noise'
 for o in c['objects']:
  for m in added:o.data.materials.append(m)
