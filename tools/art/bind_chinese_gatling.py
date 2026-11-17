"""Native Gatling art and shared twin-gun phase; retain all eight weapon-stage settings."""
from pathlib import Path
import json,sys,re
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'tools'));from test_soviet_building_art import traits
ART=ROOT/'art-source/chinese gatling/integration';B=json.loads((ART/'binding.json').read_text());old=traits((ART/'rules-before.yaml').read_text());offset=', '.join(','.join(map(str,v)) for v in B['local_offsets'])
lines=['# Native twin Gatling: existing AA/ground stages, ammo, mines, costs and power retained.','chgatling:','\tRenderSprites:','\t\tImage: chgatling.native','\tBodyOrientation:','\t\tQuantizedFacings: 32','\t\tUseClassicFacings: False','\t\tCameraPitch: 85','\t\tUseClassicPerspectiveFudge: false','\tTurreted:',f'\t\tOffset: 0,0,{B["mount"][2]}','\tWithSpriteBody:','\t\tSequence: base','\tWithSpriteTurret:','\t\tSequence: turret','\t\tRecoils: false','\tSharedBarrelSequence:','\tIsometricSelectable:','\t\tBounds: 1448, 1448',f'\t\tHeight: {B["selection_height"]}',f'\t\tDecorationBounds: {B["decoration_bounds"]}, {B["decoration_bounds"]}',f'\t\tDecorationHeight: {B["selection_height"]}']
for key,value in old.items():
 if not key.startswith('Armament@GAT'):continue
 lines.extend(['\t-'+key+':','\t'+key.replace('Armament@','SharedBarrelArmament@')+':'])
 value=re.sub(r'(\n\tLocalOffset: )[^\n]+',lambda m:m[1]+offset,value)
 lines.extend('\t'+line for line in value.splitlines() if line.strip())
(ROOT/'mods/ca/rules/china/gatling-art.yaml').write_text('\n'.join(lines)+'\n')
seq=['# RGBA native parts; original icon/construction/overlays and Soviet1x1 bib retained.','chgatling.native:','\tInherits: chgatling']
for state in ['idle','damaged-idle','base','damaged-base','dead','turret','damaged-turret']:
 turret=state.endswith('turret');file='gatling-turret32.png' if turret else 'gatling-base.png';off=B['turret_offset'] if turret else B['body_offset']
 seq.extend(['\t'+state+':','\t\tFilename: bits/china/buildings/'+file,'\t\tStart: 0','\t\tLength: 1',f'\t\tFacings: {32 if turret else 1}','\t\tUseClassicFacings: False',f'\t\tScale: {B["scale"] if turret else B.get("body_scale",B["scale"])}','\t\tAnchor: Center',f'\t\tOffset: {off[0]},{off[1]}'])
seq.extend(['\tmuzzle:','\t\tFilename: minigun.shp','\t\tStart: 0','\t\tLength: 2','\t\tFrames: 0,2,6,8,12,14,18,20,24,26,30,32,36,38,42,44','\t\tStride: 2','\t\tFacings: 8','\t\tTick: 40','\t\tScale: 1','\t\tOffset: 0,0'])
(ROOT/'mods/ca/sequences/chinese-gatling.yaml').write_text('\n'.join(seq)+'\n')
