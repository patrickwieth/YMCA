"""Pack the full V13 rotation for the disposable building test map only."""
from pathlib import Path
from PIL import Image,ImageDraw
from PIL.PngImagePlugin import PngInfo
import json,math,hashlib
ROOT=Path(__file__).resolve().parents[2];STUDY=ROOT/'art-source/soviet-sentry/game-v13-8'
OUT=ROOT/'mods/ca/bits/soviet/buildings/experimental';OUT.mkdir(parents=True,exist_ok=True)
# WVec.Yaw = ArcTan(-Y, X) - 256: facings increase counterclockwise.
# OpenRA facing index: N, NW, W, SW, S, SE, E, NE.
ORDER=(225,270,315,0,45,90,135,180)
NAMES=('N','NW','W','SW','S','SE','E','NE')
source=Image.open(ROOT/'mods/ca/bits/soviet/buildings/rotational/vorlage.png').convert('RGBA')
colors={c for c in source.getdata() if c[3]}
base_path=ROOT/'mods/ca/bits/soviet/buildings/sentry base.png';base_bytes=base_path.read_bytes()
base=Image.open(base_path).convert('RGBA');sheet=Image.new('RGBA',(960,480))
attachment=json.loads((STUDY/'attachment.json').read_text());px,py=attachment['bearing_pivot_screen']
# Unchanged base sequence: Center anchor, Offset 16,-10, source socket (73,16).
# Unchanged gameplay Turreted.Offset: 0,0,112; isometric TileSize 128,64 / scale 1448.
scale=.5 # 1x1 test footprint; keep native source pixels and scale both sequences equally.
socket=(scale*(16-base.width/2+73),scale*(-10-base.height/2+16))
world_turret_y=-112*64/1448
# Sequence offsets are scaled too, but the gameplay turret world offset is not.
offset=(round(socket[0]/scale-(px-120)),round((socket[1]-world_turret_y)/scale-(py-120)))
assert offset==(8,-53),offset
vectors={e['angle']:e['gun_screen_vector'] for e in json.loads((STUDY/'projected-directions.json').read_text())}
preview=Image.new('RGB',(4*280,2*280),'#77736e');d=ImageDraw.Draw(preview)
frames=[]
for i,angle in enumerate(ORDER):
 im=Image.open(STUDY/f'final-turret-{angle:03d}.png').convert('RGBA')
 assert im.size==(240,240)
 assert {c for c in im.getdata() if c[3]}<=colors
 assert set(im.getchannel('A').getdata())<={0,255}
 box=im.getbbox();assert min(box[:2])>0 and box[2]<240 and box[3]<240
 # Ground projection has 2:1 aspect. Unflatten before checking compass bearing.
 dx,dy=vectors[angle];bearing=math.degrees(math.atan2(-dx,-2*dy))%360
 error=abs((bearing-i*45+180)%360-180);assert error<.01,(i,angle,bearing)
 sheet.alpha_composite(im,((i%4)*240,(i//4)*240))
 cx,cy=(i%4)*280+140,(i//4)*280+190
 small_base=base.resize((round(base.width*scale),round(base.height*scale)),Image.Resampling.NEAREST)
 small_turret=im.resize((120,120),Image.Resampling.NEAREST)
 preview.paste(small_base,(round(cx+scale*(16-base.width/2)),round(cy+scale*(-10-base.height/2))),small_base)
 preview.paste(small_turret,(round(cx+scale*(offset[0]-120)),round(cy+world_turret_y+scale*(offset[1]-120))),small_turret)
 d.text(((i%4)*280+8,(i//4)*280+8),f'V13 {NAMES[i]} / engine facing {i*128}',fill='white')
 frames.append({'frame':i,'engine_facing':i*128,'blender_degrees':angle,'bounds':box,'bearing_error_degrees':error})
# PngSheetLoader reads embedded PNG text chunks, NOT the YAML sidecar.
metadata=PngInfo();metadata.add_text('FrameSize','240,240');metadata.add_text('FrameAmount','8')
sheet.save(OUT/'sentry-v13.png',pnginfo=metadata)
(OUT/'sentry-v13.yaml').write_text('FrameSize: 240,240\nFrameAmount: 8\n')
preview.save(STUDY/'game-assembly-preview.png');preview.resize((2240,1120),Image.Resampling.NEAREST).save(STUDY/'game-assembly-preview-2x.png')
assert base_path.read_bytes()==base_bytes
(STUDY/'game-binding.json').write_text(json.dumps({'source_model':'attempt-13/sentry-v13.blend',
 'sheet':'bits/soviet/buildings/experimental/sentry-v13.png','frame_size':[240,240],'facings':8,
 'sequence_offset':offset,'sequence_scale':scale,'bib_image':'sovietbib1x1','sequence_anchor':'Center','source_base_sha256':hashlib.sha256(base_bytes).hexdigest(),
 'base_sequence_offset':[16,-10],'socket_in_base_png':[73,16],'bearing_pivot_in_frame':[px,py],
 'unchanged_turret_world_offset':[0,0,112],'pixel_alignment_error':[scale*(offset[0]+px-120)-socket[0],world_turret_y+scale*(offset[1]+py-120)-socket[1]],
 'scope':'Only FTUR RenderSprites.Image in disposable test-map Rules; global FTUR/TTUR keep legacy art.',
 'frames':frames},indent=2)+'\n')
print('V13 GAME SHEET COMPLETE',offset)
