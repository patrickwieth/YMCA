"""Part-local native-density chitin coating from bounded source armor cuts.
No whole-reference projection, no random RGB noise, no source file writes.
"""
import bpy

def apply(c):
 mats=c['mats'];source=c['source_image'];src=list(source.pixels[:]);w,h=source.size;density=c['D']
 cuts=c.get('surface_cuts',[(130,43,146,57),(105,63,117,75),(70,74,82,87),(119,174,134,189)])
 patches=[]
 for x0,y0,x1,y1 in cuts:
  patch=[]
  for y in range(y0,y1):
   row=[]
   for x in range(x0,x1):
    k=((h-1-y)*w+x)*4;r,g,b,alpha=src[k:k+4]
    # Reject non-metal cyan/vascular details rather than repeat them on armor.
    row.append((r+g+b)/3 if alpha>.95 and g-r<.07 and r-g<.055 else None)
   patch.append(row)
  vals=[v for row in patch for v in row if v is not None];mean=sum(vals)/max(1,len(vals))
  patches.append([[1.0 if v is None else max(.76,min(1.16,.94+(v/max(mean,.001)-1)*c.get('coating_contrast',.18))) for v in row] for row in patch])
 atlas=bpy.data.images.new('Bounded source armor coating / native texels',64,64,alpha=True,float_buffer=True);atlas.colorspace_settings.name='Non-Color';pixels=[]
 for y in range(64):
  for x in range(64):
   patch=patches[(x//16+3*(y//16))%len(patches)];yy=y%16;xx=x%16
   value=patch[yy][xx] if yy<len(patch) and xx<len(patch[0]) else 1.0
   pixels.extend((value,value,value,1))
 atlas.pixels[:]=pixels;atlas.pack();added=[]
 for shade in range(9):
  old=mats['shell'+str(shade)];m=old.copy();key='coatedshell'+str(shade);m.name=key;n=m.node_tree.nodes;links=m.node_tree.links;em=next(node for node in n if node.type=='EMISSION');color=tuple(em.inputs[0].default_value)
  uv=n.new('ShaderNodeUVMap');uv.uv_map='Native chitin finish';tex=n.new('ShaderNodeTexImage');tex.image=atlas;tex.interpolation='Closest';tex.extension='REPEAT';links.new(uv.outputs[0],tex.inputs[0]);mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[1].default_value=color;links.new(tex.outputs['Color'],mix.inputs[2]);links.new(mix.outputs[0],em.inputs[0]);mats[key]=m;added.append(m)
 targets=c.get('coating_targets',('Main receiver','Accelerator overlapping','Deep accelerator','Yaw external bowed','Yaw shoulder','Yaw magazine scalloped','Fixed armored thorax','Fixed standing armor','Fixed thorax upright','Rotating organic neck','Rotating curved side'))
 for o in c['objects']:
  for m in added:o.data.materials.append(m)
  if o['family']!='shell' or not o.name.startswith(targets):continue
  o['family']='coatedshell';uv=o.data.uv_layers.new(name='Native chitin finish');minimum=[min(v.co[a] for v in o.data.vertices) for a in range(3)]
  for f in o.data.polygons:
   axes=sorted(range(3),key=lambda a:abs(f.normal[a]))[:2]
   for li in f.loop_indices:
    v=o.data.vertices[o.data.loops[li].vertex_index].co
    uv.data[li].uv=((v[axes[0]]-minimum[axes[0]])*density/64,(v[axes[1]]-minimum[axes[1]])*density/64)
  o['bounded_surface_cuts']=str(cuts)
