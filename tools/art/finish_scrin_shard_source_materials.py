"""Material-only source treatment. Mesh coordinates/topology/attachments are untouched.
Native-size hand-defined relief fields, source-sampled ramps, bounded crystal cuts.
Hidden crystal sides and relief patterns are inferred, not copied foreign artwork.
"""
from pathlib import Path
import bpy,json,math
from mathutils import Vector

def apply(c):
 root=c['ART']/'material-reference'
 if c['S']>=33:root=root/('source38' if c['S']>=38 else 'source37' if c['S']>=37 else 'source34' if c['S']>=34 else 'source33')
 reference=json.loads((root/'reference.json').read_text());stage=c['S'];objects=c['objects'];mats=c['mats'];D=c['D'];added=[]
 if stage>=39:
  from paint_scrin_surface_networks import eligible,paint
 armor_cuts=json.loads((root/'armor-cuts.json').read_text()) if stage>=33 else []
 # Pack independent surface fields; no repeating procedural noise or random image stamps.
 W=1024;H=1024;pixels=[.5,.5,.5,1.]*(W*H);tiles=[];px=2;py=2;rowh=0
 for o in objects:
  role='vein' if o['family']=='vein' else 'cyan' if o['family']=='cyan' else 'shell' if o['family']=='shell' or o['family'].startswith(('relief','coatedshell')) else None
  if role is None:continue
  o['material_role']=role
  for f in o.data.polygons:f.use_smooth=role!='cyan'
  o.data.use_auto_smooth=True;o.data.auto_smooth_angle=math.radians(55 if stage>=32 else 35)
  o.color=(max(0,min(1,(o['bias']+4-(1 if role=='vein' else 0))/8)),(o['shade']+1)/10 if o['shade']>=0 else 0,0,1)
  tone=o.data.uv_layers.new(name='Source material tone')
  for f in o.data.polygons:
   bias=(2 if f.index in o.get('inner_lit_faces',[]) else 0)+(3 if f.index in o.get('rim_lit_faces',[]) else 0)
   for li in f.loop_indices:tone.data[li].uv=(bias/8,0)
  uv=o.data.uv_layers.new(name='Source native relief');positions={};width=8;height=8;detail=False
  old=o.data.uv_layers.get('Native chitin edge relief')
  if stage>=40 and o.name.startswith(('Accelerator overlapping dorsal armor','Deep accelerator trough')):
   q=len(o.data.vertices)//4
   width=max(8,round((o.data.vertices[q].co-o.data.vertices[0].co).length*D))
   height=max(8,round(sum((o.data.vertices[k+1].co-o.data.vertices[k].co).length for k in range(q-1))*D))
   for f in o.data.polygons:
    for li in f.loop_indices:
     row,k=divmod(o.data.loops[li].vertex_index,q);positions[li]=(row%2,k/(q-1))
   detail=True
  elif role=='shell' and old:
   positions={li:tuple(old.data[li].uv) for f in o.data.polygons for li in f.loop_indices}
   vertical=o.name.startswith('Fixed ');axis=2 if vertical else 1
   width=max(8,min(96,round((max(v.co[axis] for v in o.data.vertices)-min(v.co[axis] for v in o.data.vertices))*D)))
   height=80 if not vertical else 36;detail=True
  elif (stage>=32 and role=='vein') or (role=='shell' and (o.name.startswith(('Rotating curved side carapace','Rotating organic neck','Rotating lower crystal magazine','Yaw compact cervical','Main receiver')) or (stage>=39 and eligible(o.name)))):
   first=sorted(o.data.polygons[0].vertices);n=first[2] if len(first)==4 else 0
   if n>=6 and len(o.data.vertices)%n==0:
    rows=len(o.data.vertices)//n;centers=[sum((o.data.vertices[j*n+k].co for k in range(n)),Vector())/n for j in range(rows)];dist=[0.]
    for j in range(1,rows):dist.append(dist[-1]+(centers[j]-centers[j-1]).length)
    width=max(8,min(96,round(dist[-1]*D)));height=max(8,min(128,round(max(sum((o.data.vertices[j*n+k].co-o.data.vertices[j*n+(k+1)%n].co).length for k in range(n)) for j in range(rows))*D)))
    for f in o.data.polygons:
     seam=any(k%n==n-1 for k in f.vertices) and any(k%n==0 for k in f.vertices)
     for li in f.loop_indices:
      vi=o.data.loops[li].vertex_index;j,k=divmod(vi,n);positions[li]=(dist[j]/max(.001,dist[-1]),1 if seam and k==0 else k/n)
    detail=role=='shell'
  if px+width+3>=W:px=2;py+=rowh+4;rowh=0
  assert py+height+3<H,'Relief atlas capacity exceeded'
  for y in range(height):
   for x in range(width):
    value=.5
    if detail:
     # Source-like stepped rim, dark inset, fine secondary curved growth seam.
     if x in (0,width-1):value=.22
     elif x==1:value=.72
     elif x==2:value=.59
     elif x==width-2:value=.35
     elif x==width-3:value=.55
     line=round(width*.52+min(3,width*.12)*math.sin(y/max(1,height-1)*math.pi))
     if height*.16<y<height*.83 and width>=13:
      if x==line:value=.31
      elif x==line+1:value=.62
     # Broad, coherent material planes, not mottling/noise.
     if value==.5 and x>width*.65:value=.54
    if stage>=32 and role=='vein':value=.40+(.32 if stage>=33 else .18)*max(0,math.cos((y/max(1,height-1)-.12)*math.tau))**6
    if stage>=33 and o.name.startswith('Accelerator overlapping dorsal armor'):
     cut=armor_cuts[int(o.name.rsplit(' ',1)[1])%5]['pixels'];xx=x-(width-12)//2;yy=y-round(height*.66-11)
     if 0<=xx<12 and 0<=yy<22 and cut[yy][xx] is not None:
      rgb=cut[yy][xx];weight=min(1,(xx+1)/2,(12-xx)/2,(yy+1)/2,(22-yy)/2)
      value+=weight*max(-.20,min(.20,(sum(rgb)/3-55)/145))
    k=((py+y)*W+px+x)*4;pixels[k:k+4]=[value,0.,0.,1.] if stage>=39 else [value,value,value,1.]
  for f in o.data.polygons:
   for li in f.loop_indices:
    u,v=positions.get(li,(.5,.5));uv.data[li].uv=((px+.5+u*(width-1))/W,(py+.5+v*(height-1))/H)
  if stage>=39 and role=='shell' and detail and eligible(o.name):paint(pixels,W,px,py,width,height,o.name,stage)
  tiles.append({'object':o.name,'size':[width,height],'detailed':detail,'vascular_field':stage>=39 and role=='shell' and detail and eligible(o.name)});px+=width+4;rowh=max(rowh,height)
 # End caps previously collapsed to a single UV column. Give broad caps their own
 # native-scale planar chart so posterior surfaces receive real material structure.
 if stage>=39:
  for o in objects:
   if o.get('material_role')!='shell' or not eligible(o.name):continue
   uv=o.data.uv_layers['Source native relief']
   for f in o.data.polygons:
    coords=[uv.data[li].uv for li in f.loop_indices]
    if min((max(p.x for p in coords)-min(p.x for p in coords))*W,(max(p.y for p in coords)-min(p.y for p in coords))*H)>=2:continue
    axes=sorted(range(3),key=lambda a:abs(f.normal[a]))[:2];points=[o.data.vertices[k].co for k in f.vertices];lo=[min(v[a] for v in points) for a in axes];hi=[max(v[a] for v in points) for a in axes];cw,ch=[min(96,round((b-a)*D)) for a,b in zip(lo,hi)]
    if min(cw,ch)<6 or cw*ch<100:continue
    if px+cw+3>=W:px=2;py+=rowh+4;rowh=0
    assert py+ch+3<H,'Cap relief atlas capacity exceeded'
    for y in range(ch):
     for x in range(cw):
      k=((py+y)*W+px+x)*4;pixels[k:k+4]=[.5,0.,0.,1.]
    paint(pixels,W,px,py,cw,ch,o.name+' cap',stage)
    for li in f.loop_indices:
     v=o.data.vertices[o.data.loops[li].vertex_index].co;u=(v[axes[0]]-lo[0])/(hi[0]-lo[0]);t=(v[axes[1]]-lo[1])/(hi[1]-lo[1]);uv.data[li].uv=((px+.5+u*(cw-1))/W,(py+.5+t*(ch-1))/H)
    tiles.append({'object':o.name,'face':f.index,'size':[cw,ch],'detailed':True,'vascular_field':True,'cap_chart':True});px+=cw+4;rowh=max(rowh,ch)
 if stage>=41:
  # A single continuous plexus across the five overlapping receiver plates. Separate
  # tiny copies of the same motif looked like a repeated circuit-board stamp.
  plates=[o for o in objects if o.name.startswith('Receiver sculpted overlapping plate')]
  centers=[sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices) for o in plates]
  direction=(centers[2]-centers[1]).normalized();lo=min(v.co.dot(direction) for o in plates for v in o.data.vertices);hi=max(v.co.dot(direction) for o in plates for v in o.data.vertices)
  cw=max(12,round((hi-lo)*D));ch=round(max(sum((o.data.vertices[k+1].co-o.data.vertices[k].co).length for k in range(20))*D for o in plates))
  if px+cw+3>=W:px=2;py+=rowh+4;rowh=0
  assert py+ch+3<H
  for y in range(ch):
   for x in range(cw):
    k=((py+y)*W+px+x)*4;pixels[k:k+4]=[.5,0.,0.,1.]
  paint(pixels,W,px,py,cw,ch,'Receiver continuous carapace field',stage)
  for o in plates:
   uv=o.data.uv_layers['Source native relief'];q=len(o.data.vertices)//4
   for f in o.data.polygons:
    for li in f.loop_indices:
     vi=o.data.loops[li].vertex_index;v=o.data.vertices[vi].co;u=(v.dot(direction)-lo)/(hi-lo);t=(vi%q)/(q-1);uv.data[li].uv=((px+.5+u*(cw-1))/W,(py+.5+t*(ch-1))/H)
  tiles.append({'object':'Receiver continuous carapace field','shared_objects':[o.name for o in plates],'size':[cw,ch],'detailed':True,'vascular_field':True})
 atlas=bpy.data.images.new('Source-like native chitin relief',W,H,alpha=True,float_buffer=True);atlas.colorspace_settings.name='Non-Color';atlas.pixels[:]=pixels;atlas.pack()
 def material(name,role,image=None,uv_name=None):
  m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear();links=m.node_tree.links
  def node(kind):return n.new(kind)
  def mathnode(op,a,b=None):
   z=node('ShaderNodeMath');z.operation=op
   if isinstance(a,(int,float)):z.inputs[0].default_value=a
   else:links.new(a,z.inputs[0])
   if b is not None:
    if isinstance(b,(int,float)):z.inputs[1].default_value=b
    else:links.new(b,z.inputs[1])
   return z.outputs[0]
  geom=node('ShaderNodeNewGeometry');dot=node('ShaderNodeVectorMath');dot.operation='DOT_PRODUCT';links.new(geom.outputs['Normal'],dot.inputs[0]);dot.inputs[1].default_value=tuple(Vector((-.35,-.55,.86)).normalized())
  info=node('ShaderNodeObjectInfo');channels=node('ShaderNodeSeparateRGB');links.new(info.outputs['Color'],channels.inputs[0])
  light=mathnode('ADD',mathnode('MULTIPLY',dot.outputs['Value'],3.0 if stage>=33 else 3.4),2.75 if stage>=33 else 3.45 if stage>=32 else 4.2)
  bias=mathnode('SUBTRACT',mathnode('MULTIPLY',channels.outputs['R'],8),4);light=mathnode('ADD',light,bias)
  tone=node('ShaderNodeUVMap');tone.uv_map='Source material tone';xyz=node('ShaderNodeSeparateXYZ');links.new(tone.outputs[0],xyz.inputs[0]);light=mathnode('ADD',light,mathnode('MULTIPLY',xyz.outputs['X'],8))
  fixed=mathnode('GREATER_THAN',channels.outputs['G'],.05);light=mathnode('ADD',mathnode('MULTIPLY',light,mathnode('SUBTRACT',1,fixed)),mathnode('MULTIPLY',fixed,mathnode('SUBTRACT',mathnode('MULTIPLY',channels.outputs['G'],10),1)))
  tex=node('ShaderNodeTexImage');tex.image=image or atlas;tex.interpolation='Linear' if stage>=33 and image and not (stage>=37 and '-facets' in image.name) else 'Closest';tex.extension='EXTEND';uv=node('ShaderNodeUVMap');uv.uv_map=uv_name or ('Source crystal planes' if image else 'Source native relief');links.new(uv.outputs[0],tex.inputs[0])
  if stage>=42 and image is None:
   # Avoid exact nearest-texel boundary ties changing with draw-pass batching.
   # This is a tiny fraction of a native texel, not a geometry/placement change.
   stable=node('ShaderNodeVectorMath');stable.operation='ADD';links.new(uv.outputs[0],stable.inputs[0]);stable.inputs[1].default_value=(.023/W,.017/H,0);links.new(stable.outputs[0],tex.inputs[0])
  if stage>=37 and image and '-facets' in image.name:
   xyz=node('ShaderNodeSeparateXYZ');links.new(uv.outputs[0],xyz.inputs[0]);wrapped=node('ShaderNodeCombineXYZ');links.new(mathnode('FRACT',xyz.outputs['X']),wrapped.inputs[0]);links.new(mathnode('MINIMUM',mathnode('MAXIMUM',xyz.outputs['Y'],.00001),.99999),wrapped.inputs[1]);links.new(wrapped.outputs[0],tex.inputs[0])
  if image:
   mix=node('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;links.new(tex.outputs['Color'],mix.inputs[1]);factor=mathnode('ADD',.78,mathnode('MULTIPLY',mathnode('MAXIMUM',dot.outputs['Value'],0),.24));links.new(factor,mix.inputs[2]) if stage<32 else setattr(mix.inputs[2],'default_value',(.96,.96,.96,1));color=mix.outputs[0]
  else:
   value=node('ShaderNodeSeparateRGB' if stage>=39 else 'ShaderNodeRGBToBW');links.new(tex.outputs['Color'],value.inputs[0]);light=mathnode('ADD',light,mathnode('MULTIPLY',mathnode('SUBTRACT',value.outputs[0],.5),8))
   ramp=node('ShaderNodeValToRGB');ramp.color_ramp.interpolation='LINEAR' if stage>=32 else 'CONSTANT';elements=ramp.color_ramp.elements
   for j,rgb in enumerate(reference['ramps'][role]):
    e=elements[j] if j<2 else elements.new(j/8);e.position=j/8;e.color=(*[c['linear'](v) for v in rgb],1)
   if stage>=32:light=mathnode('DIVIDE',mathnode('FLOOR',mathnode('MULTIPLY',light,2)),2)
   links.new(mathnode('DIVIDE',light,8),ramp.inputs[0]);color=ramp.outputs[0]
   if stage>=39 and role=='shell':
    vascular=node('ShaderNodeValToRGB');vascular.color_ramp.interpolation='LINEAR';elements=vascular.color_ramp.elements
    for j,rgb in enumerate(reference['ramps']['vein']):
     e=elements[j] if j<2 else elements.new(j/8);e.position=j/8;e.color=(*[c['linear'](v) for v in rgb],1)
    level=mathnode('ADD',value.outputs['G'],mathnode('MULTIPLY',dot.outputs['Value'],.07));links.new(level,vascular.inputs[0]);blend=node('ShaderNodeMixRGB');links.new(mathnode('GREATER_THAN',value.outputs['G'],.05),blend.inputs[0]);links.new(color,blend.inputs[1]);links.new(vascular.outputs[0],blend.inputs[2]);color=blend.outputs[0]
  em=node('ShaderNodeEmission');out=node('ShaderNodeOutputMaterial');links.new(color,em.inputs[0]);links.new(em.outputs[0],out.inputs[0]);return m
 families={role:material('Source stepped '+role,role) for role in ('shell','vein','cyan')}
 crystals={}
 for entry in reference['cuts']:
  image=bpy.data.images.load(str(root/(entry['name']+('-facets' if stage>=37 else '')+'.png')));image.pack();crystals[entry['name']]=material('Bounded source crystal '+entry['name'],'cyan',image)
 for role,m in list(families.items())+list(crystals.items()):
  for j in range(9):mats['sourcepaint_'+role+str(j)]=m;added.append(m)
 if stage>=32:
  image=bpy.data.images.load(str(root/'front-shin.png'));image.pack();m=material('Bounded source shin armor','shell',image,'Source shin armor');mats['source_shin']=m;added.append(m)
 if stage>=34:
  image=bpy.data.images.load(str(root/'cheek-gear.png'));image.pack();m=material('Bounded receiver axle engraving','shell',image,'Source cheek gear');mats['source_gear']=m;added.append(m)
 for o in objects:
  for m in added:o.data.materials.append(m)
  role=o.get('material_role')
  if not role:continue
  crystal='spear' if o.name=='Long faceted cyan shard' else 'battery'+o.name.rsplit(' ',1)[1] if o.name.startswith('Magazine shard ') else None
  host=o
  if stage>=32:
   if o.name.startswith('Crystal branching '):crystal='spear';host=next(v for v in objects if v.name=='Long faceted cyan shard')
   elif o.name.startswith(('Yaw crown crystal refractive edge','Yaw crown dark crystal flute')):
    index=o.name.rsplit(' ',1)[1].split('/')[0];crystal='battery'+index;host=next(v for v in objects if v.name=='Magazine shard '+index)
  if crystal:
   o['texture_faces']=[];verts=host.data.vertices;start=sum((verts[j].co for j in range(5)),Vector())/5;end=verts[-1].co;axis=(end-start).normalized();basis=Vector((1,0,0));basis=(basis-axis*axis.dot(basis)).normalized();uv=o.data.uv_layers.new(name='Source crystal planes');w,h=next(item['atlas_size'] for item in reference['cuts'] if item['name']==crystal)
   cross_extent=max(abs((v.co-start).dot(basis)) for v in verts)
   across=D/w if stage<35 else min(D/w,.48/max(.001,cross_extent))
   for f in o.data.polygons:
    for li in f.loop_indices:
     delta=o.data.vertices[o.data.loops[li].vertex_index].co-start;uv.data[li].uv=(.5+delta.dot(basis)*across,delta.dot(axis)/(end-start).length)
   if stage>=37:
    # True surface unwrap: planar X projection collapses the side faces into streaks.
    side=axis.cross(basis).normalized()
    for f in o.data.polygons:
     values=[]
     for li in f.loop_indices:
      delta=o.data.vertices[o.data.loops[li].vertex_index].co-start;theta=math.atan2(delta.dot(side),delta.dot(basis))/math.tau%1;values.append((li,theta,delta.dot(axis)/(end-start).length))
     seam=max(t for li,t,v in values)-min(t for li,t,v in values)>.5
     for li,t,v in values:uv.data[li].uv=(t+1 if seam and t<.5 else t,v)
   role=crystal
  shin_group=o.name.startswith('Fixed convex standing shin') or (stage>=33 and o.name.startswith(('Fixed recessed shin channel','Fixed shin channel transverse vane','Fixed protected shin energy conduit')))
  if stage>=32 and shin_group:
   index=int(o.name.rsplit(' ',1)[1].split('/')[0]);angle=(index+.5)*math.tau/4;tangent=Vector((-math.sin(angle),math.cos(angle),0));uv=o.data.uv_layers.new(name='Source shin armor')
   for f in o.data.polygons:
    for li in f.loop_indices:
     v=o.data.vertices[o.data.loops[li].vertex_index].co;x=131+v.dot(tangent)*D;y=229-v.z*D;uv.data[li].uv=((x-119)/25,1-(y-159)/65)
   o['texture_material']='source_shin'
   if not o.name.startswith('Fixed convex standing shin'):o['texture_faces']=[f.index for f in o.data.polygons]
  if stage>=34 and o.name.startswith('Receiver side axle recess'):
   verts=o.data.vertices;y0=min(v.co.y for v in verts);y1=max(v.co.y for v in verts);z0=min(v.co.z for v in verts);z1=max(v.co.z for v in verts);uv=o.data.uv_layers.new(name='Source cheek gear');faces=[]
   for f in o.data.polygons:
    if abs(f.normal.x)<.6:continue
    for li in f.loop_indices:
     v=verts[o.data.loops[li].vertex_index].co;uv.data[li].uv=((v.y-y0)/(y1-y0),(v.z-z0)/(z1-z0))
    faces.append(f.index)
   o['texture_material']='source_gear';o['texture_faces']=faces
  o['family']='sourcepaint_'+role;o['material_revision']='source-like tonal steps / native relief / bounded crystal cuts'
 c['notes'].append('31+: texture/material-only revision; all pass30 vertices, topology, dimensions and attachment coordinates preserved. Hidden texture patterns inferred.')
 (c['OUT']/'material-report.json').write_text(json.dumps({'pass':stage,'reference':reference,'relief_fields':tiles,'part_roles':{str(i):o.get('material_role','other') for i,o in enumerate(objects,1)},'fixed_part_ids':[i for i,o in enumerate(objects,1) if o['fixed']],'geometry_changes':False},indent=2)+'\n')
