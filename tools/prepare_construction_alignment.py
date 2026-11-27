"""Align shared construction foundations to resolved PNG body ground anchors.
Input: --audit-building-icons OUTPUT.json.sequences.json. Original art is read-only.
The anchor is the center of the visible isometric ground diamond (bottom minus
one quarter of its width), not the center of a tall building's bounding rectangle.
A second input from --export-legacy-construction-anchors supplies native SHP anchors.
Voxel-only bodies without an idle sprite keep their existing registration.
"""
from pathlib import Path
import json,sys
from PIL import Image
from prepare_faction_building_icons import parse
ROOT=Path(__file__).resolve().parents[1]

def ground(path):
    with Image.open(path) as source:
        im=source.convert('RGBA');w,h=im.size;x0,y0,x1,y1=im.getchannel('A').getbbox()
    return ((x0+x1)/2-w/2,y1-h/2-(x1-x0)/4),(w,h)

def main():
    seq=json.loads(Path(sys.argv[1]).read_text());out=['# Generated from resolved PNG ground registration; idle art/timing remain unchanged.'];records=[]
    for image,text in sorted(seq.items()):
        nodes=parse(text)
        if 'construction' not in nodes or 'idle' not in nodes:continue
        idle={**nodes.get('Defaults',{}),**nodes['idle']};file=idle.get('Filename','')
        path=ROOT/'mods/ca'/file
        if not file.lower().endswith('.png') or not path.exists():continue
        # Multipart frames and clipped sheets need explicit registration, not a sheet-wide bbox.
        if 'Combine' in idle or idle.get('Start','0')!='0' or idle.get('Frames','0')!='0' or int(idle.get('Facings','1'))!=1:continue
        source,(w,h)=ground(path)
        offset=[float(v) for v in idle.get('Offset','0,0').split(',')];scale=float(idle.get('Scale','1'))
        anchor=w/4-h/2 if idle.get('Anchor','Center')=='Bottom' else 0
        target=((source[0]+offset[0])*scale,(source[1]+offset[1]+anchor)*scale)
        # All stages of a family share a canvas/foundation. Register to its first stage;
        # preserve the small intentional per-stage silhouette differences.
        import re
        construction=text.split('\nconstruction:',1)[-1] if '\nconstruction:' in text else text
        found=re.search(r'Filename: (make-(?:small|medium|big)1\.png)',construction)
        if not found:continue
        stage=ROOT/'mods/ca/bits/misc'/found[1];base,_=ground(stage)
        aligned=tuple(round(target[i]-base[i],3) for i in range(2))
        out.extend(['',image+':','\tconstruction:','\t\tAnchor: Center',f'\t\tOffset: {aligned[0]:g},{aligned[1]:g}'])
        records.append(dict(image=image,source=file,scale=scale,target=target,construction=found[1],offset=aligned))
    legacy=json.loads(Path(sys.argv[2]).read_text())
    for row in legacy:
        out.extend(['',row['image']+':','\tconstruction:','\t\tAnchor: Center',f"\t\tOffset: {row['offset'][0]:g},{row['offset'][1]:g}"])
        records.append(row)
    (ROOT/'mods/ca/sequences/construction-alignment.yaml').write_text('\n'.join(out)+'\n')
    p=ROOT/'art-source/construction-alignment';p.mkdir(exist_ok=True)
    (p/'registration.json').write_text(json.dumps(records,indent=2)+'\n')
    print(f'Aligned {len(records)} building namespaces, including {len(legacy)} native legacy sprite anchors.')
if __name__=='__main__':main()
