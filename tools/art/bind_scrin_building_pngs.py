"""Direct supplied PNG bindings. Reads originals, writes only YAML/review derivatives."""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,math,hashlib
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'art-source/scrin-building-bindings';OUT.mkdir(exist_ok=True)
items=[('SFAC','headquarter.png',1.25),('REAC','power plant.png',1),('REA2','advanced power.png',1.25),('PROC.SCRIN','refinery.png',1.25),('PORT','portal.png',1),('WSPH','warp sphere.png',1.25),('NERV','nerve center.png',1),('SCRT','tech center.png',1),('GRAV','gravity stabilizer.png',1.25),('SIGN','signal transmitter.png',1.25)]
rules=['# Supplied Scrin art; footprints, production, docking and gameplay remain unchanged.'];seq=['# Direct originals, no recolored/experimental shadow paths.'];binding={};palettes=[];board=Image.new('RGBA',(1600,1500),(76,83,70,255))
for i,(actor,file,scale) in enumerate(items):
 path=ROOT/'mods/ca/bits/scrin/buildings'/file;source=Image.open(path);indexed=source.mode=='P';im=source.convert('RGBA');assert im.size==(256,256)
 yy,xx=np.where(np.array(im)[:,:,3]>0)
 # Bring sprawling ground roots inside the front edges, leaving docking/exit lanes clear.
 oy=-max(0,math.ceil(float(np.max(yy+.5+.5*np.abs(xx+.5-128)-256))))-2
 h=math.ceil(float(np.max((128-yy-.5+.5*np.abs(xx+.5-128)-oy)*scale)))+4
 bounds=round(2896*scale);image=actor.lower()+'.supplied';palette='scrin-source-'+actor.lower().replace('.','-')
 if indexed:palettes.extend(['\tPaletteFromPng@'+palette+':','\t\tName: '+palette,'\t\tFilename: bits/scrin/buildings/'+file])
 binding[actor]={'image':image,'filename':'bits/scrin/buildings/'+file,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'scale':scale,'offset':[0,oy],'bounds':bounds,'height':h,'palette':palette if indexed else None}
 rules.extend([actor+':','\t-WithBuildingBib:','\tRenderSprites:','\t\tImage: '+image,'\tIsometricSelectable:',f'\t\tBounds: {bounds}, {bounds}',f'\t\tHeight: {h}',f'\t\tDecorationBounds: {bounds}, {bounds}',f'\t\tDecorationHeight: {h}'])
 if indexed:rules.extend(['\tWithSpriteBody:','\t\tPalette: '+palette,'\t\tIsPlayerPalette: false'])
 if actor in ('PROC.SCRIN','WSPH','NERV'):rules.extend(['\tWithDeathAnimation:','\t\tDeathSequencePalette: '+palette,'\t\tDeathPaletteIsPlayerPalette: false'])
 if actor=='GRAV':rules.extend(['\t-WithIdleOverlay@orb:','\t-WithIdleOverlay@pylon:'])
 rules.append('');seq.extend([image+':','\tInherits: '+actor.lower()])
 for state in ('idle','damaged-idle','place','dead')+(('build','damaged-build','pdox','damaged-pdox') if actor=='SFAC' else ()):
  seq.extend(['\t'+state+':','\t\tFilename: bits/scrin/buildings/'+file,'\t\tStart: 0','\t\tFrames: 0','\t\tLength: 1','\t\tFacings: 1','\t\tShadowStart: -1',f'\t\tScale: {scale:g}','\t\tAnchor: Bottom',f'\t\tOffset: 0,{oy}'])
 seq.append('')
 tile=Image.new('RGBA',(400,500),(76,83,70,255));d=ImageDraw.Draw(tile);ox,oyscreen=200,375;hw=128*scale;hh=64*scale
 corners=[(ox,oyscreen-hh),(ox+hw,oyscreen),(ox,oyscreen+hh),(ox-hw,oyscreen)]
 d.line(corners+[corners[0]],fill=(140,155,105),width=1)
 art=im.resize((round(256*scale),round(256*scale)),Image.Resampling.NEAREST);tile.alpha_composite(art,(round(ox-128*scale),round(oyscreen+(-192+oy)*scale)))
 d=ImageDraw.Draw(tile);d.text((8,8),actor+' -> '+file,fill='white');d.text((8,25),f'Scale {scale:g} / Bottom offset 0,{oy}',fill='white')
 top=[(x,y-h) for x,y in corners];d.line(top+[top[0]],fill=(185,195,130),width=1)
 for k in (1,2,3):d.line([corners[k],top[k]],fill=(185,195,130),width=1)
 board.alpha_composite(tile,(i%4*400,i//4*500))
rules.extend(['SFAC.Nukular:','\tRenderSprites:','\t\tImage: sfac.supplied',''])
(ROOT/'mods/ca/rules/scrin/supplied-building-art.yaml').write_text('\n'.join(rules+['World:']+palettes+['','EditorWorld:']+palettes)+'\n')
(ROOT/'mods/ca/sequences/scrin-supplied-buildings.yaml').write_text('\n'.join(seq)+'\n')
(OUT/'binding.json').write_text(json.dumps(binding,indent=2)+'\n');board.save(OUT/'placement-review.png')
print(json.dumps(binding,indent=2))
