"""Generate modular building-only China construction bindings from engine inventory.
Input: --list-building-construction TSV. Trees/infantry are excluded by the engine.
Existing make sequences are retained as assets but no longer selected by buildings.
"""
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
rows=[line.split('\t') for line in Path(sys.argv[1]).read_text().splitlines() if '\t' in line]
images={}
for actor,image,x,y,sequence in rows:
    area=int(x)*int(y)
    size='small' if area<=2 else 'medium' if area<=9 else 'big'
    images.setdefault(image.lower(),set()).add(size)
seqs=['# Reuse the China construction stages, without inheriting obsolete make offsets/frames.']
for image,sizes in sorted(images.items()):
    seqs.extend(['',image+':'])
    for size in sorted(sizes):
        stages={'small':2,'medium':3,'big':4}[size]
        assert len(sizes)==1, (image,sizes)
        seqs.extend(['\tconstruction:','\t\tCombine:'])
        for i in range(stages):
            seqs.extend([f'\t\t\t{i}:',f'\t\t\t\tFilename: make-{size}{i+1}.png','\t\t\t\tFrames: 0, 0, 0, 0, 0','\t\t\t\tLength: 5'])
        seqs.extend([f'\t\tLength: {stages*5}','\t\tTick: 80','\t\tStart: 0','\t\tOffset: 0,0','\t\tScale: 1','\t\tFacings: 1','\t\tUseTilesetCode: false','\t\tShadowStart: -1'])
(ROOT/'mods/ca/sequences/building-construction.yaml').write_text('\n'.join(seqs)+'\n')
print(f'Generated {len(rows)} building bindings / {len(images)} image namespaces.')
