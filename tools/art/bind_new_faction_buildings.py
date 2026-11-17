"""Bind supplied originals directly. Never rewrite supplied PNGs or gameplay footprints."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,math,hashlib,sys,re
# Optional actor filter keeps unrelated supplied-art replacements out of a targeted placement fix.
ONLY=set(sys.argv[1:])
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/new-faction-buildings';sys.path.insert(0,str(ROOT/'tools'));from test_soviet_building_art import traits
OLD=json.loads((OUT/'rules-before.json').read_text());smoke=json.loads((OUT/'smoke-registration.json').read_text())
specs=[('chpower','china','power plant.png',1),('chpower2','china','advanced power plant.png',1),('chbarracks','china','barracks.png',1),('chcommand','china','command center.png',1.5),('chsupply','china','supply depot.png',1.25),('chairport','china','airfield.png',1.5),('chpropaganda','china','propaganda center.png',1.25),('chinternetcenter','china','internet center.png',1.25),('SREP','scrin','repair bay.png',1.25),('SCOL','scrin','storm column.png',.65),('RIFT','scrin','rift generator.png',1.25),('chweap','china','war factory.png',1),('chsilo','china','nuke silo.png',1.25),('CHSYRD','china','naval yard.png',1.5),('chpropagandatower','china','propaganda tower.png',1),('MANI','scrin','field manipulator.png',1),('chbunker','china','bunker.png',1),('chbunker2','china','fortified bunker.png',1)]
rules=['# Supplied originals; only presentation, smoke and discharge registration change.'];seq=[];bindings={};board=Image.new('RGBA',(2400,600*math.ceil(len(specs)/4)),(76,83,70,255))
def woffset(x,y):return [0,round(x*1448/128),round(-y*1448/64)]
def vec(v):return ','.join(map(str,v))
for i,(actor,faction,file,scale) in enumerate(specs):
 filename=f'bits/{faction}/buildings/{file}';path=ROOT/'mods/ca'/filename;im=Image.open(path).convert('RGBA');assert Image.open(path).mode=='RGBA' and im.width==im.height
 size=im.width;cx=size/2;origin=size*.75
 yy,xx=np.where(np.array(im)[:,:,3]>0);bounds=1448 if actor=='SCOL' else round(2896*scale*size/256);depth=bounds*64/1448/2
 oy=math.floor(depth/scale-float(np.max(yy+.5-origin+.5*np.abs(xx+.5-cx))))-2
 # Requested screen-space shifts, expressed as whole native sprite pixels.
 down={'chairport':8,'chbarracks':6,'chsupply':4,'chinternetcenter':8}.get(actor,0)
 if actor in ('chpower','chpower2'):down=({'chpower':12,'chpower2':24}[actor]-oy)
 oy+=down
 if actor=='chpropagandatower':oy-=12 # Previous8px up plus another4px.
 # Preserve requested deltas from the installed offsets even if source art is replaced mid-session.
 oy={'chairport':28,'chbarracks':20,'chsupply':19,'chinternetcenter':20,'chpropagandatower':-13,'chweap':2}.get(actor,oy)
 visual_y=down*scale
 height=math.ceil(float(np.max((origin-yy-.5+.5*np.abs(xx+.5-cx)-oy)*scale))-depth+visual_y)+4
 ox=36 if actor=='chweap' else 8 if actor=='chairport' else 0
 bounds_text=f'{bounds}, {bounds}'+(f', {round(ox*scale*1448/128)}, {round(visual_y*1448/64)}' if ox or visual_y else '')
 image=actor.lower()+'.supplied';states=['idle','damaged-idle','place','dead']
 if actor in ['SREP','RIFT','MANI','chsilo','CHSYRD']:states+=['active','damaged-active']
 if actor=='chcommand':states+=['build','damaged-build','pdox','damaged-pdox']
 b={'image':image,'filename':filename,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'scale':scale,'size':size,'offset':[ox,oy],'bounds':bounds,'height':height,'states':states,'extra_sequences':[]};bindings[actor]=b
 rules.extend([actor+':','\tRenderSprites:','\t\tImage: '+image,'\tIsometricSelectable:',f'\t\tBounds: {bounds_text}',f'\t\tHeight: {height}',f'\t\tDecorationBounds: {bounds_text}',f'\t\tDecorationHeight: {height}'])
 if down:b['selection_offset']=[ox*scale,visual_y]
 before=traits(OLD[actor])
 if 'WithBuildingBib' in before:rules.append('\t-WithBuildingBib:')
 # Keep the inherited Soviet decorative bib, including supplied-art buildings.
 b['bib']=before['WithIdleOverlay@CHINABIB'].split('Image: ',1)[1].splitlines()[0] if 'WithIdleOverlay@CHINABIB' in before else None
 if actor=='chinternetcenter':
  b['bib']='sovietbib4x4';rules.extend(['\tWithIdleOverlay@CHINABIB:','\t\tImage: '+b['bib']])
 if actor in ('chpower','chpower2'):
  b['bib']='sovietbib3x3';rules.extend(['\tWithIdleOverlay@CHINABIB:','\t\tImage: '+b['bib']])
 if actor in ('chbunker','chbunker2'):
  b['bib']=None
  if actor=='chbunker':rules.append('\t-WithIdleOverlay@CHINABIB:')
 b['sequence_parent']='spen.soviet' if actor=='CHSYRD' else actor.lower()
 seq.extend([image+':','\tInherits: '+b['sequence_parent']])
 for state in states:
  seq.extend(['\t'+state+':','\t\tFilename: '+filename,'\t\tStart: 0','\t\tFrames: 0','\t\tLength: 1','\t\tFacings: 1','\t\tShadowStart: -1',f'\t\tScale: {scale:g}','\t\tAnchor: Bottom',f'\t\tOffset: {ox},{oy}'])
  # The repair pad stays behind docked vehicles; other states use normal building depth.
  seq.append('\t\tZOffset: '+('-1535' if actor=='SREP' else '0'))
 if actor in ['chpower','chpower2']:
  b['smoke_pixels']=[[120,31]] if actor=='chpower' else [[55,75],[117,42]];b['extra_sequences'].append('idle-anim2')
  for j,(x,y) in enumerate(b['smoke_pixels']):
   rules.extend(['\tWithIdleOverlay@SMOKE'+('2' if j else '')+':','\t\tPalette: effect','\t\tOffset: '+vec(woffset((x-128)*scale,(y-192+oy)*scale))])
  seq.extend(['\tidle-anim2:','\t\tScale: 2','\t\tAlpha: 0.8','\t\tAnchor: Center','\t\tOffset: '+vec(smoke['sequence_offset'])])
 if actor=='chcommand':
  b['extra_sequences'].append('radar');b['radar_source']='bits/china/buildings/radar.png';b['radar_top_left']=[45+1/scale,30-1/scale]
  seq.extend(['\tradar:','\t\tFilename: '+b['radar_source'],'\t\tStart: 0','\t\tLength: 1','\t\tFacings: 1','\t\tScale: '+str(scale),'\t\tAnchor: Center',f'\t\tOffset: {-11+1/scale:.6f},{-137+oy-1/scale:.6f}','\t\tZOffset: 1024'])
 if actor=='chsilo':
  b['launch_pixel']=[116,148];x,y=b['launch_pixel'];b['launch_offset']=woffset((x-128)*scale,(y-192+oy)*scale)
  rules.extend(['\tNukePower:','\t\tSpawnOffset: '+vec(b['launch_offset'])])
 if actor=='chpropagandatower':
  b['support_pixel']=[64,32];x,y=b['support_pixel'];b['support_offset']=woffset((x-cx)*scale,(y-origin+oy)*scale)
  rules.extend(['\tArmament@PRIMARY:','\t\tLocalOffset: '+vec(b['support_offset'])])
 if actor=='SCOL':
  b['emitter_pixels']=[[129,45],[118,68],[143,65],[114,152],[149,164]]
  b['local_offsets']=[woffset((x-128)*scale,(y-192+oy)*scale) for x,y in b['emitter_pixels']]
  rules.extend(['\tArmament:','\t\tLocalOffset: '+', '.join(vec(v) for v in b['local_offsets'])])
 if actor=='RIFT':
  b['extra_sequences'].append('active-overlay')
  seq.extend(['\tactive-overlay:','\t\tFilename: '+filename,'\t\tStart: 0','\t\tFrames: '+','.join(['0']*13),'\t\tLength: 13','\t\tFacings: 1','\t\tScale: '+str(scale),'\t\tAnchor: Bottom',f'\t\tOffset: 0,{oy}','\t\tBlendMode: Additive','\t\tAlpha: 0,0.03,0.06,0.09,0.12,0.15,0.18,0.15,0.12,0.09,0.06,0.03,0','\t\tTick: 80'])
 rules.append('');seq.append('')
 tile=Image.new('RGBA',(600,600),(44,68,78,255) if actor=='CHSYRD' else (76,83,70,255));ox,ground=300,440
 if b['bib']:
  bib=Image.open(ROOT/'mods/ca/bits/soviet/buildings'/('bib'+b['bib'].removeprefix('sovietbib')+'.png')).convert('RGBA')
  tile.alpha_composite(bib,(ox-bib.width//2,ground-bib.height//2))
 d=ImageDraw.Draw(tile);hw=bounds*128/1448/2;hh=depth;ui_x=ox+b['offset'][0]*scale;ui_y=ground+visual_y;corners=[(ui_x,ui_y-hh),(ui_x+hw,ui_y),(ui_x,ui_y+hh),(ui_x-hw,ui_y)]
 d.line(corners+[corners[0]],fill=(135,155,100),width=1)
 art=im.resize((round(size*scale),round(size*scale)),Image.Resampling.NEAREST);tile.alpha_composite(art,(round(ox+(b['offset'][0]-cx)*scale),round(ground+(-origin+oy)*scale)))
 if actor=='chcommand':
  radar=Image.open(ROOT/'mods/ca'/b['radar_source']).convert('RGBA');radar=radar.resize((round(radar.width*scale),round(radar.height*scale)),Image.Resampling.NEAREST)
  rx,ry=b['radar_top_left'];tile.alpha_composite(radar,(round(ox+(rx-cx)*scale),round(ground+(ry-origin+oy)*scale)))
 d=ImageDraw.Draw(tile);d.text((8,8),actor+' -> '+file,fill='white');d.text((8,25),f'Scale {scale:g} / Bottom {b["offset"][0]},{oy}',fill='white')
 if actor=='SCOL':
  for x,y in b['emitter_pixels']:
   px=ox+(x-128)*scale;py=ground+(y-192+oy)*scale;d.ellipse((px-2,py-2,px+2,py+2),outline='yellow')
 board.alpha_composite(tile,(i%4*600,i//4*600))
for variant,parent in [('chcommand.Nukular','chcommand'),('SCOL.Temp','SCOL'),('chpropagandatower.pdl','chpropagandatower'),('chbunker.advinf','chbunker'),('chbunker.ai','chbunker'),('chbunker2.advinf','chbunker2')]:
 b=dict(bindings[parent]);bindings[variant]=b;rules.extend([variant+':','\tRenderSprites:','\t\tImage: '+b['image']])
 if parent.startswith('chbunker') or parent=='chpropagandatower':
  bounds=b['bounds'];height=b['height'];rules.extend(['\tIsometricSelectable:',f'\t\tBounds: {bounds}, {bounds}',f'\t\tHeight: {height}',f'\t\tDecorationBounds: {bounds}, {bounds}',f'\t\tDecorationHeight: {height}'])
 if parent=='chpropagandatower':rules.extend(['\tArmament@PRIMARY:','\t\tLocalOffset: '+vec(b['support_offset']),'\tArmament@pointdefense:','\t\tLocalOffset: '+vec(b['support_offset'])])
 if parent=='SCOL':rules.extend(['\tArmament:','\t\tLocalOffset: '+', '.join(vec(v) for v in b['local_offsets'])])
 rules.append('')
def save_layer(path,text,sequences=False):
 if ONLY:
  pattern=r'(?ms)^([A-Za-z][^\n:]*):\n.*?(?=^[A-Za-z][^\n:]*:\n|\Z)'
  old={m[1]:m[0] for m in re.finditer(pattern,path.read_text())}
  wanted={a.lower()+'.supplied' for a in ONLY} if sequences else ONLY
  text=re.sub(pattern,lambda m:old.get(m[1],m[0]) if m[1] not in wanted else m[0],text)
 path.write_text(text)
save_layer(ROOT/'mods/ca/rules/new-faction-building-art.yaml','\n'.join(rules)+'\n')
save_layer(ROOT/'mods/ca/sequences/new-faction-buildings.yaml','\n'.join(seq)+'\n',True)
if ONLY:
 previous=json.loads((OUT/'binding.json').read_text());bindings={a:(b if a in ONLY else previous.get(a,b)) for a,b in bindings.items()}
(OUT/'binding.json').write_text(json.dumps(bindings,indent=2)+'\n');board.save(OUT/'placement-review.png')
print({a:(b['scale'],b['offset'],b['height']) for a,b in bindings.items()})
