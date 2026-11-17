"""Bind approved SAM pass12 and visibility-only ammo states, without resampling."""
from pathlib import Path
import shutil
from PIL import Image
from PIL.PngImagePlugin import PngInfo
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'art-source/nod sam site';DEST=ROOT/'mods/ca/bits/nod/buildings'
shutil.copy2(SOURCE/'ammo-variants/base.png',DEST/'sam-base.png')
for n in range(5):shutil.copy2(SOURCE/f'ammo-variants/turret-{n}.png',DEST/f'sam-turret-{n}.png')
meta=PngInfo();meta.add_text('FrameSize','256,256');meta.add_text('FrameAmount','1')
Image.open(SOURCE/'pass-12/review/assembly-004.png').save(DEST/'sam-place.png',pnginfo=meta)
lines=['# Approved pass12: fixed base, five ammo states, native pixels, facing start4.','nsam.native:','\tInherits: nsam']
# Approved60% size; move20 source pixels /12 screen pixels upward on the1x1 bib.
for name,file,facings,offset in [('idle','base',1,'-5,-32'),('damaged-idle','base',1,'-5,-32'),('base','base',1,'-5,-32'),('damaged-base','base',1,'-5,-32'),('dead','base',1,'-5,-32'),('place','place',1,'-5,-32')]+[(f'turret-{n}',f'turret-{n}',32,'9,13') for n in range(5)]:
 lines += [f'\t{name}:',f'\t\tFilename: bits/nod/buildings/sam-{file}.png','\t\tStart: 0','\t\tLength: 1',f'\t\tFacings: {facings}','\t\tUseClassicFacings: false','\t\tScale: 0.6','\t\tAnchor: Center',f'\t\tOffset: {offset}']
(ROOT/'mods/ca/sequences/nod-sam.yaml').write_text('\n'.join(lines)+'\n')
print('Nod SAM runtime art installed without changing approved source pixels.')
